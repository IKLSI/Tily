using System.Collections.Concurrent;
using System.Text.Json;

namespace Tily.Host.Bridge;

public sealed class BrowserDriver
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);

    private readonly Action<object> _send;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private long _nextId;

    public BrowserDriver(Action<object> send) => _send = send;

    public async Task<JsonElement> CallAsync(string operation, string pane, object? arguments = null)
    {
        var id = Interlocked.Increment(ref _nextId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        _send(new { type = "host.browser", id, operation, pane, arguments });
        try
        {
            return await completion.Task.WaitAsync(CallTimeout);
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException("Le navigateur ne répond pas.");
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    public Task SendAsync(string operation, string pane, object? arguments = null) => CallAsync(operation, pane, arguments);

    public void Complete(long id, JsonElement? result, string? error)
    {
        if (!_pending.TryRemove(id, out var completion))
        {
            return;
        }

        if (error is not null)
        {
            completion.TrySetException(new InvalidOperationException($"Le navigateur a échoué : {error}"));
        }
        else
        {
            completion.TrySetResult(result?.Clone() ?? default);
        }
    }

    public void CancelAll()
    {
        foreach (var completion in _pending.Values)
        {
            completion.TrySetException(new InvalidOperationException("Tily se ferme."));
        }

        _pending.Clear();
    }
}
