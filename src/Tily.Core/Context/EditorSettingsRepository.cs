using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Context;

public sealed record EditorSettingsModel(string Command)
{
    public const string DefaultCommand = "code.cmd";
    public static readonly EditorSettingsModel Default = new(DefaultCommand);
}

public sealed class EditorSettingsRepository
{
    public const string FileName = "editor.json";

    private readonly string _filePath;

    public EditorSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public void Save(EditorSettingsModel settings) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(settings, SessionRepository.JsonOptions));

    public EditorSettingsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(EditorSettingsModel.Default, SessionRepository.JsonOptions));
            return EditorSettingsModel.Default;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<EditorSettingsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions);
            return settings is null || string.IsNullOrWhiteSpace(settings.Command) ? EditorSettingsModel.Default : settings;
        }
        catch (JsonException)
        {
            return EditorSettingsModel.Default;
        }
    }
}
