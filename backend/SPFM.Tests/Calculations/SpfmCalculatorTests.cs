using Shouldly;
using SPFM.Domain.Calculations;

namespace SPFM.Tests.Calculations;

/// <summary>
/// Tests for the calculation engine.
///
/// IMPORTANT: while SpfmCalculator holds placeholder formulas, the expected values
/// below are derived from those placeholders. They prove the engine is deterministic,
/// rounds consistently and handles edge cases — they do NOT prove the figures are
/// correct for Shield. Only the client's real projects can do that
/// (requirements s14, s33), and those cases live in RealProjectTests.
/// </summary>
public class SpfmCalculatorTests
{
    private readonly SpfmCalculator _calc = new();

    private static SpfmInputs Inputs(params SpfmLine[] lines) => new()
    {
        Lines = lines,
        Variations = 0m,
        LabourHours = 0m,
        LabourRate = 0m,
        SubcontractorCost = 0m,
        OtherDirectCost = 0m,
        OverheadPercent = 0m,
    };

    private static SpfmLine Line(decimal qty, decimal cost, decimal price) =>
        new() { Description = "Steel security door", Quantity = qty, UnitCost = cost, UnitPrice = price };

    [Fact]
    public void Revenue_and_cost_sum_across_lines()
    {
        var r = _calc.Calculate(Inputs(Line(4, 820m, 1350m), Line(2, 2100m, 3200m)));

        r.LineRevenue.ShouldBe(11800m);   // 4*1350 + 2*3200
        r.LineCost.ShouldBe(7480m);       // 4*820  + 2*2100
        r.GrossProfit.ShouldBe(4320m);
    }

    [Fact]
    public void Variations_are_revenue_not_cost()
    {
        var baseline = Inputs(Line(1, 100m, 200m));
        var withVariation = baseline with { Variations = 50m };

        var r = _calc.Calculate(withVariation);

        r.Revenue.ShouldBe(250m);
        r.DirectCost.ShouldBe(100m);
    }

    [Fact]
    public void Labour_cost_is_hours_times_rate()
    {
        var r = _calc.Calculate(Inputs(Line(1, 0m, 1000m)) with { LabourHours = 7.5m, LabourRate = 55m });
        r.LabourCost.ShouldBe(412.50m);
    }

    [Fact]
    public void Overhead_is_a_percentage_of_direct_cost()
    {
        var r = _calc.Calculate(Inputs(Line(1, 1000m, 2000m)) with { OverheadPercent = 12m });

        r.Overhead.ShouldBe(120m);
        r.NetProfit.ShouldBe(880m);
    }

    [Fact]
    public void A_job_can_lose_money_after_overhead()
    {
        var r = _calc.Calculate(Inputs(Line(1, 950m, 1000m)) with { OverheadPercent = 20m });

        r.GrossProfit.ShouldBe(50m);
        r.Overhead.ShouldBe(190m);
        r.NetProfit.ShouldBe(-140m);      // negative must survive, not clamp to zero
        r.NetMarginPercent.ShouldBe(-14m);
    }

    [Fact]
    public void Margins_are_null_when_there_is_no_revenue()
    {
        // A project with no revenue has no margin. That is not the same as 0%.
        var r = _calc.Calculate(Inputs(Line(1, 500m, 0m)));

        r.Revenue.ShouldBe(0m);
        r.GrossMarginPercent.ShouldBeNull();
        r.NetMarginPercent.ShouldBeNull();
    }

    [Fact]
    public void Markup_is_null_when_there_is_no_direct_cost()
    {
        var r = _calc.Calculate(Inputs(Line(1, 0m, 500m)));
        r.MarkupPercent.ShouldBeNull();
    }

    [Fact]
    public void Money_arithmetic_is_exact_not_floating_point()
    {
        // 0.1 + 0.2 must be 0.3. With double it is 0.30000000000000004.
        // Requirements s29: money is never a floating-point type.
        var r = _calc.Calculate(Inputs(Line(1, 0m, 0.1m), Line(1, 0m, 0.2m)));
        r.Revenue.ShouldBe(0.3m);
    }

    [Fact]
    public void Currency_results_are_rounded_to_two_places()
    {
        var r = _calc.Calculate(Inputs(Line(3, 0m, 10.005m)));
        r.Revenue.ShouldBe(30.02m);       // 30.015 rounds away from zero
    }

    [Fact]
    public void Calculation_is_deterministic()
    {
        var inputs = Inputs(Line(3, 199.99m, 349.95m)) with { LabourHours = 11.25m, LabourRate = 58.5m, OverheadPercent = 13.5m };

        _calc.Calculate(inputs).ShouldBe(_calc.Calculate(inputs));
    }

    [Fact]
    public void Empty_project_produces_zeroes_rather_than_throwing()
    {
        var r = _calc.Calculate(Inputs());

        r.Revenue.ShouldBe(0m);
        r.DirectCost.ShouldBe(0m);
        r.NetProfit.ShouldBe(0m);
    }
}
