using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SubscriptionTracker.Api.Authorization;
using SubscriptionTracker.Api.Extensions;
using SubscriptionTracker.Api.Services;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Infrastructure.Security;

namespace SubscriptionTracker.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddSignalR();

        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        AddJwtAuthentication(services, configuration);
        AddApiVersioningAndSwagger(services);
        AddCors(services, configuration, environment);
        AddRateLimiting(services);
        AddResponseCompression(services);
        AddHealthChecks(services, configuration);
        AddObservability(services, configuration);

        return services;
    }

    public const string FrontendCorsPolicy = "Frontend";

    private static readonly System.Text.RegularExpressions.Regex LocalhostOriginPattern =
        new(@"^https?://localhost:\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void AddCors(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();

                // In Development, tooling (Angular CLI, Visual Studio's SPA proxy, IIS Express) can put the
                // frontend on any localhost port, not just the one configured origin - so widen the CORS check
                // to any localhost port rather than requiring Cors:AllowedOrigins to be kept in sync with
                // whichever dev host happened to pick a port this run. Production still uses the explicit list.
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(origin => LocalhostOriginPattern.IsMatch(origin));
                }
                else
                {
                    policy.WithOrigins(allowedOrigins);
                }
            });
        });
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                // SignalR's browser client can't attach an Authorization header to the WebSocket/SSE handshake,
                // so it sends the token as an `access_token` query param instead - only honored for the hub path.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
            options.AddPolicy(SystemAdminPolicy, policy => policy.RequireClaim("system_admin", "true")));
    }

    /// <summary>Cross-tenant administration (see <see cref="Controllers.V1.AdminController"/>) - distinct from the
    /// per-workspace "permission" claims, since a system admin isn't necessarily a member of any workspace.</summary>
    public const string SystemAdminPolicy = "SystemAdmin";

    private static void AddApiVersioningAndSwagger(IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddEndpointsApiExplorer();
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }

    /// <summary>Named policy applied to the enumeration-sensitive auth endpoints (forgot-password, verify-email,
    /// reset-password) on top of the global limiter below - these leak account-existence information one guess at
    /// a time, so they get a much tighter per-IP budget than ordinary API traffic. See
    /// <see cref="Controllers.V1.AuthController"/> for the <c>[EnableRateLimiting]</c> attributes that use it.</summary>
    public const string AuthSensitivePolicy = "auth-sensitive";

    /// <summary>Named policy applied to login - a credential-stuffing attacker's primary target, and one the
    /// 100-req/min *global* limiter (partitioned across all API traffic, not just auth) leaves far too much
    /// headroom for: the per-account lockout (5 failed attempts/15 min, see <c>User.RecordFailedLogin</c>) only
    /// helps once an attacker has already guessed a valid email, so it does nothing against credential stuffing
    /// across many different accounts from one IP. Deliberately a separate, more generous policy than
    /// <see cref="AuthSensitivePolicy"/> (a 1-minute window, not 15, and a higher permit count) rather than
    /// reusing it outright - a strict 5-per-15-minutes budget is appropriate for endpoints that only ever fire
    /// on a deliberate, rare user action (forgot password), but login is routine, high-frequency traffic (every
    /// page load after a token expires, every integration test that authenticates via <c>TestAuthHelper</c>),
    /// and a login failure isn't necessarily an attack the way a forgot-password enumeration probe is. Kept as
    /// its own policy separate from <see cref="AuthRegisterPolicy"/> (rather than one shared "auth-throttle"
    /// bucket) specifically so a burst of one doesn't eat into the other's budget - `TestAuthHelper` calls both
    /// on every authenticated integration test, and a shared bucket needed a much higher limit to avoid 429s
    /// mid-test-run purely from that combined volume, which would have diluted the protection on each
    /// individually.</summary>
    public const string AuthLoginPolicy = "auth-login";

    /// <summary>Named policy applied to registration - throttles mass fake-account creation the same way
    /// <see cref="AuthLoginPolicy"/> throttles credential stuffing. See that constant's remarks for why this
    /// is a separate bucket rather than shared with login.</summary>
    public const string AuthRegisterPolicy = "auth-register";

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthSensitivePolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthLoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthRegisterPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }

    private static void AddResponseCompression(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("SubscriptionTrackerDb") ?? string.Empty,
                name: "sql-server",
                tags: ["ready"]);
    }

    /// <summary>
    /// Traces/metrics are always collected in-process; whether they ship anywhere depends entirely on
    /// `OpenTelemetry:OtlpEndpoint` being set (e.g. `http://localhost:4317` for a local OTel Collector). Unset by
    /// default - there's no collector to send to out of the box, and the OTLP exporter would otherwise just log
    /// connection-refused warnings on every export interval.
    /// </summary>
    private static void AddObservability(IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SubscriptionTracker.Api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint));
                }
            });
    }
}
