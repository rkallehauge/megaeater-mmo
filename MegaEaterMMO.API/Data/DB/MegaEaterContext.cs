using Microsoft.EntityFrameworkCore;
using MegaEaterMMO.API.Data.Model;
using MegaEaterMMO.API.Data.Model.Relations;


namespace MegaEaterMMO.API.Data.DB
{
    public class MegaEaterContext : DbContext
    {
        public MegaEaterContext(DbContextOptions<MegaEaterContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite keys
            // Configure the Friendship entity to have a composite key of UserAId and UserBId
            modelBuilder.Entity<UserFriendship>()
                .HasKey(f => new { f.UserAId, f.UserBId }
            );

            modelBuilder.Entity<UserBlocks>()
                .HasKey(f => new { f.UserAId, f.UserBId }
            );

            // Constraint to ensure UserAId is always less than UserBId to prevent duplicate entries like (1,2) and (2,1)
            modelBuilder.Entity<UserFriendship>()
                .ToTable(t =>
                    t.HasCheckConstraint(
                        "CK_UserRelations_UserAId_LessThan_UserBId", 
                        "\"UserAId\" < \"UserBId\"" // psql specific syntax, will NOT work with other vendors 💀
                    )
                );


            
            // Foregin key(s)
            modelBuilder.Entity<UserFriendship>()
                .HasOne(u => u.UserA)
                .WithMany()
                .HasForeignKey(f => f.UserAId)
                .OnDelete(DeleteBehavior.Restrict); // restrict cascading deletions of relationships (for now)

            modelBuilder.Entity<UserFriendship>()
                .HasOne(u => u.UserB)
                .WithMany()
                .HasForeignKey(f => f.UserBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBlocks>()
                .HasOne(u => u.UserA)
                .WithMany()
                .HasForeignKey(f => f.UserAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBlocks>()
                .HasOne(u => u.UserB)
                .WithMany()
                .HasForeignKey(f => f.UserBId)
                .OnDelete(DeleteBehavior.Restrict);

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Score> Scores { get; set; }

        public DbSet<UserFriendship> UserFriendships { get; set; }
        public DbSet<UserBlocks> UserBlocks { get; set; }
    }
}
