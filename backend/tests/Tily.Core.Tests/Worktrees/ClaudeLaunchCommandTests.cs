using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class ClaudeLaunchCommandTests
{
    [Fact]
    public void For_WhenNotRequested_ThenNull()
    {
        var command = ClaudeLaunchCommand.For(false, "Corrige le bug");

        Assert.Null(command);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    public void For_WhenPromptBlank_ThenClaudeAlone(string? prompt)
    {
        var command = ClaudeLaunchCommand.For(true, prompt);

        Assert.Equal("claude", command);
    }

    [Fact]
    public void For_WhenPromptOnSeveralLines_ThenFlattensWhitespace()
    {
        var command = ClaudeLaunchCommand.For(true, "  Corrige le bug\r\n\r\n\tpuis   lance les tests  ");

        Assert.Equal("claude 'Corrige le bug puis lance les tests'", command);
    }

    [Fact]
    public void For_WhenPromptHasApostrophes_ThenClosesAndEscapesThemForShell()
    {
        var command = ClaudeLaunchCommand.For(true, "Dis l'API d'être prête");

        Assert.Equal("claude 'Dis l'\\''API d'\\''être prête'", command);
    }

    [Theory]
    [InlineData("Dis \"bonjour\" à $HOME `id` $(id)", "claude 'Dis \"bonjour\" à $HOME `id` $(id)'")]
    [InlineData("Lis a\\b !! # fin", "claude 'Lis a\\b !! # fin'")]
    [InlineData("Garde ‘x’ tel quel", "claude 'Garde ‘x’ tel quel'")]
    public void For_WhenPromptHasShellMetacharacters_ThenKeepsThemInsideSingleQuotes(string prompt, string expected)
    {
        var command = ClaudeLaunchCommand.For(true, prompt);

        Assert.Equal(expected, command);
    }

    [Fact]
    public void For_WhenPromptHasControlCharacters_ThenDropsThem()
    {
        var command = ClaudeLaunchCommand.For(true, "avant\u001b[31m\u0007après");

        Assert.Equal("claude 'avant[31maprès'", command);
    }

    [Fact]
    public void For_WhenPromptStartsWithDash_ThenEndsOptionsFirst()
    {
        var command = ClaudeLaunchCommand.For(true, "--help explique-moi");

        Assert.Equal("claude -- '--help explique-moi'", command);
    }

    [Fact]
    public void For_WhenPromptIsSlashCommand_ThenPassedAsIs()
    {
        var command = ClaudeLaunchCommand.For(true, "/usage");

        Assert.Equal("claude '/usage'", command);
    }

    [Fact]
    public void For_WhenPromptTooLong_ThenFailsInFrench()
    {
        var prompt = new string('a', ClaudeLaunchCommand.PromptMaxLength + 1);

        var failure = Assert.Throws<WorktreeException>(() => ClaudeLaunchCommand.For(true, prompt));

        Assert.Equal($"Le texte pour Claude Code dépasse {ClaudeLaunchCommand.PromptMaxLength} caractères.", failure.Message);
    }
}
