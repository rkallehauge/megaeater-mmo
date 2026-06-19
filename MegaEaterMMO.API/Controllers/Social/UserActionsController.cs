using MegaEaterMMO.API.Service.Users;
using MegaEaterMMO.Shared.DTO.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MegaEaterMMO.API.Controllers.Social
{


    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UserActionsController : MegaAPIBase
    {

        private readonly ISocialService _socialService;
        public UserActionsController(IUserService userService, ISocialService socialService)
        {
            _userService = userService;
            _socialService = socialService;
        }

        [HttpPost("AddFriend")]
        public IActionResult AddFriend(FriendRequest request)
        {
            var user = GetAuth();

            var friendReq = _socialService.SendFriendRequest(user.Id, request.TargetId).Result;

            if (friendReq.Success)
            {
                return Ok();
            }

            return BadRequest(friendReq.Errors);
        }

        [HttpPost("BlockUser")]
        public IActionResult BlockUser(BlockRequest request)
        {
            var user = GetAuth();

            var blockReq = _socialService.BlockUser(user.Id, request.TargetId).Result;
            if (blockReq.Success)
            {
                return Ok();
            }

            return BadRequest(blockReq.Errors);
        }

        [HttpDelete("DeleteFriend")]
        public IActionResult DeleteFriend(FriendRequest friendRequest)
        {
            throw new NotImplementedException();
        }

        [HttpGet("IncomingFriends")]
        public async Task<IActionResult> IncomingFriends()
        {
            var user = GetAuth();

            var friendRequests = await _socialService.IncomingRequests(user.Id);

            if(!friendRequests.Success)
            {
                return BadRequest(friendRequests.Errors);
            }

            return Ok(friendRequests.Data);   
        }

        [HttpPost("RespondIncoming")]
        public async Task<IActionResult> RespondIncoming(FriendRequestResponse response) // Accept friend rq from user with incoming ID
        {
            var user = GetAuth();

            var req = await _socialService.RespondToFriendRequest(response.TargetId, user.Id, response.Accepted);
            if (!req.Success)
            {
                return BadRequest(req.Errors);
            }
            return Ok();
        }
    }
}
