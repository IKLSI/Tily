namespace Tily.Core.Mcp;

public static class McpActions
{
    public const string OpenWorkspace = "openWorkspace";
    public const string NewTab = "newTab";
    public const string Split = "split";
    public const string Focus = "focus";
    public const string Rename = "rename";
    public const string Run = "run";
    public const string Interrupt = "interrupt";
    public const string Close = "close";
    public const int MaxNameChars = 100;
    public const int MaxCommandChars = 4000;
    public static readonly TimeSpan ConsentAnswerTimeout = TimeSpan.FromSeconds(125);

    public static bool MayAskConsent(string tool) => tool is Run or Interrupt or Close;

    public static string? Command(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        if (command.Contains('\n') || command.Contains('\r'))
        {
            throw new InvalidOperationException("La commande doit tenir sur une ligne : enchaînez plusieurs commandes avec « ; ».");
        }

        return command.Length > MaxCommandChars
            ? throw new InvalidOperationException($"Commande trop longue : {MaxCommandChars} caractères au plus.")
            : command.Trim();
    }

    public static string? Name(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxNameChars ? throw new InvalidOperationException($"Nom trop long : {MaxNameChars} caractères au plus.") : trimmed;
    }
}
