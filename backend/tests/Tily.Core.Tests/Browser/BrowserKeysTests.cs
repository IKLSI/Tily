using Tily.Core.Browser;
using Xunit;

namespace Tily.Core.Tests.Browser;

public sealed class BrowserKeysTests
{
    [Fact]
    public void Parse_WhenEnter_ThenKeyWithCarriageReturnText()
    {
        var key = BrowserKeys.Parse("Enter");

        Assert.Equal(new BrowserKeyModel("Enter", "Enter", 13, 0, "\r"), key);
    }

    [Fact]
    public void Parse_WhenLowercaseLetter_ThenCodeAndText()
    {
        var key = BrowserKeys.Parse("a");

        Assert.Equal(new BrowserKeyModel("a", "KeyA", 65, 0, "a"), key);
    }

    [Fact]
    public void Parse_WhenControlLetter_ThenNoTextAndControlModifier()
    {
        var key = BrowserKeys.Parse("Ctrl+A");

        Assert.Equal(new BrowserKeyModel("a", "KeyA", 65, 2, null), key);
    }

    [Fact]
    public void Parse_WhenSeveralModifiers_ThenMaskCombined()
    {
        var key = BrowserKeys.Parse("Cmd+Shift+Enter");

        Assert.Equal(12, key.Modifiers);
    }

    [Fact]
    public void Parse_WhenDigit_ThenDigitCode()
    {
        var key = BrowserKeys.Parse("7");

        Assert.Equal("Digit7", key.Code);
    }

    [Theory]
    [InlineData("arrowdown", "ArrowDown")]
    [InlineData("ESCAPE", "Escape")]
    public void Parse_WhenCaseDiffers_ThenKnownKey(string chord, string expected)
    {
        var key = BrowserKeys.Parse(chord);

        Assert.Equal(expected, key.Key);
    }

    [Theory]
    [InlineData(null, "Indiquez la touche")]
    [InlineData("  ", "Indiquez la touche")]
    [InlineData("Hyper+A", "Modificateur inconnu")]
    [InlineData("Ctrl+Foo", "Touche inconnue")]
    public void Parse_WhenInvalid_ThenFailsInFrench(string? chord, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => BrowserKeys.Parse(chord));

        Assert.StartsWith(expected, error.Message);
    }
}
