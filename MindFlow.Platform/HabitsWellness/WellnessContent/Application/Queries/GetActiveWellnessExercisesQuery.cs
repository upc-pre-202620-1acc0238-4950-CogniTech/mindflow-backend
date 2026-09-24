using Cortex.Mediator.Queries;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.WellnessContent.Application.Dtos;

namespace Mindflow_backend.WellnessContent.Application.Queries;

/// <summary>
///     Public-facing query: only exercises an admin has marked active, ordered for display.
/// </summary>
public class GetActiveWellnessExercisesQuery : IQuery<Result<List<WellnessExerciseDto>>>
{
    public string? Type { get; set; }
}
