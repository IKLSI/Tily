using System.Text;

namespace Tily.Core.Session;

public sealed class PaneTextRepository
{
    public const string DirectoryName = "text";
    private const string Extension = ".txt";
    private const int MaxPaneIdLength = 128;

    private readonly long _maxBytes;

    public PaneTextRepository(string dataDirectory, long maxBytes)
    {
        DirectoryPath = Path.Combine(dataDirectory, DirectoryName);
        Directory.CreateDirectory(DirectoryPath);
        _maxBytes = maxBytes;
    }

    public string DirectoryPath { get; }

    public Dictionary<string, string> Load()
    {
        var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var file in PaneFiles().OrderByDescending(file => file.LastWriteTimeUtc))
        {
            total += file.Length;
            if (total > _maxBytes)
            {
                break;
            }

            text[PaneIdOf(file)] = File.ReadAllText(file.FullName);
        }

        return text;
    }

    public void Save(IReadOnlyDictionary<string, string> text, IReadOnlyCollection<string> keep)
    {
        var kept = new HashSet<string>(keep, StringComparer.OrdinalIgnoreCase);
        kept.UnionWith(text.Keys);
        var sizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in PaneFiles().ToArray())
        {
            if (kept.Contains(PaneIdOf(file)))
            {
                sizes[PaneIdOf(file)] = file.Length;
            }
            else
            {
                File.Delete(file.FullName);
            }
        }

        var total = sizes.Values.Sum();
        foreach (var (paneId, content) in text.Where(pair => IsValidPaneId(pair.Key) && pair.Value is not null))
        {
            total -= sizes.GetValueOrDefault(paneId);
            sizes.Remove(paneId);
            var bytes = Encoding.UTF8.GetByteCount(content);
            if (total + bytes > _maxBytes)
            {
                File.Delete(PathFor(paneId));
                continue;
            }

            AtomicFile.Write(PathFor(paneId), content);
            sizes[paneId] = bytes;
            total += bytes;
        }
    }

    private IEnumerable<FileInfo> PaneFiles() =>
        new DirectoryInfo(DirectoryPath)
            .EnumerateFiles("*" + Extension)
            .Where(file => file.Extension.Equals(Extension, StringComparison.OrdinalIgnoreCase) && IsValidPaneId(PaneIdOf(file)));

    private string PathFor(string paneId) => Path.Combine(DirectoryPath, paneId + Extension);

    private static string PaneIdOf(FileInfo file) => Path.GetFileNameWithoutExtension(file.Name);

    private static bool IsValidPaneId(string paneId) =>
        paneId.Length is > 0 and <= MaxPaneIdLength && paneId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
