using Mindflow_backend.Shared.Domain.Model.Entities;
namespace Mindflow_backend.Subscriptions.Domain.Entities;
public class Subscription : IAuditableEntity { public int Id { get; set; } public int UserId { get; set; } public string Plan { get; set; } = "freemium"; public string Status { get; set; } = "active"; public string? StripeCustomerId { get; set; } public string? StripeSubscriptionId { get; set; } public DateTimeOffset? CurrentPeriodEnd { get; set; } public DateTimeOffset? CreatedAt { get; set; } public DateTimeOffset? UpdatedAt { get; set; } }
