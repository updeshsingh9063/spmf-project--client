namespace SPFM.Api.Configuration;

/// <summary>
/// Fails fast on misconfiguration.
///
/// Without this, a missing Entra client id surfaces as a 500 on every request —
/// including /health — because Microsoft.Identity.Web validates its options lazily
/// on first use. A container orchestrator would see the app as "started" and route
/// traffic to it. Better to refuse to boot and say exactly what is missing.
/// </summary>
public static class StartupValidation
{
    public static void ValidateRequiredConfiguration(this IConfiguration configuration)
    {
        var missing = new List<string>();

        void Require(string key)
        {
            if (string.IsNullOrWhiteSpace(configuration[key])) missing.Add(key);
        }

        Require("AzureAd:Instance");
        Require("AzureAd:TenantId");
        Require("AzureAd:ClientId");
        Require("AzureAd:Audience");
        Require("ConnectionStrings:DefaultConnection");

        if (configuration.GetSection($"{CorsSettings.SectionName}:AllowedOrigins").Get<string[]>() is not { Length: > 0 })
            missing.Add($"{CorsSettings.SectionName}:AllowedOrigins");

        if (missing.Count == 0) return;

        throw new InvalidOperationException(
            "SPFM cannot start: required configuration is missing." + Environment.NewLine +
            string.Join(Environment.NewLine, missing.Select(k => $"  - {k}")) + Environment.NewLine +
            "Set these as environment variables (double underscore for nesting, " +
            "e.g. AzureAd__ClientId) or in appsettings.Development.json for local work. " +
            "See .env.example.");
    }
}
