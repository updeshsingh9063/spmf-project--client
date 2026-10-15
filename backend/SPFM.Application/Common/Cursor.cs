using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace SPFM.Application.Common;

/// <summary>
/// Opaque pagination cursor carrying the sort key of the last row on a page.
///
/// Keyset paging asks "give me rows after this exact point" rather than "skip 500
/// rows", so page 20 costs the same as page 1. The value is base64url-encoded to
/// discourage clients from parsing it, which keeps the sort key free to change.
/// </summary>
public readonly record struct ProjectCursor(DateTimeOffset UpdatedAt, Guid Id)
{
    public string Encode()
    {
        var raw = $"{UpdatedAt.UtcDateTime.Ticks}:{Id:N}";
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// Returns null for anything unparseable. A malformed cursor means "start from
    /// the beginning", never a 500 — it typically comes from a stale bookmark.
    /// </summary>
    public static ProjectCursor? TryDecode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));
            var parts = raw.Split(':', 2);
            if (parts.Length != 2) return null;

            if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return null;
            if (!Guid.TryParseExact(parts[1], "N", out var id)) return null;

            return new ProjectCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        }
        catch
        {
            return null;
        }
    }
}
