using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SPFM.Domain.Common;

namespace SPFM.Domain.Calculations;

/// <summary>
/// ============================= PLACEHOLDER RULES =============================
///
/// These are standard job-costing formulas used as a stand-in so the surrounding
/// system can be built and tested. They are NOT Shield's SPFM rules and are NOT
/// suitable for delivery.
///
/// Requirements s12 is explicit: "The exact formulas should come from the client's
/// handoff document and provided examples rather than being invented during
/// development."
///
/// To replace them:
///   1. Implement the real rules here, leaving the method pure.
///   2. Bump EngineVersion.
///   3. Replace the placeholder cases in SPFM.Tests with the client's 2-3 real
///      Shield projects and their known expected results (requirements s14, s33).
///
/// =============================================================================
/// </summary>
public sealed class SpfmCalculator : ISpfmCalculator
{
    /// <summary>
    /// "placeholder-*" is deliberate: any stored result carrying this version was
    /// produced by invented formulas and must not be trusted as a Shield figure.
    /// </summary>
    public string EngineVersion => "placeholder-1.0.0";

    public SpfmResults Calculate(SpfmInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        decimal lineRevenue = 0m;
        decimal lineCost = 0m;
        foreach (var line in inputs.Lines)
        {
            lineRevenue += Rounding.Intermediate(line.Quantity * line.UnitPrice);
            lineCost += Rounding.Intermediate(line.Quantity * line.UnitCost);
        }

        var revenue = lineRevenue + inputs.Variations;
        var labourCost = Rounding.Intermediate(inputs.LabourHours * inputs.LabourRate);
        var directCost = lineCost + labourCost + inputs.SubcontractorCost + inputs.OtherDirectCost;

        var grossProfit = revenue - directCost;
        var overhead = Rounding.Intermediate(directCost * (inputs.OverheadPercent / 100m));
        var netProfit = grossProfit - overhead;

        return new SpfmResults
        {
            LineRevenue = Rounding.Currency(lineRevenue),
            Variations = Rounding.Currency(inputs.Variations),
            Revenue = Rounding.Currency(revenue),

            LineCost = Rounding.Currency(lineCost),
            LabourCost = Rounding.Currency(labourCost),
            SubcontractorCost = Rounding.Currency(inputs.SubcontractorCost),
            OtherDirectCost = Rounding.Currency(inputs.OtherDirectCost),
            DirectCost = Rounding.Currency(directCost),

            GrossProfit = Rounding.Currency(grossProfit),
            GrossMarginPercent = Rounding.PercentOf(grossProfit, revenue),
            Overhead = Rounding.Currency(overhead),
            NetProfit = Rounding.Currency(netProfit),
            NetMarginPercent = Rounding.PercentOf(netProfit, revenue),
            MarkupPercent = Rounding.PercentOf(grossProfit, directCost),
        };
    }

    /// <summary>
    /// SHA-256 over a culture-invariant rendering of the inputs. Used to detect that
    /// stored results no longer match the project's current inputs.
    /// </summary>
    public string Fingerprint(SpfmInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var sb = new StringBuilder();
        foreach (var line in inputs.Lines)
        {
            sb.Append(line.Description.Trim()).Append('\u001f')
              .Append(Inv(line.Quantity)).Append('\u001f')
              .Append(Inv(line.UnitCost)).Append('\u001f')
              .Append(Inv(line.UnitPrice)).Append('\u001e');
        }
        sb.Append(Inv(inputs.Variations)).Append('\u001f')
          .Append(Inv(inputs.LabourHours)).Append('\u001f')
          .Append(Inv(inputs.LabourRate)).Append('\u001f')
          .Append(Inv(inputs.SubcontractorCost)).Append('\u001f')
          .Append(Inv(inputs.OtherDirectCost)).Append('\u001f')
          .Append(Inv(inputs.OverheadPercent));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Normalises scale so 10 and 10.00 fingerprint identically — otherwise a
    /// cosmetic re-entry of the same number would mark results stale.
    /// </summary>
    private static string Inv(decimal value) =>
        Rounding.Intermediate(value).ToString("0.####", CultureInfo.InvariantCulture);
}
