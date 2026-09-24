using Cortex.Mediator.Commands;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.WellnessContent.Application.Commands;
using Mindflow_backend.WellnessContent.Application.Dtos;
using Mindflow_backend.WellnessContent.Domain.Entities;
using Mindflow_backend.WellnessContent.Domain.Model;

namespace Mindflow_backend.WellnessContent.Application.Handlers;

public class CreateWellnessExerciseHandler(AppDbContext dbContext)
    : ICommandHandler<CreateWellnessExerciseCommand, Result<WellnessExerciseDto>>
{
    public async Task<Result<WellnessExerciseDto>> Handle(CreateWellnessExerciseCommand request, CancellationToken ct)
    {
        if (request.Type != WellnessExercise.TypeBreathing && request.Type != WellnessExercise.TypeMeditation)
            return Result<WellnessExerciseDto>.Failure(WellnessContentError.InvalidType,
                $"Type must be '{WellnessExercise.TypeBreathing}' or '{WellnessExercise.TypeMeditation}'.");

        var exercise = new WellnessExercise
        {
            Type = request.Type,
            Name = request.Name,
            Description = request.Description,
            DurationSeconds = request.DurationSeconds,
            InhaleSeconds = request.InhaleSeconds,
            HoldSeconds = request.HoldSeconds,
            ExhaleSeconds = request.ExhaleSeconds,
            HoldAfterExhaleSeconds = request.HoldAfterExhaleSeconds,
            Cycles = request.Cycles,
            AudioUrl = request.AudioUrl,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        dbContext.Set<WellnessExercise>().Add(exercise);
        await dbContext.SaveChangesAsync(ct);

        return Result<WellnessExerciseDto>.Success(Map(exercise));
    }

    internal static WellnessExerciseDto Map(WellnessExercise e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        Name = e.Name,
        Description = e.Description,
        DurationSeconds = e.DurationSeconds,
        InhaleSeconds = e.InhaleSeconds,
        HoldSeconds = e.HoldSeconds,
        ExhaleSeconds = e.ExhaleSeconds,
        HoldAfterExhaleSeconds = e.HoldAfterExhaleSeconds,
        Cycles = e.Cycles,
        AudioUrl = e.AudioUrl,
        IsActive = e.IsActive,
        SortOrder = e.SortOrder,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
