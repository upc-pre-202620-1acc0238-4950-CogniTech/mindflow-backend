using System.ComponentModel.DataAnnotations;

namespace Mindflow_backend.Journal.Application.Dtos;

public class UpdateJournalEntryRequest
{
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(20000)] public string Content { get; set; } = string.Empty;
    [RegularExpression("(?i)^(auto|positive|neutral|negative)$", ErrorMessage = "sentiment debe ser auto, positive, neutral o negative.")]
    public string Sentiment { get; set; } = "neutral";
    [MaxLength(100)] public string Category { get; set; } = "Sin categoría";
}
