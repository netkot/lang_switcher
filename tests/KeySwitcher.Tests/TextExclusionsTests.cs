using KeySwitcher.Core;
using Xunit;

namespace KeySwitcher.Tests;

public class TextExclusionsTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("www.example.com")]
    [InlineData("user@example.com")]
    [InlineData("C:\\Users\\Kostja\\file.txt")]
    [InlineData("some/relative/path")]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldSkip_ExcludesUrlsEmailsAndPaths(string text)
    {
        Assert.True(TextExclusions.ShouldSkip(text));
    }

    [Theory]
    [InlineData("ghbdtn")]
    [InlineData("привет")]
    [InlineData("hello world")]
    [InlineData("just.a.sentence")] // точки без слэша/схемы — обычный текст, конвертируем
    public void ShouldSkip_AllowsOrdinaryText(string text)
    {
        Assert.False(TextExclusions.ShouldSkip(text));
    }
}
