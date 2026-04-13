using MegaEaterMMO.API.Data.Model;
using System.Security.Claims;
using MegaEaterMMO.Shared;

namespace MegaEaterMMO.API.Service
{
    public interface IUserService
    {
        public Result<User> GetUser(int id);
        public Result<User> GetUser(string username);
        public Result<User> GetAuthenticatedUser(ClaimsPrincipal user);
        public Result RemoveUser(int id);
    }
}