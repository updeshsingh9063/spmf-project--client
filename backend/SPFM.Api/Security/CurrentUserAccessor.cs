using System.Security.Claims;
using SPFM.Application.Abstractions;

namespace SPFM.Api.Security;

/// <summary>
/// Reads the signed-in identity from the validated Entra token.
///
/// Claim names differ between token versions, so each value is resolved from the
/// alternatives Entra actually issues rather than assuming one shape.
/// </summary>
public sealed class CurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public CurrentUserIdentity? Identity
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;

            // "oid" is the stable per-user object id. Without it we cannot safely
            // attribute a change to a person, so treat its absence as unauthenticated.
            var objectId = First(principal, "oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (string.IsNullOrWhiteSpace(objectId)) return null;

            var tenantId = First(principal, "tid", "http://schemas.microsoft.com/identity/claims/tenantid") ?? "";
            var email = First(principal, "preferred_username", ClaimTypes.Upn, ClaimTypes.Email, "email") ?? "";
            var name = First(principal, "name", ClaimTypes.Name) ?? email;

            return new CurrentUserIdentity(objectId, tenantId, email, name);
        }
    }

    private static string? First(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}
