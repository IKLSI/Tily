using Dock.Core.StatusLog;
using Xunit;

namespace Dock.Core.Tests.StatusLog;

public sealed class StatusLogRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 14, 30, 0, TimeSpan.FromHours(2));

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dock-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFile_ThenStartsEmptyWithoutError()
    {
        var repository = new StatusLogRepository(_directory);

        var error = repository.Load();

        Assert.Null(error);
        Assert.Empty(repository.Entries());
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTheEntries()
    {
        var repository = new StatusLogRepository(_directory);
        repository.Append(StatusLogLevel.Info, "Onglet fermé.", At);
        repository.Append(StatusLogLevel.Error, "Push refusé.", At.AddMinutes(1));
        repository.Save();

        var reloaded = new StatusLogRepository(_directory);
        reloaded.Load();

        Assert.Equal(repository.Entries(), reloaded.Entries());
    }

    [Fact]
    public void Append_WhenTextIsBlank_ThenIgnoresIt()
    {
        var repository = new StatusLogRepository(_directory);

        var entry = repository.Append(StatusLogLevel.Info, "   ", At);

        Assert.Null(entry);
        Assert.Empty(repository.Entries());
    }

    [Fact]
    public void Append_WhenTextIsTooLong_ThenTruncatesIt()
    {
        var repository = new StatusLogRepository(_directory);

        var entry = repository.Append(StatusLogLevel.Warning, new string('a', StatusLogRepository.MaxTextLength + 50), At);

        Assert.Equal(StatusLogRepository.MaxTextLength, entry!.Text.Length);
    }

    [Fact]
    public void Append_WhenLimitIsReached_ThenDropsTheOldestEntries()
    {
        var repository = new StatusLogRepository(_directory);

        for (var index = 0; index < StatusLogRepository.MaxEntries + 3; index++)
        {
            repository.Append(StatusLogLevel.Info, $"Message {index}", At.AddSeconds(index));
        }

        Assert.Equal("Message 3", repository.Entries()[0].Text);
        Assert.Equal(StatusLogRepository.MaxEntries, repository.Entries().Count);
    }

    [Fact]
    public void Clear_ThenSave_WritesAnEmptyJournal()
    {
        var repository = new StatusLogRepository(_directory);
        repository.Append(StatusLogLevel.Info, "Onglet fermé.", At);
        repository.Save();

        repository.Clear();
        repository.Save();
        var reloaded = new StatusLogRepository(_directory);
        reloaded.Load();

        Assert.Empty(reloaded.Entries());
    }

    [Fact]
    public void Load_WhenFileIsUnreadable_ThenQuarantinesItAndStartsEmpty()
    {
        var repository = new StatusLogRepository(_directory);
        File.WriteAllText(repository.FilePath, "{ pas du json");

        var error = repository.Load();

        Assert.Contains("illisible", error);
        Assert.False(File.Exists(repository.FilePath));
        Assert.Empty(repository.Entries());
    }

    [Fact]
    public void Load_WhenEntriesHaveNoText_ThenSkipsThem()
    {
        var repository = new StatusLogRepository(_directory);
        File.WriteAllText(repository.FilePath, """{ "entries": [ { "at": "2026-09-29T14:30:00+02:00", "level": "error", "text": "Échec" }, { "at": "2026-09-29T14:31:00+02:00", "level": "info" } ] }""");

        repository.Load();

        Assert.Equal("Échec", Assert.Single(repository.Entries()).Text);
    }

    [Fact]
    public void ParseLevel_WhenLevelIsUnknown_ThenFailsInFrench()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => StatusLogRepository.ParseLevel("debug"));

        Assert.Equal("Niveau de message inconnu : debug", failure.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
