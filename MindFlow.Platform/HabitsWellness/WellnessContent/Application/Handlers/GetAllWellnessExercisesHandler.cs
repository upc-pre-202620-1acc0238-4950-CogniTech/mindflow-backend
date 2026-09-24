using Cortex.Mediator.Queries;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Application.Model;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.WellnessContent.Application.Dtos;
using Mindflow_backend.WellnessContent.Application.Queries;
using Mindflow_backend.WellnessContent.Domain.Entities;

namespace Mindflow_backend.WellnessContent.Application.Handlers;

public class GetAllWellnessExercisesHandler(AppDbContext dbContext)
    : IQueryHandler<GetAllWellnessExercisesQuery, Result<List<WellnessExerciseDto>>>
{
    public async Task<Result<List<WellnessExerciseDto>>> Handle(GetAllWellnessExercisesQuery request, CancellationToken ct)
    {
        IQueryable<WellnessExercise> query = dbContext.Set<WellnessExercise>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(e => e.Type == request.Type);

        var exercises = await query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .ToListAsync(ct);

        return Result<List<WellnessExerciseDto>>.Success(exercises.Select(CreateWellnessExerciseHandler.Map).ToList());
    }
}
