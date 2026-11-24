using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

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
        memoryTtl ??= TimeSpan.FromMinutes(1);
        redisTtl ??= TimeSpan.FromMinutes(10);

        // 1. Перевірка L1 (In-Memory)
        if (_memoryCache.TryGetValue(key, out T? l1Value) && l1Value is not null)
        {
            _metrics.Hit("L1");
            return l1Value;
        }
        _metrics.Miss("L1");

        // 2. Перевірка L2 (Redis) з Graceful Degradation[cite: 10]
        byte[]? redisBytes = null;
        try
        {
            redisBytes = await _redisCache.GetAsync(key, ct);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable, falling back to database for {Key}", key);
        }

        if (redisBytes is not null)
        {
            var l2Value = JsonSerializer.Deserialize<T>(redisBytes);
            if (l2Value is not null)
            {
                _metrics.Hit("L2");
                _memoryCache.Set(key, l2Value, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = memoryTtl
                });
                return l2Value;
            }
        }
        _metrics.Miss("L2");

        // 3. Звернення до бази даних[cite: 10, 11]
        var dbValue = await factory();
        if (dbValue is null) return default;

        // 4. Запис у L2 (Redis) з обробкою недоступності[cite: 11]
        try
        {
            await _redisCache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(dbValue),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = redisTtl },
                ct);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable, value for {Key} not cached", key);
        }

        // Запис у L1
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
        try
        {
            await _redisCache.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable during cache invalidation for {Key}", key);
        }
    }
}