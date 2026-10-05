using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

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
        var path = WorktreeTarget.PathFor("/Files/Projects/worktrees", "tily", "feat/worktrees");

        Assert.Equal("/Files/Projects/worktrees/tily-worktrees", path);
    }

    [Fact]
    public void FolderFor_WhenFolderEmpty_ThenDerivesFromProjectsRoot()
    {
        var folder = WorktreeSettingsModel.Default.FolderFor("/Projets");

        Assert.Equal("/Projets/worktrees", folder);
    }

    [Fact]
    public void FolderFor_WhenFolderConfigured_ThenUsesIt()
    {
        var folder = new WorktreeSettingsModel("/wt", "develop").FolderFor("/Projets");

        Assert.Equal("/wt", folder);
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
