using Microsoft.Extensions.Configuration;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiVerificationPolicy(
    IConfiguration configuration,
    OpenAiProvenanceSettingsStore settingsStore,
    IOpenAiConnectionSource connectionSource)
{
    internal const string Prefix = "TheBuilder:AIImageDisclosure:OpenAI";

    public bool IsConfigurationManaged => HasConfigurationKey("Enabled") || HasConfigurationKey("ApiKey");
    public bool HasConfiguredApiKey => !string.IsNullOrWhiteSpace(configuration[$"{Prefix}:ApiKey"]);
    public bool IsConfigurationEnabled => bool.TryParse(configuration[$"{Prefix}:Enabled"], out var enabled) && enabled;

    public async Task<OpenAiEffectiveSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (IsConfigurationManaged)
        {
            var enabled = IsConfigurationEnabled;
            var apiKey = configuration[$"{Prefix}:ApiKey"];
            var apiKeyConfigured = !string.IsNullOrWhiteSpace(apiKey);
            return new OpenAiEffectiveSettings(enabled, true, apiKeyConfigured, apiKeyConfigured, null,
                apiKeyConfigured ? ResolveConfigured(apiKey!) : null);
        }
        var saved = settingsStore.Get();
        if (!saved.Enabled)
            return new OpenAiEffectiveSettings(false, false, false, connectionSource.IsAvailable, saved.ConnectionId, null);
        return new OpenAiEffectiveSettings(saved.Enabled, false, false, connectionSource.IsAvailable, saved.ConnectionId,
            saved.ConnectionId is { } id ? await connectionSource.ResolveAsync(id, cancellationToken) : null);
    }

    public async Task<OpenAiPolicyResult> SaveAsync(bool enabled, Guid? connectionId, CancellationToken cancellationToken)
    {
        if (IsConfigurationManaged)
        {
            if (enabled != IsConfigurationEnabled)
                return OpenAiPolicyResult.Failure("The enabled setting is controlled by standard configuration.");
            if (connectionId is not null)
                return OpenAiPolicyResult.Failure("The connection selection is controlled by standard configuration.");
            return OpenAiPolicyResult.Success;
        }
        if (enabled)
        {
            if (connectionId is not { } id)
                return OpenAiPolicyResult.Failure("Select an active OpenAI connection before enabling the fallback.");
            if (await connectionSource.ResolveAsync(id, cancellationToken) is null)
                return OpenAiPolicyResult.Failure("The selected connection must be active, use the OpenAI provider, and target api.openai.com directly.");
        }
        settingsStore.Save(new OpenAiProvenanceSettings(enabled, connectionId));
        return OpenAiPolicyResult.Success;
    }

    public async Task<OpenAiPolicyResult> ValidateTestAsync(Guid? connectionId, CancellationToken cancellationToken)
    {
        if (IsConfigurationManaged)
            return connectionId is null && HasConfiguredApiKey
                ? OpenAiPolicyResult.Success
                : OpenAiPolicyResult.Failure(connectionId is null
                    ? "No API key is configured."
                    : "The credential source is controlled by standard configuration.");
        return connectionId is { } id && id != Guid.Empty && await connectionSource.ResolveAsync(id, cancellationToken) is not null
            ? OpenAiPolicyResult.Success
            : OpenAiPolicyResult.Failure("Select an active OpenAI connection to test.");
    }

    public async Task<ResolvedOpenAiConnection?> ResolveExplicitAsync(Guid? connectionId, CancellationToken cancellationToken)
    {
        if (IsConfigurationManaged)
            return connectionId is null && HasConfiguredApiKey ? ResolveConfigured(configuration[$"{Prefix}:ApiKey"]!) : null;
        return connectionId is { } id ? await connectionSource.ResolveAsync(id, cancellationToken) : null;
    }

    public Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken) =>
        IsConfigurationManaged ? Task.FromResult<IReadOnlyList<OpenAiConnectionOption>>([]) : connectionSource.GetConnectionsAsync(cancellationToken);

    private ResolvedOpenAiConnection ResolveConfigured(string apiKey) => new(
        apiKey, configuration[$"{Prefix}:OrganizationId"], Guid.Empty, "Standard configuration");

    private bool HasConfigurationKey(string key) => configuration.GetSection(Prefix).GetChildren()
        .Any(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
}

internal sealed record OpenAiEffectiveSettings(bool Enabled, bool ManagedByConfiguration, bool ApiKeyConfigured, bool Available,
    Guid? ConnectionId, ResolvedOpenAiConnection? Connection);

internal sealed record OpenAiPolicyResult(bool IsValid, string? Error)
{
    public static OpenAiPolicyResult Success { get; } = new(true, null);
    public static OpenAiPolicyResult Failure(string error) => new(false, error);
}
