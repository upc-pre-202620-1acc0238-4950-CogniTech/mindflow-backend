using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mindflow_backend.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Mindflow_backend.Subscriptions.Domain.Entities;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
namespace Mindflow_backend.Subscriptions.Interfaces.Rest;
[ApiController, Route("api/v1/subscriptions")]
public sealed class SubscriptionsController(AppDbContext db, IHttpClientFactory clients, IConfiguration configuration, ILogger<SubscriptionsController> logger) : ControllerBase
{
    [HttpGet("plans"), AllowAnonymous] public IActionResult Plans() => Ok(new[] { new { id = "freemium", price = 0m, currency = "USD", interval = (string?)null }, new { id = "premium", price = 4.99m, currency = "USD", interval = (string?)"month" } });
    [HttpGet("me"), Authorize] public async Task<IActionResult> Mine(CancellationToken ct) { var userId = Id(); return Ok(await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? new Subscription { UserId = userId }); }
    [HttpPost("demo/plan/{plan}"), Authorize] public async Task<IActionResult> DemoPlan(string plan, CancellationToken ct) { if (plan is not ("freemium" or "premium")) return BadRequest(); var userId = Id(); var sub = await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == userId, ct); if (sub is null) { sub = new Subscription { UserId = userId }; db.Subscriptions.Add(sub); } sub.Plan = plan; sub.Status = "active"; await db.SaveChangesAsync(ct); return Ok(sub); }
    [HttpPost("checkout"), Authorize]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        var secret = configuration["Stripe:SecretKey"];
        var price = configuration["Stripe:PremiumPriceId"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(price))
            return Problem("Stripe test credentials are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var frontend = configuration["FrontendUrl"]?.Split(',', StringSplitOptions.TrimEntries).FirstOrDefault() ?? "http://localhost:5173";
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secret}:")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["mode"] = "subscription", ["line_items[0][price]"] = price, ["line_items[0][quantity]"] = "1",
            ["client_reference_id"] = Id().ToString(), ["success_url"] = $"{frontend}/payment-success?session_id={{CHECKOUT_SESSION_ID}}", ["cancel_url"] = $"{frontend}/plans",
            ["managed_payments[enabled]"] = "false"
        });
        using var response = await clients.CreateClient("Stripe").SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Stripe Checkout failed with {StatusCode}: {StripeError}", (int)response.StatusCode, json);
            return Problem("Stripe could not create a checkout session.", statusCode: StatusCodes.Status502BadGateway);
        }
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return Ok(new { checkout_url = document.RootElement.GetProperty("url").GetString(), session_id = document.RootElement.GetProperty("id").GetString() });
    }
    [HttpPost("webhook"), AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var secret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);
        if (!ValidSignature(Request.Headers["Stripe-Signature"].ToString(), payload, secret)) return Unauthorized();
        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.GetProperty("type").GetString() is not "checkout.session.completed") return Ok();
        var data = root.GetProperty("data").GetProperty("object");
        if (!data.TryGetProperty("client_reference_id", out var reference) || !int.TryParse(reference.GetString(), out var userId)) return BadRequest();
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (subscription is null) { subscription = new Subscription { UserId = userId }; db.Subscriptions.Add(subscription); }
        subscription.Plan = "premium"; subscription.Status = "active";
        if (data.TryGetProperty("customer", out var customer)) subscription.StripeCustomerId = customer.GetString();
        if (data.TryGetProperty("subscription", out var stripeSubscription)) subscription.StripeSubscriptionId = stripeSubscription.GetString();
        await db.SaveChangesAsync(ct);
        return Ok();
    }
    private static bool ValidSignature(string header, string payload, string secret)
    {
        var timestamp = header.Split(',').FirstOrDefault(x => x.StartsWith("t="))?[2..];
        var signature = header.Split(',').FirstOrDefault(x => x.StartsWith("v1="))?[3..];
        if (timestamp is null || signature is null || !long.TryParse(timestamp, out var unixTime) || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unixTime) > 300) return false;
        var signedPayload = $"{timestamp}.{payload}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }
    private int Id() => int.Parse(User.FindFirst("user_id")!.Value);
}
