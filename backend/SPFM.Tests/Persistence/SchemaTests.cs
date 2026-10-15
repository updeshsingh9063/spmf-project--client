using Microsoft.EntityFrameworkCore;
using Shouldly;
using SPFM.Domain.Entities;
using SPFM.Infrastructure.Persistence;

namespace SPFM.Tests.Persistence;

/// <summary>
/// Guards invariants of the EF model. These inspect the built model only — no
/// database is required, so they run in CI without a container.
///
/// They exist because a model mistake surfaces as a failed migration during
/// deployment or, worse, as silently missing constraints in production.
/// </summary>
public class SchemaTests
{
    private static SpfmDbContext Context()
    {
        var options = new DbContextOptionsBuilder<SpfmDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")   // never connected to
            .Options;
        return new SpfmDbContext(options);
    }

    [Fact]
    public void Project_number_has_a_unique_index()
    {
        // REGRESSION: configuring a second index on Number without naming it merged
        // into this one, dropping uniqueness and producing an invalid UNIQUE GIN index.
        var entity = Context().Model.FindEntityType(typeof(Project))!;

        var unique = entity.GetIndexes().Where(i =>
            i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(Project.Number));

        unique.ShouldHaveSingleItem();
    }

    [Fact]
    public void Project_number_also_has_a_separate_trigram_index_for_search()
    {
        var entity = Context().Model.FindEntityType(typeof(Project))!;

        var onNumber = entity.GetIndexes()
            .Where(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Project.Number))
            .ToList();

        onNumber.Count.ShouldBe(2, "number needs a unique B-tree AND a GIN trigram index");
        onNumber.Count(i => i.IsUnique).ShouldBe(1);
    }

    [Fact]
    public void No_index_is_both_unique_and_gin()
    {
        // PostgreSQL cannot create a unique GIN index; the migration would fail.
        foreach (var entity in Context().Model.GetEntityTypes())
        {
            foreach (var index in entity.GetIndexes().Where(i => i.IsUnique))
            {
                var method = index.FindAnnotation("Npgsql:IndexMethod")?.Value as string;
                method.ShouldNotBe("gin", $"{entity.ShortName()}.{index.GetDatabaseName()} is unique and GIN");
            }
        }
    }

    [Theory]
    [InlineData(nameof(Project.Variations))]
    [InlineData(nameof(Project.LabourHours))]
    [InlineData(nameof(Project.LabourRate))]
    [InlineData(nameof(Project.SubcontractorCost))]
    [InlineData(nameof(Project.OtherDirectCost))]
    public void Project_money_columns_are_exact_decimal(string propertyName)
    {
        // Requirements s29: money is never floating point.
        var property = Context().Model.FindEntityType(typeof(Project))!.FindProperty(propertyName)!;
        property.GetColumnType().ShouldBe("numeric(18,4)");
    }

    [Theory]
    [InlineData(nameof(ProjectLine.Quantity))]
    [InlineData(nameof(ProjectLine.UnitCost))]
    [InlineData(nameof(ProjectLine.UnitPrice))]
    public void Project_line_money_columns_are_exact_decimal(string propertyName)
    {
        var property = Context().Model.FindEntityType(typeof(ProjectLine))!.FindProperty(propertyName)!;
        property.GetColumnType().ShouldBe("numeric(18,4)");
    }

    [Fact]
    public void No_money_column_anywhere_is_a_floating_point_type()
    {
        var offenders = Context().Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties()
                .Where(p => p.ClrType == typeof(double) || p.ClrType == typeof(float)
                         || p.ClrType == typeof(double?) || p.ClrType == typeof(float?))
                .Select(p => $"{e.ShortName()}.{p.Name}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Project_uses_xmin_for_optimistic_concurrency()
    {
        var property = Context().Model.FindEntityType(typeof(Project))!
            .FindProperty(nameof(Project.Version))!;

        property.GetColumnName().ShouldBe("xmin");
        property.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void Entra_object_id_is_unique_so_a_user_cannot_be_duplicated()
    {
        var entity = Context().Model.FindEntityType(typeof(User))!;

        entity.GetIndexes()
            .Any(i => i.IsUnique && i.Properties.Count == 1
                   && i.Properties[0].Name == nameof(User.EntraObjectId))
            .ShouldBeTrue();
    }

    [Fact]
    public void Deleting_a_customer_cannot_cascade_away_projects()
    {
        var fk = Context().Model.FindEntityType(typeof(Project))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Customer));

        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Tables_and_columns_are_snake_case()
    {
        foreach (var entity in Context().Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            table.ShouldBe(table.ToLowerInvariant());

            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName();
                column.ShouldBe(column.ToLowerInvariant());
            }
        }
    }
}
