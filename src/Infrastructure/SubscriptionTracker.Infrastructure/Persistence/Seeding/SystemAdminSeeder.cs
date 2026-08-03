using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Infrastructure.Persistence.Seeding;

/// <summary>
/// Bootstraps the first system administrator from configuration (`SystemAdmin:BootstrapEmail`), since there is
/// otherwise no way to grant the very first admin - every admin-management endpoint itself requires an existing
/// admin. Runs once at API startup (Program.cs, alongside SystemRoleSeeder) and is idempotent: a no-op once the
/// target user already has the flag, and a no-op entirely if the config key is unset or the user hasn't
/// registered yet (retried on every subsequent startup until they do).
/// </summary>
public static class SystemAdminSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var bootstrapEmail = configuration["SystemAdmin:BootstrapEmail"];
        if (string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            return;
        }

        var emailResult = Email.Create(bootstrapEmail);
        if (emailResult.IsFailure)
        {
            return;
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == emailResult.Value, cancellationToken);

        if (user is null || user.IsSystemAdmin)
        {
            return;
        }

        user.GrantSystemAdmin();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
