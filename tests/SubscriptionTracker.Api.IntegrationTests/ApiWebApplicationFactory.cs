using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Api.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SubscriptionTracker.Tests.{Guid.NewGuid():N}";
    private const string MasterConnectionString = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting rather than ConfigureAppConfiguration, and the difference is not cosmetic.
        //
        // ConfigureAppConfiguration callbacks are applied while the host is being built — after
        // Program.cs has already run AddInfrastructure(builder.Configuration). Quartz reads the
        // connection string eagerly there and closes over it, so the override arrived too late for
        // the scheduler: EF Core used the per-test database while Quartz used the one named in
        // appsettings. UseSetting writes into host configuration early enough for both to see it.
        //
        // These tests passed locally only because a "SubscriptionTracker" database already existed
        // from earlier runs. On a clean machine the scheduler failed schema validation during
        // startup with SQL error 4060 and took every integration test down with it. They were
        // never self-contained; a fresh CI runner is what made that visible.
        builder.UseSetting(
            "ConnectionStrings:SubscriptionTrackerDb",
            $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true");

        builder.ConfigureServices(services =>
        {
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID('{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
        command.ExecuteNonQuery();
    }
}
