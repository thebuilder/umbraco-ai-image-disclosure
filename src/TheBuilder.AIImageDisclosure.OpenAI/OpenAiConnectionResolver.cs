using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace TheBuilder.AIImageDisclosure.OpenAI;

internal sealed class OpenAiConnectionResolver(IServiceScopeFactory scopeFactory)
{
    public bool IsAvailable
    {
        get
        {
            using var scope = scopeFactory.CreateScope();
            return HasServices(scope.ServiceProvider);
        }
    }

    public async Task<IReadOnlyList<OpenAiConnectionOption>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var connections = scope.ServiceProvider.GetService<IAIConnectionService>();
        if (!HasServices(scope.ServiceProvider) || connections is null) return [];
        var result = await connections.GetConnectionsAsync("openai", cancellationToken);
        return result.Where(connection => connection.IsActive)
            .OrderBy(connection => connection.Name, StringComparer.OrdinalIgnoreCase)
            .Select(connection => new OpenAiConnectionOption(connection.Id, connection.Name))
            .ToArray();
    }

    public async Task<ResolvedOpenAiConnection?> ResolveAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var connectionService = scope.ServiceProvider.GetService<IAIConnectionService>();
        var modelResolver = scope.ServiceProvider.GetService<IAIEditableModelResolver>();
        if (!HasServices(scope.ServiceProvider) || connectionService is null || modelResolver is null) return null;
        var connection = await connectionService.GetConnectionAsync(connectionId, cancellationToken);
        if (connection is null || !connection.IsActive || !string.Equals(connection.ProviderId, "openai", StringComparison.OrdinalIgnoreCase))
            return null;
        OpenAiConnectionSettings? settings;
        try
        {
            settings = modelResolver.ResolveModel<OpenAiConnectionSettings>(connection.Settings);
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            return null;
        }
        if (settings is null || string.IsNullOrWhiteSpace(settings.ApiKey) || !IsDirectOpenAiEndpoint(settings.Endpoint))
            return null;
        return new ResolvedOpenAiConnection(settings.ApiKey, settings.OrganizationId, connection.Id, connection.Name);
    }

    internal static bool IsDirectOpenAiEndpoint(string? endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, "api.openai.com", StringComparison.OrdinalIgnoreCase)
            && uri.Port == 443
            && uri.UserInfo.Length == 0
            && uri.Query.Length == 0
            && uri.Fragment.Length == 0
            && (uri.AbsolutePath.TrimEnd('/') is "" or "/v1");
    }

    private static bool HasServices(IServiceProvider scopedServices) =>
        scopedServices.GetService<IAIConnectionService>() is not null
        && scopedServices.GetService<IAIEditableModelResolver>() is not null
        && scopedServices.GetService<AIProviderCollection>()?.GetById("openai") is not null;
}

/// <summary>A sanitized OpenAI connection selector entry.</summary>
public sealed record OpenAiConnectionOption(Guid Id, string Name);
internal sealed record ResolvedOpenAiConnection(string ApiKey, string? OrganizationId, Guid Id, string Name);
