using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dock.Core.Git;

[JsonConverter(typeof(JsonStringEnumConverter<GitDiffLineKind>))]
public enum GitDiffLineKind
{
    [JsonStringEnumMemberName("context")] Context,
    [JsonStringEnumMemberName("added")] Added,
    [JsonStringEnumMemberName("removed")] Removed,
    [JsonStringEnumMemberName("note")] Note
}

public sealed record GitDiffLineModel(GitDiffLineKind Kind, int? Old, int? New, string Text);

public sealed record GitDiffHunkModel(string Header, IReadOnlyList<GitDiffLineModel> Lines);

public sealed record GitDiffModel(string Path, string? OldPath, bool Binary, bool Truncated, IReadOnlyList<string> Notes, IReadOnlyList<GitDiffHunkModel> Hunks);

public static partial class GitDiffParser
{
    public const int MaxLines = 20000;

    public static GitDiffModel Parse(string path, string? oldPath, string diff, int maxLines = MaxLines)
    {
        var notes = new List<string>();
        var hunks = new List<GitDiffHunkModel>();
        List<GitDiffLineModel>? lines = null;
        string? header = null;
        var binary = false;
        var truncated = false;
        var count = 0;
        int oldLine = 0, newLine = 0;
        foreach (var raw in diff.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var hunk = HunkHeader().Match(line);
            if (hunk.Success)
            {
                Flush(hunks, header, lines);
                header = line;
                lines = [];
                oldLine = int.Parse(hunk.Groups[1].Value);
                newLine = int.Parse(hunk.Groups[2].Value);
                continue;
            }

            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                Flush(hunks, header, lines);
                header = null;
                lines = null;
                continue;
            }

            if (lines is null)
            {
                binary |= line.StartsWith("Binary files ", StringComparison.Ordinal) || line.StartsWith("GIT binary patch", StringComparison.Ordinal);
                AddNote(notes, line);
                continue;
            }

            if (line.Length == 0)
            {
                continue;
            }

            if (count >= maxLines)
            {
                truncated = true;
                break;
            }

            count++;
            switch (line[0])
            {
                case '+':
                    lines.Add(new GitDiffLineModel(GitDiffLineKind.Added, null, newLine++, line[1..]));
                    break;
                case '-':
                    lines.Add(new GitDiffLineModel(GitDiffLineKind.Removed, oldLine++, null, line[1..]));
                    break;
                case '\\':
                    lines.Add(new GitDiffLineModel(GitDiffLineKind.Note, null, null, "Pas de retour à la ligne en fin de fichier"));
                    break;
                default:
                    lines.Add(new GitDiffLineModel(GitDiffLineKind.Context, oldLine++, newLine++, line[1..]));
                    break;
            }
        }

        Flush(hunks, header, lines);
        return new GitDiffModel(path, oldPath, binary, truncated, notes, hunks);
    }

    private static void Flush(List<GitDiffHunkModel> hunks, string? header, List<GitDiffLineModel>? lines)
    {
        if (header is not null && lines is not null)
        {
            hunks.Add(new GitDiffHunkModel(header, lines));
        }
    }

    private static void AddNote(List<string> notes, string line)
    {
        var note = line switch
        {
            _ when line.StartsWith("new file mode", StringComparison.Ordinal) => "Nouveau fichier",
            _ when line.StartsWith("deleted file mode", StringComparison.Ordinal) => "Fichier supprimé",
            _ when line.StartsWith("old mode ", StringComparison.Ordinal) => $"Ancien mode {line[9..]}",
            _ when line.StartsWith("new mode ", StringComparison.Ordinal) => $"Nouveau mode {line[9..]}",
            _ when line.StartsWith("rename from ", StringComparison.Ordinal) => $"Renommé depuis {line[12..]}",
            _ when line.StartsWith("copy from ", StringComparison.Ordinal) => $"Copié depuis {line[10..]}",
            _ when line.StartsWith("similarity index ", StringComparison.Ordinal) => $"Similarité {line[17..]}",
            _ when line.StartsWith("Binary files ", StringComparison.Ordinal) || line.StartsWith("GIT binary patch", StringComparison.Ordinal) => "Fichier binaire : contenu non affiché",
            _ => null
        };
        if (note is not null)
        {
            notes.Add(note);
        }
    }

    [GeneratedRegex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@")]
    private static partial Regex HunkHeader();
}
