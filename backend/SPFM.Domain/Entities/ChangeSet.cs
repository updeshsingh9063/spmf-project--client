using SPFM.Domain.Common;

namespace SPFM.Domain.Entities;

public enum ChangeAction
{
    Created = 0,
    Updated = 1,
    Calculated = 2,
}

/// <summary>
/// One user action against a project, with the individual field changes it produced.
///
/// Grouping changes into a set (rather than one row per field) lets the UI show
/// "Updesh changed 3 fields at 14:32" and expand it, instead of three disconnected
/// entries. Requirements s17.
/// </summary>
public class ChangeSet : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ChangeAction Action { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<FieldChange> Changes { get; set; } = new List<FieldChange>();
}

/// <summary>A single field's before/after within a <see cref="ChangeSet"/>.</summary>
public class FieldChange : Entity
{
    public Guid ChangeSetId { get; set; }
    public ChangeSet? ChangeSet { get; set; }

    /// <summary>Readable label, e.g. "Labour hours" — not the property name.</summary>
    public required string FieldLabel { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
