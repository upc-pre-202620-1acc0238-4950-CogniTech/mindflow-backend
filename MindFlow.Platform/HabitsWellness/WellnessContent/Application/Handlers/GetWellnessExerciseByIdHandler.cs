using Cortex.Mediator.Queries;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.WellnessContent.Application.Dtos;
using Mindflow_backend.WellnessContent.Application.Queries;
using Mindflow_backend.WellnessContent.Domain.Entities;
using Mindflow_backend.WellnessContent.Domain.Model;

namespace Mindflow_backend.WellnessContent.Application.Handlers;

public class GetWellnessExerciseByIdHandler(AppDbContext dbContext)
    : IQueryHandler<GetWellnessExerciseByIdQuery, Result<WellnessExerciseDto>>
{
    public async Task<Result<WellnessExerciseDto>> Handle(GetWellnessExerciseByIdQuery request, CancellationToken ct)
    {
        var exercise = await dbContext.Set<WellnessExercise>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        if (exercise is null)
            return Result<WellnessExerciseDto>.Failure(WellnessContentError.ExerciseNotFound, "Exercise not found.");

        return Result<WellnessExerciseDto>.Success(CreateWellnessExerciseHandler.Map(exercise));
    }
}
