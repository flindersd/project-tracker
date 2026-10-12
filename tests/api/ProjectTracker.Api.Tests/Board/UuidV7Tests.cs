using ProjectTracker.Api.Shared;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class UuidV7Tests
{
    // Given no arguments, When UuidV7.NewGuid() is called,
    // Then the version nibble equals 0x7 and variant bits equal 10.
    [Fact]
    public void NewGuid_sets_version_7_and_variant_bits()
    {
        var guid = UuidV7.NewGuid();
        var bytes = guid.ToByteArray();

        // .NET Guid byte layout: bytes[7] high nibble = version, bytes[8] high 2 bits = variant.
        var version = (bytes[7] & 0xF0) >> 4;
        var variant = (bytes[8] & 0xC0) >> 6;

        Assert.Equal(0x7, version);
        Assert.Equal(0b10, variant);
    }

    // Given two sequential calls a few milliseconds apart,
    // When their timestamp bytes are compared,
    // Then the second is greater than or equal to the first.
    [Fact]
    public void NewGuid_timestamp_is_monotonic_non_decreasing()
    {
        var first = UuidV7.NewGuid();
        Thread.Sleep(5);
        var second = UuidV7.NewGuid();

        var firstTimestamp = ExtractUnixMsTimestamp(first);
        var secondTimestamp = ExtractUnixMsTimestamp(second);

        Assert.True(secondTimestamp >= firstTimestamp,
            $"expected second timestamp ({secondTimestamp}) >= first ({firstTimestamp})");
    }

    // Given 1,000 calls in a tight loop,
    // When collected into a set,
    // Then all 1,000 values are unique.
    [Fact]
    public void NewGuid_produces_unique_values_under_rapid_generation()
    {
        var seen = new HashSet<Guid>();
        for (var i = 0; i < 1000; i++)
        {
            seen.Add(UuidV7.NewGuid());
        }

        Assert.Equal(1000, seen.Count);
    }

    private static long ExtractUnixMsTimestamp(Guid guid)
    {
        var bytes = guid.ToByteArray();
        // Big-endian 48-bit timestamp occupies bytes[0..5] per TD-H (explicit byte writes, not
        // a naive round-trip through new Guid(byte[])).
        Span<byte> ts = stackalloc byte[8];
        ts[2] = bytes[0];
        ts[3] = bytes[1];
        ts[4] = bytes[2];
        ts[5] = bytes[3];
        ts[6] = bytes[4];
        ts[7] = bytes[5];
        if (BitConverter.IsLittleEndian)
        {
            ts.Reverse();
        }
        return BitConverter.ToInt64(ts);
    }
}
