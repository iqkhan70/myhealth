using StackExchange.Redis;
using System;
using System.Threading.Tasks;

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

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<string?> GetAsync(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task SetAsync(string key, string value, TimeSpan expiration)
        {
            await _db.StringSetAsync(key, value, expiration);
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
    }
}
