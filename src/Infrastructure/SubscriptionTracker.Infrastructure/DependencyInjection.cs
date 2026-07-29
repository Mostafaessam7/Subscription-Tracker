using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Infrastructure.BackgroundJobs;
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

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITwoFactorService, TotpService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        AddBackgroundJobs(services);

        return services;
    }

    private static void AddBackgroundJobs(IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
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
