using System.Diagnostics;

namespace Tily.Host.Bridge;

public sealed class BackgroundQueue
{
    private readonly object _sync = new();
    private readonly Action<Exception> _onError;
    private Task _tail = Task.CompletedTask;

    public BackgroundQueue(Action<Exception> onError) => _onError = onError;

    public void Enqueue(Action work)
    {
        lock (_sync)
        {
            _tail = _tail.ContinueWith(_ => Run(work), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }

    public bool Drain(TimeSpan timeout)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            Task tail;
            lock (_sync)
            {
                tail = _tail;
            }

            var remaining = timeout - clock.Elapsed;
            if (remaining <= TimeSpan.Zero || !tail.Wait(remaining))
            {
                return false;
            }

            lock (_sync)
            {
                if (ReferenceEquals(tail, _tail))
                {
                    return true;
                }
            }
        }
    }

    private void Run(Action work)
    {
        try
        {
            work();
        }
        catch (Exception exception)
        {
            _onError(exception);
        }
    }
}
