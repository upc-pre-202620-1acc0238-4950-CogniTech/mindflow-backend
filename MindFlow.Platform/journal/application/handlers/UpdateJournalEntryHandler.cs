using Cortex.Mediator.Commands;
using Mindflow_backend.Journal.Application.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Journal.Domain.Model;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Domain.Repositories;

namespace Mindflow_backend.Journal.Application.Handlers;

public class UpdateJournalEntryHandler(
    IBaseRepository<JournalEntry> repository,
    IUnitOfWork unitOfWork,
    IJournalSearchIndexer searchIndexer) : ICommandHandler<UpdateJournalEntryCommand, Result<JournalEntryDto>>
{
    public async Task<Result<JournalEntryDto>> Handle(UpdateJournalEntryCommand request, CancellationToken ct)
    {
        var entry = await repository.FindByIdAsync(request.Id, ct);
        if (entry is null)
            return Result<JournalEntryDto>.Failure(
                 JournalError.JournalEntryNotFound, "Entry not found");

        var sentiment = request.Sentiment;
        if (SentimentDetector.IsAuto(sentiment))
            sentiment = SentimentDetector.Detect($"{request.Content} {request.Title}");

        entry.Title = request.Title;
        entry.Content = request.Content;
        entry.Sentiment = sentiment;
        entry.Category = request.Category;
        entry.HasPreview = request.Content.Length > 200; 

        repository.Update(entry);
        await unitOfWork.CompleteAsync(ct);
        await searchIndexer.IndexAsync(entry, ct);

        return Result<JournalEntryDto>.Success(Map(entry));
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