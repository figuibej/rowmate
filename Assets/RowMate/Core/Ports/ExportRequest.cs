using System;

namespace RowMate.Core.Ports
{
    // Un pedido de exportación (It. 12; 02-tech-design §3.1, DT12-08): el nombre sin extensión (sin sufijo de colisión), la
    // extensión, el tipo MIME, el título de la hoja de compartir (en el idioma vigente al pedir) y una FUNCIÓN que produce los
    // bytes. El exportador real la corre fuera del hilo principal (con datos inmutables); el falso, en línea. Así ni App ni Core
    // ven hilos (ARCHITECTURE §6).
    public sealed record ExportRequest(string BaseName, string Extension, string MimeType, string ShareTitle, Func<byte[]> Content);
}
