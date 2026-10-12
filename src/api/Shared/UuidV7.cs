using System.Buffers.Binary;
using System.Security.Cryptography;

namespace ProjectTracker.Api.Shared;

public static class UuidV7
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        var timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        RandomNumberGenerator.Fill(bytes[6..]);

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        var a = BinaryPrimitives.ReadInt32BigEndian(bytes[..4]);
        var b = BinaryPrimitives.ReadInt16BigEndian(bytes.Slice(4, 2));
        var c = BinaryPrimitives.ReadInt16BigEndian(bytes.Slice(6, 2));
        var d = bytes.Slice(8, 8).ToArray();

        return new Guid(a, b, c, d);
    }
}
