using System.Text.Json;
using Tily.Core.Agents;
using Xunit;

namespace Tily.Core.Tests.Agents;

public sealed class PreviewRequestRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));
    private readonly PreviewRequestRepository _repository;

    public PreviewRequestRepositoryTests()
    {
        _repository = new PreviewRequestRepository(_directory);
        Directory.CreateDirectory(_repository.Directory);
    }

    [Fact]
    public void TakeAll_WhenHtmlRequested_ThenReturnsItAndDeletesTheFile()
    {
        var page = Page("relecture été.html");
        Request("a.json", "pane-1", page);

        var requests = _repository.TakeAll();

        Assert.Equal(new PreviewRequestModel("pane-1", page), Assert.Single(requests));
        Assert.Empty(Directory.EnumerateFiles(_repository.Directory));
    }

    [Fact]
    public void TakeAll_WhenFileIsNotHtmlOrMissing_ThenIgnoresAndDeletesIt()
    {
        Request("texte.json", "pane-1", Page("notes.txt"));
        Request("absent.json", "pane-1", Path.Combine(_directory, "absent.html"));
        Request("relatif.json", "pane-1", "plan.html");
        File.WriteAllText(Path.Combine(_repository.Directory, "illisible.json"), "{ oops");

        var requests = _repository.TakeAll();

        Assert.Empty(requests);
        Assert.Empty(Directory.EnumerateFiles(_repository.Directory));
    }

    [Fact]
    public void TakeAll_WhenRequestStillBeingWritten_ThenLeavesTemporaryFile()
    {
        File.WriteAllText(Path.Combine(_repository.Directory, "pane-1-abc.tmp"), "{");

        var requests = _repository.TakeAll();

        Assert.Empty(requests);
        Assert.Single(Directory.EnumerateFiles(_repository.Directory));
    }

    [Fact]
    public void Clear_WhenRequestsLeftFromPreviousRun_ThenDeletesThem()
    {
        Request("ancien.json", "pane-1", Page("plan.html"));

        _repository.Clear();

        Assert.Empty(_repository.TakeAll());
    }

    private string Page(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "<h1>Plan</h1>");
        return path;
    }

    private void Request(string name, string paneId, string path) =>
        File.WriteAllText(Path.Combine(_repository.Directory, name), JsonSerializer.Serialize(new { pane = paneId, path }));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
