using Cortex.Mediator.Commands;
using Mindflow_backend.Shared.Application.Model;

namespace Mindflow_backend.WellnessContent.Application.Commands;

public class DeleteWellnessExerciseCommand : ICommand<Result>
{
    public int Id { get; set; }
}
