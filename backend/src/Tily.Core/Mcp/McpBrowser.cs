using Tily.Core.Browser;

namespace Tily.Core.Mcp;

public static class McpBrowser
{
    public const string Open = "browserOpen";
    public const string Navigate = "browserNavigate";
    public const string Reload = "browserReload";
    public const string Resize = "browserResize";
    public const string Console = "browserConsole";
    public const string Network = "browserNetwork";
    public const string Screenshot = "browserScreenshot";
    public const string Snapshot = "browserSnapshot";
    public const string Click = "browserClick";
    public const string Type = "browserType";
    public const string Press = "browserPress";
    public const string Select = "browserSelect";
    public const string WaitFor = "browserWaitFor";
    public const string Evaluate = "browserEvaluate";
    public const int MaxTextChars = 4000;
    public const int MaxExpressionChars = 20000;
    public const int MaxResultChars = 10000;
    public const int DefaultWaitMs = 10000;
    public const int MaxWaitMs = 30000;
    public const int DefaultLimit = 50;
    public const int MaxLimit = 500;
    public static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(60);
    private static readonly string[] Placements = ["right", "down", "tab"];
    private static readonly string[] Levels = ["debug", "log", "warn", "error"];
    private const string PngExtension = ".png";

    public static bool IsLong(string tool) => tool is Open or Navigate or Reload or Resize or Screenshot or Snapshot or Click or Type or Press or Select or WaitFor or Evaluate;

    public static bool MayAskConsent(string tool) => tool is Click or Type or Press or Select or Evaluate;

    public static string Ref(string? reference)
    {
        var value = reference?.Trim();
        return value is { Length: > 1 and <= 8 } && value[0] == 'e' && value[1..].All(char.IsAsciiDigit)
            ? value
            : throw new InvalidOperationException("Référence invalide : indiquez une ref de tily_browser_snapshot, par exemple e12.");
    }

    public static string Text(string? text) =>
        string.IsNullOrEmpty(text) ? throw new InvalidOperationException("Indiquez le texte à saisir.")
        : text.Length > MaxTextChars ? throw new InvalidOperationException($"Texte trop long : {MaxTextChars} caractères au plus.")
        : text;

    public static string Expression(string? expression) =>
        string.IsNullOrWhiteSpace(expression) ? throw new InvalidOperationException("Indiquez l’expression JavaScript à évaluer.")
        : expression.Length > MaxExpressionChars ? throw new InvalidOperationException($"Expression trop longue : {MaxExpressionChars} caractères au plus.")
        : expression;

    public static string Chord(string? chord)
    {
        BrowserKeys.Parse(chord);
        return chord!.Trim();
    }

    public static string Option(string? option) =>
        string.IsNullOrWhiteSpace(option) ? throw new InvalidOperationException("Indiquez la valeur ou le libellé de l’option à choisir.") : option.Trim();

    public static int WaitMs(int requested) => requested <= 0 ? DefaultWaitMs : Math.Clamp(requested, 100, MaxWaitMs);

    public static (string? Text, string? Ref, string? Url) Condition(string? text, string? reference, string? url)
    {
        var given = new[] { text, reference, url }.Count(value => !string.IsNullOrWhiteSpace(value));
        return given == 1
            ? (string.IsNullOrWhiteSpace(text) ? null : text, string.IsNullOrWhiteSpace(reference) ? null : Ref(reference), string.IsNullOrWhiteSpace(url) ? null : url.Trim())
            : throw new InvalidOperationException("Indiquez exactement une condition d’attente : text, ref ou url.");
    }

    public static int Limit(int requested) => Math.Clamp(requested, 1, MaxLimit);

    public static string? Url(string? url) => string.IsNullOrWhiteSpace(url) ? null : BrowserAddress.Normalize(url);

    public static string Placement(string? placement)
    {
        var value = string.IsNullOrWhiteSpace(placement) ? Placements[0] : placement.Trim().ToLowerInvariant();
        return Placements.Contains(value) ? value : throw new InvalidOperationException($"Emplacement inconnu : « {placement} ». Choisissez right (côte à côte), down (en dessous) ou tab (nouvel onglet).");
    }

    public static string? Viewport(string? viewport) => string.IsNullOrWhiteSpace(viewport) ? null : BrowserViewports.Name(BrowserViewports.Parse(viewport));

    public static string Level(string? level)
    {
        var value = string.IsNullOrWhiteSpace(level) ? Levels[1] : level.Trim().ToLowerInvariant();
        return Levels.Contains(value) ? value : throw new InvalidOperationException($"Niveau inconnu : « {level} ». Choisissez debug, log, warn ou error.");
    }

    public static string? ScreenshotPath(string? path, string currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var full = Path.GetFullPath(path.Trim(), currentDirectory);
        if (!full.EndsWith(PngExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"La capture s’enregistre en PNG : le chemin doit finir par {PngExtension}.");
        }

        var folder = Path.GetDirectoryName(full);
        return folder is not null && Directory.Exists(folder) ? full : throw new InvalidOperationException($"Dossier introuvable pour la capture : {folder}.");
    }
}
