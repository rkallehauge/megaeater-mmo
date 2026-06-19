using StackExchange.Redis;

namespace MegaEaterMMO.API.Service.Security
{
    public class RateLimitingService : IRateLimitingService
    {

        IDatabase _cache;
        
        public RateLimitingService(IConnectionMultiplexer cache)
        {
            _cache = cache.GetDatabase();
        }

        public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan period)
        {
            var count = await _cache.StringIncrementAsync(key);

            if(count == 1)
            {
                await _cache.KeyExpireAsync(key, period);
            }

            return count <= limit;
        }
    }
}
