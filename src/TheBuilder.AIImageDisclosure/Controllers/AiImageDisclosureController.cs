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
        if (request.PageIndex < 0 || request.Limit is < 1 or > MediaAiRescanService.MaximumBatchSize)
            return BadRequest("PageIndex must be non-negative and Limit must be between 1 and 50.");
        return Ok(services.GetRequiredService<MediaAiRescanService>().Scan(
            request.PageIndex, request.Limit, user.Id, HttpContext.RequestAborted));
    }
}

/// <summary>Describes the page to scan.</summary>
public sealed class RescanRequest
{
    /// <summary>Gets or sets the zero-based page index.</summary>
    public int PageIndex { get; init; }
    /// <summary>Gets or sets the requested page size.</summary>
    public int Limit { get; init; } = 50;
}
