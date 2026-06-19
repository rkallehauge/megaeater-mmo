using System.ComponentModel.DataAnnotations;

namespace MegaEaterMMO.API.Data.Model.Relations
{
    public abstract class UserRelation
    {
        [Required]
        public int UserAId { get; set; } 
        [Required]
        public User UserA { get; set; } = null!;

        [Required]
        public int UserBId { get; set; } 
        [Required]
        public User UserB { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
