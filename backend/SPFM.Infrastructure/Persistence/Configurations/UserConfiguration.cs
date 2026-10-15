using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);

        // The Entra object id is how a signed-in caller is matched to a stored user
        // on every request, so it must be unique and indexed.
        b.Property(u => u.EntraObjectId).HasMaxLength(64).IsRequired();
        b.HasIndex(u => u.EntraObjectId).IsUnique();

        b.Property(u => u.TenantId).HasMaxLength(64).IsRequired();
        b.Property(u => u.Email).HasMaxLength(256).IsRequired();
        b.Property(u => u.DisplayName).HasMaxLength(256).IsRequired();
        b.Property(u => u.Role).HasMaxLength(32);
    }
}
