namespace RowMate.Tests.Support
{
    // Los paquetes de 0x2A37 de la spec de la It. 11 (01-po-spec §0). Cada lectura devuelve un arreglo nuevo.
    public static class HrPackets
    {
        // [0x00, 72] → 72 (8 bits, no informa contacto).
        public static byte[] B72 => new byte[] { 0x00, 72 };

        // [0x06, 142] → 142 con contacto.
        public static byte[] Contact142 => new byte[] { 0x06, 142 };

        // [0x01, 0x8E, 0x00] → 142 (16 bits).
        public static byte[] U16_142 => new byte[] { 0x01, 0x8E, 0x00 };

        // [0x04, 0] → sin contacto (0).
        public static byte[] NoContact => new byte[] { 0x04, 0 };

        // [0x01, 72] → inválido (16 bits con 2 bytes).
        public static byte[] Invalid => new byte[] { 0x01, 72 };
    }
}
