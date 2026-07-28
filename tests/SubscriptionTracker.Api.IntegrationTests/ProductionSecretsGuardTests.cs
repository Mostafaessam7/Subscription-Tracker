using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SubscriptionTracker.Api.Startup;

namespace SubscriptionTracker.Api.IntegrationTests;

public class ProductionSecretsGuardTests
{
    private static IConfiguration BuildConfiguration(string? signingKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(signingKey is null
                ? []
                : new Dictionary<string, string?> { ["Jwt:SigningKey"] = signingKey })
            .Build();

    [Fact]
    public void EnsureJwtSigningKeyIsConfigured_WhenNotProduction_ShouldNotThrowEvenWithPlaceholder()
    {
        var configuration = BuildConfiguration(ProductionSecretsGuard.DevPlaceholderSigningKey);

        var act = () => ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(configuration, isProduction: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureJwtSigningKeyIsConfigured_InProductionWithPlaceholderKey_ShouldThrow()
    {
        var configuration = BuildConfiguration(ProductionSecretsGuard.DevPlaceholderSigningKey);

        var act = () => ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(configuration, isProduction: true);

        act.Should().Throw<InvalidOperationException>().WithMessage("*placeholder*");
    }

    [Fact]
    public void EnsureJwtSigningKeyIsConfigured_InProductionWithNoKeyConfigured_ShouldThrow()
    {
        var configuration = BuildConfiguration(null);

        var act = () => ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(configuration, isProduction: true);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureJwtSigningKeyIsConfigured_InProductionWithARealKey_ShouldNotThrow()
    {
        var configuration = BuildConfiguration("a-real-64-character-or-longer-secret-pulled-from-a-secret-manager");

        var act = () => ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(configuration, isProduction: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureJwtSigningKeyIsConfigured_ShouldReadFromARealEnvironmentVariableViaDoubleUnderscoreConvention()
    {
        // Confirms the exact mechanism operators are told to use in HANDOVER.md/README: setting a real
        // Jwt__SigningKey OS environment variable, through ASP.NET Core's actual AddEnvironmentVariables()
        // provider (which performs the __ -> : translation), not a hand-simulated stand-in for it.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "from-a-real-env-var-secret-value");
        try
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            var act = () => ProductionSecretsGuard.EnsureJwtSigningKeyIsConfigured(configuration, isProduction: true);

            act.Should().NotThrow();
        }
        finally
        {
            Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
        }
    }
}
