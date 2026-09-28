using Mindflow_backend.Shared.Domain.Model.Entities;

namespace Mindflow_backend.Support.Domain.Entities;

public class SupportTicket : IAuditableEntity
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int? AssigneeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public string Priority { get; set; } = "normal";
    public string Status { get; set; } = "open";
    public List<SupportMessage> Messages { get; set; } = [];
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
public class SupportMessage : IAuditableEntity
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public int AuthorId { get; set; }
    public bool IsStaff { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
