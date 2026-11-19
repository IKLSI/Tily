namespace Tily.Core.Browser;

public sealed record BrowserKeyModel(string Key, string Code, int VirtualKeyCode, int Modifiers, string? Text);

public static class BrowserKeys
{
    private const int Alt = 1;
    private const int Control = 2;
    private const int Meta = 4;
    private const int Shift = 8;

    private static readonly Dictionary<string, BrowserKeyModel> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = new("Enter", "Enter", 13, 0, "\r"),
        ["Tab"] = new("Tab", "Tab", 9, 0, null),
        ["Escape"] = new("Escape", "Escape", 27, 0, null),
        ["Backspace"] = new("Backspace", "Backspace", 8, 0, null),
        ["Delete"] = new("Delete", "Delete", 46, 0, null),
        ["Space"] = new(" ", "Space", 32, 0, " "),
        ["ArrowUp"] = new("ArrowUp", "ArrowUp", 38, 0, null),
        ["ArrowDown"] = new("ArrowDown", "ArrowDown", 40, 0, null),
        ["ArrowLeft"] = new("ArrowLeft", "ArrowLeft", 37, 0, null),
        ["ArrowRight"] = new("ArrowRight", "ArrowRight", 39, 0, null),
        ["Home"] = new("Home", "Home", 36, 0, null),
        ["End"] = new("End", "End", 35, 0, null),
        ["PageUp"] = new("PageUp", "PageUp", 33, 0, null),
        ["PageDown"] = new("PageDown", "PageDown", 34, 0, null)
    };

    private static readonly Dictionary<string, int> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alt"] = Alt,
        ["Ctrl"] = Control,
        ["Control"] = Control,
        ["Cmd"] = Meta,
        ["Meta"] = Meta,
        ["Shift"] = Shift
    };

    public static BrowserKeyModel Parse(string? chord)
    {
        if (string.IsNullOrWhiteSpace(chord))
        {
            throw new InvalidOperationException("Indiquez la touche à presser, par exemple Enter, Tab ou Ctrl+A.");
        }

        var parts = chord.Trim().Split('+', StringSplitOptions.TrimEntries);
        var modifiers = 0;
        foreach (var part in parts[..^1])
        {
            modifiers |= Modifiers.TryGetValue(part, out var modifier)
                ? modifier
                : throw new InvalidOperationException($"Modificateur inconnu : « {part} ». Choisissez Ctrl, Alt, Shift ou Cmd.");
        }

        var key = parts[^1];
        if (Named.TryGetValue(key, out var named))
        {
            return named with { Modifiers = modifiers };
        }

        if (key.Length == 1 && char.IsAsciiLetter(key[0]))
        {
            var upper = char.ToUpperInvariant(key[0]);
            var shifted = (modifiers & Shift) != 0 || (modifiers == 0 && char.IsAsciiLetterUpper(key[0]));
            var character = (shifted ? upper : char.ToLowerInvariant(upper)).ToString();
            var text = (modifiers & (Control | Meta | Alt)) == 0 ? character : null;
            return new BrowserKeyModel(character, $"Key{upper}", upper, modifiers, text);
        }

        if (key.Length == 1 && char.IsAsciiDigit(key[0]))
        {
            var text = (modifiers & (Control | Meta | Alt)) == 0 ? key : null;
            return new BrowserKeyModel(key, $"Digit{key}", key[0], modifiers, text);
        }

        throw new InvalidOperationException($"Touche inconnue : « {key} ». Choisissez Enter, Tab, Escape, Backspace, Delete, Space, une flèche (ArrowUp…), Home, End, PageUp, PageDown, une lettre ou un chiffre.");
    }
}
