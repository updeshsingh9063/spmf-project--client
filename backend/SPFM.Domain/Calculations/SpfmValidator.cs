namespace SPFM.Domain.Calculations;

/// <summary>
/// Field-keyed validation messages. Keys match the form field names so the UI can
/// attach each message to its input directly (requirements s10).
/// </summary>
public sealed class ValidationResult
{
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void Add(string field, string message) => _errors.TryAdd(field, message);
}

/// <summary>
/// Server-side validation of SPFM inputs. The browser validates too, for a fast
/// response, but this is the authority: nothing incorrect reaches the database
/// because a client-side check was bypassed (requirements s10).
///
/// Messages are written for a person, not a developer.
/// </summary>
public static class SpfmValidator
{
    public static ValidationResult Validate(SpfmInputs inputs)
    {
        var result = new ValidationResult();
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Lines.Count == 0)
            result.Add("lines", "Add at least one door or product line.");

        for (var i = 0; i < inputs.Lines.Count; i++)
        {
            var line = inputs.Lines[i];
            if (string.IsNullOrWhiteSpace(line.Description))
                result.Add($"lines.{i}.description", "Describe the item.");
            if (line.Quantity <= 0)
                result.Add($"lines.{i}.quantity", "Enter a quantity of 1 or more.");
            if (line.UnitCost < 0)
                result.Add($"lines.{i}.unitCost", "Enter 0 or more.");
            if (line.UnitPrice < 0)
                result.Add($"lines.{i}.unitPrice", "Enter 0 or more.");
        }

        if (inputs.Variations < 0) result.Add("variations", "Enter 0 or more.");
        if (inputs.LabourHours < 0) result.Add("labourHours", "Enter 0 or more.");
        if (inputs.LabourRate < 0) result.Add("labourRate", "Enter 0 or more.");
        if (inputs.SubcontractorCost < 0) result.Add("subcontractorCost", "Enter 0 or more.");
        if (inputs.OtherDirectCost < 0) result.Add("otherDirectCost", "Enter 0 or more.");
        if (inputs.OverheadPercent is < 0 or > 100)
            result.Add("overheadPercent", "Enter a percentage between 0 and 100.");

        return result;
    }
}
