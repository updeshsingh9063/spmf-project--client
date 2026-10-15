using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using SPFM.Application.Abstractions;
using SPFM.Application.Projects;
using SPFM.Domain.Calculations;
using SPFM.Domain.Entities;
using SPFM.Infrastructure.Repositories;

namespace SPFM.Tests.Integration;

/// <summary>
/// Exercises the repository against a real PostgreSQL database.
///
/// These are the tests that can fail for reasons the model-level tests cannot see:
/// a migration that will not apply, a constraint that does not hold, a decimal that
/// loses precision on the round trip, a concurrent write that silently wins.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class PersistenceTests(DatabaseFixture db)
{
    /// <summary>
    /// Fails loudly, with the reason, when no database is reachable.
    ///
    /// Deliberately not a silent skip: a suite that quietly reports green when it
    /// never ran is the false confidence these tests exist to replace. To exclude
    /// them on a machine without PostgreSQL, filter them out explicitly:
    ///   dotnet test --filter "Category!=Integration"
    /// </summary>
    private void RequireDatabase() =>
        Assert.True(db.Available, db.SkipReason ?? "No PostgreSQL server reachable.");

    private ProjectRepository NewRepository(out Infrastructure.Persistence.SpfmDbContext context)
    {
        context = db.CreateContext();
        return new ProjectRepository(context, new SpfmCalculator());
    }

    private static async Task<User> SeedUserAsync(Infrastructure.Persistence.SpfmDbContext context, string name = "Samuel Fish")
    {
        var user = new User
        {
            EntraObjectId = Guid.NewGuid().ToString(),
            TenantId = "tenant",
            Email = $"{Guid.NewGuid():N}@shield.example",
            DisplayName = name,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static SaveProjectRequest Request(string name = "Warehouse fit-out") => new()
    {
        Name = name,
        CustomerName = "Bayview Logistics",
        SiteAddress = "12 Dock Road",
        Status = ProjectStatus.Quote,
        Lines = [new ProjectLineDto("Steel security door", 4m, 820m, 1350m)],
        LabourHours = 32m,
        LabourRate = 55m,
        OverheadPercent = 12m,
    };

    [Fact]
    public async Task Migrations_apply_and_create_the_expected_tables()
    {
        RequireDatabase();
        await using var context = db.CreateContext();

        var tables = new List<string>();
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));

        tables.ShouldContain("projects");
        tables.ShouldContain("project_lines");
        tables.ShouldContain("calculation_runs");
        tables.ShouldContain("change_sets");
        tables.ShouldContain("field_changes");
        tables.ShouldContain("customers");
        tables.ShouldContain("users");
    }

    [Fact]
    public async Task The_trigram_search_indexes_exist()
    {
        RequireDatabase();

        var indexes = new List<string>();
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT indexname FROM pg_indexes WHERE tablename = 'projects'", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) indexes.Add(reader.GetString(0));

        // Both must exist: a unique B-tree for correctness, a GIN trigram for search.
        indexes.ShouldContain("ix_projects_number");
        indexes.ShouldContain("ix_projects_number_trgm");
    }

    [Fact]
    public async Task A_project_survives_the_round_trip_to_the_database()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);

            // Read it back through a brand new context: anything only in the change
            // tracker would disappear here.
            await using var fresh = db.CreateContext();
            var loaded = await fresh.Projects.Include(p => p.Lines).Include(p => p.Customer)
                .FirstAsync(p => p.Id == created.Id);

            loaded.Name.ShouldBe("Warehouse fit-out");
            loaded.Customer!.Name.ShouldBe("Bayview Logistics");
            loaded.Lines.Count.ShouldBe(1);
            loaded.Lines.First().UnitPrice.ShouldBe(1350m);
            loaded.LabourHours.ShouldBe(32m);
        }
    }

    [Fact]
    public async Task Money_keeps_its_exact_value_through_postgres()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);

            // A value that a float would mangle.
            var request = Request() with
            {
                Lines = [new ProjectLineDto("Awkward", 3m, 0.1m, 1234.5678m)],
                SubcontractorCost = 0.2m,
            };
            var created = await repo.CreateAsync(request, actor);

            await using var fresh = db.CreateContext();
            var loaded = await fresh.Projects.Include(p => p.Lines).FirstAsync(p => p.Id == created.Id);

            loaded.Lines.First().UnitPrice.ShouldBe(1234.5678m);
            loaded.Lines.First().UnitCost.ShouldBe(0.1m);
            loaded.SubcontractorCost.ShouldBe(0.2m);
            (loaded.Lines.First().UnitCost + loaded.SubcontractorCost).ShouldBe(0.3m);
        }
    }

    [Fact]
    public async Task A_duplicate_project_number_is_rejected_by_the_database()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            await repo.CreateAsync(Request() with { Number = "SSD-DUP-001" }, actor);

            await Should.ThrowAsync<DuplicateProjectNumberException>(
                async () => await repo.CreateAsync(Request() with { Number = "SSD-DUP-001" }, actor));
        }
    }

    [Fact]
    public async Task A_stale_save_is_refused_instead_of_overwriting()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);
            var staleVersion = created.Version;

            // Someone else saves first.
            var otherRepo = NewRepository(out var otherContext);
            await using (otherContext)
            {
                var other = await SeedUserAsync(otherContext, "Another user");
                await otherRepo.UpdateAsync(created.Id, Request("Renamed by someone else"), other, null);
            }

            // Our save still carries the version we loaded, so it must be refused.
            var thirdRepo = NewRepository(out var thirdContext);
            await using (thirdContext)
            {
                var me = await SeedUserAsync(thirdContext, "Me");
                await Should.ThrowAsync<ConcurrencyConflictException>(
                    async () => await thirdRepo.UpdateAsync(created.Id, Request("My edit"), me, staleVersion));
            }

            await using var fresh = db.CreateContext();
            var loaded = await fresh.Projects.FirstAsync(p => p.Id == created.Id);
            loaded.Name.ShouldBe("Renamed by someone else", "the first save must survive");
        }
    }

    [Fact]
    public async Task Editing_a_project_records_what_changed()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);

            var updateRepo = NewRepository(out var updateContext);
            await using (updateContext)
            {
                var editor = await SeedUserAsync(updateContext, "Updesh");
                await updateRepo.UpdateAsync(created.Id, Request() with { LabourHours = 40m }, editor, null);
            }

            var historyRepo = NewRepository(out var historyContext);
            await using (historyContext)
            {
                var history = await historyRepo.GetHistoryAsync(created.Id, null, 25);

                history.Items.Count.ShouldBe(2); // created, then updated
                var update = history.Items.First(h => h.Action == ChangeAction.Updated);
                update.By.ShouldBe("Updesh");

                var change = update.Changes.ShouldHaveSingleItem();
                change.Field.ShouldBe("Labour hours");
                change.From.ShouldBe("32");
                change.To.ShouldBe("40");
            }
        }
    }

    [Fact]
    public async Task Saving_without_changing_anything_adds_no_history()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);

            var updateRepo = NewRepository(out var updateContext);
            await using (updateContext)
            {
                var editor = await SeedUserAsync(updateContext, "Updesh");
                await updateRepo.UpdateAsync(created.Id, Request(), editor, null);
            }

            var historyRepo = NewRepository(out var historyContext);
            await using (historyContext)
            {
                var history = await historyRepo.GetHistoryAsync(created.Id, null, 25);
                history.Items.Count.ShouldBe(1, "only the creation should be recorded");
            }
        }
    }

    [Fact]
    public async Task Calculation_runs_accumulate_rather_than_overwrite()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);
            var calculator = new SpfmCalculator();

            var inputs = ProjectInputsMapper.ToInputs(
                await context.Projects.Include(p => p.Lines).FirstAsync(p => p.Id == created.Id));

            for (var i = 0; i < 3; i++)
            {
                var runRepo = NewRepository(out var runContext);
                await using (runContext)
                {
                    var runner = await SeedUserAsync(runContext, $"User {i}");
                    await runRepo.AddCalculationRunAsync(
                        created.Id, inputs, calculator.Calculate(inputs),
                        calculator.Fingerprint(inputs), calculator.EngineVersion, runner);
                }
            }

            await using var fresh = db.CreateContext();
            var runs = await fresh.CalculationRuns.Where(r => r.ProjectId == created.Id)
                .OrderBy(r => r.Version).ToListAsync();

            runs.Count.ShouldBe(3);
            runs.Select(r => r.Version).ShouldBe(new[] { 1, 2, 3 });
            runs.ShouldAllBe(r => r.EngineVersion == calculator.EngineVersion);
        }
    }

    [Fact]
    public async Task Search_finds_a_project_by_part_of_its_name()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            await repo.CreateAsync(Request("Strongroom door replacement"), actor);
            await repo.CreateAsync(Request("Loading bay shutters"), actor);

            var searchRepo = NewRepository(out var searchContext);
            await using (searchContext)
            {
                // Case-insensitive substring, which is what the search box sends.
                var hits = await searchRepo.SearchAsync(new ProjectSearch { Query = "strongroom" });
                hits.Items.ShouldContain(p => p.Name == "Strongroom door replacement");
                hits.Items.ShouldNotContain(p => p.Name == "Loading bay shutters");
            }
        }
    }

    [Fact]
    public async Task Paging_returns_every_project_exactly_once()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            const int total = 25;
            for (var i = 0; i < total; i++)
                await repo.CreateAsync(Request($"Paged project {i:D2}") with { Number = $"PAGE-{i:D3}" }, actor);

            var seen = new List<Guid>();
            string? cursor = null;
            var pages = 0;

            var pageRepo = NewRepository(out var pageContext);
            await using (pageContext)
            {
                do
                {
                    var page = await pageRepo.SearchAsync(new ProjectSearch { Query = "Paged project", Cursor = cursor, Limit = 7 });
                    seen.AddRange(page.Items.Select(p => p.Id));
                    cursor = page.NextCursor;
                    pages++;
                } while (cursor is not null && pages < 20);
            }

            // The real risk with keyset paging is rows repeating or being skipped
            // across boundaries, which only shows up against a real database.
            seen.Count.ShouldBe(total);
            seen.Distinct().Count().ShouldBe(total);
        }
    }

    [Fact]
    public async Task A_customer_is_reused_rather_than_duplicated()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            await repo.CreateAsync(Request("First") with { CustomerName = "Harbour Freight" }, actor);

            var second = NewRepository(out var secondContext);
            await using (secondContext)
            {
                var other = await SeedUserAsync(secondContext, "Other");
                // Different casing: still the same customer.
                await second.CreateAsync(Request("Second") with { CustomerName = "harbour freight" }, other);
            }

            await using var fresh = db.CreateContext();
            var matches = await fresh.Customers.CountAsync(c => EF.Functions.ILike(c.Name, "harbour freight"));
            matches.ShouldBe(1);
        }
    }

    [Fact]
    public async Task A_customer_cannot_be_deleted_out_from_under_its_projects()
    {
        RequireDatabase();
        var repo = NewRepository(out var context);
        await using (context)
        {
            var actor = await SeedUserAsync(context);
            var created = await repo.CreateAsync(Request(), actor);

            await using var fresh = db.CreateContext();
            var customer = await fresh.Customers.FirstAsync(c => c.Id == created.CustomerId);
            fresh.Customers.Remove(customer);

            // Restrict, not cascade: deleting a customer must never take projects with it.
            await Should.ThrowAsync<DbUpdateException>(async () => await fresh.SaveChangesAsync());
        }
    }
}
