using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Worktrees;

public sealed record WorktreeProjectModel(string? Repository, string? Folder = null);

public sealed record WorktreeProjectFolderModel(string Project, string Folder);

public sealed record WorktreeProjectsModel(Dictionary<string, WorktreeProjectModel>? Projects);

public sealed class WorktreeProjectsRepository
{
    public const string FileName = "worktree-projects.json";

    private readonly string _filePath;
    private static readonly Lock Sync = new();

    public WorktreeProjectsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public string? RepositoryOf(string project)
    {
        lock (Sync)
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

        lock (Sync)
        {
            var projects = Load();
            var key = WorktreeLister.NormalizePath(project);
            projects[key] = (projects.GetValueOrDefault(key) ?? new WorktreeProjectModel(null)) with { Repository = WorktreeLister.NormalizePath(repository) };
            AtomicFile.Write(_filePath, JsonSerializer.Serialize(new WorktreeProjectsModel(projects), SessionRepository.JsonOptions));
        }
    }

    public List<WorktreeProjectFolderModel> Folders()
    {
        lock (Sync)
        {
            return Load()
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value.Folder))
                .Select(pair => new WorktreeProjectFolderModel(pair.Key, pair.Value.Folder!))
                .OrderBy(entry => entry.Project, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public void SaveFolders(IReadOnlyList<WorktreeProjectFolderModel> folders)
    {
        lock (Sync)
        {
            var projects = Load();
            foreach (var key in projects.Keys.ToList())
            {
                projects[key] = projects[key] with { Folder = null };
            }

            foreach (var entry in folders)
            {
                var key = WorktreeLister.NormalizePath(entry.Project);
                projects[key] = (projects.GetValueOrDefault(key) ?? new WorktreeProjectModel(null)) with { Folder = entry.Folder };
            }

            var kept = projects.Where(pair => pair.Value.Repository is not null || pair.Value.Folder is not null).ToDictionary(StringComparer.OrdinalIgnoreCase);
            AtomicFile.Write(_filePath, JsonSerializer.Serialize(new WorktreeProjectsModel(kept), SessionRepository.JsonOptions));
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
