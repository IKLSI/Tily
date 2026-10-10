using Tily.Core.Session;
using Xunit;

namespace Tily.Core.Tests.Session;

public sealed class PaneTextRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFile_ThenReturnsEmpty()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        var loaded = repository.Load();

        Assert.Empty(loaded);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTextPerPane()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        repository.Save(new Dictionary<string, string> { ["p1"] = "ligne 1\r\nligne 2", ["p2"] = "autre" }, []);
        var loaded = repository.Load();

        Assert.Equal("ligne 1\r\nligne 2", loaded["p1"]);
        Assert.Equal("autre", loaded["p2"]);
    }

    [Fact]
    public void Save_WhenPaneOnlyKept_ThenKeepsItsPreviousText()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p1"] = "ancien" }, []);

        repository.Save(new Dictionary<string, string> { ["p2"] = "nouveau" }, ["p1"]);
        var loaded = repository.Load();

        Assert.Equal("ancien", loaded["p1"]);
        Assert.Equal("nouveau", loaded["p2"]);
    }

    [Fact]
    public void Save_WhenPaneNeitherSentNorKept_ThenDeletesItsText()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p1"] = "gardé", ["p2"] = "fermé" }, []);

        repository.Save(new Dictionary<string, string>(), ["p1"]);
        var loaded = repository.Load();

        Assert.Equal(new[] { "p1" }, loaded.Keys);
    }

    [Fact]
    public void Save_WhenTotalExceedsLimit_ThenSkipsPanesBeyondLimit()
    {
        var repository = new PaneTextRepository(_directory, 10);

        repository.Save(new Dictionary<string, string> { ["p1"] = "123456", ["p2"] = "123456" }, []);
        var loaded = repository.Load();

        Assert.Equal(new[] { "p1" }, loaded.Keys);
    }

    [Fact]
    public void Save_WhenPaneIdIsNotAFileName_ThenIgnoresIt()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        repository.Save(new Dictionary<string, string> { ["..\\hors"] = "texte" }, []);

        Assert.Empty(Directory.GetFiles(_directory, "*", SearchOption.AllDirectories));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
