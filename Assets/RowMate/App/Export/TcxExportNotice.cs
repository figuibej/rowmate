using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Export
{
    // El resultado de una exportación para el aviso (It. 12; 02-tech-design §4, DT12-13): Saved (solo con SaveToFolder: el archivo
    // y su carpeta, AC-STR-09-19) o Failed (siempre, con Reintentar sobre la misma sesión, AC-STR-09-21). Share no avisa.
    // It. 15 (#70, AC-D15-13): OpenFolderFailed, cuando "Abrir carpeta" no pudo abrir el Explorador ni con el respaldo: la sesión
    // y el archivo del aviso Saved; 4 s, sin acción (ExportMotionSpecs.OpenFolderFailedMs).
    public enum TcxExportNoticeKind
    {
        Saved,
        Failed,
        OpenFolderFailed,
    }

    public sealed record TcxExportNotice(TcxExportNoticeKind Kind, WorkoutSession Session, ExportedFile? File);
}
