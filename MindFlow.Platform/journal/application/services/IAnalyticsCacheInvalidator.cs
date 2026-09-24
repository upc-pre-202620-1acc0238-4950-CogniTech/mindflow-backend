// Ported standalone: the real Analytics & Reporting bounded context (owner of cache
// invalidation logic) is out of scope for this repo slice. Journal's handlers only need
// the contract; a no-op stub (AnalyticsCacheInvalidatorStub) is registered in Program.cs.
// Namespace kept identical to the original so Journal's existing `using` directives don't change.
namespace Mindflow_backend.Analytics.Application.Services;

public interface IAnalyticsCacheInvalidator
{
    Task InvalidateAsync(int userId, DateOnly entryDate, CancellationToken ct = default);
}
