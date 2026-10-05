using System.Buffers.Binary;
using System.Text;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public sealed record ProcessEntryModel(int Id, int ParentId, uint TerminalDevice, string Name, int ProcessGroup = 0, int ForegroundGroup = 0);

public static class ProcessTree
{
    private const uint NoTerminal = 0xFFFFFFFF;
    private const int ListMargin = 64;

    public static IReadOnlyList<ProcessEntryModel> Snapshot()
    {
        var count = LibProcApi.proc_listallpids(null, 0);
        if (count <= 0)
        {
            return [];
        }

        var identifiers = new int[count + ListMargin];
        count = LibProcApi.proc_listallpids(identifiers, identifiers.Length * sizeof(int));
        var entries = new List<ProcessEntryModel>(Math.Max(count, 0));
        var buffer = new byte[LibProcApi.BsdInfoSize];
        foreach (var identifier in identifiers.Take(Math.Max(count, 0)))
        {
            if (identifier > 0 && Read(identifier, buffer) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public static uint? TerminalDeviceOf(IReadOnlyList<ProcessEntryModel> snapshot, int rootId) =>
        snapshot.FirstOrDefault(entry => entry.Id == rootId) is { TerminalDevice: not (0 or NoTerminal) } root ? root.TerminalDevice : null;

    public static IReadOnlyList<ProcessEntryModel> Members(IReadOnlyList<ProcessEntryModel> snapshot, int rootId, uint? knownTerminalDevice = null)
    {
        var terminalDevice = TerminalDeviceOf(snapshot, rootId) ?? knownTerminalDevice;
        var members = new Dictionary<int, ProcessEntryModel>();
        var children = snapshot.ToLookup(entry => entry.ParentId);
        var pending = new Queue<int>([rootId]);
        while (pending.Count > 0)
        {
            foreach (var child in children[pending.Dequeue()])
            {
                if (child.Id != rootId && members.TryAdd(child.Id, child))
                {
                    pending.Enqueue(child.Id);
                }
            }
        }

        if (terminalDevice is { } device)
        {
            foreach (var entry in snapshot.Where(entry => entry.Id != rootId && entry.TerminalDevice == device))
            {
                members.TryAdd(entry.Id, entry);
            }
        }

        return members.Values.OrderBy(entry => entry.Id).ToList();
    }

    public static IReadOnlyList<ProcessEntryModel> Foreground(IReadOnlyList<ProcessEntryModel> snapshot, int rootId, IReadOnlyList<ProcessEntryModel> members)
    {
        var root = snapshot.FirstOrDefault(entry => entry.Id == rootId);
        if (root is null)
        {
            return members;
        }

        return root.ForegroundGroup <= 0 || root.ForegroundGroup == root.ProcessGroup
            ? []
            : members.Where(entry => entry.ProcessGroup == root.ForegroundGroup).ToList();
    }

    private static ProcessEntryModel? Read(int identifier, byte[] buffer)
    {
        if (LibProcApi.proc_pidinfo(identifier, LibProcApi.BsdInfoFlavor, 0, buffer, buffer.Length) != LibProcApi.BsdInfoSize)
        {
            return null;
        }

        var span = buffer.AsSpan();
        var name = Text(span.Slice(LibProcApi.NameOffset, LibProcApi.NameLength));
        return new ProcessEntryModel(
            BinaryPrimitives.ReadInt32LittleEndian(span[LibProcApi.ProcessIdOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(span[LibProcApi.ParentProcessIdOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(span[LibProcApi.TerminalDeviceOffset..]),
            name.Length > 0 ? name : Text(span.Slice(LibProcApi.CommandOffset, LibProcApi.CommandLength)),
            BinaryPrimitives.ReadInt32LittleEndian(span[LibProcApi.ProcessGroupOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(span[LibProcApi.ForegroundGroupOffset..]));
    }

    private static string Text(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end < 0 ? bytes : bytes[..end]);
    }
}
