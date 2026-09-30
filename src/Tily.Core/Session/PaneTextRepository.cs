using System.Text;
using System.Text.Json;

namespace Tily.Core.Session;

public sealed record PaneTextLoadResultModel(Dictionary<string, string> Text, string? Error);

public sealed class PaneTextRepository
{
    public const string DirectoryName = "text";
    public const string LegacyFileName = "text.json";
    private const string Extension = ".txt";
    private const int MaxPaneIdLength = 128;

    private readonly string _legacyFilePath;
    private readonly long _maxBytes;

    public PaneTextRepository(string dataDirectory, long maxBytes)
    {
        DirectoryPath = Path.Combine(dataDirectory, DirectoryName);
        Directory.CreateDirectory(DirectoryPath);
        _legacyFilePath = Path.Combine(dataDirectory, LegacyFileName);
        _maxBytes = maxBytes;
    }

    public string DirectoryPath { get; }

    public PaneTextLoadResultModel Load()
    {
        var error = MigrateLegacyFile();
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

        return new PaneTextLoadResultModel(text, error);
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

    public void MoveClosedTabText(SessionModel session)
    {
        foreach (var closed in session.Closed.Where(closed => closed.Text is not null))
        {
            try
            {
                Import(closed.Text!);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }

            closed.Text = null;
        }
    }

    private string? MigrateLegacyFile()
    {
        if (!File.Exists(_legacyFilePath))
        {
            return null;
        }

        try
        {
            var legacy = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(_legacyFilePath), SessionRepository.JsonOptions) ?? throw new JsonException();
            Import(legacy.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value!));
            File.Delete(_legacyFilePath);
            return null;
        }
        catch (JsonException)
        {
            var kept = CorruptedFiles.Quarantine(_legacyFilePath);
            return $"L’historique des terminaux était illisible ; copie conservée dans {kept}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"L’historique des terminaux n’a pas pu être converti ({exception.Message}) ; {_legacyFilePath} est conservé.";
        }
    }

    private void Import(IReadOnlyDictionary<string, string> text)
    {
        foreach (var (paneId, content) in text.Where(pair => IsValidPaneId(pair.Key)))
        {
            AtomicFile.Write(PathFor(paneId), content);
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
