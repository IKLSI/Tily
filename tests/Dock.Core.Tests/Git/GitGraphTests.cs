using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitGraphTests
{
    private static GitGraphNodeModel Node(string sha, params string[] parents) => new(sha, parents);

    private static GitGraphSegmentModel Segment(int from, int to, int color, GitSegmentKind kind, bool dashed = false) => new(from, to, color, kind, dashed);

    [Fact]
    public void Layout_WhenLinearHistory_ThenSingleLane()
    {
        var rows = GitGraph.Layout([Node("c", "b"), Node("b", "a"), Node("a")]);

        Assert.All(rows, row => Assert.Equal(0, row.Lane));
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.Out)], rows[0].Segments);
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.In), Segment(0, 0, 0, GitSegmentKind.Out)], rows[1].Segments);
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.In)], rows[2].Segments);
    }

    [Fact]
    public void Layout_WhenMergeCommit_ThenSecondParentGetsOwnLaneAndConverges()
    {
        var rows = GitGraph.Layout([Node("merge", "main", "feature"), Node("main", "base"), Node("feature", "base"), Node("base")]);

        Assert.Equal([0, 0, 1, 0], rows.Select(row => row.Lane));
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.Out), Segment(0, 1, 1, GitSegmentKind.Out)], rows[0].Segments);
        Assert.Contains(Segment(1, 1, 1, GitSegmentKind.Through), rows[1].Segments);
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.In), Segment(1, 0, 1, GitSegmentKind.In)], rows[3].Segments);
        Assert.Equal(2, rows[0].Width);
    }

    [Fact]
    public void Layout_WhenTwoTipsShareParent_ThenSecondTipUsesNextLane()
    {
        var rows = GitGraph.Layout([Node("tip1", "base"), Node("tip2", "base"), Node("base")]);

        Assert.Equal([0, 1, 0], rows.Select(row => row.Lane));
        Assert.NotEqual(rows[0].Color, rows[1].Color);
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.In), Segment(1, 0, 1, GitSegmentKind.In)], rows[2].Segments);
    }

    [Fact]
    public void Layout_WhenLaneFreed_ThenReusesIt()
    {
        var rows = GitGraph.Layout([Node("tip1", "base"), Node("tip2", "base"), Node("base", "root"), Node("tip3", "root"), Node("root")]);

        Assert.Equal(1, rows[3].Lane);
    }

    [Fact]
    public void Layout_WhenDashedNode_ThenItsLaneStaysDashedUntilParent()
    {
        var rows = GitGraph.Layout([new GitGraphNodeModel("wip", ["base"], true), Node("tip", "base"), Node("base")]);

        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.Through, true), Segment(1, 1, 1, GitSegmentKind.Out)], rows[1].Segments);
        Assert.Equal([Segment(0, 0, 0, GitSegmentKind.In, true), Segment(1, 0, 1, GitSegmentKind.In)], rows[2].Segments);
    }
}
