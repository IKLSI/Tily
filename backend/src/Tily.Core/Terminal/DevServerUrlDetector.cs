using System.Text;
using System.Text.RegularExpressions;

namespace Tily.Core.Terminal;

public sealed partial class DevServerUrlDetector
{
    private const byte LineFeed = (byte)'\n';
    private const int MaxLineBytes = 1024;
    private static readonly HashSet<string> AnyAddresses = ["0.0.0.0", "[::]"];
    private const string Localhost = "localhost";

    private readonly byte[] _line = new byte[MaxLineBytes];
    private int _length;

    public event Action<string>? Detected;

    public void Feed(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            if (value == LineFeed)
            {
                Inspect();
                _length = 0;
            }
            else if (_length < MaxLineBytes)
            {
                _line[_length++] = value;
            }
        }
    }

    public static string? UrlIn(string line)
    {
        var text = AnsiSequence().Replace(line, string.Empty);
        if (!ServerKeyword().IsMatch(text))
        {
            return null;
        }

        var match = LocalUrl().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var host = AnyAddresses.Contains(match.Groups["host"].Value) ? Localhost : match.Groups["host"].Value;
        var path = match.Groups["path"].Value.TrimEnd('.', ',', ';', ':');
        return $"{match.Groups["scheme"].Value}://{host}:{match.Groups["port"].Value}{(path.Length > 0 ? path : "/")}";
    }

    private void Inspect()
    {
        if (_length == 0)
        {
            return;
        }

        if (UrlIn(Encoding.UTF8.GetString(_line, 0, _length)) is { } url)
        {
            Detected?.Invoke(url);
        }
    }

    [GeneratedRegex(@"\x1B\][^\x07\x1B]*(?:\x07|\x1B\\)|\x1B\[[0-?]*[ -/]*[@-~]|\x1B[@-Z\\-_]")]
    private static partial Regex AnsiSequence();

    [GeneratedRegex(@"\b(?:local|listening|running|ready|started|serving|server)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ServerKeyword();

    [GeneratedRegex(@"(?<scheme>https?)://(?<host>localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1?\]):(?<port>\d{2,5})(?<path>/[^\s'""<>()\x1B]*)?", RegexOptions.IgnoreCase)]
    private static partial Regex LocalUrl();
}
