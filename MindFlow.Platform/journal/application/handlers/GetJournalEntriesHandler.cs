using Cortex.Mediator.Queries;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Queries;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Mindflow_backend.Journal.Application.Handlers;

public class GetJournalEntriesHandler(AppDbContext dbContext, ISearchTokenHasher hasher)
    : IQueryHandler<GetJournalEntriesQuery, Result<IEnumerable<JournalEntryDto>>>
{
    public async Task<Result<IEnumerable<JournalEntryDto>>> Handle(GetJournalEntriesQuery request, CancellationToken ct)
    {
        IQueryable<JournalEntry> query = dbContext.JournalEntries
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId)
            .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
            .Include(e => e.Media);

        bool ascending = request.Order?.ToLower() == "asc";

        query = (request.Sort?.ToLower()) switch
        {
            "date" => ascending ? query.OrderBy(e => e.Date) : query.OrderByDescending(e => e.Date),
            _ => ascending ? query.OrderBy(e => e.CreatedAt) : query.OrderByDescending(e => e.CreatedAt)
        };

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            // Content is encrypted at rest: matching happens against a hashed token index
            // at the database level, so entries that don't match are never decrypted.
            var matchingIds = await FindMatchingEntryIdsAsync(request.UserId, request.Q, ct);
            if (matchingIds.Count == 0)
                return Result<IEnumerable<JournalEntryDto>>.Success([]);

            query = query.Where(e => matchingIds.Contains(e.Id));
        }

        if (request.Limit.HasValue)
            query = query.Take(request.Limit.Value);

        var entries = await query.ToListAsync(ct);

        return Result<IEnumerable<JournalEntryDto>>.Success(entries.Select(Map));
    }

    private async Task<List<int>> FindMatchingEntryIdsAsync(int userId, string q, CancellationToken ct)
    {
        var queryTokenHashes = JournalSearchTokenizer.Tokenize(q).Select(hasher.Hash).ToHashSet();
        if (queryTokenHashes.Count == 0)
            return [];

        // Matching runs in memory rather than via a server-side `Contains` + `GroupBy`:
        // MySql.EntityFrameworkCore fails to assign a type mapping to the parameterized IN
        // list in that shape ("Expression '@queryTokenHashes' ... does not have a type
        // mapping assigned"). Rows here are tiny index entries (no encrypted content), so
        // pulling one user's token set is still far cheaper than decrypting every entry.
        var userTokens = await dbContext.Set<JournalSearchToken>()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.EntryId, t.TokenHash })
            .ToListAsync(ct);

        return userTokens
            .Where(t => queryTokenHashes.Contains(t.TokenHash))
            .GroupBy(t => t.EntryId)
            .Where(g => g.Select(x => x.TokenHash).Distinct().Count() == queryTokenHashes.Count)
            .Select(g => g.Key)
            .ToList();
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
        Tags = e.EntryTags?.Select(et => new TagDto
        {
            Id = et.Tag.Id,
            UserId = et.Tag.UserId,
            Name = et.Tag.Name
        }).ToList() ?? [],
        Media = e.Media?.Select(m => new MediaDto
        {
            Id = m.Id,
            EntryId = m.EntryId,
            Type = m.Type,
            Url = m.Url,
            CreatedAt = m.CreatedAt
        }).ToList() ?? [],
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        DeletedAt = e.DeletedAt
    };
}
