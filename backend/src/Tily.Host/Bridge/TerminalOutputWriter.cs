using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tily.Host.Bridge;

public sealed class TerminalOutputWriter
{
    public const int MaxCharsPerMessage = 512 * 1024;
    private const int RetainedBufferLimit = 4 * 1024 * 1024;

    private readonly JsonEncodedText _typeName;
    private readonly JsonEncodedText _typeValue;
    private readonly JsonEncodedText _paneName;
    private readonly JsonEncodedText _dataName;
    private readonly Action<ReadOnlySpan<byte>> _sendLine;
    private readonly int _maxCharsPerMessage;
    private readonly object _sync = new();
    private ArrayBufferWriter<byte> _buffer = new();
    private readonly Utf8JsonWriter _writer;

    public TerminalOutputWriter(JavaScriptEncoder? encoder, Action<ReadOnlySpan<byte>> sendLine, int maxCharsPerMessage = MaxCharsPerMessage)
    {
        _typeName = JsonEncodedText.Encode("type", encoder);
        _typeValue = JsonEncodedText.Encode("terminal.output", encoder);
        _paneName = JsonEncodedText.Encode("pane", encoder);
        _dataName = JsonEncodedText.Encode("data", encoder);
        _sendLine = sendLine;
        _maxCharsPerMessage = maxCharsPerMessage;
        _writer = new Utf8JsonWriter(_buffer, new JsonWriterOptions { Encoder = encoder, Indented = false });
    }

    public void Send(string paneId, StringBuilder text)
    {
        lock (_sync)
        {
            var remainingInMessage = 0;
            foreach (var chunk in text.GetChunks())
            {
                var span = chunk.Span;
                while (!span.IsEmpty)
                {
                    if (remainingInMessage == 0)
                    {
                        Begin(paneId);
                        remainingInMessage = _maxCharsPerMessage;
                    }

                    var length = Math.Min(span.Length, remainingInMessage);
                    _writer.WriteStringValueSegment(span[..length], false);
                    span = span[length..];
                    remainingInMessage -= length;
                    if (remainingInMessage == 0)
                    {
                        End();
                    }
                }
            }

            if (remainingInMessage > 0)
            {
                End();
            }
        }
    }

    private void Begin(string paneId)
    {
        _buffer.ResetWrittenCount();
        _writer.Reset(_buffer);
        _writer.WriteStartObject();
        _writer.WriteString(_typeName, _typeValue);
        _writer.WriteString(_paneName, paneId);
        _writer.WritePropertyName(_dataName);
    }

    private void End()
    {
        _writer.WriteStringValueSegment(ReadOnlySpan<char>.Empty, true);
        _writer.WriteEndObject();
        _writer.Flush();
        _buffer.GetSpan(1)[0] = (byte)'\n';
        _buffer.Advance(1);
        _sendLine(_buffer.WrittenSpan);
        if (_buffer.Capacity > RetainedBufferLimit)
        {
            _buffer = new ArrayBufferWriter<byte>();
            _writer.Reset(_buffer);
        }
    }
}
