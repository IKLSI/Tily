using Tily.Core.Terminal;

namespace Tily.Host.Bridge;

public sealed class WindowCloseFeed
{
    private static readonly TimeSpan CloseGrace = TimeSpan.FromSeconds(3);

    private readonly HostLoop _loop;
    private readonly TerminalManager _terminals;
    private readonly Action<object> _postNow;
    private bool _webReady;
    private bool _closing;
    private IDisposable? _closeTimer;

    public WindowCloseFeed(HostLoop loop, TerminalManager terminals, Action<object> postNow)
    {
        _loop = loop;
        _terminals = terminals;
        _postNow = postNow;
    }

    public void MarkWebReady() => _webReady = true;

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "window.close":
                CloseWindow();
                break;
            case "host.closeRequested":
                RequestClose();
                break;
            case "window.closeCancel":
                CancelClose();
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void RequestClose()
    {
        if (!_webReady)
        {
            CloseWindow();
            return;
        }

        if (_closing)
        {
            return;
        }

        _closing = true;
        _postNow(new { type = "app.closing", activity = _terminals.Activity() });
        _closeTimer = _loop.Schedule(CloseGrace, CloseWindow);
    }

    private void CancelClose()
    {
        _closing = false;
        _closeTimer?.Dispose();
        _closeTimer = null;
    }

    private void CloseWindow()
    {
        _closeTimer?.Dispose();
        _closeTimer = null;
        _postNow(new { type = "host.quit" });
        _loop.Stop();
    }
}
