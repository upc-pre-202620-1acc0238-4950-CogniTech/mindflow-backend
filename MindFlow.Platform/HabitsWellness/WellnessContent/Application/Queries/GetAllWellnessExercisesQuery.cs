using Cortex.Mediator.Queries;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.WellnessContent.Application.Dtos;

namespace Mindflow_backend.WellnessContent.Application.Queries;

/// <summary>
///     Admin-facing query: includes inactive/unpublished exercises for content management.
/// </summary>
public class GetAllWellnessExercisesQuery : IQuery<Result<List<WellnessExerciseDto>>>
{
    public string? Type { get; set; }
}
