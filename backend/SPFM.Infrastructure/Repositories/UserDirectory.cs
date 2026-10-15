using Microsoft.EntityFrameworkCore;
using SPFM.Application.Abstractions;
using SPFM.Domain.Entities;
using SPFM.Infrastructure.Persistence;

namespace SPFM.Infrastructure.Repositories;

public sealed class UserDirectory(SpfmDbContext db) : IUserDirectory
{
    public async Task<User> GetOrCreateAsync(CurrentUserIdentity identity, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == identity.EntraObjectId, ct);

        if (user is null)
        {
            user = new User
            {
                EntraObjectId = identity.EntraObjectId,
                TenantId = identity.TenantId,
                Email = identity.Email,
                DisplayName = identity.DisplayName,
            };
            db.Users.Add(user);
        }
        else
        {
            // Entra is the source of truth for name and email; a rename there should
            // be reflected here rather than leaving stale text in change history.
            user.Email = identity.Email;
            user.DisplayName = identity.DisplayName;
            user.LastSeenAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return user;
    }
}
