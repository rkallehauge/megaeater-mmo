using System.ComponentModel.DataAnnotations;

namespace MegaEaterMMO.API.Data.Model.Relations
{
    public class UserFriendship : UserRelation
    {
        [Required]
        public Status Friendship { get; set; } = Status.Pending;

        public int RequesterId { get; set; } // Store who requested it
        public int RecieverId { get; set; } // And who recieved it 


        public UserFriendship() { }

        public enum Status
        {
            Friends,
            Pending
        }
    }
}
