using System.Text;
using System.Text.RegularExpressions;

namespace Tily.Core.Git;

public sealed record GitHunkSelectionModel(int Hunk, IReadOnlyList<int> Lines);

public static partial class GitPatchBuilder
{
    private const string StaleSelection = "La sélection ne correspond plus au diff : rechargez-le puis recommencez.";
    private const string NewFileMode = "new file mode ";
    private const string DeletedFileMode = "deleted file mode ";
    private const string NullName = "/dev/null";

    private sealed record RawHunk(int OldStart, int NewStart, string Context, List<string> Lines);

    public static string Build(string diff, IReadOnlyList<GitHunkSelectionModel> selection, bool reverse)
    {
        var (header, hunks) = Split(diff);
        var wanted = Wanted(hunks, selection);
        var total = hunks.Sum(hunk => hunk.Lines.Count(IsChange));
        var whole = wanted.Sum(lines => lines.Count) == total;
        var created = header.Any(line => line.StartsWith(NewFileMode, StringComparison.Ordinal));
        var deleted = header.Any(line => line.StartsWith(DeletedFileMode, StringComparison.Ordinal));
        var fileHeader = !whole && (reverse ? created : deleted) ? AsModification(header) : header;
        var patch = new StringBuilder();
        foreach (var line in fileHeader)
        {
            patch.Append(line).Append('\n');
        }

        var offset = 0;
        for (var index = 0; index < hunks.Count; index++)
        {
            if (wanted[index].Count > 0)
            {
                offset = AppendHunk(patch, hunks[index], wanted[index], reverse, offset);
            }
        }

        return patch.ToString();
    }

    public static int CountLines(IReadOnlyList<GitHunkSelectionModel> selection) =>
        selection.GroupBy(hunk => hunk.Hunk).Sum(group => group.SelectMany(hunk => hunk.Lines).Distinct().Count());

    private static (List<string> Header, List<RawHunk> Hunks) Split(string diff)
    {
        var header = new List<string>();
        var hunks = new List<RawHunk>();
        foreach (var raw in diff.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var match = HunkHeader().Match(line);
            if (match.Success)
            {
                hunks.Add(new RawHunk(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), match.Groups[3].Value, []));
            }
            else if (line.StartsWith("diff --git ", StringComparison.Ordinal) && header.Count > 0)
            {
                throw new GitCommandException(StaleSelection, string.Empty);
            }
            else if (hunks.Count == 0)
            {
                if (line.Length > 0)
                {
                    header.Add(line);
                }
            }
            else if (line.Length > 0)
            {
                hunks[^1].Lines.Add(raw);
            }
        }

        return (header, hunks);
    }

    private static List<HashSet<int>> Wanted(List<RawHunk> hunks, IReadOnlyList<GitHunkSelectionModel> selection)
    {
        var wanted = hunks.Select(_ => new HashSet<int>()).ToList();
        foreach (var hunk in selection)
        {
            foreach (var line in hunk.Lines)
            {
                if (hunk.Hunk < 0 || hunk.Hunk >= hunks.Count || line < 0 || line >= hunks[hunk.Hunk].Lines.Count || !IsChange(hunks[hunk.Hunk].Lines[line]))
                {
                    throw new GitCommandException(StaleSelection, string.Empty);
                }

                wanted[hunk.Hunk].Add(line);
            }
        }

        return wanted.Sum(lines => lines.Count) == 0 ? throw new GitCommandException("Aucune ligne modifiée n’est sélectionnée.", string.Empty) : wanted;
    }

    private static int AppendHunk(StringBuilder patch, RawHunk hunk, HashSet<int> wanted, bool reverse, int offset)
    {
        var lines = new List<string>();
        int oldCount = 0, newCount = 0;
        var kept = false;
        for (var index = 0; index < hunk.Lines.Count; index++)
        {
            var line = hunk.Lines[index];
            if (line[0] == '\\')
            {
                if (kept)
                {
                    lines.Add(line);
                }

                continue;
            }

            var selected = wanted.Contains(index);
            kept = selected || !(line[0] == (reverse ? '-' : '+'));
            if (!kept)
            {
                continue;
            }

            var kind = selected ? line[0] : ' ';
            lines.Add(IsChange(line) && !selected ? $" {line[1..]}" : line);
            oldCount += kind == '+' ? 0 : 1;
            newCount += kind == '-' ? 0 : 1;
        }

        var (anchorStart, anchorCount, otherCount) = reverse ? (hunk.NewStart, newCount, oldCount) : (hunk.OldStart, oldCount, newCount);
        var position = anchorStart - (anchorCount > 0 ? 1 : 0) + offset;
        var otherStart = position + (otherCount > 0 ? 1 : 0);
        var (oldStart, newStart) = reverse ? (otherStart, anchorStart) : (anchorStart, otherStart);
        patch.Append($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@{hunk.Context}\n");
        foreach (var line in lines)
        {
            patch.Append(line).Append('\n');
        }

        return offset + otherCount - anchorCount;
    }

    private static List<string> AsModification(List<string> header)
    {
        var result = header.Where(line => !line.StartsWith(NewFileMode, StringComparison.Ordinal) && !line.StartsWith(DeletedFileMode, StringComparison.Ordinal)).ToList();
        var minus = result.FindIndex(line => line.StartsWith("--- ", StringComparison.Ordinal));
        var plus = result.FindIndex(line => line.StartsWith("+++ ", StringComparison.Ordinal));
        if (minus < 0 || plus < 0)
        {
            throw new GitCommandException(StaleSelection, string.Empty);
        }

        if (result[minus] == $"--- {NullName}")
        {
            result[minus] = $"--- {WithPrefix(result[plus][4..], 'a')}";
        }

        if (result[plus] == $"+++ {NullName}")
        {
            result[plus] = $"+++ {WithPrefix(result[minus][4..], 'b')}";
        }

        return result;
    }

    private static string WithPrefix(string name, char prefix) => name.StartsWith('"') ? $"\"{prefix}{name[2..]}" : $"{prefix}{name[1..]}";

    private static bool IsChange(string line) => line[0] is '+' or '-';

    [GeneratedRegex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@(.*)$")]
    private static partial Regex HunkHeader();
}
