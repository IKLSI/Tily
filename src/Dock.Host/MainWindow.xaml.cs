using Dock.Host.Bridge;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;
using Windows.UI;

namespace Dock.Host;

public sealed partial class MainWindow : Window
{
    private const string VirtualHost = "dock.app";
    private const string DevServerVariable = "DOCK_WEB_DEV_URL";
    private static readonly Color Paper = Color.FromArgb(255, 0x17, 0x19, 0x1B);
    private static readonly Color Ink = Color.FromArgb(255, 0xD8, 0xDB, 0xD7);
    private static readonly Color Muted = Color.FromArgb(255, 0x84, 0x8B, 0x87);
    private static readonly Color Hover = Color.FromArgb(255, 0x24, 0x28, 0x2A);
    private readonly HostBridge _bridge;
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1480, 900));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Dock.ico"));
        ApplyDarkTitleBar();
        _bridge = new HostBridge(DispatcherQueue, App.DataDirectory, WinRT.Interop.WindowNative.GetWindowHandle(this), ForceClose);
        Closed += HandleClosed;
        Activated += HandleActivated;
        _ = InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        View.DefaultBackgroundColor = Paper;
        await View.EnsureCoreWebView2Async();
        var core = View.CoreWebView2;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.PermissionRequested += HandlePermissionRequested;
        core.SetVirtualHostNameToFolderMapping(VirtualHost, Path.Combine(AppContext.BaseDirectory, "wwwroot"), CoreWebView2HostResourceAccessKind.Allow);
        _bridge.Attach(core);
        core.Navigate(ResolveStartUrl());
        View.Focus(FocusState.Programmatic);
    }

    private void ApplyDarkTitleBar()
    {
        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Paper;
        titleBar.InactiveBackgroundColor = Paper;
        titleBar.ForegroundColor = Ink;
        titleBar.InactiveForegroundColor = Muted;
        titleBar.ButtonBackgroundColor = Paper;
        titleBar.ButtonInactiveBackgroundColor = Paper;
        titleBar.ButtonForegroundColor = Ink;
        titleBar.ButtonInactiveForegroundColor = Muted;
        titleBar.ButtonHoverBackgroundColor = Hover;
        titleBar.ButtonHoverForegroundColor = Ink;
    }

    private static string ResolveStartUrl()
    {
        var devServer = Environment.GetEnvironmentVariable(DevServerVariable);
        return string.IsNullOrWhiteSpace(devServer) ? $"https://{VirtualHost}/index.html" : devServer;
    }

    private static void HandlePermissionRequested(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs args)
    {
        if (args.PermissionKind == CoreWebView2PermissionKind.ClipboardRead)
        {
            args.State = CoreWebView2PermissionState.Allow;
        }
    }

    private void HandleActivated(object sender, WindowActivatedEventArgs args)
    {
        var active = args.WindowActivationState != WindowActivationState.Deactivated;
        _bridge.SetWindowActive(active);
        if (active)
        {
            View.Focus(FocusState.Programmatic);
        }
    }

    private void ForceClose()
    {
        if (_closeConfirmed)
        {
            return;
        }

        _closeConfirmed = true;
        Close();
    }

    private void HandleClosed(object sender, WindowEventArgs args)
    {
        if (!_closeConfirmed && _bridge.RequestClose())
        {
            args.Handled = true;
            return;
        }

        _bridge.Dispose();
    }
}
