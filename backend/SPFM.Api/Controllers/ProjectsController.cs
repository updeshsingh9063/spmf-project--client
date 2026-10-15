using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Microsoft.Net.Http.Headers;
using SPFM.Application.Abstractions;
using SPFM.Application.Common;
using SPFM.Application.Projects;
using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;

namespace SPFM.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[RequiredScope("access_as_user")]
[Produces("application/json")]
public class ProjectsController(
    IProjectRepository projects,
    ICurrentUserAccessor currentUser,
    IUserDirectory users,
    ISpfmCalculator calculator) : ControllerBase
{
    // ---- Read ------------------------------------------------------------

    /// <summary>Search and page the project list.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 25,
        CancellationToken ct = default)
    {
        var result = await projects.SearchAsync(
            new ProjectSearch { Query = query, Status = status, Cursor = cursor, Limit = limit }, ct);

        return Ok(new { items = result.Items, nextCursor = result.NextCursor });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var project = await projects.GetWithLinesAsync(id, ct);
        if (project is null) return NotFoundProblem(id);

        var detail = await BuildDetailAsync(project, ct);

        // The frontend echoes this back as If-Match so a stale save is rejected.
        Response.Headers.ETag = detail.ETag;
        return Ok(detail);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(
        Guid id, [FromQuery] string? cursor, [FromQuery] int limit = 25, CancellationToken ct = default)
    {
        if (await projects.GetAsync(id, ct) is null) return NotFoundProblem(id);

        var result = await projects.GetHistoryAsync(id, cursor, limit, ct);
        return Ok(new { items = result.Items, nextCursor = result.NextCursor });
    }

    // ---- Write -----------------------------------------------------------

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SaveProjectRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } invalid) return invalid;

        var actor = await RequireActorAsync(ct);
        if (actor is null) return Unauthorized();

        try
        {
            var created = await projects.CreateAsync(request, actor, ct);
            var project = await projects.GetWithLinesAsync(created.Id, ct);
            var detail = await BuildDetailAsync(project!, ct);

            Response.Headers.ETag = detail.ETag;
            return CreatedAtAction(nameof(Get), new { id = created.Id }, detail);
        }
        catch (DuplicateProjectNumberException e)
        {
            return Problem(
                title: "Project number already in use",
                detail: $"Project number '{e.Number}' belongs to another project. Choose a different number.",
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (ArgumentException e)
        {
            return Problem(title: "Invalid project", detail: e.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] SaveProjectRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } invalid) return invalid;

        // Requiring If-Match makes the lost-update case impossible to hit by accident:
        // a client that never read the project cannot overwrite one that someone else
        // is editing (requirements s29).
        if (!TryReadIfMatch(out var expectedVersion))
        {
            return Problem(
                title: "If-Match required",
                detail: "Re-open the project and try again. Saving needs the version you loaded, " +
                        "so another person's changes cannot be overwritten silently.",
                statusCode: StatusCodes.Status428PreconditionRequired);
        }

        var actor = await RequireActorAsync(ct);
        if (actor is null) return Unauthorized();

        try
        {
            var updated = await projects.UpdateAsync(id, request, actor, expectedVersion, ct);
            var project = await projects.GetWithLinesAsync(updated.Id, ct);
            var detail = await BuildDetailAsync(project!, ct);

            Response.Headers.ETag = detail.ETag;
            return Ok(detail);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundProblem(id);
        }
        catch (ConcurrencyConflictException e)
        {
            return Problem(title: "Project changed elsewhere", detail: e.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (DuplicateProjectNumberException e)
        {
            return Problem(
                title: "Project number already in use",
                detail: $"Project number '{e.Number}' belongs to another project.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    // ---- Calculate -------------------------------------------------------

    /// <summary>
    /// Runs the SPFM calculations against the project's saved inputs and stores the
    /// result as a new run.
    ///
    /// Calculating deliberately uses what is in the database, not a payload: the
    /// figures of record must correspond to a project someone can reopen.
    /// </summary>
    [HttpPost("{id:guid}/calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Calculate(Guid id, CancellationToken ct)
    {
        var project = await projects.GetWithLinesAsync(id, ct);
        if (project is null) return NotFoundProblem(id);

        var actor = await RequireActorAsync(ct);
        if (actor is null) return Unauthorized();

        var inputs = ProjectInputsMapper.ToInputs(project);

        var validation = SpfmValidator.Validate(inputs);
        if (!validation.IsValid)
        {
            // Field-keyed so the UI can place each message on its input.
            return ValidationProblem(new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        var results = calculator.Calculate(inputs);
        var fingerprint = calculator.Fingerprint(inputs);

        await projects.AddCalculationRunAsync(
            id, inputs, results, fingerprint, calculator.EngineVersion, actor, ct);

        return Ok(new
        {
            results,
            calculationState = CalculationState.Current,
            engineVersion = calculator.EngineVersion,
        });
    }

    // ---- Helpers ---------------------------------------------------------

    private async Task<ProjectDetailDto> BuildDetailAsync(Project project, CancellationToken ct)
    {
        var latest = await projects.GetLatestRunAsync(project.Id, ct);
        var results = latest is null ? null : CalculationJson.DecodeResults(latest.ResultsJson);

        var state = latest is null
            ? CalculationState.Never
            : latest.InputsFingerprint == project.InputsFingerprint
                ? CalculationState.Current
                : CalculationState.Outdated;

        return new ProjectDetailDto(
            project.Id,
            project.Number,
            project.Name,
            project.CustomerId,
            project.Customer?.Name ?? "",
            project.SiteAddress,
            project.Status,
            [.. project.Lines.OrderBy(l => l.Ordinal)
                .Select(l => new ProjectLineDto(l.Description, l.Quantity, l.UnitCost, l.UnitPrice))],
            project.Variations,
            project.LabourHours,
            project.LabourRate,
            project.SubcontractorCost,
            project.OtherDirectCost,
            project.OverheadPercent,
            results,
            state,
            latest?.CalculatedAt,
            latest?.EngineVersion,
            project.CreatedAt,
            project.UpdatedAt,
            FormatETag(project.Version));
    }

    private static string FormatETag(uint version) => $"\"{version}\"";

    private bool TryReadIfMatch(out uint? version)
    {
        version = null;

        var header = Request.Headers[HeaderNames.IfMatch].ToString();
        if (string.IsNullOrWhiteSpace(header)) return false;

        // "*" means "any version": the caller is explicitly opting out of the check.
        if (header.Trim() == "*") return true;

        var trimmed = header.Trim().Trim('"');
        if (!uint.TryParse(trimmed, out var parsed)) return false;

        version = parsed;
        return true;
    }

    private async Task<User?> RequireActorAsync(CancellationToken ct)
    {
        var identity = currentUser.Identity;
        return identity is null ? null : await users.GetOrCreateAsync(identity, ct);
    }

    private IActionResult NotFoundProblem(Guid id) => Problem(
        title: "Project not found",
        detail: $"No project with id {id} exists.",
        statusCode: StatusCodes.Status404NotFound);

    private IActionResult? Validate(SaveProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors["name"] = ["Enter a project name."];

        if (request.CustomerId is null && string.IsNullOrWhiteSpace(request.CustomerName))
            errors["customerName"] = ["Enter the customer's name."];

        var inputs = ProjectInputsMapper.ToInputs(request);

        foreach (var (field, message) in SpfmValidator.Validate(inputs).Errors)
            errors[field] = [message];

        return errors.Count == 0 ? null : ValidationProblem(new ValidationProblemDetails(errors));
    }
}
