namespace Dock.Core.Git;

public static class GitStateReader
{
    public static GitStateModel Read(GitRepository repository, GitUndoRecordModel? undo, Func<string, bool> forcePushAllowed)
    {
        var status = repository.Status();
        var operation = repository.Operation();
        var refs = GitRefsReader.Read(repository, status.Head);
        var stashes = GitRefsReader.ReadStashes(repository);
        var lastMessage = status.Head.Unborn ? null : repository.Read("log", "-1", "--no-show-signature", "--format=%B", "HEAD").TrimEnd();
        var name = Path.GetFileName(repository.Root.TrimEnd(Path.DirectorySeparatorChar));
        return new GitStateModel(
            repository.Root,
            name.Length > 0 ? name : repository.Root,
            status.Head,
            operation,
            status.Staged,
            status.Unstaged,
            status.Conflicts,
            status.StagedTotal,
            status.UnstagedTotal,
            refs.Branches,
            refs.RemoteBranches,
            refs.Tags,
            stashes,
            refs.Remotes,
            lastMessage,
            GitUndo.Describe(repository, undo, status.Head, operation),
            status.Head.Branch is { } branch && forcePushAllowed(branch));
    }

    public static string Signature(GitStateModel state) =>
        string.Join('\n', new[] { state.Head.Sha ?? string.Empty, state.Head.Branch ?? string.Empty, state.Head.Upstream ?? string.Empty }
            .Concat(state.Branches.Select(branch => $"{branch.Name} {branch.Sha}"))
            .Concat(state.RemoteBranches.Select(branch => $"{branch.Name} {branch.Sha}"))
            .Concat(state.Tags.Select(tag => $"{tag.Name} {tag.Sha}"))
            .Concat(state.Stashes.Select(stash => $"stash {stash.Sha}")));
}
