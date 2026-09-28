namespace Dock.Core.Git;

public static class GitNames
{
    private const string ForbiddenCharacters = "Les espaces, « .. », « ~ », « ^ », « : », « ? », « * », « [ », « \\ » et « @{ » sont interdits, comme un nom qui commence par « - » ou « . » ou finit par « .lock ».";

    public static string RequireRevision(string? revision)
    {
        if (string.IsNullOrWhiteSpace(revision) || revision.StartsWith('-') || revision.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            throw new GitCommandException($"Référence Git invalide : « {revision} ».", string.Empty);
        }

        return revision;
    }

    public static string RequireRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Split('/', '\\').Contains(".."))
        {
            throw new GitCommandException($"Chemin de fichier invalide : « {path} ».", string.Empty);
        }

        return path;
    }

    public static string RequireBranchName(GitRepository repository, string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        var output = trimmed.Length == 0 || trimmed.StartsWith('-') ? null : repository.Run("check-ref-format", "--branch", trimmed);
        if (output is null || !output.Succeeded || output.Output.Trim() != trimmed)
        {
            throw new GitCommandException($"Nom de branche invalide : « {trimmed} ».", ForbiddenCharacters);
        }

        return trimmed;
    }

    public static string RequireTagName(GitRepository repository, string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.StartsWith('-') || !repository.Run("check-ref-format", $"refs/tags/{trimmed}").Succeeded)
        {
            throw new GitCommandException($"Nom de tag invalide : « {trimmed} ».", ForbiddenCharacters);
        }

        return trimmed;
    }
}
