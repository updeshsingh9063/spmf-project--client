using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

/// <summary>
/// A single product/door line on a project.
///
/// PLACEHOLDER SHAPE: the real SPFM line fields come from the client's handoff
/// document (requirements s12). Money columns are decimal and map to numeric(18,4).
/// </summary>
public class ProjectLine : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>Display order within the project. Keeps line order stable across saves.</summary>
    public int Ordinal { get; set; }

    public required string Description { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>What Shield pays per unit.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>What the customer is charged per unit.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>QuickBooks Online item id. Always null in the Alpha.</summary>
    public string? QboItemId { get; set; }
}
