using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;

namespace SPFM.Application.Projects;

/// <summary>
/// Maps a stored project to the inputs the calculation engine accepts.
///
/// Lives in the application layer so neither the API nor the engine has to know how
/// a project is persisted, and the engine keeps taking a plain immutable record
/// rather than an EF entity.
/// </summary>
public static class ProjectInputsMapper
{
    public static SpfmInputs ToInputs(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new SpfmInputs
        {
            Lines = [.. project.Lines.OrderBy(l => l.Ordinal).Select(l => new SpfmLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                UnitPrice = l.UnitPrice,
            })],
            Variations = project.Variations,
            LabourHours = project.LabourHours,
            LabourRate = project.LabourRate,
            SubcontractorCost = project.SubcontractorCost,
            OtherDirectCost = project.OtherDirectCost,
            OverheadPercent = project.OverheadPercent,
        };
    }

    public static SpfmInputs ToInputs(SaveProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SpfmInputs
        {
            Lines = [.. request.Lines.Select(l => new SpfmLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                UnitPrice = l.UnitPrice,
            })],
            Variations = request.Variations,
            LabourHours = request.LabourHours,
            LabourRate = request.LabourRate,
            SubcontractorCost = request.SubcontractorCost,
            OtherDirectCost = request.OtherDirectCost,
            OverheadPercent = request.OverheadPercent,
        };
    }
}
