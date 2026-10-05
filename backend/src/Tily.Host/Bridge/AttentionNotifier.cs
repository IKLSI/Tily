using Tily.Core.Agents;

namespace Tily.Host.Bridge;

public sealed class AttentionNotifier
{
    private readonly Action<object> _sendToShell;

    public AttentionNotifier(Action<object> sendToShell) => _sendToShell = sendToShell;

    public bool WindowActive { get; set; } = true;

    public object Describe() => new { toastAvailable = true, toastError = (string?)null };

    public void Notify(string paneId, IEnumerable<string?> lines, string sound, NotificationSettingsModel settings, bool force)
    {
        if (WindowActive && !force)
        {
            return;
        }

        _sendToShell(new
        {
            type = "host.notify",
            pane = paneId,
            lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList(),
            sound = sound == NotificationSettingsModel.NoSound ? null : sound,
            bounce = settings.DockBounce,
            toast = settings.SystemNotification
        });
    }

    public void FlashWhenInactive(NotificationSettingsModel settings)
    {
        if (!WindowActive && settings.DockBounce)
        {
            _sendToShell(new { type = "host.bounce" });
        }
    }
}
