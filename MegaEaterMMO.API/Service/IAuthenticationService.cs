using MegaEaterMMO.Shared;

namespace MegaEaterMMO.API.Service
{
    public interface IAuthenticationService
    {
        public bool IsAuthenticated { get; } // unused?
        public Task<Result<string>> Authenticate(string username, string password); // Returns a token if authentication is successful, otherwise returns null or an empty string.
        public Result Register(string username, string password); // Don't log in user automatically, e-mail vertification can be implemented later.
        public Result Logout(string jti, string username);
        public Result VerifyToken(string token);
    }
}