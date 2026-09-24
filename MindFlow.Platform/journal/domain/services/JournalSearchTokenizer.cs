using System.Globalization;
using System.Text;

namespace Mindflow_backend.Journal.Domain.Services;

/// <summary>
///     Normalizes free text into a set of indexable words: lowercased, accent-stripped,
///     split on non letter/digit boundaries, de-duplicated. Used identically when
///     indexing an entry and when parsing a search query, so both sides hash the same way.
/// </summary>
public static class JournalSearchTokenizer
{
    private const int MinTokenLength = 2;

    public static IReadOnlySet<string> Tokenize(string? text)
    {
        var tokens = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(text))
            return tokens;

        var normalized = RemoveDiacritics(text.ToLowerInvariant());
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length >= MinTokenLength)
                tokens.Add(buffer.ToString());
            buffer.Clear();
        }

        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
                buffer.Append(c);
            else
                Flush();
        }
        Flush();

        return tokens;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
