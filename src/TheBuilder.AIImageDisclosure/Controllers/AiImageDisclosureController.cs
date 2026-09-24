using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TheBuilder.AIImageDisclosure.Media;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace TheBuilder.AIImageDisclosure.Controllers;

/// <summary>Provides the administrator-only bounded media rescan endpoint.</summary>
[ApiController]
[Route("umbraco/backoffice/api/ai-image-disclosure")]
public sealed class AiImageDisclosureController(IServiceProvider services) : UmbracoAuthorizedController
{
    /// <summary>Scans one bounded page of Image media.</summary>
    [HttpPost("rescan")]
    public IActionResult Rescan([FromBody] RescanRequest request)
    {
        var security = services.GetRequiredService<IBackOfficeSecurityAccessor>();
        var user = security.BackOfficeSecurity?.CurrentUser;
        if (user is null || (!user.IsAdmin() && !user.IsSuper()))
            return Forbid();
        if (request.Cursor is { } cursor && (cursor.LastId < 0 || cursor.MaximumId < cursor.LastId))
            return BadRequest("The scan cursor is invalid.");
        if (request.Limit is < 1 or > MediaAiRescanService.MaximumBatchSize)
            return BadRequest("Limit must be between 1 and 50.");
        return Ok(services.GetRequiredService<MediaAiRescanService>().Scan(
            request.Cursor, request.Limit, user.Id, HttpContext.RequestAborted));
    }
}

/// <summary>Describes the next batch to scan.</summary>
public sealed class RescanRequest
{
    /// <summary>Gets or sets the cursor returned by the previous batch, or null to start a scan.</summary>
    public RescanCursor? Cursor { get; init; }
    /// <summary>Gets or sets the requested page size.</summary>
    public int Limit { get; init; } = 50;
}
