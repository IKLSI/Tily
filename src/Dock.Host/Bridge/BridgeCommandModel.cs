using System.Text.Json;

namespace Dock.Host.Bridge;

public sealed class BridgeCommandModel
{
    public required string Type { get; init; }
    public string? Pane { get; init; }
    public string? Data { get; init; }
    public string? Shell { get; init; }
    public string? Cwd { get; init; }
    public string? Path { get; init; }
    public string? Target { get; init; }
    public string? Url { get; init; }
    public string? Field { get; init; }
    public string? Title { get; init; }
    public string? Body { get; init; }
    public string? Name { get; init; }
    public string? Kind { get; init; }
    public string? Parent { get; init; }
    public string? File { get; init; }
    public string? OldFile { get; init; }
    public string? Message { get; init; }
    public string? Commit { get; init; }
    public string? Reference { get; init; }
    public string? NewName { get; init; }
    public string? Mode { get; init; }
    public string? Scope { get; init; }
    public string? Source { get; init; }
    public string[]? Panes { get; init; }
    public string[]? Keep { get; init; }
    public string[]? Paths { get; init; }
    public string[]? Files { get; init; }
    public int Cols { get; init; }
    public int Rows { get; init; }
    public int Chars { get; init; }
    public int Count { get; init; }
    public int Index { get; init; }
    public int Request { get; init; }
    public bool Amend { get; init; }
    public bool Push { get; init; }
    public bool Force { get; init; }
    public bool Pop { get; init; }
    public bool Checkout { get; init; }
    public bool Untracked { get; init; }
    public bool Confirmed { get; init; }
    public JsonElement? Session { get; init; }
    public JsonElement? Text { get; init; }
    public JsonElement? Settings { get; init; }
    public JsonElement? Notifications { get; init; }
}
