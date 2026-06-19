using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.API.Service.Users;
using Microsoft.AspNetCore.Mvc;

namespace MegaEaterMMO.API.Controllers
{
    public abstract class MegaAPIBase : Controller
    {
        protected IUserService _userService;

        protected User? GetAuth()
        {
            var userReq = _userService.GetAuthenticatedUser(HttpContext.User);
            if (!userReq.Success)
            {
                throw new Exception("Authenticated user has somehow not been found. Code is ass, terminating.");
            }

            if(userReq.Data == null)
            {
                throw new Exception("Authenticated user is somehow null. Code is ass, terminating.");
            }
            return userReq.Data;
        }
    }
}
