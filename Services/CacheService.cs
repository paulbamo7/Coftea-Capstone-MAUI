using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Coftea_Capstone.Services
{
    /// <summary>
    /// Thread-safe caching service for frequently accessed data
    /// </summary>
    public class CacheService
    {
        private static readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private static readonly Timer _cleanupTimer;

        static CacheService()
        {
            // Cleanup expired items every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private static void CleanupExpiredItems(object state)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt < now)
                {
                    _cache.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>
        /// Gets a cached item or computes it if not cached
        /// </summary>
        public static async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null)
        {
            duration ??= TimeSpan.FromMinutes(5);

            if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                if (cached.Value is T typedValue)
                {
                    return typedValue;
                }
            }

            var value = await factory();
            _cache.TryAdd(key, new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(duration.Value)
            });

            return value;
        }

        /// <summary>
        /// Gets a cached item synchronously
        /// </summary>
        public static T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                if (cached.Value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Sets a cached item
        /// </summary>
        public static void Set<T>(string key, T value, TimeSpan? duration = null)
        {
            duration ??= TimeSpan.FromMinutes(5);
            _cache.AddOrUpdate(key, new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(duration.Value)
            }, (k, v) => new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.Add(duration.Value)
            });
        }

        /// <summary>
        /// Invalidates a cached item
        /// </summary>
        public static void Invalidate(string key)
        {
            _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// Invalidates all cached items matching a prefix
        /// </summary>
        public static void InvalidatePrefix(string prefix)
        {
            foreach (var key in _cache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Clears all cached items
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}

