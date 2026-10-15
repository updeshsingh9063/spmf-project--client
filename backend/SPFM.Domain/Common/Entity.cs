namespace SPFM.Domain.Common;

/// <summary>Base for persisted entities. Identity is the surrogate key.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
