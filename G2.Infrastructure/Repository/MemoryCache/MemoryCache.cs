using StackExchange.Redis;

namespace G2.Infrastructure.Repository
{
    public class MemoryCache: IMemoryCache
    {
        private readonly IDatabase _redis;

        public MemoryCache(IConnectionMultiplexer multiplexer)
        {
            _redis = multiplexer.GetDatabase();
        }

        public async Task AddToSet(string key, string value)
        {
            await _redis.SetAddAsync(key, value);
        }

        public async Task<bool> DeleteString(string key)
        {
            return await _redis.KeyDeleteAsync(key);
        }

        public async Task<string?> GetString(string key)
        {
            return await _redis.StringGetAsync(key);
        }

        public async Task<long> IncrementString(string key)
        {
           return await _redis.StringIncrementAsync(key);
        }

        public async Task RemoveFromSet(string key, string value)
        {
            await _redis.SetRemoveAsync(key, value);
        }

        public async Task<bool> SetContains(string key, string value)
        {
            return await _redis.SetContainsAsync(key, value);
        }

        public async Task SetString(string key, string value, TimeSpan? expiry = null)
        {
            await _redis.StringSetAsync(key, value, expiry);
        }
    }
}