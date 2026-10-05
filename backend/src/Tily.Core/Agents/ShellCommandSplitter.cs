using System.Text;
using System.Text.RegularExpressions;

namespace Tily.Core.Agents;

public static class ShellCommandSplitter
{
    private const string LineBreak = "\n";

    private static readonly Regex Token = new("""
        \G(?:(?<heredoc>(?<!<)<<(?<indented>-)?\s*(?:'(?<delimiter>[^']+)'|"(?<delimiter>[^"]+)"|\\?(?<delimiter>[A-Za-z0-9_]+)))|(?<comment>(?<![^\s;&|(])\#[^\n]*)|(?<separator>&&|\|\||\|&|;;?|\n|(?<![<>])&(?!>)|(?<!>)\|)|'[^']*'|"(?:[^"\\]|\\.)*"|\\.|[^'"\\<\#&|;\n]+|.)
        """.Trim(), RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private sealed record Heredoc(string Delimiter, bool Indented);

    public static IReadOnlyList<string> Split(string command)
    {
        var segments = new List<string>();
        var segment = new StringBuilder();
        var heredocs = new Queue<Heredoc>();
        var index = 0;
        while (index < command.Length)
        {
            var token = Token.Match(command, index);
            index += Math.Max(token.Length, 1);
            if (token.Groups["separator"].Success)
            {
                segments.Add(segment.ToString());
                segment.Clear();
                if (token.Value == LineBreak)
                {
                    index = SkipHeredocBodies(command, index, heredocs);
                }

                continue;
            }

            if (token.Groups["heredoc"].Success)
            {
                heredocs.Enqueue(new Heredoc(token.Groups["delimiter"].Value, token.Groups["indented"].Success));
            }

            if (!token.Groups["comment"].Success)
            {
                segment.Append(token.Value);
            }
        }

        segments.Add(segment.ToString());
        return segments;
    }

    private static int SkipHeredocBodies(string command, int index, Queue<Heredoc> heredocs)
    {
        while (heredocs.TryDequeue(out var heredoc))
        {
            int end;
            string line;
            do
            {
                end = command.IndexOf('\n', index);
                line = end < 0 ? command[index..] : command[index..end];
                index = end < 0 ? command.Length : end + 1;
                if (heredoc.Indented)
                {
                    line = line.TrimStart('\t');
                }
            }
            while (line.TrimEnd('\r') != heredoc.Delimiter && end >= 0);
        }

        return index;
    }
}
