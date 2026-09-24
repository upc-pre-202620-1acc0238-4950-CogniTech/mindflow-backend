using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Mindflow_backend.Shared.Infrastructure.Caching;

public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0)
            {
                logger.LogInformation("Cache miss for key {CacheKey}", key);
                return null;
            }

            logger.LogInformation("Cache hit for key {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cache is an optimization, not a source of truth: a Redis hiccup must not fail the request.
            logger.LogWarning(ex, "Cache read failed for key {CacheKey}; falling back to source", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache write failed for key {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache invalidation failed for key {CacheKey}", key);
        }
    }
}
