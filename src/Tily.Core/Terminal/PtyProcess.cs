using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public sealed class PtyProcess : IDisposable
{
    private IntPtr _processHandle;
    private IntPtr _threadHandle;

    public int ProcessId { get; }
    public IntPtr Handle => _processHandle;

    private PtyProcess(ProcessInformation information)
    {
        _processHandle = information.hProcess;
        _threadHandle = information.hThread;
        ProcessId = information.dwProcessId;
    }

    public static PtyProcess StartSuspended(string commandLine, string? workingDirectory, IReadOnlyDictionary<string, string> environment, IntPtr pseudoConsoleHandle)
    {
        var attributeListSize = IntPtr.Zero;
        ProcessApi.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);
        var attributeList = Marshal.AllocHGlobal(attributeListSize);
        var environmentBlock = Marshal.StringToHGlobalUni(BuildEnvironmentBlock(environment));
        try
        {
            if (!ProcessApi.InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeListSize))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Initialisation des attributs de processus impossible.");
            }

            if (!ProcessApi.UpdateProcThreadAttribute(attributeList, 0, ProcessApi.ProcThreadAttributePseudoConsole, pseudoConsoleHandle, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Association de la pseudo-console au processus impossible.");
            }

            var startupInfo = new StartupInfoEx
            {
                StartupInfo = new StartupInfo { cb = Marshal.SizeOf<StartupInfoEx>(), dwFlags = ProcessApi.StartFUseStdHandles },
                lpAttributeList = attributeList
            };
            var flags = ProcessApi.ExtendedStartupInfoPresent | ProcessApi.CreateUnicodeEnvironment | ProcessApi.CreateSuspended;
            LetChildrenReceiveCtrlC();
            var created = ProcessApi.CreateProcessW(null, new StringBuilder(commandLine), IntPtr.Zero, IntPtr.Zero, false, flags, environmentBlock, workingDirectory, ref startupInfo, out var information);
            if (!created)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Lancement du shell impossible : {commandLine}");
            }

            return new PtyProcess(information);
        }
        finally
        {
            ProcessApi.DeleteProcThreadAttributeList(attributeList);
            Marshal.FreeHGlobal(attributeList);
            Marshal.FreeHGlobal(environmentBlock);
        }
    }

    public void Resume()
    {
        ProcessApi.ResumeThread(_threadHandle);
        ProcessApi.CloseHandle(_threadHandle);
        _threadHandle = IntPtr.Zero;
    }

    public uint WaitForExit(uint timeoutMilliseconds = ProcessApi.Infinite)
    {
        ProcessApi.WaitForSingleObject(_processHandle, timeoutMilliseconds);
        ProcessApi.GetExitCodeProcess(_processHandle, out var exitCode);
        return exitCode;
    }

    private static void LetChildrenReceiveCtrlC() => ProcessApi.SetConsoleCtrlHandler(IntPtr.Zero, false);

    private static string BuildEnvironmentBlock(IReadOnlyDictionary<string, string> environment)
    {
        var block = new StringBuilder();
        foreach (var pair in environment.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            block.Append(pair.Key).Append('=').Append(pair.Value).Append('\0');
        }

        block.Append('\0');
        return block.ToString();
    }

    public void Dispose()
    {
        if (_threadHandle != IntPtr.Zero)
        {
            ProcessApi.CloseHandle(_threadHandle);
            _threadHandle = IntPtr.Zero;
        }

        if (_processHandle != IntPtr.Zero)
        {
            ProcessApi.CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
        }
    }
}
