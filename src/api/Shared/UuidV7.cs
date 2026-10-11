using System.Security.Cryptography;

namespace ProjectTracker.Api.Shared;

public static class UuidV7
{
    public static Guid NewGuid()
    {
        var timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> bytes = stackalloc byte[16];

        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        RandomNumberGenerator.Fill(bytes[6..]);

        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }
}
