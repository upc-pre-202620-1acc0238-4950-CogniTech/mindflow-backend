using Cortex.Mediator.Commands;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Analytics.Application.Services;
using Mindflow_backend.Journal.Application.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Mindflow_backend.Journal.Application.Handlers;

/// <summary>
///     Reconciles a batch of entries a client edited while offline against the server's
///     current state, one item at a time, using last-write-wins on <c>UpdatedAt</c> vs.
///     the client's local edit timestamp. See US54.
/// </summary>
public class SyncJournalEntriesHandler(
    AppDbContext dbContext,
    IAnalyticsCacheInvalidator cacheInvalidator,
    IJournalSearchIndexer searchIndexer)
    : ICommandHandler<SyncJournalEntriesCommand, Result<List<SyncJournalEntryResultDto>>>
{
    public async Task<Result<List<SyncJournalEntryResultDto>>> Handle(SyncJournalEntriesCommand request, CancellationToken ct)
    {
        var results = new List<SyncJournalEntryResultDto>(request.Items.Count);

        foreach (var item in request.Items)
            results.Add(await ReconcileAsync(request.UserId, item, ct));

        return Result<List<SyncJournalEntryResultDto>>.Success(results);
    }

    private async Task<SyncJournalEntryResultDto> ReconcileAsync(int userId, SyncJournalEntryItemRequest item, CancellationToken ct)
    {
        // IgnoreQueryFilters: a soft-deleted entry must still be found by ClientId, otherwise
        // a retried sync batch (network retry, same ClientId) would try to create a duplicate
        // and collide with the (UserId, ClientId) unique index.
        var entry = await dbContext.JournalEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ClientId == item.ClientId, ct);

        if (entry is null)
            return item.Deleted
                ? new SyncJournalEntryResultDto { ClientId = item.ClientId, Action = "deleted" }
                : await CreateAsync(userId, item, ct);

        var serverIsNewerOrEqual = entry.UpdatedAt.HasValue && entry.UpdatedAt.Value >= item.ClientUpdatedAt;
        if (serverIsNewerOrEqual)
            return new SyncJournalEntryResultDto
            {
                ClientId = item.ClientId,
                Action = "conflict_kept_server",
                Entry = entry.DeletedAt is null ? Map(entry) : null
            };

        if (item.Deleted)
            return await SoftDeleteAsync(userId, entry, item.ClientId, ct);

        return await ApplyClientUpdateAsync(userId, entry, item, ct);
    }

    private async Task<SyncJournalEntryResultDto> CreateAsync(int userId, SyncJournalEntryItemRequest item, CancellationToken ct)
    {
        var entry = new JournalEntry
        {
            UserId = userId,
            ClientId = item.ClientId,
            Date = item.Date,
            Title = item.Title,
            Content = item.Content,
            Sentiment = item.Sentiment,
            Category = item.Category,
            HasPreview = item.Content.Length > 200
        };

        dbContext.JournalEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
        await cacheInvalidator.InvalidateAsync(userId, entry.Date, ct);
        await searchIndexer.IndexAsync(entry, ct);

        return new SyncJournalEntryResultDto { ClientId = item.ClientId, Action = "created", Entry = Map(entry) };
    }

    private async Task<SyncJournalEntryResultDto> SoftDeleteAsync(int userId, JournalEntry entry, string clientId, CancellationToken ct)
    {
        if (entry.DeletedAt is null)
        {
            entry.DeletedAt = DateTimeOffset.UtcNow;
            dbContext.JournalEntries.Update(entry);
            await dbContext.SaveChangesAsync(ct);
            await cacheInvalidator.InvalidateAsync(userId, entry.Date, ct);
        }

        return new SyncJournalEntryResultDto { ClientId = clientId, Action = "deleted" };
    }

    private async Task<SyncJournalEntryResultDto> ApplyClientUpdateAsync(
        int userId, JournalEntry entry, SyncJournalEntryItemRequest item, CancellationToken ct)
    {
        entry.DeletedAt = null; // resurrects the entry if the client's edit is newer than a prior delete
        entry.Date = item.Date;
        entry.Title = item.Title;
        entry.Content = item.Content;
        entry.Sentiment = item.Sentiment;
        entry.Category = item.Category;
        entry.HasPreview = item.Content.Length > 200;

        dbContext.JournalEntries.Update(entry);
        await dbContext.SaveChangesAsync(ct);
        await cacheInvalidator.InvalidateAsync(userId, entry.Date, ct);
        await searchIndexer.IndexAsync(entry, ct);

        return new SyncJournalEntryResultDto { ClientId = item.ClientId, Action = "updated", Entry = Map(entry) };
    }

    private static JournalEntryDto Map(JournalEntry e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        Date = e.Date,
        Title = e.Title,
        Content = e.Content,
        Sentiment = e.Sentiment,
        Category = e.Category,
        HasPreview = e.HasPreview,
        AiResponse = e.AiResponse,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        DeletedAt = e.DeletedAt
    };
}
