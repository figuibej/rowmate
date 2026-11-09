using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Export
{
    // El resultado de una exportación para el aviso (It. 12; 02-tech-design §4, DT12-13): Saved (solo con SaveToFolder: el archivo
    // y su carpeta, AC-STR-09-19) o Failed (siempre, con Reintentar sobre la misma sesión, AC-STR-09-21). Share no avisa.
    public enum TcxExportNoticeKind
    {
        Saved,
        Failed,
    }

    public sealed record TcxExportNotice(TcxExportNoticeKind Kind, WorkoutSession Session, ExportedFile? File);
}
