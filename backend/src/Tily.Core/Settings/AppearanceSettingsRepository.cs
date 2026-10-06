using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Settings;

public sealed record AppearanceSettingsModel(int FontSize = AppearanceSettingsModel.DefaultFontSize, string FontFamily = AppearanceSettingsModel.DefaultFontFamily)
{
    public const int DefaultFontSize = 12;
    public const int MinFontSize = 8;
    public const int MaxFontSize = 32;
    public const string DefaultFontFamily = "Menlo";
    public static readonly IReadOnlyList<string> FontFamilies = [DefaultFontFamily, "Monaco", "Courier New", "CaskaydiaCove Nerd Font", "MesloLGS Nerd Font", "Hack Nerd Font", "JetBrainsMono Nerd Font", "FiraCode Nerd Font"];

    public static readonly AppearanceSettingsModel Default = new();

    public AppearanceSettingsModel Clamped() => this with
    {
        FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize),
        FontFamily = FontFamilies.FirstOrDefault(known => string.Equals(known, FontFamily?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? DefaultFontFamily
    };
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
