using Dock.Core.Files;
using Dock.Core.Git;

namespace Dock.Core.Worktrees;

public static class WorktreeSteps
{
    public const string Verification = "Vérification";
    public const string Fetch = "Fetch";
    public const string Creation = "Création du worktree";
    public const string Ports = "Ports";
    public const string Install = "pnpm install";
    public const string Database = "Base de données";
    public const string Removal = "Suppression du worktree";
    public const string Prune = "Prune";
    public const string Branch = "Branche";

    public static WorktreeStepModel Ok(string step, string message) => new(step, WorktreeStepStatus.Ok, message);

    public static WorktreeStepModel Warning(string step, string message) => new(step, WorktreeStepStatus.Warning, message);
}

public static class WorktreeTarget
{
    public static string Slug(string branch) => branch.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? branch;

    public static string MainRoot(GitRepository repository, IReadOnlyList<WorktreeModel> worktrees) =>
        worktrees.FirstOrDefault(worktree => worktree.IsMain)?.Path
        ?? WorktreeLister.NormalizePath(Path.GetDirectoryName(repository.Location.CommonDirectory.TrimEnd(Path.DirectorySeparatorChar)) ?? repository.Root);

    public static string Project(string mainRoot) => Path.GetFileName(mainRoot.TrimEnd(Path.DirectorySeparatorChar));

    public static string PathFor(string folder, string project, string branch)
    {
        string name;
        try
        {
            name = FileExplorer.RequireValidName($"{project}-{Slug(branch)}");
        }
        catch (InvalidOperationException exception)
        {
            throw new WorktreeException($"Nom de dossier impossible pour cette branche : {exception.Message}", WorktreeSteps.Verification);
        }

        return Path.Combine(folder, name);
    }

    public static bool SamePath(string first, string second) =>
        string.Equals(WorktreeLister.NormalizePath(first), WorktreeLister.NormalizePath(second), StringComparison.OrdinalIgnoreCase);

    public static bool IsWithin(string path, string folder)
    {
        var normalizedPath = WorktreeLister.NormalizePath(path);
        var normalizedFolder = WorktreeLister.NormalizePath(folder);
        return string.Equals(normalizedPath, normalizedFolder, StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith(normalizedFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
