using System.Text.Json;
using Tily.Core.Session;

namespace Tily.Core.Agents;

public sealed record NotificationSettingsModel(bool SystemNotification = true, string Sound = NotificationSettingsModel.DefaultSound, bool DockBounce = true, bool NotifyDone = true, string DoneSound = NotificationSettingsModel.DefaultDoneSound)
{
    public const string NoSound = "none";
    public const string DefaultSound = "Glass";
    public const string DefaultDoneSound = "Hero";
    public static readonly IReadOnlyList<string> Sounds = [NoSound, "Basso", "Blow", "Bottle", "Frog", "Funk", DefaultSound, DefaultDoneSound, "Morse", "Ping", "Pop", "Purr", "Sosumi", "Submarine", "Tink"];
    public static readonly NotificationSettingsModel Default = new(true, DefaultSound, true);

    public static readonly IReadOnlyList<string> SoundFileExtensions = [".aiff", ".aif", ".wav", ".mp3"];

    public bool UsesFile => IsSoundFilePath(Sound);

    public static bool IsSoundFilePath(string sound) =>
        Path.IsPathFullyQualified(sound) && SoundFileExtensions.Contains(Path.GetExtension(sound), StringComparer.OrdinalIgnoreCase);

    public NotificationSettingsModel Normalized() => this with { Sound = NormalizedSound(Sound, DefaultSound), DoneSound = NormalizedSound(DoneSound, DefaultDoneSound) };

    private static string NormalizedSound(string? sound, string fallback)
    {
        var trimmed = sound?.Trim() ?? string.Empty;
        var alias = Sounds.FirstOrDefault(known => string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase));
        return alias ?? (IsSoundFilePath(trimmed) ? trimmed : fallback);
    }
}

public sealed class NotificationSettingsRepository
{
    public const string FileName = "notifications.json";

    private readonly string _filePath;

    public NotificationSettingsRepository(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public void Save(NotificationSettingsModel settings) => AtomicFile.Write(_filePath, JsonSerializer.Serialize(settings.Normalized(), SessionRepository.JsonOptions));

    public NotificationSettingsModel Load()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(NotificationSettingsModel.Default, SessionRepository.JsonOptions));
            return NotificationSettingsModel.Default;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<NotificationSettingsModel>(File.ReadAllText(_filePath), SessionRepository.JsonOptions);
            return settings?.Normalized() ?? NotificationSettingsModel.Default;
        }
        catch (JsonException)
        {
            return NotificationSettingsModel.Default;
        }
    }
}
