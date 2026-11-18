using Cortex.Mediator.Commands;
using Mindflow_backend.AiIntegration.Application.Services;
using Mindflow_backend.Journal.Application.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Journal.Domain.Services;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Domain.Repositories;

namespace Mindflow_backend.Journal.Application.Handlers;

public class CreateJournalEntryHandler(
    IBaseRepository<JournalEntry> repository,
    IUnitOfWork unitOfWork,
    IJournalSearchIndexer searchIndexer,
    IAiService aiService) : ICommandHandler<CreateJournalEntryCommand, Result<JournalEntryDto>>
{
    public async Task<Result<JournalEntryDto>> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        var sentiment = request.Sentiment;

        if (SentimentDetector.IsAuto(sentiment))
            sentiment = SentimentDetector.Detect($"{request.Content} {request.Title}");

        var aiResponse = await aiService.GenerateEmpathicResponseAsync(request.Content, sentiment);

        var entry = new JournalEntry
        {
            UserId = request.UserId,
            Date = request.Date,
            Title = request.Title,
            Content = request.Content,
            Sentiment = sentiment,
            Category = request.Category,
            HasPreview = request.Content.Length > 200,
            AiResponse = string.IsNullOrWhiteSpace(aiResponse) ? null : aiResponse
        };

        await repository.AddAsync(entry, ct);
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
        UpdatedAt = e.UpdatedAt
    };
}
