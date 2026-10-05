using Tily.Core.Git;

namespace Tily.Core.Worktrees;

public sealed record WorktreeSourcesModel(string Project, IReadOnlyList<string> Repositories, string? Selected, string? DefaultRepository);

public static class WorktreeSources
{
    public static WorktreeSourcesModel Resolve(GitRunner runner, string path, bool fromProject, string projectsRoot, string worktreeFolder, Func<string, string?> remembered)
    {
        var mainRoot = fromProject ? null : MainRootOf(runner, path);
        var project = fromProject ? WorktreeLister.NormalizePath(path) : ProjectOf(mainRoot ?? path, projectsRoot, worktreeFolder);
        var stored = remembered(project) is { } repository && Directory.Exists(repository) ? WorktreeLister.NormalizePath(repository) : null;
        var found = RepositoryFinder.Find(project);
        var defaultRepository = stored ?? found.FirstOrDefault();
        var selected = mainRoot ?? defaultRepository;
        var repositories = found
            .Concat(new[] { stored, selected }.OfType<string>())
            .DistinctBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new WorktreeSourcesModel(project, repositories, selected, defaultRepository);
    }

    public static string ProjectOf(string folder, string projectsRoot, string worktreeFolder)
    {
        var normalized = WorktreeLister.NormalizePath(folder);
        if (!WorktreeTarget.IsWithin(normalized, projectsRoot) || WorktreeTarget.SamePath(normalized, projectsRoot) || WorktreeTarget.IsWithin(normalized, worktreeFolder))
        {
            return normalized;
        }

        var first = Path.GetRelativePath(WorktreeLister.NormalizePath(projectsRoot), normalized).Split(Path.DirectorySeparatorChar)[0];
        return Path.Combine(WorktreeLister.NormalizePath(projectsRoot), first);
    }

    private static string? MainRootOf(GitRunner runner, string path)
    {
        var location = GitRepository.Locate(runner, path);
        if (location is null)
        {
            return null;
        }

        var repository = new GitRepository(runner, location);
        return WorktreeTarget.MainRoot(repository, WorktreeLister.TryList(repository));
    }
}
