using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Habits.Domain.Model.Aggregates;
using Mindflow_backend.Habits.Domain.Model.Entities;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
namespace Mindflow_backend.Analytics.Interfaces.Rest;
[ApiController, Route("api/v1/analytics"), Authorize]
public sealed class AnalyticsController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")] public async Task<IActionResult> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) { var user = Id(); var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)); var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow); if (start > end) return BadRequest(); var entries = await db.JournalEntries.AsNoTracking().Where(e => e.UserId == user && e.Date >= start && e.Date <= end).ToListAsync(ct); var habits = await db.Set<Habit>().AsNoTracking().Where(h => h.UserId == user && h.DeletedAt == null).Select(h => h.Id).ToListAsync(ct); var completed = await db.Set<HabitCompletionLog>().AsNoTracking().CountAsync(l => habits.Contains(l.HabitId) && l.Completed && DateOnly.FromDateTime(l.Date) >= start && DateOnly.FromDateTime(l.Date) <= end, ct); return Ok(new { from = start, to = end, journal_entries = entries.Count, sentiments = entries.GroupBy(e => e.Sentiment).ToDictionary(g => g.Key, g => g.Count()), top_categories = entries.GroupBy(e => e.Category).OrderByDescending(g => g.Count()).Take(5).Select(g => new { category = g.Key, count = g.Count() }), habit_completions = completed, active_habits = habits.Count }); }
    [HttpGet("report.csv")] public async Task<IActionResult> Csv(CancellationToken ct) { var entries = await db.JournalEntries.AsNoTracking().Where(e => e.UserId == Id()).OrderByDescending(e => e.Date).ToListAsync(ct); var rows = new List<string> { "date,category,sentiment,title" }; rows.AddRange(entries.Select(e => $"{e.Date:yyyy-MM-dd},\"{e.Category.Replace("\"", "\"\"")}\",{e.Sentiment},\"{e.Title.Replace("\"", "\"\"")}\"")); return File(System.Text.Encoding.UTF8.GetBytes(string.Join('\n', rows)), "text/csv", "mindflow-report.csv"); }
    private int Id() => int.Parse(User.FindFirst("user_id")!.Value);
}
