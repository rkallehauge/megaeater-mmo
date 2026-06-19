using MegaEaterMMO.API.Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MegaEaterMMO.Shared;
using MegaEaterMMO.API.Service.Game;
using MegaEaterMMO.API.Service.Users;

namespace MegaEaterMMO.API.Controllers.Game
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ScoreController : MegaAPIBase
    {
        IScoreService _scoreService;
        public ScoreController(IScoreService scoreService, IUserService userService)
        {
            _scoreService = scoreService;
            _userService = userService;
        }

        [HttpGet("getScores")]
        public IActionResult GetScores()
        {
            var user = GetAuth();

            var scores = _scoreService.GetScores(user).Result;
            if (!scores.Success)
            {
                return BadRequest(scores.Errors);
            }

            return Ok(scores.Data);
        }

        [HttpPost("addScore")]
        public IActionResult AddScore(ScoreRequest req)
        {
            var user = GetAuth();

            var addScoreReq = _scoreService.AddScore(user, req.Score);
            if(!addScoreReq.Success)
            {
                return BadRequest(addScoreReq.Errors);
            }
            return Ok();

        }

        [Authorize(Roles = "Admin")]
        [HttpPost("addElevatedScore")]
        public IActionResult AdminAddScore(AdminScoreRequest req)
        {
            var user = GetAuth();

            var addScoreReq = _scoreService.AddScore(user, req.Score);
            if (!addScoreReq.Success)
            {
                return BadRequest(addScoreReq.Errors);
            }
            return Ok();
        }

        public class ScoreRequest
        {
            public int Score { get; set; }
        }

        public class AdminScoreRequest : ScoreRequest
        {
            public int UserId { get; set; }
        }
    }
}
