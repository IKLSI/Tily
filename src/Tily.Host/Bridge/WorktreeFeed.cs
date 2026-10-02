using Tily.Core.Git;
using Tily.Core.Settings;
using Tily.Core.StatusLog;
using Tily.Core.Worktrees;

namespace Tily.Host.Bridge;

public sealed class WorktreeFeed
{
    private const string CreateOperation = "create";
    private const string RemoveOperation = "remove";

    private readonly Action<object> _post;
    private readonly Func<SettingsModel> _settings;
    private readonly Action _changed;
    private readonly GitRunner _runner = new();
    private readonly WorktreeDatabase _database = new();
    private readonly WorktreeProjectsRepository _projects;
    private readonly BackgroundQueue _plans;
    private readonly BackgroundQueue _operations;

    public WorktreeFeed(Action<object> post, Func<SettingsModel> settings, Action changed, Action<Exception> onError, string dataDirectory)
    {
        _post = post;
        _projects = new WorktreeProjectsRepository(dataDirectory);
        _settings = settings;
        _changed = changed;
        _plans = new BackgroundQueue(onError);
        _operations = new BackgroundQueue(onError);
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "worktrees.sources":
                _plans.Enqueue(() => Sources(command));
                break;
            case "projects.repositories":
                _plans.Enqueue(() => ProjectRepositories(command));
                break;
            case "projects.rememberRepository":
                _plans.Enqueue(() => RememberRepository(command));
                break;
            case "worktrees.plan":
                _plans.Enqueue(() => Plan(command));
                break;
            case "worktrees.create":
                _operations.Enqueue(() => Create(command));
                break;
            case "worktrees.remove":
                _operations.Enqueue(() => Remove(command));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void Sources(BridgeCommandModel command)
    {
        var settings = _settings();
        var path = command.Project ?? command.Path ?? throw new InvalidOperationException("Dossier manquant.");
        var sources = WorktreeSources.Resolve(_runner, path, command.Project is not null, settings.ProjectsRoot, settings.Worktrees.FolderFor(settings.ProjectsRoot), _projects.RepositoryOf);
        _post(new { type = "worktrees.sourcesFound", request = command.Request, sources });
    }

    private void ProjectRepositories(BridgeCommandModel command)
    {
        var settings = _settings();
        var project = command.Project ?? throw new InvalidOperationException("Projet manquant.");
        var sources = WorktreeSources.Resolve(_runner, project, true, settings.ProjectsRoot, settings.Worktrees.FolderFor(settings.ProjectsRoot), _projects.RepositoryOf);
        _post(new { type = "projects.repositoriesFound", request = command.Request, sources });
    }

    private void RememberRepository(BridgeCommandModel command)
    {
        var project = command.Project ?? throw new InvalidOperationException("Projet manquant.");
        var repository = command.Repository ?? throw new InvalidOperationException("Dépôt manquant.");
        if (GitRepository.Locate(_runner, repository) is null)
        {
            throw new InvalidOperationException($"Aucun dépôt Git dans {repository} : dépôt par défaut non enregistré.");
        }

        _projects.SaveRepository(project, repository);
        _post(new { type = "projects.repositoryRemembered", project = WorktreeLister.NormalizePath(project), repository = WorktreeLister.NormalizePath(repository) });
    }

    private void Plan(BridgeCommandModel command)
    {
        var settings = _settings();
        var plan = WorktreeCreator.Plan(_runner, RequestOf(command), settings.Worktrees, settings.ProjectsRoot);
        _post(new { type = "worktrees.planned", request = command.Request, plan });
    }

    private void Create(BridgeCommandModel command)
    {
        var settings = _settings();
        Run(CreateOperation, () =>
        {
            var creation = WorktreeCreator.Create(_runner, RequestOf(command), settings.Worktrees, settings.ProjectsRoot, message => Progress(CreateOperation, message));
            Progress(CreateOperation, "Randomisation des ports…");
            var ports = Ports(creation.Path);
            var install = WorktreeCreator.InstallCommandFor(creation.Path, command.Install);
            _post(new { type = "worktrees.created", path = creation.Path, name = creation.Name, branch = creation.Branch, install });
            _changed();
            List<WorktreeStepModel> steps = [.. creation.Steps, ports];
            if (command.Remember)
            {
                steps.Add(Remember(command.Project, creation.MainRoot));
            }

            if (command.Database)
            {
                Progress(CreateOperation, "Réplication de la base…");
                steps.Add(_database.Replicate(creation.Path, creation.Branch, creation.Project));
            }

            var summary = steps.Where(step => step.Status == WorktreeStepStatus.Ok && step.Step is WorktreeSteps.Ports or WorktreeSteps.Database).Select(step => step.Message);
            return (string.Join(' ', new[] { $"Worktree « {creation.Name} » créé." }.Concat(summary)), steps);
        });
    }

    private void Remove(BridgeCommandModel command) =>
        Run(RemoveOperation, () =>
        {
            var removal = new WorktreeRemover(_runner, _database).Remove(command.Path ?? string.Empty, command.KeepBranch, command.DropDatabase, command.Confirmed, message => Progress(RemoveOperation, message));
            _changed();
            return ($"Worktree « {Path.GetFileName(removal.Path)} » supprimé.", removal.Steps);
        });

    private void Run(string operation, Func<(string Message, IReadOnlyList<WorktreeStepModel> Steps)> action)
    {
        try
        {
            var (message, steps) = action();
            var warnings = steps.Where(step => step.Status != WorktreeStepStatus.Ok).Select(step => step.Message).ToList();
            _post(new { type = "worktrees.done", operation, message, warnings });
        }
        catch (WorktreeException failure)
        {
            _post(new
            {
                type = "worktrees.failed",
                operation,
                step = failure.Step,
                message = failure.Message,
                output = failure.Output.Length > 0 ? failure.Output : null,
                lockedBy = failure.LockedBy.Count > 0 ? failure.LockedBy : null
            });
        }
        catch (Exception exception) when (exception is GitCommandException or IOException or UnauthorizedAccessException)
        {
            _post(new { type = "worktrees.failed", operation, step = WorktreeSteps.Verification, message = UserErrorMessage.Of(exception), output = (exception as GitCommandException)?.Output is { Length: > 0 } output ? output : null });
        }
    }

    private static WorktreeStepModel Ports(string path)
    {
        try
        {
            var result = new PortRandomizer().Randomize(path);
            var unavailable = result.Unavailable.Count > 0 ? $" Aucun port libre pour : {string.Join(", ", result.Unavailable)}." : string.Empty;
            return result.Unavailable.Count > 0
                ? WorktreeSteps.Warning(WorktreeSteps.Ports, PortRandomizer.Describe(result) + unavailable)
                : WorktreeSteps.Ok(WorktreeSteps.Ports, PortRandomizer.Describe(result));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return WorktreeSteps.Warning(WorktreeSteps.Ports, $"Randomisation des ports ignorée : {exception.Message}");
        }
    }

    private WorktreeStepModel Remember(string? project, string repository)
    {
        try
        {
            _projects.SaveRepository(project ?? string.Empty, repository);
            return WorktreeSteps.Ok(WorktreeSteps.Repository, $"Dépôt par défaut du projet : {repository}");
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return WorktreeSteps.Warning(WorktreeSteps.Repository, $"Dépôt par défaut non enregistré : {exception.Message}");
        }
    }

    private void Progress(string operation, string message) => _post(new { type = "worktrees.progress", operation, message });

    private static WorktreeRequestModel RequestOf(BridgeCommandModel command) =>
        new(command.Repository ?? string.Empty, command.Branch ?? string.Empty, ModeOf(command.Mode), command.Base);

    private static WorktreeBranchMode ModeOf(string? mode) => mode switch
    {
        "local" => WorktreeBranchMode.Local,
        "remote" => WorktreeBranchMode.Remote,
        _ => WorktreeBranchMode.New
    };
}
