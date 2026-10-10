using Tily.Core.Git;

namespace Tily.Core.Worktrees;

public sealed class WorktreeRemover
{
    private const int LockAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly GitRunner _runner;
    private readonly WorktreeDatabase _database;

    public WorktreeRemover(GitRunner runner, WorktreeDatabase database)
    {
        _runner = runner;
        _database = database;
    }

    public WorktreeRemovalModel Remove(string path, bool keepBranch, bool dropDatabase, bool confirmed, Action<string> progress)
    {
        if (!confirmed)
        {
            throw new WorktreeException("La suppression d’un worktree demande une confirmation.", WorktreeSteps.Verification);
        }

        var target = WorktreeLister.NormalizePath(path);
        var location = Directory.Exists(target) ? GitRepository.Locate(_runner, target) : null;
        if (location is null)
        {
            throw new WorktreeException($"Worktree introuvable : {target}", WorktreeSteps.Verification);
        }

        var worktrees = WorktreeLister.List(new GitRepository(_runner, location));
        var mainRoot = WorktreeTarget.MainRoot(new GitRepository(_runner, location), worktrees);
        if (WorktreeTarget.SamePath(target, mainRoot))
        {
            throw new WorktreeException($"Refus : impossible de supprimer le dépôt principal ({mainRoot}).", WorktreeSteps.Verification);
        }

        var entry = worktrees.FirstOrDefault(worktree => WorktreeTarget.SamePath(worktree.Path, target))
            ?? throw new WorktreeException($"Ce dossier n’est pas un worktree de {WorktreeTarget.Project(mainRoot)} : {target}", WorktreeSteps.Verification);
        var main = GitRepository.Locate(_runner, mainRoot) is { } mainLocation ? new GitRepository(_runner, mainLocation) : new GitRepository(_runner, location);
        var branch = entry.IsDetached ? null : entry.Branch;
        var replicated = dropDatabase ? WorktreeDatabase.Replicated(target, mainRoot) : null;
        var steps = new List<WorktreeStepModel>();

        progress($"Suppression du worktree « {Path.GetFileName(target)} »…");
        if (entry.Locked)
        {
            main.Run("worktree", "unlock", target);
        }

        MoveToTrash(target);
        steps.Add(WorktreeSteps.Ok(WorktreeSteps.Removal, $"Worktree supprimé : {target}."));
        main.Run("worktree", "prune");
        steps.Add(WorktreeLister.List(main).Any(worktree => WorktreeTarget.SamePath(worktree.Path, target))
            ? WorktreeSteps.Warning(WorktreeSteps.Prune, $"Git liste encore le worktree {target} : lancez git worktree prune dans {mainRoot}.")
            : WorktreeSteps.Ok(WorktreeSteps.Prune, "Prune des worktrees terminé."));

        if (replicated is not null)
        {
            progress("Suppression de la base répliquée…");
            steps.Add(replicated.Info is { } info
                ? _database.Drop(info, mainRoot, WorktreeTarget.Project(mainRoot))
                : WorktreeSteps.Ok(WorktreeSteps.Database, replicated.Reason));
        }

        steps.Add(DeleteBranch(main, branch, keepBranch));
        return new WorktreeRemovalModel(target, branch, WorktreeTrash.RootFor(target), steps);
    }

    private static void MoveToTrash(string target)
    {
        for (var attempt = 0; attempt < LockAttempts; attempt++)
        {
            if (attempt > 0)
            {
                Thread.Sleep(RetryDelay);
            }

            if (WorktreeTrash.TryMove(target))
            {
                return;
            }
        }

        throw Locked(target);
    }

    private static WorktreeException Locked(string target) =>
        new($"Le dossier du worktree est verrouillé : {target}. Fermez les programmes qui l’utilisent (shell ouvert dans le dossier, Visual Studio, dotnet, node…) puis réessayez.", WorktreeSteps.Removal);

    private static WorktreeStepModel DeleteBranch(GitRepository main, string? branch, bool keepBranch)
    {
        if (branch is null)
        {
            return WorktreeSteps.Ok(WorktreeSteps.Branch, "HEAD détachée : aucune branche à supprimer.");
        }

        if (keepBranch)
        {
            return WorktreeSteps.Ok(WorktreeSteps.Branch, $"Branche « {branch} » conservée.");
        }

        var deleted = main.Run("branch", "-D", branch);
        return deleted.Succeeded
            ? WorktreeSteps.Ok(WorktreeSteps.Branch, $"Branche « {branch} » supprimée.")
            : WorktreeSteps.Warning(WorktreeSteps.Branch, $"Branche « {branch} » non supprimée : {deleted.Details}");
    }
}
