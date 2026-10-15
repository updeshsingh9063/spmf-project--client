using Microsoft.EntityFrameworkCore;
using SPFM.Application.Common;
using SPFM.Application.Projects;
using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Repositories;

public sealed partial class ProjectRepository
{
    public Task<CalculationRun?> GetLatestRunAsync(Guid projectId, CancellationToken ct = default) =>
        db.CalculationRuns.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Appends a calculation run. Runs are never overwritten: the history of what the
    /// figures said, and which engine produced them, is part of the audit trail.
    /// </summary>
    public async Task<CalculationRun> AddCalculationRunAsync(
        Guid projectId, SpfmInputs inputs, SpfmResults results, string fingerprint,
        string engineVersion, User actor, CancellationToken ct = default)
    {
        var lastVersion = await db.CalculationRuns
            .Where(r => r.ProjectId == projectId)
            .MaxAsync(r => (int?)r.Version, ct) ?? 0;

        var run = new CalculationRun
        {
            ProjectId = projectId,
            Version = lastVersion + 1,
            InputsJson = CalculationJson.Encode(inputs),
            ResultsJson = CalculationJson.Encode(results),
            InputsFingerprint = fingerprint,
            EngineVersion = engineVersion,
            CalculatedById = actor.Id,
            CalculatedAt = DateTimeOffset.UtcNow,
        };

        db.CalculationRuns.Add(run);

        db.ChangeSets.Add(new ChangeSet
        {
            ProjectId = projectId,
            UserId = actor.Id,
            Action = ChangeAction.Calculated,
            At = run.CalculatedAt,
            Changes = [new FieldChange
            {
                FieldLabel = "Calculated",
                OldValue = null,
                NewValue = $"Run {run.Version} (engine {engineVersion})",
            }],
        });

        await db.SaveChangesAsync(ct);
        return run;
    }

    /// <summary>
    /// Change history, newest first, paged. History grows without bound, so an
    /// unbounded query here would make the project screen slower over time.
    /// </summary>
    public async Task<PagedResult<ChangeSetDto>> GetHistoryAsync(
        Guid projectId, string? cursor, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = db.ChangeSets.AsNoTracking()
            .Where(c => c.ProjectId == projectId);

        if (ProjectCursor.TryDecode(cursor) is { } after)
        {
            query = query.Where(c =>
                c.At < after.UpdatedAt ||
                (c.At == after.UpdatedAt && c.Id.CompareTo(after.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(c => c.At).ThenByDescending(c => c.Id)
            .Take(limit + 1)
            .Select(c => new
            {
                c.Id,
                c.At,
                c.Action,
                By = c.User!.DisplayName,
                Changes = c.Changes
                    .Select(f => new FieldChangeDto(f.FieldLabel, f.OldValue, f.NewValue))
                    .ToList(),
            })
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        var items = rows
            .Select(r => new ChangeSetDto(r.Id, r.By, r.Action, r.At, r.Changes))
            .ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? new ProjectCursor(rows[^1].At, rows[^1].Id).Encode()
            : null;

        return new PagedResult<ChangeSetDto>(items, nextCursor);
    }
}
