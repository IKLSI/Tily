namespace Dock.Core.Projects;

public sealed record ProjectModel(string Name, string Path, bool Worktree = false);

public sealed record ProjectListModel(string Root, IReadOnlyList<ProjectModel> Projects, string? Error);

public static class ProjectCatalog
{
    public const string DefaultRoot = @"C:\Files\Projects";
    public const string ExcludedFolder = "worktrees";

    public static ProjectListModel List(string root)
    {
        if (!Directory.Exists(root))
        {
            return new ProjectListModel(root, [], $"Le dossier des projets est introuvable : {root}");
        }

        try
        {
            var projects = FoldersOf(root)
                .Where(directory => !string.Equals(directory.Name, ExcludedFolder, StringComparison.OrdinalIgnoreCase))
                .Select(directory => new ProjectModel(directory.Name, directory.FullName))
                .ToList();
            return new ProjectListModel(root, [.. projects, .. WorktreesOf(root)], null);
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

    private static IReadOnlyList<ProjectModel> WorktreesOf(string root)
    {
        var folder = Path.Combine(root, ExcludedFolder);
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
