using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Tily.Core.Native;

[StructLayout(LayoutKind.Sequential)]
public struct WindowSize
{
    public ushort Rows;
    public ushort Columns;
    public ushort Width;
    public ushort Height;
}

public static partial class PosixApi
{
    private const string LibC = "libc";

    public const int OpenReadWrite = 0x0002;
    public const int OpenNoControllingTerminal = 0x20000;
    public const int OpenCloseOnExec = 0x1000000;
    public const ulong SetCloseOnExec = 0x20006601;
    public const ulong SetWindowSizeRequest = 0x80087467;
    public const ulong SetControllingTerminal = 0x20007461;
    public const short SpawnSetSignalDefault = 0x04;
    public const short SpawnSetSignalMask = 0x08;
    public const short SpawnSetSession = 0x0400;
    public const short SpawnCloseOnExecDefault = 0x4000;
    public const int SignalSetMask = 3;
    public const int SignalHangUp = 1;
    public const int SignalKill = 9;
    public const int SignalStop = 17;
    public const int SignalCount = 32;
    public const uint AllSignals = 0xFFFFFFFF;
    public const int InterruptedCall = 4;
    public const int TryAgain = 35;
    public const int TerminalNameLength = 128;
    public const int KernelControl = 1;
    public const int KernelArgumentMaximum = 8;
    public const int KernelProcessArguments = 49;

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_openpt(int flags);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int grantpt(int fileDescriptor);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int unlockpt(int fileDescriptor);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int ptsname_r(int fileDescriptor, byte[] buffer, nuint length);

    [LibraryImport(LibC, EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int Open(string path, int flags);

    [LibraryImport(LibC, EntryPoint = "close", SetLastError = true)]
    public static partial int Close(int fileDescriptor);

    [LibraryImport(LibC, EntryPoint = "read", SetLastError = true)]
    public static partial nint Read(SafeFileHandle fileDescriptor, byte[] buffer, nint count);

    [LibraryImport(LibC, EntryPoint = "write", SetLastError = true)]
    public static partial nint Write(SafeFileHandle fileDescriptor, byte[] buffer, nint count);

    [LibraryImport(LibC, EntryPoint = "ioctl", SetLastError = true)]
    public static partial int IoctlWithoutArgument(int fileDescriptor, ulong request);

    [LibraryImport(LibC, EntryPoint = "ioctl", SetLastError = true)]
    private static partial int IoctlWithPointer(SafeFileHandle fileDescriptor, ulong request, ref WindowSize registerArgument, nint x3, nint x4, nint x5, nint x6, nint x7, ref WindowSize stackArgument);

    public static int SetWindowSize(SafeFileHandle fileDescriptor, WindowSize size)
    {
        var copy = size;
        return IoctlWithPointer(fileDescriptor, SetWindowSizeRequest, ref size, 0, 0, 0, 0, 0, ref copy);
    }

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawn_file_actions_init(ref nint actions);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawn_file_actions_destroy(ref nint actions);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawn_file_actions_adddup2(ref nint actions, int fileDescriptor, int target);

    [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int posix_spawn_file_actions_addchdir_np(ref nint actions, string path);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawnattr_init(ref nint attributes);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawnattr_destroy(ref nint attributes);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawnattr_setflags(ref nint attributes, short flags);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawnattr_setsigdefault(ref nint attributes, ref uint signals);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int posix_spawnattr_setsigmask(ref nint attributes, ref uint signals);

    [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int posix_spawn(out int processId, string path, ref nint actions, ref nint attributes, nint[] arguments, nint[] environment);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int waitpid(int processId, out int status, int options);

    [LibraryImport(LibC, EntryPoint = "kill", SetLastError = true)]
    public static partial int SendSignal(int processId, int signal);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int killpg(int processGroup, int signal);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial nint ttyname(int fileDescriptor);

    [LibraryImport(LibC, EntryPoint = "signal", SetLastError = true)]
    public static partial nint ResetSignal(int signal, nint handler);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int sigprocmask(int how, ref uint signals, nint previous);

    [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int unsetenv(string name);

    [LibraryImport(LibC, SetLastError = true)]
    public static partial int sysctl(int[] name, uint nameLength, byte[]? value, ref nuint valueLength, nint newValue, nuint newValueLength);

    [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int execvp(string file, nint[] arguments);
}
