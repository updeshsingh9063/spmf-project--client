using Microsoft.EntityFrameworkCore;
using SPFM.Application.Common;
using SPFM.Application.Projects;

namespace SPFM.Infrastructure.Repositories;

public sealed partial class ProjectRepository
{
    public async Task<PagedResult<ProjectSummaryDto>> SearchAsync(
        ProjectSearch search, CancellationToken ct = default)
    {
        var limit = Math.Clamp(search.Limit, 1, 100);

        // AsNoTracking: this is a read. Change tracking would cost memory and CPU for
        // entities nobody is going to modify.
        var query = db.Projects.AsNoTracking().AsQueryable();

        if (search.Status is { } status)
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            var term = $"%{search.Query.Trim()}%";
            // ILIKE with a leading wildcard, served by the GIN trigram indexes on name
            // and number -- without them this is a sequential scan on every keystroke.
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, term) ||
                EF.Functions.ILike(p.Number, term) ||
                EF.Functions.ILike(p.Customer!.Name, term));
        }

        // Keyset, not OFFSET: "the page after this exact row".
        if (ProjectCursor.TryDecode(search.Cursor) is { } cursor)
        {
            query = query.Where(p =>
                p.UpdatedAt < cursor.UpdatedAt ||
                (p.UpdatedAt == cursor.UpdatedAt && p.Id.CompareTo(cursor.Id) < 0));
        }

        // Id breaks ties so the ordering is total: without it, rows sharing a timestamp
        // could repeat or vanish across page boundaries.
        query = query.OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id);

        // Fetching one row beyond the page tells us whether a next page exists without
        // a second COUNT over the whole table.
        var rows = await query
            .Take(limit + 1)
            .Select(p => new
            {
                p.Id,
                p.Number,
                p.Name,
                CustomerName = p.Customer!.Name,
                p.Status,
                p.UpdatedAt,
                p.InputsFingerprint,
                // Projected inside the same query. A per-row lookup here would be the
                // classic N+1 that makes a list screen crawl.
                LatestResultsJson = p.CalculationRuns
                    .OrderByDescending(r => r.Version)
                    .Select(r => r.ResultsJson)
                    .FirstOrDefault(),
                LatestFingerprint = p.CalculationRuns
                    .OrderByDescending(r => r.Version)
                    .Select(r => r.InputsFingerprint)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        var items = rows.Select(row =>
        {
            var results = CalculationJson.DecodeResults(row.LatestResultsJson);

            // Stale results must never be presented as current (requirements s16).
            var state = row.LatestFingerprint is null
                ? CalculationState.Never
                : row.LatestFingerprint == row.InputsFingerprint
                    ? CalculationState.Current
                    : CalculationState.Outdated;

            return new ProjectSummaryDto(
                row.Id, row.Number, row.Name, row.CustomerName, row.Status,
                results?.Revenue, results?.NetProfit, state, row.UpdatedAt);
        }).ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? new ProjectCursor(rows[^1].UpdatedAt, rows[^1].Id).Encode()
            : null;

        return new PagedResult<ProjectSummaryDto>(items, nextCursor);
    }
}
