using System.Text.Json;

namespace Tily.Host.Bridge;

internal sealed record BrowserNavigationModel(string Url, string Title, bool Success, int? Status, string? Error, int Errors);

internal sealed record BrowserStateModel(string Pane, string Url, string Title, bool Loading, bool CanGoBack, bool CanGoForward, int Errors, string Viewport);

internal sealed record BrowserCaptureModel(string Png, int Width, int Height, double Scale, string Viewport, string Url, string Title)
{
    public const double MaxFullPageHeight = 16000;
}

internal sealed record BrowserSnapshotModel(string Url, string Title, int Count, int Total, bool Truncated, string Text);

internal sealed record BrowserActionModel(string Action, string? Target, string? Detail, string Url, string Title, bool Loading, int Errors);

internal sealed record BrowserWaitModel(bool Found, int ElapsedMs, string Url, string Title);

internal sealed record BrowserEvaluationModel(string Value, string Type, bool Truncated, string Url, string Title);

internal sealed record BrowserCallbacks(Action<BrowserView> StateChanged, Action<BrowserView, string> NewPane, Action<BrowserView, JsonElement> Key, Action<BrowserView> Focused);

internal sealed class BrowserCommandModel
{
    public required string Type { get; init; }
    public long Id { get; init; }
    public string? Kind { get; init; }
    public string? Pane { get; init; }
    public string? Url { get; init; }
    public string? Title { get; init; }
    public string? Viewport { get; init; }
    public string? Level { get; init; }
    public string? Method { get; init; }
    public string? Ref { get; init; }
    public string? Text { get; init; }
    public string? Option { get; init; }
    public string? Chord { get; init; }
    public string? Expression { get; init; }
    public bool Clear { get; init; }
    public bool Submit { get; init; }
    public bool Gone { get; init; }
    public int TimeoutMs { get; init; }
    public string? Error { get; init; }
    public JsonElement? Params { get; init; }
    public JsonElement? Result { get; init; }
    public JsonElement? Key { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public int Status { get; init; }
    public bool Visible { get; init; }
    public bool Success { get; init; }
    public bool CanGoBack { get; init; }
    public bool CanGoForward { get; init; }
    public bool SinceLoad { get; init; }
    public bool FailedOnly { get; init; }
    public bool FullPage { get; init; }
    public int Limit { get; init; }
    public int Request { get; init; }
}
