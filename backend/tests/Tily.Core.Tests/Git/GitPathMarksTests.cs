using Tily.Core.Git;
using Xunit;

namespace Tily.Core.Tests.Git;

public sealed class GitPathMarksTests
{
    private const string Root = "/dépôt";

    private static readonly GitHeadModel Head = new("main", null, false, false, null, 0, 0);

    private static GitStatusModel Status(GitFileChangeModel[] staged, GitFileChangeModel[] unstaged, GitConflictModel[]? conflicts = null) =>
        new(Head, staged, unstaged, conflicts ?? [], staged.Length, unstaged.Length);

    [Fact]
    public void From_WhenNestedPath_ThenGivesFullPath()
    {
        var status = Status([], [new GitFileChangeModel("dossier avec espace/é.txt", null, GitChangeKind.Modified)]);

        var marks = GitPathMarks.From(Root, status);

        Assert.Equal([new GitPathMarkModel("/dépôt/dossier avec espace/é.txt", GitChangeKind.Modified, false)], marks);
    }

    [Fact]
    public void From_WhenStagedAddedThenModified_ThenKeepsAdded()
    {
        var status = Status([new GitFileChangeModel("nouveau.txt", null, GitChangeKind.Added)], [new GitFileChangeModel("nouveau.txt", null, GitChangeKind.Modified)]);

        var marks = GitPathMarks.From(Root, status);

        Assert.Equal([new GitPathMarkModel("/dépôt/nouveau.txt", GitChangeKind.Added, false)], marks);
    }

    [Fact]
    public void From_WhenStagedModifiedThenDeleted_ThenKeepsDeleted()
    {
        var status = Status([new GitFileChangeModel("a.txt", null, GitChangeKind.Modified)], [new GitFileChangeModel("a.txt", null, GitChangeKind.Deleted)]);

        var marks = GitPathMarks.From(Root, status);

        Assert.Equal([new GitPathMarkModel("/dépôt/a.txt", GitChangeKind.Deleted, false)], marks);
    }

    [Fact]
    public void From_WhenConflict_ThenMarksConflicted()
    {
        var status = Status([], [], [new GitConflictModel("conflit.txt", GitConflictKind.BothModified)]);

        var marks = GitPathMarks.From(Root, status);

        Assert.Equal([new GitPathMarkModel("/dépôt/conflit.txt", GitChangeKind.Modified, true)], marks);
    }

    [Fact]
    public void DisplayRoot_WhenFolderIsRepositoryRoot_ThenKeepsFolder()
    {
        var root = GitPathMarks.DisplayRoot("/lien/dépôt/", string.Empty, "/réel/dépôt");

        Assert.Equal("/lien/dépôt", root);
    }

    [Fact]
    public void DisplayRoot_WhenFolderIsNested_ThenRemovesPrefixFromFolder()
    {
        var root = GitPathMarks.DisplayRoot("/lien/dépôt/web/src", "web/src/", "/réel/dépôt");

        Assert.Equal("/lien/dépôt", root);
    }

    [Fact]
    public void DisplayRoot_WhenPrefixDoesNotMatchFolder_ThenFallsBack()
    {
        var root = GitPathMarks.DisplayRoot("/lien/autre", "web/", "/réel/dépôt");

        Assert.Equal("/réel/dépôt", root);
    }

    [Fact]
    public void From_WhenRepositoryStatus_ThenMarksModifiedAndUntrackedFiles()
    {
        using var sandbox = new GitSandbox();
        File.WriteAllText(Path.Combine(sandbox.Work, "suivi.txt"), "un");
        sandbox.Git("add", "suivi.txt");
        sandbox.Git("commit", "-q", "-m", "Premier commit");
        File.WriteAllText(Path.Combine(sandbox.Work, "suivi.txt"), "deux");
        Directory.CreateDirectory(Path.Combine(sandbox.Work, "neuf"));
        File.WriteAllText(Path.Combine(sandbox.Work, "neuf", "fichier.md"), "texte");

        var marks = GitPathMarks.From(sandbox.Work, sandbox.Repository.Status());

        Assert.Equal(
            [new GitPathMarkModel(Path.Combine(sandbox.Work, "suivi.txt"), GitChangeKind.Modified, false), new GitPathMarkModel(Path.Combine(sandbox.Work, "neuf", "fichier.md"), GitChangeKind.Untracked, false)],
            marks.OrderBy(mark => mark.Kind).ToList());
    }

    [Fact]
    public void DisplayRootFrom_WhenFolderIsNested_ThenGivesRootAsSeenFromFolder()
    {
        using var sandbox = new GitSandbox();
        var nested = Path.Combine(sandbox.Work, "src", "web");
        Directory.CreateDirectory(nested);

        var root = GitPathMarks.DisplayRootFrom(sandbox.Runner, nested, "/ailleurs");

        Assert.Equal(sandbox.Work, root);
    }

    [Fact]
    public void DisplayRootFrom_WhenFolderOutsideRepository_ThenFallsBack()
    {
        var folder = Directory.CreateTempSubdirectory("tily-hors-depot-").FullName;

        var root = GitPathMarks.DisplayRootFrom(new GitRunner(), folder, "/repli");

        Directory.Delete(folder);
        Assert.Equal("/repli", root);
    }
}
