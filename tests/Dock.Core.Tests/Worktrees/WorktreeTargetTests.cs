using Dock.Core.Worktrees;
using Xunit;

namespace Dock.Core.Tests.Worktrees;

public sealed class WorktreeTargetTests
{
    [Theory]
    [InlineData("feat/vue-git", "vue-git")]
    [InlineData("correctif", "correctif")]
    [InlineData("equipe/feat/liste", "liste")]
    public void Slug_WhenBranchHasPrefixes_ThenKeepsLastSegment(string branch, string expected)
    {
        var slug = WorktreeTarget.Slug(branch);

        Assert.Equal(expected, slug);
    }

    [Fact]
    public void PathFor_WhenBranchGiven_ThenJoinsProjectAndSlug()
    {
        var path = WorktreeTarget.PathFor(@"C:\Files\Projects\worktrees", "dock-terminal", "feat/worktrees");

        Assert.Equal(@"C:\Files\Projects\worktrees\dock-terminal-worktrees", path);
    }

    [Fact]
    public void FolderFor_WhenFolderEmpty_ThenDerivesFromProjectsRoot()
    {
        var folder = WorktreeSettingsModel.Default.FolderFor(@"D:\Projets");

        Assert.Equal(@"D:\Projets\worktrees", folder);
    }

    [Fact]
    public void FolderFor_WhenFolderConfigured_ThenUsesIt()
    {
        var folder = new WorktreeSettingsModel(@"E:\wt", "develop").FolderFor(@"D:\Projets");

        Assert.Equal(@"E:\wt", folder);
    }

    [Fact]
    public void Error_WhenFolderRelative_ThenRefusesInFrench()
    {
        var error = new WorktreeSettingsModel("relatif", "develop").Error();

        Assert.Equal("Le dossier des worktrees doit être un chemin absolu : relatif", error);
    }

    [Fact]
    public void Normalized_WhenBaseEmpty_ThenFallsBackToDevelop()
    {
        var settings = new WorktreeSettingsModel(" ", " ").Normalized();

        Assert.Equal(new WorktreeSettingsModel(string.Empty, "develop"), settings);
    }
}
