using System.Diagnostics;
using Tily.Core.Git;

namespace Tily.Core.Files;

public sealed record ProjectFilesModel(string Root, IReadOnlyList<string> Files, IReadOnlyList<string> Changed, bool Truncated);

public static class ProjectFiles
{
    public const int MaxFiles = 20000;

    public static readonly TimeSpan DiskTimeLimit = TimeSpan.FromSeconds(3);

    private const string SubmoduleMode = "160000 ";
    private const char StageSeparator = '\t';

    private const char GitSeparator = '/';
    private const char EntrySeparator = '\0';

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { ".git", ".vs", "node_modules", "bin", "obj" };

    public static ProjectFilesModel List(GitRunner runner, string folder, int maxFiles = MaxFiles, TimeSpan? diskTimeLimit = null, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            throw new InvalidOperationException($"Dossier introuvable : {folder}");
        }

        var location = GitRunner.IsInstalled ? GitRepository.Locate(runner, folder) : null;
        return location is null ? FromDisk(folder, maxFiles, diskTimeLimit ?? DiskTimeLimit, cancellation) : FromGit(runner, folder, location, maxFiles);
    }

    private static ProjectFilesModel FromGit(GitRunner runner, string folder, GitLocationModel location, int maxFiles)
    {
        var root = location.Root;
        var listed = GitRepository.Require(runner.Run(root, ["ls-files", "-z", "--cached", "--others", "--exclude-standard"]), "Liste des fichiers du dépôt impossible.");
        var deleted = runner.Run(root, ["ls-files", "-z", "--deleted"]);
        var gone = deleted.Succeeded ? Entries(deleted.Output).ToHashSet(StringComparer.Ordinal) : [];
        var staged = runner.Run(root, ["ls-files", "-z", "--stage"]);
        var submodules = staged.Succeeded ? Submodules(staged.Output) : [];
        var files = Entries(listed)
            .Where(path => !gone.Contains(path) && !submodules.Contains(path) && !path.EndsWith(GitSeparator))
            .Distinct(StringComparer.Ordinal)
            .Select(LocalPath)
            .ToList();
        var present = files.ToHashSet(StringComparer.Ordinal);
        var status = new GitRepository(runner, location).Status();
        var changed = status.Staged.Concat(status.Unstaged)
            .Where(change => change.Kind != GitChangeKind.Deleted)
            .Select(change => change.Path)
            .Concat(status.Conflicts.Select(conflict => conflict.Path))
            .Select(LocalPath)
            .Where(present.Contains)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Limited(GitPathMarks.DisplayRootFrom(runner, folder, root), files, changed, maxFiles, false);
    }

    private static ProjectFilesModel FromDisk(string folder, int maxFiles, TimeSpan timeLimit, CancellationToken cancellation)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var files = new List<string>();
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(root)]);
        var clock = Stopwatch.StartNew();
        var stopped = false;
        while (pending.Count > 0 && !stopped)
        {
            try
            {
                foreach (var entry in pending.Pop().EnumerateFileSystemInfos())
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (files.Count > maxFiles || clock.Elapsed >= timeLimit)
                    {
                        stopped = true;
                        break;
                    }

                    if (entry is FileInfo)
                    {
                        files.Add(Path.GetRelativePath(root, entry.FullName));
                    }
                    else if (!SkippedFolders.Contains(entry.Name) && !entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        pending.Push((DirectoryInfo)entry);
                    }
                }
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
            {
            }
        }

        return Limited(root, files, [], maxFiles, stopped || pending.Count > 0);
    }

    private static ProjectFilesModel Limited(string root, List<string> files, IReadOnlyList<string> changed, int maxFiles, bool incomplete)
    {
        files.Sort(StringComparer.OrdinalIgnoreCase);
        return new ProjectFilesModel(root, files.Take(maxFiles).ToList(), changed, incomplete || files.Count > maxFiles);
    }

    private static HashSet<string> Submodules(string output) =>
        Entries(output)
            .Where(entry => entry.StartsWith(SubmoduleMode, StringComparison.Ordinal) && entry.Contains(StageSeparator))
            .Select(entry => entry[(entry.IndexOf(StageSeparator) + 1)..])
            .ToHashSet(StringComparer.Ordinal);

    private static string LocalPath(string path) => path.Replace(GitSeparator, Path.DirectorySeparatorChar);

    private static IEnumerable<string> Entries(string output) =>
        output.Split(EntrySeparator, StringSplitOptions.RemoveEmptyEntries).Where(entry => entry.Trim().Length > 0);
}
