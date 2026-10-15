using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

/// <summary>
/// An SPFM project: the unit of work the whole application revolves around.
/// </summary>
public class Project : Entity
{
    /// <summary>
    /// Human-facing project number, unique across Shield.
    ///
    /// OPEN QUESTION: format, and whether SPFM assigns it or it arrives from
    /// QuickBooks (requirements s7).
    /// </summary>
    public required string Number { get; set; }

    public required string Name { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string SiteAddress { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Quote;

    // ---- SPFM scalar inputs -------------------------------------------------
    // PLACEHOLDER FIELDS pending the client's SPFM field list (requirements s12).
    // Typed columns are used for values we expect to query or aggregate;
    // ExtraInputs carries anything the confirmed field list adds, so the schema can
    // absorb the real list without a rewrite. Frequently-queried extras get promoted
    // to typed columns by migration once known.

    public decimal Variations { get; set; }
    public decimal LabourHours { get; set; }
    public decimal LabourRate { get; set; }
    public decimal SubcontractorCost { get; set; }
    public decimal OtherDirectCost { get; set; }
    public decimal OverheadPercent { get; set; }

    /// <summary>Additional SPFM inputs not yet promoted to typed columns. Stored as jsonb.</summary>
    public string? ExtraInputsJson { get; set; }

    /// <summary>
    /// Fingerprint of the current inputs, recomputed on every save.
    ///
    /// Denormalised deliberately: comparing it to the latest run's fingerprint tells
    /// the project list whether each row's figures are current, using one column
    /// instead of loading every line item per row to recompute the hash.
    /// </summary>
    public string? InputsFingerprint { get; set; }

    public ICollection<ProjectLine> Lines { get; set; } = new List<ProjectLine>();
    public ICollection<CalculationRun> CalculationRuns { get; set; } = new List<CalculationRun>();
    public ICollection<ChangeSet> ChangeSets { get; set; } = new List<ChangeSet>();

    // ---- QuickBooks (Phase 2) ----------------------------------------------
    public string? QboProjectId { get; set; }
    public DateTimeOffset? QboLastSyncedAt { get; set; }

    // ---- Audit --------------------------------------------------------------
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optimistic concurrency token (PostgreSQL xmin). Two users editing the same
    /// project must collide visibly rather than silently overwrite one another
    /// (requirements s29).
    /// </summary>
    public uint Version { get; set; }
}
