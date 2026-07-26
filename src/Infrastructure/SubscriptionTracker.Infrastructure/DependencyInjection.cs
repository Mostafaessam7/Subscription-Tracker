using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Infrastructure.Persistence;
using SubscriptionTracker.Infrastructure.Persistence.Interceptors;
using SubscriptionTracker.Infrastructure.Persistence.Repositories;

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
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        return services;
    }
}
