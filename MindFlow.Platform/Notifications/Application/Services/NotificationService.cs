using Mindflow_backend.Notifications.Domain.Entities;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.iam.application.services;

namespace Mindflow_backend.Notifications.Application.Services;
public interface INotificationService { Task CreateAsync(int userId, string type, string title, string body, CancellationToken ct = default); }
public sealed class NotificationService(AppDbContext db, IEmailService email) : INotificationService
{
    public async Task CreateAsync(int userId, string type, string title, string body, CancellationToken ct = default)
    {
        await db.Notifications.AddAsync(new Notification { UserId = userId, Type = type, Title = title, Body = body }, ct);
        await db.SaveChangesAsync(ct);
        var user = await db.Users.FindAsync([userId], ct);
        if (user is not null) await email.SendNotificationAsync(user.Email, title, body);
    }
}
