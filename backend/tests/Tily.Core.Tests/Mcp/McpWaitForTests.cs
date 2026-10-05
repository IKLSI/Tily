using System.Text.Json;
using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpWaitForTests
{
    [Fact]
    public void AnswerTimeout_WhenTimeoutGiven_ThenTimeoutPlusMargin()
    {
        var arguments = JsonSerializer.SerializeToElement(new { pane = "p1", timeoutSeconds = 30 });

        var timeout = McpWaitFor.AnswerTimeout(arguments);

        Assert.Equal(TimeSpan.FromSeconds(35), timeout);
    }

    [Fact]
    public void AnswerTimeout_WhenTimeoutMissing_ThenDefaultPlusMargin()
    {
        var arguments = JsonSerializer.SerializeToElement(new { pane = "p1" });

        var timeout = McpWaitFor.AnswerTimeout(arguments);

        Assert.Equal(TimeSpan.FromSeconds(McpWaitFor.DefaultTimeoutSeconds + 5), timeout);
    }

    [Fact]
    public void AnswerTimeout_WhenNoArguments_ThenDefaultPlusMargin()
    {
        var timeout = McpWaitFor.AnswerTimeout(null);

        Assert.Equal(TimeSpan.FromSeconds(McpWaitFor.DefaultTimeoutSeconds + 5), timeout);
    }

    [Fact]
    public void AnswerTimeout_WhenTimeoutTooLong_ThenBoundedByMaximum()
    {
        var arguments = JsonSerializer.SerializeToElement(new { timeoutSeconds = 86_400 });

        var timeout = McpWaitFor.AnswerTimeout(arguments);

        Assert.Equal(TimeSpan.FromSeconds(McpWaitFor.MaxTimeoutSeconds + 5), timeout);
    }

    [Fact]
    public void AnswerTimeout_WhenTimeoutNotANumber_ThenDefaultPlusMargin()
    {
        var arguments = JsonSerializer.SerializeToElement(new { timeoutSeconds = "longtemps" });

        var timeout = McpWaitFor.AnswerTimeout(arguments);

        Assert.Equal(TimeSpan.FromSeconds(McpWaitFor.DefaultTimeoutSeconds + 5), timeout);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(45, 45)]
    [InlineData(301, 300)]
    public void TimeoutSeconds_WhenRequested_ThenBetweenOneAndMaximum(int requested, int expected)
    {
        var seconds = McpWaitFor.TimeoutSeconds(requested);

        Assert.Equal(expected, seconds);
    }
}
