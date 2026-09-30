using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

public sealed class PathFallbackTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-fallback-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void NearestExisting_WhenPathExists_ThenReturnsIt()
    {
        Directory.CreateDirectory(_root);

        Assert.Equal(_root, PathFallback.NearestExisting(_root));
    }

    [Fact]
    public void NearestExisting_WhenLeafRemoved_ThenReturnsClosestExistingAncestor()
    {
        var kept = Path.Combine(_root, "projet");
        Directory.CreateDirectory(kept);
        var removed = Path.Combine(kept, "worktrees", "wt-supprime");

        Assert.Equal(kept, PathFallback.NearestExisting(removed));
    }

    [Fact]
    public void NearestExisting_WhenNothingExists_ThenReturnsUserProfile()
    {
        var fallback = PathFallback.NearestExisting(@"Q:\dossier\inexistant");

        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fallback);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
