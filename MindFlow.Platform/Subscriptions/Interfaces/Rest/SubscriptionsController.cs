using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.Subscriptions.Domain.Entities;
namespace Mindflow_backend.Subscriptions.Interfaces.Rest;
[ApiController, Route("api/v1/subscriptions")]
public sealed class SubscriptionsController(AppDbContext db) : ControllerBase
{
    [HttpGet("plans"), AllowAnonymous] public IActionResult Plans() => Ok(new[] { new { id = "freemium", price = 0m, currency = "USD", interval = (string?)null }, new { id = "premium", price = 4.99m, currency = "USD", interval = (string?)"month" } });
    [HttpGet("me"), Authorize] public async Task<IActionResult> Mine(CancellationToken ct) { var userId = Id(); return Ok(await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? new Subscription { UserId = userId }); }
    [HttpPost("demo/plan/{plan}"), Authorize] public async Task<IActionResult> DemoPlan(string plan, CancellationToken ct) { if (plan is not ("freemium" or "premium")) return BadRequest(); var userId = Id(); var sub = await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == userId, ct); if (sub is null) { sub = new Subscription { UserId = userId }; db.Subscriptions.Add(sub); } sub.Plan = plan; sub.Status = "active"; await db.SaveChangesAsync(ct); return Ok(sub); }
    [HttpPost("checkout"), Authorize] public IActionResult Checkout() => StatusCode(501, new { error = "Stripe adapter requires deployment secrets and Price IDs." });
    private int Id() => int.Parse(User.FindFirst("user_id")!.Value);
}
