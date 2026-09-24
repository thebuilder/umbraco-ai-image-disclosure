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
        var policy = services.GetRequiredService<OpenAiVerificationPolicy>();
        var effective = await policy.GetAsync(cancellationToken);
        var connections = await policy.GetConnectionsAsync(cancellationToken);
        var status = !effective.Available ? "unavailable" : !effective.Enabled ? "disabled"
            : effective.Connection is not null ? "ready" : "invalid";
        return Ok(new OpenAiSettingsResponse(effective.Enabled, effective.ConnectionId, connections, effective.Available, status,
            effective.ManagedByConfiguration, effective.ApiKeyConfigured));
    }

    /// <summary>Stores whether the fallback is enabled and which connection it uses.</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] OpenAiSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdministrator()) return Forbid();
        var policy = services.GetRequiredService<OpenAiVerificationPolicy>();
        var save = await policy.SaveAsync(request.Enabled, request.ConnectionId, cancellationToken);
        if (!save.IsValid) return BadRequest(save.Error);
        return await GetSettings(cancellationToken);
    }

    /// <summary>Tests the selected connection against the actual provenance endpoint using a bundled sample image.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] OpenAiTestRequest request, CancellationToken cancellationToken)
    {
        if (!IsAdministrator()) return Forbid();
        var policy = services.GetRequiredService<OpenAiVerificationPolicy>();
        var validation = await policy.ValidateTestAsync(request.ConnectionId, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Error);
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
