namespace SPFM.Domain.Calculations;

/// <summary>A single product/door line as the calculator sees it.</summary>
public sealed record SpfmLine
{
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
    public required decimal UnitPrice { get; init; }
}

/// <summary>
/// Everything the SPFM calculation needs, and nothing else.
///
/// Immutable by design: the calculator is a pure function, and inputs that cannot
/// change underneath it are what make that guarantee real.
///
/// PLACEHOLDER FIELDS pending the client's SPFM field list (requirements s12).
/// </summary>
public sealed record SpfmInputs
{
    public required IReadOnlyList<SpfmLine> Lines { get; init; }
    public decimal Variations { get; init; }
    public decimal LabourHours { get; init; }
    public decimal LabourRate { get; init; }
    public decimal SubcontractorCost { get; init; }
    public decimal OtherDirectCost { get; init; }
    public decimal OverheadPercent { get; init; }
}
