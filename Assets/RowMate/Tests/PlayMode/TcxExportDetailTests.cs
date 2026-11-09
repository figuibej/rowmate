using System.Collections;
using System.IO;
using System.Xml;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.Ports;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // TED · "Exportar TCX" en el Detalle (12-exportar-tcx/02-tech-design §5.1, §6.3; AC-STR-09-10, -11, -17, -18, -23): el botón de la
    // AppBar, cuándo aparece, su ayuda (nombre accesible), el teclado, el indicador de 18/2 y que la exportación sigue aunque se salga
    // del Detalle. Con el FakeFileExporter del anfitrión (carpeta temporal; nunca Documentos ni el Explorador).
    public sealed class TcxExportDetailTests : ExportUiTestBase
    {
        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator ButtonOnlyWhenExportable()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return Frames();
            H.Tap("history-card-0");
            if (Detail.Phase == SessionDetailPhase.Loading)
            {
                Assert.That(Button.IsShown, Is.False, "mientras carga, sin botón");
            }

            yield return Settle();
            Detail.Chart.FinishEntry();
            yield return Frames();
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            Assert.That(Button.IsShown, Is.True, "n.º 1 exportable: el botón");
            Assert.That(H.IsShown(ExportTcxButton.ElementName), Is.True);
            var button = H.BoundsOf(ExportTcxButton.ElementName);
            Assert.That(button.width, Is.EqualTo(48).Within(0.5f), "48 dp de toque");
            Assert.That(button.height, Is.EqualTo(48).Within(0.5f));
            Assert.That(button.xMin, Is.GreaterThan(H.BoundsOf("session-back").xMax), "a la derecha del título");
            Assert.That(Detail.AppBar.Title.worldBound.xMax, Is.LessThanOrEqualTo(button.xMin + 0.5f));
            yield return Tap("session-back");
            yield return OpenCard(3);
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.NoTelemetry));
            Assert.That(Button.IsShown, Is.False, "n.º 4 sin puntos: sin botón (DV-126)");
        }

        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator NoButtonOnLoadError()
        {
            ScriptedSessionRepository? repository = null;
            CreateScriptedHistory(r => repository = r, DeviceProfile.W1280);
            yield return GoToHistory();
            repository!.PointsFailures = 1;
            yield return OpenCard(0);
            Assert.That(Detail.Phase, Is.EqualTo(SessionDetailPhase.LoadFailed));
            Assert.That(Button.IsShown, Is.False, "error de carga: sin botón");
            yield return Tap("session-retry");
            Assert.That(Button.IsShown, Is.True, "con el detalle cargado, el botón");
        }

        [UnityTest, Category("AC-STR-09-10")]
        public IEnumerator NoButtonWithoutTelemetry()
        {
            yield return OpenSession(3, DeviceProfile.TV);
            Assert.That(Detail.Store.State.Value.CanExport, Is.False);
            Assert.That(H.IsShown(ExportTcxButton.ElementName), Is.False);
            Assert.That(H.TextOf("session-no-telemetry-text"), Is.EqualTo("No hay datos de telemetría"));
        }

        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator TooltipIsAccessibleName()
        {
            yield return OpenSession(0);
            Assert.That(Button.AccessibleName, Is.EqualTo("Exportar TCX"));
            H.PointerEnter(ExportTcxButton.ElementName);
            yield return Settle();
            Assert.That(H.IsTextVisible("Exportar TCX"), Is.True, "la ayuda al pasar el cursor");
            H.PointerLeave(ExportTcxButton.ElementName);
            yield return Settle();
        }

        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator TabThenEnterOrSpaceExports()
        {
            yield return OpenSession(0);
            Detail.Back.Focus();
            yield return Frames();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Button.Button), "Tab: de la flecha al botón");
            H.PressEnter();
            yield return WaitUntil(() => Exporter.Requests.Count == 1);
            Assert.That(Exporter.Requests.Count, Is.EqualTo(1), "Enter exporta");
            yield return FinishBusy();
            Button.Button.Focus();
            H.PressKey(KeyCode.Space, ' ');
            yield return WaitUntil(() => Exporter.Requests.Count == 2);
            Assert.That(Exporter.Requests.Count, Is.EqualTo(2), "Espacio exporta");
            yield return FinishBusy();
            Assert.That(Exporter.Files[1].FileName, Is.EqualTo("RowMate_2026-11-05_1840_Escalera_2.tcx"), "no pisa el anterior");
        }

        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator LongTitleEllipsizesBeforeButton()
        {
            yield return OpenSession(0, DeviceProfile.TV);
            Detail.AppBar.SetTitle("Escalera piramidal larguísima de intervalos con descansos activos y final fuerte");
            yield return Frames(3);
            var title = Detail.AppBar.Title;
            Assert.That(title.worldBound.xMax, Is.LessThanOrEqualTo(H.BoundsOf(ExportTcxButton.ElementName).xMin + 0.5f), "el título no tapa el botón");
            Assert.That(title.resolvedStyle.textOverflow, Is.EqualTo(TextOverflow.Ellipsis), "se recorta con …");
            Assert.That(H.BoundsOf(ExportTcxButton.ElementName).xMax, Is.LessThanOrEqualTo(H.Root.worldBound.xMax + 0.5f), "el botón a la vista");
        }

        [UnityTest, Category("AC-STR-09-17")]
        public IEnumerator ShareDeliveryShowsShareGlyph()
        {
            // m-07: la variante de Android (hoja de compartir) usa el glifo de compartir y no avisa el éxito.
            yield return OpenSession(0, DeviceProfile.TV, o => o.FakeExportDelivery = ExportDelivery.Share);
            Assert.That(Button.Delivery, Is.EqualTo(ExportDelivery.Share));
            Assert.That(Button.Glyph, Is.EqualTo(ExportTcxButton.GlyphFor(ExportDelivery.Share)));
            Assert.That(Button.Glyph, Is.Not.EqualTo(ExportTcxButton.GlyphFor(ExportDelivery.SaveToFolder)));
            var shown = H.Shell.Toasts.ShowCount;
            yield return ExportNow();
            Assert.That(Exporter.Files.Count, Is.EqualTo(1));
            Assert.That(H.Shell.Toasts.ShowCount, Is.EqualTo(shown), "Share: sin aviso de éxito");
            Assert.That(Button.IsBusy, Is.False);
        }

        [UnityTest, Category("AC-STR-09-18")]
        public IEnumerator SpinnerEighteenByTwoAndDisabled()
        {
            yield return OpenSession(0);
            Exporter.Hold();
            H.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => Exporter.IsHolding);
            yield return Frames();
            Assert.That(Button.IsBusy, Is.True);
            Assert.That(Button.Spinner.IsShown, Is.True, "el indicador en lugar del ícono");
            Assert.That(Button.Spinner.SizeInDp, Is.EqualTo(18).Within(0.01f));
            Assert.That(Button.Spinner.StrokeInDp, Is.EqualTo(2).Within(0.01f));
            Assert.That(Button.Spinner.layout.width, Is.EqualTo(18).Within(0.5f), "18 dp (el layout: girando, el worldBound es mayor)");
            Assert.That(Button.GlyphView.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Button.Button.enabledSelf, Is.False, "deshabilitado");
            Assert.That(Button.Button.resolvedStyle.opacity, Is.EqualTo(1).Within(0.01f), "el indicador sin el 0,38 de :disabled");
            Assert.That(Button.AccessibleName, Is.EqualTo("Exportando…"));
            H.TapUnchecked(ExportTcxButton.ElementName);
            yield return Frames();
            Assert.That(Exporter.Requests.Count, Is.EqualTo(1), "otro toque no hace nada");
            Exporter.Release();
            yield return Frames();
            Assert.That(Button.IsBusy, Is.True, "el indicador se ve al menos 300 ms");
            yield return FinishBusy();
            Assert.That(Button.IsBusy, Is.False);
            Assert.That(Button.Spinner.IsShown, Is.False);
            Assert.That(Button.GlyphView.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "vuelve el ícono");
            Assert.That(Button.Button.enabledSelf, Is.True);
        }

        [UnityTest, Category("AC-STR-09-18")]
        public IEnumerator BackDuringExportStillNotifies()
        {
            yield return OpenSession(0);
            Exporter.Hold();
            H.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => Exporter.IsHolding);
            yield return Tap("session-back");
            Assert.That(DetailOrNull, Is.Null, "volvió al Historial");
            Exporter.Release();
            yield return Frames();
            yield return FinishBusy();
            Assert.That(H.Shell.Toasts.CurrentAction?.Title, Is.EqualTo(SavedTitle), "el aviso igual aparece");
            Assert.That(Exporter.Files[0].FileName, Is.EqualTo(FileName));
        }

        [UnityTest, Category("AC-STR-09-18")]
        public IEnumerator ReopenShowsBusy()
        {
            yield return OpenSession(0);
            Exporter.Hold();
            H.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => Exporter.IsHolding);
            yield return Tap("session-back");
            yield return OpenCard(0);
            Assert.That(Button.IsBusy, Is.True, "al reabrir, ya aparece ocupado");
            Assert.That(Button.Spinner.IsShown, Is.True);
            Exporter.Release();
            yield return Frames();
            yield return FinishBusy();
            Assert.That(Button.IsBusy, Is.False);
        }

        [UnityTest, Category("AC-STR-09-11")]
        public IEnumerator HistoryUnchangedAfterExport()
        {
            yield return OpenSession(0);
            var beforeTask = H.Sessions.GetHistoryAsync();
            yield return WaitUntil(() => beforeTask.IsCompleted);
            var before = Describe(beforeTask.Result);
            var state = Detail.Store.State.Value;
            yield return ExportNow();
            var afterTask = H.Sessions.GetHistoryAsync();
            yield return WaitUntil(() => afterTask.IsCompleted);
            Assert.That(Describe(afterTask.Result), Is.EqualTo(before), "la base no cambia");
            Assert.That(Detail.Store.State.Value, Is.SameAs(state), "el detalle no se recarga");
            var file = Exporter.Files[0];
            Assert.That(file.FileName, Is.EqualTo(FileName));
            var xml = new XmlDocument();
            xml.Load(file.FullPath);
            var ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("t", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");
            Assert.That(xml.SelectNodes("//t:Lap", ns)!.Count, Is.EqualTo(8), "Escalera de F-9: 8 vueltas");
            Assert.That(xml.SelectSingleNode("//t:Id", ns)!.InnerText, Is.EqualTo("2026-11-05T18:40:00-03:00"));
            Assert.That(File.ReadAllText(file.FullPath), Does.Not.Contain("ROWMATE"), "sin metadatos");
        }

        [UnityTest, Category("AC-STR-09-23")]
        public IEnumerator AllTextsFromCatalog()
        {
            yield return OpenSession(0, DeviceProfile.W1280, o => o.LanguageTags = new[] { "en-US" });
            Assert.That(Button.AccessibleName, Is.EqualTo("Export TCX"));
            yield return ExportNow();
            var toast = H.Shell.Toasts.CurrentAction!;
            Assert.That(toast.Title, Is.EqualTo("Saved to Documents\\RowMate\\TCX"));
            Assert.That(toast.ActionText, Is.EqualTo("Open folder"));
            Assert.That(Exporter.Requests[0].ShareTitle, Is.EqualTo("Share session"), "el título de la hoja en el idioma vigente");
        }

        private static string Describe(System.Collections.Generic.IReadOnlyList<RowMate.Core.Models.HistoryEntry> entries)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var entry in entries)
            {
                parts.Add(entry.Session.Id + "|" + entry.Session.RoutineName + "|" + entry.Session.FinishedAt + "|" + entry.Session.TotalTimeSeconds + "|"
                          + entry.Session.StravaActivityId + "|" + entry.Stats.TotalDistance + "|" + entry.Stats.TotalTimeSeconds);
            }

            return string.Join("\n", parts);
        }
    }
}
