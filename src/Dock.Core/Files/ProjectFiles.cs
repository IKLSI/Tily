using Dock.Core.Git;

namespace Dock.Core.Files;

public sealed record ProjectFilesModel(string Root, IReadOnlyList<string> Files, IReadOnlyList<string> Changed, bool Truncated);

public static class ProjectFiles
{
    public const int MaxFiles = 20000;

    private const char GitSeparator = '/';
    private const char EntrySeparator = '\0';

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { ".git", ".vs", "node_modules", "bin", "obj" };

    public static ProjectFilesModel List(GitRunner runner, string folder, int maxFiles = MaxFiles)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            throw new InvalidOperationException($"Dossier introuvable : {folder}");
        }

        var location = GitRunner.IsInstalled ? GitRepository.Locate(runner, folder) : null;
        return location is null ? FromDisk(folder, maxFiles) : FromGit(runner, folder, location, maxFiles);
    }

    private static ProjectFilesModel FromGit(GitRunner runner, string folder, GitLocationModel location, int maxFiles)
    {
        var root = location.Root;
        var listed = GitRepository.Require(runner.Run(root, ["ls-files", "-z", "--cached", "--others", "--exclude-standard"]), "Liste des fichiers du dépôt impossible.");
        var deleted = runner.Run(root, ["ls-files", "-z", "--deleted"]);
        var gone = deleted.Succeeded ? Entries(deleted.Output).ToHashSet(StringComparer.Ordinal) : [];
        var files = Entries(listed)
            .Where(path => !gone.Contains(path))
            .Distinct(StringComparer.Ordinal)
            .Select(WindowsPath)
            .ToList();
        var status = new GitRepository(runner, location).Status();
        var changed = status.Staged.Concat(status.Unstaged)
            .Where(change => change.Kind != GitChangeKind.Deleted)
            .Select(change => change.Path)
            .Concat(status.Conflicts.Select(conflict => conflict.Path))
            .Select(WindowsPath)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Limited(GitPathMarks.DisplayRootFrom(runner, folder, root), files, changed, maxFiles);
    }

    private static ProjectFilesModel FromDisk(string folder, int maxFiles)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var files = new List<string>();
        var pending = new Stack<DirectoryInfo>([new DirectoryInfo(root)]);
        while (pending.Count > 0 && files.Count <= maxFiles)
        {
            try
            {
                foreach (var entry in pending.Pop().EnumerateFileSystemInfos())
                {
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

        return Limited(root, files, [], maxFiles);
    }

    private static ProjectFilesModel Limited(string root, List<string> files, IReadOnlyList<string> changed, int maxFiles)
    {
        files.Sort(StringComparer.OrdinalIgnoreCase);
        return new ProjectFilesModel(root, files.Take(maxFiles).ToList(), changed, files.Count > maxFiles);
    }

    private static string WindowsPath(string path) => path.Replace(GitSeparator, Path.DirectorySeparatorChar);

    private static IEnumerable<string> Entries(string output) =>
        output.Split(EntrySeparator, StringSplitOptions.RemoveEmptyEntries).Where(entry => entry.Trim().Length > 0);
}
