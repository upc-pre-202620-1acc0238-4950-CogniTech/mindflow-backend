namespace Mindflow_backend.Shared.Infrastructure.Caching;

/// <summary>
///     Thin JSON-based wrapper over <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>,
///     used to cache read-heavy, per-user query results (analytics, habits) in Redis.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class;

    Task RemoveAsync(string key, CancellationToken ct = default);
}
