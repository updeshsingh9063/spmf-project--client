namespace SPFM.Domain.Calculations;

/// <summary>
/// The computed SPFM figures. Percentages are null when the denominator is zero,
/// rather than being silently reported as 0% — a project with no revenue has no
/// margin, which is not the same as a zero margin.
/// </summary>
public sealed record SpfmResults
{
    public required decimal LineRevenue { get; init; }
    public required decimal Variations { get; init; }
    public required decimal Revenue { get; init; }

    public required decimal LineCost { get; init; }
    public required decimal LabourCost { get; init; }
    public required decimal SubcontractorCost { get; init; }
    public required decimal OtherDirectCost { get; init; }
    public required decimal DirectCost { get; init; }

    public required decimal GrossProfit { get; init; }
    public required decimal? GrossMarginPercent { get; init; }
    public required decimal Overhead { get; init; }
    public required decimal NetProfit { get; init; }
    public required decimal? NetMarginPercent { get; init; }
    public required decimal? MarkupPercent { get; init; }
}
