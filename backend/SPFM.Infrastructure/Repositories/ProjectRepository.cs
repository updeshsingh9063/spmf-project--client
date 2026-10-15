using Microsoft.EntityFrameworkCore;
using SPFM.Application.Abstractions;
using SPFM.Application.Projects;
using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;
using SPFM.Infrastructure.Persistence;

namespace SPFM.Infrastructure.Repositories;

public sealed partial class ProjectRepository(SpfmDbContext db, ISpfmCalculator calculator) : IProjectRepository
{
    public Task<Project?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Projects.AsNoTracking()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Project?> GetWithLinesAsync(Guid id, CancellationToken ct = default) =>
        db.Projects
            .Include(p => p.Customer)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project> CreateAsync(
        SaveProjectRequest request, User actor, CancellationToken ct = default)
    {
        var customer = await ResolveCustomerAsync(request, ct);
        var number = string.IsNullOrWhiteSpace(request.Number)
            ? await GenerateNumberAsync(ct)
            : request.Number.Trim();

        if (await db.Projects.AnyAsync(p => p.Number == number, ct))
            throw new DuplicateProjectNumberException(number);

        var now = DateTimeOffset.UtcNow;
        var project = new Project
        {
            Number = number,
            Name = request.Name.Trim(),
            CustomerId = customer.Id,
            SiteAddress = request.SiteAddress.Trim(),
            Status = request.Status,
            CreatedById = actor.Id,
            UpdatedById = actor.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ApplyInputs(project, request);
        db.Projects.Add(project);

        db.ChangeSets.Add(new ChangeSet
        {
            ProjectId = project.Id,
            UserId = actor.Id,
            Action = ChangeAction.Created,
            At = now,
            Changes = [new FieldChange { FieldLabel = "Project", OldValue = null, NewValue = project.Name }],
        });

        await SaveWithNumberGuardAsync(number, ct);
        return project;
    }

    public async Task<Project> UpdateAsync(
        Guid id, SaveProjectRequest request, User actor, uint? expectedVersion, CancellationToken ct = default)
    {
        var project = await db.Projects
            .Include(p => p.Customer)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");

        if (expectedVersion is { } expected && project.Version != expected)
        {
            throw new ConcurrencyConflictException(
                "This project was changed by someone else while you were editing it.");
        }

        var customer = await ResolveCustomerAsync(request, ct);

        // Diff BEFORE mutating, or there is nothing left to compare against.
        var changes = ProjectChangeTracker.Diff(project, request, customer.Name);

        var number = string.IsNullOrWhiteSpace(request.Number) ? project.Number : request.Number.Trim();
        if (number != project.Number && await db.Projects.AnyAsync(p => p.Number == number && p.Id != id, ct))
            throw new DuplicateProjectNumberException(number);

        project.Number = number;
        project.Name = request.Name.Trim();
        project.CustomerId = customer.Id;
        project.SiteAddress = request.SiteAddress.Trim();
        project.Status = request.Status;
        project.UpdatedById = actor.Id;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        ApplyInputs(project, request);

        // A save that changed nothing should not manufacture a history entry.
        if (changes.Count > 0)
        {
            db.ChangeSets.Add(new ChangeSet
            {
                ProjectId = project.Id,
                UserId = actor.Id,
                Action = ChangeAction.Updated,
                At = project.UpdatedAt,
                Changes = [.. changes],
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // xmin moved between read and write: another save landed first.
            throw new ConcurrencyConflictException(
                "This project was changed by someone else while you were editing it.");
        }

        return project;
    }

    /// <summary>Replaces the scalar inputs and line items, and refreshes the fingerprint.</summary>
    private void ApplyInputs(Project project, SaveProjectRequest request)
    {
        project.Variations = request.Variations;
        project.LabourHours = request.LabourHours;
        project.LabourRate = request.LabourRate;
        project.SubcontractorCost = request.SubcontractorCost;
        project.OtherDirectCost = request.OtherDirectCost;
        project.OverheadPercent = request.OverheadPercent;

        // Lines are replaced wholesale rather than merged. They carry no stable
        // client-side identity, so matching them up would be guesswork; the table is
        // small and scoped to one project, so the cost is negligible.
        //
        // Both halves go through the DbSet. Assigning project.Lines instead lets EF's
        // relationship fixup pair a new row with a deleted one and emit an UPDATE
        // against a row it is also deleting. That statement affects zero rows, which
        // EF reports as DbUpdateConcurrencyException -- so every save failed as a
        // phantom "changed by someone else" conflict. Caught by the integration tests;
        // invisible to tests that never touch a database.
        foreach (var existing in project.Lines.ToList())
            db.ProjectLines.Remove(existing);

        db.ProjectLines.AddRange(request.Lines.Select((l, i) => new ProjectLine
        {
            ProjectId = project.Id,
            Ordinal = i,
            Description = l.Description.Trim(),
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            UnitPrice = l.UnitPrice,
        }));

        // Computed from the request, not from the entity: the entity's Lines
        // collection still holds the rows being deleted.
        project.InputsFingerprint = calculator.Fingerprint(ProjectInputsMapper.ToInputs(request));
    }

    private async Task<Customer> ResolveCustomerAsync(SaveProjectRequest request, CancellationToken ct)
    {
        if (request.CustomerId is { } id)
        {
            return await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new KeyNotFoundException($"Customer {id} was not found.");
        }

        var name = (request.CustomerName ?? "").Trim();
        if (name.Length == 0)
            throw new ArgumentException("A customer is required.", nameof(request));

        var existing = await db.Customers
            .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, name), ct);
        if (existing is not null) return existing;

        var created = new Customer { Name = name };
        db.Customers.Add(created);
        return created;
    }

    /// <summary>
    /// PLACEHOLDER numbering. OPEN QUESTION (requirements s7): the real format, and
    /// whether SPFM or QuickBooks owns it.
    /// </summary>
    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var prefix = $"SSD-{year}-";

        var last = await db.Projects
            .Where(p => p.Number.StartsWith(prefix))
            .OrderByDescending(p => p.Number)
            .Select(p => p.Number)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var parsed)) next = parsed + 1;

        return $"{prefix}{next:D3}";
    }

    private async Task SaveWithNumberGuardAsync(string number, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException?.Message.Contains("ix_projects_number") == true)
        {
            // The unique index is the real guard: the pre-check above can lose a race
            // with a concurrent create.
            throw new DuplicateProjectNumberException(number);
        }
    }
}
