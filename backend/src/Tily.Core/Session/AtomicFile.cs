using System.Text;

namespace Tily.Core.Session;

public static class AtomicFile
{
    public static void Write(string filePath, string content)
    {
        var targetPath = ResolvedTarget(filePath);
        var temporaryPath = $"{targetPath}.{Environment.ProcessId}-{Guid.NewGuid():N}.tmp";
        var mode = ExistingMode(targetPath);
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(content);
                stream.Write(bytes);
                stream.Flush(true);
            }

            if (mode is { } unixMode)
            {
                File.SetUnixFileMode(temporaryPath, unixMode);
            }

            File.Move(temporaryPath, targetPath, true);
        }
        catch
        {
            DeleteQuietly(temporaryPath);
            throw;
        }
    }

    private static void DeleteQuietly(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string ResolvedTarget(string filePath)
    {
        var file = new FileInfo(filePath);
        return file.LinkTarget is null ? filePath : file.ResolveLinkTarget(true)?.FullName ?? filePath;
    }

    private static UnixFileMode? ExistingMode(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        return File.GetUnixFileMode(filePath);
    }
}
