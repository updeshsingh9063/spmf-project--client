using Shouldly;
using SPFM.Application.Projects;
using SPFM.Domain.Entities;

namespace SPFM.Tests.Projects;

/// <summary>
/// Change history is a delivered feature (requirements s17) and the only record of
/// who altered a financial figure. It has to record real changes and stay quiet
/// about non-changes, or it becomes noise nobody reads.
/// </summary>
public class ProjectChangeTrackerTests
{
    private static Project Existing() => new()
    {
        Number = "SSD-2026-014",
        Name = "Warehouse fit-out",
        Customer = new Customer { Name = "Bayview Logistics" },
        SiteAddress = "12 Dock Road",
        Status = ProjectStatus.Quote,
        LabourHours = 32m,
        LabourRate = 55m,
        OverheadPercent = 12m,
        Lines =
        [
            new ProjectLine { Ordinal = 0, Description = "Steel door", Quantity = 4m, UnitCost = 820m, UnitPrice = 1350m },
        ],
    };

    private static SaveProjectRequest Unchanged() => new()
    {
        Number = "SSD-2026-014",
        Name = "Warehouse fit-out",
        CustomerName = "Bayview Logistics",
        SiteAddress = "12 Dock Road",
        Status = ProjectStatus.Quote,
        LabourHours = 32m,
        LabourRate = 55m,
        OverheadPercent = 12m,
        Lines = [new ProjectLineDto("Steel door", 4m, 820m, 1350m)],
    };

    [Fact]
    public void Saving_without_editing_records_nothing()
    {
        // Otherwise every Save manufactures a history entry and the audit trail
        // becomes unreadable.
        ProjectChangeTracker.Diff(Existing(), Unchanged(), "Bayview Logistics").ShouldBeEmpty();
    }

    [Fact]
    public void Records_the_old_and_new_value_with_a_readable_label()
    {
        var after = Unchanged() with { LabourHours = 40m };

        var change = ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldHaveSingleItem();

        change.FieldLabel.ShouldBe("Labour hours");
        change.OldValue.ShouldBe("32");
        change.NewValue.ShouldBe("40");
    }

    [Fact]
    public void Labels_are_the_words_on_the_form_not_property_names()
    {
        var after = Unchanged() with { OverheadPercent = 15m, SiteAddress = "9 Quay Street" };

        var labels = ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics")
            .Select(c => c.FieldLabel).ToList();

        labels.ShouldContain("Overhead %");
        labels.ShouldContain("Site address");
        labels.ShouldNotContain("OverheadPercent");
    }

    [Fact]
    public void Re_typing_a_number_in_another_form_is_not_a_change()
    {
        // 32 and 32.00 are the same amount. Recording that as an edit would be noise.
        var after = Unchanged() with { LabourHours = 32.00m };

        ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldBeEmpty();
    }

    [Fact]
    public void Surrounding_whitespace_is_not_a_change()
    {
        var after = Unchanged() with { Name = "  Warehouse fit-out  " };

        ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldBeEmpty();
    }

    [Fact]
    public void An_added_line_is_recorded_as_an_addition()
    {
        var after = Unchanged() with
        {
            Lines = [new ProjectLineDto("Steel door", 4m, 820m, 1350m), new ProjectLineDto("Roller shutter", 2m, 2100m, 3200m)],
        };

        var change = ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldHaveSingleItem();

        change.FieldLabel.ShouldBe("Line 2");
        change.OldValue.ShouldBeNull();
        change.NewValue.ShouldNotBeNull();
        change.NewValue!.ShouldContain("Roller shutter");
    }

    [Fact]
    public void A_removed_line_is_recorded_as_a_removal()
    {
        var after = Unchanged() with { Lines = [] };

        var change = ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldHaveSingleItem();

        change.FieldLabel.ShouldBe("Line 1");
        change.OldValue.ShouldNotBeNull();
        change.OldValue!.ShouldContain("Steel door");
        change.NewValue.ShouldBeNull();
    }

    [Fact]
    public void An_edited_line_records_only_the_field_that_changed()
    {
        var after = Unchanged() with { Lines = [new ProjectLineDto("Steel door", 4m, 820m, 1500m)] };

        var change = ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldHaveSingleItem();

        change.FieldLabel.ShouldBe("Line 1 unit price");
        change.OldValue.ShouldBe("1350");
        change.NewValue.ShouldBe("1500");
    }

    [Fact]
    public void A_customer_change_is_recorded()
    {
        var after = Unchanged() with { CustomerName = "Harbour Freight" };

        var change = ProjectChangeTracker.Diff(Existing(), after, "Harbour Freight").ShouldHaveSingleItem();

        change.FieldLabel.ShouldBe("Customer");
        change.OldValue.ShouldBe("Bayview Logistics");
        change.NewValue.ShouldBe("Harbour Freight");
    }

    [Fact]
    public void A_status_change_is_recorded()
    {
        var after = Unchanged() with { Status = ProjectStatus.Active };

        ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics")
            .ShouldHaveSingleItem().FieldLabel.ShouldBe("Status");
    }

    [Fact]
    public void Several_edits_in_one_save_are_all_recorded()
    {
        var after = Unchanged() with
        {
            Name = "Warehouse fit-out stage 2",
            LabourHours = 48m,
            Status = ProjectStatus.Active,
        };

        ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").Count.ShouldBe(3);
    }

    [Fact]
    public void A_number_omitted_from_the_request_is_not_treated_as_a_deletion()
    {
        // Number is optional on the request; absent means "keep what is there".
        var after = Unchanged() with { Number = null };

        ProjectChangeTracker.Diff(Existing(), after, "Bayview Logistics").ShouldBeEmpty();
    }
}
