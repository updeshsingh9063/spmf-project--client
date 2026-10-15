using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SPFM.Tests.Integration;

/// <summary>
/// The API with its genuine Microsoft Entra authentication left in place.
///
/// Used to prove that a bad token is actually rejected. The stubbed factory cannot
/// show that, because it replaces the very handler doing the rejecting — so without
/// this, "authentication works" would rest on a test of our own stub.
/// </summary>
public sealed class WebApplicationFactoryWithRealAuth(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
            }));

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
