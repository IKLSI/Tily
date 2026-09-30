using System.Text;

namespace Dock.Core.Session;

public static class AtomicFile
{
    public static void Write(string filePath, string content)
    {
        var temporaryPath = filePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var bytes = new UTF8Encoding(false).GetBytes(content);
            stream.Write(bytes);
            stream.Flush(true);
        }

        File.Move(temporaryPath, filePath, true);
    }
}
