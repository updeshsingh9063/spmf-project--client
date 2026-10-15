using SPFM.Application.Common;
using SPFM.Domain.Calculations;
using SPFM.Application.Projects;
using SPFM.Domain.Entities;

namespace SPFM.Application.Abstractions;

/// <summary>
/// Thrown when a write targets a version of the project that is no longer current.
/// Surfaces as 409 rather than silently overwriting another user's edit.
/// </summary>
public sealed class ConcurrencyConflictException(string message) : Exception(message);

/// <summary>Thrown when a project number is already taken.</summary>
public sealed class DuplicateProjectNumberException(string number)
    : Exception($"Project number '{number}' is already in use.")
{
    public string Number { get; } = number;
}

public interface IProjectRepository
{
    Task<PagedResult<ProjectSummaryDto>> SearchAsync(ProjectSearch search, CancellationToken ct = default);

    Task<Project?> GetAsync(Guid id, CancellationToken ct = default);

    Task<Project?> GetWithLinesAsync(Guid id, CancellationToken ct = default);

    Task<Project> CreateAsync(SaveProjectRequest request, User actor, CancellationToken ct = default);

    /// <param name="expectedVersion">
    /// The version the caller last read, from the ETag. Null skips the check, which
    /// should only happen for a caller that has not read the project.
    /// </param>
    Task<Project> UpdateAsync(
        Guid id, SaveProjectRequest request, User actor, uint? expectedVersion, CancellationToken ct = default);

    Task<CalculationRun?> GetLatestRunAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Appends a calculation run. Takes domain objects rather than JSON so the
    /// storage format stays an Infrastructure concern.
    /// </summary>
    Task<CalculationRun> AddCalculationRunAsync(
        Guid projectId, SpfmInputs inputs, SpfmResults results, string fingerprint,
        string engineVersion, User actor, CancellationToken ct = default);

    Task<PagedResult<ChangeSetDto>> GetHistoryAsync(
        Guid projectId, string? cursor, int limit, CancellationToken ct = default);
}
