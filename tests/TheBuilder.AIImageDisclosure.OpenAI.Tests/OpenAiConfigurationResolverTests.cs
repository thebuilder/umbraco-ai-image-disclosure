using Microsoft.Extensions.Configuration;
using TheBuilder.AIImageDisclosure.OpenAI;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiConfigurationResolverTests
{
    [Fact]
    public async Task RequiresExplicitEnabledAndDoesNotExposeTheConfiguredSecret()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = "secret-value",
        });

        Assert.True(resolver.IsConfigurationManaged);
        Assert.True(resolver.HasConfiguredApiKey);
        Assert.False(resolver.IsEnabled);
        var resolved = await resolver.ResolveAsync(null, TestContext.Current.CancellationToken);
        Assert.NotNull(resolved);
        Assert.Equal("secret-value", resolved.ApiKey);
    }

    [Fact]
    public async Task ResolvesConfiguredCredentialsWhenExplicitlyEnabled()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["TheBuilder:AIImageDisclosure:OpenAI:Enabled"] = "true",
            ["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = "secret-value",
            ["TheBuilder:AIImageDisclosure:OpenAI:OrganizationId"] = "org-value",
        });

        var resolved = await resolver.ResolveAsync(null, TestContext.Current.CancellationToken);

        Assert.NotNull(resolved);
        Assert.Equal("secret-value", resolved.ApiKey);
        Assert.Equal("org-value", resolved.OrganizationId);
        Assert.DoesNotContain("secret-value", resolved.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyApiKeyWithExplicitEnabledIsManagedButUnavailable()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["TheBuilder:AIImageDisclosure:OpenAI:Enabled"] = "true",
            ["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = "",
        });

        Assert.True(resolver.IsConfigurationManaged);
        Assert.True(resolver.IsEnabled);
        Assert.False(resolver.HasConfiguredApiKey);
        Assert.False(resolver.IsAvailable);
    }

    [Fact]
    public void ExplicitNullApiKeyStillControlsSourceSelection()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = null,
        });

        Assert.True(resolver.IsConfigurationManaged);
        Assert.False(resolver.HasConfiguredApiKey);
    }

    private static OpenAiConfigurationResolver CreateResolver(Dictionary<string, string?> values) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
}
