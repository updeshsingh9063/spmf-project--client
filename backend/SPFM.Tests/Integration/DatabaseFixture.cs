using Microsoft.EntityFrameworkCore;
using Npgsql;
using SPFM.Infrastructure.Persistence;

namespace SPFM.Tests.Integration;

/// <summary>
/// A real PostgreSQL database for integration tests.
///
/// Each run creates its own throwaway database and applies the migrations to it,
/// so these tests prove the migrations actually work — which unit tests against the
/// EF model cannot do. The database is dropped afterwards.
///
/// Connection comes from SPFM_TEST_CONNECTION, falling back to a local server.
/// If no server is reachable the tests are skipped rather than failed: a missing
/// database is an environment gap, not a defect in the code under test.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultAdmin =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private string _adminConnection = DefaultAdmin;
    private readonly string _databaseName = $"spfm_test_{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = "";

    /// <summary>False when no PostgreSQL server could be reached.</summary>
    public bool Available { get; private set; }

    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        _adminConnection = Environment.GetEnvironmentVariable("SPFM_TEST_CONNECTION") ?? DefaultAdmin;

        try
        {
            await using (var admin = new NpgsqlConnection(_adminConnection))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            var builder = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _databaseName };
            ConnectionString = builder.ConnectionString;

            await using var db = CreateContext();
            await db.Database.MigrateAsync();

            Available = true;
        }
        catch (Exception e)
        {
            Available = false;
            SkipReason =
                $"No PostgreSQL server reachable for integration tests ({e.GetType().Name}: {e.Message}). " +
                "Start one and set SPFM_TEST_CONNECTION.";
        }
    }

    public SpfmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SpfmDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new SpfmDbContext(options);
    }

    public async Task DisposeAsync()
    {
        if (!Available) return;

        // Pooled connections would keep the database busy and block the drop.
        NpgsqlConnection.ClearAllPools();

        try
        {
            await using var admin = new NpgsqlConnection(_adminConnection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // A leftover test database is untidy, not a failure worth reporting.
        }
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}
