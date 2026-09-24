using Cortex.Mediator.Commands;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.WellnessContent.Application.Commands;
using Mindflow_backend.WellnessContent.Domain.Entities;
using Mindflow_backend.WellnessContent.Domain.Model;

namespace Mindflow_backend.WellnessContent.Application.Handlers;

public class DeleteWellnessExerciseHandler(AppDbContext dbContext)
    : ICommandHandler<DeleteWellnessExerciseCommand, Result>
{
    public async Task<Result> Handle(DeleteWellnessExerciseCommand request, CancellationToken ct)
    {
        var exercise = await dbContext.Set<WellnessExercise>().FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (exercise is null)
            return Result.Failure(WellnessContentError.ExerciseNotFound, "Exercise not found.");

        dbContext.Set<WellnessExercise>().Remove(exercise);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
