using KeySwitcher.Core;
using KeySwitcher.Dictionaries;
using Xunit;

namespace KeySwitcher.Tests;

public class LayoutDetectorTests
{
    private static LayoutDetector CreateDetector()
    {
        var dict = WordDictionary.FromSets(new Dictionary<Language, IEnumerable<string>>
        {
            [Language.English] = new[] { "hello", "world" },
            [Language.Russian] = new[] { "привет", "мир" },
            [Language.Ukrainian] = new[] { "привіт", "світ" },
        });
        return new LayoutDetector(dict);
    }

    [Fact]
    public void Detect_WrongLayoutRussianTypedAsLatin_SuggestsRussian()
    {
        var d = CreateDetector();
        var r = d.Detect("ghbdtn", Language.English);
        Assert.True(r.ShouldConvert);
        Assert.Equal(Language.Russian, r.Target);
        Assert.Equal(DetectionConfidence.Dictionary, r.Confidence);
    }

    [Fact]
    public void Detect_WrongLayoutEnglishTypedAsCyrillic_SuggestsEnglish()
    {
        var d = CreateDetector();
        var r = d.Detect("руддщ", Language.Russian);
        Assert.True(r.ShouldConvert);
        Assert.Equal(Language.English, r.Target);
    }

    [Fact]
    public void Detect_UkrainianWord_SuggestsUkrainian()
    {
        var d = CreateDetector();
        var r = d.Detect("ghbdsn", Language.English); // -> привіт
        Assert.True(r.ShouldConvert);
        Assert.Equal(Language.Ukrainian, r.Target);
    }

    [Fact]
    public void Detect_ValidWordInCurrentLanguage_NoChange()
    {
        var d = CreateDetector();
        var r = d.Detect("привет", Language.Russian);
        Assert.False(r.ShouldConvert);
    }

    [Fact]
    public void Detect_ValidEnglishWord_NoChange()
    {
        var d = CreateDetector();
        var r = d.Detect("hello", Language.English);
        Assert.False(r.ShouldConvert);
    }

    [Fact]
    public void Detect_ShortInput_NoChange()
    {
        var d = CreateDetector();
        Assert.False(d.Detect("a", Language.English).ShouldConvert);
    }

    [Fact]
    public void PickManualTarget_PrefersDictionaryMatch()
    {
        var d = CreateDetector();
        Assert.Equal(Language.Russian, d.PickManualTarget("ghbdtn", Language.English));
        Assert.Equal(Language.English, d.PickManualTarget("руддщ", Language.Russian));
    }
}
