using System.Text;

namespace Tily.Host;

public sealed class StdioChannel
{
    private readonly object _writeLock = new();
    private readonly StreamWriter _output = new(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false, NewLine = "\n" };
    private readonly StreamReader _input = new(Console.OpenStandardInput(), new UTF8Encoding(false));

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
