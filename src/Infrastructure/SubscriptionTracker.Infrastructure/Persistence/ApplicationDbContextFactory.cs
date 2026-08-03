using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SubscriptionTracker.Application.Abstractions;

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

        // Design-time only (migration generation) - no per-request tenant context exists here, and none is
        // needed since no queries actually run, only model-building.
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? WorkspaceId => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
        public bool HasPermission(string permissionCode) => false;
    }
}
