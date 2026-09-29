using System.Runtime.InteropServices;

namespace Dock.Core.Native;

public static class ShellFileApi
{
    public const uint OperationDelete = 0x0003;
    public const ushort FlagSilent = 0x0004;
    public const ushort FlagNoConfirmation = 0x0010;
    public const ushort FlagAllowUndo = 0x0040;
    public const ushort FlagNoErrorUi = 0x0400;
    public const ushort FlagWantNukeWarning = 0x4000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FileOperation
    {
        public nint WindowHandle;
        public uint Function;
        public string From;
        public string? To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool AnyOperationsAborted;
        public nint NameMappings;
        public string? ProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    public static extern int SHFileOperation(ref FileOperation operation);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ILCreateFromPathW")]
    public static extern nint ILCreateFromPath(string path);

    [DllImport("shell32.dll", EntryPoint = "ILFree")]
    public static extern void ILFree(nint itemIdList);

    [DllImport("shell32.dll", EntryPoint = "SHOpenFolderAndSelectItems")]
    public static extern int SHOpenFolderAndSelectItems(nint folder, uint count, nint[]? items, uint flags);
}
