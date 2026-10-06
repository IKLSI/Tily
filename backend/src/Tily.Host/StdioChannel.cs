using System.Text;

namespace Tily.Host;

public sealed class StdioChannel
{
    private readonly object _writeLock = new();
    private readonly Stream _standardOutput;
    private readonly StreamWriter _output;
    private readonly StreamReader _input = new(Console.OpenStandardInput(), new UTF8Encoding(false));

    public StdioChannel()
    {
        _standardOutput = Console.OpenStandardOutput();
        _output = new StreamWriter(_standardOutput, new UTF8Encoding(false)) { AutoFlush = false, NewLine = "\n" };
    }

    public void Send(string json)
    {
        lock (_writeLock)
        {
            try
            {
                _output.WriteLine(json);
                _output.Flush();
            }
            catch (IOException)
            {
            }
        }
    }

    public void SendUtf8Line(ReadOnlySpan<byte> line)
    {
        lock (_writeLock)
        {
            try
            {
                _standardOutput.Write(line);
                _standardOutput.Flush();
            }
            catch (IOException)
            {
            }
        }
    }

    public void Listen(Action<string> receive, Action closed)
    {
        var reader = new Thread(() =>
        {
            try
            {
                while (_input.ReadLine() is { } line)
                {
                    if (line.Length > 0)
                    {
                        receive(line);
                    }
                }
            }
            catch (IOException)
            {
            }

            closed();
        })
        {
            IsBackground = true,
            Name = "stdio-reader"
        };
        reader.Start();
    }
}
