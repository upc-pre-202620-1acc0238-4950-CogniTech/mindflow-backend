using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Mindflow_backend.Notifications.Interfaces.Rest;

[ApiController, Route("api/v1/notifications"), Authorize]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.Notifications.Where(n => n.UserId == UserId()).OrderByDescending(n => n.CreatedAt).ToListAsync(ct));
    [HttpGet("unread-count")] public async Task<IActionResult> Count(CancellationToken ct) => Ok(new { count = await db.Notifications.CountAsync(n => n.UserId == UserId() && n.ReadAt == null, ct) });
    [HttpPost("{id:int}/read")] public async Task<IActionResult> Read(int id, CancellationToken ct) { var item = await db.Notifications.SingleOrDefaultAsync(n => n.Id == id && n.UserId == UserId(), ct); if (item is null) return NotFound(); item.ReadAt ??= DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpPost("read-all")] public async Task<IActionResult> ReadAll(CancellationToken ct) { await db.Notifications.Where(n => n.UserId == UserId() && n.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow), ct); return NoContent(); }
    private int UserId() => int.Parse(User.FindFirst("user_id")!.Value);
}
