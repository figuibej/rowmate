namespace RowMate.Core.Ports
{
    // Un archivo ya exportado (It. 12; 02-tech-design §3.1): el nombre (lo que se muestra), la ruta completa (solo para "Abrir
    // carpeta", nunca se muestra ni se registra) y la carpeta en la que quedó.
    public sealed record ExportedFile(string FileName, string FullPath, ExportLocation Location);
}
