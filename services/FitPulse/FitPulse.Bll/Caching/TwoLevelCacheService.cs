using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FitPulse.Bll.Caching;

public class TwoLevelCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly CacheMetrics _metrics;
    private readonly ILogger<TwoLevelCacheService> _logger;

    public TwoLevelCacheService(
        IMemoryCache memoryCache,
        IDistributedCache redisCache,
        CacheMetrics metrics,
        ILogger<TwoLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? memoryTtl = null,
        TimeSpan? redisTtl = null,
        CancellationToken ct = default)
    {
        memoryTtl ??= TimeSpan.FromMinutes(1);   // L1 коротший TTL[cite: 14, 15]
        redisTtl ??= TimeSpan.FromMinutes(10);   // L2 довший TTL[cite: 14, 15]

        // 1. Перевірка L1 (In-Memory)[cite: 14]
        if (_memoryCache.TryGetValue(key, out T? l1Value) && l1Value is not null)
        {
            _metrics.Hit("L1");
            _logger.LogDebug("Cache hit L1 (Key: {Key})", key);
            return l1Value;
        }
        _metrics.Miss("L1");

        // 2. Перевірка L2 (Redis)[cite: 14]
        var redisBytes = await _redisCache.GetAsync(key, ct);
        if (redisBytes is not null)
        {
            var l2Value = JsonSerializer.Deserialize<T>(redisBytes);
            if (l2Value is not null)
            {
                _metrics.Hit("L2");
                _logger.LogDebug("Cache hit L2 (Key: {Key})", key);
                _memoryCache.Set(key, l2Value, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = memoryTtl
                });
                return l2Value;
            }
        }
        _metrics.Miss("L2");

        // 3. Звернення до бази даних[cite: 14]
        _logger.LogDebug("Cache miss (Key: {Key}). Fetching from database", key);
        var dbValue = await factory();
        if (dbValue is null) return default;

        // Заповнюємо L2 та L1 на зворотному шляху[cite: 14, 15]
        await _redisCache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(dbValue),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = redisTtl },
            ct);

        _memoryCache.Set(key, dbValue, new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = memoryTtl
        });

        return dbValue;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memoryCache.Remove(key);
        await _redisCache.RemoveAsync(key, ct);
        _logger.LogInformation("Cache invalidated (Key: {Key})", key);
    }
}