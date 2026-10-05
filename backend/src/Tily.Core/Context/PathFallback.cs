namespace Tily.Core.Context;

public sealed record MissingDirectoryModel(string PaneId, string Path, string Fallback);

public static class PathFallback
{
    public static string NearestExisting(string path)
    {
        var current = string.IsNullOrWhiteSpace(path) ? null : new DirectoryInfo(path);
        while (current is not null)
        {
            if (current.Exists && current.Parent is not null)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
