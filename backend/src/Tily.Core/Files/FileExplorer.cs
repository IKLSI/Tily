namespace Tily.Core.Files;

public sealed record FileEntryModel(string Name, string Path, bool IsDirectory, PreviewKind? Preview = null);

public sealed record DirectoryListingModel(string Path, IReadOnlyList<FileEntryModel> Entries, int Total, string? Error);

public static class FileExplorer
{
    public const int MaxEntries = 2000;

    private static readonly char[] ForbiddenNameCharacters = ['/', ':'];

    public static DirectoryListingModel List(string path)
    {
        try
        {
            RequireFullPath(path);
            var entries = new DirectoryInfo(path)
                .EnumerateFileSystemInfos()
                .Select(EntryOf)
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            return new DirectoryListingModel(path, entries.Take(MaxEntries).ToList(), entries.Count, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new DirectoryListingModel(path, [], 0, $"Le dossier n’existe plus : {path}");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            return new DirectoryListingModel(path, [], 0, $"Dossier illisible : {exception.Message}");
        }
    }

    private static FileEntryModel EntryOf(FileSystemInfo info)
    {
        var isDirectory = info.Attributes.HasFlag(FileAttributes.Directory);
        return new FileEntryModel(info.Name, info.FullName, isDirectory, isDirectory ? null : PreviewTypes.KindOf(info.Name));
    }

    public static string CreateFile(string parent, string name)
    {
        var target = NewEntryPath(parent, name);
        using (new FileStream(target, FileMode.CreateNew))
        {
        }

        return target;
    }

    public static string CreateFolder(string parent, string name)
    {
        var target = NewEntryPath(parent, name);
        Directory.CreateDirectory(target);
        return target;
    }

    public static string Rename(string path, string name)
    {
        RequireFullPath(path);
        var isDirectory = Directory.Exists(path);
        if (!isDirectory && !File.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        var parent = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Impossible de renommer la racine d’un lecteur : {path}");
        var target = Path.Combine(parent, RequireValidName(name));
        if (string.Equals(path, target, StringComparison.Ordinal))
        {
            return target;
        }

        if (!string.Equals(path, target, StringComparison.OrdinalIgnoreCase))
        {
            RequireAvailable(target, name);
        }

        if (isDirectory)
        {
            Directory.Move(path, target);
        }
        else
        {
            File.Move(path, target);
        }

        return target;
    }

    public static string RequireValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Le nom ne peut pas être vide.");
        }

        if (name is "." or ".." || name.IndexOfAny(ForbiddenNameCharacters) >= 0 || name.Any(char.IsControl))
        {
            throw new InvalidOperationException($"Nom invalide : « {name} ». Les caractères / et : sont interdits.");
        }

        return name;
    }

    public static void RequireFullPath(string path)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            throw new InvalidOperationException($"Chemin absolu attendu : {path}");
        }
    }

    private static string NewEntryPath(string parent, string name)
    {
        RequireFullPath(parent);
        if (!Directory.Exists(parent))
        {
            throw new InvalidOperationException($"Le dossier n’existe plus : {parent}");
        }

        var target = Path.Combine(parent, RequireValidName(name));
        RequireAvailable(target, name);
        return target;
    }

    private static void RequireAvailable(string target, string name)
    {
        if (File.Exists(target) || Directory.Exists(target))
        {
            throw new InvalidOperationException($"« {name} » existe déjà dans ce dossier.");
        }
    }
}
