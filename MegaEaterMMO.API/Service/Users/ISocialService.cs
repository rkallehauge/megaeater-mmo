using MegaEaterMMO.Shared;
using MegaEaterMMO.API.Data.Model.Relations;
using MegaEaterMMO.Shared.DTO.User;
namespace MegaEaterMMO.API.Service.Users
{
    public interface ISocialService
    {
        public Task<Result> SendFriendRequest(int requesterId, int receiverId);
        public Task<Result> RespondToFriendRequest(int requesterId, int recieverId, bool accept);

        public Task<Result> BlockUser(int requesterId, int recieverId);

        public Task<Result<IReadOnlyList<UserDTO>>> IncomingRequests(int requesterId);

        public Task<Result> RemoveFriend(int requesterId, int recorderId);
    }
}
