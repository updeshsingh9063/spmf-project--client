using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class ChangeSetConfiguration : IEntityTypeConfiguration<ChangeSet>
{
    public void Configure(EntityTypeBuilder<ChangeSet> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Action).HasConversion<string>().HasMaxLength(32).IsRequired();

        b.HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(c => c.Changes)
            .WithOne(f => f.ChangeSet!)
            .HasForeignKey(f => f.ChangeSetId)
            .OnDelete(DeleteBehavior.Cascade);

        // History is read newest-first and paged, so it stays fast as it accumulates.
        b.HasIndex(c => new { c.ProjectId, c.At });
    }
}

public class FieldChangeConfiguration : IEntityTypeConfiguration<FieldChange>
{
    public void Configure(EntityTypeBuilder<FieldChange> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.FieldLabel).HasMaxLength(128).IsRequired();

        // Values are stored as rendered text: history must still read correctly after
        // a field's type or formatting changes.
        b.Property(f => f.OldValue).HasMaxLength(1024);
        b.Property(f => f.NewValue).HasMaxLength(1024);

        b.HasIndex(f => f.ChangeSetId);
    }
}
