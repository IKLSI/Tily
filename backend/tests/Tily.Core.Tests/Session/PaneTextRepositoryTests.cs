using Tily.Core.Session;
using Xunit;

namespace Tily.Core.Tests.Session;

public sealed class PaneTextRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFile_ThenReturnsEmptyWithoutError()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        var loaded = repository.Load();

        Assert.Empty(loaded.Text);
        Assert.Null(loaded.Error);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTextPerPane()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        repository.Save(new Dictionary<string, string> { ["p1"] = "ligne 1\r\nligne 2", ["p2"] = "autre" }, []);
        var loaded = repository.Load();

        Assert.Equal("ligne 1\r\nligne 2", loaded.Text["p1"]);
        Assert.Equal("autre", loaded.Text["p2"]);
    }

    [Fact]
    public void Save_WhenPaneOnlyKept_ThenKeepsItsPreviousText()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p1"] = "ancien" }, []);

        repository.Save(new Dictionary<string, string> { ["p2"] = "nouveau" }, ["p1"]);
        var loaded = repository.Load();

        Assert.Equal("ancien", loaded.Text["p1"]);
        Assert.Equal("nouveau", loaded.Text["p2"]);
    }

    [Fact]
    public void Save_WhenPaneNeitherSentNorKept_ThenDeletesItsText()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p1"] = "gardé", ["p2"] = "fermé" }, []);

        repository.Save(new Dictionary<string, string>(), ["p1"]);
        var loaded = repository.Load();

        Assert.Equal(new[] { "p1" }, loaded.Text.Keys);
    }

    [Fact]
    public void Save_WhenTotalExceedsLimit_ThenSkipsPanesBeyondLimit()
    {
        var repository = new PaneTextRepository(_directory, 10);

        repository.Save(new Dictionary<string, string> { ["p1"] = "123456", ["p2"] = "123456" }, []);
        var loaded = repository.Load();

        Assert.Equal(new[] { "p1" }, loaded.Text.Keys);
    }

    [Fact]
    public void Save_WhenPaneIdIsNotAFileName_ThenIgnoresIt()
    {
        var repository = new PaneTextRepository(_directory, 1000);

        repository.Save(new Dictionary<string, string> { ["..\\hors"] = "texte" }, []);

        Assert.Empty(Directory.GetFiles(_directory, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Load_WhenLegacyFileExists_ThenConvertsItToPaneFiles()
    {
        Directory.CreateDirectory(_directory);
        var legacyPath = Path.Combine(_directory, PaneTextRepository.LegacyFileName);
        File.WriteAllText(legacyPath, """{ "p1": "historique" }""");
        var repository = new PaneTextRepository(_directory, 1000);

        var loaded = repository.Load();

        Assert.Equal("historique", loaded.Text["p1"]);
        Assert.False(File.Exists(legacyPath));
    }

    [Fact]
    public void Load_WhenLegacyFileWrittenAfterPaneFiles_ThenLegacyTextWins()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p1"] = "texte périmé" }, []);
        File.WriteAllText(Path.Combine(_directory, PaneTextRepository.LegacyFileName), """{ "p1": "écrit par une version antérieure" }""");

        var loaded = repository.Load();

        Assert.Equal("écrit par une version antérieure", loaded.Text["p1"]);
    }

    [Fact]
    public void Load_WhenLegacyFileCorrupted_ThenQuarantinesAndReturnsEmpty()
    {
        Directory.CreateDirectory(_directory);
        var legacyPath = Path.Combine(_directory, PaneTextRepository.LegacyFileName);
        File.WriteAllText(legacyPath, "{ pas du json");
        var repository = new PaneTextRepository(_directory, 1000);

        var loaded = repository.Load();

        Assert.Empty(loaded.Text);
        Assert.False(File.Exists(legacyPath));
        Assert.Contains("copie conservée", loaded.Error);
        Assert.Single(Directory.GetFiles(_directory, "text.corrompu-*.json"));
    }

    [Fact]
    public void MoveClosedTabText_WhenClosedTabHasText_ThenStoresItAndClearsTheSession()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        var session = SessionFactory.Initial();
        session.Closed.Add(new ClosedTabModel { WorkspaceId = session.Active, WorkspaceName = "Général", Tab = SessionFactory.Tab("/Users/moi", "zsh"), Text = new() { ["p9"] = "onglet fermé" } });

        repository.MoveClosedTabText(session);

        Assert.Null(session.Closed[0].Text);
        Assert.Equal("onglet fermé", repository.Load().Text["p9"]);
    }

    [Fact]
    public void MoveClosedTabText_WhenPaneTextAlreadyStored_ThenSessionTextWins()
    {
        var repository = new PaneTextRepository(_directory, 1000);
        repository.Save(new Dictionary<string, string> { ["p9"] = "texte périmé" }, []);
        var session = SessionFactory.Initial();
        session.Closed.Add(new ClosedTabModel { WorkspaceId = session.Active, WorkspaceName = "Général", Tab = SessionFactory.Tab("/Users/moi", "zsh"), Text = new() { ["p9"] = "écrit par une version antérieure" } });

        repository.MoveClosedTabText(session);

        Assert.Equal("écrit par une version antérieure", repository.Load().Text["p9"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
