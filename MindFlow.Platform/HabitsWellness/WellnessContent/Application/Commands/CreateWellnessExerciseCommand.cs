using Cortex.Mediator.Commands;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.WellnessContent.Application.Dtos;

namespace Mindflow_backend.WellnessContent.Application.Commands;

public class CreateWellnessExerciseCommand : ICommand<Result<WellnessExerciseDto>>
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int? InhaleSeconds { get; set; }
    public int? HoldSeconds { get; set; }
    public int? ExhaleSeconds { get; set; }
    public int? HoldAfterExhaleSeconds { get; set; }
    public int? Cycles { get; set; }
    public string? AudioUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
