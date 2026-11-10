namespace RowMate.Core.Design
{
    // Adler-32 de zlib (RFC 1950 §8.2) a mano, para el PNG del ícono (DT14-02).
    public static class Adler32
    {
        private const uint Mod = 65521;

        public static uint Compute(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            uint a = 1;
            uint b = 0;
            var end = offset + count;
            var i = offset;
            while (i < end)
            {
                // 5552 = el mayor n que no desborda b antes del módulo (zlib NMAX).
                var block = System.Math.Min(5552, end - i);
                for (var j = 0; j < block; j++)
                {
                    a += data[i++];
                    b += a;
                }

                a %= Mod;
                b %= Mod;
            }

            return (b << 16) | a;
        }

        public static uint Compute(byte[] data)
        {
            return Compute(data, 0, data == null ? 0 : data.Length);
        }
    }
}
