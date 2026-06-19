using MegaEaterMMO.Shared;
using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.API.Data.Model.Relations;
using MegaEaterMMO.API.Data.DB;
using RabbitMQ.Client;
using Microsoft.IdentityModel.Tokens;
using MegaEaterMMO.Shared.DTO.User;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MegaEaterMMO.API.Service.Users
{
    public class SocialService : ISocialService
    {

        private IUserService _userService;
        private MegaEaterContext _context;
        private IConnection _queue; 


        public SocialService(IUserService userService, MegaEaterContext context, IConnection queue)
        {
            _userService = userService;
            _context = context;
            _queue = queue;
        }

        public Task<Result> BlockUser(int requesterId, int recieverId)
        {


            var userA = _userService.GetUser(requesterId); // nullchecks handled in service
            if (!userA.Success)
            {
                return Task.FromResult(Result.Fail(new Error("UserNotFound", $"User with ID {requesterId} not found.")));
            }

            var userB = _userService.GetUser(recieverId); // nullchecks handled in service
            if (!userB.Success)
            {
                return Task.FromResult(Result.Fail(new Error("UserNotFound", $"User with ID {recieverId} not found.")));
            }

            UserBlocks block = new()
            {
                UserA = userA.Data!,
                UserB = userB.Data!,
                UserAId = requesterId,
                UserBId = recieverId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserBlocks.Add(block);
            var result = _context.SaveChanges();
            if (result == 1)
            {
                return Task.FromResult(Result.OK());
            }

            return Task.FromResult(Result.Fail(new Error("DatabaseError", "An error occurred while saving the block to the database.")));
        }

        public Task<Result<IReadOnlyList<UserDTO>>> IncomingRequests(int requesterId)
        {
            var list = _context.UserFriendships
                .Where(uf => uf.RecieverId == requesterId && uf.Friendship == UserFriendship.Status.Pending)
                .Join(
                _context.Users,
                friendship => friendship.RequesterId,
                user => user.Id,
                (friendship, user) => new UserDTO
                {
                    Id = user.Id,
                    Username = user.Username
                }).ToImmutableList<UserDTO>();

            var task = Task.FromResult(Result<IReadOnlyList<UserDTO>>.OK(list));

            return task;
        }

        public Task<Result> RemoveFriend(int requesterId, int recieverId)
        {
            throw  new NotImplementedException();
        }

        public Task<Result> RespondToFriendRequest(int requesterId, int recieverId, bool accept)
        {
           if(accept)
                return SetRelation(requesterId,recieverId, UserFriendship.Status.Friends);

            var toBeRemoved = _context.UserFriendships.FirstOrDefault(uf => uf.RecieverId == recieverId && uf.RequesterId == requesterId);
            _context.UserFriendships.Remove(toBeRemoved);

            var changes = _context.SaveChanges();
            if (changes == 1)
            {
                return Task.FromResult(Result.OK());
            }

            return Task.FromResult(Result.Fail(new Error("InvalidDecline", "Could not decline friend request.")));
        }

        public Task<Result> SendFriendRequest(int requesterId, int receiverId)
        {
            return SetRelation(requesterId, receiverId, UserFriendship.Status.Pending);
        }

        public Task<Result> SetRelation(int requesterId, int recieverId, UserFriendship.Status status)
        {

            if(requesterId == recieverId)
            {
                return Task.FromResult(Result.Fail(new Error("InvalidRelation", "A user cannot have a relation with themselves.")));
            }

            // enforce unique composite key of UserA.id and UserB.id by always ordering them the same way
            int userAId = Math.Min(requesterId, recieverId);
            int userBId = Math.Max(requesterId, recieverId);

            // If existing relation exists, update it. Otherwise, create a new one.
            UserFriendship? relation = _context.UserFriendships.Where(r => r.UserAId == userAId && r.UserBId == userBId).FirstOrDefault();
            if (relation != null) // uhhhhh
            {
              
                relation.Friendship = status;
                _context.UserFriendships.Update(relation);
            }
            else
            {
                var userA = _userService.GetUser(userAId);
                if (!userA.Success) // nullchecks handled in service
                {
                    return Task.FromResult(Result.Fail(new Error("UserNotFound", $"User with ID {userAId} not found.")));
                }

                var userB = _userService.GetUser(userBId);
                if(!userB.Success) // nullchecks handled in service 
                {
                    return Task.FromResult(Result.Fail(new Error("UserNotFound", $"User with ID {userBId} not found.")));
                }

                UserFriendship newRelation = new()
                {
                    UserA = userA.Data!,
                    UserB = userB.Data!,
                    UserAId = userAId,
                    UserBId = userBId,
                    RequesterId = requesterId,
                    RecieverId = recieverId,
                    Friendship = status
                };

                _context.UserFriendships.Add(newRelation);
            }
            var result = _context.SaveChanges();
            if (result == 1)
            {
                return Task.FromResult(Result.OK());
            }
            return Task.FromResult(Result.Fail(new Error("DatabaseError", "An error occurred while saving the friend request to the database.")));
        }
    }
}
