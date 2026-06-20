namespace SH.Framework.Cryptography;

internal static class Crc32
{
    public static uint Compute(byte[] data)
    {
        if (data == null || data.Length == 0)
            return 0u;

        uint crc = 0xFFFFFFFFu;
        foreach (byte b in data)
        {
            uint idx = (crc ^ b) & 0xFF;
            crc = (crc >> 8) ^ Table[idx];
        }
        return ~crc;
    }

    private static readonly uint[] Table = CreateTable();

    private static uint[] CreateTable()
    {
        const uint poly = 0xEDB88320u;
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; ++i)
        {
            uint c = i;
            for (int j = 0; j < 8; ++j)
            {
                if ((c & 1) != 0)
                    c = poly ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[i] = c;
        }
        return table;
    }
}
