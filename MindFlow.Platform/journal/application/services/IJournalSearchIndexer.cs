using Mindflow_backend.Journal.Domain.Entities;

namespace Mindflow_backend.Journal.Application.Services;

public interface IJournalSearchIndexer
{
    /// <summary>
    ///     (Re)indexes an entry's title/content into search tokens, replacing any tokens
    ///     previously stored for it. Call after every create/update.
    /// </summary>
    Task IndexAsync(JournalEntry entry, CancellationToken ct = default);
}
