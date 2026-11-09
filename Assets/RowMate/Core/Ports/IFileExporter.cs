using System.Threading.Tasks;

namespace RowMate.Core.Ports
{
    // Exportar un archivo a la plataforma (It. 12; 02-tech-design §3.1, DT12-07). Un solo puerto: WindowsFileExporter (carpeta
    // Documentos\RowMate\TCX y Explorador), AndroidFileExporter (caché y hoja de compartir) y FakeFileExporter (pruebas y arnés:
    // carpeta temporal, "abrir carpeta" solo se registra).
    public interface IFileExporter
    {
        ExportDelivery Delivery { get; }

        // La carpeta en la que escribe (null si no exporta). La usa el registro para cambiar la ruta real por su etiqueta.
        ExportLocation? Location { get; }

        // Corre Content fuera del hilo principal (el falso: en línea), escribe sin pisar ni dejar restos y devuelve el archivo.
        // En Share, además abre la hoja de compartir (en el hilo principal, después del await). Cualquier excepción = falla.
        Task<ExportedFile> ExportAsync(ExportRequest request);

        // Explorador con el archivo seleccionado; si ya no está, la carpeta (AC-STR-09-20). Sin efecto si no es SaveToFolder.
        void Reveal(ExportedFile file);
    }
}
