using Shouldly;
using SPFM.Domain.Calculations;

namespace SPFM.Tests.Calculations;

public class SpfmValidatorTests
{
    private static SpfmInputs Valid() => new()
    {
        Lines = [new SpfmLine { Description = "Door", Quantity = 1m, UnitCost = 100m, UnitPrice = 200m }],
        Variations = 0m,
        LabourHours = 8m,
        LabourRate = 55m,
        SubcontractorCost = 0m,
        OtherDirectCost = 0m,
        OverheadPercent = 12m,
    };

    [Fact]
    public void A_complete_project_passes()
        => SpfmValidator.Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void A_project_with_no_lines_is_rejected()
    {
        var r = SpfmValidator.Validate(Valid() with { Lines = [] });

        r.IsValid.ShouldBeFalse();
        r.Errors["lines"].ShouldBe("Add at least one door or product line.");
    }

    [Fact]
    public void Line_errors_are_keyed_by_index_so_the_UI_can_place_them()
    {
        var inputs = Valid() with
        {
            Lines =
            [
                new SpfmLine { Description = "Door", Quantity = 1m, UnitCost = 1m, UnitPrice = 1m },
                new SpfmLine { Description = "   ", Quantity = 0m, UnitCost = -1m, UnitPrice = 1m },
            ],
        };

        var r = SpfmValidator.Validate(inputs);

        r.Errors.Keys.ShouldContain("lines.1.description");
        r.Errors.Keys.ShouldContain("lines.1.quantity");
        r.Errors.Keys.ShouldContain("lines.1.unitCost");
        r.Errors.Keys.ShouldNotContain("lines.0.description");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Overhead_outside_zero_to_one_hundred_is_rejected(decimal percent)
    {
        var r = SpfmValidator.Validate(Valid() with { OverheadPercent = percent });
        r.Errors.Keys.ShouldContain("overheadPercent");
    }

    [Fact]
    public void Negative_money_is_rejected_where_it_makes_no_sense()
    {
        var r = SpfmValidator.Validate(Valid() with { SubcontractorCost = -500m });
        r.Errors["subcontractorCost"].ShouldBe("Enter 0 or more.");
    }

    [Fact]
    public void Messages_are_written_for_a_person_not_a_developer()
    {
        var r = SpfmValidator.Validate(Valid() with { Lines = [] });

        foreach (var message in r.Errors.Values)
        {
            message.ShouldNotContain("Exception");
            message.ShouldNotContain("null");
            message.ShouldEndWith(".");
        }
    }
}
