namespace SPFM.Application.Common;

/// <summary>
/// A page of results with an opaque cursor for the next page.
///
/// Keyset pagination, not OFFSET: OFFSET makes the database walk and discard every
/// skipped row, so later pages get steadily slower as data grows
/// (SYSTEM-DESIGN.md s11.1). The cursor is opaque so the paging key can change
/// without breaking clients.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public bool HasMore => NextCursor is not null;

    public static PagedResult<T> Empty() => new([], null);
}
