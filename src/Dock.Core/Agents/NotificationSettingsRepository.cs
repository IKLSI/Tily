using System.Text.Json;
using Dock.Core.Session;

namespace Dock.Core.Agents;

public sealed record NotificationSettingsModel(bool WindowsToast, string Sound, bool TaskbarFlash, bool NotifyDone = true, string DoneSound = NotificationSettingsModel.DefaultDoneSound)
{
    public const string NoSound = "none";
    public const string DefaultSound = "Notification.Default";
    public const string DefaultDoneSound = "Notification.IM";
    public static readonly IReadOnlyList<string> Sounds = [NoSound, DefaultSound, "Notification.IM", "Notification.Mail", "Notification.Reminder", "Notification.SMS"];
    public static readonly NotificationSettingsModel Default = new(true, DefaultSound, true);

    public const string WavExtension = ".wav";

    public bool UsesFile => IsWavPath(Sound);

    public static bool IsWavPath(string sound) => Path.IsPathRooted(sound) && string.Equals(Path.GetExtension(sound), WavExtension, StringComparison.OrdinalIgnoreCase);

    public NotificationSettingsModel Normalized() => this with { Sound = NormalizedSound(Sound, DefaultSound), DoneSound = NormalizedSound(DoneSound, DefaultDoneSound) };

    private static string NormalizedSound(string? sound, string fallback)
    {
        var trimmed = sound?.Trim() ?? string.Empty;
        var alias = Sounds.FirstOrDefault(known => string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase));
        return alias ?? (IsWavPath(trimmed) ? trimmed : fallback);
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
