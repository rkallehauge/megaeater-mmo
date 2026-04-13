using MegaEaterMMO.API.Data.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MegaEaterMMO.API.Migrations.Builder
{
    public class MegaEaterContextFactory : IDesignTimeDbContextFactory<MegaEaterContext>
    {
        public MegaEaterContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MegaEaterContext>();

            optionsBuilder.UseNpgsql(
                // Only for test environment, real prod will involve .env vars
                "Host=localhost;Port=5432;Database=megaeater;Username=postgres;Password=postgres"
            );

            return new MegaEaterContext(optionsBuilder.Options);
        }
    }
}
