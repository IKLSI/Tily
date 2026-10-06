using System.Diagnostics;
using System.Text;

namespace Tily.Core.Processes;

public sealed record ProcessRequestModel(
    string Executable,
    IEnumerable<string> Arguments,
    TimeSpan Timeout,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string?>? Environment = null,
    string? Input = null,
    bool InheritsInput = false,
    Encoding? Encoding = null,
    Stream? OutputDestination = null);

public sealed record ProcessOutputModel(int ExitCode, string Output, string Error, bool TimedOut)
{
    public static readonly ProcessOutputModel Expired = new(-1, string.Empty, string.Empty, true);
}

public static class ProcessRunner
{
    public static ProcessOutputModel Run(ProcessRequestModel request)
    {
        using var process = new Process { StartInfo = StartInfo(request) };
        process.Start();
        var output = request.OutputDestination is null ? process.StandardOutput.ReadToEndAsync() : CopyOutputAsync(process, request.OutputDestination);
        var error = process.StandardError.ReadToEndAsync();
        if (!request.InheritsInput)
        {
            WriteInput(process, request.Input);
        }

        if (!process.WaitForExit(request.Timeout))
        {
            process.Kill(true);
            return ProcessOutputModel.Expired;
        }

        process.WaitForExit();
        return new ProcessOutputModel(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult(), false);
    }

    private static ProcessStartInfo StartInfo(ProcessRequestModel request)
    {
        var info = new ProcessStartInfo(request.Executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = !request.InheritsInput,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (request.WorkingDirectory is not null)
        {
            info.WorkingDirectory = request.WorkingDirectory;
        }

        if (request.Encoding is not null)
        {
            if (!request.InheritsInput)
            {
                info.StandardInputEncoding = request.Encoding;
            }

            info.StandardOutputEncoding = request.Encoding;
            info.StandardErrorEncoding = request.Encoding;
        }

        foreach (var argument in request.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in request.Environment ?? new Dictionary<string, string?>())
        {
            info.Environment[name] = value;
        }

        return info;
    }

    private static async Task<string> CopyOutputAsync(Process process, Stream destination)
    {
        await process.StandardOutput.BaseStream.CopyToAsync(destination).ConfigureAwait(false);
        return string.Empty;
    }

    private static void WriteInput(Process process, string? input)
    {
        if (input is not null)
        {
            process.StandardInput.Write(input);
        }

        process.StandardInput.Close();
    }
}
