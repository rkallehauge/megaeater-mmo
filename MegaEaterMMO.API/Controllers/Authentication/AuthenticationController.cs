using MegaEaterMMO.Shared;
using MegaEaterMMO.API.Data.DB;
using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.API.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MegaEaterMMO.API.Controllers.Authentication
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private MegaEaterContext _context;
        private IAuthenticationService _authService;
        private IUserService _userService;
        private IRateLimitingService _rateLimitingService;

        public AuthenticationController(MegaEaterContext context, IAuthenticationService authService, IUserService userService, IRateLimitingService rateLimitingService)
        {
            _context = context;
            _authService = authService;
            _userService = userService;
            _rateLimitingService = rateLimitingService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            // Per-IP rate limiting
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"; // only real hackermen get unknown IPs 😎

            var rateLimitResult = await _rateLimitingService.IsAllowedAsync(
                $"login:{ip}",
                limit: 5, // allow 5 attempts
                period: TimeSpan.FromMinutes(1) // per minute
            );

            if(!rateLimitResult)
            {
                return StatusCode(429, "Too many login attempts. Please try again later.");
            }

            Result<string> authResult = _authService.Authenticate(request.Username, request.Password).Result;

            if (authResult.Success)
            {
                return Ok(new { Token = authResult.Data });
            }
            else
            {
                return Unauthorized(authResult.Errors);
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var rateLimitResult = _rateLimitingService.IsAllowedAsync(
                $"register:{ip}",
                limit: 1, // very strict limit for registrations, one would assume the user remembers what they just registered with
                period: TimeSpan.FromMinutes(1) 
            ).Result;

            if(!rateLimitResult)
            {
                return StatusCode(429, "Too many registration attempts. Please try again later.");
            }

            Result registerResult = _authService.Register(request.Username, request.Password);
            if (registerResult.Success)
            {
                return Ok();
            }
            else
            {
                return BadRequest(registerResult.Errors);
            }
        }

        //tbd refresh

        //tbd logout
        [Authorize]
        [HttpPost("requestAccountRemoval")]
        public IActionResult RequestAccountRemoval() // requests removal of user account
        {
            var jti = User.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                return BadRequest("No JTI provided.");
            }

            var userReq = _userService.GetAuthenticatedUser(User);
            if(!userReq.Success)
            {
                return Unauthorized(userReq.Errors);
            }

            User user = (userReq.Data != null) ? userReq.Data : throw new Exception("Authenticated user not found, this should never happen");
            
            // invalidate tokens for user BEFORE deleting the account
            _authService.Logout(jti, user.Username);

            // todo : 2fa confirmation (email/sms) 
            _userService.RemoveUser(user.Id);

            return Ok();
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("removeAccountAdmin")]
        public IActionResult RemoveAccountAdmin([FromBody] string username)
        {
            throw new NotImplementedException("to be done :)");
        }



        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var jti = User.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
            var username = User.Identity?.Name; // or use a specific claim if you have one for username
            if (string.IsNullOrEmpty(jti))
            {
                return BadRequest("No JTI provided.");
            }
            var result = _authService.Logout(jti, username);

            if (result.Success) {
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        public class AuthRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
