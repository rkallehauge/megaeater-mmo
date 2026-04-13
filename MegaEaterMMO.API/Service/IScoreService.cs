using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.Shared;
using MegaEaterMMO.Shared.DTO.Game;

namespace MegaEaterMMO.API.Service
{
    public interface IScoreService
    {
        public Result AddScore(User user, int score);

        public Result RemoveScore(int scoreId);
        public Result RemoveAllUserScores(User user);

        public Task<Result<List<ScoreDTO>>> GetTopScores(int count);
        public Task<Result<List<ScoreDTO>>> GetScores(User user);
    }
}
