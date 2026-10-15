using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Number).HasMaxLength(64).IsRequired();
        b.HasIndex(p => p.Number).IsUnique();

        b.Property(p => p.Name).HasMaxLength(256).IsRequired();
        b.Property(p => p.SiteAddress).HasMaxLength(512);

        // Stored as text rather than an int so the database stays readable and adding
        // a status later cannot silently renumber the existing ones.
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        // ---- Money and quantities: exact decimal (requirements s29) ----
        b.Property(p => p.Variations).HasColumnType("numeric(18,4)");
        b.Property(p => p.LabourHours).HasColumnType("numeric(18,4)");
        b.Property(p => p.LabourRate).HasColumnType("numeric(18,4)");
        b.Property(p => p.SubcontractorCost).HasColumnType("numeric(18,4)");
        b.Property(p => p.OtherDirectCost).HasColumnType("numeric(18,4)");
        b.Property(p => p.OverheadPercent).HasColumnType("numeric(9,4)");

        // Absorbs SPFM fields not yet promoted to typed columns (SYSTEM-DESIGN.md s6.2).
        b.Property(p => p.ExtraInputsJson).HasColumnType("jsonb");

        b.Property(p => p.InputsFingerprint).HasMaxLength(64);

        b.Property(p => p.QboProjectId).HasMaxLength(64);

        // ---- Optimistic concurrency ----
        // PostgreSQL's xmin system column changes on every row update, so a stale
        // write is rejected instead of silently overwriting another user's edit
        // (requirements s29). No trigger or manual bookkeeping required.
        b.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // ---- Relationships ----
        b.HasOne(p => p.Customer)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // never lose projects by deleting a customer

        b.HasMany(p => p.Lines)
            .WithOne(l => l.Project!)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(p => p.CalculationRuns)
            .WithOne(r => r.Project!)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(p => p.ChangeSets)
            .WithOne(c => c.Project!)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(p => p.CreatedBy).WithMany()
            .HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.UpdatedBy).WithMany()
            .HasForeignKey(p => p.UpdatedById).OnDelete(DeleteBehavior.Restrict);

        // ---- Indexes ----
        // Keyset pagination of the project list orders by (UpdatedAt desc, Id).
        b.HasIndex(p => new { p.UpdatedAt, p.Id });
        b.HasIndex(p => new { p.Status, p.UpdatedAt });
        b.HasIndex(p => p.CustomerId);

        // Substring search on name and number without a sequential scan.
        //
        // The number index MUST use the named overload. HasIndex(p => p.Number) alone
        // would resolve to the unique index declared above and reconfigure it, which
        // both drops the uniqueness guarantee and produces a UNIQUE GIN index --
        // invalid in PostgreSQL, so the migration fails on apply. Naming it declares
        // a second, separate index.
        b.HasIndex(p => p.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex([nameof(Project.Number)], "ix_projects_number_trgm")
            .HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
