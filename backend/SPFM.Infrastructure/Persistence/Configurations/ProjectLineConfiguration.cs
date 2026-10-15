using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class ProjectLineConfiguration : IEntityTypeConfiguration<ProjectLine>
{
    public void Configure(EntityTypeBuilder<ProjectLine> b)
    {
        b.HasKey(l => l.Id);
        b.Property(l => l.Description).HasMaxLength(512).IsRequired();

        // numeric, never floating point (requirements s29).
        b.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        b.Property(l => l.UnitCost).HasColumnType("numeric(18,4)");
        b.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");

        b.Property(l => l.QboItemId).HasMaxLength(64);

        // Lines are always fetched for one project, in display order.
        b.HasIndex(l => new { l.ProjectId, l.Ordinal });
    }
}
