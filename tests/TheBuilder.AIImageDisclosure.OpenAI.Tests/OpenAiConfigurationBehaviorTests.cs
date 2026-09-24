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
        var resolver = ConfiguredResolver(enabled: null, key: "secret-key");
        var verifier = CreateVerifier(resolver, new StubHandler(_ => throw new InvalidOperationException("must not call")),
            new OpenAiProvenanceSettings(true, Guid.NewGuid()));

        var result = await verifier.VerifyAsync(() => new MemoryStream([1, 2, 3]), "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Disabled, result.Status);
    }

    [Fact]
    public async Task ExplicitConfiguredTestWorksWhenAutomaticChecksAreDisabled()
    {
        var resolver = ConfiguredResolver(enabled: false, key: "secret-key");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"results\":[{\"type\":\"synthid\",\"outcome\":\"detected\"}]}", Encoding.UTF8, "application/json"),
        });
        var verifier = CreateVerifier(resolver, handler, OpenAiProvenanceSettings.Disabled);

        var result = await verifier.VerifyConnectionAsync(null, () => new MemoryStream([1, 2, 3]), "image/png", TestContext.Current.CancellationToken);

        Assert.Equal(ImageWatermarkStatus.Detected, result.Status);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("Bearer secret-key", handler.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task EmptyConfiguredKeyDoesNotFallBackToSavedConnection()
    {
        var resolver = ConfiguredResolver(enabled: true, key: "");
        var verifier = CreateVerifier(resolver, new StubHandler(_ => throw new InvalidOperationException("must not call")),
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
        using var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        var resolver = new UmbracoAiResolver(services.GetRequiredService<IServiceScopeFactory>(), configuration);

        Assert.True(resolver.IsConfigurationManaged);
        Assert.True(resolver.IsEnabled);
        Assert.Equal("configured-secret", (await resolver.ResolveAsync(null, TestContext.Current.CancellationToken))!.ApiKey);
    }

    [Fact]
    public async Task ManagedSaveRejectsEnabledAndConnectionOverrides()
    {
        var resolver = ConfiguredResolver(enabled: true, key: "secret-key");
        var controller = CreateController(resolver, new OpenAiProvenanceSettings(true, null));

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
        var resolver = ConfiguredResolver(enabled: true, key: secret);
        var controller = CreateController(resolver, OpenAiProvenanceSettings.Disabled);

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

    private static IOpenAiConnectionResolver ConfiguredResolver(bool? enabled, string? key)
    {
        var values = new Dictionary<string, string?>();
        if (enabled is { } enabledValue)
            values["TheBuilder:AIImageDisclosure:OpenAI:Enabled"] = enabledValue.ToString();
        if (key is not null)
            values["TheBuilder:AIImageDisclosure:OpenAI:ApiKey"] = key;
        return new OpenAiConfigurationResolver(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    private static OpenAiWatermarkVerifier CreateVerifier(
        IOpenAiConnectionResolver resolver,
        StubHandler handler,
        OpenAiProvenanceSettings settings)
    {
        var keyValue = Substitute.For<IKeyValueService>();
        keyValue.GetValue(Arg.Any<string>()).Returns(JsonSerializer.Serialize(settings));
        var storeServices = new ServiceCollection().AddSingleton(keyValue).BuildServiceProvider();
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler, disposeHandler: false));
        return new OpenAiWatermarkVerifier(resolver, clients,
            new OpenAiProvenanceSettingsStore(storeServices.GetRequiredService<IServiceScopeFactory>()),
            Substitute.For<ILogger<OpenAiWatermarkVerifier>>());
    }

    private static OpenAiProvenanceController CreateController(IOpenAiConnectionResolver resolver, OpenAiProvenanceSettings settings)
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
            .AddSingleton(resolver)
            .AddSingleton(httpFactory)
            .AddSingleton<OpenAiWatermarkVerifier>(services => new OpenAiWatermarkVerifier(
                resolver, services.GetRequiredService<IHttpClientFactory>(), services.GetRequiredService<OpenAiProvenanceSettingsStore>(),
                Substitute.For<ILogger<OpenAiWatermarkVerifier>>()))
            .AddSingleton<IBackOfficeSecurityAccessor>(accessor)
            .BuildServiceProvider();
        return new OpenAiProvenanceController(provider);
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
