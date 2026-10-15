using System.Text.Json;
using System.Text.Json.Serialization;
using SPFM.Domain.Calculations;

namespace SPFM.Application.Common;

/// <summary>
/// JSON contract for the input/result snapshots stored on a calculation run.
///
/// Pinned here, in the application layer, rather than inherited from the web host:
/// these payloads are persisted financial records, so a future change to the API's
/// serializer must not silently alter how historical results read back.
/// </summary>
public static class CalculationJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.Strict,
        WriteIndented = false,
    };

    public static string Encode<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static SpfmResults? DecodeResults(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SpfmResults>(json, Options);

    public static SpfmInputs? DecodeInputs(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SpfmInputs>(json, Options);
}
