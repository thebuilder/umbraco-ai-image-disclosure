using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.OpenAI.Controllers;

/// <summary>Provides administrator-only OpenAI provenance settings and connection tests.</summary>
[ApiController]
[Route("umbraco/backoffice/api/ai-image-disclosure/openai")]
public sealed class OpenAiProvenanceController(IServiceProvider services) : UmbracoAuthorizedController
{
    /// <summary>Gets sanitized settings and active OpenAI connections.</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        if (!IsAdministrator()) return Forbid();
        var available = IsAvailable();
        var settings = services.GetRequiredService<OpenAiProvenanceSettingsStore>().Get();
        var connectionResolver = services.GetRequiredService<IOpenAiConnectionResolver>();
        var connections = await connectionResolver.GetConnectionsAsync(cancellationToken);
        var managedByConfiguration = connectionResolver.IsConfigurationManaged;
        var effectiveEnabled = managedByConfiguration ? connectionResolver.IsEnabled : settings.Enabled;
        var effectiveConnectionId = managedByConfiguration ? null : settings.ConnectionId;
        var selectedConnectionIsUsable = effectiveConnectionId is { } selectedId
            && await connectionResolver.ResolveAsync(selectedId, cancellationToken) is not null;
        var status = !available ? "unavailable" : !effectiveEnabled ? "disabled"
            : managedByConfiguration ? "ready" : selectedConnectionIsUsable ? "ready" : "invalid";
        return Ok(new OpenAiSettingsResponse(effectiveEnabled, effectiveConnectionId, connections, available, status,
            managedByConfiguration, connectionResolver.HasConfiguredApiKey));
    }

    /// <summary>Stores whether the fallback is enabled and which connection it uses.</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] OpenAiSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdministrator()) return Forbid();
        var connectionResolver = services.GetRequiredService<IOpenAiConnectionResolver>();
        if (connectionResolver.IsConfigurationManaged)
        {
            if (request.Enabled != connectionResolver.IsEnabled)
                return BadRequest("The enabled setting is controlled by standard configuration.");
            if (request.ConnectionId is not null)
                return BadRequest("The connection selection is controlled by standard configuration.");
            return await GetSettings(cancellationToken);
        }
        if (request.Enabled)
        {
            if (request.ConnectionId is not { } connectionId)
                return BadRequest("Select an active OpenAI connection before enabling the fallback.");
            if (await connectionResolver
                    .ResolveAsync(connectionId, cancellationToken) is null)
                return BadRequest("The selected connection must be active, use the OpenAI provider, and target api.openai.com directly.");
        }
        services.GetRequiredService<OpenAiProvenanceSettingsStore>()
            .Save(new OpenAiProvenanceSettings(request.Enabled, request.ConnectionId));
        return await GetSettings(cancellationToken);
    }

    /// <summary>Tests the selected connection against the actual provenance endpoint using a bundled sample image.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] OpenAiTestRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdministrator()) return Forbid();
        var resolver = services.GetRequiredService<IOpenAiConnectionResolver>();
        if (resolver.IsConfigurationManaged && request.ConnectionId is not null)
            return BadRequest("The credential source is controlled by standard configuration.");
        if (request.ConnectionId is null && !resolver.IsConfigurationManaged)
            return BadRequest("Select an OpenAI connection to test.");
        if (request.ConnectionId == Guid.Empty) return BadRequest("Select an OpenAI connection to test.");
        var bytes = ReadTestImage();
        var result = await services.GetRequiredService<OpenAiWatermarkVerifier>()
            .VerifyConnectionAsync(request.ConnectionId, () => new MemoryStream(bytes, writable: false), "image/png", cancellationToken);
        return Ok(new OpenAiTestResponse(result.Status.ToString().ToLowerInvariant(), GetTestMessage(result.Status)));
    }

    private bool IsAdministrator()
    {
        var security = services.GetService<IBackOfficeSecurityAccessor>();
        var user = security?.BackOfficeSecurity?.CurrentUser;
        return user is not null && (user.IsAdmin() || user.IsSuper());
    }

    private bool IsAvailable() => services.GetService<IOpenAiConnectionResolver>()?.IsAvailable is true;

    private static string GetTestMessage(TheBuilder.AIImageDisclosure.Watermarks.ImageWatermarkStatus status) => status switch
    {
        TheBuilder.AIImageDisclosure.Watermarks.ImageWatermarkStatus.Detected => "The provenance endpoint detected a SynthID watermark in the sample.",
        TheBuilder.AIImageDisclosure.Watermarks.ImageWatermarkStatus.NotDetected => "The endpoint is reachable; no SynthID watermark was detected in the sample.",
        TheBuilder.AIImageDisclosure.Watermarks.ImageWatermarkStatus.Unsupported => "The sample image is not supported.",
        _ => "The provenance endpoint could not complete the check. Verify endpoint access and try again.",
    };

    internal static byte[] ReadTestImage()
    {
        var assembly = typeof(OpenAiProvenanceController).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("Assets.openai-provenance-test.png", StringComparison.Ordinal));
        using var resource = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The bundled provenance test image is missing.");
        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);
        return buffer.ToArray();
    }
}

/// <summary>Sanitized OpenAI fallback settings view.</summary>
public sealed record OpenAiSettingsResponse(bool Enabled, Guid? ConnectionId, IReadOnlyList<OpenAiConnectionOption> Connections, bool Available, string Status, bool ManagedByConfiguration, bool ApiKeyConfigured);

/// <summary>OpenAI fallback settings write model.</summary>
public sealed record OpenAiSettingsRequest(bool Enabled, Guid? ConnectionId);

/// <summary>Connection selection for the endpoint check.</summary>
public sealed record OpenAiTestRequest(Guid? ConnectionId);

/// <summary>Sanitized provenance test response.</summary>
public sealed record OpenAiTestResponse(string Status, string Message);
