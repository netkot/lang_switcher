using KeySwitcher.Core;
using Xunit;

namespace KeySwitcher.Tests;

public class LayoutConverterTests
{
    [Theory]
    [InlineData("ghbdtn", Language.English, Language.Russian, "привет")]
    [InlineData("привет", Language.Russian, Language.English, "ghbdtn")]
    [InlineData("руддщ", Language.Russian, Language.English, "hello")]
    [InlineData("hello", Language.English, Language.Russian, "руддщ")]
    [InlineData("ghbdsn", Language.English, Language.Ukrainian, "привіт")]
    [InlineData("привіт", Language.Ukrainian, Language.English, "ghbdsn")]
    public void Convert_MapsBetweenLayouts(string input, Language from, Language to, string expected)
    {
        Assert.Equal(expected, LayoutConverter.Convert(input, from, to));
    }

    [Fact]
    public void Convert_PreservesCase()
    {
        Assert.Equal("Привет", LayoutConverter.Convert("Ghbdtn", Language.English, Language.Russian));
        Assert.Equal("ПРИВЕТ", LayoutConverter.Convert("GHBDTN", Language.English, Language.Russian));
    }

    [Fact]
    public void Convert_LeavesDigitsAndUnknownCharsUnchanged()
    {
        Assert.Equal("привет123", LayoutConverter.Convert("ghbdtn123", Language.English, Language.Russian));
    }

    [Fact]
    public void Convert_SameLanguage_ReturnsInput()
    {
        Assert.Equal("hello", LayoutConverter.Convert("hello", Language.English, Language.English));
    }

    [Fact]
    public void Convert_RoundTrip_IsStable()
    {
        const string original = "ghbdtn";
        string ru = LayoutConverter.Convert(original, Language.English, Language.Russian);
        string back = LayoutConverter.Convert(ru, Language.Russian, Language.English);
        Assert.Equal(original, back);
    }
}
