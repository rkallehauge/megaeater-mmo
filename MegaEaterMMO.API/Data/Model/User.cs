using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace MegaEaterMMO.API.Data.Model
{
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Hash { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Player;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public int TokenVersion { get; set; } = 0; 
    }

    public enum UserRole
    {
        Player = 0,
        Admin = 1
    }
}
