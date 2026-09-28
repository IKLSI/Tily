namespace Dock.Core.Git;

public static class GitStashTagCommands
{
    public static GitOutcomeModel CreateTag(GitRepository repository, string? name, string? commit)
    {
        var tag = GitNames.RequireTagName(repository, name);
        if (repository.RefValue($"refs/tags/{tag}") is not null)
        {
            throw new GitCommandException($"Le tag « {tag} » existe déjà.", string.Empty);
        }

        var sha = repository.RequireCommit(string.IsNullOrWhiteSpace(commit) ? "HEAD" : commit);
        GitRepository.Require(repository.Run("tag", tag, sha), $"La création du tag « {tag} » a échoué.");
        return new GitOutcomeModel($"Tag « {tag} » créé sur {GitRepository.Short(sha)}.", Undo: new GitUndoRecordModel { Kind = GitUndoKind.TagCreate, Label = $"Création du tag « {tag} »", RefName = tag, RefTarget = sha });
    }

    public static GitOutcomeModel DeleteTag(GitRepository repository, string? name)
    {
        var tag = GitNames.RequireTagName(repository, name);
        var target = repository.RefValue($"refs/tags/{tag}") ?? throw new GitCommandException($"Le tag « {tag} » n’existe plus.", string.Empty);
        GitRepository.Require(repository.Run("tag", "-d", tag), $"La suppression du tag « {tag} » a échoué.");
        return new GitOutcomeModel($"Tag « {tag} » supprimé.", Undo: new GitUndoRecordModel { Kind = GitUndoKind.TagDelete, Label = $"Suppression du tag « {tag} »", RefName = tag, RefTarget = target });
    }

    public static GitOutcomeModel Stash(GitRepository repository, string? message)
    {
        repository.RequireNoOperation();
        var status = repository.Status();
        if (status.StagedTotal + status.UnstagedTotal == 0)
        {
            throw new GitCommandException("Aucune modification à stash.", string.Empty);
        }

        var text = message?.Trim() ?? string.Empty;
        var arguments = new List<string> { "stash", "push", "--include-untracked" };
        if (text.Length > 0)
        {
            arguments.AddRange(["-m", text]);
        }

        GitRepository.Require(repository.Run([.. arguments]), "Le stash a échoué.");
        var sha = repository.RefValue("refs/stash");
        var record = sha is null ? null : new GitUndoRecordModel { Kind = GitUndoKind.StashPush, Label = "Stash des modifications", Backup = sha };
        return new GitOutcomeModel(text.Length > 0 ? $"Stash créé : « {text} »." : "Stash créé.", Undo: record, ClearUndo: record is null);
    }

    public static GitOutcomeModel ApplyStash(GitRepository repository, int index, string? sha, bool pop)
    {
        var stash = RequireStash(repository, index, sha);
        var output = repository.Run("stash", pop ? "pop" : "apply", $"stash@{{{index}}}");
        if (!output.Succeeded)
        {
            if (repository.Status().Conflicts.Count == 0)
            {
                throw new GitCommandException("L’application du stash a échoué.", output.Details);
            }

            return new GitOutcomeModel(pop ? "Stash appliqué avec des conflits : il est conservé, résolvez puis supprimez-le." : "Stash appliqué avec des conflits à résoudre.", true, ClearUndo: true);
        }

        return new GitOutcomeModel(pop ? $"Stash appliqué et supprimé : « {stash.Message} »." : $"Stash appliqué : « {stash.Message} ».", ClearUndo: true);
    }

    public static GitOutcomeModel DropStash(GitRepository repository, int index, string? sha, bool confirmed)
    {
        GitPaths.RequireConfirmation(confirmed);
        var stash = RequireStash(repository, index, sha);
        GitRepository.Require(repository.Run("stash", "drop", "-q", $"stash@{{{index}}}"), "La suppression du stash a échoué.");
        var record = new GitUndoRecordModel { Kind = GitUndoKind.StashDrop, Label = $"Suppression du stash « {stash.Message} »", Backup = stash.Sha, StashMessage = stash.Message };
        return new GitOutcomeModel($"Stash supprimé : « {stash.Message} ».", Undo: record);
    }

    private static GitStashModel RequireStash(GitRepository repository, int index, string? sha)
    {
        var stash = GitRefsReader.ReadStashes(repository).FirstOrDefault(candidate => candidate.Index == index);
        if (stash is null || (sha is not null && stash.Sha != sha))
        {
            throw new GitCommandException("La liste des stashes a changé : actualisez puis réessayez.", string.Empty);
        }

        return stash;
    }
}
