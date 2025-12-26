using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IRedisCacheService
    {
        Task<string?> GetAsync(string key);
        Task SetAsync(string key, string value, TimeSpan expiration);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService>? _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService>? logger = null)
        {
            _db = redis.GetDatabase();
            _redis = redis;
            _logger = logger;
        }

        private bool IsRedisAvailable()
        {
            try
            {
                return _redis.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            if (!IsRedisAvailable())
            {
                _logger?.LogWarning("Redis not available, returning null for key: {Key}", key);
                return null;
            }

            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis GetAsync failed for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync(string key, string value, TimeSpan expiration)
        {
            if (!IsRedisAvailable())
            {
                _logger?.LogWarning("Redis not available, skipping SetAsync for key: {Key}", key);
                return;
            }

            try
            {
                await _db.StringSetAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis SetAsync failed for key: {Key}", key);
                // Don't throw - Redis is optional
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            if (!IsRedisAvailable())
            {
                _logger?.LogWarning("Redis not available, skipping RemoveAsync for key: {Key}", key);
                return false;
            }

            try
            {
                return await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis RemoveAsync failed for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (!IsRedisAvailable())
            {
                _logger?.LogWarning("Redis not available, returning false for ExistsAsync key: {Key}", key);
                return false;
            }

            try
            {
                return await _db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis ExistsAsync failed for key: {Key}", key);
                return false;
            }
        }
    }
}
