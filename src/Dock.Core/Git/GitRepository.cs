namespace Dock.Core.Git;

public sealed class GitRepository
{
    private readonly GitRunner _runner;

    public GitRepository(GitRunner runner, GitLocationModel location)
    {
        _runner = runner;
        Location = location;
    }

    public GitLocationModel Location { get; }

    public string Root => Location.Root;

    public static GitLocationModel? Locate(GitRunner runner, string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        var output = runner.Run(folder, ["rev-parse", "--path-format=absolute", "--show-toplevel", "--git-dir", "--git-common-dir"]);
        var lines = output.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!output.Succeeded || lines.Length < 3)
        {
            return null;
        }

        return new GitLocationModel(NormalizePath(lines[0]), NormalizePath(lines[1]), NormalizePath(lines[2]));
    }

    public static GitRepository Open(GitRunner runner, string folder) =>
        new(runner, Locate(runner, folder) ?? throw new GitCommandException($"Aucun dépôt Git dans {folder}.", string.Empty));

    public GitOutputModel Run(params string[] arguments) => _runner.Run(Root, arguments);

    public GitOutputModel Run(GitRunOptionsModel options, params string[] arguments) => _runner.Run(Root, arguments, options);

    public string Read(params string[] arguments) => Require(Run(arguments), $"La commande « git {arguments.FirstOrDefault()} » a échoué.");

    public string Read(GitRunOptionsModel options, params string[] arguments) => Require(Run(options, arguments), $"La commande « git {arguments.FirstOrDefault()} » a échoué.");

    public byte[] ReadBytes(params string[] arguments) => _runner.ReadBytes(Root, arguments);

    public static string Require(GitOutputModel output, string failure) =>
        output.Succeeded ? output.Output : throw new GitCommandException(failure, output.Details);

    public string? HeadSha() => ValueOf(Run("rev-parse", "--verify", "-q", "HEAD^{commit}"));

    public string? CurrentBranch() => ValueOf(Run("symbolic-ref", "-q", "--short", "HEAD"));

    public string? Resolve(string revision) => ValueOf(Run("rev-parse", "--verify", "-q", $"{GitNames.RequireRevision(revision)}^{{commit}}"));

    public string? RefValue(string refName) => ValueOf(Run("rev-parse", "--verify", "-q", refName));

    public string RequireCommit(string? revision) =>
        Resolve(GitNames.RequireRevision(revision)) ?? throw new GitCommandException($"Référence introuvable : « {revision} ».", string.Empty);

    public void RequireNoOperation()
    {
        if (Operation() is { } operation)
        {
            throw new GitCommandException($"{GitOperationNames.Label(operation)} en cours : terminez ou annulez l’opération d’abord.", string.Empty);
        }
    }

    public string? Config(string key) => ValueOf(Run("config", "--get", key));

    public bool IsAncestor(string ancestor, string descendant) => Run("merge-base", "--is-ancestor", ancestor, descendant).Succeeded;

    public bool IsPublished(string sha) => Read("for-each-ref", "--contains", sha, "--count=1", "--format=%(refname)", "refs/remotes").Trim().Length > 0;

    public IReadOnlyList<string> Remotes() => Read("remote").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public GitOperationKind? Operation()
    {
        var directory = Location.GitDirectory;
        if (Directory.Exists(Path.Combine(directory, "rebase-merge")) || (Directory.Exists(Path.Combine(directory, "rebase-apply")) && !File.Exists(Path.Combine(directory, "rebase-apply", "applying"))))
        {
            return GitOperationKind.Rebase;
        }

        if (File.Exists(Path.Combine(directory, "MERGE_HEAD")))
        {
            return GitOperationKind.Merge;
        }

        if (File.Exists(Path.Combine(directory, "CHERRY_PICK_HEAD")))
        {
            return GitOperationKind.CherryPick;
        }

        return File.Exists(Path.Combine(directory, "REVERT_HEAD")) ? GitOperationKind.Revert : null;
    }

    public GitStatusModel Status(int limit = GitStatusParser.MaxFilesPerGroup) =>
        GitStatusParser.Parse(Read("status", "--porcelain=v2", "--branch", "-z", "--untracked-files=all"), limit);

    public string FullPath(string relativePath) => Path.GetFullPath(Path.Combine(Root, GitNames.RequireRelativePath(relativePath)));

    public static string Short(string? sha) => sha is { Length: > 7 } ? sha[..7] : sha ?? string.Empty;

    public static string? ValueOf(GitOutputModel output)
    {
        var value = output.Output.Trim();
        return output.Succeeded && value.Length > 0 ? value : null;
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar));
}
