using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public static class ProcessCommandName
{
    private const int DefaultArgumentMaximum = 256 * 1024;
    private static readonly HashSet<string> Interpreters = new(StringComparer.Ordinal) { "node", "bun", "deno", "python", "python3", "ruby", "perl" };
    private static readonly string[] ScriptExtensions = [".js", ".mjs", ".cjs", ".ts", ".py", ".rb", ".pl"];
    private static readonly Lazy<int> ArgumentMaximum = new(ReadArgumentMaximum);

    public static string Of(int processId, string fallback) =>
        Arguments(processId) is { Count: > 0 } arguments ? FromArguments(arguments) ?? fallback : fallback;

    public static string? FromArguments(IReadOnlyList<string> arguments)
    {
        var program = BaseName(arguments[0]);
        if (program.Length == 0)
        {
            return null;
        }

        if (Interpreters.Contains(program))
        {
            var script = arguments.Skip(1).FirstOrDefault(argument => !argument.StartsWith('-'));
            return script is null ? program : StripScriptExtension(BaseName(script));
        }

        return program;
    }

    private static string BaseName(string path) => path.TrimEnd('/').Split('/').Last().TrimStart('-');

    private static string StripScriptExtension(string name)
    {
        var extension = ScriptExtensions.FirstOrDefault(candidate => name.EndsWith(candidate, StringComparison.OrdinalIgnoreCase));
        return extension is null ? name : name[..^extension.Length];
    }

    private static IReadOnlyList<string>? Arguments(int processId)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ArgumentMaximum.Value);
        try
        {
            var length = (nuint)buffer.Length;
            return PosixApi.sysctl([PosixApi.KernelControl, PosixApi.KernelProcessArguments, processId], 3, buffer, ref length, 0, 0) == 0 && length >= sizeof(int)
                ? Parse(buffer.AsSpan(0, (int)length))
                : null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static IReadOnlyList<string> Parse(ReadOnlySpan<byte> span)
    {
        var count = BinaryPrimitives.ReadInt32LittleEndian(span);
        var position = sizeof(int);
        position = SkipString(span, position);
        while (position < span.Length && span[position] == 0)
        {
            position++;
        }

        var arguments = new List<string>(Math.Max(count, 0));
        while (arguments.Count < count && position < span.Length)
        {
            var end = span[position..].IndexOf((byte)0);
            var stop = end < 0 ? span.Length : position + end;
            arguments.Add(Encoding.UTF8.GetString(span[position..stop]));
            position = stop + 1;
        }

        return arguments;
    }

    private static int SkipString(ReadOnlySpan<byte> span, int position)
    {
        var end = span[position..].IndexOf((byte)0);
        return end < 0 ? span.Length : position + end;
    }

    private static int ReadArgumentMaximum()
    {
        var buffer = new byte[sizeof(int)];
        var length = (nuint)buffer.Length;
        return PosixApi.sysctl([PosixApi.KernelControl, PosixApi.KernelArgumentMaximum], 2, buffer, ref length, 0, 0) == 0
            ? Math.Max(BinaryPrimitives.ReadInt32LittleEndian(buffer), DefaultArgumentMaximum)
            : DefaultArgumentMaximum;
    }
}
