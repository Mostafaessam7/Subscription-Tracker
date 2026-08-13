using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Infrastructure.BackgroundJobs;
using SubscriptionTracker.Infrastructure.Financial;
using SubscriptionTracker.Infrastructure.Notifications;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Infrastructure.Persistence.Interceptors;
using SubscriptionTracker.Infrastructure.Persistence.Repositories;
using SubscriptionTracker.Infrastructure.Security;
using SubscriptionTracker.Infrastructure.Storage;

namespace SubscriptionTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("SubscriptionTrackerDb"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                    sql.EnableRetryOnFailure(maxRetryCount: 3);
                });

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<ExchangeRatesOptions>(configuration.GetSection(ExchangeRatesOptions.SectionName));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITwoFactorService, TotpService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        AddFileStorage(services, configuration);
        services.AddSingleton<IExchangeRateProvider, StaticExchangeRateProvider>();
        services.AddSingleton<IBackgroundJobTrigger, QuartzBackgroundJobTrigger>();

        AddBackgroundJobs(services, configuration);

        return services;
    }

    /// <summary>Selects the IFileStorageService implementation via FileStorage:Provider (defaults to Local).
    /// Fails fast at startup rather than on first upload if AzureBlob is selected without a connection string -
    /// same "surface the misconfiguration immediately" philosophy as ProductionSecretsGuard.</summary>
    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<FileStorageProvider?>($"{FileStorageOptions.SectionName}:Provider")
            ?? FileStorageProvider.Local;

        var connectionString = configuration[$"{FileStorageOptions.SectionName}:Blob:ConnectionString"];
        FileStorageProviderValidator.EnsureConfigured(provider, connectionString);

        if (provider == FileStorageProvider.AzureBlob)
        {
            services.AddSingleton<IFileStorageService, AzureBlobFileStorageService>();
        }
        else
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        }
    }

    /// <summary>
    /// Backed by SQL Server (the same database as everything else - see QuartzSchemaInitializer for how its
    /// QRTZ_* tables get created) rather than the default in-memory RAMJobStore, so scheduled triggers survive
    /// an API restart and - the actual motivating reason - don't duplicate-fire if this API is ever scaled to
    /// more than one replica (RAMJobStore has no cross-instance coordination at all; every replica would run
    /// every job independently). UseProperties=true keeps job/trigger data as strings instead of requiring
    /// .NET binary serialization, and UseSystemTextJsonSerializer avoids Quartz's legacy BinaryFormatter-based
    /// serializer (deprecated/insecure, and blocked entirely on newer .NET).
    /// </summary>
    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SubscriptionTrackerDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:SubscriptionTrackerDb is required (Quartz's persistent job store uses the same database).");

        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.RetryInterval = TimeSpan.FromSeconds(15);
                store.UseSqlServer(sql => sql.ConnectionString = connectionString);
                store.UseSystemTextJsonSerializer();
            });

            AddDailyJob<RenewalReminderJob>(quartz, "renewal-reminder", "0 0 6 * * ?");
            AddDailyJob<AutoRenewalJob>(quartz, "auto-renewal", "0 15 6 * * ?");
            AddDailyJob<ExpireSubscriptionsJob>(quartz, "expire-subscriptions", "0 30 6 * * ?");
            AddDailyJob<BudgetAlertJob>(quartz, "budget-alert", "0 45 6 * * ?");
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }

    private static void AddDailyJob<TJob>(IServiceCollectionQuartzConfigurator quartz, string name, string cronExpression)
        where TJob : IJob
    {
        var jobKey = new JobKey(name);
        quartz.AddJob<TJob>(opts => opts.WithIdentity(jobKey));
        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity($"{name}-trigger")
            .WithCronSchedule(cronExpression));
    }
}
