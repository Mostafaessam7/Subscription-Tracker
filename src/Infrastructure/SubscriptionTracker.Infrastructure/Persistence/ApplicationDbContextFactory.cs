using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SubscriptionTracker.Infrastructure.Persistence;

/// <summary>Enables `dotnet ef migrations` to construct the context without running the full application host.</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SubscriptionTracker;Trusted_Connection=True;MultipleActiveResultSets=true",
            sql => sql.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
