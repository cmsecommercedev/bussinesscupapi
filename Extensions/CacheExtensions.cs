using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

public static class CacheExtensions
{
    public static async Task<T> GetWithDependencyAsync<T>(
        this IMemoryCache cache,
        IDistributedCache distributedCache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null)
    {
        string versionKey = "fm_league_version";
        long currentVersion = await GetCacheVersionAsync(distributedCache, versionKey);
        string fullKey = $"{key}_{currentVersion}";

        return await cache.GetOrCreateAsync(fullKey, async entry =>
        {
            if (absoluteExpiration.HasValue)
                entry.SetAbsoluteExpiration(absoluteExpiration.Value);
            
            return await factory();
        });
    }

    private static async Task<long> GetCacheVersionAsync(IDistributedCache cache, string key)
    {
        var data = await cache.GetAsync(key);
        if (data == null)
            return 0;
        return BitConverter.ToInt64(data, 0);
    }
} 