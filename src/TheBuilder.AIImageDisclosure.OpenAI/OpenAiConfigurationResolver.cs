using Microsoft.Extensions.Configuration;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiConfigurationResolver(IConfiguration configuration) : IOpenAiConnectionResolver
{
    internal const string Prefix = "TheBuilder:AIImageDisclosure:OpenAI";

    public bool IsConfigurationManaged => HasConfigurationKey("Enabled") || HasConfigurationKey("ApiKey");
    public bool HasConfiguredApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public bool IsAvailable => HasConfiguredApiKey;
    public bool IsEnabled => bool.TryParse(configuration[$"{Prefix}:Enabled"], out var enabled) && enabled;

    public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]);

    public Task<ResolvedOpenAiConnection?> ResolveAsync(Guid? connectionId, CancellationToken cancellationToken)
    {
        if (!HasConfiguredApiKey)
            return Task.FromResult<ResolvedOpenAiConnection?>(null);
        return Task.FromResult<ResolvedOpenAiConnection?>(new ResolvedOpenAiConnection(
            ApiKey!, configuration[$"{Prefix}:OrganizationId"], Guid.Empty, "Standard configuration"));
    }

    internal string? ApiKey => configuration[$"{Prefix}:ApiKey"];

    private bool HasConfigurationKey(string key) => configuration.GetSection(Prefix).GetChildren()
        .Any(section => string.Equals(section.Key, key, StringComparison.OrdinalIgnoreCase));
}
