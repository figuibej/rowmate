using System;
using System.Text;

namespace RowMate.Core.Export
{
    // El archivo TCX en bytes (DT12-01, AC-STR-09-01): UTF-8 SIN BOM (el texto ya trae sus \n). Las constantes del pedido de
    // exportación (QA-G12-02): la extensión y el tipo que Garmin registró para el TCX.
    public static class TcxDocument
    {
        public const string Extension = ".tcx";
        public const string MimeType = "application/vnd.garmin.tcx+xml";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

        public static byte[] Encode(string xml)
        {
            if (xml == null)
            {
                throw new ArgumentNullException(nameof(xml));
            }

            return Utf8NoBom.GetBytes(xml);
        }
    }
}
