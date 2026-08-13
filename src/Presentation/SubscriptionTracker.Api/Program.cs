using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SubscriptionTracker.Api;
using SubscriptionTracker.Api.Startup;
using SubscriptionTracker.Application;
using SubscriptionTracker.Infrastructure;
using SubscriptionTracker.Infrastructure.BackgroundJobs;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName());

builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration, builder.Environment);

ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(builder.Configuration, builder.Environment.IsProduction());

var app = builder.Build();

if (builder.Configuration.GetValue("ApplyMigrationsOnStartup", defaultValue: true))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    await SystemRoleSeeder.SeedAsync(dbContext);
    await SystemAdminSeeder.SeedAsync(dbContext, builder.Configuration);

    // Quartz's persistent job store (see Infrastructure.DependencyInjection.AddBackgroundJobs) needs its
    // QRTZ_* tables created before the scheduler starts - EF migrations don't own this schema since it's
    // Quartz's own, framework-agnostic table set, not one of our aggregates.
    var connectionString = builder.Configuration.GetConnectionString("SubscriptionTrackerDb")
        ?? throw new InvalidOperationException("ConnectionStrings:SubscriptionTrackerDb is required.");
    await QuartzSchemaInitializer.EnsureSchemaAsync(connectionString);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRateLimiter();

app.UseCors(SubscriptionTracker.Api.DependencyInjection.FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SubscriptionTracker.Api.Hubs.NotificationsHub>("/hubs/notifications");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.Run();

/// <summary>Entry point partial class, exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
