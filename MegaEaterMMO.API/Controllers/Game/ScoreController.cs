using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MegaEaterMMO.Shared;

namespace MegaEaterMMO.API.Controllers.Game
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ScoreController : Controller
    {
        IScoreService _scoreService;
        IUserService _userService;
        public ScoreController(IScoreService scoreService, IUserService userService)
        {
            _scoreService = scoreService;
            _userService = userService;
        }

        [HttpGet("getScores")]
        public IActionResult GetScores()
        {
            var getAuthReq = _userService.GetAuthenticatedUser(HttpContext.User);
            User? user = (getAuthReq.Success && getAuthReq.Data!=null) ? getAuthReq.Data : throw new Exception("Authenticated user not found.");
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
            var getAuthReq = _userService.GetAuthenticatedUser(HttpContext.User);
            User? user = (getAuthReq.Success && getAuthReq.Data != null) ? getAuthReq.Data : throw new Exception("Authenticated user not found.");

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
            var getUserReq = _userService.GetUser(req.UserId);
            if (!getUserReq.Success || getUserReq.Data==null)
            {
                return BadRequest(getUserReq.Errors);
            }

            User user = getUserReq.Data;
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
