using System.Collections.Concurrent;
using Dock.Core.Git;

namespace Dock.Host.Bridge;

public sealed class GitOperationRunner
{
    private readonly Func<string?, GitRepository> _open;
    private readonly Action<object> _post;
    private readonly Action _completed;
    private readonly BackgroundQueue _queue;
    private readonly ConcurrentDictionary<string, byte> _forceAllowed = new(StringComparer.OrdinalIgnoreCase);

    public GitOperationRunner(Func<string?, GitRepository> open, Action<object> post, Action completed, Action<Exception> onError)
    {
        _open = open;
        _post = post;
        _completed = completed;
        _queue = new BackgroundQueue(onError);
    }

    public GitUndoJournal Journal { get; } = new();

    public bool IsForceAllowed(string root, string branch) => _forceAllowed.ContainsKey(ForceKey(root, branch));

    public bool Handle(BridgeCommandModel command)
    {
        var files = command.Files ?? [];
        Func<GitRepository, GitOutcomeModel>? action = command.Type switch
        {
            "git.stage" => repository => GitChangeCommands.Stage(repository, files),
            "git.unstage" => repository => GitChangeCommands.Unstage(repository, files),
            "git.discard" => repository => GitChangeCommands.Discard(repository, files, command.Confirmed),
            "git.commit" => repository => CommitThenPush(repository, command),
            "git.push" => repository => Push(repository, command.Force, command.Confirmed),
            "git.pull" => GitSyncCommands.Pull,
            "git.fetch" => GitSyncCommands.Fetch,
            "git.merge" => repository => GitHistoryCommands.Merge(repository, command.Reference),
            "git.rebase" => repository => GitHistoryCommands.Rebase(repository, command.Reference),
            "git.cherryPick" => repository => GitHistoryCommands.CherryPick(repository, command.Commit),
            "git.reset" => repository => GitHistoryCommands.Reset(repository, command.Commit, command.Mode, command.Confirmed),
            "git.switch" => repository => GitBranchCommands.Switch(repository, command.Reference, command.Target),
            "git.branchCreate" => repository => GitBranchCommands.Create(repository, command.Name, command.Reference, command.Checkout),
            "git.branchRename" => repository => GitBranchCommands.Rename(repository, command.Name, command.NewName),
            "git.branchDelete" => repository => GitBranchCommands.Delete(repository, command.Name, command.Force, command.Confirmed),
            "git.remoteBranchDelete" => repository => GitSyncCommands.DeleteRemoteBranch(repository, command.Reference, command.Confirmed),
            "git.tagCreate" => repository => GitStashTagCommands.CreateTag(repository, command.Name, command.Commit),
            "git.tagDelete" => repository => GitStashTagCommands.DeleteTag(repository, command.Name),
            "git.tagPush" => repository => GitSyncCommands.PushTag(repository, command.Name),
            "git.stash" => repository => GitStashTagCommands.Stash(repository, command.Message),
            "git.stashApply" => repository => GitStashTagCommands.ApplyStash(repository, command.Index, command.Commit, command.Pop),
            "git.stashDrop" => repository => GitStashTagCommands.DropStash(repository, command.Index, command.Commit, command.Confirmed),
            "git.resolve" => repository => GitChangeCommands.Resolve(repository, files, command.Confirmed),
            "git.continue" => repository => GitChangeCommands.Continue(repository, Journal.Get(repository.Root)),
            "git.abort" => repository => GitChangeCommands.Abort(repository, command.Confirmed),
            "git.undo" => repository => GitUndo.Apply(repository, Journal.Get(repository.Root)),
            _ => null
        };
        if (action is null)
        {
            return false;
        }

        _queue.Enqueue(() => Run(command, action));
        return true;
    }

    private void Run(BridgeCommandModel command, Func<GitRepository, GitOutcomeModel> action)
    {
        GitRepository? repository = null;
        try
        {
            repository = _open(command.Path);
            var outcome = action(repository);
            Journal.Apply(repository.Root, outcome);
            _post(new { type = "git.done", operation = command.Type, message = outcome.Message, warning = outcome.Warning });
        }
        catch (GitPushRejectedException rejected)
        {
            _forceAllowed[ForceKey(repository!.Root, rejected.Branch)] = 0;
            _post(new { type = "git.pushRejected", operation = command.Type, branch = rejected.Branch, message = rejected.Message, output = rejected.Output });
        }
        catch (GitCommandException failure)
        {
            _post(new { type = "git.failed", operation = command.Type, message = failure.Message, output = failure.Output.Length > 0 ? failure.Output : null, code = failure.Code == GitFailureCode.None ? (GitFailureCode?)null : failure.Code });
        }
        catch (Exception exception)
        {
            _post(new { type = "git.failed", operation = command.Type, message = exception.Message });
        }
        finally
        {
            _completed();
        }
    }

    private GitOutcomeModel CommitThenPush(GitRepository repository, BridgeCommandModel command)
    {
        var committed = GitChangeCommands.Commit(repository, command.Message, command.Amend);
        if (!command.Push)
        {
            return committed;
        }

        Journal.Apply(repository.Root, committed);
        return new GitOutcomeModel($"{committed.Message} {Push(repository, false, false).Message}");
    }

    private GitOutcomeModel Push(GitRepository repository, bool force, bool confirmed)
    {
        var key = ForceKey(repository.Root, repository.CurrentBranch() ?? string.Empty);
        var outcome = GitSyncCommands.Push(repository, force, confirmed, _forceAllowed.ContainsKey(key));
        _forceAllowed.TryRemove(key, out _);
        return outcome;
    }

    private static string ForceKey(string root, string branch) => $"{root}\n{branch}";
}
