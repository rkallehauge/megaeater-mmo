using MegaEaterMMO.API.Data.DB;
using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.Shared;
using MegaEaterMMO.Shared.DTO.Game;
using StackExchange.Redis;
using System.Security.Claims;

namespace MegaEaterMMO.API.Service.Game
{
    public class ScoreService : IScoreService
    {

        MegaEaterContext _context;
        IConnectionMultiplexer _cache;

        public ScoreService(MegaEaterContext context, IConnectionMultiplexer cache)
        {
            _context = context;
            _cache = cache;
        }

        public Result AddScore(User user, int score)
        {
            var entry = _context.Scores.Add(new Score
            {
                User = user,
                Value = score,
                CreatedAt = DateTime.UtcNow
            });
            if(entry is null)
            {
                return Result.Fail(new Error("SCORE.ADD_FAILED", "Failed to add score"));
            }
            _context.SaveChanges();
            return Result.OK();
        }

        // Doesn't strictly need to be async
        public async Task<Result<List<ScoreDTO>>> GetScores(User user) // ordered by score
        {
            var cacheKey = $"user_scores_{user.Id}";
            var result = new List<ScoreDTO>();

            if(_cache.GetDatabase().KeyExists(cacheKey))
            {
                var cacheEntry = _cache.GetDatabase().StringGetAsync(cacheKey).Result;
                if(cacheEntry.HasValue)
                {
                    var cachedScores = System.Text.Json.JsonSerializer.Deserialize<List<ScoreDTO>>(cacheEntry.ToString());
                    if(cachedScores != null)
                    {
                        return await Task.FromResult(Result<List<ScoreDTO>>.OK(cachedScores));
                    }
                }
            }

            result = _context.Scores
                .Where(s => s.User.Id == user.Id)
                .OrderByDescending(s => s.Value)
                .Select(s => new ScoreDTO
                {
                    Id = s.Id,
                    Username = s.User.Username,
                    Value = s.Value,
                    CreatedAt = s.CreatedAt
                })
                .ToList();


            _cache.GetDatabase().StringSet(cacheKey, System.Text.Json.JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10)); // cache for 10 minutes

            return await Task.FromResult(Result<List<ScoreDTO>>.OK(result)); // empty score list is not an error
        }
        // Doesn't strictly need to be async
        public async Task<Result<List<ScoreDTO>>> GetTopScores(int count)
        {
            var cacheKey = $"top_scores_{count}";

            var cacheEntry = await _cache.GetDatabase().StringGetAsync(cacheKey);
            if(cacheEntry.HasValue)
            {
                var cachedScores = System.Text.Json.JsonSerializer.Deserialize<List<ScoreDTO>>(cacheEntry.ToString());
                if(cachedScores != null)
                {
                    return Result<List<ScoreDTO>>.OK(cachedScores);
                }
            }

            var result = new List<ScoreDTO>();

            result = _context.Scores
                .OrderByDescending(s => s.Value)
                .Take(count)
                .Select(s => new ScoreDTO
                {
                    Id = s.Id,
                    Username = s.User.Username,
                    Value = s.Value,
                    CreatedAt = s.CreatedAt
                })
                .ToList();

            return Result<List<ScoreDTO>>.OK(result); // empty score list is not an error
        }

        public Result RemoveAllUserScores(User user)
        {
            // only used by authenticated user / admin to remove scores
            _context.Scores.RemoveRange(_context.Scores.Where(s => s.User.Id == user.Id));

            return Result.OK();
        }

        public Result RemoveScore(int scoreId)
        {
            _context.Scores.RemoveRange(_context.Scores.Where(s => s.Id == scoreId));

            return Result.OK();
        }

    }
}
