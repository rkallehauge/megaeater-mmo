namespace MegaEaterMMO.API.Service
{
    public interface IRateLimitingService
    {
        public Task<bool> IsAllowedAsync(string key, int limit, TimeSpan period);
    }
}
