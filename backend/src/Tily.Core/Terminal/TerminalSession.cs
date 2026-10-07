using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Tily.Core.Native;
using Tily.Core.Shell;

namespace Tily.Core.Terminal;

public sealed class TerminalSession : IDisposable
{
    private const int ReadBufferSize = 64 * 1024;
    private const int SignalExitBase = 128;
    private static readonly string[] LocaleVariables = ["LC_ALL", "LC_CTYPE", "LANG"];
    private const string DefaultLocale = "en_US.UTF-8";
    private static readonly TimeSpan HangUpGrace = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FinalOutputGrace = TimeSpan.FromSeconds(1);

    private readonly UnixPty _pty;
    private readonly OscCwdParser _cwdParser = new();
    private readonly DevServerUrlDetector _devServerDetector = new();
    private readonly Thread _readerThread;
    private readonly Thread _exitThread;
    private readonly Thread _writerThread;
    private readonly BlockingCollection<byte[]> _pendingWrites = new();
    private readonly ManualResetEventSlim _exited = new();
    private Task? _closing;
    private int _closed;
    private uint? _terminalDevice;

    public string PaneId { get; }
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public int ProcessId => _pty.ProcessId;
    public string? CurrentDirectory { get; private set; }
    public bool HasExited { get; private set; }
    public uint ExitCode { get; private set; }

    public event Action<ReadOnlyMemory<byte>>? OutputReceived;
    public event Action<string>? CurrentDirectoryChanged;
    public event Action<string>? DevServerDetected;
    public event Action<uint>? Exited;

    public TerminalSession(TerminalSessionOptions options)
    {
        PaneId = options.PaneId;
        _pty = UnixPty.Start(options.Executable, options.Arguments, options.WorkingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BuildEnvironment(options), options.Columns, options.Rows);
        _cwdParser.CurrentDirectoryChanged += HandleCurrentDirectoryChanged;
        _devServerDetector.Detected += url => DevServerDetected?.Invoke(url);
        _readerThread = new Thread(ReadLoop) { IsBackground = true, Name = $"pty-reader-{PaneId}" };
        _readerThread.Start();
        _exitThread = new Thread(WaitForExit) { IsBackground = true, Name = $"pty-exit-{PaneId}" };
        _exitThread.Start();
        _writerThread = new Thread(WriteLoop) { IsBackground = true, Name = $"pty-writer-{PaneId}" };
        _writerThread.Start();
    }

    public IReadOnlyList<int> ProcessIds() =>
        [ProcessId, .. Members().Select(entry => entry.Id)];

    public IReadOnlyList<string> ActiveProcessNames() => NamesOf(ActiveProcesses());

    public IReadOnlyList<string> ActiveProcessNames(IReadOnlyList<ProcessEntryModel> snapshot) => NamesOf(ActiveProcesses(snapshot));

    public static IReadOnlyList<string> NamesOf(IEnumerable<ActiveProcessModel> processes) =>
        new SortedSet<string>(processes.Select(process => process.Name), StringComparer.OrdinalIgnoreCase).ToList();

    public IReadOnlyList<ActiveProcessModel> ActiveProcesses()
    {
        if (HasExited || Volatile.Read(ref _closed) == 1)
        {
            return Array.Empty<ActiveProcessModel>();
        }

        return ActiveProcesses(ProcessTree.Snapshot());
    }

    public IReadOnlyList<ActiveProcessModel> ActiveProcesses(IReadOnlyList<ProcessEntryModel> snapshot)
    {
        if (HasExited || Volatile.Read(ref _closed) == 1)
        {
            return Array.Empty<ActiveProcessModel>();
        }

        return ProcessTree.Foreground(snapshot, ProcessId, Members(snapshot))
            .Select(entry => new ActiveProcessModel(entry.Id, ProcessCommandName.Of(entry.Id, entry.Name)))
            .ToList();
    }

    public bool Contains(int processId, IReadOnlyList<ProcessEntryModel> snapshot)
    {
        if (HasExited || Volatile.Read(ref _closed) == 1)
        {
            return false;
        }

        _terminalDevice = ProcessTree.TerminalDeviceOf(snapshot, ProcessId) ?? _terminalDevice;
        return ProcessTree.Contains(snapshot, ProcessId, processId, _terminalDevice);
    }

    private IReadOnlyList<ProcessEntryModel> Members() => Members(ProcessTree.Snapshot());

    private IReadOnlyList<ProcessEntryModel> Members(IReadOnlyList<ProcessEntryModel> snapshot)
    {
        _terminalDevice = ProcessTree.TerminalDeviceOf(snapshot, ProcessId) ?? _terminalDevice;
        return ProcessTree.Members(snapshot, ProcessId, _terminalDevice);
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (Volatile.Read(ref _closed) == 1)
        {
            return;
        }

        try
        {
            _pendingWrites.Add(data.ToArray());
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void WriteLoop()
    {
        try
        {
            foreach (var chunk in _pendingWrites.GetConsumingEnumerable())
            {
                WriteAll(chunk);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void WriteAll(byte[] pending)
    {
        while (pending.Length > 0)
        {
            var written = PosixApi.Write(_pty.Master, pending, pending.Length);
            if (written < 0)
            {
                if (Marshal.GetLastPInvokeError() is PosixApi.InterruptedCall or PosixApi.TryAgain)
                {
                    continue;
                }

                return;
            }

            pending = pending[(int)written..];
        }
    }

    public void Resize(int columns, int rows)
    {
        if (Volatile.Read(ref _closed) == 0 && columns > 0 && rows > 0)
        {
            try
            {
                UnixPty.Resize(_pty.Master, columns, rows);
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    public void Close() => BeginClose().Wait();

    public Task BeginClose()
    {
        if (Interlocked.Exchange(ref _closed, 1) == 1)
        {
            return _closing ?? Task.CompletedTask;
        }

        _pendingWrites.CompleteAdding();
        var members = Members();
        if (!HasExited)
        {
            PosixApi.killpg(ProcessId, PosixApi.SignalHangUp);
            PosixApi.SendSignal(ProcessId, PosixApi.SignalHangUp);
        }

        _closing = Task.Run(() => Terminate(members));
        return _closing;
    }

    private void Terminate(IReadOnlyList<ProcessEntryModel> members)
    {
        if (!HasExited && !_exited.Wait(HangUpGrace))
        {
            PosixApi.SendSignal(ProcessId, PosixApi.SignalKill);
        }

        foreach (var member in members)
        {
            if (ProcessTree.IsSameProcess(member, ProcessTree.Find(member.Id)))
            {
                PosixApi.SendSignal(member.Id, PosixApi.SignalKill);
            }
        }

        _pty.RevokeTerminal();
        _pty.Dispose();
    }

    private void ReadLoop()
    {
        var buffer = new byte[ReadBufferSize];
        try
        {
            while (true)
            {
                var count = PosixApi.Read(_pty.Master, buffer, buffer.Length);
                if (count < 0 && Marshal.GetLastPInvokeError() == PosixApi.InterruptedCall)
                {
                    continue;
                }

                if (count <= 0)
                {
                    break;
                }

                var chunk = buffer.AsMemory(0, (int)count);
                _cwdParser.Feed(chunk.Span);
                _devServerDetector.Feed(chunk.Span);
                OutputReceived?.Invoke(chunk);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void WaitForExit()
    {
        int result;
        int status;
        do
        {
            result = PosixApi.waitpid(ProcessId, out status, 0);
        }
        while (result < 0 && Marshal.GetLastPInvokeError() == PosixApi.InterruptedCall);

        ExitCode = result < 0 ? 0 : DecodeExitCode(status);
        HasExited = true;
        _exited.Set();
        if (Volatile.Read(ref _closed) == 0)
        {
            _readerThread.Join(FinalOutputGrace);
        }

        BeginClose();
        Exited?.Invoke(ExitCode);
    }

    private static uint DecodeExitCode(int status)
    {
        var signal = status & 0x7F;
        return signal == 0 ? (uint)((status >> 8) & 0xFF) : (uint)(SignalExitBase + signal);
    }

    private void HandleCurrentDirectoryChanged(string directory)
    {
        CurrentDirectory = directory;
        CurrentDirectoryChanged?.Invoke(directory);
    }

    private static Dictionary<string, string> BuildEnvironment(TerminalSessionOptions options)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = (string)entry.Key;
            if (!key.StartsWith("WEZTERM_", StringComparison.Ordinal))
            {
                environment[key] = (string?)entry.Value ?? string.Empty;
            }
        }

        environment["TERM"] = "xterm-256color";
        environment["COLORTERM"] = "truecolor";
        environment["TILY_TERMINAL"] = "1";
        environment["TILY_PANE_ID"] = options.PaneId;
        environment["TERM_PROGRAM"] = "TilyTerminal";
        if (!LocaleVariables.Any(name => environment.TryGetValue(name, out var value) && value.Length > 0))
        {
            environment["LANG"] = DefaultLocale;
        }

        foreach (var pair in options.ExtraEnvironment)
        {
            environment[pair.Key] = pair.Value;
        }

        return environment;
    }

    public void Dispose() => BeginClose();
}
