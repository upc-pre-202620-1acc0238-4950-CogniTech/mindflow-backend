using System.Net.Mime;
using System.Text.Json;
using Mindflow_backend.Resources.Errors;
using Mindflow_backend.Resources.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Mindflow_backend.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Global Exception Handling Middleware
/// </summary>
/// <remarks>
///     This middleware catches all unhandled exceptions and returns a Problem Details response.
/// </remarks>
public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    IStringLocalizer<CommonMessages> commonLocalizer)
{
    private readonly IStringLocalizer<CommonMessages> _commonLocalizer = commonLocalizer;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = errorLocalizer;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Request was cancelled: {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleOperationCanceledExceptionAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request: {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleBadRequestAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value) && value is string correlationId
            ? correlationId
            : context.TraceIdentifier;
    }

    private async Task HandleOperationCanceledExceptionAsync(HttpContext context, OperationCanceledException exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = StatusCodes.Status409Conflict;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = _errorLocalizer["OperationCancelled"],
            Detail = _errorLocalizer["OperationCancelled"],
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = GetCorrelationId(context);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonSerializer.Serialize(problemDetails, jsonOptions);

        await context.Response.WriteAsync(result);
    }

    private static async Task HandleBadRequestAsync(HttpContext context, ArgumentException exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = GetCorrelationId(context);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }

    private async Task HandleGenericExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = _commonLocalizer["InternalServerError"],
            Detail = _errorLocalizer["GenericError"],
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = GetCorrelationId(context);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonSerializer.Serialize(problemDetails, jsonOptions);

        await context.Response.WriteAsync(result);
    }
}
