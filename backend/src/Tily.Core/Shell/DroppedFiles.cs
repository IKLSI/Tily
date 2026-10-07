namespace Tily.Core.Shell;

public sealed class DroppedFiles(string directory)
{
    public const int MaxBytes = 50 * 1024 * 1024;

    private const string DefaultName = "fichier";

    public static DroppedFiles Default { get; } = new(Path.Combine(Path.GetTempPath(), "tily-drops"));

    public string Save(string name, byte[] content)
    {
        if (content.Length > MaxBytes)
        {
            throw new InvalidOperationException("Fichier déposé trop volumineux (50 Mo au plus).");
        }

        var target = Path.Combine(directory, Guid.NewGuid().ToString("N"), SafeName(name));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllBytes(target, content);
        return target;
    }

    private static string SafeName(string name)
    {
        var fileName = Path.GetFileName(name.Replace('\\', '/'));
        return fileName is "" or "." or ".." || fileName.Any(char.IsControl) ? DefaultName : fileName;
    }
}
