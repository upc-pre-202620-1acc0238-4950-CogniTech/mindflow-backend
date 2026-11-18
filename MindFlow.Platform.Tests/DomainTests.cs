using Mindflow_backend.Habits.Application.Internal.CommandServices;
using Mindflow_backend.Habits.Domain.Model.ValueObjects;
using Mindflow_backend.Habits.Interfaces.Rest.Assemblers;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Journal.Infrastructure.Services;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Encryption;
using Xunit;

namespace MindFlow.Platform.Tests;

public class StreakTests
{
    private static List<DateTime> Days(params int[] daysAgo) =>
        daysAgo.Select(d => DateTime.UtcNow.Date.AddDays(-d)).OrderByDescending(d => d).ToList();

    [Fact]
    public void Daily_streak_counts_consecutive_days_up_to_today() =>
        Assert.Equal(3, HabitLogCommandService.ComputeStreak(Days(0, 1, 2), HabitFrequency.Daily));

    [Fact]
    public void Daily_streak_survives_if_only_yesterday_is_logged() =>
        Assert.Equal(2, HabitLogCommandService.ComputeStreak(Days(1, 2), HabitFrequency.Daily));

    [Fact]
    public void Daily_streak_resets_after_a_gap() =>
        Assert.Equal(0, HabitLogCommandService.ComputeStreak(Days(2, 3), HabitFrequency.Daily));

    [Fact]
    public void Daily_streak_stops_at_the_first_gap() =>
        Assert.Equal(2, HabitLogCommandService.ComputeStreak(Days(0, 1, 3), HabitFrequency.Daily));

    [Fact]
    public void Empty_logs_give_zero() =>
        Assert.Equal(0, HabitLogCommandService.ComputeStreak([], HabitFrequency.Weekly));

    [Fact]
    public void Weekly_streak_counts_consecutive_weeks() =>
        Assert.Equal(2, HabitLogCommandService.ComputeStreak(Days(0, 7), HabitFrequency.Weekly));
}

public class SentimentDetectorTests
{
    [Theory]
    [InlineData("Hoy me siento feliz", "positive")]
    [InlineData("Estoy muy triste y agotado", "negative")]
    [InlineData("Fue un dia normal", "neutral")]          // "mal" dentro de "normal" no cuenta
    [InlineData("Lo hice también", "neutral")]            // "bien" dentro de "también" no cuenta
    [InlineData("Me fue bien", "positive")]
    [InlineData("Me fue mal", "negative")]
    public void Detects_sentiment_from_tokens(string text, string expected) =>
        Assert.Equal(expected, SentimentDetector.Detect(text));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("AUTO", true)]
    [InlineData("positive", false)]
    public void IsAuto_recognises_the_auto_marker(string? value, bool expected) =>
        Assert.Equal(expected, SentimentDetector.IsAuto(value));
}

public class HabitFrequencyParserTests
{
    [Theory]
    [InlineData("Daily", HabitFrequency.Daily)]
    [InlineData("daily", HabitFrequency.Daily)]
    [InlineData("Diario", HabitFrequency.Daily)]
    [InlineData("Semanal", HabitFrequency.Weekly)]
    [InlineData("Weekly", HabitFrequency.Weekly)]
    [InlineData("Mensual", HabitFrequency.Monthly)]
    public void Accepts_english_and_spanish_values(string value, HabitFrequency expected) =>
        Assert.Equal(expected, HabitFrequencyParser.Parse(value));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Cada rato")]
    public void Rejects_unknown_values(string? value) =>
        Assert.Throws<ArgumentException>(() => HabitFrequencyParser.Parse(value));
}

public class EncryptionAndSearchTests
{
    [Fact]
    public void Aes_round_trips_and_uses_a_random_iv()
    {
        var key = Convert.ToBase64String(new byte[32].Select((_, i) => (byte)i).ToArray());
        var aes = new AesEncryptionService(key);
        var a = aes.Encrypt("Hoy me sentí con ansiedad");
        var b = aes.Encrypt("Hoy me sentí con ansiedad");
        Assert.NotEqual(a, b);
        Assert.Equal("Hoy me sentí con ansiedad", aes.Decrypt(a));
    }

    [Fact]
    public void Aes_rejects_short_keys() =>
        Assert.Throws<ArgumentException>(() => new AesEncryptionService(Convert.ToBase64String(new byte[16])));

    [Fact]
    public void Tokenizer_ignores_accents_case_and_duplicates()
    {
        var tokens = JournalSearchTokenizer.Tokenize("Ansiedad ANSIEDAD, estrés!");
        Assert.Contains("ansiedad", tokens);
        Assert.Contains("estres", tokens);
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void Search_hash_is_deterministic_per_key_and_differs_across_keys()
    {
        var h1 = new SearchTokenHasher(SearchTokenHasher.GenerateKey());
        var h2 = new SearchTokenHasher(SearchTokenHasher.GenerateKey());
        Assert.Equal(h1.Hash("ansiedad"), h1.Hash("ansiedad"));
        Assert.NotEqual(h1.Hash("ansiedad"), h2.Hash("ansiedad"));
    }
}
