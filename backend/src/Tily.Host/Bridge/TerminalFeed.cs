using System.Collections.Concurrent;
using System.Text;
using Tily.Core.Context;
using Tily.Core.Shell;
using Tily.Core.Terminal;

namespace Tily.Host.Bridge;

public sealed class TerminalFeed : IDisposable
{
    public const string Prefix = "terminal.";

    private const string InvalidDroppedPath = "Chemin déposé invalide.";
    private const string InvalidDroppedFile = "Fichier déposé illisible.";

    private readonly HostLoop _loop;
    private readonly TerminalManager _terminals;
    private readonly Action<string> _forgetAgent;
    private readonly BackgroundQueue _queries;
    private readonly Action<object> _post;
    private readonly Action<object> _postNow;
    private readonly TerminalOutputWriter _output;
    private readonly ConcurrentDictionary<string, PaneOutputBuffer> _buffers = new();
    private int _flushScheduled;

    public TerminalFeed(HostLoop loop, TerminalManager terminals, Action<string> forgetAgent, BackgroundQueue queries, Action<object> post, Action<object> postNow, TerminalOutputWriter output)
    {
        _loop = loop;
        _terminals = terminals;
        _forgetAgent = forgetAgent;
        _queries = queries;
        _post = post;
        _postNow = postNow;
        _output = output;
        _terminals.OutputReceived += HandleOutput;
        _terminals.CurrentDirectoryChanged += HandleCurrentDirectoryChanged;
        _terminals.DevServerDetected += (paneId, url) => _post(new { type = "terminal.devServer", pane = paneId, url });
        _terminals.Exited += (paneId, code) => _loop.TryEnqueue(() =>
        {
            Flush();
            _postNow(new { type = "terminal.exit", pane = paneId, code });
        });
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "terminal.create":
                Create(command);
                break;
            case "terminal.input":
                _terminals.Require(RequirePane(command)).Write(Encoding.UTF8.GetBytes(command.Data ?? string.Empty));
                break;
            case "terminal.resize":
                _terminals.Require(RequirePane(command)).Resize(command.Cols, command.Rows);
                break;
            case "terminal.ack":
                if (_buffers.TryGetValue(RequirePane(command), out var buffer))
                {
                    buffer.Acknowledge(command.Chars);
                }

                break;
            case "terminal.close":
                Close(RequirePane(command));
                break;
            case "terminal.drop":
                ReceiveDrop(command);
                break;
            case "terminal.dropPath":
                PostDroppedPath(command);
                break;
            case "terminal.dropFile":
                SaveDroppedFile(command);
                break;
            case "terminal.activity":
                var request = command.Request;
                var panes = command.Panes ?? [];
                _queries.Enqueue(() => _post(new { type = "terminal.activityResult", request, panes = _terminals.Activity(panes) }));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void Create(BridgeCommandModel command)
    {
        var paneId = RequirePane(command);
        Close(paneId);
        _buffers[paneId] = new PaneOutputBuffer(paneId);
        var cwd = command.Cwd ?? string.Empty;
        _terminals.Start(paneId, command.Shell ?? ShellCatalog.DefaultShellId, cwd, command.Cols, command.Rows, command.Command);
        if (cwd.Length > 0 && !Directory.Exists(cwd))
        {
            _post(new { type = "terminal.pathMissing", pane = paneId, path = cwd, fallback = PathFallback.NearestExisting(cwd) });
        }
    }

    private void Close(string paneId)
    {
        if (_buffers.TryRemove(paneId, out var buffer))
        {
            buffer.Release();
        }

        _terminals.Stop(paneId);
        _forgetAgent(paneId);
    }

    private void ReceiveDrop(BridgeCommandModel command)
    {
        var paths = (command.Paths ?? []).Where(path => Path.IsPathFullyQualified(path) && !path.Any(char.IsControl)).ToList();
        _post(new { type = "terminal.dropped", pane = RequirePane(command), text = DroppedPaths.Format(paths) });
    }

    private void SaveDroppedFile(BridgeCommandModel command)
    {
        var paneId = RequirePane(command);
        var name = command.Name ?? string.Empty;
        var data = command.Data ?? string.Empty;
        _queries.Enqueue(() =>
        {
            try
            {
                var path = DroppedFiles.Default.Save(name, Convert.FromBase64String(data));
                _post(new { type = "terminal.dropped", pane = paneId, text = DroppedPaths.Format([path]) });
            }
            catch (FormatException)
            {
                _post(new { type = "error", message = InvalidDroppedFile });
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                _post(new { type = "error", message = exception is InvalidOperationException ? exception.Message : $"Impossible d’enregistrer le fichier déposé : {exception.Message}" });
            }
        });
    }

    private void PostDroppedPath(BridgeCommandModel command)
    {
        var paneId = RequirePane(command);
        if (command.Path is not { } path || path.Any(char.IsControl) || !Path.IsPathFullyQualified(path))
        {
            _post(new { type = "error", message = InvalidDroppedPath });
            return;
        }

        _queries.Enqueue(() =>
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                _post(new { type = "terminal.dropped", pane = paneId, text = DroppedPaths.Format([path]) });
            }
            else
            {
                _post(new { type = "error", message = InvalidDroppedPath });
            }
        });
    }

    private void HandleCurrentDirectoryChanged(string paneId, string path)
    {
        _post(new { type = "terminal.cwd", pane = paneId, path });
        foreach (var missing in _terminals.MissingDirectories())
        {
            _post(new { type = "terminal.pathMissing", pane = missing.PaneId, path = missing.Path, fallback = missing.Fallback });
        }
    }

    private void HandleOutput(string paneId, ReadOnlyMemory<byte> data)
    {
        if (!_buffers.TryGetValue(paneId, out var buffer))
        {
            return;
        }

        buffer.Append(data.Span);
        if (Interlocked.CompareExchange(ref _flushScheduled, 1, 0) == 0)
        {
            _loop.TryEnqueue(Flush);
        }
    }

    private void Flush()
    {
        Interlocked.Exchange(ref _flushScheduled, 0);
        foreach (var buffer in _buffers.Values)
        {
            var text = buffer.Take();
            if (text is null)
            {
                continue;
            }

            _output.Send(buffer.PaneId, text);
            buffer.Recycle(text);
        }
    }

    private static string RequirePane(BridgeCommandModel command) =>
        command.Pane ?? throw new InvalidOperationException("Identifiant de pane manquant.");

    public void Dispose()
    {
        foreach (var buffer in _buffers.Values)
        {
            buffer.Release();
        }
    }
}
