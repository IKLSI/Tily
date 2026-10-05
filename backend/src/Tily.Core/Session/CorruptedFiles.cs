namespace Tily.Core.Session;

public static class CorruptedFiles
{
    public static string Quarantine(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var target = Path.Combine(directory, $"{name}.corrompu-{stamp}{extension}");
        var attempt = 1;
        while (File.Exists(target))
        {
            target = Path.Combine(directory, $"{name}.corrompu-{stamp}-{attempt++}{extension}");
        }

        File.Move(filePath, target);
        return target;
    }
}
