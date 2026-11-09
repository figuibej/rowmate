using System;
using System.Collections;
using RowMate.Presentation.History;
using RowMate.Tests.PlayMode.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;

namespace RowMate.Tests.PlayMode
{
    // Base de las pruebas de "Exportar TCX" (12-exportar-tcx/02-tech-design §6): el historial F-9 en la base temporal (zona UTC−03:00)
    // y el FakeFileExporter propio del anfitrión (carpeta temporal; "abrir carpeta" solo se registra: nunca el Explorador).
    public abstract class ExportUiTestBase : HistoryUiTestBase
    {
        public const string FileName = "RowMate_2026-11-05_1840_Escalera.tcx";
        public const string SavedTitle = "Guardado en Documentos\\RowMate\\TCX";

        protected FakeFileExporter Exporter => H.FakeExporter ?? throw new InvalidOperationException("sin el exportador falso");

        protected ExportTcxButton Button => Detail.Export ?? throw new InvalidOperationException("el detalle no tiene el botón");

        protected long SessionId => Detail.Store.State.Value.Session.Id ?? throw new InvalidOperationException("la sesión no tiene id");

        // F-9, Historial → la tarjeta n (0 = n.º 1, Escalera).
        protected IEnumerator OpenSession(int card, DeviceProfile? profile = null, Action<UiHostOptions>? configure = null)
        {
            CreateHistory(HistorySeeds.F9, profile ?? DeviceProfile.W1280, o =>
            {
                o.IsDesktop = (profile ?? DeviceProfile.W1280).Code == DeviceProfile.W1280.Code;
                configure?.Invoke(o);
            });
            yield return GoToHistory();
            yield return OpenCard(card);
        }

        // Cuadros reales hasta que se cumpla (los resultados de la base y del exportador vuelven al hilo principal).
        protected static IEnumerator WaitUntil(Func<bool> done, int maxFrames = 120)
        {
            for (var i = 0; i < maxFrames && !done(); i++)
            {
                yield return null;
            }
        }

        // Tocar "Exportar TCX", esperar el pedido al exportador, los 300 ms del indicador y asentar el aviso.
        protected IEnumerator ExportNow(int requestsBefore = 0)
        {
            yield return Frames();
            H.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => Exporter.Requests.Count > requestsBefore);
            yield return Frames();
            yield return FinishBusy();
        }

        // Los 300 ms del indicador (ExportMotionSpecs.MinBusyMs) y el aviso asentado.
        protected IEnumerator FinishBusy()
        {
            H.Advance(0.3);
            yield return WaitUntil(() => H.App.Export.Busy.Value.Count == 0);
            yield return Frames();
            yield return Settle();
        }
    }
}
