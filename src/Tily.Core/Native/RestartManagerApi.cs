using System.Runtime.InteropServices;
using System.Text;

namespace Tily.Core.Native;

public static class RestartManagerApi
{
    public const int SessionKeyLength = 33;
    public const int ErrorMoreData = 234;

    [StructLayout(LayoutKind.Sequential)]
    public struct UniqueProcess
    {
        public int ProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME StartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProcessInfo
    {
        public UniqueProcess Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string ApplicationName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string ServiceShortName;
        public int ApplicationType;
        public uint ApplicationStatus;
        public uint TerminalServicesSessionId;
        [MarshalAs(UnmanagedType.Bool)] public bool Restartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    public static extern int RmStartSession(out uint session, int flags, StringBuilder sessionKey);

    [DllImport("rstrtmgr.dll")]
    public static extern int RmEndSession(uint session);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    public static extern int RmRegisterResources(uint session, uint fileCount, string[] fileNames, uint applicationCount, UniqueProcess[]? applications, uint serviceCount, string[]? serviceNames);

    [DllImport("rstrtmgr.dll")]
    public static extern int RmGetList(uint session, out uint needed, ref uint count, [In, Out] ProcessInfo[]? processes, ref uint rebootReasons);
}
