using System.Text;
using System.Text.Json.Serialization;

namespace Dock.Core.Git;

[JsonConverter(typeof(JsonStringEnumConverter<GitDiffSource>))]
public enum GitDiffSource
{
    [JsonStringEnumMemberName("unstaged")] Unstaged,
    [JsonStringEnumMemberName("staged")] Staged,
    [JsonStringEnumMemberName("commit")] Commit
}

public sealed record GitDiffRequestModel(GitDiffSource Source, string Path, string? OldPath, string? Commit, bool Untracked);

public static class GitDiffReader
{
    public const int MaxUntrackedBytes = 2 * 1024 * 1024;
    public const int MaxCommitFiles = 5000;

    private const int BinaryProbeBytes = 8000;
    private const char Separator = '\u001f';
    private static readonly string[] DiffOptions = ["--no-ext-diff", "--no-color", "-U3", "-M"];

    public static GitDiffModel Read(GitRepository repository, GitDiffRequestModel request)
    {
        var path = GitNames.RequireRelativePath(request.Path);
        var oldPath = request.OldPath is null ? null : GitNames.RequireRelativePath(request.OldPath);
        if (request.Source == GitDiffSource.Unstaged && request.Untracked)
        {
            return ReadUntracked(repository, path);
        }

        string[] paths = oldPath is null ? [path] : [oldPath, path];
        string[] arguments = request.Source switch
        {
            GitDiffSource.Staged => ["diff", "--cached", .. DiffOptions, "--", .. paths],
            GitDiffSource.Unstaged => ["diff", .. DiffOptions, "--", .. paths],
            _ => CommitArguments(repository, request.Commit, paths)
        };
        return GitDiffParser.Parse(path, oldPath, repository.Read(new GitRunOptionsModel(LiteralPaths: true), arguments));
    }

    public static GitCommitDetailsModel ReadCommit(GitRepository repository, string? commit)
    {
        var sha = RequireCommit(repository, commit);
        var fields = repository.Read("show", "-s", "--no-show-signature", "--format=%H%x1f%P%x1f%an%x1f%ae%x1f%at%x1f%B", sha).Split(Separator, 6);
        if (fields.Length < 6)
        {
            throw new GitCommandException($"Commit illisible : {commit}.", string.Empty);
        }

        var parents = GitHistoryReader.Parents(fields[1]);
        string[] arguments = parents.Count == 0
            ? ["diff-tree", "-r", "-M", "--root", "--no-commit-id", "--name-status", "-z", sha]
            : ["diff-tree", "-r", "-M", "--name-status", "-z", parents[0], sha];
        var files = ParseNameStatus(repository.Read(arguments));
        return new GitCommitDetailsModel(fields[0].Trim(), parents, fields[2], fields[3], long.TryParse(fields[4], out var date) ? date : 0, fields[5].TrimEnd(), files);
    }

    public static IReadOnlyList<GitFileChangeModel> ParseNameStatus(string output)
    {
        var tokens = output.Split('\0');
        var files = new List<GitFileChangeModel>();
        for (var index = 0; index < tokens.Length && files.Count < MaxCommitFiles; index++)
        {
            var status = tokens[index].Trim('\n');
            if (status.Length == 0)
            {
                continue;
            }

            var kind = status[0] switch
            {
                'A' => GitChangeKind.Added,
                'D' => GitChangeKind.Deleted,
                'R' => GitChangeKind.Renamed,
                'C' => GitChangeKind.Copied,
                'T' => GitChangeKind.TypeChanged,
                _ => GitChangeKind.Modified
            };
            if (kind is GitChangeKind.Renamed or GitChangeKind.Copied && index + 2 < tokens.Length)
            {
                files.Add(new GitFileChangeModel(tokens[index + 2], tokens[index + 1], kind));
                index += 2;
            }
            else if (index + 1 < tokens.Length)
            {
                files.Add(new GitFileChangeModel(tokens[index + 1], null, kind));
                index++;
            }
        }

        return files;
    }

    private static string RequireCommit(GitRepository repository, string? commit) =>
        repository.Resolve(commit ?? string.Empty) ?? throw new GitCommandException($"Commit introuvable : {commit}.", string.Empty);

    private static string[] CommitArguments(GitRepository repository, string? commit, string[] paths)
    {
        var sha = RequireCommit(repository, commit);
        var parents = GitHistoryReader.Parents(repository.Read("rev-list", "--parents", "-n", "1", sha).Trim()).Skip(1).ToList();
        return parents.Count == 0
            ? ["diff-tree", "-p", "-r", "--root", "--no-commit-id", .. DiffOptions, sha, "--", .. paths]
            : ["diff-tree", "-p", "-r", .. DiffOptions, parents[0], sha, "--", .. paths];
    }

    private static GitDiffModel ReadUntracked(GitRepository repository, string path)
    {
        var info = new FileInfo(repository.FullPath(path));
        if (!info.Exists)
        {
            throw new GitCommandException($"Le fichier n’existe plus : {path}", string.Empty);
        }

        const string untracked = "Nouveau fichier non suivi";
        if (info.Length > MaxUntrackedBytes)
        {
            return new GitDiffModel(path, null, false, true, [untracked, $"Fichier trop volumineux pour être affiché ({info.Length / 1024} Kio)."], []);
        }

        var bytes = File.ReadAllBytes(info.FullName);
        if (bytes.AsSpan(0, Math.Min(bytes.Length, BinaryProbeBytes)).Contains((byte)0))
        {
            return new GitDiffModel(path, null, true, false, [untracked, "Fichier binaire : contenu non affiché"], []);
        }

        var text = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n");
        var content = text.EndsWith('\n') ? text[..^1] : text;
        var lines = content.Length == 0 ? [] : content.Split('\n');
        var shown = lines.Take(GitDiffParser.MaxLines).Select((line, index) => new GitDiffLineModel(GitDiffLineKind.Added, null, index + 1, line)).ToList();
        var hunks = shown.Count == 0 ? [] : new List<GitDiffHunkModel> { new($"@@ -0,0 +1,{lines.Length} @@", shown) };
        return new GitDiffModel(path, null, false, lines.Length > shown.Count, [untracked], hunks);
    }
}
