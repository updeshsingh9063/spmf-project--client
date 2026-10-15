using Shouldly;
using SPFM.Domain.Calculations;

namespace SPFM.Tests.Calculations;

/// <summary>
/// The fingerprint decides whether saved results are shown as current or as
/// "needs recalculating" (requirements s16). Getting it wrong either hides stale
/// figures or nags about changes that never happened.
/// </summary>
public class FingerprintTests
{
    private readonly SpfmCalculator _calc = new();

    private static SpfmInputs Inputs(decimal hours = 10m) => new()
    {
        Lines = [new SpfmLine { Description = "Door", Quantity = 1m, UnitCost = 100m, UnitPrice = 200m }],
        Variations = 0m,
        LabourHours = hours,
        LabourRate = 55m,
        SubcontractorCost = 0m,
        OtherDirectCost = 0m,
        OverheadPercent = 10m,
    };

    [Fact]
    public void Same_inputs_fingerprint_the_same()
        => _calc.Fingerprint(Inputs()).ShouldBe(_calc.Fingerprint(Inputs()));

    [Fact]
    public void Changed_inputs_fingerprint_differently()
        => _calc.Fingerprint(Inputs(10m)).ShouldNotBe(_calc.Fingerprint(Inputs(16m)));

    [Fact]
    public void Trailing_zeroes_do_not_count_as_a_change()
    {
        // Re-typing "10" as "10.00" must not mark saved results stale.
        _calc.Fingerprint(Inputs(10m)).ShouldBe(_calc.Fingerprint(Inputs(10.00m)));
    }

    [Fact]
    public void Surrounding_whitespace_in_a_description_is_not_a_change()
    {
        var a = Inputs();
        var b = a with { Lines = [a.Lines[0] with { Description = "  Door  " }] };

        _calc.Fingerprint(a).ShouldBe(_calc.Fingerprint(b));
    }

    [Fact]
    public void Reordering_lines_is_a_change()
    {
        var a = Inputs();
        var second = new SpfmLine { Description = "Shutter", Quantity = 2m, UnitCost = 10m, UnitPrice = 20m };

        var ab = a with { Lines = [a.Lines[0], second] };
        var ba = a with { Lines = [second, a.Lines[0]] };

        _calc.Fingerprint(ab).ShouldNotBe(_calc.Fingerprint(ba));
    }
}
