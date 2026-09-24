namespace Mindflow_backend.Journal.Application.Dtos;

public class SyncJournalEntryResultDto
{
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     One of: "created", "updated", "deleted", "conflict_kept_server".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     The consolidated entry state after reconciliation, or null when the item was deleted.
    /// </summary>
    public JournalEntryDto? Entry { get; set; }
}
