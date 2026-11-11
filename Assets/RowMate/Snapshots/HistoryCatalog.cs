using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Presentation.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;

namespace RowMate.Snapshots
{
    // Catálogo it9 (09-historial/01-po-spec §7 → 02-tech-design §7): CAP-01 a -17, VID-01 y -02 y DEMO-01 a -05 en una secuencia.
    // Cada toma arma su UiTestHost con una base TEMPORAL sembrada por código con HistorySeeds a través de los repositorios reales
    // (F-9, F-9V, F-9P; F-ROTA para CAP-17), el reloj de prueba, la zona UTC−03:00 (UiHostOptions.LocalZone), español (CAP-03 en
    // inglés con letra 1,3× en P360) y sin escena. Las CAP y los VID van con los dispositivos de prueba y sin la capa del simulador
    // (el historial no depende del remo; la llave no tapa ninguna tarjeta); la DEMO, con el simulador sin ruido y su capa. Las
    // líneas de tiempo del gráfico terminan con FinishEntry() (salvo en los videos). Nunca se tocan el prefs.json ni las bases
    // del usuario.
    public static class HistoryCatalog
    {
        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
                AddLists(all);
                AddDetails(all);
                AddVideos(all);
#if ROWMATE_SIMULATOR
                all.Add(Demo());
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // ── CAP-01 a -05 y -17: la lista ──

        private static void AddLists(List<SnapshotScenario> all)
        {
            var cap01 = Shot("CAP-01", "CAP-01-lista-tv", DeviceProfile.TV, HistorySeeds.F9, "F-9, Historial", ToHistory);
            cap01.Checks = h => Expect(
                ("6 tarjetas en orden", () => Names(h) == "Escalera|Libre|Escalera|Libre|Libre|Rutina borrada"),
                ("fechas", () => Card(h, 0).DateLabel.text == "05/11/2026  18:40" && Card(h, 5).DateLabel.text == "01/10/2026  21:05"),
                ("columnas 6/6/6/5/5/6", () => Columns(h) == "6/6/6/5/5/6"),
                ("n.º 1", () => Values(h, 0) == "11:00 2253m 150 24.1 3:00 139/150"),
                ("n.º 2", () => Values(h, 1) == "33:44 7923m 160 22.0 2:10 145/145"),
                ("n.º 3 cortada", () => Values(h, 2) == "03:00 651m 150 24.1 2:30 140/150"),
                ("n.º 4 sin puntos", () => Values(h, 3) == "00:00 0m 0 0.0 --:--"),
                ("n.º 5 sin pulso", () => Values(h, 4) == "1:02:05 13796m 140 20.0 2:15"),
                ("n.º 6", () => Values(h, 5) == "04:00 851m 150 24.1 2:30 136/150"));
            cap01.WithProbe(PixelProbe.Dominant("metric.watts", "history-card-0-stat-2 .rm-stat__value", Tokens.MetricWatts.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("metric.hr", "history-card-0-stat-5 .rm-stat__value", Tokens.MetricHr.Rgb.ToHex(), 3), true);
            all.Add(cap01);

            var cap02 = Shot("CAP-02", "CAP-02-lista-th", DeviceProfile.TH, HistorySeeds.F9, "F-9, Historial en el teléfono horizontal", ToHistory);
            cap02.Checks = h => Expect(("una columna a todo el ancho", () => Math.Abs(Card(h, 0).worldBound.width - (h.Root.worldBound.width - 24)) < 0.6f),
                ("ninguna cifra cortada", () => AllFit(h)));
            all.Add(cap02);

            var cap03 = Shot("CAP-03", "CAP-03-lista-p360-en-130", DeviceProfile.P360, HistorySeeds.F9, "F-9, Historial en inglés con letra 1,3×", ToHistory,
                "en-US", 1.3);
            cap03.Checks = h => Expect(("History", () => h.IsTextVisible("History")), ("Free", () => Card(h, 1).NameLabel.text == "Free"),
                ("Time, Distance, HR", () => Card(h, 0).Stats.LabelAt(0).text == "Time" && Card(h, 0).Stats.LabelAt(1).text == "Distance"
                                               && Card(h, 0).Stats.LabelAt(5).text == "HR"),
                ("1:02:05 achicado", () => Card(h, 4).Stats.FitAt(0).FitScale < 1), ("13796m achicado", () => Card(h, 4).Stats.FitAt(1).FitScale < 1),
                ("139/150 achicado", () => Card(h, 0).Stats.FitAt(5).FitScale < 1), ("sin desborde", () => AllFit(h)));
            cap03.Inspect = (h, image, values) =>
            {
                values.Add("escala de 1:02:05 " + F(Card(h, 4).Stats.FitAt(0).FitScale) + ", de 13796m " + F(Card(h, 4).Stats.FitAt(1).FitScale)
                           + ", de 139/150 " + F(Card(h, 0).Stats.FitAt(5).FitScale));
                return new string[0];
            };
            all.Add(cap03);

            var cap04 = Shot("CAP-04", "CAP-04-lista-w1280", DeviceProfile.W1280, HistorySeeds.F9, "F-9, Historial en el escritorio", ToHistory);
            cap04.Checks = h => Expect(("una columna", () => Math.Abs(Card(h, 0).worldBound.width - (h.Root.worldBound.width - 24)) < 0.6f),
                ("fecha y flecha a la derecha", () => Card(h, 0).DateLabel.worldBound.xMin > h.Root.worldBound.width * 0.8f));
            all.Add(cap04);

            var cap05 = Shot("CAP-05", "CAP-05-vacia-tv", DeviceProfile.TV, HistorySeeds.F9V, "F-9V, Historial", ToHistory);
            cap05.Checks = h => Expect(("el vacío", () => h.IsShown("history-empty") && h.TextOf("history-empty-text") == "No hay sesiones guardadas"),
                ("centrado", () => Math.Abs(h.BoundsOf("history-empty-icon").center.x - h.Root.worldBound.center.x) < 1f));
            all.Add(cap05);

            var rota = new UiHostOptions { Profile = DeviceProfile.TV, IsDesktop = false, Backend = HostBackends.TestDevices, Routines = HostRoutines.Rota() };
            var cap17 = new SnapshotScenario("CAP-17", "CAP-17-error-tv", rota, "F-ROTA, Historial", ToHistory) { BackendName = "Prueba" };
            cap17.Checks = h => Expect(("No se pudo cargar el historial", () => h.TextOf("history-error-text") == "No se pudo cargar el historial"),
                ("Reintentar", () => h.TextOf("history-retry") == "Reintentar"), ("sin indicador", () => !h.IsShown("history-loading")));
            all.Add(cap17);
        }

        // ── CAP-06 a -16: el detalle ──

        private static void AddDetails(List<SnapshotScenario> all)
        {
            var cap06 = Detail("CAP-06", "CAP-06-detalle-libre-tv", DeviceProfile.TV, 1, ChartMetric.Watts, "F-9 n.º 2 (libre), Vatios");
            cap06.Checks = h => Expect(("Entrenamiento libre", () => Page(h).AppBar.Title.text == "Entrenamiento libre"),
                ("sin rendimiento por paso", () => !h.IsShown("session-steps")), ("0 franjas", () => Chart(h).Current!.BandCount == 0),
                ("resumen con 6 columnas", () => Page(h).SummaryStats.Count == 6), ("X 3:44 … 33:44", () => XLabels(h).StartsWith("3:44 5:00", StringComparison.Ordinal)
                                                                                                    && XLabels(h).EndsWith("30:00 33:44", StringComparison.Ordinal)));
            all.Add(cap06);

            var cap07 = Detail("CAP-07", "CAP-07-detalle-escalera-vatios-tv", DeviceProfile.TV, 0, ChartMetric.Watts, "F-9 n.º 1 (Escalera), Vatios");
            cap07.Checks = h => Expect(("8 filas", () => Page(h).StepRows.Count == 8),
                ("fila 1", () => Page(h).StepRows.Line1At(0).text == "1:00  ·  183m" && string.Join(" ", Page(h).StepRows.Line2At(0)) == "100W 20.0 spm 2:30/500m"),
                ("fila 2", () => Page(h).StepRows.Line1At(1).text == "2:00  ·  432m" && string.Join(" ", Page(h).StepRows.Line2At(1)) == "150W 24.1 spm 2:13/500m"),
                ("fila 6", () => Page(h).StepRows.Line1At(5).text == "2:00  ·  433m" && string.Join(" ", Page(h).StepRows.Line2At(5)) == "138W 22.1 spm 2:02/500m"),
                ("fila 8", () => Page(h).StepRows.Line1At(7).text == "1:05  ·  183m"),
                ("8 franjas, 7 separadores, 6 objetivos", () => Chart(h).Current!.BandCount == 8 && Chart(h).Current!.SeparatorCount == 7 && Chart(h).Current!.TargetCount == 6),
                ("Y 0 … 198", () => YLabels(h) == "0 50 100 150 198"),
                ("X 0:05 … 11:00", () => XLabels(h) == "0:05 2:00 4:00 6:00 8:00 10:00 11:00"),
                ("ceros en 425 y 430 s", () => RawAt(h, ChartMetric.Watts, 425) == 0 && RawAt(h, ChartMetric.Watts, 430) == 0));
            cap07.WithProbe(PixelProbe.Point("bg.sunken", "session-chart", new Vector2(0.5f, 0.02f), Tokens.BgSunken.Rgb.ToHex(), 2), true);
            all.Add(cap07);

            var cap08 = Detail("CAP-08", "CAP-08-detalle-spm-tv", DeviceProfile.TV, 0, ChartMetric.Spm, "F-9 n.º 1, SPM, desplazado hasta el gráfico", true);
            cap08.Checks = h => Expect(("objetivos 18/20/22", () => Targets(h) == "18 20 22"), ("Y 0 … 26", () => YLabels(h) == "0 5 10 15 20 25 26"));
            all.Add(cap08);

            var cap09 = Detail("CAP-09", "CAP-09-detalle-split-tv", DeviceProfile.TV, 0, ChartMetric.Split, "F-9 n.º 1, Split 500m, desplazado hasta el gráfico", true);
            cap09.Checks = h => Expect(("Y 3:00 … 2:00", () => YLabels(h) == "3:00 2:50 2:40 2:30 2:20 2:10 2:00"),
                ("ceros al piso", () => PlottedAt(h, ChartMetric.Split, 425) == 0),
                // gate m-05: la y del objetivo es la de su ritmo invertido (no una marca).
                ("objetivos 2:10, 2:08 y 2:06 invertidos", () => Targets(h) == F(180 - 130) + " " + F(180 - 128) + " " + F(180 - 126)));
            all.Add(cap09);

            var cap10 = Detail("CAP-10", "CAP-10-detalle-pulso-tv", DeviceProfile.TV, 0, ChartMetric.HeartRate, "F-9 n.º 1, Pulso, desplazado hasta el gráfico", true);
            cap10.Checks = h => Expect(("color #EF476F", () => Chart(h).Current!.SeriesColor.ToHex() == Tokens.MetricHr.Rgb.ToHex()),
                ("0 objetivos", () => Chart(h).Current!.TargetCount == 0), ("Y 0 … 165", () => YLabels(h) == "0 50 100 150 165"));
            all.Add(cap10);

            var cap11 = Detail("CAP-11", "CAP-11-detalle-distancia-tv", DeviceProfile.TV, 0, ChartMetric.Distance, "F-9 n.º 1, Distancia, desplazado hasta el gráfico", true);
            cap11.Checks = h => Expect(("0 objetivos", () => Chart(h).Current!.TargetCount == 0), ("máximo 2253", () => Math.Abs(Chart(h).Current!.View!.MaxPlotted - 2253) < 0.5));
            all.Add(cap11);

            var cap12 = Detail("CAP-12", "CAP-12-detalle-cortada-tv", DeviceProfile.TV, 2, ChartMetric.Watts, "F-9 n.º 3 (cortada), Vatios");
            cap12.Checks = h => Expect(("2 filas", () => Page(h).StepRows.Count == 2), ("2 franjas", () => Chart(h).Current!.BandCount == 2),
                ("X 0:05 … 3:00", () => XLabels(h).StartsWith("0:05", StringComparison.Ordinal) && XLabels(h).EndsWith("3:00", StringComparison.Ordinal)));
            all.Add(cap12);

            var cap13 = Detail("CAP-13", "CAP-13-detalle-sin-datos-tv", DeviceProfile.TV, 3, null, "F-9 n.º 4 (sin puntos)");
            cap13.Checks = h => Expect(("solo sessionNoTelemetry", () => h.IsShown("session-no-telemetry") && !h.IsShown("session-scroll")
                                                                          && h.TextOf("session-no-telemetry-text") == "No hay datos de telemetría"));
            all.Add(cap13);

            var cap14 = Detail("CAP-14", "CAP-14-detalle-th", DeviceProfile.TH, 0, ChartMetric.Watts, "F-9 n.º 1, Vatios, desplazado hasta el gráfico", true);
            cap14.Checks = h => Expect(("una columna: selector y gráfico a todo el ancho", () =>
                Math.Abs(h.BoundsOf("session-chart").width - h.BoundsOf("session-summary").width) < 0.6f
                && Math.Abs(h.BoundsOf("session-metric").width - h.BoundsOf("session-chart").width) < 0.6f));
            all.Add(cap14);

            var cap15 = Detail("CAP-15", "CAP-15-cursor-w1280", DeviceProfile.W1280, 0, ChartMetric.Split, "F-9 n.º 1, Split 500m, cursor en 300 s", true, 300);
            cap15.Checks = h => Expect(("tooltip 2:13 s", () => Chart(h).Cursor.TooltipText == "2:13 s"),
                ("guía en x(300)", () => Math.Abs(Chart(h).Cursor.GuideX - Chart(h).Current!.PixelX(300)) < 0.5));
            all.Add(cap15);

            var cap16 = Detail("CAP-16", "CAP-16-detalle-rutina-borrada-tv", DeviceProfile.TV, 5, ChartMetric.Watts, "F-9 n.º 6 (Rutina borrada), Vatios");
            cap16.Checks = h => Expect(("Rutina borrada", () => Page(h).AppBar.Title.text == "Rutina borrada"), ("3 filas", () => Page(h).StepRows.Count == 3),
                ("3 franjas", () => Chart(h).Current!.BandCount == 3), ("0 objetivos", () => Chart(h).Current!.TargetCount == 0));
            all.Add(cap16);
        }

        // ── VID-01 y -02 ──

        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Shot("VID-01", "VID-01-recarga-sin-parpadeo", DeviceProfile.TV, HistorySeeds.F9,
                "F-9: Historial → una sesión terminada (el cierre recarga) → dos toques en Historial (10 s)", ToHistory);
            vid01.Kind = SnapshotKind.Video;
            vid01.Video = new VideoSpec(10)
                .At(1, h =>
                {
                    // Como al terminar un entrenamiento: la sesión se guarda y el cierre recarga el historial (DT9-05).
                    HistorySeeds.AddFinished(h.Sessions, HistorySeeds.Local(2026, 11, 6, 9, 0));
                    RowMate.Core.Common.TaskExtensions.Forget(h.App.History.LoadAsync(), h.App.Logger, "DB");
                })
                .At(4.5, h => h.Tap("tab-history"))
                .At(7, h => h.Tap("tab-history"));
            vid01.Video.FrameCheck = (h, t) => h.IsShown("history-loading") ? "el indicador apareció en " + F(t) + " s" : null;
            all.Add(vid01);

            var vid02 = Shot("VID-02", "VID-02-metricas", DeviceProfile.TV, HistorySeeds.F9, "F-9 n.º 1: abrir y pasar por las 5 métricas cada 1,2 s (8 s)",
                ToHistory);
            vid02.Kind = SnapshotKind.Video;
            vid02.Video = new VideoSpec(8)
                .At(0.4, h => h.Tap("history-card-0"))
                // El gráfico a la vista antes de que termine el push (la entrada arranca al final del push).
                .At(0.5, h => h.ScrollTo("session-chart"))
                .At(2.0, h => h.Tap("session-metric-spm"))
                .At(3.2, h => h.Tap("session-metric-split"))
                .At(4.4, h => h.Tap("session-metric-distance"))
                .At(5.6, h => h.Tap("session-metric-hr"))
                .At(6.8, h => h.Tap("session-metric-watts"));
            all.Add(vid02);
        }

#if ROWMATE_SIMULATOR
        // ── DEMO-01 a -05 (spec §1): una secuencia en W1280 con el simulador sobre una base temporal vacía ──

        private static SnapshotScenario Demo()
        {
            var options = new UiHostOptions
            {
                Profile = DeviceProfile.W1280,
                IsDesktop = true,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            options.UseHistory(HistorySeeds.F9V);
            var demo = new SnapshotScenario("DEMO", "DEMO", options, "Secuencia de la demo (spec §1)", DemoScript) { BackendName = "Sim", Kind = SnapshotKind.Sequence };
            demo.Shot("DEMO-01", "DEMO-01-libre-arriba", "Libre ≈ 2 min y Terminar; Historial: arriba, 6 columnas")
                .Shot("DEMO-02", "DEMO-02-demo9-remada", "La rutina Demo 9 (calentamiento 1:00; trabajo 2:00 140-160 W, 24 SPM, 2:13; enfriamiento 1:00) remada entera")
                .Shot("DEMO-03", "DEMO-03-detalle-vatios", "Detalle de Demo 9: resumen, 3 filas y Vatios con franjas y objetivos 140/160")
                .Shot("DEMO-04", "DEMO-04-split-cursor", "Split 500m con el cursor en el trabajo")
                .Shot("DEMO-05", "DEMO-05-cortada", "Demo 9 otra vez, cerrar la app a mitad del trabajo y volver a abrir: la cortada en Historial");
            return demo;
        }

        private static Routine Demo9()
        {
            return RoutineSeeds.Make("Demo 9", new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero),
                RoutineSeeds.Step(StepType.Warmup, dur: 60),
                RoutineSeeds.Step(StepType.Work, dur: 120, wMin: 140, wMax: 160, spm: 24, split: 133),
                RoutineSeeds.Step(StepType.Cooldown, dur: 60));
        }

        private static IEnumerator DemoScript(ScenarioScript s)
        {
            yield return s.TapInView("hrm-search");
            yield return s.TapInView(WorkoutCatalog.HrmRow);
            yield return s.StartFree();
            yield return s.AdvanceTo(120);
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Settle();
            yield return s.Tap("tab-history");
            yield return s.Shot("DEMO-01", h => Expect(("Libre arriba", () => Card(h, 0).NameLabel.text == "Libre"),
                ("6 columnas", () => Card(h, 0).Stats.Count == 6), ("sin indicador", () => !h.IsShown("history-loading"))));

            // La rutina se crea por el repositorio (el editor es de la It. 3 y ya tiene su demo) y se rema entera. Con Rutinas a la vista:
            // la tarjeta nueva crece ahí (con la pestaña oculta, su GrowMotion espera un layout que no llega; para issues).
            yield return s.Tap("tab-routines");
            yield return s.Do(h =>
            {
                h.Routines.Repository.InsertAsync(Demo9()).GetAwaiter().GetResult();
                RowMate.Core.Common.TaskExtensions.Forget(h.App.Routines.LoadAsync(), h.App.Logger, "DB");
            });
            yield return s.Settle();
            yield return s.StartRoutine("Demo 9");
            yield return s.AdvanceTo(250);
            yield return s.Settle();
            yield return s.Do(h =>
            {
                // El diálogo de rutina completada: Terminar.
                if (h.Find("completion-finish") != null)
                {
                    h.Tap("completion-finish");
                }
            });
            yield return s.Settle();
            yield return s.Tap("tab-history");
            yield return s.Shot("DEMO-02", h => Expect(("Demo 9 arriba", () => Card(h, 0).NameLabel.text == "Demo 9"), ("2 sesiones", () => h.Shell.HistoryScreen!.List.Count == 2)));

            yield return s.Tap("history-card-0");
            yield return s.Do(h => Chart(h).FinishEntry());
            yield return s.Frames(2);
            yield return s.Shot("DEMO-03", h => Expect(("3 filas", () => Page(h).StepRows.Count == 3), ("3 franjas", () => Chart(h).Current!.BandCount == 3),
                ("objetivos 140 y 160", () => Targets(h) == "140 160")));

            yield return s.Do(h =>
            {
                h.ScrollTo("session-chart");
                Page(h).Store.SelectMetric(ChartMetric.Split);
                Chart(h).FinishEntry();
            });
            yield return s.Frames(3);
            yield return s.Do(h => Chart(h).ShowCursorAt(Chart(h).Data!.X[0] + 120));
            yield return s.Frames(2);
            yield return s.Shot("DEMO-04", h => Expect(("cursor visible", () => Chart(h).Cursor.IsShown)));

            yield return s.Tap("session-back");
            yield return s.StartRoutine("Demo 9");
            yield return s.AdvanceTo(120);
            yield return s.Reopen();
            yield return s.Tap("tab-history");
            yield return s.Shot("DEMO-05", h => Expect(("la cortada arriba", () => Card(h, 0).NameLabel.text == "Demo 9"), ("3 sesiones", () => h.Shell.HistoryScreen!.List.Count == 3)));
        }
#endif

        // ── Ayudas ──

        private static SnapshotScenario Shot(string id, string stem, DeviceProfile profile, string set, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                             string language = "es-AR", double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = HostBackends.TestDevices,
                LanguageTags = new[] { language },
                TextScale = textScale,
            };
            options.UseHistory(set);
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Prueba" };
        }

        // Un detalle: Historial → la tarjeta n → la métrica (Vatios si es null) → la entrada en su final; scrollToChart lleva el
        // gráfico a la vista y cursorAt pone el cursor en ese segundo.
        private static SnapshotScenario Detail(string id, string stem, DeviceProfile profile, int card, ChartMetric? metric, string state,
                                               bool scrollToChart = false, double? cursorAt = null)
        {
            return Shot(id, stem, profile, HistorySeeds.F9, state, s => OpenDetail(s, card, metric, scrollToChart, cursorAt));
        }

        private static IEnumerator ToHistory(ScenarioScript s)
        {
            yield return s.Tap("tab-history");
            yield return s.Do(h => h.Shell.HistoryScreen!.List.FinishBatches());
            yield return s.Settle();
        }

        private static IEnumerator OpenDetail(ScenarioScript s, int card, ChartMetric? metric, bool scrollToChart, double? cursorAt)
        {
            yield return ToHistory(s);
            yield return s.Tap("history-card-" + card);
            yield return s.Settle();
            if (metric.HasValue)
            {
                yield return s.Do(h => Page(h).Store.SelectMetric(metric.Value));
            }

            yield return s.Do(h =>
            {
                if (Page(h).Chart.Data != null)
                {
                    Page(h).Chart.FinishEntry();
                }
            });
            yield return s.Frames(2);
            if (scrollToChart)
            {
                yield return s.Do(h => h.ScrollTo("session-chart"));
                yield return s.Frames(3);
            }

            if (cursorAt.HasValue)
            {
                yield return s.Do(h => Page(h).Chart.ShowCursorAt(cursorAt.Value));
                yield return s.Frames(2);
            }
        }

        private static HistoryCardView Card(UiTestHost h, int index)
        {
            return h.Shell.HistoryScreen!.List.CardAt(index) ?? throw new InvalidOperationException("no hay tarjeta " + index);
        }

        private static SessionDetailPage Page(UiTestHost h)
        {
            return h.Shell.Pages.Top as SessionDetailPage ?? throw new InvalidOperationException("no hay detalle abierto");
        }

        private static SessionChartView Chart(UiTestHost h)
        {
            return Page(h).Chart;
        }

        private static string Names(UiTestHost h)
        {
            var list = h.Shell.HistoryScreen!.List;
            var names = new List<string>();
            for (var i = 0; i < list.Count; i++)
            {
                names.Add(list.CardAt(i)!.NameLabel.text);
            }

            return string.Join("|", names);
        }

        private static string Columns(UiTestHost h)
        {
            var list = h.Shell.HistoryScreen!.List;
            var counts = new List<string>();
            for (var i = 0; i < list.Count; i++)
            {
                counts.Add(list.CardAt(i)!.Stats.Count.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join("/", counts);
        }

        private static string Values(UiTestHost h, int index)
        {
            var stats = Card(h, index).Stats;
            var values = new List<string>();
            for (var i = 0; i < stats.Count; i++)
            {
                values.Add(stats.ValueAt(i).text);
            }

            return string.Join(" ", values);
        }

        private static bool AllFit(UiTestHost h)
        {
            var list = h.Shell.HistoryScreen!.List;
            for (var i = 0; i < list.Count; i++)
            {
                var stats = list.CardAt(i)!.Stats;
                for (var c = 0; c < stats.Count; c++)
                {
                    if (!stats.FitAt(c).Fits)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string YLabels(UiTestHost h)
        {
            return string.Join(" ", Chart(h).Labels.YTexts());
        }

        private static string XLabels(UiTestHost h)
        {
            return string.Join(" ", Chart(h).Labels.XTexts());
        }

        // Las y (en valor dibujado) de los objetivos de la métrica a la vista, sin repetir, en orden.
        private static string Targets(UiTestHost h)
        {
            var targets = Chart(h).Current!.View!.Targets;
            var values = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var text = F(targets[i].Y);
                if (!values.Contains(text))
                {
                    values.Add(text);
                }
            }

            return string.Join(" ", values);
        }

        private static double RawAt(UiTestHost h, ChartMetric metric, double seconds)
        {
            var data = Chart(h).Data!;
            return data.Raw(metric)[ChartHitTest.Nearest(data.X, data.Count, seconds)];
        }

        private static double PlottedAt(UiTestHost h, ChartMetric metric, double seconds)
        {
            var data = Chart(h).Data!;
            return data.Plotted(metric)[ChartHitTest.Nearest(data.X, data.Count, seconds)];
        }

        private static string F(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> Expect(params (string What, Func<bool> Ok)[] checks)
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

            return problems;
        }
    }
}
