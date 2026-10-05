using System.Collections.Concurrent;

namespace Tily.Host;

public sealed class HostLoop : IDisposable
{
    private readonly BlockingCollection<Action> _work = new();
    private readonly ConcurrentDictionary<Timer, byte> _timers = new();
    private readonly Action<Exception> _onError;
    private int _stopped;

    public HostLoop(Action<Exception> onError) => _onError = onError;

    public bool TryEnqueue(Action work)
    {
        if (Volatile.Read(ref _stopped) == 1)
        {
            return false;
        }

        try
        {
            _work.Add(work);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public IDisposable Schedule(TimeSpan delay, Action work)
    {
        Timer? timer = null;
        timer = new Timer(state =>
        {
            if (_timers.TryRemove(timer!, out _))
            {
                TryEnqueue(work);
            }

            timer!.Dispose();
        });
        _timers[timer] = 0;
        timer.Change(delay, Timeout.InfiniteTimeSpan);
        return new ScheduledWork(() =>
        {
            _timers.TryRemove(timer, out _);
            timer.Dispose();
        });
    }

    public void Run()
    {
        SynchronizationContext.SetSynchronizationContext(new LoopSynchronizationContext(this));
        foreach (var work in _work.GetConsumingEnumerable())
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

    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) == 0)
        {
            _work.CompleteAdding();
        }
    }

    public void Dispose()
    {
        Stop();
        _work.Dispose();
    }
}

public sealed class LoopSynchronizationContext(HostLoop loop) : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state) => loop.TryEnqueue(() => callback(state));

    public override SynchronizationContext CreateCopy() => new LoopSynchronizationContext(loop);
}

public sealed class ScheduledWork(Action cancel) : IDisposable
{
    public void Dispose() => cancel();
}
