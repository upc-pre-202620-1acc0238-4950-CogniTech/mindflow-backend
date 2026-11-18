using Google.Apis.Auth;
using Mindflow_backend.iam.application.services;

namespace Mindflow_backend.iam.infrastructure.services;

public class GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleUserInfo?> ValidateAsync(string credential)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogError("Google:ClientId is not configured; Google sign-in is disabled.");
            return null;
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            if (!payload.EmailVerified)
            {
                logger.LogWarning("Google account email is not verified; sign-in rejected.");
                return null;
            }

            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google authentication validation failed.");
            return null;
        }
    }
}
