using Serilog.Context;

namespace Mindflow_backend.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Ensures every request carries a correlation/trace id, exposes it on the response
///     and pushes it into the Serilog log context so all logs written during the
///     request (including from the global exception handler) can be correlated.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
            return headerValue.ToString();

        return context.TraceIdentifier;
    }
}
