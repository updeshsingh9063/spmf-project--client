using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SPFM.Application.Abstractions;

namespace SPFM.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[RequiredScope("access_as_user")]
public class UsersController(
    ICurrentUserAccessor currentUser,
    IUserDirectory users) : ControllerBase
{
    /// <summary>
    /// The signed-in user. Called once after sign-in, which is also when the user row
    /// is created on first use.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var identity = currentUser.Identity;
        if (identity is null) return Unauthorized();

        var user = await users.GetOrCreateAsync(identity, ct);

        // Name and email only. The client's original endpoint returned the entire
        // claim set, which exposes tenant internals to the browser for no benefit
        // (requirements s22).
        return Ok(new
        {
            id = user.Id,
            name = user.DisplayName,
            email = user.Email,
        });
    }
}
