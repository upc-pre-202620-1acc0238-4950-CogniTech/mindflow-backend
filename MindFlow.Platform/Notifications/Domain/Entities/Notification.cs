using Mindflow_backend.Shared.Domain.Model.Entities;

namespace Mindflow_backend.Notifications.Domain.Entities;

public class Notification : IAuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "general";
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
