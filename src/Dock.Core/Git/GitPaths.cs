namespace Dock.Core.Git;

public static class GitPaths
{
    public static void Run(GitRepository repository, string failure, IReadOnlyList<string> files, params string[] arguments)
    {
        var paths = files.Select(GitNames.RequireRelativePath).ToList();
        var options = new GitRunOptionsModel(Input: string.Join('\0', paths), LiteralPaths: true);
        GitRepository.Require(repository.Run(options, [.. arguments, "--pathspec-from-file=-", "--pathspec-file-nul"]), failure);
    }

    public static Dictionary<string, string> Hash(GitRepository repository, IReadOnlyList<string> files, bool store)
    {
        if (files.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        string[] arguments = store ? ["hash-object", "-w", "--no-filters", "--stdin-paths"] : ["hash-object", "--stdin-paths"];
        var hashes = repository.Read(new GitRunOptionsModel(Input: string.Join('\n', files) + "\n"), arguments)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return files.Zip(hashes).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
    }

    public static void RequireConfirmation(bool confirmed)
    {
        if (!confirmed)
        {
            throw new GitCommandException("Cette opération demande une confirmation.", string.Empty);
        }
    }
}
