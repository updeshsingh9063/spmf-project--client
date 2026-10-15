using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SPFM.Tests.Integration;

/// <summary>
/// Hosts the real API in-process, against the real database, with sign-in stubbed.
///
/// Stubbing authentication is deliberate and limited: acquiring a genuine Entra token
/// needs a Shield work account and a browser, which no automated test can do. What is
/// replaced is only the token *exchange*. Everything the API does with an identity —
/// the default-deny policy, scope checks, user provisioning, attribution of changes —
/// runs exactly as in production.
///
/// Rejection of invalid tokens is covered separately, by the real pipeline returning
/// 401 for a malformed bearer token.
/// </summary>
public sealed class SpfmApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string TestScheme = "TestAuth";

    /// <summary>Entra object id of the signed-in test user. Changing it simulates a second person.</summary>
    public string ObjectId { get; set; } = Guid.NewGuid().ToString();

    public string DisplayName { get; set; } = "Samuel Fish";
    public string Email { get; set; } = "samuel@shieldsecuritydoors.example";

    /// <summary>When false the caller is anonymous, so default-deny can be verified.</summary>
    public bool SignedIn { get; set; } = true;

    /// <summary>When false the token carries no access_as_user scope.</summary>
    public bool HasScope { get; set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(this);

            // Replace only the authentication scheme. Authorization policies, including
            // the default-deny fallback, are untouched.
            services.AddAuthentication(TestScheme)
                .AddScheme<AuthenticationSchemeOptions, StubAuthHandler>(TestScheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestScheme;
                options.DefaultChallengeScheme = TestScheme;
            });
        });

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }

    private sealed class StubAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SpfmApiFactory factory) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!factory.SignedIn) return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim>
            {
                new("oid", factory.ObjectId),
                new("tid", "83dba18f-9e69-44c9-9af6-ea69f74eb71d"),
                new("name", factory.DisplayName),
                new("preferred_username", factory.Email),
            };

            if (factory.HasScope) claims.Add(new Claim("http://schemas.microsoft.com/identity/claims/scope", "access_as_user"));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, TestScheme)));
        }
    }
}
