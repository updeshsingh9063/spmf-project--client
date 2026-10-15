using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).HasMaxLength(256).IsRequired();
        b.Property(c => c.QboCustomerId).HasMaxLength(64);

        // Customers are searched by name from the project form.
        b.HasIndex(c => c.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Phase 2 looks customers up by their QuickBooks id during import.
        b.HasIndex(c => c.QboCustomerId).HasFilter("qbo_customer_id IS NOT NULL");
    }
}
