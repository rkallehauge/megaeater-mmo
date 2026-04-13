using Microsoft.EntityFrameworkCore;
using MegaEaterMMO.API.Data.Model;


namespace MegaEaterMMO.API.Data.DB
{
    public class MegaEaterContext : DbContext
    {
        public MegaEaterContext(DbContextOptions<MegaEaterContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Score> Scores { get; set; }
    }
}
