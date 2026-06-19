using MegaEaterMMO.Shared;
using MegaEaterMMO.API.Data.DB;
using MegaEaterMMO.API.Data.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;



namespace MegaEaterMMO.API.Service.Auth
{
    public class AuthenticationService : IAuthenticationService
    {
        private MegaEaterContext _context;
        private IConnectionMultiplexer _cache;

        private IConfiguration _config;

        private PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        private const int expiryTime = 60;

        public AuthenticationService(MegaEaterContext context, IConfiguration config, IConnectionMultiplexer cache)
        {
            _context = context;
            _cache = cache;

            _config = config;
        }

        public bool IsAuthenticated => throw new NotImplementedException();

        private string GenerateToken(User user)
        {

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Jwt id for token blacklisting
                new Claim("tokenVersion", user.TokenVersion.ToString()) // Custom claim for token versioning, allows for server-side token invalidation
            };
            Console.WriteLine("auth signingkey");
            Console.WriteLine(_config["JWT:SigningKey"]);
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JWT:SigningKey"])
            );


            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryTime), // limited lifespan for token, if token gets compromised we limit damage extent, but don't limit lifespan so much it puts exessive strain on server
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<Result<string>> Authenticate(string username, string password)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Result<string>.Fail(new Error("AUTHENTICATION.FAILED", "Failed to authenticate user")); // don't tell more info that strictly necessary

            if(_context.Users.Where(u => u.Id == user.Id).Select(u => u.LockoutEnd).FirstOrDefault() > DateTime.UtcNow) // User is currently locked out
            {
                return Result<string>.Fail(new Error("AUTHENTICATION.LOCKED_OUT", "User is locked out due to too many failed login attempts. Please try again later.")); 
                // perhaps introduce subroutine for resetting password if same user keeps getting hit
            } 

            var result = _passwordHasher.VerifyHashedPassword(user, user.Hash, password);

            // Per-user lockout mechanism
            if(result != PasswordVerificationResult.Success)
            {
                DateTime lockoutEnd = DateTime.UtcNow.AddMinutes(15); // non client-side lockout time

                await _context.Users
                    .Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.FailedLoginAttempts, u => Math.Min(u.FailedLoginAttempts, 10)) // Increment failed login attempts, prevent overflow attacks however unlikely the server is to survive 2^32 reqeusts  
                    .SetProperty(u => u.LockoutEnd, u => u.FailedLoginAttempts + 1 >= 3 ? lockoutEnd : u.LockoutEnd) // Lock out user for 15 minutes after 3 failed login attempts
                );
                return Result<string>.Fail(new Error("AUTHENTICATION.FAILED", "Failed to authenticate user")); // don't tell more info that strictly necessary
            }
            // reset lockout vars after successful login
            await _context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FailedLoginAttempts, 0) // Reset failed login attempts on successful login
                .SetProperty(u => u.LockoutEnd, (DateTime?) null) // Reset lockout end on successful login
                .SetProperty(u => u.LastLogin, DateTime.UtcNow) // Update last login time
            );

            var token = GenerateToken(user); 

            return Result<string>.OK(token);
        }

        public Result Register(string username, string password)
        {
            if(_context.Users.Any(u => u.Username == username))
                return Result.Fail(new Error("UID.TAKEN", "Username already exists"));

            var user = new User()
            {
                Username = username,
                Hash = _passwordHasher.HashPassword(null, password) // warning can be ignored, this is correct
            };
            EntityEntry<User> entry;
            try
            {
                entry = _context.Users.Add(user);
            } catch (DbUpdateException e)
            {
                return Result.Fail(new Error("USER.ADD_FAILED", "Failed to add user to database"));
                // log e 
                // potential race-condition occuring when 2 register requests occur at the same time
                // DB field is unique, so this will cause throw an exception, we're catching it however..
            }

            var entries_changed = _context.SaveChanges();
            if(entry == null || entries_changed != 1)
            {
                return Result.Fail(new Error("USER.ADD_FAILED", "Failed to add user to database"));
            }

            return Result.OK();
        }

     

        public Result Logout(string jti, string username)
        {
            if (string.IsNullOrEmpty(jti))
            {
                return Result.Fail(new Error("USER.LOGOUT_FAILED", "No JWT id was provided."));
            }

            if(string.IsNullOrEmpty(username))
            {
                return Result.Fail(new Error("USER.LOGOUT_FAILED", "No username was provided."));
            }

            var cacheKey = $"blacklist:{jti}";
            _cache.GetDatabase().StringSet(cacheKey, "1", TimeSpan.FromMinutes(expiryTime));

            if (!updateTokenVersion(username))
            {
                return Result.Fail(new Error("USER.LOGOUT_FAILED", "Failed to update token version for user."));
            }

            return Result.OK();
        }

        public Result VerifyToken(string token)
        {
            throw new NotImplementedException(); // Not stricly necessary for current usage, but may be useful in future.
        }

        private bool updateTokenVersion(string username)
        {
            var affected = _context.Users.Where(u => u.Username == username).ExecuteUpdate(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1));
            return affected == 1;
        }
    }
}
