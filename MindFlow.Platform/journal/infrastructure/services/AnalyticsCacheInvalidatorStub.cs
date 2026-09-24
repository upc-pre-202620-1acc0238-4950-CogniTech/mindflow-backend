using Mindflow_backend.Analytics.Application.Services;

namespace Mindflow_backend.Journal.Infrastructure.Services;

/// <summary>
///     No-op stand-in for the real Analytics & Reporting cache invalidator, which lives
///     outside this repo's scope. Swap for the real implementation once that bounded
///     context is ported here.
/// </summary>
public class AnalyticsCacheInvalidatorStub : IAnalyticsCacheInvalidator
{
    public Task InvalidateAsync(int userId, DateOnly entryDate, CancellationToken ct = default) =>
        Task.CompletedTask;
}
