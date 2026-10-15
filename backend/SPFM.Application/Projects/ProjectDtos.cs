using System.Text.Json.Serialization;
using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;

namespace SPFM.Application.Projects;

public sealed record ProjectLineDto(
    string Description,
    decimal Quantity,
    decimal UnitCost,
    decimal UnitPrice);

/// <summary>Row shape for the project list. Deliberately small: the list screen must
/// not drag every line item and calculation run across the wire.</summary>
public sealed record ProjectSummaryDto(
    Guid Id,
    string Number,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    decimal? Revenue,
    decimal? NetProfit,
    CalculationState CalculationState,
    DateTimeOffset UpdatedAt);

/// <summary>Whether stored results still match the project's current inputs.</summary>
public enum CalculationState
{
    /// <summary>Never calculated.</summary>
    Never,
    /// <summary>Inputs changed since the last calculation — figures are stale.</summary>
    Outdated,
    /// <summary>Results match the current inputs.</summary>
    Current,
}

public sealed record ProjectDetailDto(
    Guid Id,
    string Number,
    string Name,
    Guid CustomerId,
    string CustomerName,
    string SiteAddress,
    ProjectStatus Status,
    IReadOnlyList<ProjectLineDto> Lines,
    decimal Variations,
    decimal LabourHours,
    decimal LabourRate,
    decimal SubcontractorCost,
    decimal OtherDirectCost,
    decimal OverheadPercent,
    SpfmResults? Results,
    CalculationState CalculationState,
    DateTimeOffset? CalculatedAt,
    string? EngineVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    // Without this it serializes as "eTag", which no client would guess.
    [property: JsonPropertyName("etag")] string ETag);

public sealed record SaveProjectRequest
{
    /// <summary>
    /// Optional. Generated when absent.
    /// OPEN QUESTION: the real numbering format, and whether SPFM or QuickBooks owns it.
    /// </summary>
    public string? Number { get; init; }

    public string Name { get; init; } = "";

    /// <summary>Existing customer, or null to create one from <see cref="CustomerName"/>.</summary>
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }

    public string SiteAddress { get; init; } = "";
    public ProjectStatus Status { get; init; } = ProjectStatus.Quote;

    public IReadOnlyList<ProjectLineDto> Lines { get; init; } = [];
    public decimal Variations { get; init; }
    public decimal LabourHours { get; init; }
    public decimal LabourRate { get; init; }
    public decimal SubcontractorCost { get; init; }
    public decimal OtherDirectCost { get; init; }
    public decimal OverheadPercent { get; init; }
}

public sealed record FieldChangeDto(string Field, string? From, string? To);

public sealed record ChangeSetDto(
    Guid Id,
    string By,
    ChangeAction Action,
    DateTimeOffset At,
    IReadOnlyList<FieldChangeDto> Changes);

public sealed record ProjectSearch
{
    public string? Query { get; init; }
    public ProjectStatus? Status { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 25;
}
