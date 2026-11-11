using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using RowMate.Presentation.Shell;
using RowMate.Core.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it15 (15-deuda-pc/01-po-spec §5 → 02-tech-design §4): la deuda resoluble en la PC. Reglas de la 6a §7.1: reloj de prueba,
    // ROWMATE_DATA_DIR temporal (lo pone capture-snapshots.ps1), F-9 en una base TEMPORAL, FakeFileExporter del anfitrión (nunca el
    // Explorador) y TextAuditProbe en cada toma. Las de HUD y escena (CAP-07, -08, -10 y AUX-15-01) van con la escena REAL en nivel Alto,
    // como it6a. AUX-15-01 es diagnóstico (fuera del INDEX de arte, DT15-17): el mismo cuadro que CAP-10 con el renderer del casco apagado,
    // que se vuelve a prender después de la toma (en Inspect; el anfitrión se descarta igual al terminar).
    public static class DebtCatalog
    {
        private static readonly Regex MinutesSeconds = new Regex(@"^\d{1,2}:\d{2}$");

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();

                // CAP-01: el Historial con el botón de exportar en las tarjetas exportables, Vatios y el split m:ss.
                var cap01 = History("CAP-01", "CAP-01-historial-exportar-tv", DeviceProfile.TV, "Historial: botón de exportar, Vatios y split m:ss", OpenHistory);
                cap01.Checks = h => Audit(h, "Historial",
                    ("botón en la n.º 1", () => h.IsShown("history-card-0-export")),
                    ("sin botón en la n.º 4", () => !h.IsShown("history-card-3-export")),
                    ("Vatios", () => ColumnLabels(h, 0).Contains("Vatios")),
                    ("split m:ss", () => MinutesSeconds.IsMatch(ColumnValue(h, 0, "Split"))),
                    ("el glifo de la fuente", () => h.Shell.HistoryScreen!.List.CardAt(0)!.Export!.GlyphView.Codepoint == Icons.Download));
                all.Add(cap01);

                // CAP-02: exportar desde la tarjeta (en) → "Saved to …" sobre la barra de pestañas.
                var cap02 = History("CAP-02", "CAP-02-tarjeta-guardado-w1280-en", DeviceProfile.W1280, "Exportar desde la tarjeta: Saved to … sobre la barra",
                    s => ExportFromCard(s), "en-US");
                cap02.Checks = h => Audit(h, "Historial",
                    ("el aviso", () => h.Shell.Toasts.CurrentAction != null && h.Shell.Toasts.CurrentAction.Title.StartsWith("Saved to", StringComparison.Ordinal)),
                    ("sobre la barra", () => h.BoundsOf("toast").yMax <= h.BoundsOf("tab-bar").yMin + 0.5f),
                    ("sin Detalle", () => !h.Shell.Pages.HasPages));
                all.Add(cap02);

                // CAP-03: Escalera, fila 2 marcada después de 160 ms (Vatios).
                var cap03 = History("CAP-03", "CAP-03-paso-marcado-tv", DeviceProfile.TV, "Detalle de Escalera: fila 2 marcada y sus franjas al 22 %",
                    s => Steps(OpenDetail(s, 0), MarkRow(s, 1)));
                cap03.Checks = h => Audit(h, "Detalle",
                    ("fila 2 marcada", () => Page(h).StepRows.RowAt(1).ClassListContains(StepBreakdownView.SelectedClass)),
                    ("franjas al 22 %", () => Math.Abs(ChartGeometry.BandAlphaFor(Page(h).Chart.DrawnVisual.StepEmphasis(Page(h).SelectedStep)) - ChartSpecs.BandSelectedAlpha) < 1e-9));
                all.Add(cap03);

                // CAP-04: Split 500m, el cursor fijo en un escalón (la sombra del tooltip) y el resumen m:ss.
                var cap04 = History("CAP-04", "CAP-04-split-cursor-w1280", DeviceProfile.W1280, "Split 500m sin picos, resumen m:ss y la sombra del tooltip",
                    s => Steps(OpenDetail(s, 0), s.ScrollToEnd("session-scroll"), s.Settle(), SplitCursor(s)));
                cap04.Checks = h => Audit(h, "Detalle",
                    ("Split elegido", () => Page(h).Chart.Metric == ChartMetric.Split),
                    ("la sombra del tooltip", () => Page(h).Chart.Cursor.IsShown && Page(h).Chart.Cursor.TooltipShadow.resolvedStyle.opacity > 0.99f),
                    ("resumen m:ss", () => MinutesSeconds.IsMatch(SummaryValue(h, "Split"))));
                all.Add(cap04);

                // CAP-05: la barra del Detalle con el glifo download de la fuente.
                var cap05 = History("CAP-05", "CAP-05-detalle-glifo-w1280", DeviceProfile.W1280, "Barra del Detalle con el glifo download de la fuente",
                    s => OpenDetail(s, 0));
                cap05.CropElement = "session-appbar";
                cap05.Checks = h => Audit(h, "Detalle",
                    ("el glifo de la fuente", () => Page(h).Export != null && Page(h).Export!.GlyphView.Codepoint == Icons.Download),
                    ("del tamaño de la flecha", () => Math.Abs(Page(h).Export!.GlyphView.resolvedStyle.fontSize - Page(h).Back.Icon.resolvedStyle.fontSize) < 0.5f));
                all.Add(cap05);

                // CAP-06: "Abrir carpeta" falla (lanzador y respaldo falsos) → "No se pudo abrir la carpeta".
                var cap06 = History("CAP-06", "CAP-06-abrir-carpeta-fallo-w1280", DeviceProfile.W1280, "Abrir carpeta falla: No se pudo abrir la carpeta",
                    s => OpenFolderFails(s));
                cap06.Checks = h => Audit(h, "Detalle",
                    ("el aviso", () => h.Shell.Toasts.CurrentText == h.App.Texts.Value.Get(L10nKeys.ExportTcxOpenFolderFailed)),
                    ("sin acción", () => h.Shell.Toasts.CurrentAction == null));
                all.Add(cap06);

#if ROWMATE_SIMULATOR
                // CAP-07 y CAP-08: el HUD con la escena real (Vatios / Watts, Split 500m, el tracking de 0,04 em).
                var cap07 = Scene("CAP-07", "CAP-07-hud-l640-13", DeviceProfile.L640, "HUD: Vatios, Split 500m y kcal con 0,04 em, letra 1,3×",
                    s => Free(s, 60.2), "es-AR", 1.3);
                cap07.Inspect = (h, frame, values) => Audit(h, "HUD",
                    ("Vatios", () => h.Immersive!.View.Watts.Label.text == "Vatios"),
                    ("Split 500m", () => h.Immersive!.View.Split.Label.text == "Split 500m"),
                    ("0,04 em", () => h.Immersive!.View.Watts.Label.ClassListContains(RowMate.Core.Text.LabelTracking.MixedClass)),
                    ("sin recortes", () => h.Audit().ClippedTexts.Count == 0));
                all.Add(cap07);
                var cap08 = Scene("CAP-08", "CAP-08-hud-tv-en", DeviceProfile.TV, "HUD en inglés: Watts, Split 500m", s => Free(s, 60.2), "en-US");
                cap08.Inspect = (h, frame, values) => Audit(h, "HUD",
                    ("Watts", () => h.Immersive!.View.Watts.Label.text == "Watts"),
                    ("Split 500m", () => h.Immersive!.View.Split.Label.text == "Split 500m"));
                all.Add(cap08);

                // CAP-09: Rutinas con el simulador: la cinta no tapa el título.
                var cap09 = new SnapshotScenario("CAP-09", "CAP-09-rutinas-simulador-w1280", new UiHostOptions
                {
                    Profile = DeviceProfile.W1280,
                    IsDesktop = true,
                    Backend = HostBackends.Simulator(false),
                    SimulatorLayer = SimulatorLayerMode.Full,
                    LanguageTags = new[] { "es-AR" },
                    Routines = HostRoutines.Seeded(RoutineSeeds.Lista),
                }, "Rutinas con el simulador: la cinta no tapa el título", s => Steps(s.Tap("tab-routines"), s.Settle())) { BackendName = "Sim" };
                // La regla de 44 dp es la USS de WP-D (.rm-has-sim-ribbon .rm-appbar): sin ella, la toma muestra hoy y no la exige.
                cap09.Checks = h => AuditWith(h, "Rutinas", RoutineTexts(h), ("la clase en la raíz del panel", (Func<bool>)(() =>
                    h.Root.panel.visualTree.ClassListContains(RowMate.Simulator.Presentation.SimulatorLayerView.HasRibbonClass))),
                    ("el título fuera de la cinta (si está la USS de WP-D)", (Func<bool>)(() =>
                {
                    if (!UssHas(RowMate.Simulator.Presentation.SimulatorLayerView.HasRibbonClass))
                    {
                        return true;
                    }

                    var title = h.Find("routines-appbar")!.Q<UnityEngine.UIElements.Label>(className: "rm-appbar__title")!.worldBound;
                    return title.xMin + title.yMin > 56.5f;
                })));
                all.Add(cap09);

                // CAP-10: el lago W1280, nivel Alto, como hoy (la referencia del diagnóstico) y AUX-15-01: el mismo cuadro sin el casco.
                var cap10 = Scene("CAP-10", "CAP-10-lago-w1280", DeviceProfile.W1280, "Lago W1280, nivel Alto, como hoy (referencia de AUX-15-01)",
                    s => Free(s, 60.2), "es-AR");
                cap10.Inspect = (h, frame, values) => Audit(h, "HUD", ("la escena", () => h.Scene != null && h.Scene.World != null));
                all.Add(cap10);
                var aux = Scene("AUX-15-01", "AUX-15-01-lago-sin-casco-w1280", DeviceProfile.W1280, "Diagnóstico: el cuadro de CAP-10 con el casco oculto",
                    s => Steps(Free(s, 60.2), s.Do(h => HullOff(h, true)), s.Frames(2)), "es-AR");
                aux.Inspect = (h, frame, values) =>
                {
                    var hidden = HullOff(h, false);
                    values.Add("casco: " + hidden + " renderers apagados durante la toma y prendidos después");
                    return hidden > 0 ? new List<string>() : new List<string> { "no se encontró el renderer del casco" };
                };
                all.Add(aux);
#endif

                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // Las tomas fuera del INDEX de arte (diagnóstico).
        public static readonly string[] Auxiliary = { "AUX-15-01" };

        // Apaga (off) o prende los renderers del casco (los que usan HullMaterial); devuelve cuántos tocó. No toca el agua ni los shaders.
        public static int HullOff(UiTestHost h, bool off)
        {
            var world = h.Scene?.World;
            if (world == null || world.HullMaterial == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var renderer in world.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial == world.HullMaterial)
                {
                    renderer.enabled = !off;
                    count++;
                }
            }

            return count;
        }

        private static SnapshotScenario History(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                                string language = "es-AR")
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestDevices,
                LanguageTags = new[] { language },
            };
            options.UseHistory(HistorySeeds.F9);
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba" };
        }

#if ROWMATE_SIMULATOR
        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator> arrange,
                                              string language, double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code || profile.Code == DeviceProfile.L640.Code,
                Backend = HostBackends.TestRower(RowerScripts.R4),
                LanguageTags = new[] { language },
                Routines = HostRoutines.Seeded(RoutineSeeds.F4A),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                Scene = SceneMode.Real,
                SceneTier = QualityTier.High,
                TextScale = textScale,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "R-4", ExactInstant = true };
        }

        private static IEnumerator Free(ScenarioScript s, double at)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(at);
        }
#endif

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        private static SessionDetailPage Page(UiTestHost h)
        {
            return h.Shell.Pages.Top as SessionDetailPage ?? throw new InvalidOperationException("no hay un Detalle abierto");
        }

        private static List<string> ColumnLabels(UiTestHost h, int card)
        {
            var stats = h.Shell.HistoryScreen!.List.CardAt(card)!.Stats;
            var labels = new List<string>();
            for (var i = 0; i < stats.Count; i++)
            {
                labels.Add(stats.LabelAt(i).text);
            }

            return labels;
        }

        private static string ColumnValue(UiTestHost h, int card, string label)
        {
            var stats = h.Shell.HistoryScreen!.List.CardAt(card)!.Stats;
            for (var i = 0; i < stats.Count; i++)
            {
                if (stats.LabelAt(i).text == label)
                {
                    return stats.ValueAt(i).text;
                }
            }

            return "";
        }

        private static string SummaryValue(UiTestHost h, string label)
        {
            var stats = Page(h).SummaryStats;
            for (var i = 0; i < stats.Count; i++)
            {
                if (stats.LabelAt(i).text == label)
                {
                    return stats.ValueAt(i).text;
                }
            }

            return "";
        }

        private static IEnumerator OpenHistory(ScenarioScript s)
        {
            yield return s.Tap("tab-history");
            yield return s.Do(h => h.Shell.HistoryScreen!.List.FinishBatches());
            yield return s.Settle();
        }

        private static IEnumerator OpenDetail(ScenarioScript s, int card)
        {
            yield return OpenHistory(s);
            yield return s.Tap("history-card-" + card);
            yield return s.Settle();
            yield return s.Do(h =>
            {
                if (h.Shell.Pages.Top is SessionDetailPage page && page.Chart.Data != null)
                {
                    page.Chart.FinishEntry();
                }
            });
            yield return s.Frames(2);
        }

        private static IEnumerator MarkRow(ScenarioScript s, int row)
        {
            yield return s.Do(h => h.Tap("session-step-" + row));
            yield return s.Advance(MotionSpecs.StepHighlightMs / 1000.0 + 0.02);
            yield return s.Settle();
        }

        private static IEnumerator SplitCursor(ScenarioScript s)
        {
            yield return s.Do(h => Page(h).Store.SelectMetric(ChartMetric.Split));
            yield return s.Settle();
            yield return s.Do(h =>
            {
                var page = Page(h);
                page.Chart.FinishEntry();
                // El primer cambio de paso (un escalón): el primer segundo con otro índice.
                var data = page.Chart.Data!;
                var at = data.X[data.Count / 3];
                page.Chart.ShowCursorAt(at);
            });
            yield return s.Frames(3);
        }

        private static IEnumerator ExportFromCard(ScenarioScript s)
        {
            yield return OpenHistory(s);
            yield return s.Do(h => h.Tap("history-card-0-export"));
            for (var i = 0; i < 240 && s.Host.FakeExporter!.Requests.Count == 0; i++)
            {
                yield return null;
            }

            yield return s.Frames(2);
            yield return s.Advance(0.3);
            for (var i = 0; i < 240 && !(s.Host.App.Export.Busy.Value.Count == 0 && s.Host.Shell.Toasts.IsVisible); i++)
            {
                yield return null;
            }

            yield return s.Frames(2);
            yield return s.Settle();
        }

        private static IEnumerator OpenFolderFails(ScenarioScript s)
        {
            yield return OpenDetail(s, 0);
            yield return s.Tap(ExportTcxButton.ElementName);
            for (var i = 0; i < 240 && s.Host.FakeExporter!.Requests.Count == 0; i++)
            {
                yield return null;
            }

            yield return s.Frames(2);
            yield return s.Advance(0.3);
            for (var i = 0; i < 240 && !(s.Host.App.Export.Busy.Value.Count == 0 && s.Host.Shell.Toasts.CurrentAction != null); i++)
            {
                yield return null;
            }

            yield return s.Settle();
            yield return s.Do(h =>
            {
                h.FakeExporter!.FailReveals();
                h.Shell.Toasts.CurrentAction!.OnAction();
            });
            yield return s.Frames(2);
            yield return s.Settle();
        }

        // Alguna USS de Presentation/Styles define el selector (lo de WP-D).
        private static bool UssHas(string selector)
        {
            foreach (var file in System.IO.Directory.GetFiles(System.IO.Path.Combine("Assets", "RowMate", "Presentation", "Styles"), "*.uss"))
            {
                if (System.IO.File.ReadAllText(file).Contains(selector))
                {
                    return true;
                }
            }

            return false;
        }

        // Los nombres y descripciones de las rutinas sembradas (datos del usuario para la auditoría).
        private static string[] RoutineTexts(UiTestHost h)
        {
            var texts = new List<string>();
            foreach (var routine in h.App.Routines.State.Value.Routines)
            {
                texts.Add(routine.Name);
                if (!string.IsNullOrEmpty(routine.Description))
                {
                    texts.Add(routine.Description);
                }
            }

            return texts.ToArray();
        }

        private static IEnumerable<string> Audit(UiTestHost h, string screen, params (string What, Func<bool> Ok)[] checks)
        {
            return AuditWith(h, screen, new string[0], checks);
        }

        // Lo propio de cada toma y la auditoría de textos (D-01), con datos del usuario extra.
        private static IEnumerable<string> AuditWith(UiTestHost h, string screen, string[] userData, params (string What, Func<bool> Ok)[] checks)
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

            var data = new List<string> { "Escalera", "Rutina borrada", ExportCatalog.FileName };
            data.AddRange(userData);
            problems.AddRange(TextAuditProbe.Violations(h, screen, "toma", data.ToArray()));
            return problems;
        }
    }
}
