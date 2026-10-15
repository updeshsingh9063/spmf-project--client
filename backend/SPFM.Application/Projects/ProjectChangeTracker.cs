using System.Globalization;
using SPFM.Domain.Entities;

namespace SPFM.Application.Projects;

/// <summary>
/// Works out what actually changed between a saved project and an incoming edit, so
/// change history records real differences rather than "the user pressed Save".
/// Requirements s17.
///
/// Labels are the words the user sees on the form, not property names: history has
/// to stay readable to Samuel, not just to a developer.
/// </summary>
public static class ProjectChangeTracker
{
    public static IReadOnlyList<FieldChange> Diff(Project before, SaveProjectRequest after, string afterCustomerName)
    {
        var changes = new List<FieldChange>();

        Compare(changes, "Project number", before.Number, after.Number ?? before.Number);
        Compare(changes, "Project name", before.Name, after.Name);
        Compare(changes, "Customer", before.Customer?.Name ?? "", afterCustomerName);
        Compare(changes, "Site address", before.SiteAddress, after.SiteAddress);
        Compare(changes, "Status", before.Status.ToString(), after.Status.ToString());

        CompareNumber(changes, "Variations", before.Variations, after.Variations);
        CompareNumber(changes, "Labour hours", before.LabourHours, after.LabourHours);
        CompareNumber(changes, "Labour rate", before.LabourRate, after.LabourRate);
        CompareNumber(changes, "Subcontractor cost", before.SubcontractorCost, after.SubcontractorCost);
        CompareNumber(changes, "Other direct cost", before.OtherDirectCost, after.OtherDirectCost);
        CompareNumber(changes, "Overhead %", before.OverheadPercent, after.OverheadPercent);

        DiffLines(changes, before.Lines.OrderBy(l => l.Ordinal).ToList(), after.Lines);

        return changes;
    }

    private static void DiffLines(List<FieldChange> changes, IReadOnlyList<ProjectLine> before, IReadOnlyList<ProjectLineDto> after)
    {
        // Lines are matched by position. Reordering therefore reads as a set of edits
        // rather than a move -- acceptable while lines have no stable client-side id,
        // and far better than reporting nothing at all.
        var shared = Math.Min(before.Count, after.Count);

        for (var i = 0; i < shared; i++)
        {
            var label = $"Line {i + 1}";
            Compare(changes, $"{label} description", before[i].Description, after[i].Description);
            CompareNumber(changes, $"{label} quantity", before[i].Quantity, after[i].Quantity);
            CompareNumber(changes, $"{label} unit cost", before[i].UnitCost, after[i].UnitCost);
            CompareNumber(changes, $"{label} unit price", before[i].UnitPrice, after[i].UnitPrice);
        }

        for (var i = shared; i < after.Count; i++)
            changes.Add(new FieldChange { FieldLabel = $"Line {i + 1}", OldValue = null, NewValue = Describe(after[i]) });

        for (var i = shared; i < before.Count; i++)
            changes.Add(new FieldChange { FieldLabel = $"Line {i + 1}", OldValue = Describe(before[i]), NewValue = null });
    }

    private static string Describe(ProjectLineDto l) =>
        $"{l.Description} x{Num(l.Quantity)} @ {Num(l.UnitPrice)}";

    private static string Describe(ProjectLine l) =>
        $"{l.Description} x{Num(l.Quantity)} @ {Num(l.UnitPrice)}";

    private static void Compare(List<FieldChange> changes, string label, string? before, string? after)
    {
        var a = (before ?? "").Trim();
        var b = (after ?? "").Trim();
        if (!string.Equals(a, b, StringComparison.Ordinal))
            changes.Add(new FieldChange { FieldLabel = label, OldValue = a, NewValue = b });
    }

    private static void CompareNumber(List<FieldChange> changes, string label, decimal before, decimal after)
    {
        // Compared by value, so re-typing 10 as 10.00 is not recorded as a change.
        if (before != after)
            changes.Add(new FieldChange { FieldLabel = label, OldValue = Num(before), NewValue = Num(after) });
    }

    private static string Num(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);
}
