using System.Text.Json.Serialization;

namespace Dock.Core.Git;

[JsonConverter(typeof(JsonStringEnumConverter<GitSegmentKind>))]
public enum GitSegmentKind
{
    [JsonStringEnumMemberName("through")] Through,
    [JsonStringEnumMemberName("in")] In,
    [JsonStringEnumMemberName("out")] Out
}

public sealed record GitGraphNodeModel(string Sha, IReadOnlyList<string> Parents, bool Dashed = false);

public sealed record GitGraphSegmentModel(int From, int To, int Color, GitSegmentKind Kind, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Dashed = false);

public sealed record GitGraphRowModel(int Lane, int Color, int Width, IReadOnlyList<GitGraphSegmentModel> Segments);

public static class GitGraph
{
    public const int PaletteSize = 8;

    public static IReadOnlyList<GitGraphRowModel> Layout(IReadOnlyList<GitGraphNodeModel> nodes)
    {
        var lanes = new List<Lane?>();
        var nextColor = 0;
        var rows = new List<GitGraphRowModel>(nodes.Count);
        foreach (var node in nodes)
        {
            var before = lanes.ToArray();
            var matches = Enumerable.Range(0, before.Length).Where(index => before[index]?.Sha == node.Sha).ToList();
            int lane;
            int color;
            if (matches.Count == 0)
            {
                lane = Claim(lanes);
                color = nextColor++ % PaletteSize;
            }
            else
            {
                lane = matches[0];
                color = before[lane]!.Color;
            }

            var segments = new List<GitGraphSegmentModel>();
            for (var index = 0; index < before.Length; index++)
            {
                if (before[index] is { } passing)
                {
                    segments.Add(matches.Contains(index)
                        ? new GitGraphSegmentModel(index, lane, passing.Color, GitSegmentKind.In, passing.Dashed)
                        : new GitGraphSegmentModel(index, index, passing.Color, GitSegmentKind.Through, passing.Dashed));
                }
            }

            foreach (var index in matches)
            {
                lanes[index] = null;
            }

            if (node.Parents.Count > 0)
            {
                lanes[lane] = new Lane(node.Parents[0], color, node.Dashed);
                segments.Add(new GitGraphSegmentModel(lane, lane, color, GitSegmentKind.Out, node.Dashed));
            }

            foreach (var parent in node.Parents.Skip(1))
            {
                var target = lanes.FindIndex(candidate => candidate?.Sha == parent);
                if (target < 0)
                {
                    target = Claim(lanes);
                    lanes[target] = new Lane(parent, nextColor++ % PaletteSize, false);
                }

                segments.Add(new GitGraphSegmentModel(lane, target, lanes[target]!.Color, GitSegmentKind.Out));
            }

            var width = Math.Max(before.Length, lanes.Count);
            while (lanes.Count > 0 && lanes[^1] is null)
            {
                lanes.RemoveAt(lanes.Count - 1);
            }

            rows.Add(new GitGraphRowModel(lane, color, Math.Max(width, lane + 1), segments));
        }

        return rows;
    }

    private static int Claim(List<Lane?> lanes)
    {
        var free = lanes.IndexOf(null);
        if (free >= 0)
        {
            return free;
        }

        lanes.Add(null);
        return lanes.Count - 1;
    }

    private sealed record Lane(string Sha, int Color, bool Dashed);
}
