namespace Dock.Core.Git;

public sealed record GitRefDeletionModel(IReadOnlyList<string> Branches, IReadOnlyList<string> RemoteBranches, IReadOnlyList<string> Tags, IReadOnlyList<string> Stashes);

public static class GitRefDeleteCommands
{
    private const string HeadsPrefix = "refs/heads/";

    public static GitOutcomeModel Delete(GitRepository repository, GitRefDeletionModel deletion, bool confirmed)
    {
        GitPaths.RequireConfirmation(confirmed);
        var branches = deletion.Branches.Select(name => GitNames.RequireBranchName(repository, name)).Distinct(StringComparer.Ordinal).ToList();
        var tags = deletion.Tags.Select(name => GitNames.RequireTagName(repository, name)).Distinct(StringComparer.Ordinal).ToList();
        var remotes = RemoteTargets(repository, deletion.RemoteBranches);
        var stashes = RequireStashes(repository, deletion.Stashes);
        if (branches.Count + tags.Count + remotes.Count + stashes.Count == 0)
        {
            throw new GitCommandException("Aucune référence à supprimer.", string.Empty);
        }

        var current = repository.CurrentBranch();
        if (current is not null && branches.Contains(current))
        {
            throw new GitCommandException($"Impossible de supprimer la branche courante « {current} » : faites d’abord le checkout d’une autre branche.", string.Empty);
        }

        var steps = branches.Select(branch => BranchStep(repository, branch))
            .Concat(tags.Select(tag => TagStep(repository, tag)))
            .Concat(stashes.Select(stash => new GitUndoRecordModel { Kind = GitUndoKind.StashDrop, Label = stash.Message, Backup = stash.Sha, StashMessage = stash.Message }))
            .ToList();

        foreach (var group in remotes.GroupBy(target => target.Remote, StringComparer.Ordinal))
        {
            GitRepository.Require(repository.Run(["push", "--porcelain", group.Key, "--delete", .. group.Select(target => target.Reference)]), $"La suppression des branches distantes de {group.Key} a échoué.");
        }

        var failure = remotes.Count > 0 ? "Les branches distantes sont supprimées, mais la suppression locale a échoué." : "La suppression des références a échoué.";
        if (branches.Count > 0)
        {
            GitRepository.Require(repository.Run(["branch", "-D", .. branches]), failure);
        }

        if (tags.Count > 0)
        {
            GitRepository.Require(repository.Run(["tag", "-d", .. tags]), failure);
        }

        foreach (var stash in stashes.OrderByDescending(stash => stash.Index))
        {
            GitRepository.Require(repository.Run("stash", "drop", "-q", $"stash@{{{stash.Index}}}"), failure);
        }

        var summary = Summary(branches.Count, remotes.Count, tags.Count, stashes.Count);
        var restorable = Summary(branches.Count, 0, tags.Count, stashes.Count);
        var record = steps.Count == 0 ? null : new GitUndoRecordModel { Kind = GitUndoKind.RefsDelete, Label = $"Suppression de {restorable}", Steps = steps };
        return new GitOutcomeModel($"Références supprimées : {summary}.", Undo: record, ClearUndo: record is null);
    }

    private static List<(string Remote, string Reference)> RemoteTargets(GitRepository repository, IReadOnlyList<string> names)
    {
        var known = repository.Remotes();
        return names.Select(GitNames.RequireRevision).Distinct(StringComparer.Ordinal).Select(name =>
        {
            var remote = GitRefsReader.RemoteOf(known, name) ?? throw new GitCommandException($"Branche distante inconnue : « {name} ».", string.Empty);
            return (remote, $"{HeadsPrefix}{name[(remote.Length + 1)..]}");
        }).ToList();
    }

    private static List<GitStashModel> RequireStashes(GitRepository repository, IReadOnlyList<string> shas)
    {
        if (shas.Count == 0)
        {
            return [];
        }

        var stashes = GitRefsReader.ReadStashes(repository);
        return shas.Distinct(StringComparer.Ordinal).Select(sha => stashes.FirstOrDefault(stash => stash.Sha == sha) ?? throw new GitCommandException("La liste des stashes a changé : actualisez puis réessayez.", string.Empty))
            .OrderBy(stash => stash.Index)
            .ToList();
    }

    private static GitUndoRecordModel BranchStep(GitRepository repository, string branch)
    {
        var target = repository.Resolve($"{HeadsPrefix}{branch}") ?? throw new GitCommandException($"La branche « {branch} » n’existe plus.", string.Empty);
        var upstream = GitRepository.ValueOf(repository.Run("rev-parse", "--abbrev-ref", $"{branch}@{{upstream}}"));
        return new GitUndoRecordModel { Kind = GitUndoKind.BranchDelete, Label = branch, RefName = branch, RefTarget = target, Upstream = upstream };
    }

    private static GitUndoRecordModel TagStep(GitRepository repository, string tag)
    {
        var target = repository.RefValue($"refs/tags/{tag}") ?? throw new GitCommandException($"Le tag « {tag} » n’existe plus.", string.Empty);
        return new GitUndoRecordModel { Kind = GitUndoKind.TagDelete, Label = tag, RefName = tag, RefTarget = target };
    }

    private static string Summary(int branches, int remotes, int tags, int stashes)
    {
        var parts = new[] { Count(branches, "branche", "branches"), Count(remotes, "branche distante", "branches distantes"), Count(tags, "tag", "tags"), Count(stashes, "stash", "stash") }
            .Where(part => part is not null)
            .Cast<string>()
            .ToList();
        return parts.Count > 1 ? $"{string.Join(", ", parts[..^1])} et {parts[^1]}" : string.Join(string.Empty, parts);
    }

    private static string? Count(int count, string one, string several) => count == 0 ? null : $"{count} {(count > 1 ? several : one)}";
}
