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

    [Fact]
    public void Backoff_GivesPartialCreditForUnseenTrigramWithSeenBigram()
    {
        // Обучаем на "abc"/"bcd". Слово "abd": триграммы ^^a,^ab знакомы (кредит 1),
        // abd незнакома и биграмма "bd" тоже (0), bd$ незнакома, но биграмма "d$" знакома
        // (откат 0.4). Итог: (1+1+0+0.4)/4 = 60%.
        var model = NgramModel.Build(Dict("abc", "bcd"), reliableThreshold: 1);
        double score = model.PlausibilityPercent("abd", Language.English);

        Assert.Equal(60.0, score, precision: 6);
        Assert.InRange(score, 0.1, 99.9); // строго между «мусором» и полным совпадением
    }
}
