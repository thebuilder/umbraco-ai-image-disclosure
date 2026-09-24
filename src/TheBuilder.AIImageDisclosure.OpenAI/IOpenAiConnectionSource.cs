namespace TheBuilder.AIImageDisclosure.OpenAI;

/// <summary>Provides optional selectable OpenAI connections.</summary>
public interface IOpenAiConnectionSource
{
    /// <summary>Gets whether the source is available.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets sanitized selectable connections.</summary>
    Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken);

    /// <summary>Resolves one selected connection.</summary>
    Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken);
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
