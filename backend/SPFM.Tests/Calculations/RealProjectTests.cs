using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using SPFM.Domain.Calculations;

namespace SPFM.Tests.Calculations;

/// <summary>
/// Checks the calculation engine against named real Shield projects whose results
/// the client already knows to be correct (requirements §14, §33).
///
/// This is the acceptance gate for the calculations. §14 is explicit that financial
/// logic must not be validated by checking whether the UI "looks correct", and these
/// cases are what replace that. They are handed over as permanent regression tests.
///
/// Cases live in real-projects.json so adding one needs no C#.
/// </summary>
public class RealProjectTests
{
    public sealed record CaseFile([property: JsonPropertyName("cases")] List<NamedCase> Cases);

    public sealed record NamedCase
    {
        public required string Id { get; init; }
        public required string Name { get; init; }

        /// <summary>"shield-real-project" is checked; "placeholder" is not.</summary>
        public required string Source { get; init; }

        public string? Notes { get; init; }
        public required CaseInputs Inputs { get; init; }

        /// <summary>Only the figures present are asserted.</summary>
        public Dictionary<string, decimal> Expected { get; init; } = [];

        public override string ToString() => $"{Id} {Name}";
    }

    public sealed record CaseLine(string Description, decimal Quantity, decimal UnitCost, decimal UnitPrice);

    public sealed record CaseInputs
    {
        public List<CaseLine> Lines { get; init; } = [];
        public decimal Variations { get; init; }
        public decimal LabourHours { get; init; }
        public decimal LabourRate { get; init; }
        public decimal SubcontractorCost { get; init; }
        public decimal OtherDirectCost { get; init; }
        public decimal OverheadPercent { get; init; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static CaseFile LoadCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Calculations", "real-projects.json");
        File.Exists(path).ShouldBeTrue($"Test case file missing at {path}");

        return JsonSerializer.Deserialize<CaseFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("real-projects.json could not be read.");
    }

    public static TheoryData<NamedCase> RealProjects()
    {
        var data = new TheoryData<NamedCase>();
        foreach (var c in LoadCases().Cases.Where(c => c.Source == "shield-real-project"))
            data.Add(c);
        return data;
    }

    // Tagged with the gate so both move together: xUnit fails a Theory with no
    // data, and "no real projects yet" is reported once, by the gate below, rather
    // than twice.
    [Theory]
    [MemberData(nameof(RealProjects))]
    [Trait("Category", "AcceptanceGate")]
    public void A_real_Shield_project_calculates_to_its_known_result(NamedCase testCase)
    {
        var calculator = new SpfmCalculator();

        var results = calculator.Calculate(new SpfmInputs
        {
            Lines = [.. testCase.Inputs.Lines.Select(l => new SpfmLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                UnitPrice = l.UnitPrice,
            })],
            Variations = testCase.Inputs.Variations,
            LabourHours = testCase.Inputs.LabourHours,
            LabourRate = testCase.Inputs.LabourRate,
            SubcontractorCost = testCase.Inputs.SubcontractorCost,
            OtherDirectCost = testCase.Inputs.OtherDirectCost,
            OverheadPercent = testCase.Inputs.OverheadPercent,
        });

        foreach (var (field, expected) in testCase.Expected)
        {
            var property = typeof(SpfmResults).GetProperty(
                field, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            property.ShouldNotBeNull($"{testCase.Id}: '{field}' is not a result of the SPFM calculation. " +
                                     $"Available: {string.Join(", ", typeof(SpfmResults).GetProperties().Select(p => p.Name))}");

            var actual = property!.GetValue(results);
            actual.ShouldNotBeNull($"{testCase.Id}: {field} came back empty.");

            Convert.ToDecimal(actual).ShouldBe(expected,
                $"{testCase.Id} ({testCase.Name}): {field}");
        }
    }

    /// <summary>
    /// Fails while no real project has been supplied.
    ///
    /// Deliberate: without it, a suite containing zero real cases reports green and
    /// the Alpha looks validated when the single most important check has never run.
    /// Requirements §14 asks for 2-3 real projects; this test is what makes their
    /// absence visible instead of silent.
    /// </summary>
    [Fact]
    [Trait("Category", "AcceptanceGate")]
    public void The_client_has_supplied_real_projects_to_validate_against()
    {
        var real = LoadCases().Cases.Count(c => c.Source == "shield-real-project");

        real.ShouldBeGreaterThanOrEqualTo(2,
            "Requirements §14 and §33 require 2-3 real Shield projects with known expected results. " +
            $"{real} supplied so far. Add them to SPFM.Tests/Calculations/real-projects.json. " +
            "Until then the SPFM calculations are unverified, whatever the rest of this suite reports.");
    }
}
