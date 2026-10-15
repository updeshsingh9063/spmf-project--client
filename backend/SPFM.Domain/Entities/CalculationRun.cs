using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

/// <summary>
/// One execution of the SPFM calculation engine, stored permanently.
///
/// Runs are append-only: calculating again adds a row rather than overwriting the
/// previous one. That gives a genuine audit trail for financial figures, answers
/// "what did this project say in March?", and makes it possible to spot results that
/// predate a formula correction.
/// </summary>
public class CalculationRun : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>1-based, increments per project.</summary>
    public int Version { get; set; }

    /// <summary>Snapshot of the inputs this run was computed from (jsonb).</summary>
    public required string InputsJson { get; set; }

    /// <summary>The computed results (jsonb).</summary>
    public required string ResultsJson { get; set; }

    /// <summary>
    /// Hash of the inputs. If the project's current inputs hash differently, the
    /// stored results are stale and the UI must say so rather than present them as
    /// current (requirements s16).
    /// </summary>
    public required string InputsFingerprint { get; set; }

    /// <summary>Version of the calculation engine that produced this result.</summary>
    public required string EngineVersion { get; set; }

    public Guid CalculatedById { get; set; }
    public User? CalculatedBy { get; set; }
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}
