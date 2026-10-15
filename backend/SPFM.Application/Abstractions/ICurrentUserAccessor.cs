namespace SPFM.Application.Abstractions;

/// <summary>The Entra identity behind the current request, as read from the token.</summary>
public sealed record CurrentUserIdentity(
    string EntraObjectId,
    string TenantId,
    string Email,
    string DisplayName);

/// <summary>
/// Supplies the signed-in identity without the application layer knowing anything
/// about HTTP or claims.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Null when the request is unauthenticated.</summary>
    CurrentUserIdentity? Identity { get; }
}
