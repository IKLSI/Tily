using System.Diagnostics;
using System.Text.Json;
using Tily.Core.Browser;
using Tily.Core.Mcp;

namespace Tily.Host.Bridge;

internal sealed class BrowserView
{
    private const double OffscreenLeft = -20000;
    private static readonly TimeSpan ErrorsDelay = TimeSpan.FromMilliseconds(250);
    private static readonly string[] ConsoleEvents = [DevToolsConsole.ConsoleApiCalled, DevToolsConsole.ExceptionThrown, DevToolsConsole.EntryAdded];
    private const string BindingCalled = "Runtime.bindingCalled";
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(150);

    private readonly BrowserDriver _driver;
    private readonly BrowserCallbacks _callbacks;
    private readonly HostLoop _loop;
    private readonly DevToolsNetwork _network = new();
    private readonly List<BrowserNavigationWaiter> _navigations = [];
    private (double X, double Y, double Width, double Height) _bounds;
    private bool _visible;
    private string? _keysScriptId;
    private bool _shown;
    private bool _capturing;
    private bool _errorsScheduled;
    private int _lastErrors;
    private int? _lastStatus;

    public BrowserView(string paneId, BrowserViewport viewport, BrowserDriver driver, HostLoop loop, BrowserCallbacks callbacks)
    {
        PaneId = paneId;
        Viewport = viewport;
        _driver = driver;
        _loop = loop;
        _callbacks = callbacks;
    }

    public string PaneId { get; }
    public BrowserLog Log { get; } = new();
    public BrowserViewport Viewport { get; private set; }
    public bool Ready { get; private set; }
    public bool Loading { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public bool CanGoBack { get; private set; }
    public bool CanGoForward { get; private set; }

    public async Task InitializeAsync(string url, IReadOnlyList<string> shortcutChords)
    {
        await _driver.SendAsync("create", PaneId);
        foreach (var domain in new[] { "Runtime.enable", "Log.enable", "Network.enable", "Page.enable" })
        {
            await CdpAsync(domain);
        }

        await CdpAsync("Runtime.addBinding", new { name = BrowserPageScript.KeyBinding });
        await InstallKeysAsync(shortcutChords, evaluateNow: false);
        Ready = true;
        await ApplyViewportAsync();
        await LayoutAsync();
        await _driver.SendAsync("navigate", PaneId, new { url });
    }

    public Task UpdateShortcutsAsync(IReadOnlyList<string> shortcutChords) =>
        Ready ? InstallKeysAsync(shortcutChords, evaluateNow: true) : Task.CompletedTask;

    public Task SetBoundsAsync(double x, double y, double width, double height, bool visible)
    {
        _bounds = (x, y, width, height);
        _visible = visible;
        return _capturing ? Task.CompletedTask : LayoutAsync();
    }

    public async Task SetViewportAsync(BrowserViewport viewport)
    {
        Viewport = viewport;
        await LayoutAsync();
        await ApplyViewportAsync();
        PostState();
    }

    public Task NavigateAsync(string url) => _driver.SendAsync("navigate", PaneId, new { url });

    public Task<BrowserNavigationModel> NavigateAndWaitAsync(string url, TimeSpan timeout) => WaitForNavigationAsync(() => NavigateAsync(url), timeout);

    public Task<BrowserNavigationModel> ReloadAndWaitAsync(TimeSpan timeout) => WaitForNavigationAsync(ReloadAsync, timeout);

    public Task BackAsync() => _driver.SendAsync("back", PaneId);

    public Task ReloadAsync() => _driver.SendAsync("reload", PaneId);

    public Task OpenDevToolsAsync() => _driver.SendAsync("devtools", PaneId);

    public Task FocusAsync() => _driver.SendAsync("focus", PaneId);

    public Task CloseAsync() => _driver.SendAsync("close", PaneId);

    public BrowserStateModel State() => new(PaneId, Url, Title, Loading, CanGoBack, CanGoForward, Log.ErrorCount(), BrowserViewports.Name(Viewport));

    public async Task<string> SnapshotAsync()
    {
        if (!_shown)
        {
            return string.Empty;
        }

        var result = await _driver.CallAsync("snapshot", PaneId);
        return result.ValueKind == JsonValueKind.String ? result.GetString() ?? string.Empty : string.Empty;
    }

    public async Task<BrowserCaptureModel> CaptureAsync(BrowserViewport viewport, bool fullPage)
    {
        var size = BrowserViewports.Capture(viewport);
        _capturing = true;
        try
        {
            if (!_visible)
            {
                await _driver.SendAsync("layout", PaneId, new { x = OffscreenLeft, y = 0, width = size.Width, height = size.Height, visible = true });
            }

            await CdpAsync("Emulation.setDeviceMetricsOverride", new { width = size.Width, height = size.Height, deviceScaleFactor = size.Scale, mobile = size.Mobile });
            await CdpAsync("Runtime.evaluate", new { expression = "new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(resolve, 150))))", awaitPromise = true });
            var (width, height) = fullPage ? await ContentSizeAsync() : (size.Width, size.Height);
            var parameters = fullPage
                ? (object)new { format = "png", captureBeyondViewport = true, clip = new { x = 0, y = 0, width, height, scale = 1 } }
                : new { format = "png" };
            var capture = await CdpAsync("Page.captureScreenshot", parameters);
            var data = capture.GetProperty("data").GetString() ?? string.Empty;
            return new BrowserCaptureModel(data, width, height, size.Scale, BrowserViewports.Name(viewport), Url, Title);
        }
        finally
        {
            _capturing = false;
            await ApplyViewportAsync();
            await LayoutAsync();
        }
    }

    public Task<BrowserSnapshotModel> PageSnapshotAsync() => InteractAsync(async () =>
    {
        var value = await RunAgentAsync("window.__tilyAgent.snapshot()");
        return new BrowserSnapshotModel(
            value.GetProperty("url").GetString() ?? Url,
            value.GetProperty("title").GetString() ?? Title,
            value.GetProperty("count").GetInt32(),
            value.GetProperty("total").GetInt32(),
            value.GetProperty("truncated").GetBoolean(),
            value.GetProperty("text").GetString() ?? string.Empty);
    });

    public Task<BrowserActionModel> ClickAsync(string reference) => InteractAsync(async () =>
    {
        var target = await RunAgentAsync($"window.__tilyAgent.target({Quote(reference)})");
        var x = target.GetProperty("x").GetDouble();
        var y = target.GetProperty("y").GetDouble();
        await CdpAsync("Input.dispatchMouseEvent", new { type = "mouseMoved", x, y });
        await CdpAsync("Input.dispatchMouseEvent", new { type = "mousePressed", x, y, button = "left", buttons = 1, clickCount = 1 });
        await CdpAsync("Input.dispatchMouseEvent", new { type = "mouseReleased", x, y, button = "left", buttons = 0, clickCount = 1 });
        await Task.Delay(SettleDelay);
        return Acted("click", Describe(target), null);
    });

    public Task<BrowserActionModel> TypeAsync(string reference, string text, bool clear, bool submit) => InteractAsync(async () =>
    {
        var target = await RunAgentAsync($"window.__tilyAgent.focus({Quote(reference)}, {(clear ? "true" : "false")})");
        await CdpAsync("Input.insertText", new { text });
        if (submit)
        {
            await PressKeyAsync(BrowserKeys.Parse("Enter"));
        }

        await Task.Delay(SettleDelay);
        return Acted("type", Describe(target), submit ? "validé avec Entrée" : null);
    });

    public Task<BrowserActionModel> PressAsync(string chord) => InteractAsync(async () =>
    {
        await PressKeyAsync(BrowserKeys.Parse(chord));
        await Task.Delay(SettleDelay);
        return Acted("press", null, chord);
    });

    public Task<BrowserActionModel> SelectOptionAsync(string reference, string option) => InteractAsync(async () =>
    {
        var chosen = await RunAgentAsync($"window.__tilyAgent.select({Quote(reference)}, {Quote(option)})");
        await Task.Delay(SettleDelay);
        return Acted("select", reference, chosen.GetProperty("selected").GetString());
    });

    public Task<BrowserWaitModel> WaitForAsync(string? text, string? reference, string? url, bool gone, int timeoutMs) => InteractAsync(async () =>
    {
        var condition = JsonSerializer.Serialize(new { text, @ref = reference, url });
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var met = false;
            try
            {
                met = (await RunAgentAsync($"window.__tilyAgent.check({condition})")).GetBoolean();
            }
            catch (InvalidOperationException)
            {
            }

            if (met != gone || clock.ElapsedMilliseconds >= timeoutMs)
            {
                return new BrowserWaitModel(met != gone, (int)clock.ElapsedMilliseconds, Url, Title);
            }

            await Task.Delay(PollDelay);
        }
    });

    public async Task<BrowserEvaluationModel> EvaluateAsync(string expression)
    {
        var response = await CdpAsync("Runtime.evaluate", new { expression, returnByValue = true, awaitPromise = true, userGesture = true });
        ThrowIfFailed(response);
        var result = response.GetProperty("result");
        var type = result.TryGetProperty("type", out var kind) ? kind.GetString() ?? "undefined" : "undefined";
        var value = result.TryGetProperty("value", out var raw)
            ? raw.ValueKind == JsonValueKind.String ? raw.GetString() ?? string.Empty : raw.GetRawText()
            : result.TryGetProperty("description", out var description) ? description.GetString() ?? type : type;
        var truncated = value.Length > McpBrowser.MaxResultChars;
        return new BrowserEvaluationModel(truncated ? value[..McpBrowser.MaxResultChars] : value, type, truncated, Url, Title);
    }

    public void Receive(BrowserCommandModel message)
    {
        switch (message.Kind)
        {
            case "started":
                StartNavigation();
                break;
            case "navigated":
                _lastStatus = message.Status > 0 ? message.Status : null;
                break;
            case "finished":
                FinishNavigation(message.Success, message.Error);
                break;
            case "state":
                Url = message.Url ?? Url;
                Title = message.Title ?? Title;
                CanGoBack = message.CanGoBack;
                CanGoForward = message.CanGoForward;
                PostState();
                break;
            case "newPane" when message.Url is { } url && BrowserAddress.IsAllowed(url):
                _callbacks.NewPane(this, url);
                break;
            case "focused":
                _callbacks.Focused(this);
                break;
            case "cdp" when message.Method is { } method && message.Params is { } parameters:
                ReceiveCdp(method, parameters);
                break;
        }
    }

    public void Fail()
    {
        foreach (var waiter in _navigations.ToList())
        {
            waiter.Completion.TrySetException(new InvalidOperationException("Le pane navigateur a été fermé."));
        }
    }

    private void StartNavigation()
    {
        foreach (var waiter in _navigations)
        {
            waiter.Started = true;
        }

        Loading = true;
        _lastStatus = null;
        Log.StartLoad();
        _lastErrors = 0;
        PostState();
    }

    private void FinishNavigation(bool success, string? error)
    {
        Loading = false;
        var result = new BrowserNavigationModel(Url, Title, success, _lastStatus, success ? null : $"Chargement impossible : {error}.", Log.ErrorCount());
        foreach (var waiter in _navigations.Where(waiter => waiter.Started).ToList())
        {
            waiter.Completion.TrySetResult(result);
        }

        PostState();
    }

    private void ReceiveCdp(string method, JsonElement parameters)
    {
        try
        {
            if (method == BindingCalled)
            {
                ReceiveKey(parameters);
            }
            else if (ConsoleEvents.Contains(method))
            {
                if (DevToolsConsole.Parse(method, parameters.GetRawText()) is { } message)
                {
                    Log.AddConsole(message);
                    PostErrorsIfChanged();
                }
            }
            else if (DevToolsNetwork.Events.Contains(method) && _network.Handle(method, parameters.GetRawText()) is { } finished)
            {
                var entry = Log.AddNetwork(finished.Entry);
                PostErrorsIfChanged();
                if (finished.NeedsBody)
                {
                    _ = AttachBodyAsync(entry.Sequence, finished.RequestId);
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
        }
    }

    private void ReceiveKey(JsonElement parameters)
    {
        if (parameters.GetProperty("name").GetString() != BrowserPageScript.KeyBinding || parameters.GetProperty("payload").GetString() is not { } payload)
        {
            return;
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out var type) && type.GetString() == BrowserPageScript.KeyMessage)
        {
            _callbacks.Key(this, root.Clone());
        }
    }

    private async Task AttachBodyAsync(long sequence, string requestId)
    {
        try
        {
            var result = await CdpAsync("Network.getResponseBody", new { requestId });
            if (DevToolsNetwork.BodyOf(result.GetRawText()) is { } body)
            {
                Log.AttachBody(sequence, body);
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
        }
    }

    private async Task<T> InteractAsync<T>(Func<Task<T>> action)
    {
        _capturing = true;
        try
        {
            if (!_visible)
            {
                var size = BrowserViewports.Capture(Viewport);
                await _driver.SendAsync("layout", PaneId, new { x = OffscreenLeft, y = 0, width = size.Width, height = size.Height, visible = true });
            }

            return await action();
        }
        finally
        {
            _capturing = false;
            await LayoutAsync();
        }
    }

    private async Task<JsonElement> RunAgentAsync(string call)
    {
        var response = await CdpAsync("Runtime.evaluate", new { expression = BrowserAgentScript.Prepare(call), returnByValue = true });
        ThrowIfFailed(response);
        return response.GetProperty("result").GetProperty("value");
    }

    private static void ThrowIfFailed(JsonElement response)
    {
        if (!response.TryGetProperty("exceptionDetails", out var details))
        {
            return;
        }

        var text = details.TryGetProperty("exception", out var exception) && exception.TryGetProperty("description", out var description)
            ? description.GetString() ?? string.Empty
            : details.TryGetProperty("text", out var fallback) ? fallback.GetString() ?? string.Empty : string.Empty;
        var line = text.Split('\n')[0];
        throw new InvalidOperationException(line.StartsWith("Error: ", StringComparison.Ordinal) ? line[7..] : line);
    }

    private async Task PressKeyAsync(BrowserKeyModel key)
    {
        var type = key.Text is null ? "rawKeyDown" : "keyDown";
        await CdpAsync("Input.dispatchKeyEvent", new { type, modifiers = key.Modifiers, key = key.Key, code = key.Code, windowsVirtualKeyCode = key.VirtualKeyCode, text = key.Text, unmodifiedText = key.Text });
        await CdpAsync("Input.dispatchKeyEvent", new { type = "keyUp", modifiers = key.Modifiers, key = key.Key, code = key.Code, windowsVirtualKeyCode = key.VirtualKeyCode });
    }

    private BrowserActionModel Acted(string action, string? target, string? detail) => new(action, target, detail, Url, Title, Loading, Log.ErrorCount());

    private static string Describe(JsonElement target) =>
        $"{target.GetProperty("role").GetString()} \"{target.GetProperty("name").GetString()}\"";

    private static string Quote(string value) => JsonSerializer.Serialize(value);

    private async Task<(int Width, int Height)> ContentSizeAsync()
    {
        var metrics = await CdpAsync("Page.getLayoutMetrics");
        var content = metrics.GetProperty("cssContentSize");
        var width = (int)Math.Ceiling(content.GetProperty("width").GetDouble());
        var height = (int)Math.Min(Math.Ceiling(content.GetProperty("height").GetDouble()), BrowserCaptureModel.MaxFullPageHeight);
        return (width, height);
    }

    private Task ApplyViewportAsync() =>
        Viewport == BrowserViewport.Mobile
            ? CdpAsync("Emulation.setDeviceMetricsOverride", new { width = 0, height = 0, deviceScaleFactor = 0, mobile = true })
            : CdpAsync("Emulation.clearDeviceMetricsOverride");

    private Task LayoutAsync()
    {
        var width = Viewport == BrowserViewport.Mobile ? Math.Min(BrowserViewports.MobilePaneWidth, _bounds.Width) : _bounds.Width;
        _shown = _visible && _bounds.Width > 0 && _bounds.Height > 0;
        return _driver.SendAsync("layout", PaneId, new
        {
            x = _bounds.X + (_bounds.Width - width) / 2,
            y = _bounds.Y,
            width = Math.Max(0, width),
            height = Math.Max(0, _bounds.Height),
            visible = _shown
        });
    }

    private async Task<BrowserNavigationModel> WaitForNavigationAsync(Func<Task> start, TimeSpan timeout)
    {
        var waiter = new BrowserNavigationWaiter();
        _navigations.Add(waiter);
        try
        {
            await start();
            return await waiter.Completion.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            return new BrowserNavigationModel(Url, Title, false, null, "La page n’a pas fini de charger dans le délai imparti.", Log.ErrorCount());
        }
        finally
        {
            _navigations.Remove(waiter);
        }
    }

    private async Task InstallKeysAsync(IReadOnlyList<string> shortcutChords, bool evaluateNow)
    {
        var script = BrowserPageScript.Keys(shortcutChords);
        if (_keysScriptId is not null)
        {
            await CdpAsync("Page.removeScriptToEvaluateOnNewDocument", new { identifier = _keysScriptId });
        }

        var added = await CdpAsync("Page.addScriptToEvaluateOnNewDocument", new { source = script });
        _keysScriptId = added.TryGetProperty("identifier", out var identifier) ? identifier.GetString() : null;
        if (evaluateNow)
        {
            await CdpAsync("Runtime.evaluate", new { expression = script });
        }
    }

    private async Task<JsonElement> CdpAsync(string method, object? parameters = null) =>
        await _driver.CallAsync("cdp", PaneId, new { method, @params = parameters ?? new { } });

    private void PostErrorsIfChanged()
    {
        if (_errorsScheduled)
        {
            return;
        }

        _errorsScheduled = true;
        _loop.Schedule(ErrorsDelay, PostErrorsNow);
    }

    private void PostErrorsNow()
    {
        _errorsScheduled = false;
        var errors = Log.ErrorCount();
        if (errors != _lastErrors)
        {
            _lastErrors = errors;
            PostState();
        }
    }

    private void PostState() => _callbacks.StateChanged(this);
}

internal sealed class BrowserNavigationWaiter
{
    public TaskCompletionSource<BrowserNavigationModel> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool Started { get; set; }
}
