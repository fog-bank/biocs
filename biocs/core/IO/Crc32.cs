namespace Biocs.IO;

// Ref: https://tools.ietf.org/html/rfc1952#section-8
internal static class Crc32
{
    private static uint[] CrcTable { get; } = MakeCrcTable();

    // Updates a running crc with the bytes and returns the updated CRC32 value.
    public static uint UpdateCrc(uint crc, ReadOnlySpan<byte> buffer)
    {
        crc ^= 0xffffffff;

        for (int i = 0; i < buffer.Length; i++)
            crc = CrcTable[(crc ^ buffer[i]) & 0xff] ^ (crc >> 8);

        return crc ^ 0xffffffff;
    }

    // Initializes the table for a fast CRC.
    private static uint[] MakeCrcTable()
    {
        const uint Polynomial = 0xedb88320;

        var table = new uint[256];

        for (uint i = 0; i < table.Length; i++)
        {
            uint c = i;

            for (int j = 0; j < 8; j++)
            {
                if ((c & 1) != 0)
                    c = Polynomial ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[i] = c;
        }
        return table;
    }
}
