using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Tily.Core.Browser;
using Windows.Foundation;

namespace Tily.Host.Bridge;

internal sealed class BrowserView : IDisposable
{
    public const string ProfileName = "browser";
    private const double OffscreenLeft = -20000;
    private static readonly string[] ConsoleEvents = [DevToolsConsole.ConsoleApiCalled, DevToolsConsole.ExceptionThrown, DevToolsConsole.EntryAdded];

    private readonly BrowserCallbacks _callbacks;
    private readonly DevToolsNetwork _network = new();
    private readonly List<TaskCompletionSource<BrowserNavigationModel>> _navigations = [];
    private CoreWebView2? _core;
    private Rect _bounds;
    private bool _visible;
    private bool _capturing;
    private bool _focusPending;
    private int _lastErrors;

    public BrowserView(string paneId, BrowserViewport viewport, BrowserCallbacks callbacks)
    {
        PaneId = paneId;
        Viewport = viewport;
        _callbacks = callbacks;
        Control = new WebView2 { Visibility = Visibility.Collapsed, DefaultBackgroundColor = Microsoft.UI.Colors.White };
        Control.GotFocus += (_, _) => _callbacks.Focused(this);
    }

    public string PaneId { get; }

    public WebView2 Control { get; }

    public BrowserLog Log { get; } = new();

    public BrowserViewport Viewport { get; private set; }

    public bool Ready => _core is not null;

    public bool Loading { get; private set; }

    public async Task InitializeAsync(CoreWebView2Environment environment, string url, string? shortcutLetters)
    {
        var options = environment.CreateCoreWebView2ControllerOptions();
        options.ProfileName = ProfileName;
        await Control.EnsureCoreWebView2Async(environment, options);
        var core = Control.CoreWebView2;
        core.Settings.IsGeneralAutofillEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = true;
        core.NavigationStarting += HandleNavigationStarting;
        core.NavigationCompleted += HandleNavigationCompleted;
        core.SourceChanged += (_, _) => PostState();
        core.DocumentTitleChanged += (_, _) => PostState();
        core.HistoryChanged += (_, _) => PostState();
        core.NewWindowRequested += HandleNewWindowRequested;
        core.WebMessageReceived += HandleWebMessage;
        foreach (var name in ConsoleEvents)
        {
            core.GetDevToolsProtocolEventReceiver(name).DevToolsProtocolEventReceived += (_, args) => ReceiveConsole(name, args.ParameterObjectAsJson);
        }

        foreach (var name in DevToolsNetwork.Events)
        {
            core.GetDevToolsProtocolEventReceiver(name).DevToolsProtocolEventReceived += (_, args) => ReceiveNetwork(name, args.ParameterObjectAsJson);
        }

        await core.AddScriptToExecuteOnDocumentCreatedAsync(BrowserPageScript.Keys(shortcutLetters));
        await core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");
        await core.CallDevToolsProtocolMethodAsync("Log.enable", "{}");
        await core.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
        _core = core;
        await ApplyViewportAsync();
        core.Navigate(url);
    }

    public void SetBounds(Rect bounds, bool visible)
    {
        _bounds = bounds;
        _visible = visible;
        if (!_capturing)
        {
            Layout();
        }
    }

    public async Task SetViewportAsync(BrowserViewport viewport)
    {
        Viewport = viewport;
        Layout();
        await ApplyViewportAsync();
        PostState();
    }

    public void Navigate(string url) => _core?.Navigate(url);

    public Task<BrowserNavigationModel> NavigateAsync(string url, TimeSpan timeout) => WaitForNavigationAsync(() => Require().Navigate(url), timeout);

    public Task<BrowserNavigationModel> ReloadAsync(TimeSpan timeout) => WaitForNavigationAsync(() => Require().Reload(), timeout);

    public void Back()
    {
        if (_core?.CanGoBack == true)
        {
            _core.GoBack();
        }
    }

    public void Reload() => _core?.Reload();

    public void OpenDevTools() => _core?.OpenDevToolsWindow();

    public void Focus()
    {
        _focusPending = Control.Visibility != Visibility.Visible;
        if (!_focusPending)
        {
            Control.Focus(FocusState.Programmatic);
        }
    }

    public BrowserStateModel State() => new(
        PaneId,
        _core?.Source ?? string.Empty,
        _core?.DocumentTitle ?? string.Empty,
        Loading,
        _core?.CanGoBack ?? false,
        _core?.CanGoForward ?? false,
        Log.ErrorCount(),
        BrowserViewports.Name(Viewport));

    public async Task<BrowserCaptureModel> CaptureAsync(BrowserViewport viewport, bool fullPage)
    {
        var core = Require();
        var size = BrowserViewports.Capture(viewport);
        _capturing = true;
        try
        {
            if (!_visible)
            {
                Canvas.SetLeft(Control, OffscreenLeft);
                Control.Width = size.Width;
                Control.Height = size.Height;
                Control.Visibility = Visibility.Visible;
            }

            await core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", Json(new { width = size.Width, height = size.Height, deviceScaleFactor = size.Scale, mobile = size.Mobile }));
            await SettleAsync(core);
            var (width, height) = fullPage ? await ContentSizeAsync(core) : (size.Width, size.Height);
            var parameters = fullPage
                ? Json(new { format = "png", captureBeyondViewport = true, clip = new { x = 0, y = 0, width, height, scale = 1 } })
                : Json(new { format = "png" });
            using var result = JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", parameters));
            var data = result.RootElement.GetProperty("data").GetString() ?? string.Empty;
            return new BrowserCaptureModel(data, width, height, size.Scale, BrowserViewports.Name(viewport), core.Source, core.DocumentTitle);
        }
        finally
        {
            _capturing = false;
            await ApplyViewportAsync();
            Layout();
        }
    }

    public async Task<string> SnapshotAsync()
    {
        var core = Require();
        if (Control.Visibility != Visibility.Visible)
        {
            return string.Empty;
        }

        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, stream);
        var bytes = new byte[stream.Size];
        using var input = stream.GetInputStreamAt(0).AsStreamForRead();
        await input.ReadExactlyAsync(bytes);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
    }

    private static async Task<(int Width, int Height)> ContentSizeAsync(CoreWebView2 core)
    {
        using var metrics = JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.getLayoutMetrics", "{}"));
        var content = metrics.RootElement.GetProperty("cssContentSize");
        var width = (int)Math.Ceiling(content.GetProperty("width").GetDouble());
        var height = (int)Math.Min(Math.Ceiling(content.GetProperty("height").GetDouble()), BrowserCaptureModel.MaxFullPageHeight);
        return (width, height);
    }

    private static async Task SettleAsync(CoreWebView2 core)
    {
        await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", Json(new
        {
            expression = "new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(resolve, 150))))",
            awaitPromise = true
        }));
    }

    private async Task ApplyViewportAsync()
    {
        if (_core is null)
        {
            return;
        }

        if (Viewport == BrowserViewport.Mobile)
        {
            await _core.CallDevToolsProtocolMethodAsync("Emulation.setDeviceMetricsOverride", Json(new { width = 0, height = 0, deviceScaleFactor = 0, mobile = true }));
        }
        else
        {
            await _core.CallDevToolsProtocolMethodAsync("Emulation.clearDeviceMetricsOverride", "{}");
        }
    }

    private void Layout()
    {
        var width = Viewport == BrowserViewport.Mobile ? Math.Min(BrowserViewports.MobilePaneWidth, _bounds.Width) : _bounds.Width;
        Canvas.SetLeft(Control, _bounds.X + (_bounds.Width - width) / 2);
        Canvas.SetTop(Control, _bounds.Y);
        Control.Width = Math.Max(0, width);
        Control.Height = Math.Max(0, _bounds.Height);
        Control.Visibility = _visible && _bounds.Width > 0 && _bounds.Height > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_focusPending && Control.Visibility == Visibility.Visible)
        {
            _focusPending = false;
            Control.Focus(FocusState.Programmatic);
        }
    }

    private async Task<BrowserNavigationModel> WaitForNavigationAsync(Action start, TimeSpan timeout)
    {
        var completion = new TaskCompletionSource<BrowserNavigationModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        _navigations.Add(completion);
        start();
        try
        {
            return await completion.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            return new BrowserNavigationModel(_core?.Source ?? string.Empty, _core?.DocumentTitle ?? string.Empty, false, null, "La page n’a pas fini de charger dans le délai imparti.", Log.ErrorCount());
        }
        finally
        {
            _navigations.Remove(completion);
        }
    }

    private void HandleNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        Loading = true;
        Log.StartLoad();
        _lastErrors = 0;
        PostState();
    }

    private void HandleNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        Loading = false;
        var status = args.HttpStatusCode > 0 ? args.HttpStatusCode : (int?)null;
        var error = args.IsSuccess ? null : args.WebErrorStatus.ToString();
        var result = new BrowserNavigationModel(sender.Source, sender.DocumentTitle, args.IsSuccess, status, error, Log.ErrorCount());
        foreach (var navigation in _navigations.ToList())
        {
            navigation.TrySetResult(result);
        }

        PostState();
    }

    private void HandleNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        if (args.WindowFeatures.HasSize || args.WindowFeatures.HasPosition)
        {
            return;
        }

        args.Handled = true;
        if (BrowserAddress.IsAllowed(args.Uri))
        {
            _callbacks.NewPane(this, args.Uri);
        }
    }

    private void HandleWebMessage(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out var type) && type.GetString() == BrowserPageScript.KeyMessage)
            {
                _callbacks.Key(this, root.Clone());
            }
        }
        catch (JsonException)
        {
        }
    }

    private void ReceiveConsole(string name, string json)
    {
        try
        {
            if (DevToolsConsole.Parse(name, json) is { } message)
            {
                Log.AddConsole(message);
                PostErrorsIfChanged();
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
        }
    }

    private void ReceiveNetwork(string name, string json)
    {
        try
        {
            if (_network.Handle(name, json) is not { } finished)
            {
                return;
            }

            var entry = Log.AddNetwork(finished.Entry);
            PostErrorsIfChanged();
            if (finished.NeedsBody)
            {
                _ = AttachBodyAsync(entry.Sequence, finished.RequestId);
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
        }
    }

    private async Task AttachBodyAsync(long sequence, string requestId)
    {
        try
        {
            var json = await Require().CallDevToolsProtocolMethodAsync("Network.getResponseBody", Json(new { requestId }));
            if (DevToolsNetwork.BodyOf(json) is { } body)
            {
                Log.AttachBody(sequence, body);
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or System.Runtime.InteropServices.COMException or ArgumentException)
        {
        }
    }

    private void PostErrorsIfChanged()
    {
        var errors = Log.ErrorCount();
        if (errors != _lastErrors)
        {
            _lastErrors = errors;
            PostState();
        }
    }

    private void PostState() => _callbacks.StateChanged(this);

    private CoreWebView2 Require() => _core ?? throw new InvalidOperationException("Le navigateur de ce pane démarre encore : réessayez dans un instant.");

    private static string Json(object value) => JsonSerializer.Serialize(value);

    public void Dispose()
    {
        foreach (var navigation in _navigations.ToList())
        {
            navigation.TrySetException(new InvalidOperationException("Le pane navigateur a été fermé."));
        }

        Control.Close();
    }
}
