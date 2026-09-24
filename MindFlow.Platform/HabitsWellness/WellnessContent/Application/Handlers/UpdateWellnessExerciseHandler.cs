using Cortex.Mediator.Commands;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.WellnessContent.Application.Commands;
using Mindflow_backend.WellnessContent.Application.Dtos;
using Mindflow_backend.WellnessContent.Domain.Entities;
using Mindflow_backend.WellnessContent.Domain.Model;

namespace Mindflow_backend.WellnessContent.Application.Handlers;

public class UpdateWellnessExerciseHandler(AppDbContext dbContext)
    : ICommandHandler<UpdateWellnessExerciseCommand, Result<WellnessExerciseDto>>
{
    public async Task<Result<WellnessExerciseDto>> Handle(UpdateWellnessExerciseCommand request, CancellationToken ct)
    {
        if (request.Type != WellnessExercise.TypeBreathing && request.Type != WellnessExercise.TypeMeditation)
            return Result<WellnessExerciseDto>.Failure(WellnessContentError.InvalidType,
                $"Type must be '{WellnessExercise.TypeBreathing}' or '{WellnessExercise.TypeMeditation}'.");

        var exercise = await dbContext.Set<WellnessExercise>().FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (exercise is null)
            return Result<WellnessExerciseDto>.Failure(WellnessContentError.ExerciseNotFound, "Exercise not found.");

        exercise.Type = request.Type;
        exercise.Name = request.Name;
        exercise.Description = request.Description;
        exercise.DurationSeconds = request.DurationSeconds;
        exercise.InhaleSeconds = request.InhaleSeconds;
        exercise.HoldSeconds = request.HoldSeconds;
        exercise.ExhaleSeconds = request.ExhaleSeconds;
        exercise.HoldAfterExhaleSeconds = request.HoldAfterExhaleSeconds;
        exercise.Cycles = request.Cycles;
        exercise.AudioUrl = request.AudioUrl;
        exercise.IsActive = request.IsActive;
        exercise.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(ct);

        return Result<WellnessExerciseDto>.Success(CreateWellnessExerciseHandler.Map(exercise));
    }
}
