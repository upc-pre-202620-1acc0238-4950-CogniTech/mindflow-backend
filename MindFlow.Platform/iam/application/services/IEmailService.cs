namespace Mindflow_backend.iam.application.services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetToken);
    Task SendNotificationAsync(string toEmail, string subject, string message);
}
