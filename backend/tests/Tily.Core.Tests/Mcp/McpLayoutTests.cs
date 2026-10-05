using System.Text.Json;
using Tily.Core.Context;
using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpLayoutTests
{
    private static readonly JsonElement Layout = JsonSerializer.SerializeToElement(new
    {
        caller = new { workspace = "w1", tab = "t1", pane = "p1" },
        workspaces = new[]
        {
            new
            {
                id = "w1",
                tabs = new[]
                {
                    new { id = "t1", panes = new object[] { new { id = "p1", path = "/Projets/api" }, new { id = "p2", path = "/Projets/api" }, new { id = "p3", path = "/Temp" } } }
                }
            }
        }
    });

    [Fact]
    public void WithBranches_WhenPanesInRepositories_ThenAddsBranchOrNull()
    {
        var result = McpLayout.WithBranches(Layout, path => path.EndsWith("api") ? new GitContextModel(true, "develop", false) : GitContextModel.None);

        var panes = PanesOf(result);
        Assert.Equal("develop", panes[0].GetProperty("branch").GetString());
        Assert.Equal(JsonValueKind.Null, panes[2].GetProperty("branch").ValueKind);
    }

    [Fact]
    public void WithBranches_WhenPanesShareAFolder_ThenResolvesItOnce()
    {
        var calls = 0;

        McpLayout.WithBranches(Layout, _ =>
        {
            calls++;
            return GitContextModel.None;
        });

        Assert.Equal(2, calls);
    }

    [Fact]
    public void WithBranches_WhenHeadDetached_ThenMarksIt()
    {
        var result = McpLayout.WithBranches(Layout, _ => new GitContextModel(true, null, true));

        Assert.True(PanesOf(result)[0].GetProperty("detachedHead").GetBoolean());
    }

    [Fact]
    public void WithBranches_WhenCalled_ThenKeepsCaller()
    {
        var result = McpLayout.WithBranches(Layout, _ => GitContextModel.None);

        Assert.Equal("p1", result.GetProperty("caller").GetProperty("pane").GetString());
    }

    private static JsonElement PanesOf(JsonElement layout) =>
        layout.GetProperty("workspaces")[0].GetProperty("tabs")[0].GetProperty("panes");
}
