using System;
using System.Threading.Tasks;
using RowMate.Core.Ports;

namespace RowMate.App.Export
{
    // Sin exportación (AppServices.Exporter = null: dotnet y las pruebas que no la miran; 02-tech-design §3.5): Delivery None, así
    // el Detalle no muestra el botón. Exportar igual falla (nunca escribe).
    public sealed class NoFileExporter : IFileExporter
    {
        public static readonly NoFileExporter Instance = new NoFileExporter();

        private NoFileExporter()
        {
        }

        public ExportDelivery Delivery => ExportDelivery.None;

        public ExportLocation? Location => null;

        public Task<ExportedFile> ExportAsync(ExportRequest request)
        {
            return Task.FromException<ExportedFile>(new NotSupportedException("Esta plataforma no exporta archivos."));
        }

        public void Reveal(ExportedFile file)
        {
        }
    }
}
