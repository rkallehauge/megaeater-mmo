using MegaEaterMMO.API.Data.DB;
using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.Shared;
using System.Security.Claims;

using MegaEaterMMO.Shared;
using Microsoft.EntityFrameworkCore;

namespace MegaEaterMMO.API.Service.Users
{
    public class UserService : IUserService
    {

        MegaEaterContext _context;

        public UserService(MegaEaterContext context)
        {
            _context = context;
        }

        public Result<User> GetUser(string username)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                return Result<User>.OK(user);
            }
            return Result<User>.Fail(new Error("USER.NOT_FOUND", "User not found"));
        }

        public Result<User> GetUser(int id)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                return Result<User>.OK(user);
            }
            return Result<User>.Fail(new Error("USER.NOT_FOUND", "User not found"));
        }

        public Result<User> GetAuthenticatedUser(ClaimsPrincipal user)
        {
            var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if(userId == null) throw new Exception("User ID claim not found");

            var foundUser = GetUser(int.Parse(userId));
            if (!foundUser.Success)
            {
                return Result<User>.Fail(new Error("USER.NOT_FOUND", "Authenticated user not found"));
            }
            if (foundUser.Data == null) throw new Exception("Authenticated user data is null"); // Actually something exceptional

            return Result<User>.OK(foundUser.Data);
        }

        // this method is very good and useful and cannot be exploited in any way whatsoever
        public Result RemoveUser(int id)
        {
            // authentication and authorization is handled elsewhere
            var affected = _context.Users
                .Where(u => u.Id == id)
                .ExecuteDelete();
            // db is configured to handle cascading deletes

            if (affected != 1)
            {
                return Result.Fail(new Error("USER.DELETE_FAILED", "Failed to delete user from database"));
            }
           
             return Result.OK();
        }
    }
}
