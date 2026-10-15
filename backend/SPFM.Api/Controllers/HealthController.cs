using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SPFM.Api.Controllers;

/// <summary>
/// Liveness probe. Explicitly anonymous — every other endpoint is protected by the
/// fallback authorization policy, so opting out has to be deliberate and visible.
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        time = DateTimeOffset.UtcNow,
    });
}
