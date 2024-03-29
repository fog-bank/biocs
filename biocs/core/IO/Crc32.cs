﻿namespace Biocs.IO
{
    // Ref: https://tools.ietf.org/html/rfc1952#section-8
    internal static class Crc32
    {
        private static readonly uint[] CrcTable = MakeCrcTable();

        // Updates a running crc with the bytes and returns the updated CRC32 value.
        public static uint UpdateCrc(uint crc, byte[] array, int offset, int length)
        {
            crc ^= 0xffffffff;

            for (int i = 0; i < length; i++)
                crc = CrcTable[(crc ^ array[offset + i]) & 0xff] ^ (crc >> 8);

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
}
