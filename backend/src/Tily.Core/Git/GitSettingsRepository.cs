using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Git;

public sealed record GitSettingsModel(bool AutoFetch = true)
{
    public static readonly GitSettingsModel Default = new();
}

public sealed class GitSettingsRepository
{
    public const string FileName = "git.json";

    private readonly string _filePath;

    public GitSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public void Save(GitSettingsModel settings) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(settings, SessionRepository.JsonOptions));

    public GitSettingsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(GitSettingsModel.Default, SessionRepository.JsonOptions));
            return GitSettingsModel.Default;
        }

        try
        {
            return JsonSerializer.Deserialize<GitSettingsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions) ?? GitSettingsModel.Default;
        }
        catch (JsonException)
        {
            return GitSettingsModel.Default;
        }
    }
}
