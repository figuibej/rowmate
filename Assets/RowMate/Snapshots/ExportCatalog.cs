using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RowMate.Presentation.History;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;

namespace RowMate.Snapshots
{
    // Catálogo it12 (12-exportar-tcx/01-po-spec §7 → 02-tech-design §7): CAP-01 a -12 de "Exportar TCX". Cada toma arma su UiTestHost
    // con F-9 en una base TEMPORAL (HistorySeeds, UseHistory), la zona UTC−03:00 fija, el reloj de prueba, español (CAP-10 en inglés
    // con letra 1,3×), los dispositivos de prueba sin la capa del simulador y el FakeFileExporter del anfitrión: muestra
    // Documentos\RowMate\TCX, escribe en una carpeta temporal (que el anfitrión borra), Hold() lo deja en pausa (CAP-04), FailNext lo
    // hace fallar (CAP-07, -08) y "Abrir carpeta" solo se registra (nunca el Explorador). capture-snapshots.ps1 pone además
    // ROWMATE_DATA_DIR en una carpeta temporal. Cada toma controla el texto exacto del aviso, el nombre del archivo, el botón y que
    // ningún texto a la vista tenga una ruta de la PC.
    public static class ExportCatalog
    {
        public const string FileName = "RowMate_2026-11-05_1840_Escalera.tcx";
        public const string Saved = "Guardado en Documentos\\RowMate\\TCX";
        public const string Failed = "No se pudo exportar la sesión";

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();

                // CAP-01 a -03: el botón a la derecha del título.
                foreach (var (id, stem, profile) in new[]
                         {
                             ("CAP-01", "CAP-01-boton-w1280", DeviceProfile.W1280), ("CAP-02", "CAP-02-boton-tv", DeviceProfile.TV),
                             ("CAP-03", "CAP-03-boton-th", DeviceProfile.TH),
                         })
                {
                    var shot = Shot(id, stem, profile, "F-9 n.º 1 (Escalera): el botón Exportar TCX a la derecha del título", s => OpenDetail(s, 0));
                    shot.Checks = h => Expect(h, ("botón a la vista", () => ButtonShown(h)), ("título Escalera", () => Page(h).AppBar.Title.text == "Escalera"),
                        ("a la derecha del título", () => Page(h).AppBar.Title.worldBound.xMax <= h.BoundsOf(ExportTcxButton.ElementName).xMin + 0.5f),
                        ("48 dp", () => Math.Abs(h.BoundsOf(ExportTcxButton.ElementName).width - 48) < 0.6f),
                        ("sin aviso", () => !h.Shell.Toasts.IsVisible));
                    all.Add(shot);
                }

                var cap04 = Shot("CAP-04", "CAP-04-exportando-w1280", DeviceProfile.W1280, "F-9 n.º 1, exportando (destino falso en pausa, +100 ms)",
                    s => Exporting(s));
                cap04.Checks = h => Expect(h, ("indicador de 18 dp", () => Page(h).Export!.Spinner.IsShown && Math.Abs(Page(h).Export!.Spinner.layout.width - 18) < 0.6f),
                    ("sin el ícono", () => !h.IsShown(ExportTcxButton.ElementName) || Page(h).Export!.GlyphView.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None),
                    ("deshabilitado", () => !Page(h).Export!.Button.enabledSelf), ("Exportando…", () => Page(h).Export!.AccessibleName == "Exportando…"),
                    ("sin aviso todavía", () => !h.Shell.Toasts.IsVisible));
                cap04.MotionMayRun = true;
                all.Add(cap04);

                foreach (var (id, stem, profile, state) in new[]
                         {
                             ("CAP-05", "CAP-05-guardado-w1280", DeviceProfile.W1280, "F-9 n.º 1, exportado: el aviso de éxito"),
                             ("CAP-06", "CAP-06-guardado-tv", DeviceProfile.TV, "Igual que CAP-05 en el teléfono vertical: de 16 a 16, en varias líneas"),
                             ("CAP-11", "CAP-11-guardado-l640", DeviceProfile.L640, "Igual que CAP-05 en L640: el aviso entero sobre la barra de pestañas"),
                         })
                {
                    var shot = Shot(id, stem, profile, state, s => Exported(s, null));
                    shot.Checks = h => SavedChecks(h, Saved, "Abrir carpeta");
                    all.Add(shot);
                }

                var cap10 = Shot("CAP-10", "CAP-10-guardado-w1280-en", DeviceProfile.W1280, "Igual que CAP-05 en inglés con letra 1,3×", s => Exported(s, null),
                    "en-US", 1.3);
                cap10.Checks = h => SavedChecks(h, "Saved to Documents\\RowMate\\TCX", "Open folder");
                all.Add(cap10);

                foreach (var (id, stem, profile) in new[] { ("CAP-07", "CAP-07-error-w1280", DeviceProfile.W1280), ("CAP-08", "CAP-08-error-th", DeviceProfile.TH) })
                {
                    var shot = Shot(id, stem, profile, "F-9 n.º 1, error de escritura (destino falso que falla): Reintentar",
                        s => Exported(s, new IOException("disco lleno")));
                    shot.Checks = h => Expect(h, ("No se pudo exportar la sesión", () => h.Shell.Toasts.CurrentAction?.Title == Failed && h.Shell.Toasts.Text.text == Failed),
                        ("Reintentar", () => h.Shell.Toasts.Action.Label.text == "Reintentar"), ("sin detalle", () => !h.IsShown("toast-detail")),
                        ("botón con su ícono", () => ButtonShown(h) && !Page(h).Export!.IsBusy && Page(h).Export!.Button.enabledSelf),
                        ("ningún archivo", () => h.FakeExporter!.Files.Count == 0));
                    all.Add(shot);
                }

                var cap09 = Shot("CAP-09", "CAP-09-sin-datos-tv", DeviceProfile.TV, "F-9 n.º 4 (sin puntos): sin botón", s => OpenDetail(s, 3));
                cap09.Checks = h => Expect(h, ("No hay datos de telemetría", () => h.TextOf("session-no-telemetry-text") == "No hay datos de telemetría"),
                    ("sin botón", () => !h.IsShown(ExportTcxButton.ElementName)));
                all.Add(cap09);

                var cap12 = Shot("CAP-12", "CAP-12-ayuda-w1280", DeviceProfile.W1280, "F-9 n.º 1, el cursor sobre el botón: la ayuda",
                    s => Hovered(s));
                cap12.Checks = h => Expect(h, ("ayuda Exportar TCX", () => h.IsTextVisible("Exportar TCX")), ("botón a la vista", () => ButtonShown(h)));
                all.Add(cap12);

                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        private static SnapshotScenario Shot(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                             string language = "es-AR", double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestDevices,
                LanguageTags = new[] { language },
                TextScale = textScale,
            };
            options.UseHistory(HistorySeeds.F9);
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba" };
        }

        private static IEnumerator OpenDetail(ScenarioScript s, int card)
        {
            yield return s.Tap("tab-history");
            yield return s.Do(h => h.Shell.HistoryScreen!.List.FinishBatches());
            yield return s.Settle();
            yield return s.Tap("history-card-" + card);
            yield return s.Settle();
            yield return s.Do(h =>
            {
                if (Page(h).Chart.Data != null)
                {
                    Page(h).Chart.FinishEntry();
                }
            });
            yield return s.Frames(2);
        }

        // CAP-04: el destino falso en pausa; el toque y 100 ms (el indicador a la vista).
        private static IEnumerator Exporting(ScenarioScript s)
        {
            yield return OpenDetail(s, 0);
            yield return s.Do(h => h.FakeExporter!.Hold());
            yield return s.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => s.Host.FakeExporter!.IsHolding);
            yield return s.Advance(0.1);
            yield return s.Frames(2);
        }

        // CAP-05 a -08, -10, -11: el toque, el pedido al destino falso (que falla si error != null), los 300 ms y el aviso asentado.
        private static IEnumerator Exported(ScenarioScript s, Exception? error)
        {
            yield return OpenDetail(s, 0);
            if (error != null)
            {
                yield return s.Do(h => h.FakeExporter!.FailNext(error));
            }

            yield return s.Tap(ExportTcxButton.ElementName);
            yield return WaitUntil(() => s.Host.FakeExporter!.Requests.Count > 0);
            yield return s.Frames(2);
            yield return s.Advance(0.3);
            yield return WaitUntil(() => s.Host.App.Export.Busy.Value.Count == 0 && s.Host.Shell.Toasts.IsVisible);
            yield return s.Frames(2);
            yield return s.Settle();
        }

        // CAP-12: el cursor sobre el botón (ScenarioScript.Hover).
        private static IEnumerator Hovered(ScenarioScript s)
        {
            yield return OpenDetail(s, 0);
            yield return s.Hover(ExportTcxButton.ElementName);
        }

        private static IEnumerator WaitUntil(Func<bool> done, int maxFrames = 240)
        {
            for (var i = 0; i < maxFrames && !done(); i++)
            {
                yield return null;
            }
        }

        private static IEnumerable<string> SavedChecks(UiTestHost h, string title, string action)
        {
            return Expect(h, ("título " + title, () => h.Shell.Toasts.CurrentAction?.Title == title && h.Shell.Toasts.Text.text == title),
                ("nombre " + FileName, () => h.Shell.Toasts.Detail.text == FileName && h.IsShown("toast-detail")),
                ("acción " + action, () => h.Shell.Toasts.Action.Label.text == action && h.IsShown("toast-action")),
                ("archivo escrito", () => h.FakeExporter!.Files.Count == 1 && h.FakeExporter.Files[0].FileName == FileName),
                ("botón con su ícono", () => ButtonShown(h) && !Page(h).Export!.IsBusy),
                ("el aviso entero en la ventana", () => h.Shell.Toasts.Toast.worldBound.xMin >= -0.5f && h.Shell.Toasts.Toast.worldBound.xMax <= h.Root.worldBound.xMax + 0.5f
                                                       && h.Shell.Toasts.Toast.worldBound.yMin >= -0.5f),
                ("sobre la barra de pestañas", () => h.Find("tab-bar") == null || !h.IsShown("tab-bar")
                                                     || h.Shell.Toasts.Toast.worldBound.yMax <= h.BoundsOf("tab-bar").yMin + 0.5f));
        }

        private static bool ButtonShown(UiTestHost h)
        {
            return Page(h).Export != null && Page(h).Export!.IsShown && h.IsShown(ExportTcxButton.ElementName);
        }

        private static SessionDetailPage Page(UiTestHost h)
        {
            return h.Shell.Pages.Top as SessionDetailPage ?? throw new InvalidOperationException("no hay detalle abierto");
        }

        // Cada toma: además de lo suyo, ningún texto a la vista con una ruta de la PC (repo público).
        private static IEnumerable<string> Expect(UiTestHost h, params (string What, Func<bool> Ok)[] checks)
        {
            var problems = new List<string>();
            foreach (var check in checks)
            {
                bool ok;
                try
                {
                    ok = check.Ok();
                }
                catch (Exception error)
                {
                    problems.Add("no se cumple: " + check.What + " (" + error.GetType().Name + ": " + error.Message + ")");
                    continue;
                }

                if (!ok)
                {
                    problems.Add("no se cumple: " + check.What);
                }
            }

            foreach (var text in UiQuery.VisibleTexts(h.Root))
            {
                if (text.Contains(":\\") || text.Contains("Users") || text.Contains("AppData") || text.Contains(Path.GetTempPath().TrimEnd('\\')))
                {
                    problems.Add("texto con una ruta de la PC: " + text);
                }
            }

            return problems;
        }
    }
}
