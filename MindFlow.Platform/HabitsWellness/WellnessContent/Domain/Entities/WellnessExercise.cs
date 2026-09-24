using Mindflow_backend.Shared.Domain.Model.Entities;

namespace Mindflow_backend.WellnessContent.Domain.Entities;

/// <summary>
///     A single piece of wellness content (a breathing pattern like 4-7-8, or a guided
///     micro-meditation) served to the client so it can be edited/published without a
///     client release. See US55.
/// </summary>
public class WellnessExercise : IAuditableEntity
{
    public const string TypeBreathing = "breathing";
    public const string TypeMeditation = "meditation";

    public int Id { get; set; }
    public string Type { get; set; } = TypeBreathing;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }

    // Breathing-only fields (null for meditations)
    public int? InhaleSeconds { get; set; }
    public int? HoldSeconds { get; set; }
    public int? ExhaleSeconds { get; set; }
    public int? HoldAfterExhaleSeconds { get; set; }
    public int? Cycles { get; set; }

    // Meditation-only field (null for breathing exercises)
    public string? AudioUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
