using Shouldly;
using SPFM.Application.Common;

namespace SPFM.Tests.Projects;

/// <summary>
/// The cursor is how every list page finds its place. A rounding or parsing mistake
/// here shows up as rows repeating or disappearing between pages — the kind of bug
/// that is easy to miss by eye and alarming in a financial list.
/// </summary>
public class CursorTests
{
    [Fact]
    public void Round_trips_exactly()
    {
        var original = new ProjectCursor(
            new DateTimeOffset(2026, 10, 15, 14, 32, 11, 123, TimeSpan.Zero),
            Guid.Parse("8f14e45f-ceea-467a-9575-1e0a5e39f5a1"));

        var decoded = ProjectCursor.TryDecode(original.Encode());

        decoded.ShouldNotBeNull();
        decoded!.Value.Id.ShouldBe(original.Id);
        decoded.Value.UpdatedAt.ShouldBe(original.UpdatedAt);
    }

    [Fact]
    public void Sub_millisecond_precision_survives()
    {
        // Timestamps are the sort key. Truncating ticks would make two rows written
        // in the same millisecond indistinguishable, so a page boundary between them
        // could skip or repeat one.
        var precise = new DateTimeOffset(2026, 10, 15, 14, 32, 11, TimeSpan.Zero).AddTicks(1234567);
        var cursor = new ProjectCursor(precise, Guid.NewGuid());

        ProjectCursor.TryDecode(cursor.Encode())!.Value.UpdatedAt.ShouldBe(precise);
    }

    [Fact]
    public void Is_normalised_to_utc_so_a_server_timezone_change_cannot_shift_pages()
    {
        var inSydney = new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.FromHours(11));
        var cursor = new ProjectCursor(inSydney, Guid.NewGuid());

        var decoded = ProjectCursor.TryDecode(cursor.Encode())!.Value;

        decoded.UpdatedAt.Offset.ShouldBe(TimeSpan.Zero);
        decoded.UpdatedAt.UtcDateTime.ShouldBe(inSydney.UtcDateTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!!")]
    [InlineData("YWJj")]                      // valid base64, wrong shape
    [InlineData("MTIzOm5vdGFndWlk")]          // ticks ok, id not a guid
    [InlineData("bm90YW51bWJlcjphYmM=")]      // ticks not a number
    public void Malformed_cursors_decode_to_null_rather_than_throwing(string? input)
    {
        // A bad cursor usually means a stale bookmark. It should restart the list,
        // not return a 500.
        ProjectCursor.TryDecode(input).ShouldBeNull();
    }

    [Fact]
    public void Encoding_is_url_safe()
    {
        // Cursors travel in query strings; +, / or = would need escaping.
        for (var i = 0; i < 50; i++)
        {
            var encoded = new ProjectCursor(DateTimeOffset.UtcNow.AddSeconds(i), Guid.NewGuid()).Encode();

            encoded.ShouldNotContain("+");
            encoded.ShouldNotContain("/");
            encoded.ShouldNotContain("=");
        }
    }

    [Fact]
    public void Different_rows_produce_different_cursors()
    {
        var at = DateTimeOffset.UtcNow;
        var a = new ProjectCursor(at, Guid.NewGuid()).Encode();
        var b = new ProjectCursor(at, Guid.NewGuid()).Encode();

        a.ShouldNotBe(b);
    }
}
