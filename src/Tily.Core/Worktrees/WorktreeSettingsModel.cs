using Tily.Core.Projects;

namespace Tily.Core.Worktrees;

public sealed record WorktreeSettingsModel(string Folder, string DefaultBase)
{
    public const string DefaultBaseBranch = "develop";

    public static readonly WorktreeSettingsModel Default = new(string.Empty, DefaultBaseBranch);

    public WorktreeSettingsModel Normalized() =>
        new(Folder?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(DefaultBase) ? DefaultBaseBranch : DefaultBase.Trim());

    public string FolderFor(string projectsRoot) =>
        string.IsNullOrWhiteSpace(Folder) ? Path.Combine(projectsRoot, ProjectCatalog.ExcludedFolder) : Folder.Trim();

    public string? Error()
    {
        if (!string.IsNullOrWhiteSpace(Folder) && !Path.IsPathRooted(Folder.Trim()))
        {
            return $"Le dossier des worktrees doit être un chemin absolu : {Folder}";
        }

        var branch = DefaultBase?.Trim() ?? string.Empty;
        return branch.StartsWith('-') || branch.Any(character => char.IsWhiteSpace(character) || char.IsControl(character))
            ? $"Base par défaut invalide : « {branch} »."
            : null;
    }
}
