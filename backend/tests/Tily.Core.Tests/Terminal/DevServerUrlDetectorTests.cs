using System.Text;
using Tily.Core.Terminal;
using Xunit;

namespace Tily.Core.Tests.Terminal;

public sealed class DevServerUrlDetectorTests
{
    private const string Escape = "\u001b";

    [Theory]
    [InlineData("  \u001b[32m➜\u001b[39m  \u001b[1mLocal\u001b[22m:   \u001b[36mhttp://localhost:\u001b[1m5173\u001b[22m/\u001b[39m\r\n", "http://localhost:5173/")]
    [InlineData("   - Local:        http://localhost:3000\r\n", "http://localhost:3000/")]
    [InlineData(" ┃ Local    http://localhost:4321/\r\n", "http://localhost:4321/")]
    [InlineData("      Now listening on: https://localhost:7043\r\n", "https://localhost:7043/")]
    [InlineData("Serving HTTP on 0.0.0.0 port 8000 (http://0.0.0.0:8000/) ...\r\n", "http://localhost:8000/")]
    [InlineData("* Listening on http://127.0.0.1:3000\r\n", "http://127.0.0.1:3000/")]
    [InlineData("Serving HTTP on :: port 8765 (http://[::]:8765/) ...\r\n", "http://localhost:8765/")]
    [InlineData("  ➜  Local:   http://localhost:5173/app/\r\n", "http://localhost:5173/app/")]
    public void Feed_WhenServerAnnouncesLocalUrl_ThenDetected(string output, string expected)
    {
        Assert.Equal([expected], Detect(output));
    }

    [Theory]
    [InlineData("curl http://localhost:8080/api\r\n")]
    [InlineData("  ➜  Network: use --host to expose\r\n")]
    [InlineData("Local: https://example.com:8443/\r\n")]
    [InlineData("Local: http://localhost:5173/")]
    public void Feed_WhenNoServerLineWithLocalUrl_ThenNothing(string output)
    {
        Assert.Empty(Detect(output));
    }

    [Fact]
    public void Feed_WhenLineSplitBetweenReads_ThenDetectedOnce()
    {
        var detector = new DevServerUrlDetector();
        var detected = new List<string>();
        detector.Detected += detected.Add;

        detector.Feed(Encoding.UTF8.GetBytes("  ➜  Local:   http://local"));
        detector.Feed(Encoding.UTF8.GetBytes($"host:5173/{Escape}[39m\r\n"));

        Assert.Equal(["http://localhost:5173/"], detected);
    }

    [Fact]
    public void Feed_WhenLineExceedsLimit_ThenKeepsDetectingNextLines()
    {
        var output = new string('x', 5000) + "\r\nLocal: http://localhost:4200/\r\n";

        Assert.Equal(["http://localhost:4200/"], Detect(output));
    }

    private static List<string> Detect(string output)
    {
        var detector = new DevServerUrlDetector();
        var detected = new List<string>();
        detector.Detected += detected.Add;
        detector.Feed(Encoding.UTF8.GetBytes(output));
        return detected;
    }
}
