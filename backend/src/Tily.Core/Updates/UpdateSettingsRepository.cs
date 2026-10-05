using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Updates;

public sealed record UpdateSettingsModel(bool AutoCheck = true)
{
    public static readonly UpdateSettingsModel Default = new(true);
}

public sealed class UpdateSettingsRepository
{
    public const string FileName = "updates.json";

    private readonly string _filePath;

    public UpdateSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public UpdateSettingsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            Save(UpdateSettingsModel.Default);
            return UpdateSettingsModel.Default;
        }

        try
        {
            return JsonSerializer.Deserialize<UpdateSettingsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions) ?? UpdateSettingsModel.Default;
        }
        catch (JsonException)
        {
            return UpdateSettingsModel.Default;
        }
    }

    public void Save(UpdateSettingsModel settings) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(settings, SessionRepository.JsonOptions));
}
