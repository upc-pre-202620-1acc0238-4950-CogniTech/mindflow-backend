namespace Mindflow_backend.Journal.Domain.Entities;

/// <summary>
///     A keyed (HMAC) hash of a single normalized word extracted from a journal entry's
///     title/content, used as a blind search index: matching happens on the hash at the
///     database level, so entries that don't match never need to be decrypted.
/// </summary>
public class JournalSearchToken
{
    public int Id { get; set; }
    public int EntryId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;

    public JournalEntry Entry { get; set; } = null!;
}
