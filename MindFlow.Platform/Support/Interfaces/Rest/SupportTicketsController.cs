using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Notifications.Application.Services;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.Support.Domain.Entities;

namespace Mindflow_backend.Support.Interfaces.Rest;
public record CreateTicketResource(string Subject, string Category, string? Message, string Priority = "normal"); public record MessageResource(string Message); public record UpdateTicketResource(int? AssigneeId, string? Status);
[ApiController, Route("api/v1/support/tickets"), Authorize]
public sealed class SupportTicketsController(AppDbContext db, INotificationService notifications) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateTicketResource r, CancellationToken ct) { if (string.IsNullOrWhiteSpace(r.Subject)) return BadRequest(new { error = "Subject is required." }); var ticket = new SupportTicket { OwnerId = UserId(), Subject = r.Subject.Trim(), Category = r.Category, Priority = r.Priority }; if (!string.IsNullOrWhiteSpace(r.Message)) ticket.Messages.Add(new SupportMessage { AuthorId = UserId(), Body = r.Message.Trim() }); db.SupportTickets.Add(ticket); await db.SaveChangesAsync(ct); await notifications.CreateAsync(UserId(), "support", "Ticket creado", $"Tu solicitud '{ticket.Subject}' fue recibida.", ct); return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket); }
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var q = db.SupportTickets.AsNoTracking(); if (!Staff()) q = q.Where(t => t.OwnerId == UserId()); return Ok(await q.OrderByDescending(t => t.UpdatedAt).ToListAsync(ct)); }
    [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id, CancellationToken ct) { var ticket = await db.SupportTickets.Include(t => t.Messages).SingleOrDefaultAsync(t => t.Id == id, ct); return ticket is null || (!Staff() && ticket.OwnerId != UserId()) ? NotFound() : Ok(ticket); }
    [HttpPost("{id:int}/messages")] public async Task<IActionResult> Message(int id, MessageResource r, CancellationToken ct) { var ticket = await db.SupportTickets.Include(t => t.Messages).SingleOrDefaultAsync(t => t.Id == id, ct); if (ticket is null || (!Staff() && ticket.OwnerId != UserId())) return NotFound(); if (string.IsNullOrWhiteSpace(r.Message)) return BadRequest(); var staff = Staff(); ticket.Messages.Add(new SupportMessage { AuthorId = UserId(), IsStaff = staff, Body = r.Message.Trim() }); await db.SaveChangesAsync(ct); if (staff) await notifications.CreateAsync(ticket.OwnerId, "support", "Nueva respuesta de soporte", ticket.Subject, ct); return NoContent(); }
    [HttpPatch("{id:int}"), Authorize(Roles = "Support,Admin")] public async Task<IActionResult> Update(int id, UpdateTicketResource r, CancellationToken ct) { var ticket = await db.SupportTickets.FindAsync([id], ct); if (ticket is null) return NotFound(); ticket.AssigneeId = r.AssigneeId; if (!string.IsNullOrWhiteSpace(r.Status)) ticket.Status = r.Status; await db.SaveChangesAsync(ct); return NoContent(); }
    private int UserId() => int.Parse(User.FindFirst("user_id")!.Value); private bool Staff() => User.IsInRole("Support") || User.IsInRole("Admin");
}
