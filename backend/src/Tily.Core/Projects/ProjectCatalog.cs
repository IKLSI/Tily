using Tily.Core.Worktrees;

namespace Tily.Core.Projects;

public sealed record ProjectModel(string Name, string Path, bool Worktree = false);

public sealed record ProjectListModel(string Root, IReadOnlyList<ProjectModel> Projects, string? Error);

public static class ProjectCatalog
{
    public static readonly string DefaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Developer");
    public const string ExcludedFolder = "worktrees";

    private const string GitEntry = ".git";

    public static ProjectListModel List(string root, string? worktreeFolder = null, IReadOnlyList<WorktreeProjectFolderModel>? projectFolders = null)
    {
        var worktrees = worktreeFolder ?? Path.Combine(root, ExcludedFolder);
        var dedicated = (projectFolders ?? []).Where(entry => !SamePath(entry.Folder, entry.Project)).Select(entry => entry.Folder).ToList();
        if (!Directory.Exists(root))
        {
            return new ProjectListModel(root, [], $"Le dossier des projets est introuvable : {root}");
        }

        try
        {
            var projects = FoldersOf(root)
                .Where(directory => !string.Equals(directory.Name, ExcludedFolder, StringComparison.OrdinalIgnoreCase) && !SamePath(directory.FullName, worktrees) && !dedicated.Any(folder => SamePath(directory.FullName, folder)))
                .Select(directory => new ProjectModel(directory.Name, directory.FullName))
                .ToList();
            var projectWorktrees = (projectFolders ?? [])
                .Select(entry => entry.Folder)
                .Where(folder => !SamePath(folder, worktrees))
                .SelectMany(folder => WorktreesOf(folder, IsLinkedWorktree));
            var listed = WorktreesOf(worktrees, _ => true)
                .Concat(projectWorktrees)
                .DistinctBy(project => Path.TrimEndingDirectorySeparator(project.Path), StringComparer.OrdinalIgnoreCase);
            return new ProjectListModel(root, [.. projects, .. listed], null);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return new ProjectListModel(root, [], $"Le dossier des projets est inaccessible : {exception.Message}");
        }
    }

    private static IEnumerable<DirectoryInfo> FoldersOf(string path) =>
        new DirectoryInfo(path)
            .EnumerateDirectories()
            .Where(directory => !directory.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(directory => directory.Name, StringComparer.CurrentCultureIgnoreCase);

    private static bool SamePath(string first, string second) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)), StringComparison.OrdinalIgnoreCase);

    private static bool IsLinkedWorktree(DirectoryInfo directory) => File.Exists(Path.Combine(directory.FullName, GitEntry));

    private static IReadOnlyList<ProjectModel> WorktreesOf(string folder, Func<DirectoryInfo, bool> accepted)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        try
        {
            return FoldersOf(folder).Where(accepted).Select(directory => new ProjectModel(directory.Name, directory.FullName, true)).ToList();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }
}
