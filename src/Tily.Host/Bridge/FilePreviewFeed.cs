using Tily.Core.Context;
using Tily.Core.Files;
using Microsoft.Web.WebView2.Core;

namespace Tily.Host.Bridge;

public sealed class FilePreviewFeed : IDisposable
{
    private static readonly TimeSpan ChangeDelay = TimeSpan.FromMilliseconds(150);

    private readonly Func<string> _editorCommand;
    private readonly Action<object> _post;
    private readonly BackgroundQueue _queue;
    private readonly object _sync = new();
    private readonly Timer _changeTimer;
    private FileSystemWatcher? _watcher;
    private string? _path;

    public FilePreviewFeed(Func<string> editorCommand, Action<object> post, Action<Exception> onError)
    {
        _editorCommand = editorCommand;
        _post = post;
        _queue = new BackgroundQueue(onError);
        _changeTimer = new Timer(_ => Reload());
    }

    public void Attach(CoreWebView2 core)
    {
        core.AddWebResourceRequestedFilter(PreviewAddress.Filter, CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
        core.WebResourceRequested += HandleResourceRequested;
        core.FrameNavigationStarting += HandleFrameNavigationStarting;
        core.NewWindowRequested += HandleNewWindowRequested;
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "preview.open":
                var path = RequirePath(command);
                _queue.Enqueue(() => Open(path, null));
                break;
            case "preview.follow":
                var current = RequirePath(command);
                var href = command.Href ?? throw new InvalidOperationException("Lien manquant.");
                _queue.Enqueue(() => Follow(current, href));
                break;
            case "preview.close":
                Watch(null);
                break;
            case "preview.save":
                var target = RequirePath(command);
                var content = command.Content ?? throw new InvalidOperationException("Contenu manquant.");
                _queue.Enqueue(() => Save(target, content, command.Version, command.Force));
                break;
            case "preview.browser":
                var page = RequirePath(command);
                _queue.Enqueue(() => LocalActions.OpenLink(new Uri(page).AbsoluteUri));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void HandleFrameNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var current = CurrentPath();
        if (current is not null && string.Equals(PreviewAddress.PathOf(args.Uri), current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        args.Cancel = true;
        if (current is not null && args.IsUserInitiated)
        {
            FollowFromPage(current, args.Uri);
        }
    }

    private void HandleNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        var current = CurrentPath();
        if (current is not null && args.IsUserInitiated)
        {
            FollowFromPage(current, args.Uri);
        }
    }

    private void FollowFromPage(string current, string url)
    {
        var path = PreviewAddress.PathOf(url);
        var href = path is null ? url : new Uri(path).AbsoluteUri + new Uri(url).Fragment;
        _queue.Enqueue(() => Follow(current, href));
    }

    private string? CurrentPath()
    {
        lock (_sync)
        {
            return _path;
        }
    }

    private void Follow(string current, string href)
    {
        var link = PreviewLinks.Resolve(current, href);
        switch (link.Kind)
        {
            case PreviewLinkKind.Web:
                LocalActions.OpenLink(link.Target);
                break;
            case PreviewLinkKind.Editor:
                LocalActions.OpenFileInEditor(link.Target, _editorCommand());
                break;
            default:
                Open(link.Target, link.Anchor);
                break;
        }
    }

    private void Open(string path, string? anchor)
    {
        var preview = FilePreview.Read(path);
        Watch(path);
        _post(new { type = "preview.loaded", preview, anchor, reload = false });
    }

    private void Save(string path, string content, string? version, bool force)
    {
        var error = TextFileWriter.Save(path, content, version, force);
        var preview = FilePreview.Read(path);
        if (error is null)
        {
            _post(new { type = "preview.saved", preview });
        }
        else
        {
            _post(new { type = "preview.saveFailed", preview, message = error });
        }
    }

    private void Reload()
    {
        string? path;
        lock (_sync)
        {
            path = _path;
        }

        if (path is not null)
        {
            _queue.Enqueue(() => PostReload(path));
        }
    }

    private void PostReload(string path)
    {
        lock (_sync)
        {
            if (!string.Equals(_path, path, StringComparison.Ordinal))
            {
                return;
            }
        }

        _post(new { type = "preview.loaded", preview = FilePreview.Read(path), reload = true });
    }

    private void Watch(string? path)
    {
        lock (_sync)
        {
            if (string.Equals(_path, path, StringComparison.Ordinal))
            {
                return;
            }

            _watcher?.Dispose();
            _watcher = path is null ? null : CreateWatcher(path);
            _path = path;
        }
    }

    private FileSystemWatcher? CreateWatcher(string path)
    {
        try
        {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path) ?? path, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            watcher.Changed += (_, _) => MarkChanged();
            watcher.Created += (_, _) => MarkChanged();
            watcher.Deleted += (_, _) => MarkChanged();
            watcher.Renamed += (_, _) => MarkChanged();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    private void MarkChanged() => _changeTimer.Change(ChangeDelay, Timeout.InfiniteTimeSpan);

    private static async void HandleResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        var path = PreviewAddress.PathOf(args.Request.Uri);
        var contentType = path is null ? null : PreviewTypes.PageResourceContentType(path);
        if (path is null || contentType is null)
        {
            args.Response = NotFound(sender);
            return;
        }

        var deferral = args.GetDeferral();
        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            args.Response = sender.Environment.CreateWebResourceResponse(new MemoryStream(bytes).AsRandomAccessStream(), 200, "OK", $"Content-Type: {contentType}\r\nCache-Control: no-cache");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            args.Response = NotFound(sender);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static CoreWebView2WebResourceResponse NotFound(CoreWebView2 sender) =>
        sender.Environment.CreateWebResourceResponse(null, 404, "Not Found", string.Empty);

    private static string RequirePath(BridgeCommandModel command) =>
        command.Path ?? throw new InvalidOperationException("Chemin manquant.");

    public void Dispose()
    {
        _changeTimer.Dispose();
        lock (_sync)
        {
            _watcher?.Dispose();
            _watcher = null;
            _path = null;
        }
    }
}
