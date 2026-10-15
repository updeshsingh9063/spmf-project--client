namespace SPFM.Api.Configuration;

/// <summary>
/// Allowed browser origins, from configuration rather than hardcoded, so the same
/// build runs locally and in production (requirements s31).
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "SpfmFrontend";

    public string[] AllowedOrigins { get; set; } = [];
}
