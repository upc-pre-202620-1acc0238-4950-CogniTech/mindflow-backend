using Cortex.Mediator.Queries;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.WellnessContent.Application.Dtos;

namespace Mindflow_backend.WellnessContent.Application.Queries;

public class GetWellnessExerciseByIdQuery : IQuery<Result<WellnessExerciseDto>>
{
    public int Id { get; set; }
}
