using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Shell;

public sealed class ShellPathsRepository
{
    public const string FileName = "shells.json";

    private readonly string _filePath;

    public ShellPathsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public ShellPathsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            WriteTemplate();
            return ShellPathsModel.Empty;
        }

        try
        {
            var executables = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(_filePath), SessionRepository.JsonOptions);
            var cleaned = (executables ?? new Dictionary<string, string?>())
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);
            return new ShellPathsModel(cleaned);
        }
        catch (JsonException)
        {
            return ShellPathsModel.Empty;
        }
    }

    public void Save(Dictionary<string, string> executables) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(executables, SessionRepository.JsonOptions));

    private void WriteTemplate()
    {
        var defaults = ShellCatalog.Profiles(ShellPathsModel.Empty)
            .Where(profile => profile.Id != ShellCatalog.DefaultShellId)
            .ToDictionary(profile => profile.Id, profile => profile.Executable);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(defaults, SessionRepository.JsonOptions));
    }
}
