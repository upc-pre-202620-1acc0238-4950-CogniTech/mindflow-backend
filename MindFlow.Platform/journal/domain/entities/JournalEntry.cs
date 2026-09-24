using Mindflow_backend.Shared.Domain.Model.Entities;

namespace Mindflow_backend.Journal.Domain.Entities;

public class JournalEntry : IAuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    /// <summary>
    ///     Client-generated correlation id for offline-first sync (US54): lets a mobile
    ///     client created this entry while offline and retried the sync request without
    ///     creating duplicates once connectivity is restored.
    /// </summary>
    public string? ClientId { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Sentiment { get; set; } = "neutral";
    public string Category { get; set; } = "Sin categoría";
    public bool HasPreview { get; set; }
    public string? AiResponse { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<EntryTag> EntryTags { get; set; } = [];
    public ICollection<Media> Media { get; set; } = [];
}