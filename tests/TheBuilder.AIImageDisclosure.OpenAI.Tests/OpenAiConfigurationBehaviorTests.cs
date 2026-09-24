using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TheBuilder.AIImageDisclosure.OpenAI;
using TheBuilder.AIImageDisclosure.OpenAI.Controllers;
using TheBuilder.AIImageDisclosure.Watermarks;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using UmbracoAiResolver = TheBuilder.AIImageDisclosure.OpenAI.OpenAiConnectionResolver;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiConfigurationBehaviorTests
{
    [Fact]
    public async Task ConfiguredKeyAloneDisablesAutomaticChecksDespiteSavedEnabledSettings()
    {
        var policy = ConfiguredPolicy(enabled: null, key: "secret-key");
        var verifier = CreateVerifier(policy, new StubHandler(_ => throw new InvalidOperationException("must not call")),
            new OpenAiProvenanceSettings(true, Guid.NewGuid()));

        var result = await verifier.VerifyAsync(() => new MemoryStream([1, 2, 3]), "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Disabled, result.Status);
    }

    [Fact]
    public async Task ExplicitConfiguredTestWorksWhenAutomaticChecksAreDisabled()
    {
        var policy = ConfiguredPolicy(enabled: false, key: "secret-key");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"detected\"}]}", Encoding.UTF8, "application/json"),
        });
        var verifier = CreateVerifier(policy, handler, OpenAiProvenanceSettings.Disabled);

        var result = await verifier.VerifyConnectionAsync(null, () => new MemoryStream([1, 2, 3]), "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Detected, result.Status);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("Bearer secret-key", handler.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task EmptyConfiguredKeyDoesNotFallBackToSavedConnection()
    {
        var policy = ConfiguredPolicy(enabled: true, key: "");
        var verifier = CreateVerifier(policy, new StubHandler(_ => throw new InvalidOperationException("must not call")),
            new OpenAiProvenanceSettings(true, Guid.NewGuid()));

        var result = await verifier.VerifyAsync(() => new MemoryStream([1]), "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task UmbracoAiAdapterUsesStandardConfigurationWhenBothSourcesAreInstalled()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TheBuilder:AIImageDisclosure:OpenAI:Enabled"] = "true",
            ["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = "configured-secret",
        }).Build();
        using var services = new ServiceCollection().BuildServiceProvider();
        var source = new UmbracoAiResolver(services.GetRequiredService<IServiceScopeFactory>());
        var policy = new OpenAiVerificationPolicy(configuration, Store(), source);

        var effective = await policy.GetAsync(TestContext.Current.CancellationToken);
        Assert.True(effective.ManagedByConfiguration);
        Assert.True(effective.Enabled);
        Assert.Equal("configured-secret", effective.Connection!.ApiKey);
    }

    [Fact]
    public async Task ManagedSaveRejectsEnabledAndConnectionOverrides()
    {
        var policy = ConfiguredPolicy(enabled: true, key: "secret-key");
        var controller = CreateController(policy, new OpenAiProvenanceSettings(true, null));

        var result = await controller.SaveSettings(new OpenAiSettingsRequest(false, null), TestContext.Current.CancellationToken);

        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

        var connectionOverride = await controller.SaveSettings(
            new OpenAiSettingsRequest(true, Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(connectionOverride);

        var testOverride = await controller.TestConnection(
            new OpenAiTestRequest(Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(testOverride);
    }

    [Fact]
    public async Task SettingsResponseReportsConfigurationWithoutReturningSecret()
    {
        const string secret = "secret-key";
        var policy = ConfiguredPolicy(enabled: true, key: secret);
        var controller = CreateController(policy, OpenAiProvenanceSettings.Disabled);

        var result = await controller.GetSettings(TestContext.Current.CancellationToken);
        var payload = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result).Value;
        var json = JsonSerializer.Serialize(payload);

        Assert.DoesNotContain(secret, json, StringComparison.Ordinal);
        var response = Assert.IsType<OpenAiSettingsResponse>(payload);
        Assert.True(response.ManagedByConfiguration);
        Assert.True(response.ApiKeyConfigured);
        Assert.True(response.Enabled);
        Assert.Equal("ready", response.Status);
        Assert.Null(response.ConnectionId);
    }

    private static OpenAiVerificationPolicy ConfiguredPolicy(bool? enabled, string? key)
    {
        var values = new Dictionary<string, string?>();
        if (enabled is { } enabledValue)
            values["TheBuilder:AIImageDisclosure:OpenAI:Enabled"] = enabledValue.ToString();
        if (key is not null)
            values["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = key;
        return new OpenAiVerificationPolicy(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), Store(), new OpenAiConnectionSourceForTest());
    }

    private static OpenAiWatermarkVerifier CreateVerifier(
        OpenAiVerificationPolicy policy,
        StubHandler handler,
        OpenAiProvenanceSettings settings)
    {
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns(JsonSerializer.Serialize(settings));
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler, disposeHandler: false));
        return new OpenAiWatermarkVerifier(policy, clients,
            Substitute.For<ILogger<OpenAiWatermarkVerifier>>());
    }

    private static OpenAiProvenanceController CreateController(OpenAiVerificationPolicy policy, OpenAiProvenanceSettings settings)
    {
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns(JsonSerializer.Serialize(settings));
        var adminGroup = Substitute.For<IReadOnlyUserGroup>();
        adminGroup.Alias.Returns("admin");
        var user = Substitute.For<IUser>();
        user.Groups.Returns([adminGroup]);
        var security = Substitute.For<IBackOfficeSecurity>();
        security.CurrentUser.Returns(user);
        var accessor = Substitute.For<IBackOfficeSecurityAccessor>();
        accessor.BackOfficeSecurity.Returns(security);
        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"not_detected\"}]}", Encoding.UTF8, "application/json"),
        }), disposeHandler: true));
        var provider = new ServiceCollection()
            .AddSingleton<IKeyValueService>(keyValue)
            .AddSingleton<OpenAiProvenanceSettingsStore>()
            .AddSingleton(policy)
            .AddSingleton(httpFactory)
            .AddSingleton<OpenAiWatermarkVerifier>(services => new OpenAiWatermarkVerifier(
                policy, services.GetRequiredService<IHttpClientFactory>(),
                Substitute.For<ILogger<OpenAiWatermarkVerifier>>()))
            .AddSingleton<IBackOfficeSecurityAccessor>(accessor)
            .BuildServiceProvider();
        return new OpenAiProvenanceController(provider);
    }

    private static OpenAiProvenanceSettingsStore Store()
    {
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns("{}");
        return new OpenAiProvenanceSettingsStore(new ServiceCollection().AddSingleton(keyValue).BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class OpenAiConnectionSourceForTest : IOpenAiConnectionSource
    {
        public bool IsAvailable => false;
        public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]);
        public Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken) => Task.FromResult<ResolvedOpenAiConnection?>(null);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequest = request;
            return Task.FromResult(response(request));
        }
    }
}
