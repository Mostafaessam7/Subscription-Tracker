using FluentAssertions;
using SubscriptionTracker.Infrastructure.Storage;

namespace SubscriptionTracker.Application.UnitTests.Subscriptions;

public class FileStorageProviderValidatorTests
{
    [Fact]
    public void EnsureConfigured_WithLocalProvider_ShouldNotThrowEvenWithNoConnectionString()
    {
        var act = () => FileStorageProviderValidator.EnsureConfigured(FileStorageProvider.Local, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureConfigured_WithAzureBlobProviderAndNoConnectionString_ShouldThrow()
    {
        var act = () => FileStorageProviderValidator.EnsureConfigured(FileStorageProvider.AzureBlob, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*AzureBlob*");
    }

    [Fact]
    public void EnsureConfigured_WithAzureBlobProviderAndBlankConnectionString_ShouldThrow()
    {
        var act = () => FileStorageProviderValidator.EnsureConfigured(FileStorageProvider.AzureBlob, "   ");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureConfigured_WithAzureBlobProviderAndARealConnectionString_ShouldNotThrow()
    {
        var act = () => FileStorageProviderValidator.EnsureConfigured(
            FileStorageProvider.AzureBlob, "UseDevelopmentStorage=true");

        act.Should().NotThrow();
    }
}
