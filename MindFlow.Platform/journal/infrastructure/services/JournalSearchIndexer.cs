using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Mindflow_backend.Journal.Infrastructure.Services;

public class JournalSearchIndexer(AppDbContext dbContext, ISearchTokenHasher hasher) : IJournalSearchIndexer
{
    public async Task IndexAsync(JournalEntry entry, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: the filter joins back to journal_entries, and MySQL rejects
        // a DELETE whose subquery references the same table being deleted from. Filtering
        // on entry.Id directly needs no join anyway.
        await dbContext.Set<JournalSearchToken>()
            .IgnoreQueryFilters()
            .Where(t => t.EntryId == entry.Id)
            .ExecuteDeleteAsync(ct);

        var tokens = JournalSearchTokenizer.Tokenize($"{entry.Title} {entry.Content}");
        if (tokens.Count == 0)
            return;

        var rows = tokens.Select(token => new JournalSearchToken
        {
            EntryId = entry.Id,
            UserId = entry.UserId,
            TokenHash = hasher.Hash(token)
        });

        await dbContext.Set<JournalSearchToken>().AddRangeAsync(rows, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
