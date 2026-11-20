using System.Text.RegularExpressions;

namespace Tily.Core.Worktrees;

public static partial class ClaudeLaunchCommand
{
    public const int PromptMaxLength = 4000;

    private const string Executable = "claude";
    private const string EndOfOptions = " --";

    public static string? For(bool requested, string? prompt)
    {
        if (!requested)
        {
            return null;
        }

        var text = Flatten(prompt);
        if (text.Length == 0)
        {
            return Executable;
        }

        if (text.Length > PromptMaxLength)
        {
            throw new WorktreeException($"Le texte pour Claude Code dépasse {PromptMaxLength} caractères.", WorktreeSteps.Verification);
        }

        var separator = text.StartsWith('-') ? EndOfOptions : string.Empty;
        return $"{Executable}{separator} '{text.Replace("'", "'\\''")}'";
    }

    private static string Flatten(string? prompt) =>
        string.Concat(Whitespace().Replace(prompt ?? string.Empty, " ").Trim().Where(character => !char.IsControl(character)));

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
