using System.Runtime.InteropServices;

namespace Tily.Core.Native;

public static partial class LibProcApi
{
    private const string LibProc = "libproc";

    public const int BsdInfoFlavor = 3;
    public const int BsdInfoSize = 136;
    public const int ProcessIdOffset = 12;
    public const int ParentProcessIdOffset = 16;
    public const int CommandOffset = 48;
    public const int CommandLength = 16;
    public const int NameOffset = 64;
    public const int NameLength = 32;
    public const int ProcessGroupOffset = 100;
    public const int TerminalDeviceOffset = 108;
    public const int ForegroundGroupOffset = 112;
    public const int StartSecondsOffset = 120;
    public const int StartMicrosecondsOffset = 128;

    [LibraryImport(LibProc, SetLastError = true)]
    public static partial int proc_listallpids(int[]? buffer, int bufferSize);

    [LibraryImport(LibProc, SetLastError = true)]
    public static partial int proc_pidinfo(int processId, int flavor, ulong argument, byte[] buffer, int bufferSize);
}
