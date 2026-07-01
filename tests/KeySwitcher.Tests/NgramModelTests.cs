using KeySwitcher.Core;
using KeySwitcher.Dictionaries;
using Xunit;

namespace KeySwitcher.Tests;

public class NgramModelTests
{
    private static WordDictionary Dict(params string[] english) =>
        WordDictionary.FromSets(new Dictionary<Language, IEnumerable<string>>
        {
            [Language.English] = english,
        });

    [Fact]
    public void SmallDictionary_IsNotReliable_ReturnsMinusOne()
    {
        var model = NgramModel.Build(Dict("hello", "world"));
        Assert.False(model.IsReliable(Language.English));
        Assert.Equal(-1, model.PlausibilityPercent("hello", Language.English));
    }

    [Fact]
    public void ReliableModel_ScoresKnownWordsAboveGarbage()
    {
        // Низкий порог надёжности, чтобы задействовать модель на небольшом наборе.
        var model = NgramModel.Build(Dict("hello", "help", "hell", "held", "hero"),
            reliableThreshold: 1);

        double known = model.PlausibilityPercent("hello", Language.English); // все биграммы знакомы
        double garbage = model.PlausibilityPercent("ghbdtn", Language.English); // раскладочный «мусор»

        Assert.True(known > garbage,
            $"ожидалось, что осмысленное слово правдоподобнее мусора: {known} vs {garbage}");
        Assert.Equal(100, known);
    }
}
