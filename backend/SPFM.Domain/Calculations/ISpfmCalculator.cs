namespace SPFM.Domain.Calculations;

/// <summary>
/// The SPFM calculation engine.
///
/// Deliberately an interface with a single pure method: it takes inputs and returns
/// results. No database, no clock, no HTTP context, no I/O. That is what lets the
/// formulas be tested on their own against the client's real projects
/// (requirements s11, s13, s14).
/// </summary>
public interface ISpfmCalculator
{
    /// <summary>Identifies the formula set that produced a result. Recorded on every run.</summary>
    string EngineVersion { get; }

    /// <summary>Deterministic: identical inputs always produce identical results.</summary>
    SpfmResults Calculate(SpfmInputs inputs);

    /// <summary>Stable hash of the inputs, used to detect stale results.</summary>
    string Fingerprint(SpfmInputs inputs);
}
