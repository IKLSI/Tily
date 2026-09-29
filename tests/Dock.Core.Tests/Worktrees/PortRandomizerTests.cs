using Dock.Core.Worktrees;
using Xunit;

namespace Dock.Core.Tests.Worktrees;

public sealed class PortRandomizerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dock-ports-" + Guid.NewGuid().ToString("N"));

    public PortRandomizerTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Randomize_WhenViteConfigHasPort_ThenKeepsFirstDigitAndRewrites()
    {
        Write(@"client\vite.config.ts", "server: { port: 5173 }");

        var result = Randomizer().Randomize(_root);

        var change = Assert.Single(result.Ports);
        Assert.Equal(5173, change.Old);
        Assert.StartsWith("5", change.New.ToString());
        Assert.Equal($"server: {{ port: {change.New} }}", Read(@"client\vite.config.ts"));
    }

    [Fact]
    public void Randomize_WhenPortAppearsInsideLongerNumbers_ThenLeavesThemUntouched()
    {
        Write(".env", "API=http://localhost:5173\nAUTRE=15173\nVERSION=1.5173\nSUIVANT=51730\n");

        var result = Randomizer().Randomize(_root);

        var change = Assert.Single(result.Ports);
        Assert.Equal($"API=http://localhost:{change.New}\nAUTRE=15173\nVERSION=1.5173\nSUIVANT=51730\n", Read(".env"));
    }

    [Fact]
    public void Randomize_WhenLaunchSettingsNested_ThenFindsIt()
    {
        Write(@"server\App\App.API\Properties\launchSettings.json", "{ \"applicationUrl\": \"https://localhost:7001;http://localhost:5001\" }");

        var result = Randomizer().Randomize(_root);

        Assert.Equal([5001, 7001], result.Ports.Select(change => change.Old).Order());
    }

    [Fact]
    public void Randomize_WhenNoFreePort_ThenReportsUnavailableAndKeepsFile()
    {
        Write(".env", "port=5173");

        var result = new PortRandomizer(_ => false, new Random(1)).Randomize(_root);

        Assert.Equal(("5173", "port=5173"), (string.Join(",", result.Unavailable), Read(".env")));
    }

    [Fact]
    public void Randomize_WhenFileHasBom_ThenKeepsBom()
    {
        var path = Path.Combine(_root, ".env");
        File.WriteAllText(path, "port=5173", new System.Text.UTF8Encoding(true));

        Randomizer().Randomize(_root);

        Assert.Equal([0xEF, 0xBB, 0xBF], File.ReadAllBytes(path).Take(3));
    }

    private static PortRandomizer Randomizer() => new(_ => true, new Random(42));

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private string Read(string relativePath) => File.ReadAllText(Path.Combine(_root, relativePath));

    public void Dispose() => Directory.Delete(_root, true);
}
