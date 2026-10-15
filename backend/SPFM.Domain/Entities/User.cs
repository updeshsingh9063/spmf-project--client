using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

/// <summary>
/// A Shield user, mirrored from their Microsoft Entra identity on first sign-in.
///
/// The Alpha has a single user (requirements s4), but every write is attributed so
/// change history is meaningful and additional users need no schema change.
/// </summary>
public class User : Entity
{
    /// <summary>The Entra object id ("oid" claim). Stable for the life of the account.</summary>
    public required string EntraObjectId { get; set; }

    public required string TenantId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }

    /// <summary>
    /// Reserved. Roles are explicitly out of scope (requirements s42); this column
    /// exists so they can be introduced later without a disruptive migration.
    /// </summary>
    public string? Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
