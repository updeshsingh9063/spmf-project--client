using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SPFM.Domain.Entities;

namespace SPFM.Infrastructure.Persistence;

public partial class SpfmDbContext(DbContextOptions<SpfmDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectLine> ProjectLines => Set<ProjectLine>();
    public DbSet<CalculationRun> CalculationRuns => Set<CalculationRun>();
    public DbSet<ChangeSet> ChangeSets => Set<ChangeSet>();
    public DbSet<FieldChange> FieldChanges => Set<FieldChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Trigram matching powers project search. Without it, "contains" search is a
        // sequential scan on every keystroke (see SYSTEM-DESIGN.md s11.1).
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpfmDbContext).Assembly);

        ApplySnakeCaseNames(modelBuilder);
    }

    /// <summary>
    /// PostgreSQL folds unquoted identifiers to lower case, so PascalCase names would
    /// have to be quoted in every hand-written query. snake_case keeps the database
    /// usable from psql and migrations readable.
    /// </summary>
    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null) entity.SetTableName(ToSnakeCase(tableName));

            foreach (var property in entity.GetProperties())
            {
                // xmin is a PostgreSQL system column: its name must not be rewritten.
                var current = property.GetColumnName();
                if (current == "xmin") continue;
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
                key.SetName(ToSnakeCase(key.GetName()!));

            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));

            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }
    }

    private static string ToSnakeCase(string name) =>
        SnakeCasePattern().Replace(name, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCasePattern();
}
