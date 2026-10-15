using SPFM.Domain.Entities;

namespace SPFM.Application.Abstractions;

/// <summary>Maps an Entra identity to the stored <see cref="User"/> row.</summary>
public interface IUserDirectory
{
    /// <summary>
    /// Returns the stored user for this identity, creating it on first sign-in and
    /// refreshing name/email if they have changed in Entra.
    /// </summary>
    Task<User> GetOrCreateAsync(CurrentUserIdentity identity, CancellationToken ct = default);
}
