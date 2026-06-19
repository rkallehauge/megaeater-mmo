namespace MegaEaterMMO.API.Service.Security
{
    public interface IRateLimitingService
    {
        public Task<bool> IsAllowedAsync(string key, int limit, TimeSpan period);
    }
}
