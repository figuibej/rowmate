namespace RowMate.Core.Design
{
    // CRC-32 de PNG (ISO 3309, polinomio 0xEDB88320 reflejado; PNG §5.5), con tabla.
    public static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Compute(byte[] data, int offset, int count)
        {
            return Update(0xFFFFFFFFu, data, offset, count) ^ 0xFFFFFFFFu;
        }

        public static uint Compute(byte[] data)
        {
            return Compute(data, 0, data == null ? 0 : data.Length);
        }

        // Sigue un CRC en curso (sin la inversión final): Compute = Update(0xFFFFFFFF, …) ^ 0xFFFFFFFF.
        public static uint Update(uint crc, byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            var c = crc;
            var end = offset + count;
            for (var i = offset; i < end; i++)
            {
                c = Table[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            }

            return c;
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }
    }
}
