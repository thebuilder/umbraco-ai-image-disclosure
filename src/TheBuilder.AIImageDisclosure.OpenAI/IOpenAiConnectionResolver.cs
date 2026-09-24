namespace TheBuilder.AIImageDisclosure.OpenAI;

/// <summary>Resolves OpenAI credentials from the configured credential source.</summary>
public interface IOpenAiConnectionResolver
{
    /// <summary>Gets whether standard configuration supplies an API key.</summary>
    bool IsConfigurationManaged { get; }

    /// <summary>Gets whether a credential source is available.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets whether standard configuration contains a nonempty API key.</summary>
    bool HasConfiguredApiKey { get; }

    /// <summary>Gets whether configuration explicitly enables automatic verification.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets sanitized selectable connections.</summary>
    Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken);

    /// <summary>Resolves credentials for automatic or explicitly selected verification.</summary>
    Task<ResolvedOpenAiConnection?> ResolveAsync(Guid? connectionId, CancellationToken cancellationToken);
}

/// <summary>A sanitized OpenAI connection selector entry.</summary>
public sealed record OpenAiConnectionOption(Guid Id, string Name);

/// <summary>Resolved credentials held only in memory while making one request.</summary>
public sealed class ResolvedOpenAiConnection(string apiKey, string? organizationId, Guid id, string name)
{
    /// <summary>Gets the resolved API key for the active request.</summary>
    public string ApiKey { get; } = apiKey;
    /// <summary>Gets the optional organization ID.</summary>
    public string? OrganizationId { get; } = organizationId;
    /// <summary>Gets the source connection ID.</summary>
    public Guid Id { get; } = id;
    /// <summary>Gets the sanitized source name.</summary>
    public string Name { get; } = name;
    /// <inheritdoc />
    public override string ToString() => $"{nameof(ResolvedOpenAiConnection)}({Name}, {Id})";
}
