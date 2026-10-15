using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SPFM.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core command-line tools (migrations). The running application
/// builds its context through dependency injection instead.
///
/// Reads SPFM_MIGRATION_CONNECTION when set, so migrations can be generated against a
/// throwaway database without editing a file.
/// </summary>
public class SpfmDbContextFactory : IDesignTimeDbContextFactory<SpfmDbContext>
{
    public SpfmDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SPFM_MIGRATION_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=spfm;Username=spfm;Password=spfm123";

        var options = new DbContextOptionsBuilder<SpfmDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new SpfmDbContext(options);
    }
}
