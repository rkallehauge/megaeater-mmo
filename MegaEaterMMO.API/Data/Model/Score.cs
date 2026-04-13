using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace MegaEaterMMO.API.Data.Model
{
    public class Score
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public User User { get; set; }

        [Required]
        public int Value { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
