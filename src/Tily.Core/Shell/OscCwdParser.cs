using System.Text;

namespace Tily.Core.Shell;

public sealed class OscCwdParser
{
    private const byte Escape = 0x1B;
    private const byte Bell = 0x07;
    private const byte Bracket = (byte)']';
    private const byte Backslash = (byte)'\\';
    private const int MaxPayloadLength = 4096;
    private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("7;");

    private readonly List<byte> _payload = new();
    private ParserState _state = ParserState.Text;
    private int _prefixIndex;

    public event Action<string>? CurrentDirectoryChanged;

    public void Feed(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            Step(value);
        }
    }

    private void Step(byte value)
    {
        switch (_state)
        {
            case ParserState.Text:
                _state = value == Escape ? ParserState.Escape : ParserState.Text;
                break;
            case ParserState.Escape:
                _state = value == Bracket ? ParserState.OscPrefix : ParserState.Text;
                _prefixIndex = 0;
                break;
            case ParserState.OscPrefix:
                StepPrefix(value);
                break;
            case ParserState.OscPayload:
                StepPayload(value);
                break;
            case ParserState.PayloadEscape:
                if (value == Backslash)
                {
                    EmitPayload();
                }

                _state = ParserState.Text;
                break;
        }
    }

    private void StepPrefix(byte value)
    {
        if (value != Prefix[_prefixIndex])
        {
            _state = ParserState.Text;
            return;
        }

        _prefixIndex++;
        if (_prefixIndex == Prefix.Length)
        {
            _payload.Clear();
            _state = ParserState.OscPayload;
        }
    }

    private void StepPayload(byte value)
    {
        if (value == Bell)
        {
            EmitPayload();
            _state = ParserState.Text;
            return;
        }

        if (value == Escape)
        {
            _state = ParserState.PayloadEscape;
            return;
        }

        if (_payload.Count >= MaxPayloadLength)
        {
            _state = ParserState.Text;
            return;
        }

        _payload.Add(value);
    }

    private void EmitPayload()
    {
        var uri = Encoding.UTF8.GetString(_payload.ToArray());
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
        {
            CurrentDirectoryChanged?.Invoke(parsed.LocalPath);
        }
    }

    private enum ParserState
    {
        Text,
        Escape,
        OscPrefix,
        OscPayload,
        PayloadEscape
    }
}
