using System.Text.RegularExpressions;

namespace Mindflow_backend.Journal.Domain.Services;

/// <summary>
///     Detección léxica del sentimiento (positive/neutral/negative) usada cuando el cliente
///     envía <c>auto</c>. Es una heurística por palabras, no un modelo de IA.
/// </summary>
public static class SentimentDetector
{
    // Prefijos: cubren conjugaciones/género ("agotad" -> agotado/agotada) comparando contra
    // tokens completos, nunca contra el texto crudo.
    private static readonly string[] PositiveStems =
        ["feliz", "genial", "excelente", "alegre", "content", "motivad", "logré", "logre",
         "happy", "great", "amazing", "wonderful"];

    private static readonly string[] NegativeStems =
        ["triste", "terribl", "ansios", "estresad", "frustrad", "agotad",
         "sad", "stress", "anxious", "exhaust", "frustrat"];

    // Palabras cortas que calzarían como prefijo de palabras no relacionadas
    // ("mal" en "normal"/"animal", "bien" en "también"), así que exigimos token exacto.
    private static readonly string[] PositiveExact = ["bien", "good"];
    private static readonly string[] NegativeExact = ["mal", "bad"];

    public static bool IsAuto(string? sentiment) =>
        string.IsNullOrWhiteSpace(sentiment)
        || string.Equals(sentiment, "auto", StringComparison.OrdinalIgnoreCase);

    public static string Detect(string text)
    {
        var tokens = Regex.Matches(text.ToLowerInvariant(), @"[\p{L}]+").Select(m => m.Value).ToArray();

        var positives = PositiveStems.Count(stem => tokens.Any(t => t.StartsWith(stem, StringComparison.Ordinal)))
            + PositiveExact.Count(tokens.Contains);
        var negatives = NegativeStems.Count(stem => tokens.Any(t => t.StartsWith(stem, StringComparison.Ordinal)))
            + NegativeExact.Count(tokens.Contains);

        return positives > negatives ? "positive"
             : negatives > positives ? "negative"
             : "neutral";
    }
}
