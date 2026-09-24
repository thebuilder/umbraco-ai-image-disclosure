using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TheBuilder.AIImageDisclosure.OpenAI;
using TheBuilder.AIImageDisclosure.OpenAI.Controllers;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace TheBuilder.AIImageDisclosure.OpenAI.Tests;

public sealed class OpenAiProvenanceSettingsControllerTests
{
    [Fact]
    public async Task SaveRetainsSubmittedConnectionWhenDisabled()
    {
        var selectedConnectionId = Guid.NewGuid();
        var controller = CreateController(selectedConnectionId, out var getStoredValue);

        await controller.SaveSettings(new OpenAiSettingsRequest(false, selectedConnectionId), TestContext.Current.CancellationToken);

        var saved = JsonSerializer.Deserialize<OpenAiProvenanceSettings>(getStoredValue(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(saved);
        Assert.False(saved.Enabled);
        Assert.Equal(selectedConnectionId, saved.ConnectionId);
    }

    [Fact]
    public async Task SaveClearsConnectionWhenSelectionIsNull()
    {
        var controller = CreateController(Guid.NewGuid(), out var getStoredValue);

        await controller.SaveSettings(new OpenAiSettingsRequest(false, null), TestContext.Current.CancellationToken);

        var saved = JsonSerializer.Deserialize<OpenAiProvenanceSettings>(getStoredValue(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(saved);
        Assert.False(saved.Enabled);
        Assert.Null(saved.ConnectionId);
    }

    private static OpenAiProvenanceController CreateController(Guid connectionId, out Func<string> getStoredValue)
    {
        var keyValueService = Substitute.For<IKeyValueService>();
        var storedValue = JsonSerializer.Serialize(new OpenAiProvenanceSettings(true, connectionId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        keyValueService.GetValue(Arg.Any<string>()).Returns(_ => storedValue);
        keyValueService.When(service => service.SetValue(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => storedValue = call.ArgAt<string>(1));
        getStoredValue = () => storedValue;

        var adminGroup = Substitute.For<IReadOnlyUserGroup>();
        adminGroup.Alias.Returns("admin");
        var admin = Substitute.For<IUser>();
        admin.Groups.Returns([adminGroup]);
        var backOfficeSecurity = Substitute.For<IBackOfficeSecurity>();
        backOfficeSecurity.CurrentUser.Returns(admin);
        var securityAccessor = Substitute.For<IBackOfficeSecurityAccessor>();
        securityAccessor.BackOfficeSecurity.Returns(backOfficeSecurity);

        var services = new ServiceCollection()
            .AddSingleton<IKeyValueService>(keyValueService)
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton(securityAccessor)
            .AddSingleton<OpenAiProvenanceSettingsStore>()
            .AddSingleton(new OpenAiVerificationPolicy(new ConfigurationBuilder().Build(),
                new OpenAiProvenanceSettingsStore(new ServiceCollection().AddSingleton(keyValueService).BuildServiceProvider()
                    .GetRequiredService<IServiceScopeFactory>()), new EmptySource()))
            .BuildServiceProvider();
        return new OpenAiProvenanceController(services);
    }

    private sealed class EmptySource : IOpenAiConnectionSource
    {
        public bool IsAvailable => false;
        public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]);
        public Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken) => Task.FromResult<ResolvedOpenAiConnection?>(null);
    }
}
