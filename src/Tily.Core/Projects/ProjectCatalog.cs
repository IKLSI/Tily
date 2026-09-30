namespace Tily.Core.Projects;

public sealed record ProjectModel(string Name, string Path, bool Worktree = false);

public sealed record ProjectListModel(string Root, IReadOnlyList<ProjectModel> Projects, string? Error);

public static class ProjectCatalog
{
    public const string DefaultRoot = @"C:\Files\Projects";
    public const string ExcludedFolder = "worktrees";

    public static ProjectListModel List(string root, string? worktreeFolder = null)
    {
        var worktrees = worktreeFolder ?? Path.Combine(root, ExcludedFolder);
        if (!Directory.Exists(root))
        {
            return new ProjectListModel(root, [], $"Le dossier des projets est introuvable : {root}");
        }

        try
        {
            var projects = FoldersOf(root)
                .Where(directory => !string.Equals(directory.Name, ExcludedFolder, StringComparison.OrdinalIgnoreCase) && !SamePath(directory.FullName, worktrees))
                .Select(directory => new ProjectModel(directory.Name, directory.FullName))
                .ToList();
            return new ProjectListModel(root, [.. projects, .. WorktreesOf(worktrees)], null);
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

    private static IReadOnlyList<ProjectModel> WorktreesOf(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        try
        {
            return FoldersOf(folder).Select(directory => new ProjectModel(directory.Name, directory.FullName, true)).ToList();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }
}
