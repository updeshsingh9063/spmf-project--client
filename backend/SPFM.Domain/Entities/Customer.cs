using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

/// <summary>
/// A Shield customer.
///
/// A first-class table from day one because QuickBooks Online models a project as a
/// sub-customer of a customer (requirements s19). Holding the customer as free text on
/// the project would force a painful migration in Phase 2.
/// </summary>
public class Customer : Entity
{
    public required string Name { get; set; }

    /// <summary>QuickBooks Online customer id. Always null in the Alpha; filled by Phase 2.</summary>
    public string? QboCustomerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
