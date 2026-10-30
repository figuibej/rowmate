using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it4a (04a-motor-hud-funcional/01-po-spec §7.3-§7.5 → 02-tech-design §7.3; aspecto: 04-design-notes §8): 30 CAP
    // (CAP-27 con a, b y c: 32 fotos), DEMO-01…14 en una sola secuencia y VID-01…03 = 49 archivos. Salvo que diga otra cosa:
    // modo simulador sin ruido con la capa completa (150 W y 24 spm dan 02:13), español (es-AR), el pulsómetro simulado sin
    // conectar, el reloj de prueba desde 2026-10-28T13:00:00Z, teléfono en TV, TH y L640 y escritorio en W1280, y una base de
    // prueba propia (TempDatabase, ejecutor en línea) con las rutinas y las sesiones en el mismo archivo: ninguna toma abre
    // rower_app.db ni rower_app_dev.db (AC-HIS-20-04). "Libre a los N s" = Entrenamiento libre y N s desde el toque;
    // "rutina R a los N s" = la tarjeta, Iniciar y N s desde el toque (ScenarioScript.StartFree / StartRoutine / AdvanceTo).
    // Las comprobaciones hacen fallar la toma; en cada foto fija el arnés corre además WrenchAudit y, en el inmersivo,
    // HudOverlap. Las sondas de aspecto (aspect: true) se anotan para WP-D sin hacerla fallar; las que no lo son (el vidrio y
    // los valores de CAP-12, AC-APP-06-01) sí.
    public static class WorkoutCatalog
    {
        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddRest(all);
                AddHud(all);
                AddRoutineHud(all);
                AddDialogsAndToasts(all);
                all.Add(Demo());
                AddVideos(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                MarkSuperseded(all);
                return all;
            }
        }

        // Archivos que salen (las secuencias cuentan una por toma).
        public static int FileCount
        {
            get
            {
                var count = 0;
                foreach (var scenario in All)
                {
                    count += scenario.Kind == SnapshotKind.Sequence ? scenario.SequenceShots.Count : 1;
                }

                return count;
            }
        }

        public const string HrmRow = "hrm-device-SIM:HRM";

        // It. 4b (04b-hud-exr/02-tech-design §5.6; DT4b-07): las tomas del HUD de it4a (CAP-12 a CAP-30, la secuencia DEMO-01 a -14 y
        // VID-01 a -03) muestran la barra de 40 sin píldoras ni reloj, el aviso workoutCompleted y la salida sin la última foto: con
        // la 4b ya no son la app. Quedan REEMPLAZADAS por las de it4b: no se vuelven a tomar y sus archivos quedan como el registro
        // de la 4a. CAP-01 a CAP-11 (Workout en reposo y el diálogo de inicio) se siguen tomando.
        public const string SupersedingSet = "it4b";

        private static void MarkSuperseded(List<SnapshotScenario> all)
        {
            foreach (var scenario in all)
            {
                var id = scenario.Id;
                if (id.StartsWith("DEMO", StringComparison.Ordinal))
                {
                    scenario.SupersededBy = SupersedingSet;
                    scenario.SupersededIds = "DEMO-01 a DEMO-12";
                }
                else if (id.StartsWith("VID", StringComparison.Ordinal))
                {
                    scenario.SupersededBy = SupersedingSet;
                    scenario.SupersededIds = "VID-01 a VID-08";
                }
                else if (id.StartsWith("CAP-", StringComparison.Ordinal) && id.Length >= 6
                         && int.TryParse(id.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= 12)
                {
                    scenario.SupersededBy = SupersedingSet;
                    scenario.SupersededIds = number <= 18 ? "CAP-13 a CAP-18" : "CAP-01 a CAP-31";
                }
            }
        }

#if ROWMATE_SIMULATOR
        // ── Workout en reposo y diálogo de inicio (CAP-01 a CAP-11; F-WKT-12, DV-21, DV-22) ──
        private static void AddRest(List<SnapshotScenario> all)
        {
            var cap01 = Wk("CAP-01", "CAP-01-workout-lista", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Workout con F-LISTA",
                s => s.Tap("tab-workout"));
            cap01.Checks = h => Concat(RestChecks(h, "Entrenamiento", "Entrenamiento libre", "  O elegí una rutina:",
                new[] { "1 serie · 1 descanso", "2 series · 1 descanso", "1 paso", "4 series · 3 descansos" }), Expect(
                ("#sim-open abajo a la izquierda (xMin 16 ± 1, 16 sobre la barra ± 1)", () => Near(X(h, "sim-open").xMin, 16, 1)
                                                                                            && Near(h.BoundsOf("tab-bar").yMin - h.BoundsOf("sim-open").yMax, 16, 1)),
                ("pestaña 1 elegida", () => h.App.Shell.SelectedIndex.Value == 1)));
            RestProbes(cap01);
            all.Add(cap01);

            var cap02 = Wk("CAP-02", "CAP-02-workout-sin-remo", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Dispositivo → Desconectar; Workout", s => Steps(s.TapInView("device-disconnect"), s.Tap("tab-workout")));
            cap02.Checks = h => Expect(
                ("#workout-banner con warning", () => h.IsShown("workout-banner") && (h.Find("workout-banner-icon") as IconView)?.Codepoint == Icons.Warning),
                ("el texto del aviso", () => h.TextOf("workout-banner-text") == "Conectá el rower para iniciar un entrenamiento"),
                ("#workout-free al 0,38", () => Near(h.Find("workout-free")!.resolvedStyle.opacity, 0.38f, 0.01f)),
                ("#workout-free sin responder", () => !h.Find("workout-free")!.enabledInHierarchy),
                ("#workout-routines al 0,5", () => Near(h.Find("workout-routines")!.resolvedStyle.opacity, 0.5f, 0.01f)),
                ("las tarjetas sin responder", () => h.WhyNotTappable("workout-routine-0") != null));
            cap02.WithProbe(PixelProbe.Point("aviso warning @12 %", "workout-banner", new Vector2(0.97f, 0.2f), PixelProbe.Composite(Tokens.StateWarningBg, Tokens.BgApp), 2), true)
                .WithProbe(PixelProbe.Dominant("texto del aviso", "workout-banner-text", Tokens.StateWarning.Rgb.ToHex(), 3), true);
            all.Add(cap02);

            var cap03 = Wk("CAP-03", "CAP-03-workout-sin-rutinas", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "Workout con F-VACIA",
                s => s.Tap("tab-workout"));
            cap03.Checks = h => Expect(
                ("sin etiqueta", () => !h.IsShown("workout-routines-label")),
                ("sin tarjetas", () => W(h).List.Count == 0),
                ("#workout-empty con su texto", () => h.TextOf("workout-empty") == "Creá una rutina en la pestaña \"Rutinas\""),
                ("#workout-empty centrado en x ± 1", () => Near(h.BoundsOf("workout-empty").center.x, h.Root.worldBound.center.x, 1)),
                ("alfa del texto ≥ 0,54", () => h.Find("workout-empty")!.resolvedStyle.color.a >= 0.54f - 0.005f));
            cap03.WithProbe(PixelProbe.Dominant("texto text.tertiary", "workout-empty", PixelProbe.Composite(Tokens.TextTertiary, Tokens.BgApp), 3), true);
            all.Add(cap03);

            var cap04 = Wk("CAP-04", "CAP-04-workout-lista-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Como CAP-01, en inglés",
                s => s.Tap("tab-workout"), "en-US");
            cap04.Checks = h => RestChecks(h, "Workout", "Free workout", "  Or pick a routine:",
                new[] { "1 interval · 1 rest", "2 intervals · 1 rest", "1 step", "4 intervals · 3 rests" });
            RestProbes(cap04);
            all.Add(cap04);

            var cap05 = Wk("CAP-05", "CAP-05-workout-l640-final", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.ListaPlus(26)),
                "F-LISTA + 26, letra 1,3, sin remo, desplazada al final",
                s => Steps(s.TapInView("device-disconnect"), s.Tap("tab-workout"), s.ScrollToEnd("workout-scroll"), s.Settle()), textScale: 1.3);
            cap05.Checks = h => Expect(
                ("30 tarjetas", () => W(h).List.Count == 30),
                ("la última entera", () => Inside(h, "workout-routine-29")),
                ("la última por encima de la barra", () => h.BoundsOf("workout-routine-29").yMax <= h.BoundsOf("tab-bar").yMin + 0.5f),
                ("la última 16 ± 1 sobre la llave (o más)", () => h.BoundsOf("workout-routine-29").yMax <= h.BoundsOf("sim-open").yMin - 16 + 1));
            all.Add(cap05);

            var cap06 = Wk("CAP-06", "CAP-06-workout-cargando", DeviceProfile.TV,
                HostRoutines.Scripted(r => r.LoadDelay = ScriptedRoutineRepository.Never), "Workout con la base lenta, después de la gracia y el fundido",
                s => Steps(s.Tap("tab-workout"), s.Advance(0.3)));
            cap06.Checks = h => Expect(
                ("#workout-free", () => h.IsShown("workout-free")),
                ("solo #workout-loading", () => h.IsShown("workout-loading") && !h.IsShown("workout-error") && !h.IsShown("workout-routines-label")),
                ("indicador entero", () => h.Find("workout-loading")!.resolvedStyle.opacity > 0.99f),
                ("nunca #workout-empty", () => !h.IsShown("workout-empty")));
            cap06.WithProbe(PixelProbe.Dominant("indicador accent.primary", "workout-loading", Tokens.AccentPrimary.Rgb.ToHex(), 3), true);
            all.Add(cap06);

            var cap07 = Wk("CAP-07", "CAP-07-workout-error", DeviceProfile.TV, HostRoutines.Rota(), "Workout con F-ROTA", s => s.Tap("tab-workout"));
            cap07.Checks = h => Expect(
                ("#workout-error", () => h.IsShown("workout-error")),
                ("No se pudieron cargar las rutinas", () => h.IsTextVisible("No se pudieron cargar las rutinas")),
                ("#workout-retry debajo de #workout-free", () => h.IsShown("workout-retry") && h.BoundsOf("workout-retry").yMin >= h.BoundsOf("workout-free").yMax),
                ("#workout-free habilitado", () => h.WhyNotTappable("workout-free") == null));
            cap07.WithProbe(PixelProbe.Dominant("ícono state.dangerText", "workout-error-icon", Tokens.StateDangerText.Rgb.ToHex(), 3), true);
            all.Add(cap07);

            var cap08 = Wk("CAP-08", "CAP-08-dialogo-de-inicio", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Tocar Escalera SPM",
                s => Steps(s.Tap("tab-workout"), s.Tap("workout-routine-0")));
            cap08.Checks = h => Expect(
                ("workout-start-dialog", () => h.TopDialog?.Name == StartWorkoutDialog.DialogName),
                ("título Escalera SPM", () => h.TextOf("dialog-title") == "Escalera SPM"),
                ("contenido", () => h.TextOf("dialog-message") == "1 serie · 1 descanso\n\n¿Iniciar el entrenamiento?"),
                ("Cancelar de texto", () => h.Find("dialog-cancel")!.ClassListContains("rm-button--text") && h.TextOf("dialog-cancel") == "Cancelar"),
                ("Iniciar relleno", () => h.Find("dialog-confirm")!.ClassListContains("rm-button--primary") && h.TextOf("dialog-confirm") == "Iniciar"),
                ("ancho 331 ± 1", () => Near(h.TopDialog!.Box.worldBound.width, 331, 1)),
                ("#sim-open tocable sobre el velo", () => h.WhyNotTappable("sim-open") == null));
            cap08.WithProbe(PixelProbe.Point("diálogo surface.overlay", "workout-start-dialog .rm-dialog", new Vector2(0.03f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("Iniciar accent.primary", "dialog-confirm", new Vector2(0.08f, 0.5f), Tokens.AccentPrimary.Rgb.ToHex(), 1), true);
            all.Add(cap08);

            var cap09 = Wk("CAP-09", "CAP-09-rutina-que-no-inicia", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Rara4a("Serie enorme")),
                "Tocar Serie enorme (F-RARA)", s => Steps(s.Tap("tab-workout"), s.Tap("workout-routine-0")));
            cap09.Checks = h => Expect(
                ("sin diálogo", () => h.TopDialog == null),
                ("el aviso", () => h.Shell.Toasts.CurrentText == "Esta rutina no se puede iniciar: revisá sus pasos en Rutinas"),
                ("el aviso entero", () => Inside(h, "toast")),
                ("sin cruzarse con #sim-open", () => !Overlap(h.BoundsOf("toast"), h.BoundsOf("sim-open"))),
                ("8 dp sobre la llave ± 1", () => Near(h.BoundsOf("toast").yMax + 8, h.BoundsOf("sim-open").yMin, 1)),
                ("ninguna fila en workout_sessions", () => SessionCount(h) == 0));
            cap09.WithProbe(PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.02f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 1), true);
            all.Add(cap09);

            var cap10 = Wk("CAP-10", "CAP-10-workout-w1280", DeviceProfile.W1280, HostRoutines.Seeded(RoutineSeeds.Lista), "Como CAP-01, en el escritorio",
                s => s.Tap("tab-workout"));
            cap10.Checks = h => Concat(RestChecks(h, "Entrenamiento", "Entrenamiento libre", "  O elegí una rutina:",
                new[] { "1 serie · 1 descanso", "2 series · 1 descanso", "1 paso", "4 series · 3 descansos" }), Expect(
                ("botón de 1248 ± 1", () => Near(h.BoundsOf("workout-free").width, 1248, 1)),
                ("tarjetas de 1248 ± 1", () => Near(h.BoundsOf("workout-routine-0").width, 1248, 1))));
            RestProbes(cap10);
            all.Add(cap10);

            var cap11 = Wk("CAP-11", "CAP-11-workout-letra-130", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Workout con letra 1,3",
                s => s.Tap("tab-workout"), textScale: 1.3);
            cap11.Checks = h => Expect(("clase 1,3", () => h.Shell.ClassListContains("rm-text-scale-130")), ("4 tarjetas", () => W(h).List.Count == 4));
            all.Add(cap11);
        }

        // ── El inmersivo libre (CAP-12 a CAP-17; F-WKT-13, -16, -17, -20; AC-SCN-01-01) ──
        private static void AddHud(List<SnapshotScenario> all)
        {
            var cap12 = Wk("CAP-12", "CAP-12-libre-tv", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Libre a los 42 s",
                s => Steps(s.StartFree(), s.AtSecond(42)));
            cap12.Checks = h => Concat(FreeChecks(h, false), Expect(
                ("#hud-free-title con rowing", () => Hud(h).Free != null && Hud(h).Free!.Icon.Codepoint == Icons.Rowing),
                ("24.0", () => h.TextOf("hud-spm-value") == "24.0"),
                ("02:13", () => h.TextOf("hud-split-value") == "02:13"),
                ("150 W", () => h.TextOf("hud-watts-value") == "150" && Hud(h).Watts.UnitText == "W"),
                ("00:42", () => h.TextOf("hud-time-value") == "00:42"),
                ("sin #hud-bpm", () => !h.IsShown("hud-bpm")),
                ("cielo arriba del 40 %", () => Near((float)Zones(h).Horizon, 0.40f, 0.001f)),
                ("#sim-ribbon sobre el comienzo de la barra", () => Overlap(h.BoundsOf("sim-ribbon"), h.BoundsOf("hud-topbar"))),
                ("la llave en su lugar (355-395 · 575,9-615,9 ± 1)", () => RectIs(h, "sim-open", 355.0, 395.0, 575.9, 615.9))));
            HudProbes(cap12, DeviceProfile.TV, false);
            // 04-design-notes §8.2: los rótulos en el color base, la m al 80 % sobre el vidrio y el agua, y el contorno de Pausa
            // (border.strong sobre el vidrio y el agua).
            var glassWater = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropWater);
            cap12.WithProbe(PixelProbe.Dominant("rótulo SPM metric.spm", "hud-spm-label", Tokens.MetricSpm.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("rótulo Split metric.split", "hud-split-label", Tokens.MetricSplit.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("rótulo Watts metric.watts", "hud-watts-label", Tokens.MetricWatts.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("rótulo kcal metric.kcal", "hud-kcal-label", Tokens.MetricKcal.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.DominantIn("m metric.distance.hud @80 %", "hud-distance-value", PixelProbe.SuffixOf, Over(Tokens.MetricDistanceHud, 0.8, glassWater), 3), true)
                .WithProbe(PixelProbe.Point("contorno de Pausa border.strong", "hud-pause", new Vector2(0.5f, 0.004f), Over(Tokens.TextPrimary, 0.24, glassWater), 6), true)
                .WithProbe(DividerProbe(), true);
            all.Add(cap12);

            var cap13 = Wk("CAP-13", "CAP-13-libre-con-pulso", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Pulsómetro simulado conectado; libre a los 42 s", s => Steps(ConnectHrm(s), s.StartFree(), s.AtSecond(42)));
            cap13.Checks = h => Concat(FreeChecks(h, true), Expect(
                ("#hud-bpm con favorite", () => h.IsShown("hud-bpm") && Hud(h).Bpm.Heart != null && Hud(h).Bpm.Heart!.Codepoint == Icons.Favorite),
                ("la llave en el mismo lugar que en CAP-12", () => RectIs(h, "sim-open", 355.0, 395.0, 575.9, 615.9))));
            cap13.WithProbe(PixelProbe.Dominant("BPM metric.hr.hud", "hud-bpm-value", Tokens.MetricHrHud.Rgb.ToHex(), 2), true);
            all.Add(cap13);

            string? pausedAt = null;
            var cap14 = Wk("CAP-14", "CAP-14-pausa", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "CAP-13, Pausa y 3 s",
                s => Steps(ConnectHrm(s), s.StartFree(), s.AdvanceTo(42), s.Tap("hud-pause"), s.Do(h => pausedAt = h.TextOf("hud-time-value")), s.Advance(3)));
            cap14.Checks = h => Expect(
                ("#hud-resume en lugar de #hud-pause", () => h.IsShown("hud-resume") && !h.IsShown("hud-pause")),
                ("Reanudar state.success", () => Hud(h).Controls.Resume.ClassListContains("rm-button--hud-success")),
                ("#hud-time igual que al pausar", () => pausedAt != null && h.TextOf("hud-time-value") == pausedAt));
            cap14.WithProbe(PixelProbe.Point("Reanudar state.success", "hud-resume", new Vector2(0.06f, 0.5f), Tokens.StateSuccess.Rgb.ToHex(), 2), true)
                .WithProbe(PixelProbe.Dominant("texto de Reanudar text.onAccent", "hud-resume", Tokens.TextOnAccent.Rgb.ToHex(), 3), true);
            all.Add(cap14);

            var cap15 = Wk("CAP-15", "CAP-15-libre-th", DeviceProfile.TH, HostRoutines.Seeded(RoutineSeeds.Lista), "Como CAP-13 en el teléfono horizontal",
                s => Steps(ConnectHrm(s), s.StartFree(), s.AtSecond(42)));
            cap15.Checks = h => Concat(BoatZoneFree(h), Expect(
                ("la banda compacta", () => Hud(h).Band.IsCompact),
                ("BPM y kcal en fila, kcal en el rincón", () => Hud(h).VitalsInRow && h.BoundsOf("hud-kcal").xMin >= h.BoundsOf("hud-bpm").xMax),
                ("la llave a la derecha de Pausa", () => h.BoundsOf("sim-open").xMin >= h.BoundsOf("hud-pause").xMax + 8 - 1)),
                // Los rectángulos de 04-design-notes §3.8: los controles (de Terminar a Pausa), la llave, kcal y BPM con sus mínimos,
                // la barra y, desde WP-B0 (QA-V4a-06, las cifras de RowMate Numeric SC), también Split.
                ControlsRect(h, 303.1, 610.9, 335.1, 394.1),
                Rects(h, ("sim-open", 618.9, 658.9, 344.6, 384.6), ("hud-kcal", 798.1, 899.2, 327.1, 394.1), ("hud-bpm", 688.5, 789.6, 327.1, 394.1),
                    ("hud-topbar", 12.6, 901.4, 8.4, 50.6), ("hud-split", 758.3, 899.2, 94.8, 180.1)),
                Expect(
                    // La banda de §3.8 (14,8-242,5) es la de `00:42 │ 1048 m`; acá el simulador lleva 3 cifras de distancia: la
                    // misma esquina y el mismo alto, con el ancho que calcula HudGeometry para lo que muestra.
                    ("la banda en 14,8 · 318,3-394,1 (± 1), del ancho de HudGeometry", () => Near(X(h, "hud-band").xMin, 14.8f, 1) && Near(X(h, "hud-band").yMin, 318.3f, 1)
                        && Near(X(h, "hud-band").yMax, 394.1f, 1) && Near(X(h, "hud-band").width, (float)Zones(h).Band.Width, 1)),
                    // DD4a-07: el reloj en celdas tabulares de la derivada 700 (cuatro cifras de 0,521 em y el `:` de 0,32 em, a 34).
                    ("00:42 en celdas de RowMate Numeric SC (± 0,6 dp base)", () => h.TextOf("hud-time-value") == "00:42"
                        && Near(X(h, "hud-time-value").width / (float)Zones(h).Scale, (4 * 0.521f + 0.32f) * HudBand.ValueSizeDp, 0.6f))));
            cap15.WithProbe(DividerProbe(), true);
            all.Add(cap15);

            var cap16 = Wk("CAP-16", "CAP-16-valores-largos-l640", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.Empty), "Remo de prueba (Long); libre a los 3725 s",
                s => Steps(s.StartFree(), s.AtSecond(3725)));
            cap16.Host.Backend = HostBackends.TestRower(RowerScripts.Long);
            cap16.Host.SimulatorLayer = SimulatorLayerMode.ChromeOnly;
            cap16.Host.Sessions = HostSessions.InMemory;
            cap16.Host.BoatZoneOutline = true;
            cap16.BackendName = "Prueba (Long)";
            cap16.Checks = h => Concat(BoatZoneFree(h), Expect(
                ("1:02:05", () => h.TextOf("hud-time-value") == "1:02:05"),
                ("123456 m", () => h.TextOf("hud-distance-value") == "123456" && Hud(h).Band.UnitText == "m"),
                ("kcal 9999", () => h.TextOf("hud-kcal-value") == "9999"),
                ("BPM 188", () => h.TextOf("hud-bpm-value") == "188"),
                ("SPM 40.5", () => h.TextOf("hud-spm-value") == "40.5"),
                // 0,561 en 04-design-notes §3.7, con las cifras de RowMate Numeric SC (WP-B0, QA-V4a-06), y la que calculó HudGeometry.
                ("la banda a escala 0,561 (± 0,005), la de HudGeometry", () => Near(Hud(h).Band.Fit, 0.561f, 0.005f) && Near(Hud(h).Band.Fit, (float)Zones(h).ValueScale(HudValue.Band), 0.001f)),
                ("kcal y BPM a ancho de contenido", () => Zones(h).VitalsInRow && !Zones(h).VitalsMinWidth),
                ("#hud-boat-zone a la vista", () => h.IsShown("hud-boat-zone")),
                ("la llave sobre Pausa (414,8-454,8 · 245,5-285,5 ± 1)", () => RectIs(h, "sim-open", 414.8, 454.8, 245.5, 285.5))));
            cap16.WithProbe(PixelProbe.Point("contorno dev.simulator", "hud-boat-zone", new Vector2(0.5f, 0.002f), Tokens.DevSimulator.Rgb.ToHex(), 3), true);
            all.Add(cap16);

            var cap17 = Wk("CAP-17", "CAP-17-libre-w1280", DeviceProfile.W1280, HostRoutines.Seeded(RoutineSeeds.Lista), "Como CAP-13 en el escritorio",
                s => Steps(ConnectHrm(s), s.StartFree(), s.AtSecond(42)));
            cap17.Checks = h => Concat(BoatZoneFree(h), Expect(
                ("escala 1,6", () => Near((float)Zones(h).Scale, 1.6f, 0.001f)),
                ("la caja del valor de SPM 102,4 ± 1", () => Near(h.BoundsOf("hud-spm-value").height, 102.4f, 1)),
                ("kcal y BPM en fila, de 153,6 ± 1", () => Hud(h).VitalsInRow && Near(h.BoundsOf("hud-kcal").width, 153.6f, 1) && Near(h.BoundsOf("hud-bpm").width, 153.6f, 1)),
                ("la llave a la derecha de Pausa (881,6-921,6 · 629,6-669,6 ± 1)", () => RectIs(h, "sim-open", 881.6, 921.6, 629.6, 669.6)),
                ("la banda compacta, sin achicar", () => Hud(h).Band.IsCompact && Near(Hud(h).Band.Fit, 1, 0.001f))));
            HudProbes(cap17, DeviceProfile.W1280, true);
            all.Add(cap17);
        }

        // ── El inmersivo con rutina (CAP-18 a CAP-25, CAP-27c, CAP-30; F-WKT-14, F-WKT-10, DV-38) ──
        private static void AddRoutineHud(List<SnapshotScenario> all)
        {
            var cap18 = Wk("CAP-18", "CAP-18-calentamiento", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Prueba 4a a los 6 s",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AtSecond(6)));
            cap18.Checks = h => Expect(
                ("Calentamiento en step.warmup.hud", () => h.TextOf("hud-step-type") == "Calentamiento" && ColorIs(h.Find("hud-step-type")!.resolvedStyle.color, Tokens.StepWarmupHud)),
                ("0:24", () => h.TextOf("hud-countdown") == "0:24"),
                ("sin #hud-step-target", () => !h.IsShown("hud-step-target")),
                ("sin #hud-up-next", () => !h.IsShown("hud-up-next")),
                ("ninguna tarjeta con objetivo", () => !Hud(h).SpmHasTarget && !Hud(h).SplitHasTarget && !Hud(h).WattsHasTarget));
            cap18.WithProbe(PixelProbe.Dominant("tipo step.warmup.hud", "hud-step-type", Tokens.StepWarmupHud.Rgb.ToHex(), 3), true);
            all.Add(cap18);

            all.Add(Cap19Like("CAP-19", "CAP-19-trabajo-con-objetivos", DeviceProfile.TV, "Prueba 4a a los 50 s", true));

            var cap20 = Wk("CAP-20", "CAP-20-piramide-500", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Suave; Pirámide 500 a los 605 s",
                s => Steps(s.Tap("sim-open"), s.TapInView("sim-preset-suave"), s.Tap("sim-close"), s.StartRoutine("Pirámide 500"), s.AtSecond(605)));
            cap20.Checks = h => Expect(
                ("500m", () => h.TextOf("hud-countdown") == "500m"),
                ("la pastilla", () => Hud(h).Steps?.TargetText is { } t && t.Contains("150-180W · 02:05/500m") && t.Contains("W +10/rep, Split -2s/rep")),
                ("Watts fuera (▲, obj. 150)", () => Off(h, Hud(h).Watts, HudCard.Arrow) && h.TextOf("hud-target-watts") == "obj. 150"),
                ("Split fuera (▼, obj. 02:05)", () => Off(h, Hud(h).Split, HudCard.ArrowDown) && h.TextOf("hud-target-split") == "obj. 02:05"),
                ("SPM sin objetivo", () => !Hud(h).SpmHasTarget));
            cap20.WithProbe(PixelProbe.Point("borde de Split @70 %", "hud-split", new Vector2(0.5f, 0.008f),
                    Over(Tokens.MetricSplit, 0.7, PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky)), 6), true)
                .WithProbe(PixelProbe.DominantIn("flecha de Split state.offTarget.hud", "hud-split-value", PixelProbe.PrefixOf, Tokens.StateOffTargetHud.Rgb.ToHex(), 3), true);
            all.Add(cap20);

            var cap21 = Wk("CAP-21", "CAP-21-proximo", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Prueba 4a a los 78 s",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AtSecond(78)));
            cap21.Checks = h => Expect(
                ("Próximo: Descanso", () => h.TextOf("hud-up-next-label") == "Próximo:" && h.TextOf("hud-up-next-type")?.Trim() == "Descanso"),
                ("Próximo entero", () => Inside(h, "hud-up-next") && Hud(h).Steps!.UpNextShown && UpNextWhole(h)),
                ("a 8 de 0:12 ± 1", () => h.TextOf("hud-countdown") == "0:12" && Near(h.BoundsOf("hud-countdown").xMin - h.BoundsOf("hud-up-next").xMax, 8, 1)),
                ("Próximo: en text.secondary", () => ColorIs(h.Find("hud-up-next-label")!.resolvedStyle.color, Tokens.TextSecondary)),
                ("Descanso en blanco", () => ColorIs(h.Find("hud-up-next-type")!.resolvedStyle.color, Tokens.TextHudPrimary)));
            cap21.WithProbe(PixelProbe.Dominant("Descanso blanco", "hud-up-next-type", Tokens.TextHudPrimary.Rgb.ToHex(), 3), true);
            all.Add(cap21);

            var cap22 = Wk("CAP-22", "CAP-22-por-terminar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Prueba 4a a los 87,6 s",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(87.6)));
            // A los 87,6 s el reloj común está en p = 1 (DN4a-21): la toma no espera la ventana de la lectura.
            cap22.SkipReadingWindow = true;
            cap22.Checks = h => Expect(
                ("0:03", () => h.TextOf("hud-countdown") == "0:03"),
                ("escala 1,1", () => Near((float)Hud(h).Steps!.Pulse.Scale, 1.1f, 0.002f)),
                ("en step.work.hud puro", () => ColorIs(h.Find("hud-countdown")!.resolvedStyle.color, Tokens.StepWorkHud)),
                ("Próximo: Descanso entero", () => Inside(h, "hud-up-next") && UpNextWhole(h) && h.TextOf("hud-up-next-type")?.Trim() == "Descanso"));
            cap22.WithProbe(PixelProbe.Dominant("cuenta step.work.hud", "hud-countdown", Tokens.StepWorkHud.Rgb.ToHex(), 3), true);
            all.Add(cap22);

            var cap23 = Cap19Like("CAP-23", "CAP-23-trabajo-th", DeviceProfile.TH, "Como CAP-19 en el teléfono horizontal", false);
            all.Add(cap23);

            var cap24 = Wk("CAP-24", "CAP-24-dos-fuera-l640", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.F4A),
                "Prueba 4a a los 118 s, Ritmo − (23) y 7 s",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(118), s.Tap("sim-open"), s.TapInView("sim-spm-minus"), s.Tap("sim-close"), s.Advance(7)));
            cap24.Checks = h => Concat(BoatZoneFree(h), Expect(
                ("SPM fuera (▲, obj. 24)", () => Off(h, Hud(h).Spm, HudCard.Arrow) && h.TextOf("hud-target-spm") == "obj. 24"),
                ("Watts fuera (▲, obj. 160)", () => Off(h, Hud(h).Watts, HudCard.Arrow) && h.TextOf("hud-target-watts") == "obj. 160"),
                // 0,962 en 04-design-notes §3.7, con las cifras de RowMate Numeric SC (WP-B0, QA-V4a-06), y la que calculó HudGeometry (T2).
                ("SPM a escala 0,962 (± 0,005) por la zona del bote (T2), la de HudGeometry", () => Near(Hud(h).Spm.Fit, 0.962f, 0.005f) && Near(Hud(h).Spm.Fit, (float)Zones(h).ValueScale(HudValue.Spm), 0.001f)),
                ("la llave sobre Pausa", () => h.BoundsOf("sim-open").yMax <= h.BoundsOf("hud-pause").yMin - 8 + 1
                                               && Near(h.BoundsOf("sim-open").xMax, h.BoundsOf("hud-pause").xMax, 1))));
            cap24.WithProbe(PixelProbe.DominantIn("flecha de SPM state.offTarget.hud", "hud-spm-value", PixelProbe.PrefixOf, Tokens.StateOffTargetHud.Rgb.ToHex(), 3), true);
            all.Add(cap24);

            all.Add(Cap19Like("CAP-25", "CAP-25-trabajo-w1280", DeviceProfile.W1280, "Como CAP-19 en el escritorio", true));

            var cap27c = Wk("CAP-27c", "CAP-27c-rutina-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Prueba 4a a los 78 s, en inglés",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AtSecond(78)), "en-US");
            cap27c.Checks = h => Expect(
                ("Work", () => h.TextOf("hud-step-type") == "Work"),
                ("Up next: Rest", () => h.TextOf("hud-up-next-label") == "Up next:" && h.TextOf("hud-up-next-type")?.Trim() == "Rest"),
                ("Rest en blanco", () => ColorIs(h.Find("hud-up-next-type")!.resolvedStyle.color, Tokens.TextHudPrimary)),
                ("target 160", () => h.TextOf("hud-target-watts") == "target 160"));
            all.Add(cap27c);

            var cap30 = Wk("CAP-30", "CAP-30-paso-sin-meta", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Sin meta a los 3 s",
                s => Steps(s.StartRoutine("Sin meta"), s.AtSecond(3)));
            cap30.Checks = h => Expect(
                ("?", () => h.TextOf("hud-countdown") == "?"),
                ("? en blanco", () => ColorIs(h.Find("hud-countdown")!.resolvedStyle.color, Tokens.TextHudPrimary)),
                ("sin #hud-up-next", () => !h.IsShown("hud-up-next")));
            all.Add(cap30);
        }

        // CAP-19 y sus variantes por perfil (CAP-23 en TH, CAP-25 en W1280).
        private static SnapshotScenario Cap19Like(string id, string stem, DeviceProfile profile, string state, bool full)
        {
            var scenario = Wk(id, stem, profile, HostRoutines.Seeded(RoutineSeeds.F4A), state, s => Steps(s.StartRoutine("Prueba 4a"), s.AtSecond(50)));
            scenario.Checks = h =>
            {
                var checks = new List<(string, Func<bool>)>
                {
                    ("Trabajo en step.work.hud", () => h.TextOf("hud-step-type") == "Trabajo" && ColorIs(h.Find("hud-step-type")!.resolvedStyle.color, Tokens.StepWorkHud)),
                    ("la pastilla entera o con …", () => Hud(h).Steps?.TargetText is { } t && t.Contains(">160W · 22 spm · ") && t.Contains("SPM +2/rep")),
                    ("Watts 150 fuera (▲)", () => h.TextOf("hud-watts-value") == "150" && Off(h, Hud(h).Watts, HudCard.Arrow)),
                    ("Watts en state.offTarget.hud", () => ColorIs(h.Find("hud-watts-value")!.resolvedStyle.color, Tokens.StateOffTargetHud)),
                    ("obj. 160 debajo de las cifras", () => h.TextOf("hud-target-watts") == "obj. 160"
                                                            && h.BoundsOf("hud-target-watts").yMin >= h.BoundsOf("hud-watts-value").yMax - 1),
                    ("SPM cumple, con el hueco vacío y obj. 22", () => Hud(h).SpmHasTarget && !Hud(h).Spm.IsOff && !Hud(h).Spm.ArrowShown
                                                                       && h.TextOf("hud-target-spm") == "obj. 22"),
                    ("Split sin objetivo", () => !Hud(h).SplitHasTarget),
                };
                if (full)
                {
                    checks.Add(("SPM en blanco", () => ColorIs(h.Find("hud-spm-value")!.resolvedStyle.color, Tokens.TextHudPrimary)));
                }

                return Expect(checks.ToArray());
            };
            // La tendencia es un glifo dentro del texto de la pastilla (un solo Label): el color dominante es el del texto, así que
            // se mide un punto del trazo de la flecha de trending_up (el mismo en TV, TH y W1280 con este texto; se vuelve a
            // ubicar si cambia el ancho del texto, por ejemplo con las tablas K2: WP-D).
            scenario.WithProbe(PixelProbe.Dominant("Watts state.offTarget.hud", "hud-watts-value", Tokens.StateOffTargetHud.Rgb.ToHex(), 2), true)
                .WithProbe(PixelProbe.DominantIn("flecha de Watts", "hud-watts-value", PixelProbe.PrefixOf, Tokens.StateOffTargetHud.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("tendencia metric.distance", "hud-step-target", new Vector2(0.5936f, 0.40f), Tokens.MetricDistance.Rgb.ToHex(), 3), true);
            if (full)
            {
                // 04-design-notes §8.2: la pastilla de trabajo al 20 % sobre el vidrio y el cielo, obj. en text.secondary y los
                // bordes con objetivo al 70 % (compuestos sobre el vidrio y el cielo; ± 6 junto al borde, como el halo).
                var glassSky = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky);
                scenario.WithProbe(PixelProbe.Point("pastilla step.work @20 %", "hud-step-target", new Vector2(0.01f, 0.5f), Over(Tokens.StepWork, 0.2, glassSky), 2), true)
                    .WithProbe(PixelProbe.Dominant("obj. text.secondary", "hud-target-watts", Over(Tokens.TextPrimary, 0.7, glassSky), 3), true)
                    .WithProbe(PixelProbe.Point("borde de Watts @70 %", "hud-watts", new Vector2(0.5f, 0.008f), Over(Tokens.MetricWatts, 0.7, glassSky), 6), true)
                    .WithProbe(PixelProbe.Point("borde de SPM @70 %", "hud-spm", new Vector2(0.5f, 0.008f), Over(Tokens.MetricSpm, 0.7, glassSky), 6), true);
            }

            return scenario;
        }

        // ── Diálogos y avisos (CAP-26 a CAP-29) ──
        private static void AddDialogsAndToasts(List<SnapshotScenario> all)
        {
            var cap26 = Wk("CAP-26", "CAP-26-dialogo-terminar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Libre a los 10 s, Terminar",
                s => Steps(s.StartFree(), s.AtSecond(10), s.Tap("hud-finish")));
            cap26.Checks = h => Expect(
                ("workout-finish-dialog", () => h.TopDialog?.Name == FinishWorkoutDialog.DialogName),
                ("opaco, de 331 ± 1", () => Near(h.TopDialog!.Box.worldBound.width, 331, 1) && Near(h.TopDialog.Box.resolvedStyle.backgroundColor.a, 1, 0.001f)),
                ("Terminar entrenamiento en una línea", () => h.TextOf("dialog-title") == "Terminar entrenamiento" && OneLine(h.Find("dialog-title")!)),
                ("el contenido", () => h.TextOf("dialog-message") == "Se guardará la sesión. ¿Continuar?"),
                ("Cancelar de texto y Terminar relleno", () => h.Find("dialog-cancel")!.ClassListContains("rm-button--text") && h.TextOf("dialog-cancel") == "Cancelar"
                                                               && h.Find("dialog-confirm")!.ClassListContains("rm-button--primary") && h.TextOf("dialog-confirm") == "Terminar"),
                ("sin .rm-dialog--hud", () => h.Root.Q(className: "rm-dialog--hud") == null),
                ("el HUD detrás del velo", () => h.Immersive != null && Index(h, "dialog-layer") > Index(h, "page-stack")),
                ("cinta y llave encima", () => Index(h, "simulator-layer") > Index(h, "dialog-layer") && h.IsShown("sim-ribbon") && h.IsShown("sim-open")));
            // El velo al 60 % sobre una tarjeta (vidrio y cielo), sobre el cielo y sobre el agua (04-design-notes §8.2).
            var veilGlass = Over(Tokens.OverlayScrim, 0.6, PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky));
            var veilSky = Over(Tokens.OverlayScrim, 0.6, Tokens.BackdropSky);
            var veilWater = Over(Tokens.OverlayScrim, 0.6, Tokens.BackdropWater);
            var tvSky = (float)((HudGeometry.TopBarTop + HudGeometry.TopBarHeight + HudGeometry.CardsTop) / 2
                                * HudGeometry.Scale(DeviceProfile.TV.WidthDp, DeviceProfile.TV.HeightDp) / DeviceProfile.TV.HeightDp);
            cap26.WithProbe(PixelProbe.Point("diálogo surface.overlay", "workout-finish-dialog .rm-dialog", new Vector2(0.03f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("velo sobre la tarjeta", "hud-spm", new Vector2(0.03f, 0.5f), veilGlass, 1), true)
                .WithProbe(PixelProbe.Point("velo sobre el cielo", "hud-backdrop", new Vector2(0.5f, tvSky), veilSky, 1), true)
                .WithProbe(PixelProbe.Point("velo sobre el agua", "hud-backdrop", new Vector2(0.5f, (float)(640 / DeviceProfile.TV.HeightDp)), veilWater, 1), true);
            all.Add(cap26);

            var cap27a = Wk("CAP-27a", "CAP-27a-libre-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Libre a los 10 s, en inglés",
                s => Steps(s.StartFree(), s.AtSecond(10)), "en-US");
            cap27a.Checks = h => Expect(
                ("Free Workout", () => h.TextOf("hud-free-title-text") == "Free Workout"),
                ("SPM, Split /500m, Watts, kcal", () => h.TextOf("hud-spm-label") == "SPM" && h.TextOf("hud-split-label") == "Split /500m"
                                                        && h.TextOf("hud-watts-label") == "Watts" && h.TextOf("hud-kcal-label") == "kcal"),
                ("Finish y Pause", () => h.TextOf("hud-finish") == "Finish" && h.TextOf("hud-pause") == "Pause"));
            all.Add(cap27a);

            var cap27b = Wk("CAP-27b", "CAP-27b-terminar-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Libre a los 10 s, Terminar, en inglés",
                s => Steps(s.StartFree(), s.AtSecond(10), s.Tap("hud-finish")), "en-US");
            cap27b.Checks = h => Expect(
                ("Finish workout", () => h.TextOf("dialog-title") == "Finish workout"),
                ("The session will be saved. Continue?", () => h.TextOf("dialog-message") == "The session will be saved. Continue?"),
                ("Cancel y Finish", () => h.TextOf("dialog-cancel") == "Cancel" && h.TextOf("dialog-confirm") == "Finish"));
            all.Add(cap27b);

            var cap28 = Wk("CAP-28", "CAP-28-rutina-completada", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A), "Corta a los 31 s",
                s => Steps(s.StartRoutine("Corta"), s.AtSecond(31), s.Settle()));
            cap28.Checks = h => Expect(
                ("Workout en reposo", () => h.Immersive == null && W(h).IsOnScreen),
                ("¡Entrenamiento completado!", () => h.Shell.Toasts.CurrentText == "¡Entrenamiento completado!"),
                ("el aviso entero", () => Inside(h, "toast")),
                ("8 dp sobre la llave ± 1", () => Near(h.BoundsOf("toast").yMax + 8, h.BoundsOf("sim-open").yMin, 1)),
                ("en la base, la sesión cerrada con 7 puntos", () => OnlySession(h) is { } session && !session.IsCut && session.PointCount == 7));
            cap28.WithProbe(PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.02f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 1), true);
            all.Add(cap28);

            var cap29 = Wk("CAP-29", "CAP-29-no-se-pudo-guardar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Alta que falla; libre a los 2 s",
                s => Steps(s.StartFree(), s.AtSecond(2)));
            cap29.Host.Sessions = HostSessions.Scripted(r => r.FailOnCreate = true);
            cap29.Checks = h => Expect(
                ("No se pudo guardar la sesión", () => h.Shell.Toasts.CurrentText == "No se pudo guardar la sesión"),
                ("el aviso entero", () => Inside(h, "toast")),
                ("#toast.yMax + 8 ≤ #hud-controls.yMin", () => h.BoundsOf("toast").yMax + 8 <= h.BoundsOf("hud-controls").yMin + 0.5f),
                ("#toast.yMin ≥ #hud-band.yMax", () => h.BoundsOf("toast").yMin >= h.BoundsOf("hud-band").yMax - 0.5f),
                ("00:02 (el reloj andando)", () => h.TextOf("hud-time-value") == "00:02"));
            all.Add(cap29);
        }

        // ── DEMO-01…14: una sola secuencia en W1280 con F-4A (spec §7.4) ──
        private static SnapshotScenario Demo()
        {
            SnapshotScenario? demo = null;
            demo = Wk("DEMO", "DEMO", DeviceProfile.W1280, HostRoutines.Seeded(RoutineSeeds.F4A), "Secuencia de la demo (spec §7.4)",
                s => DemoScript(s, demo!));
            demo.Kind = SnapshotKind.Sequence;
            demo.Shot("DEMO-01", "DEMO-01-workout", "Workout con F-4A")
                .Shot("DEMO-02", "DEMO-02-libre", "Libre a los 5 s")
                .Shot("DEMO-03", "DEMO-03-panel-en-el-hud", "La llave, Watts + y 1 s: el panel abierto en el HUD")
                .Shot("DEMO-04", "DEMO-04-pausa", "Cerrar el panel y Pausa")
                .Shot("DEMO-05", "DEMO-05-terminar", "Reanudar y Terminar")
                .Shot("DEMO-06", "DEMO-06-reposo", "Terminar: Workout en reposo")
                .Shot("DEMO-07", "DEMO-07-inicio-prueba-4a", "Watts − y la tarjeta de Prueba 4a")
                .Shot("DEMO-08", "DEMO-08-calentamiento-proximo", "Iniciar y 18 s")
                .Shot("DEMO-09", "DEMO-09-watts-fuera", "Hasta los 45 s")
                .Shot("DEMO-10", "DEMO-10-watts-cumple", "Watts + y 1 s")
                .Shot("DEMO-11", "DEMO-11-spm-fuera", "Hasta los 120 s, Ritmo − y 5 s")
                .Shot("DEMO-12", "DEMO-12-por-terminar", "Hasta los 237 s")
                .Shot("DEMO-13", "DEMO-13-completada", "4 s más: fin de la rutina")
                .Shot("DEMO-14", "DEMO-14-sin-remo", "Dispositivo → Desconectar; Workout");
            return demo;
        }

        private static IEnumerator DemoScript(ScenarioScript s, SnapshotScenario demo)
        {
            yield return s.Tap("tab-workout");
            yield return s.Shot("DEMO-01", h => Expect(("4 tarjetas", () => W(h).List.Count == 4),
                ("Prueba 4a primera, con 6 tramos", () => W(h).List.CardAt(0)!.NameLabel.text == "Prueba 4a" && W(h).List.CardAt(0)!.Bar.SegmentCount == 6)));

            yield return s.StartFree();
            yield return s.AtSecond(5);
            yield return s.Shot("DEMO-02", h => Expect(("00:05", () => h.TextOf("hud-time-value") == "00:05"), ("24.0", () => h.TextOf("hud-spm-value") == "24.0"),
                ("150 W", () => h.TextOf("hud-watts-value") == "150")));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-watts-plus");
            yield return s.Advance(1);
            yield return s.Shot("DEMO-03", h => Expect(("el panel abierto", () => h.IsShown("sim-panel")),
                ("desde la esquina de abajo a la derecha de la llave", () => PanelFromWrenchCorner(h)),
                ("Split y Watts a la vista", () => !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-split")) && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-watts"))),
                ("160 W", () => h.TextOf("hud-watts-value") == "160")));

            yield return s.Tap("sim-close");
            yield return s.Tap("hud-pause");
            yield return s.Shot("DEMO-04", h => Expect(("Reanudar", () => h.IsShown("hud-resume") && !h.IsShown("hud-pause"))));

            yield return s.Tap("hud-resume");
            yield return s.Tap("hud-finish");
            yield return s.Shot("DEMO-05", h => Expect(("el diálogo de terminar", () => h.TopDialog?.Name == FinishWorkoutDialog.DialogName)));

            yield return s.Tap("dialog-confirm");
            yield return s.Settle();
            yield return s.Shot("DEMO-06", h => Expect(("Workout en reposo", () => h.Immersive == null && W(h).IsOnScreen),
                ("sin aviso", () => !h.Shell.Toasts.IsVisible)));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-watts-minus");
            yield return s.Tap("sim-close");
            yield return s.TapRoutine("Prueba 4a");
            yield return s.Shot("DEMO-07", h => Expect(("el diálogo de Prueba 4a", () => h.TopDialog?.Name == StartWorkoutDialog.DialogName && h.TextOf("dialog-title") == "Prueba 4a")));

            yield return s.ConfirmStart();
            yield return s.AtSecond(18);
            yield return s.Shot("DEMO-08", h => Expect(("Calentamiento con 0:12", () => h.TextOf("hud-step-type") == "Calentamiento" && h.TextOf("hud-countdown") == "0:12"),
                ("Próximo: Trabajo entero", () => Inside(h, "hud-up-next") && UpNextWhole(h) && h.TextOf("hud-up-next-type")?.Trim() == "Trabajo")));

            yield return s.AtSecond(45);
            yield return s.Shot("DEMO-09", h => Expect(("Watts fuera (▲, obj. 160)", () => Off(h, Hud(h).Watts, HudCard.Arrow) && h.TextOf("hud-target-watts") == "obj. 160")));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-watts-plus");
            yield return s.Tap("sim-close");
            yield return s.Advance(1);
            yield return s.Shot("DEMO-10", h => Expect(("Watts cumple (blanco, hueco vacío)", () => !Hud(h).Watts.IsOff && !Hud(h).Watts.ArrowShown)));

            yield return s.AdvanceTo(120);
            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-spm-minus");
            yield return s.Tap("sim-close");
            yield return s.Advance(5);
            yield return s.Shot("DEMO-11", h => Expect(("SPM fuera (▲, obj. 24)", () => Off(h, Hud(h).Spm, HudCard.Arrow) && h.TextOf("hud-target-spm") == "obj. 24")));

            // DEMO-12, "la cuenta 0:03 grande y teñida" (spec §7.4): el reloj común nace a los 235,0 s (por terminar del
            // enfriamiento) y en la ventana de la lectura del segundo 237 está cerca de su valle (p ≈ 0,1: la cuenta casi blanca).
            // La toma va a los 237,9 s (p ≈ 0,9), después del rodillo de la lectura y sin la ventana, como CAP-22 (WP-D).
            demo.SkipReadingWindow = true;
            yield return s.AdvanceTo(237.9);
            yield return s.Shot("DEMO-12", h => Expect(("0:03", () => h.TextOf("hud-countdown") == "0:03"), ("sin Próximo", () => !h.IsShown("hud-up-next")),
                ("teñida (p ≥ 0,5)", () => Hud(h).Steps!.Pulse.PulseAt(h.Scheduler.MonotonicSeconds) >= 0.5)));
            demo.SkipReadingWindow = false;

            yield return s.Advance(4);
            yield return s.Settle();
            yield return s.Shot("DEMO-13", h => Expect(("Workout en reposo", () => h.Immersive == null),
                ("¡Entrenamiento completado!", () => h.Shell.Toasts.CurrentText == "¡Entrenamiento completado!")));

            yield return s.Tap("tab-device");
            yield return s.TapInView("device-disconnect");
            yield return s.Tap("tab-workout");
            yield return s.Shot("DEMO-14", h => Expect(("el aviso sin remo", () => h.IsShown("workout-banner")),
                ("todo apagado", () => !h.Find("workout-free")!.enabledInHierarchy && h.WhyNotTappable("workout-routine-0") != null)));
        }

        // ── VID-01…03 (spec §7.5) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Wk("VID-01", "VID-01-proximo-y-cambio-de-paso", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A),
                "Prueba 4a de los 14,5 s a los 31,5 s (17 s): Próximo, por terminar y el cambio a Trabajo",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(14.5)));
            vid01.Kind = SnapshotKind.Video;
            vid01.Video = new VideoSpec(17);
            all.Add(vid01);

            var vid02 = Wk("VID-02", "VID-02-fuera-de-objetivo", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.F4A),
                "Prueba 4a a los 35 s, con el panel: Ritmo − tres veces (primer cruce), +, − (segundo cruce, antes de 4 s), + y + (10 s)",
                s => Steps(s.StartRoutine("Prueba 4a"), s.AdvanceTo(35), s.Tap("sim-open")));
            vid02.Kind = SnapshotKind.Video;
            vid02.Video = new VideoSpec(10).At(1, h => h.Tap("sim-spm-minus")).At(2, h => h.Tap("sim-spm-minus")).At(3, h => h.Tap("sim-spm-minus"))
                .At(4.5, h => h.Tap("sim-spm-plus")).At(5.5, h => h.Tap("sim-spm-minus")).At(7, h => h.Tap("sim-spm-plus")).At(8.5, h => h.Tap("sim-spm-plus"));
            all.Add(vid02);

            var vid03 = Wk("VID-03", "VID-03-pausa-y-terminar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Libre a los 3 s: Pausa, Reanudar, Terminar, Cancelar, Terminar y Terminar (10 s)", s => Steps(s.StartFree(), s.AdvanceTo(3)));
            vid03.Kind = SnapshotKind.Video;
            vid03.Video = new VideoSpec(10).At(1, h => h.Tap("hud-pause")).At(2.5, h => h.Tap("hud-resume")).At(4, h => h.Tap("hud-finish"))
                .At(5.5, h => h.Tap("dialog-cancel")).At(7, h => h.Tap("hud-finish")).At(8.5, h => h.Tap("dialog-confirm"));
            all.Add(vid03);
        }

        // ── Ayudas de armado ──

        private static SnapshotScenario Wk(string id, string stem, DeviceProfile profile, HostRoutineFactory routines, string state,
                                           Func<ScenarioScript, IEnumerator>? arrange, string language = "es-AR", double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                LanguageTags = new[] { language },
                TextScale = textScale,
                Routines = routines,
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Sim" };
        }

        // El pulsómetro simulado desde Dispositivo (Buscar y la fila del sensor, como en la It. 2).
        private static IEnumerator ConnectHrm(ScenarioScript s)
        {
            yield return s.TapInView("hrm-search");
            yield return s.TapInView(HrmRow);
        }
#endif

        // ── Comprobaciones compartidas ──

        // CAP-01, -04, -10: Workout en reposo con F-LISTA (DV-21, DV-22; DN4a-19).
        private static IEnumerable<string> RestChecks(UiTestHost h, string title, string free, string label, string[] summaries)
        {
            var names = new[] { "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" };
            var segments = new[] { 8, 14, 1, 7 };
            var checks = new List<(string, Func<bool>)>
            {
                (title, () => h.IsTextVisible(title)),
                ("barra sin acciones", () => W(h).AppBar.Actions.childCount == 0),
                ("#workout-free " + free, () => h.TextOf("workout-free") == free),
                ("#workout-free habilitado", () => h.WhyNotTappable("workout-free") == null),
                ("#workout-free de 16 a 16 ± 1", () => Near(X(h, "workout-free").xMin, 16, 1) && Near(h.Root.worldBound.xMax - h.BoundsOf("workout-free").xMax, 16, 1)),
                ("#workout-free alto ≥ 56", () => h.BoundsOf("workout-free").height >= 56 - 0.5f),
                ("etiqueta", () => h.TextOf("workout-routines-label") == label),
                ("4 tarjetas", () => W(h).List.Count == 4),
                ("ningún ⋮", () => W(h).List.Q<IconButton>() == null),
                ("sin #workout-banner", () => !h.IsShown("workout-banner")),
                ("sin escenario", () => !h.IsTextVisible("Escenario") && !h.IsTextVisible("Scenery")),
            };
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                checks.Add(("workout-routine-" + i + " = " + names[i], () => W(h).List.CardAt(index)!.NameLabel.text == names[index]));
                checks.Add(("resumen " + summaries[i], () => W(h).List.CardAt(index)!.SummaryLabel.text == summaries[index]));
                checks.Add((segments[i] + " tramos", () => W(h).List.CardAt(index)!.Bar.SegmentCount == segments[index]));
                checks.Add(("chevron de la tarjeta " + i + " centrado en la columna de 28 dp ± 1", () => ChevronInColumn(W(h).List.CardAt(index)!)));
            }

            return Expect(checks.ToArray());
        }

        // CAP-12, -13: lo del inmersivo libre que no depende del pulso.
        private static IEnumerable<string> FreeChecks(UiTestHost h, bool withBpm)
        {
            return Expect(
                ("el inmersivo", () => h.Immersive != null && !h.Immersive.IsExiting),
                ("Entrenamiento Libre", () => h.TextOf("hud-free-title-text") == "Entrenamiento Libre"),
                ("#hud-distance", () => h.IsShown("hud-distance")),
                ("#hud-kcal", () => h.IsShown("hud-kcal")),
                ("#hud-bpm " + (withBpm ? "a la vista" : "oculto"), () => h.IsShown("hud-bpm") == withBpm),
                ("#hud-finish y #hud-pause", () => h.IsShown("hud-finish") && h.IsShown("hud-pause") && !h.IsShown("hud-resume")),
                ("la barra de pestañas tapada", () => h.Shell.Pages.CoversShell));
        }

        // Ninguna pieza del HUD entra en la zona del bote (DN4a-02; AC-WKT-17-09).
        private static IEnumerable<string> BoatZoneFree(UiTestHost h)
        {
            var problems = new List<string>();
            if (h.Immersive?.Layout == null)
            {
                problems.Add("no hay inmersivo con disposición");
                return problems;
            }

            var zone = h.Immersive.Layout.BoatZone;
            var window = h.Root.worldBound;
            // ± 0,5 dp: el redondeo de cada caja al píxel (la geometría deja las piezas justo en el borde de la zona, T2 y T3).
            var rect = new Rect(window.xMin + (float)zone.Left + 0.5f, window.yMin + (float)zone.Top + 0.5f, (float)zone.Width - 1f, (float)zone.Height - 1f);
            foreach (var name in new[] { "hud-topbar", "hud-spm", "hud-split", "hud-watts", "hud-kcal", "hud-bpm", "hud-band", "hud-finish", "hud-pause", "hud-resume" })
            {
                var piece = h.Find(name);
                if (piece != null && UiQuery.IsShown(piece) && Overlap(piece.worldBound, rect))
                {
                    problems.Add("#" + name + " entra en la zona del bote (" + Fmt(piece.worldBound) + "; zona " + Fmt(rect) + ")");
                }
            }

            return problems;
        }

        // Los rectángulos de 04-design-notes §3.8 (± 1 dp), con la caja real si no se cumple.
        private static IEnumerable<string> Rects(UiTestHost h, params (string Name, double X0, double X1, double Y0, double Y1)[] expected)
        {
            var problems = new List<string>();
            foreach (var e in expected)
            {
                if (!RectIs(h, e.Name, e.X0, e.X1, e.Y0, e.Y1))
                {
                    var box = h.Find(e.Name) != null ? X(h, e.Name) : Rect.zero;
                    problems.Add("no se cumple: #" + e.Name + " en " + F(e.X0) + "-" + F(e.X1) + " · " + F(e.Y0) + "-" + F(e.Y1) + " ± 1 (es " + Fmt(box) + ")");
                }
            }

            return problems;
        }

        private static bool RectIs(UiTestHost h, string name, double x0, double x1, double y0, double y1)
        {
            if (h.Find(name) == null)
            {
                return false;
            }

            var b = X(h, name);
            return Near(b.xMin, (float)x0, 1) && Near(b.xMax, (float)x1, 1) && Near(b.yMin, (float)y0, 1) && Near(b.yMax, (float)y1, 1);
        }

        // La caja en coordenadas de la ventana.
        private static Rect X(UiTestHost h, string name)
        {
            var b = h.BoundsOf(name);
            var w = h.Root.worldBound;
            return new Rect(b.xMin - w.xMin, b.yMin - w.yMin, b.width, b.height);
        }

        // Fuera de objetivo con la flecha pedida a la vista en su hueco.
        private static bool Off(UiTestHost h, HudCard card, string arrow)
        {
            return card.HasTarget && card.IsOff && card.ArrowShown && card.ArrowGlyph == arrow;
        }

        // El chevron, a 28 dp del borde derecho de la tarjeta (el centro de la columna del ⋮ de Rutinas).
        private static bool ChevronInColumn(RoutineCardView card)
        {
            if (card.Chevron == null || card.Chevron.childCount == 0)
            {
                return false;
            }

            var icon = card.Chevron[0].worldBound;
            return Near(card.worldBound.xMax - icon.center.x, 28, 1) && Near(icon.center.y, card.Chevron.worldBound.center.y, 1);
        }

        // DEMO-03: el panel crece desde la esquina de abajo a la derecha del lugar de la llave (AC-BLE-16-03).
        private static bool PanelFromWrenchCorner(UiTestHost h)
        {
            var layout = h.Immersive?.Layout;
            if (layout?.Wrench == null)
            {
                return false;
            }

            var panel = X(h, "sim-panel");
            var slot = layout.Wrench.Value;
            return Near(panel.xMax, (float)slot.Right, 1) && Near(panel.yMax, (float)slot.Bottom, 1);
        }

        private static WorkoutScreenView W(UiTestHost h)
        {
            return h.WorkoutScreen ?? throw new InvalidOperationException("no existe la pantalla Workout");
        }

        private static HudView Hud(UiTestHost h)
        {
            return h.Immersive?.View ?? throw new InvalidOperationException("no hay inmersivo");
        }

        private static HudLayout Zones(UiTestHost h)
        {
            return h.Immersive?.Layout ?? throw new InvalidOperationException("el inmersivo no tiene disposición");
        }

        private static int SessionCount(UiTestHost h)
        {
            var task = h.Sessions.ListAsync();
            return task.IsCompleted ? task.Result.Count : -1;
        }

        private static SessionListing? OnlySession(UiTestHost h)
        {
            var task = h.Sessions.ListAsync();
            return task.IsCompleted && task.Result.Count == 1 ? task.Result[0] : null;
        }

        private static int Index(UiTestHost h, string name)
        {
            var root = h.Find("app-root")!;
            var element = h.Find(name);
            if (element == null)
            {
                return -1;
            }

            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.hierarchy.parent == root)
                {
                    return root.hierarchy.IndexOf(e);
                }
            }

            return -1;
        }

        // El color resuelto contra un token (± 1 por canal en 0-255 y el alfa del token, ± 0,01).
        private static bool ColorIs(Color color, ColorToken token)
        {
            var c = (Color32)color;
            var t = token.Rgb;
            return Math.Abs(c.r - t.R) <= 1 && Math.Abs(c.g - t.G) <= 1 && Math.Abs(c.b - t.B) <= 1 && Math.Abs(color.a - (float)token.Alpha) <= 0.01f;
        }

        // La pastilla de Próximo mide sus dos textos enteros más el relleno (en dp base: la caja / la escala de la zona).
        private static bool UpNextWhole(UiTestHost h)
        {
            var steps = Hud(h).Steps;
            if (steps == null || !steps.UpNextShown)
            {
                return false;
            }

            var label = steps.UpNextLabel;
            var type = steps.UpNextType;
            var need = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x
                       + type.MeasureTextSize(type.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x
                       + 2 * (StepInfoBar.UpNextPadHDp + 1);
            return h.BoundsOf("hud-up-next").width / (float)Zones(h).Scale >= need - 1;
        }

        // Una sola línea de texto: el alto del contenido (sin el relleno) no llega a dos líneas de su letra.
        private static bool OneLine(VisualElement label)
        {
            return label.contentRect.height < label.resolvedStyle.fontSize * 1.6f;
        }

        // Los controles, de Terminar a Pausa (o Reanudar), contra el rectángulo de 04-design-notes §3.8 (± 1).
        private static IEnumerable<string> ControlsRect(UiTestHost h, double x0, double x1, double y0, double y1)
        {
            var finish = X(h, "hud-finish");
            var pause = X(h, h.IsShown("hud-resume") ? "hud-resume" : "hud-pause");
            var box = Rect.MinMaxRect(Mathf.Min(finish.xMin, pause.xMin), Mathf.Min(finish.yMin, pause.yMin), Mathf.Max(finish.xMax, pause.xMax), Mathf.Max(finish.yMax, pause.yMax));
            if (Near(box.xMin, (float)x0, 1) && Near(box.xMax, (float)x1, 1) && Near(box.yMin, (float)y0, 1) && Near(box.yMax, (float)y1, 1))
            {
                return new string[0];
            }

            return new[] { "no se cumple: los controles en " + F(x0) + "-" + F(x1) + " · " + F(y0) + "-" + F(y1) + " ± 1 (es " + Fmt(box) + ")" };
        }

        // ── Sondas de aspecto compartidas (04-design-notes §8.2) ──

        // QA-V4a-03 (DD4b-01 de 04b/00-deuda-diseno §1.3): el divisor de la banda se ve, blanco al 12 % sobre el vidrio y el agua
        // (#263241), en el medio de su columna. Con el divisor en 0 la toma falla.
        private static PixelProbe DividerProbe()
        {
            var glassWater = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropWater);
            return PixelProbe.Point("divisor de la banda blanco @12 %", "hud-band-divider", new Vector2(0.5f, 0.5f), Over(Tokens.TextPrimary, HudBand.DividerAlpha, glassWater), 2);
        }

        private static void RestProbes(SnapshotScenario scenario)
        {
            scenario.WithProbe(PixelProbe.Point("botón accent.primary", "workout-free", new Vector2(0.03f, 0.5f), Tokens.AccentPrimary.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("tarjeta surface.card", "workout-routine-0", new Vector2(0.5f, 0.05f), Tokens.SurfaceCard.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Dominant("chevron text.tertiary", "workout-routine-0 .rm-routine-card__chevron", PixelProbe.Composite(Tokens.TextTertiary, Tokens.SurfaceCard), 3), true)
                .WithProbe(PixelProbe.Point("llave surface.overlay", "sim-open", new Vector2(0.5f, 0.15f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true);
        }

        // CAP-12 (y CAP-17): el fondo, el vidrio y los colores del HUD. El vidrio y los valores no son de aspecto (AC-APP-06-01,
        // DV-31: ± 2), el resto sí.
        private static void HudProbes(SnapshotScenario scenario, DeviceProfile profile, bool aspectOnly)
        {
            // El cielo, a mitad de camino entre la barra superior y las tarjetas (69·s: 72,7 en TV, 110,4 en W1280). Con un
            // y fijo de 70 dp la sonda caía sobre la barra en W1280, que llega a 76,8 (WP-D).
            var s = HudGeometry.Scale(profile.WidthDp, profile.HeightDp);
            var skyY = (float)((HudGeometry.TopBarTop + HudGeometry.TopBarHeight + HudGeometry.CardsTop) / 2 * s / profile.HeightDp);
            var waterY = (float)(Math.Min(640, profile.HeightDp * 0.7) / profile.HeightDp);
            var glassSky = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky);
            var glassWater = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropWater);
            scenario.WithProbe(PixelProbe.Point("cielo backdrop.sky", "hud-backdrop", new Vector2(0.5f, skyY), Tokens.BackdropSky.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("agua backdrop.water", "hud-backdrop", new Vector2(0.5f, waterY), Tokens.BackdropWater.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("vidrio sobre el cielo", "hud-spm", new Vector2(0.03f, 0.5f), glassSky, 2), aspectOnly)
                .WithProbe(PixelProbe.Point("vidrio sobre el agua", "hud-band", new Vector2(0.02f, 0.5f), glassWater, 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("SPM metric.spm.hud", "hud-spm-value", Tokens.MetricSpmHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("Split metric.split.hud", "hud-split-value", Tokens.MetricSplitHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("Watts metric.watts.hud", "hud-watts-value", Tokens.MetricWattsHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("tiempo metric.time.hud", "hud-time-value", Tokens.MetricTimeHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("distancia metric.distance.hud", "hud-distance-value", Tokens.MetricDistanceHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.Dominant("kcal metric.kcal.hud", "hud-kcal-value", Tokens.MetricKcalHud.Rgb.ToHex(), 2), aspectOnly)
                .WithProbe(PixelProbe.DominantIn("W metric.watts.hud @80 %", "hud-watts-value", PixelProbe.SuffixOf, Over(Tokens.MetricWattsHud, 0.8, glassSky), 3), true)
                .WithProbe(PixelProbe.Dominant("Terminar state.dangerText", "hud-finish .rm-button__label", Tokens.StateDangerText.Rgb.ToHex(), 3), true);
        }

        // ── Ayudas genéricas ──

        private static string Over(ColorToken top, double alpha, ColorToken under)
        {
            return Over(top, alpha, under.Rgb.ToHex());
        }

        // El color de un token con alfa sobre otro ya compuesto, en sRGB como la tubería GammaRt (sin literales: ADR 016).
        private static string Over(ColorToken top, double alpha, string underHex)
        {
            byte Mix(byte a, byte b)
            {
                return (byte)Math.Floor(a * alpha + b * (1 - alpha) + 0.5);
            }

            var t = top.Rgb;
            var u = Rgb.Parse(underHex);
            return new Rgb(Mix(t.R, u.R), Mix(t.G, u.G), Mix(t.B, u.B)).ToHex();
        }

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
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

        private static IEnumerable<string> Concat(params IEnumerable<string>[] parts)
        {
            var list = new List<string>();
            foreach (var part in parts)
            {
                list.AddRange(part);
            }

            return list;
        }

        private static bool Inside(UiTestHost h, string name)
        {
            var window = h.Root.worldBound;
            var bounds = h.BoundsOf(name);
            return h.IsShown(name) && bounds.xMin >= window.xMin - 0.5f && bounds.yMin >= window.yMin - 0.5f && bounds.xMax <= window.xMax + 0.5f
                   && bounds.yMax <= window.yMax + 0.5f;
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        private static bool Near(float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        private static string F(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Fmt(Rect r)
        {
            return F(r.xMin) + "-" + F(r.xMax) + " · " + F(r.yMin) + "-" + F(r.yMax);
        }
    }
}
