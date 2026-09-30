using System.Text.Json;
using Dock.Core.Session;

namespace Dock.Core.Settings;

public sealed record AppearanceSettingsModel(int FontSize = AppearanceSettingsModel.DefaultFontSize)
{
    public const int DefaultFontSize = 14;
    public const int MinFontSize = 8;
    public const int MaxFontSize = 32;

    public static readonly AppearanceSettingsModel Default = new();

    public AppearanceSettingsModel Clamped() => this with { FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize) };
}

public sealed class AppearanceSettingsRepository
{
    public const string FileName = "appearance.json";

    private readonly string _filePath;

    public AppearanceSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public void Save(AppearanceSettingsModel settings) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(settings, SessionRepository.JsonOptions));

    public AppearanceSettingsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(AppearanceSettingsModel.Default, SessionRepository.JsonOptions));
            return AppearanceSettingsModel.Default;
        }

        try
        {
            return (JsonSerializer.Deserialize<AppearanceSettingsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions) ?? AppearanceSettingsModel.Default).Clamped();
        }
        catch (JsonException)
        {
            return AppearanceSettingsModel.Default;
        }
    }
}
