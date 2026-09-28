using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Habits.Domain.Model.Aggregates;
using Mindflow_backend.Habits.Domain.Model.Entities;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Mindflow_backend.Analytics.Interfaces.Rest;
[ApiController, Route("api/v1/analytics"), Authorize]
public sealed class AnalyticsController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")] public async Task<IActionResult> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) { var user = Id(); var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)); var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow); if (start > end) return BadRequest(); var entries = await db.JournalEntries.AsNoTracking().Where(e => e.UserId == user && e.Date >= start && e.Date <= end).ToListAsync(ct); var habits = await db.Set<Habit>().AsNoTracking().Where(h => h.UserId == user && h.DeletedAt == null).Select(h => h.Id).ToListAsync(ct); var completed = await db.Set<HabitCompletionLog>().AsNoTracking().CountAsync(l => habits.Contains(l.HabitId) && l.Completed && DateOnly.FromDateTime(l.Date) >= start && DateOnly.FromDateTime(l.Date) <= end, ct); return Ok(new { from = start, to = end, journal_entries = entries.Count, sentiments = entries.GroupBy(e => e.Sentiment).ToDictionary(g => g.Key, g => g.Count()), top_categories = entries.GroupBy(e => e.Category).OrderByDescending(g => g.Count()).Take(5).Select(g => new { category = g.Key, count = g.Count() }), habit_completions = completed, active_habits = habits.Count }); }
    [HttpGet("report.csv")] public async Task<IActionResult> Csv(CancellationToken ct) { var entries = await db.JournalEntries.AsNoTracking().Where(e => e.UserId == Id()).OrderByDescending(e => e.Date).ToListAsync(ct); var rows = new List<string> { "date,category,sentiment,title" }; rows.AddRange(entries.Select(e => $"{e.Date:yyyy-MM-dd},\"{e.Category.Replace("\"", "\"\"")}\",{e.Sentiment},\"{e.Title.Replace("\"", "\"\"")}\"")); return File(System.Text.Encoding.UTF8.GetBytes(string.Join('\n', rows)), "text/csv", "mindflow-report.csv"); }
    [HttpGet("report.pdf")] public async Task<IActionResult> Pdf(CancellationToken ct)
    {
        var user = Id();
        var entries = await db.JournalEntries.AsNoTracking().Where(e => e.UserId == user).OrderByDescending(e => e.Date).ToListAsync(ct);
        var grouped = entries.GroupBy(e => e.Sentiment).ToDictionary(g => g.Key, g => g.Count());
        var document = Document.Create(root => root.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(36); page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Column(c => { c.Item().Text("Reporte MindFlow").FontSize(22).SemiBold().FontColor(Colors.Blue.Darken2); c.Item().Text($"Generado: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC").FontColor(Colors.Grey.Darken1); });
            page.Content().PaddingVertical(18).Column(c =>
            {
                c.Spacing(8); c.Item().Text("Resumen emocional").FontSize(14).SemiBold();
                c.Item().Text($"Registros: {entries.Count} | Positivos: {grouped.GetValueOrDefault("positive")} | Neutrales: {grouped.GetValueOrDefault("neutral")} | Negativos: {grouped.GetValueOrDefault("negative")}");
                c.Item().PaddingTop(8).Text("Entradas del diario").FontSize(14).SemiBold();
                foreach (var entry in entries) c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6).Column(x => { x.Item().Text($"{entry.Date:yyyy-MM-dd} - {entry.Category} - {entry.Sentiment}").SemiBold(); x.Item().Text(entry.Title); });
            });
            page.Footer().AlignCenter().Text(x => { x.Span("MindFlow - reporte personal - página "); x.CurrentPageNumber(); });
        }));
        return File(document.GeneratePdf(), "application/pdf", "mindflow-report.pdf");
    }
    private int Id() => int.Parse(User.FindFirst("user_id")!.Value);
}
