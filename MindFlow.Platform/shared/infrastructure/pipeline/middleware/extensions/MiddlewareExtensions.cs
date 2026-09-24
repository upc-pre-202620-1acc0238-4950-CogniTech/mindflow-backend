using Mindflow_backend.Shared.Infrastructure.Pipeline.Middleware.Components;

namespace Mindflow_backend.Shared.Infrastructure.Pipeline.Middleware.Extensions;

/// <summary>
///     Middleware extensions
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    ///     Use the global exception handler middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }

    /// <summary>
    ///     Use the correlation id middleware
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
