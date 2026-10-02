using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Worktrees;

public sealed record WorktreeProjectModel(string? Repository);

public sealed record WorktreeProjectsModel(Dictionary<string, WorktreeProjectModel>? Projects);

public sealed class WorktreeProjectsRepository
{
    public const string FileName = "worktree-projects.json";

    private readonly string _filePath;
    private readonly Lock _sync = new();

    public WorktreeProjectsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public string? RepositoryOf(string project)
    {
        lock (_sync)
        {
            return Load().TryGetValue(WorktreeLister.NormalizePath(project), out var settings) ? settings.Repository : null;
        }
    }

    public void SaveRepository(string project, string repository)
    {
        if (!Path.IsPathRooted(project) || !Path.IsPathRooted(repository))
        {
            throw new InvalidOperationException($"Le dossier du projet et celui du dépôt doivent être des chemins absolus : {project}, {repository}");
        }

        lock (_sync)
        {
            var projects = Load();
            var key = WorktreeLister.NormalizePath(project);
            projects[key] = (projects.GetValueOrDefault(key) ?? new WorktreeProjectModel(null)) with { Repository = WorktreeLister.NormalizePath(repository) };
            AtomicFile.Write(_filePath, JsonSerializer.Serialize(new WorktreeProjectsModel(projects), SessionRepository.JsonOptions));
        }
    }

    private Dictionary<string, WorktreeProjectModel> Load()
    {
        var projects = new Dictionary<string, WorktreeProjectModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var (project, settings) in Stored() ?? [])
        {
            if (Path.IsPathRooted(project) && settings is not null)
            {
                projects[WorktreeLister.NormalizePath(project)] = settings;
            }
        }

        return projects;
    }

    private Dictionary<string, WorktreeProjectModel>? Stored()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WorktreeProjectsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions)?.Projects;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
