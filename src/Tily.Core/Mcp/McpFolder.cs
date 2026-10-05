namespace Tily.Core.Mcp;

public static class McpFolder
{
    public static string? Resolve(string? path, string currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string full;
        try
        {
            full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()), currentDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"Chemin invalide : {path}.");
        }

        return Directory.Exists(full) ? Path.TrimEndingDirectorySeparator(full) : throw new InvalidOperationException($"Dossier introuvable : {full}.");
    }
}
