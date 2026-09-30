using Dock.Core.Session;
using Xunit;

namespace Dock.Core.Tests.Session;

public sealed class SessionRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dock-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFile_ThenReturnsNothingWithoutError()
    {
        var repository = new SessionRepository(_directory);

        var loaded = repository.Load();

        Assert.Null(loaded.Session);
        Assert.Null(loaded.Error);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTheSession()
    {
        var repository = new SessionRepository(_directory);
        var session = SessionFactory.Initial();
        session.Workspaces[0].Name = "Projet A";

        var saved = repository.Save(session);
        var loaded = repository.Load().Session;

        Assert.True(saved.IsValid);
        Assert.NotNull(loaded);
        Assert.Equal("Projet A", loaded!.Workspaces[0].Name);
        Assert.Equal(session.Workspaces[0].Tabs[0].Tree.Pane!.Id, loaded.Workspaces[0].Tabs[0].Tree.Pane!.Id);
    }

    [Fact]
    public void Save_WhenClosedTabHasNoText_ThenWritesNoTextInTheSession()
    {
        var repository = new SessionRepository(_directory);
        var session = SessionFactory.Initial();
        session.Closed.Add(new ClosedTabModel { WorkspaceId = session.Active, WorkspaceName = "Général", Tab = SessionFactory.Tab("C:\\", "powershell") });

        var saved = repository.Save(session);

        Assert.True(saved.IsValid);
        Assert.DoesNotContain("\"text\"", File.ReadAllText(repository.FilePath));
    }

    [Fact]
    public void Save_WhenInvalid_ThenRefusesAndKeepsPreviousFile()
    {
        var repository = new SessionRepository(_directory);
        repository.Save(SessionFactory.Initial());
        var invalid = SessionFactory.Initial();
        invalid.Active = "inconnu";

        var result = repository.Save(invalid);

        Assert.False(result.IsValid);
        Assert.NotNull(repository.Load().Session);
    }

    [Fact]
    public void Load_WhenFileCorrupted_ThenQuarantinesItAndReportsError()
    {
        var repository = new SessionRepository(_directory);
        File.WriteAllText(repository.FilePath, "{ pas du json");

        var loaded = repository.Load();

        Assert.Null(loaded.Session);
        Assert.Contains("copie conservée", loaded.Error);
        Assert.False(File.Exists(repository.FilePath));
        Assert.Single(Directory.GetFiles(_directory, "session.corrompu-*.json"));
    }

    [Fact]
    public void Load_WhenFileEmptiedAfterTwoSaves_ThenRestoresPreviousSave()
    {
        var repository = new SessionRepository(_directory);
        var first = SessionFactory.Initial();
        first.Workspaces[0].Name = "Avant-dernier";
        repository.Save(first);
        var second = SessionFactory.Initial();
        second.Workspaces[0].Name = "Dernier";
        repository.Save(second);
        File.WriteAllText(repository.FilePath, string.Empty);

        var loaded = repository.Load();

        Assert.Equal("Avant-dernier", loaded.Session!.Workspaces[0].Name);
        Assert.Contains("L’avant-dernier enregistrement de la session a été restauré.", loaded.Error);
        Assert.Single(Directory.GetFiles(_directory, "session.corrompu-*.json"));
    }

    [Fact]
    public void Save_WhenPreviousCopyReadOnly_ThenStillWritesTheSession()
    {
        var repository = new SessionRepository(_directory);
        repository.Save(SessionFactory.Initial());
        var previous = Path.Combine(_directory, SessionRepository.PreviousFileName);
        File.WriteAllText(previous, "{}");
        File.SetAttributes(previous, FileAttributes.ReadOnly);
        var session = SessionFactory.Initial();
        session.Workspaces[0].Name = "Après";

        var result = repository.Save(session);
        File.SetAttributes(previous, FileAttributes.Normal);

        Assert.True(result.IsValid);
        Assert.Equal("Après", repository.Load().Session!.Workspaces[0].Name);
    }

    [Fact]
    public void Load_WhenSessionInvalid_ThenQuarantinesItWithTheValidationError()
    {
        var repository = new SessionRepository(_directory);
        File.WriteAllText(repository.FilePath, """{ "version": 2, "workspaces": [], "active": "fantome" }""");

        var loaded = repository.Load();

        Assert.Null(loaded.Session);
        Assert.Contains("Workspace actif invalide.", loaded.Error);
        Assert.Single(Directory.GetFiles(_directory, "session.corrompu-*.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
