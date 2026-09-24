using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAggregate = Mindflow_backend.iam.domain.model.aggregates.User;
using Mindflow_backend.WellnessContent.Application.Commands;
using Mindflow_backend.WellnessContent.Application.Dtos;
using Mindflow_backend.WellnessContent.Application.Queries;

namespace Mindflow_backend.WellnessContent.Interfaces.Rest.Controllers;

[ApiController]
[Route("wellness/exercises")]
[Authorize]
public sealed class WellnessExercisesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    ///     Public catalog: only exercises an admin has published, ready for the client to render.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveExercises([FromQuery] string? type)
    {
        var result = await mediator.QueryAsync(new GetActiveWellnessExercisesQuery { Type = type });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Message);
    }

    [HttpGet("all")]
    [Authorize(Roles = UserAggregate.AdminRole)]
    public async Task<IActionResult> GetAllExercises([FromQuery] string? type)
    {
        var result = await mediator.QueryAsync(new GetAllWellnessExercisesQuery { Type = type });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Message);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = UserAggregate.AdminRole)]
    public async Task<IActionResult> GetExerciseById(int id)
    {
        var result = await mediator.QueryAsync(new GetWellnessExerciseByIdQuery { Id = id });
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Message);
    }

    [HttpPost]
    [Authorize(Roles = UserAggregate.AdminRole)]
    public async Task<IActionResult> CreateExercise([FromBody] CreateWellnessExerciseRequest request)
    {
        var command = new CreateWellnessExerciseCommand
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
        var result = await mediator.SendAsync(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetExerciseById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Message);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = UserAggregate.AdminRole)]
    public async Task<IActionResult> UpdateExercise(int id, [FromBody] UpdateWellnessExerciseRequest request)
    {
        var command = new UpdateWellnessExerciseCommand
        {
            Id = id,
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
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Message);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserAggregate.AdminRole)]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var result = await mediator.SendAsync(new DeleteWellnessExerciseCommand { Id = id });
        return result.IsSuccess ? Ok() : NotFound(result.Message);
    }
}
