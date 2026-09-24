using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TheBuilder.AIImageDisclosure.OpenAI;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiVerificationPolicyTests
{
    [Fact]
    public async Task KeyAloneIsManagedButDoesNotEnableAutomaticChecks()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>
        {
            [$"{OpenAiVerificationPolicy.Prefix}:ApiKey"] = "secret-value",
        });
        var effective = await policy.GetAsync(TestContext.Current.CancellationToken);
        Assert.True(effective.ManagedByConfiguration);
        Assert.False(effective.Enabled);
        Assert.True(effective.ApiKeyConfigured);
        Assert.NotNull(effective.Connection);
        Assert.DoesNotContain("secret-value", effective.Connection.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitEnableResolvesConfiguredCredentials()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>
        {
            [$"{OpenAiVerificationPolicy.Prefix}:Enabled"] = "true",
            [$"{OpenAiVerificationPolicy.Prefix}:ApiKey"] = "secret-value",
            [$"{OpenAiVerificationPolicy.Prefix}:OrganizationId"] = "org-value",
        });
        var effective = await policy.GetAsync(TestContext.Current.CancellationToken);
        Assert.True(effective.Enabled);
        Assert.Equal("secret-value", effective.Connection!.ApiKey);
        Assert.Equal("org-value", effective.Connection.OrganizationId);
    }

    [Fact]
    public async Task ExplicitNullApiKeyStillPreventsSavedConnectionFallback()
    {
        var policy = CreatePolicy(new Dictionary<string, string?>
        {
            [$"{OpenAiVerificationPolicy.Prefix}:Enabled"] = "true",
            [$"{OpenAiVerificationPolicy.Prefix}:ApiKey"] = null,
        });
        var effective = await policy.GetAsync(TestContext.Current.CancellationToken);
        Assert.True(effective.ManagedByConfiguration);
        Assert.True(effective.Enabled);
        Assert.False(effective.ApiKeyConfigured);
        Assert.Null(effective.Connection);
    }

    [Fact]
    public async Task DisabledSavedSettingsReportUnavailableWhenNoConnectionSourceExists()
    {
        var configuration = new ConfigurationBuilder().Build();
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns("{\"enabled\":false}");
        var store = new OpenAiProvenanceSettingsStore(new ServiceCollection().AddSingleton(keyValue)
            .BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
        var policy = new OpenAiVerificationPolicy(configuration, store, new EmptyOpenAiConnectionSource());

        var effective = await policy.GetAsync(TestContext.Current.CancellationToken);

        Assert.False(effective.Enabled);
        Assert.False(effective.Available);
    }

    private static OpenAiVerificationPolicy CreatePolicy(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns("{\"enabled\":true}");
        var store = new OpenAiProvenanceSettingsStore(new ServiceCollection().AddSingleton(keyValue)
            .BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
        return new OpenAiVerificationPolicy(configuration, store, new OpenAiConnectionSourceForTest());
    }

    private sealed class OpenAiConnectionSourceForTest : IOpenAiConnectionSource
    {
        public bool IsAvailable => true;
        public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]);
        public Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken) =>
            Task.FromResult<ResolvedOpenAiConnection?>(new ResolvedOpenAiConnection("saved-key", null, connectionId, "saved"));
    }

}
