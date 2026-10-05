using System.Runtime.InteropServices;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public static class PtyLauncher
{
    public const string DiagnosticsVariable = "DOTNET_EnableDiagnostics";
    public const string ResetDiagnosticsVariable = "TILY_PTY_RESET_DIAGNOSTICS";
    private const int ExecutionFailed = 127;
    private const int StandardInput = 0;

    public static int Run(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            Console.Error.WriteLine("Usage : tily-pty <exécutable> [arguments…]");
            return ExecutionFailed;
        }

        AttachControllingTerminal();
        ResetSignals();
        if (Environment.GetEnvironmentVariable(ResetDiagnosticsVariable) is not null)
        {
            PosixApi.unsetenv(DiagnosticsVariable);
            PosixApi.unsetenv(ResetDiagnosticsVariable);
        }

        var pointers = arguments.Select(Marshal.StringToCoTaskMemUTF8).Append(0).ToArray();
        PosixApi.execvp(arguments[0], pointers);
        Console.Error.WriteLine($"Impossible de lancer {arguments[0]} (erreur système {Marshal.GetLastPInvokeError()}).");
        return ExecutionFailed;
    }

    private static void AttachControllingTerminal()
    {
        PosixApi.IoctlWithoutArgument(StandardInput, PosixApi.SetControllingTerminal);
        var name = Marshal.PtrToStringUTF8(PosixApi.ttyname(StandardInput));
        if (name is not null)
        {
            var descriptor = PosixApi.Open(name, PosixApi.OpenReadWrite);
            if (descriptor >= 0)
            {
                PosixApi.Close(descriptor);
            }
        }
    }

    private static void ResetSignals()
    {
        for (var signal = 1; signal < PosixApi.SignalCount; signal++)
        {
            if (signal is not (PosixApi.SignalKill or PosixApi.SignalStop))
            {
                PosixApi.ResetSignal(signal, 0);
            }
        }

        var empty = 0u;
        PosixApi.sigprocmask(PosixApi.SignalSetMask, ref empty, 0);
    }
}
