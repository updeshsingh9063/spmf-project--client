using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class CalculationRunConfiguration : IEntityTypeConfiguration<CalculationRun>
{
    public void Configure(EntityTypeBuilder<CalculationRun> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.InputsJson).HasColumnType("jsonb").IsRequired();
        b.Property(r => r.ResultsJson).HasColumnType("jsonb").IsRequired();
        b.Property(r => r.InputsFingerprint).HasMaxLength(64).IsRequired();
        b.Property(r => r.EngineVersion).HasMaxLength(64).IsRequired();

        b.HasOne(r => r.CalculatedBy).WithMany()
            .HasForeignKey(r => r.CalculatedById).OnDelete(DeleteBehavior.Restrict);

        // Runs are append-only, so a project's version numbers must never collide.
        b.HasIndex(r => new { r.ProjectId, r.Version }).IsUnique();

        // The project screen always wants the most recent run.
        b.HasIndex(r => new { r.ProjectId, r.CalculatedAt })
            .HasDatabaseName("ix_calculation_runs_project_id_calculated_at");
    }
}
