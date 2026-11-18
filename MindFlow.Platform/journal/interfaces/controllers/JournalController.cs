using Cortex.Mediator;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Journal.Application.Commands;
using Mindflow_backend.Journal.Application.Dtos;
using Mindflow_backend.Journal.Application.Queries;
using Mindflow_backend.Journal.Application.Services;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Mindflow_backend.Shared.Infrastructure.Security;
namespace Mindflow_backend.Journal.Interfaces.Controllers;

[ApiController]
[Route("api/v1/journal")]
[Authorize]
public sealed class JournalController(IMediator mediator, IFileStorageService fileStorage, AppDbContext dbContext) : ControllerBase
{
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
    [FromQuery(Name = "_sort")] string? sort,
    [FromQuery(Name = "_order")] string? order,
    [FromQuery(Name = "_limit")] int? limit,
    [FromQuery(Name = "q")] string? q)
    {
        var userId = User.GetUserId();
        var query = new GetJournalEntriesQuery
        {
            UserId = userId,
            Sort = sort,
            Order = order,
            Limit = limit,
            Q = q
        };
        var result = await mediator.QueryAsync(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpGet("entries/{id}")]
    public async Task<IActionResult> GetEntryById(int id)
    {
        var userId = User.GetUserId();
        var query = new GetJournalEntryByIdQuery { Id = id };
        var result = await mediator.QueryAsync(query);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Message });
        if (result.Value!.UserId != userId)
            return NotFound();
        return Ok(result.Value);
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateJournalEntryRequest request)
    {
        var userId = User.GetUserId();
        var command = new CreateJournalEntryCommand
        {
            UserId = userId,
            Date = request.Date,
            Title = request.Title,
            Content = request.Content,
            Sentiment = request.Sentiment,
            Category = request.Category
        };
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPut("entries/{id}")]
    public async Task<IActionResult> UpdateEntry(int id, [FromBody] UpdateJournalEntryRequest request)
    {
        var userId = User.GetUserId();

        var entryQuery = new GetJournalEntryByIdQuery { Id = id };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        var command = new UpdateJournalEntryCommand
        {
            Id = id,
            Title = request.Title,
            Content = request.Content,
            Sentiment = request.Sentiment,
            Category = request.Category
        };
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPost("entries/sync")]
    public async Task<IActionResult> SyncEntries([FromBody] List<SyncJournalEntryItemRequest> items)
    {
        var userId = User.GetUserId();
        var command = new SyncJournalEntriesCommand { UserId = userId, Items = items };
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpDelete("entries/{id}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var userId = User.GetUserId();

        var entryQuery = new GetJournalEntryByIdQuery { Id = id };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        var command = new DeleteJournalEntryCommand { Id = id };
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Message });
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        var userId = User.GetUserId();
        var query = new GetTagsQuery { UserId = userId };
        var result = await mediator.QueryAsync(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPost("tags")]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
    {
        var userId = User.GetUserId();
        var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { error = "El nombre de la etiqueta es obligatorio." });

        var existing = await dbContext.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == name);
        if (existing is not null)
            return Ok(new TagDto { Id = existing.Id, UserId = existing.UserId, Name = existing.Name });

        var tag = new Mindflow_backend.Journal.Domain.Entities.Tag { UserId = userId, Name = name };
        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync();
        return Created($"api/v1/journal/tags/{tag.Id}", new TagDto { Id = tag.Id, UserId = tag.UserId, Name = tag.Name });
    }

    [HttpGet("entry-tags")]
    public async Task<IActionResult> GetEntryTags([FromQuery] int? entryId)
    {
        var userId = User.GetUserId();

        if (entryId.HasValue)
        {
            var entryQuery = new GetJournalEntryByIdQuery { Id = entryId.Value };
            var entryResult = await mediator.QueryAsync(entryQuery);
            if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
                return NotFound();
        }

        var query = new GetEntryTagsQuery { UserId = userId, EntryId = entryId };
        var result = await mediator.QueryAsync(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPost("entry-tags")]
    public async Task<IActionResult> CreateEntryTag([FromBody] CreateEntryTagCommand command)
    {
        var userId = User.GetUserId();

        var entryQuery = new GetJournalEntryByIdQuery { Id = command.EntryId };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        // La etiqueta también debe ser del usuario (evita usar o leer etiquetas ajenas por id)
        var tagIsOwned = await dbContext.Tags.AnyAsync(t => t.Id == command.TagId && t.UserId == userId);
        if (!tagIsOwned)
            return NotFound();

        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpDelete("entry-tags/{id}")]
    public async Task<IActionResult> DeleteEntryTag(int id)
    {
        var userId = User.GetUserId();

        var entryTag = await dbContext.EntryTags
            .AsNoTracking()
            .FirstOrDefaultAsync(et => et.Id == id);

        if (entryTag is null)
            return NotFound();

        var entryQuery = new GetJournalEntryByIdQuery { Id = entryTag.EntryId };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        var command = new DeleteEntryTagCommand { Id = id };
        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Message });
    }

    [HttpGet("media")]
    public async Task<IActionResult> GetMedia([FromQuery] int? entryId)
    {
        var userId = User.GetUserId();

        if (entryId.HasValue)
        {
            var entryQuery = new GetJournalEntryByIdQuery { Id = entryId.Value };
            var entryResult = await mediator.QueryAsync(entryQuery);
            if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
                return NotFound();
        }

        var query = new GetMediaQuery { UserId = userId, EntryId = entryId };
        var result = await mediator.QueryAsync(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPost("media")]
    public async Task<IActionResult> CreateMedia([FromBody] CreateMediaCommand command)
    {
        var userId = User.GetUserId();

        var entryQuery = new GetJournalEntryByIdQuery { Id = command.EntryId };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        var result = await mediator.SendAsync(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
    }

    [HttpPost("media/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia([FromForm] int entryId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No se proporcionó ningún archivo." });

        var userId = User.GetUserId();

        var entryQuery = new GetJournalEntryByIdQuery { Id = entryId };
        var entryResult = await mediator.QueryAsync(entryQuery);
        if (!entryResult.IsSuccess || entryResult.Value!.UserId != userId)
            return NotFound();

        try
        {
            var (url, type) = await fileStorage.SaveAsync(file, userId);
            var command = new CreateMediaCommand { EntryId = entryId, Type = type, Url = url };
            var result = await mediator.SendAsync(command);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateTagRequest([Required, MaxLength(50)] string Name);
