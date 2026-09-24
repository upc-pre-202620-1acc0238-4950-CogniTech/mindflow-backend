using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Mindflow_backend.Journal.Infrastructure.BackgroundServices;

/// <summary>
///     One-time (idempotent) backfill of search tokens for journal entries that existed
///     before US53 shipped. Runs as a background task rather than blocking app startup,
///     so a large backlog can't delay Kestrel from listening / fail a deploy health check.
///     A no-op once every entry has been indexed.
/// </summary>
public class JournalSearchBackfillService(
    IServiceScopeFactory scopeFactory,
    ILogger<JournalSearchBackfillService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var indexer = scope.ServiceProvider.GetRequiredService<IJournalSearchIndexer>();

            var unindexedEntryIds = await context.JournalEntries
                .Where(e => !context.Set<JournalSearchToken>().Any(t => t.EntryId == e.Id))
                .Select(e => e.Id)
                .ToListAsync(stoppingToken);

            foreach (var entryId in unindexedEntryIds)
            {
                var entry = await context.JournalEntries.FirstAsync(e => e.Id == entryId, stoppingToken);
                await indexer.IndexAsync(entry, stoppingToken);
            }

            if (unindexedEntryIds.Count > 0)
                logger.LogInformation("Journal search backfill indexed {Count} pre-existing entries", unindexedEntryIds.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Journal search backfill failed");
        }
    }
}
