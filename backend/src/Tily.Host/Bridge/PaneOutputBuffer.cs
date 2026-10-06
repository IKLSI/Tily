using System.Buffers;
using System.Text;

namespace Tily.Host.Bridge;

public sealed class PaneOutputBuffer
{
    private const long UnackedCharsLimit = 4L * 1024 * 1024;
    private const int DecodeChunkChars = 16 * 1024;
    private const int RecycledCapacityLimit = 64 * 1024;

    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _flowGate = new(true);
    private StringBuilder _pending = new();
    private StringBuilder? _spare;
    private long _unackedChars;
    private volatile bool _released;

    public string PaneId { get; }

    public PaneOutputBuffer(string paneId) => PaneId = paneId;

    public void Append(ReadOnlySpan<byte> data)
    {
        var written = Decode(data);
        if (Interlocked.Add(ref _unackedChars, written) >= UnackedCharsLimit)
        {
            _flowGate.Reset();
            if (_released || Interlocked.Read(ref _unackedChars) < UnackedCharsLimit)
            {
                _flowGate.Set();
            }

            _flowGate.Wait(TimeSpan.FromSeconds(10));
        }
    }

    private int Decode(ReadOnlySpan<byte> data)
    {
        var chars = ArrayPool<char>.Shared.Rent(DecodeChunkChars);
        var written = 0;
        try
        {
            bool completed;
            do
            {
                _decoder.Convert(data, chars, false, out var bytesUsed, out var charsUsed, out completed);
                data = data[bytesUsed..];
                if (charsUsed > 0)
                {
                    lock (_sync)
                    {
                        _pending.Append(chars, 0, charsUsed);
                    }

                    written += charsUsed;
                }
            }
            while (!completed);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }

        return written;
    }

    public StringBuilder? Take()
    {
        lock (_sync)
        {
            if (_pending.Length == 0)
            {
                return null;
            }

            var taken = _pending;
            _pending = _spare ?? new StringBuilder();
            _spare = null;
            return taken;
        }
    }

    public void Recycle(StringBuilder taken)
    {
        if (taken.Capacity > RecycledCapacityLimit)
        {
            return;
        }

        taken.Clear();
        lock (_sync)
        {
            _spare = taken;
        }
    }

    public void Acknowledge(int chars)
    {
        if (Interlocked.Add(ref _unackedChars, -chars) < UnackedCharsLimit)
        {
            _flowGate.Set();
        }
    }

    public void Release()
    {
        _released = true;
        _flowGate.Set();
    }
}
