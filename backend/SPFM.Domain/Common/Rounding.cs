namespace SPFM.Domain.Common;

/// <summary>
/// Rounding for financial values. Money is never a floating-point type anywhere in
/// this system (requirements s29): all arithmetic is decimal, rounded at defined
/// points only.
///
/// OPEN QUESTION: the client's specification must confirm where rounding applies and
/// to how many places (requirements s12). Commercial "away from zero" is used until
/// then, with intermediate values held at 4dp so error does not compound.
/// </summary>
public static class Rounding
{
    public const int CurrencyPlaces = 2;
    public const int IntermediatePlaces = 4;
    public const int PercentPlaces = 2;

    public static decimal Currency(decimal value) =>
        Math.Round(value, CurrencyPlaces, MidpointRounding.AwayFromZero);

    public static decimal Intermediate(decimal value) =>
        Math.Round(value, IntermediatePlaces, MidpointRounding.AwayFromZero);

    public static decimal Percent(decimal value) =>
        Math.Round(value, PercentPlaces, MidpointRounding.AwayFromZero);

    /// <summary>Percentage of a whole, or null when the whole is zero.</summary>
    public static decimal? PercentOf(decimal part, decimal whole) =>
        whole == 0m ? null : Percent(part / whole * 100m);
}
