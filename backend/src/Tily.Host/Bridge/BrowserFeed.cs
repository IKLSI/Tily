using System.Text.Json;
using Tily.Core.Browser;
using Tily.Core.Session;

namespace Tily.Host.Bridge;

public sealed class BrowserFeed : IDisposable
{
    public const string Prefix = """{"type":"browser.""";
    public const string DriverPrefix = """{"type":"host.browser""";
    private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds(30);

    private readonly HostLoop _loop;
    private readonly Action<object> _post;
    private readonly BrowserDriver _driver;
    private readonly Dictionary<string, BrowserView> _views = new();
    private readonly BrowserCallbacks _callbacks;

    public BrowserFeed(HostLoop loop, Action<object> post, Action<object> sendToShell)
    {
        _loop = loop;
        _post = post;
        _driver = new BrowserDriver(sendToShell);
        _callbacks = new BrowserCallbacks(PostState, PostNewPane, PostKey, PostFocused);
    }

    public void Receive(string json)
    {
        BrowserCommandModel? command;
        try
        {
            command = JsonSerializer.Deserialize<BrowserCommandModel>(json, SessionRepository.JsonOptions);
        }
        catch (JsonException)
        {
            _post(new { type = "error", message = "Message du navigateur illisible." });
            return;
        }

        switch (command?.Type)
        {
            case null:
                return;
            case "host.browserResult":
                _driver.Complete(command.Id, command.Result, command.Error);
                return;
            case "host.browserEvent":
                if (command.Pane is { } pane && _views.TryGetValue(pane, out var view))
                {
                    view.Receive(command);
                }

                return;
            default:
                _ = HandleAsync(command);
                return;
        }
    }

    private async Task HandleAsync(BrowserCommandModel command)
    {
        try
        {
            var result = await DispatchAsync(command);
            if (command.Request > 0)
            {
                _post(new { type = "browser.reply", request = command.Request, result = result ?? new { } });
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or TimeoutException or KeyNotFoundException)
        {
            _post(command.Request > 0 ? new { type = "browser.reply", request = command.Request, error = exception.Message } : new { type = "error", message = exception.Message });
        }
    }

    private async Task<object?> DispatchAsync(BrowserCommandModel command)
    {
        switch (command.Type)
        {
            case "browser.attach":
                await AttachAsync(command);
                return null;
            case "browser.bounds":
                if (Find(command) is { } bounded)
                {
                    await bounded.SetBoundsAsync(command.X, command.Y, Math.Max(0, command.Width), Math.Max(0, command.Height), command.Visible);
                }

                return null;
            case "browser.snapshot":
                return new { image = await Require(command).SnapshotAsync() };
            case "browser.navigate":
                var url = BrowserAddress.Normalize(command.Url);
                if (command.Request > 0)
                {
                    return await Require(command).NavigateAndWaitAsync(url, NavigationTimeout);
                }

                await Require(command).NavigateAsync(url);
                return null;
            case "browser.back":
                await Require(command).BackAsync();
                return null;
            case "browser.reload":
                if (command.Request > 0)
                {
                    return await Require(command).ReloadAndWaitAsync(NavigationTimeout);
                }

                await Require(command).ReloadAsync();
                return null;
            case "browser.viewport":
                var view = Require(command);
                await view.SetViewportAsync(BrowserViewports.Parse(command.Viewport));
                return view.State();
            case "browser.devtools":
                await Require(command).OpenDevToolsAsync();
                return null;
            case "browser.focus":
                if (Find(command) is { } focused)
                {
                    await focused.FocusAsync();
                }

                return null;
            case "browser.close":
                await CloseAsync(command.Pane);
                return null;
            case "browser.console":
                return Require(command).Log.Console(Level(command.Level), command.SinceLoad, Math.Max(1, command.Limit));
            case "browser.network":
                return Require(command).Log.Network(command.FailedOnly, command.SinceLoad, Math.Max(1, command.Limit));
            case "browser.screenshot":
                return await Require(command).CaptureAsync(BrowserViewports.Parse(command.Viewport), command.FullPage);
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private async Task AttachAsync(BrowserCommandModel command)
    {
        var paneId = command.Pane ?? throw new InvalidOperationException("Identifiant de pane manquant.");
        if (_views.ContainsKey(paneId))
        {
            return;
        }

        var view = new BrowserView(paneId, BrowserViewports.Parse(command.Viewport), _driver, _loop, _callbacks);
        _views[paneId] = view;
        try
        {
            await view.InitializeAsync(BrowserAddress.IsAllowed(command.Url) ? command.Url! : BrowserAddress.Blank, command.Shortcuts);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or KeyNotFoundException)
        {
            await CloseAsync(paneId);
            _post(new { type = "browser.failed", pane = paneId, message = $"Le navigateur n’a pas pu démarrer : {exception.Message}" });
        }
    }

    private static BrowserLogLevel Level(string? level) => level switch
    {
        null or "" or "log" => BrowserLogLevel.Log,
        "debug" => BrowserLogLevel.Debug,
        "warn" => BrowserLogLevel.Warn,
        "error" => BrowserLogLevel.Error,
        _ => throw new InvalidOperationException($"Niveau inconnu : « {level} ». Choisissez debug, log, warn ou error.")
    };

    private BrowserView? Find(BrowserCommandModel command) => command.Pane is { } paneId && _views.TryGetValue(paneId, out var view) ? view : null;

    private BrowserView Require(BrowserCommandModel command) =>
        Find(command) is { Ready: true } view ? view : throw new InvalidOperationException("Ce pane navigateur n’a pas encore démarré : affichez-le d’abord (tily_focus).");

    private async Task CloseAsync(string? paneId)
    {
        if (paneId is null || !_views.Remove(paneId, out var view))
        {
            return;
        }

        view.Fail();
        try
        {
            await view.CloseAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void PostState(BrowserView view) => _post(new { type = "browser.state", state = view.State() });

    private void PostNewPane(BrowserView view, string url) => _post(new { type = "browser.newPane", pane = view.PaneId, url });

    private void PostFocused(BrowserView view) => _post(new { type = "browser.focused", pane = view.PaneId });

    private void PostKey(BrowserView view, JsonElement key) => _post(new { type = "browser.key", pane = view.PaneId, key });

    public void Dispose()
    {
        foreach (var view in _views.Values)
        {
            view.Fail();
        }

        _views.Clear();
        _driver.CancelAll();
    }
}
