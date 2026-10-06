using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public sealed class UnixPty : IDisposable
{
    public const string HelperName = "tily-pty";

    private static readonly int[] StandardStreams = [0, 1, 2];

    private UnixPty(SafeFileHandle master, int processId, string terminalPath)
    {
        Master = master;
        ProcessId = processId;
        TerminalPath = terminalPath;
    }

    public SafeFileHandle Master { get; }
    public int ProcessId { get; }
    public string TerminalPath { get; }

    public static string HelperPath => Path.Combine(AppContext.BaseDirectory, HelperName);

    public static UnixPty Start(string executable, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string> environment, int columns, int rows)
    {
        var helper = HelperPath;
        if (!File.Exists(helper))
        {
            throw new InvalidOperationException($"Le lanceur de terminal {HelperName} est introuvable à côté de l'exécutable : {helper}");
        }

        var masterDescriptor = PosixApi.posix_openpt(PosixApi.OpenReadWrite | PosixApi.OpenNoControllingTerminal);
        Require(masterDescriptor >= 0, "Impossible de créer le pseudo-terminal");
        var master = new SafeFileHandle(masterDescriptor, true);
        try
        {
            PosixApi.IoctlWithoutArgument(masterDescriptor, PosixApi.SetCloseOnExec);
            Require(PosixApi.grantpt(masterDescriptor) == 0, "Impossible d'attribuer le pseudo-terminal");
            Require(PosixApi.unlockpt(masterDescriptor) == 0, "Impossible de déverrouiller le pseudo-terminal");
            var slavePath = SlavePath(masterDescriptor);
            var slave = PosixApi.Open(slavePath, PosixApi.OpenReadWrite | PosixApi.OpenNoControllingTerminal | PosixApi.OpenCloseOnExec);
            Require(slave >= 0, $"Impossible d'ouvrir le pseudo-terminal {slavePath}");
            try
            {
                Resize(master, columns, rows);
                return new UnixPty(master, Spawn(helper, [helper, executable, .. arguments], workingDirectory, environment, slave), slavePath);
            }
            finally
            {
                PosixApi.Close(slave);
            }
        }
        catch
        {
            master.Dispose();
            throw;
        }
    }

    public static void Resize(SafeFileHandle master, int columns, int rows) =>
        Require(PosixApi.SetWindowSize(master, new WindowSize { Columns = (ushort)columns, Rows = (ushort)rows }) == 0, "Impossible de redimensionner le pseudo-terminal");

    private static string SlavePath(int masterDescriptor)
    {
        var buffer = new byte[PosixApi.TerminalNameLength];
        Require(PosixApi.ptsname_r(masterDescriptor, buffer, (nuint)buffer.Length) == 0, "Impossible de lire le nom du pseudo-terminal");
        var end = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end);
    }

    private static int Spawn(string helper, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string> environment, int slave)
    {
        nint actions = 0;
        nint attributes = 0;
        var argumentPointers = Allocate(arguments);
        var environmentPointers = Allocate(HelperEnvironment(environment).Select(pair => $"{pair.Key}={pair.Value}").ToList());
        PosixApi.posix_spawn_file_actions_init(ref actions);
        PosixApi.posix_spawnattr_init(ref attributes);
        try
        {
            foreach (var target in StandardStreams)
            {
                PosixApi.posix_spawn_file_actions_adddup2(ref actions, slave, target);
            }

            PosixApi.posix_spawn_file_actions_addchdir_np(ref actions, workingDirectory);
            var defaults = PosixApi.AllSignals;
            var mask = 0u;
            PosixApi.posix_spawnattr_setsigdefault(ref attributes, ref defaults);
            PosixApi.posix_spawnattr_setsigmask(ref attributes, ref mask);
            PosixApi.posix_spawnattr_setflags(ref attributes, (short)(PosixApi.SpawnSetSession | PosixApi.SpawnSetSignalDefault | PosixApi.SpawnSetSignalMask | PosixApi.SpawnCloseOnExecDefault));
            var result = PosixApi.posix_spawn(out var processId, helper, ref actions, ref attributes, argumentPointers, environmentPointers);
            Require(result == 0, $"Impossible de lancer le shell (code {result})");
            return processId;
        }
        finally
        {
            PosixApi.posix_spawn_file_actions_destroy(ref actions);
            PosixApi.posix_spawnattr_destroy(ref attributes);
            Free(argumentPointers);
            Free(environmentPointers);
        }
    }

    private static Dictionary<string, string> HelperEnvironment(IReadOnlyDictionary<string, string> environment)
    {
        var helper = environment.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        if (helper.TryAdd(PtyLauncher.DiagnosticsVariable, "0"))
        {
            helper[PtyLauncher.ResetDiagnosticsVariable] = "1";
        }

        return helper;
    }

    private static nint[] Allocate(IReadOnlyList<string> values) =>
        [.. values.Select(Marshal.StringToCoTaskMemUTF8), 0];

    private static void Free(nint[] pointers)
    {
        foreach (var pointer in pointers.Where(pointer => pointer != 0))
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{message} (erreur système {Marshal.GetLastPInvokeError()}).");
        }
    }

    public void RevokeTerminal()
    {
        if (!Master.IsClosed)
        {
            PosixApi.revoke(TerminalPath);
        }
    }

    public void Dispose() => Master.Dispose();
}
