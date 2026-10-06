using System.Text.RegularExpressions;
using Cortex.Mediator.Commands;
using Mindflow_backend.AiIntegration.Application.Services;
using Mindflow_backend.Analytics.Application.Services;
using Mindflow_backend.Journal.Application.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Journal.Domain.Entities;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Domain.Repositories;

namespace Mindflow_backend.Journal.Application.Handlers;

public class CreateJournalEntryHandler(
    IBaseRepository<JournalEntry> repository,
    IUnitOfWork unitOfWork,
    IAnalyticsCacheInvalidator cacheInvalidator,
    IJournalSearchIndexer searchIndexer,
    IAiService aiService) : ICommandHandler<CreateJournalEntryCommand, Result<JournalEntryDto>>
{
    // Prefijos: cubren conjugaciones/género ("agotad" -> agotado/agotada) comparando contra
    // tokens completos, nunca contra el texto crudo.
    private static readonly string[] PositiveStems =
        ["feliz", "genial", "excelente", "alegre", "content", "motivad", "logré", "logre",
         "happy", "great", "amazing", "wonderful"];

    private static readonly string[] NegativeStems =
        ["triste", "terribl", "ansios", "estresad", "frustrad", "agotad",
         "sad", "stress", "anxious", "exhaust", "frustrat"];

    // Palabras cortas que calzarían como prefijo de palabras no relacionadas
    // ("mal" en "normal"/"animal", "bien" en "también"), así que exigimos token exacto.
    private static readonly string[] PositiveExact = ["bien", "good"];
    private static readonly string[] NegativeExact = ["mal", "bad"];

    public async Task<Result<JournalEntryDto>> Handle(CreateJournalEntryCommand request, CancellationToken ct)
    {
        var sentiment = request.Sentiment;

        if (string.IsNullOrWhiteSpace(sentiment)
            || string.Equals(sentiment, "auto", StringComparison.OrdinalIgnoreCase))
        {
            sentiment = DetectSentiment($"{request.Content} {request.Title}");
        }

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
        await cacheInvalidator.InvalidateAsync(request.UserId, request.Date, ct);
        await searchIndexer.IndexAsync(entry, ct);

        return Result<JournalEntryDto>.Success(Map(entry));
    }

    private static string DetectSentiment(string text)
    {
        var tokens = Regex.Matches(text.ToLowerInvariant(), @"[\p{L}]+").Select(m => m.Value).ToArray();

        var positives = PositiveStems.Count(stem => tokens.Any(t => t.StartsWith(stem, StringComparison.Ordinal)))
            + PositiveExact.Count(tokens.Contains);
        var negatives = NegativeStems.Count(stem => tokens.Any(t => t.StartsWith(stem, StringComparison.Ordinal)))
            + NegativeExact.Count(tokens.Contains);

        return positives > negatives ? "positive"
             : negatives > positives ? "negative"
             : "neutral";
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
