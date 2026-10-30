using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Motion.Hud;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it4b (04b-hud-exr/01-po-spec §7 → 02-tech-design §5; aspecto: 04-design-notes §7): CAP-01 a CAP-31, DEMO-01 a -12
    // en una sola secuencia y VID-01 a -08. Salvo que diga otra cosa: español (es-AR), el remo de prueba R-4 (L(k) en los k − 0,5
    // de la sesión: el toque cae en un k + 0,5 del reloj de prueba) con la cinta y la llave sin panel, la sesión creada a los
    // 2026-10-28T13:00:00.000Z con la zona UTC−03:00 (el reloj de pared dice 10:00 en el primer minuto), una base de prueba propia
    // (TempDatabase: nunca rower_app.db ni rower_app_dev.db, AC-HIS-20-04) y FakeSystemUi en cada anfitrión (ninguna toma llama al
    // sistema). La DEMO y los videos que usan el panel van con el simulador sin ruido.
    // Regla de toma de la 4b (§5.2, DT4b-19; ExactInstant): cada toma es en el instante exacto de la spec (nunca en un segundo
    // entero del motor: +0,2 s) y FALLA si algo de la lista de bloqueos corre (SnapshotTakeRule): nunca espera. Los pulsos van en la
    // fase fija de su instante. En cada foto el arnés corre además HudOverlap y WrenchAudit (con las piezas nuevas y el diálogo de
    // completado). Los videos dejan en el manifest los valores por cuadro de los IMotion de WP-D1 y los comparan con las líneas de
    // tiempo puras (± 0,01).
    public static class HudExrCatalog
    {
        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddPills(all);
                AddCompletion(all);
                AddFreeBar(all);
                AddInfoRow(all);
                AddLandscape(all);
                all.Add(Demo());
                AddVideos(all);
                AddAspectProbes(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
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

#if ROWMATE_SIMULATOR
        // ── Las píldoras, la cuenta grande y el sello (CAP-01 a CAP-06, CAP-12, CAP-22, CAP-27) ──
        private static void AddPills(List<SnapshotScenario> all)
        {
            var cap01 = Exr("CAP-01", "CAP-01-pildoras-tv", DeviceProfile.TV, RoutineSeeds.F4B, "Estados a los 40,2 s", s => Routine(s, "Estados", 40.2));
            cap01.Checks = h => Concat(Looks(h, StepPillLook.Solid, StepPillLook.Hollow, StepPillLook.Solid, StepPillLook.Current, StepPillLook.Pending), Expect(
                ("anchos iguales ± 1 px", () => EqualWidths(h)),
                ("la 3 con relleno 0,510 ± 0,005", () => Near(Row(h).Fill, 0.510, 0.005)),
                ("#hud-step-type Trabajo", () => h.TextOf("hud-step-type") == "Trabajo"),
                ("la pastilla 18 spm entera", () => Hud(h).Steps!.PillText == "18 spm"),
                ("Próximo: Descanso", () => h.IsShown("hud-up-next") && h.TextOf("hud-up-next-type")?.Trim() == "Descanso"),
                ("#hud-countdown 0:10", () => h.TextOf("hud-countdown") == "0:10"),
                ("#hud-wall-clock 10:00 con schedule", () => h.TextOf("hud-wall-clock") == "10:00" && Hud(h).WallClock.Icon == Icons.Schedule),
                ("la cuenta y el reloj pegados al borde derecho", () => h.BoundsOf("hud-wall-clock").xMax <= h.BoundsOf("hud-topbar").xMax + 0.5f
                                                                        && h.BoundsOf("hud-wall-clock").xMin >= h.BoundsOf("hud-countdown").xMax - 0.5f),
                ("SPM con obj. 18", () => h.TextOf("hud-target-spm") == "obj. 18"),
                ("la cinta sobre el comienzo de la barra", () => Overlap(h.BoundsOf("sim-ribbon"), h.BoundsOf("hud-topbar")))));
            all.Add(cap01);

            var cap02 = Exr("CAP-02", "CAP-02-cuenta-grande-y-halo", DeviceProfile.TV, RoutineSeeds.F4B, "Estados a los 47,6 s (p = 1)",
                s => Routine(s, "Estados", 47.6));
            cap02.Checks = h => Expect(
                ("#hud-big-countdown 3", () => h.IsShown("hud-big-countdown") && h.TextOf("hud-big-countdown") == "3"),
                ("en HudLayout.BigCount", () => DiscCentered(h)),
                ("el aro en 0,80 ± 0,01", () => Near(Hud(h).Callouts.Motion.RingFraction, 0.80, 0.01)),
                ("el halo de la 3 en α 0,70 ± 0,01", () => Near(Row(h).HaloAlpha, 0.70, 0.01)),
                ("#hud-countdown 0:03", () => h.TextOf("hud-countdown") == "0:03"),
                ("Próximo: Descanso", () => h.IsShown("hud-up-next")),
                ("fuera de la zona del bote", () => Hud(h).Callouts.Disc.Bottom <= Layout(h).BoatZone.Top + 0.5));
            all.Add(cap02);

            var cap03 = Exr("CAP-03", "CAP-03-sello-cumplido", DeviceProfile.TV, RoutineSeeds.F4B, "Estados a los 10,6 s", s => Routine(s, "Estados", 10.6));
            cap03.Checks = h => Expect(
                ("el sello con check_circle", () => Seal(h).IsShowing && Seal(h).Icon.Codepoint == Icons.CheckCircle),
                ("Objetivo cumplido · 24.0 / 18 spm", () => Seal(h).Text == "Objetivo cumplido · 24.0 / 18 spm"),
                ("en #06D6A0", () => ColorIs(Seal(h).Title.resolvedStyle.color, Tokens.StateSuccess)),
                ("en una línea", () => Math.Abs(Seal(h).Title.worldBound.center.y - Seal(h).Detail.worldBound.center.y) < 2),
                ("la barra en 74·s ± 0,5", () => Near(h.BoundsOf("hud-topbar").height / (float)Layout(h).Scale, 74, 0.5)),
                ("la 0 sólida", () => Row(h).LookAt(0) == StepPillLook.Solid),
                ("la 1 en curso con relleno 0,060 ± 0,005", () => Row(h).LookAt(1) == StepPillLook.Current && Near(Row(h).Fill, 0.060, 0.005)));
            all.Add(cap03);

            var cap04 = Exr("CAP-04", "CAP-04-sello-no-alcanzado", DeviceProfile.TV, RoutineSeeds.F4B, "Estados a los 20,6 s", s => Routine(s, "Estados", 20.6));
            cap04.Checks = h => Expect(
                ("error de contorno", () => Seal(h).IsShowing && Seal(h).Icon.Codepoint == Icons.Error && !Seal(h).Icon.IsFilled),
                ("No alcanzado · 24.0 / 30 spm", () => Seal(h).Text == "No alcanzado · 24.0 / 30 spm"),
                ("en #FFD166", () => ColorIs(Seal(h).Title.resolvedStyle.color, Tokens.PhasePaused)),
                ("la 1 hueca", () => Row(h).LookAt(1) == StepPillLook.Hollow));
            all.Add(cap04);

            var cap05 = Exr("CAP-05", "CAP-05-sello-piramide", DeviceProfile.TV, RoutineSeeds.Lista, "Pirámide 500 a los 725,6 s",
                s => Routine(s, "Pirámide 500", 725.6));
            cap05.Checks = h => Expect(
                ("el sello entero", () => Seal(h).IsShowing && Seal(h).Text.StartsWith("Objetivo cumplido", StringComparison.Ordinal) && !Seal(h).Text.Contains("…")),
                ("14 píldoras del mismo ancho", () => Row(h).Count == 14 && EqualWidths(h)),
                ("la 2 en curso sin relleno (Q-51)", () => Row(h).LookAt(2) == StepPillLook.Current && Row(h).Fill <= 1e-6),
                ("#hud-countdown 250m", () => h.TextOf("hud-countdown") == "250m"));
            all.Add(cap05);

            var cap06 = Exr("CAP-06", "CAP-06-ultima-llena", DeviceProfile.TV, RoutineSeeds.F4A, "Corta a los 30,25 s", s => Routine(s, "Corta", 30.25));
            cap06.Checks = h => Expect(
                ("la 3 en curso con relleno 1", () => Row(h).LookAt(3) == StepPillLook.Current && Near(Row(h).Fill, 1, 0.005)),
                ("0, 1 y 2 sólidas", () => Row(h).LookAt(0) == StepPillLook.Solid && Row(h).LookAt(1) == StepPillLook.Solid && Row(h).LookAt(2) == StepPillLook.Solid),
                ("Descanso, 0:00", () => h.TextOf("hud-step-type") == "Descanso" && h.TextOf("hud-countdown") == "0:00"),
                ("sin Próximo, halo, cuenta grande, sello ni diálogo", () => !h.IsShown("hud-up-next") && Row(h).HaloAlpha <= 1e-6 && !h.IsShown("hud-big-countdown")
                                                                          && !Seal(h).IsShowing && h.Completion == null));
            all.Add(cap06);

            var cap12 = Exr("CAP-12", "CAP-12-final-por-distancia", DeviceProfile.TV, RoutineSeeds.F4B, "Final por distancia a los 35,2 s",
                s => Routine(s, "Final por distancia", 35.2));
            cap12.Checks = h => Expect(
                ("la 0 sólida", () => Row(h).LookAt(0) == StepPillLook.Solid),
                ("la 1 en curso con la pista sola", () => Row(h).LookAt(1) == StepPillLook.Current && Row(h).Fill <= 1e-6),
                ("100m", () => h.TextOf("hud-countdown") == "100m"),
                ("sin sello", () => !Seal(h).IsShowing));
            all.Add(cap12);

            var cap22 = Exr("CAP-22", "CAP-22-serie-larga", DeviceProfile.TV, RoutineSeeds.F4B, "Serie larga a los 3016,6 s",
                s => Routine(s, "Serie larga", 3016.6));
            cap22.Checks = h => Expect(
                ("la tira de 200", () => Row(h).CurrentLayout.Strip && Row(h).Count == 200),
                ("el 100 en curso como pulgar", () => Row(h).LookAt(100) == StepPillLook.Current && Row(h).CurrentLayout.Narrow),
                ("relleno 0,553 ± 0,005", () => Near(Row(h).Fill, 0.553, 0.005)),
                ("Trabajo, Próximo: Descanso, 0:14", () => h.TextOf("hud-step-type") == "Trabajo" && h.IsShown("hud-up-next") && h.TextOf("hud-countdown") == "0:14"),
                ("#hud-wall-clock 10:50", () => h.TextOf("hud-wall-clock") == "10:50"));
            all.Add(cap22);

            var cap27 = Exr("CAP-27", "CAP-27-pausa-con-la-cuenta", DeviceProfile.TV, RoutineSeeds.F4B, "Estados: Pausa a los 47,0 s, toma a los 47,6 s",
                s => Steps(Routine(s, "Estados", 47.0), s.Do(h => h.Tap("hud-pause")), s.AdvanceTo(47.6)));
            cap27.Checks = h => Expect(
                ("el velo", () => Near(Hud(h).Backdrop.Veil, MotionSpecs.PauseVeilAlpha, 1e-6)),
                ("PAUSA quieto en su disco", () => h.IsShown("hud-paused-label") && Hud(h).Callouts.Motion.Shown == CenterDisc.Paused),
                ("sin #hud-big-countdown", () => !h.IsShown("hud-big-countdown")),
                ("0:03", () => h.TextOf("hud-countdown") == "0:03"));
            all.Add(cap27);
        }

        // ── El diálogo de completado (CAP-07 a CAP-11, CAP-28) ──
        private static void AddCompletion(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, profile, language, textScale) in new[]
                     {
                         ("CAP-07", "CAP-07-dialogo-completado", DeviceProfile.TV, "es-AR", 1.0),
                         ("CAP-08", "CAP-08-dialogo-completado-en", DeviceProfile.TV, "en-US", 1.0),
                         ("CAP-09", "CAP-09-dialogo-completado-th", DeviceProfile.TH, "es-AR", 1.0),
                         ("CAP-10", "CAP-10-dialogo-completado-l640-letra", DeviceProfile.L640, "es-AR", 1.3),
                         ("CAP-11", "CAP-11-dialogo-completado-w1280", DeviceProfile.W1280, "es-AR", 1.0),
                     })
            {
                var english = language == "en-US";
                var scenario = Exr(id, stem, profile, RoutineSeeds.F4A, "Corta a los 32,0 s" + (english ? ", en inglés" : textScale > 1 ? ", letra 1,3" : ""),
                    s => Routine(s, "Corta", 32.0), language, textScale);
                scenario.Checks = h => Expect(
                    ("#completion-dialog", () => h.Completion != null && h.IsShown("completion-dialog")),
                    ("el título", () => h.Completion!.HeaderTitle.text == (english ? "Workout Completed!" : "¡Entrenamiento completado!")),
                    ("el check a la izquierda del título", () => h.Completion!.Check.worldBound.xMax <= h.Completion.HeaderTitle.worldBound.xMin + 0.5f),
                    ("00:30, 1120 m, 43 (" + Totals(h) + ")", () => h.Completion!.TimeValue.Text == "00:30" && h.Completion.DistanceValue.Text == "1120"
                                                && h.Completion.DistanceValue.SuffixText == "m" && h.Completion.CaloriesValue.Text == "43"),
                    ("Terminar a la vista", () => h.IsShown("completion-finish") && h.TextOf("completion-finish") == (english ? "Finish" : "Terminar")),
                    ("el diálogo entero en la ventana", () => Inside(h, "completion-dialog")),
                    ("la llave por encima del velo y fuera del diálogo", () => !h.IsShown("sim-open") || !Overlap(h.BoundsOf("sim-open"), h.BoundsOf("completion-dialog"))));
                all.Add(scenario);
            }

            var cap28 = Exr("CAP-28", "CAP-28-cierre-que-falla", DeviceProfile.TV, RoutineSeeds.F4A, "Corta con la base que falla al cerrar; Terminar del diálogo a los 32 s",
                s => Steps(Routine(s, "Corta", 32.0), s.Tap("completion-finish"), s.Settle()));
            cap28.Host.Sessions = HostSessions.Scripted(r => r.FailOnFinish = true);
            cap28.Checks = h => Expect(
                ("Workout en reposo", () => h.Immersive == null),
                ("el aviso No se pudo guardar la sesión", () => h.Shell.Toasts.CurrentText == "No se pudo guardar la sesión" && Inside(h, "toast")),
                ("ningún diálogo", () => h.TopDialog == null));
            all.Add(cap28);
        }

        // ── La barra libre (CAP-13 a CAP-18) ──
        private static void AddFreeBar(List<SnapshotScenario> all)
        {
            var cap13 = Exr("CAP-13", "CAP-13-barra-libre-tv", DeviceProfile.TV, RoutineSeeds.Lista, "Libre a los 42,2 s", s => Free(s, 42.2));
            cap13.Checks = h => FreeChecks(h, "Entrenamiento Libre", "EN MARCHA", true, -1);
            all.Add(cap13);

            var cap14 = Exr("CAP-14", "CAP-14-pausa-libre", DeviceProfile.TV, RoutineSeeds.Lista, "Libre: Pausa a los 42 s, toma a los 44 s",
                s => Steps(Free(s, 42.0), s.Do(h => h.Tap("hud-pause")), s.AdvanceTo(44.0)));
            cap14.Checks = h => Expect(
                ("el velo", () => Near(Hud(h).Backdrop.Veil, MotionSpecs.PauseVeilAlpha, 1e-6)),
                ("PAUSA en su disco", () => h.IsShown("hud-paused-label") && h.TextOf("hud-paused-label") == "PAUSA" && DiscRect(h, "hud-paused-label")),
                ("el chip PAUSADO sin punto", () => h.TextOf("hud-phase-label") == "PAUSADO" && !h.IsShown("hud-phase-dot")),
                ("#hud-resume", () => h.IsShown("hud-resume")),
                ("las tarjetas y la barra al 100 %", () => h.Find("hud-spm")!.resolvedStyle.opacity >= 0.999f && h.Find("hud-topbar")!.resolvedStyle.opacity >= 0.999f),
                ("#hud-time 00:42", () => h.TextOf("hud-time-value") == "00:42"));
            all.Add(cap14);

            var cap15 = Exr("CAP-15", "CAP-15-finalizado-guardando", DeviceProfile.TV, RoutineSeeds.Lista, "Libre a los 10 s con la base lenta, Terminar → Terminar, 1 s después",
                s => Steps(Free(s, 10.0), s.Do(h => h.Tap("hud-finish")), s.Settle(), s.Do(h => h.Tap("dialog-confirm")), s.Advance(1.0)));
            cap15.Host.Sessions = HostSessions.Scripted(r => r.WriteDelay = TimeSpan.FromSeconds(2));
            cap15.Checks = h => Expect(
                ("el chip FINALIZADO en #FF7A92", () => h.TextOf("hud-phase-label") == "FINALIZADO" && ColorIs(h.Find("hud-phase-label")!.resolvedStyle.color, Tokens.PhaseFinishedHud)),
                ("sin punto", () => !h.IsShown("hud-phase-dot")),
                ("ningún #completion-dialog", () => h.Completion == null));
            all.Add(cap15);

            var cap16 = Exr("CAP-16", "CAP-16-barra-libre-th", DeviceProfile.TH, RoutineSeeds.Lista, "Libre a los 42,2 s con pulso", s => Free(s, 42.2),
                reading: k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            cap16.Checks = h => FreeChecks(h, "Entrenamiento Libre", "EN MARCHA", true, 1);
            all.Add(cap16);

            var cap17 = Exr("CAP-17", "CAP-17-barra-libre-w1280", DeviceProfile.W1280, RoutineSeeds.Lista, "Libre a los 42,2 s con pulso, escritorio", s => Free(s, 42.2),
                reading: k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            cap17.Checks = h => FreeChecks(h, "Entrenamiento Libre", "EN MARCHA", true, 1);
            all.Add(cap17);

            var cap18 = Exr("CAP-18", "CAP-18-barra-libre-en", DeviceProfile.TV, RoutineSeeds.Lista, "Libre a los 42,2 s, en inglés", s => Free(s, 42.2), "en-US");
            cap18.Checks = h => FreeChecks(h, "Free Workout", "ACTIVE", true, 1);
            all.Add(cap18);
        }

        // ── La fila de información (CAP-19, CAP-20, CAP-31) ──
        private static void AddInfoRow(List<SnapshotScenario> all)
        {
            var cap19 = Exr("CAP-19", "CAP-19-fila-mas-larga", DeviceProfile.TV, RoutineSeeds.F4B, "Texto largo a los 8,2 s", s => Routine(s, "Texto largo", 8.2));
            cap19.Checks = h => Expect(
                ("Calentamiento", () => h.TextOf("hud-step-type") == "Calentamiento"),
                ("Próximo: Enfriamiento", () => h.IsShown("hud-up-next") && h.TextOf("hud-up-next-type")?.Trim() == "Enfriamiento"),
                ("0:12 y 10:00", () => h.TextOf("hud-countdown") == "0:12" && h.TextOf("hud-wall-clock") == "10:00"),
                ("sin pastilla", () => !h.IsShown("hud-step-target")),
                ("nada sale de la barra", () => h.BoundsOf("hud-wall-clock").xMax <= h.BoundsOf("hud-topbar").xMax + 0.5f));
            all.Add(cap19);

            var cap20 = Exr("CAP-20", "CAP-20-fila-mas-larga-en", DeviceProfile.TV, RoutineSeeds.F4B, "Texto largo a los 8,2 s, en inglés",
                s => Routine(s, "Texto largo", 8.2), "en-US");
            cap20.Checks = h => Expect(
                ("Warmup, Up next: Cooldown", () => h.TextOf("hud-step-type") == "Warmup" && h.TextOf("hud-up-next-type")?.Trim() == "Cooldown"),
                ("sin pastilla", () => !h.IsShown("hud-step-target")),
                ("la cuenta y el reloj enteros", () => h.BoundsOf("hud-wall-clock").xMax <= h.BoundsOf("hud-topbar").xMax + 0.5f));
            all.Add(cap20);

            var cap31 = Exr("CAP-31", "CAP-31-pastilla-por-fragmentos", DeviceProfile.TV, RoutineSeeds.F4B, "Texto largo a los 2,2 s (sin Próximo todavía)",
                s => Routine(s, "Texto largo", 2.2));
            cap31.Checks = h => Expect(
                ("Calentamiento", () => h.TextOf("hud-step-type") == "Calentamiento"),
                ("fragmentos enteros con el … pegado", () => Hud(h).Steps!.PillText.EndsWith(StepInfoBar.Ellipsis, StringComparison.Ordinal)
                                                         && !Hud(h).Steps!.PillText.Contains(">15…")),
                ("0:18 y 10:00", () => h.TextOf("hud-countdown") == "0:18" && h.TextOf("hud-wall-clock") == "10:00"),
                ("nada sale de la barra", () => h.BoundsOf("hud-wall-clock").xMax <= h.BoundsOf("hud-topbar").xMax + 0.5f));
            all.Add(cap31);
        }

        // ── Horizontal, L640 y escritorio (CAP-21, CAP-23 a CAP-26, CAP-29, CAP-30) ──
        private static void AddLandscape(List<SnapshotScenario> all)
        {
            var cap21 = Exr("CAP-21", "CAP-21-prueba-4a-l640", DeviceProfile.L640, RoutineSeeds.F4A, "Prueba 4a a los 150,2 s con R-4(23)",
                s => Routine(s, "Prueba 4a", 150.2), reading: RowerScripts.R4Spm(23));
            cap21.Checks = h => Concat(Looks(h, StepPillLook.Solid, StepPillLook.Hollow, StepPillLook.Solid, StepPillLook.Current, StepPillLook.Pending, StepPillLook.Pending),
                Expect(("relleno 0,503 ± 0,005", () => Near(Row(h).Fill, 0.503, 0.005)),
                    ("SPM y Watts fuera con ▲", () => Hud(h).Spm.ArrowShown && Hud(h).Watts.ArrowShown && Hud(h).Watts.ArrowGlyph == HudCard.Arrow)));
            all.Add(cap21);

            var cap23 = Exr("CAP-23", "CAP-23-prueba-4a-th", DeviceProfile.TH, RoutineSeeds.F4A, "Prueba 4a a los 50,2 s", s => Routine(s, "Prueba 4a", 50.2));
            cap23.Checks = h => Expect(("6 píldoras del mismo ancho", () => Row(h).Count == 6 && EqualWidths(h)),
                ("la 1 en curso con relleno 0,337 ± 0,005", () => Row(h).LookAt(1) == StepPillLook.Current && Near(Row(h).Fill, 0.337, 0.005)));
            all.Add(cap23);

            var cap24 = Exr("CAP-24", "CAP-24-prueba-4a-w1280", DeviceProfile.W1280, RoutineSeeds.F4A, "Prueba 4a a los 50,2 s, escritorio", s => Routine(s, "Prueba 4a", 50.2));
            cap24.Checks = h => Expect(("6 píldoras", () => Row(h).Count == 6 && EqualWidths(h)),
                ("la cuenta y el reloj a la derecha", () => h.BoundsOf("hud-wall-clock").xMin >= h.BoundsOf("hud-countdown").xMax - 0.5f));
            all.Add(cap24);

            var cap25 = Exr("CAP-25", "CAP-25-cuenta-grande-th", DeviceProfile.TH, RoutineSeeds.F4B, "Estados a los 47,6 s", s => Routine(s, "Estados", 47.6));
            cap25.Checks = h => Expect(("la cuenta grande 3", () => h.TextOf("hud-big-countdown") == "3" && h.IsShown("hud-big-countdown")),
                ("en su disco, fuera de la zona del bote", () => DiscCentered(h) && Hud(h).Callouts.Disc.Bottom <= Layout(h).BoatZone.Top + 0.5));
            all.Add(cap25);

            var cap26 = Exr("CAP-26", "CAP-26-sello-th", DeviceProfile.TH, RoutineSeeds.F4B, "Estados a los 20,6 s", s => Routine(s, "Estados", 20.6));
            cap26.Checks = h => Expect(("No alcanzado", () => Seal(h).IsShowing && Seal(h).Title.text == "No alcanzado"));
            all.Add(cap26);

            var cap29 = Exr("CAP-29", "CAP-29-cuenta-grande-l640", DeviceProfile.L640, RoutineSeeds.F4B, "Estados a los 47,6 s", s => Routine(s, "Estados", 47.6));
            cap29.Checks = h => Expect(("la cuenta grande sin tapar tarjetas ni la zona del bote", () => h.IsShown("hud-big-countdown") && DiscCentered(h)
                                                                                                      && Hud(h).Callouts.Disc.Bottom <= Layout(h).BoatZone.Top + 0.5));
            all.Add(cap29);

            var cap30 = Exr("CAP-30", "CAP-30-sello-l640", DeviceProfile.L640, RoutineSeeds.F4B, "Estados a los 20,6 s", s => Routine(s, "Estados", 20.6));
            cap30.Checks = h => Expect(("el sello en una línea", () => Seal(h).IsShowing && Math.Abs(Seal(h).Title.worldBound.center.y - Seal(h).Detail.worldBound.center.y) < 2),
                ("entre la barra y las tarjetas", () => Seal(h).Title.worldBound.yMax <= h.BoundsOf("hud-spm").yMin));
            all.Add(cap30);
        }

        // ── La demo (DEMO-01 a -12, una secuencia; §5.4) ──
        private static SnapshotScenario Demo()
        {
            var demo = Exr("DEMO", "DEMO", DeviceProfile.W1280, RoutineSeeds.F4A, "La demo de la It. 4b", DemoSteps, simulator: true);
            demo.Kind = SnapshotKind.Sequence;
            foreach (var (id, stem, state) in new[]
                     {
                         ("DEMO-01", "DEMO-01-workout", "Workout en reposo"),
                         ("DEMO-02", "DEMO-02-libre", "Libre a los 3,2 s"),
                         ("DEMO-03", "DEMO-03-pausa", "Pausa y 0,5 s"),
                         ("DEMO-04", "DEMO-04-terminado", "Reanudar, Terminar, Terminar; 800 ms de la salida"),
                         ("DEMO-05", "DEMO-05-prueba-4a", "Prueba 4a a los 18,2 s"),
                         ("DEMO-06", "DEMO-06-cuenta-grande", "A los 27,6 s"),
                         ("DEMO-07", "DEMO-07-sello", "A los 90,6 s"),
                         ("DEMO-08", "DEMO-08-panel", "El panel abierto a los 101,6 s"),
                         ("DEMO-09", "DEMO-09-cumplido", "A los 180,6 s"),
                         ("DEMO-10", "DEMO-10-cuenta-final", "A los 239,2 s"),
                         ("DEMO-11", "DEMO-11-completado", "A los 242 s"),
                         ("DEMO-12", "DEMO-12-reposo", "Terminar del diálogo; 800 ms de la salida"),
                     })
            {
                demo.Shot(id, stem, state);
            }

            return demo;
        }

        private static IEnumerator DemoSteps(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Shot("DEMO-01", h => Expect(("Workout en reposo con Entrenamiento libre y Prueba 4a", () => h.Immersive == null && h.IsShown("workout-free"))));

            yield return s.StartFree();
            yield return s.AdvanceTo(3.2);
            yield return s.Shot("DEMO-02", h => Expect(("el HUD entero", () => h.Immersive != null && !h.Immersive.Cascade.IsRunning),
                ("10:00 y EN MARCHA con el punto", () => h.TextOf("hud-wall-clock") == "10:00" && h.TextOf("hud-phase-label") == "EN MARCHA" && h.IsShown("hud-phase-dot"))));

            yield return s.Do(h => h.Tap("hud-pause"));
            yield return s.Advance(0.5);
            yield return s.Shot("DEMO-03", h => Expect(("el velo, PAUSA, PAUSADO y Reanudar", () => Near(Hud(h).Backdrop.Veil, MotionSpecs.PauseVeilAlpha, 1e-6)
                && h.IsShown("hud-paused-label") && h.TextOf("hud-phase-label") == "PAUSADO" && h.IsShown("hud-resume"))));

            yield return s.Tap("hud-resume");
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Shot("DEMO-04", h => Expect(("Workout en reposo, sin diálogo ni aviso", () => h.Immersive == null && h.TopDialog == null && !h.Shell.Toasts.IsVisible)));

            yield return s.StartRoutine("Prueba 4a");
            yield return s.AdvanceTo(18.2);
            yield return s.Shot("DEMO-05", h => Expect(("6 píldoras", () => Row(h).Count == 6),
                ("la amarilla con relleno 0,607 ± 0,005", () => Near(Row(h).Fill, 0.607, 0.005)),
                ("Calentamiento, Próximo: Trabajo, 0:12", () => h.TextOf("hud-step-type") == "Calentamiento" && h.IsShown("hud-up-next") && h.TextOf("hud-countdown") == "0:12")));

            yield return s.AdvanceTo(27.6);
            yield return s.Shot("DEMO-06", h => Expect(("el halo de la 0 y la cuenta grande 3", () => Row(h).HaloAlpha > 0.29 && h.TextOf("hud-big-countdown") == "3")));

            yield return s.AdvanceTo(90.6);
            yield return s.Shot("DEMO-07", h => Expect(("No alcanzado", () => Seal(h).IsShowing && Seal(h).Title.text == "No alcanzado"),
                ("la 1 hueca", () => Row(h).LookAt(1) == StepPillLook.Hollow)));

            yield return s.AdvanceTo(100);
            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-watts-plus");
            yield return s.TapInView("sim-watts-plus");
            yield return s.TapInView("sim-spm-plus");
            yield return s.TapInView("sim-spm-plus");
            yield return s.AdvanceTo(101.6);
            yield return s.Shot("DEMO-08", h => Expect(("el panel abierto", () => h.IsShown("sim-panel")),
                ("Split, Watts y la barra a la vista", () => !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-split")) && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-watts"))
                                                            && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-topbar")))));

            yield return s.Tap("sim-close");
            yield return s.AdvanceTo(180.6);
            yield return s.Shot("DEMO-09", h => Expect(("Objetivo cumplido", () => Seal(h).IsShowing && Seal(h).Title.text == "Objetivo cumplido"),
                ("la 3 sólida", () => Row(h).LookAt(3) == StepPillLook.Solid)));

            yield return s.AdvanceTo(239.2);
            yield return s.Shot("DEMO-10", h => Expect(("la cuenta grande 1", () => h.TextOf("hud-big-countdown") == "1"), ("sin Próximo", () => !h.IsShown("hud-up-next"))));

            yield return s.AdvanceTo(242);
            yield return s.Shot("DEMO-11", h => Expect(("el diálogo con 04:00", () => h.Completion != null && h.Completion.TimeValue.Text == "04:00"),
                ("detrás, la 5 llena en curso", () => Row(h).LookAt(5) == StepPillLook.Current)));

            yield return s.Tap("completion-finish");
            yield return s.Shot("DEMO-12", h => Expect(("Workout en reposo, sin aviso", () => h.Immersive == null && !h.Shell.Toasts.IsVisible)));
        }

        // ── Los videos (VID-01 a -08; §5.5) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Exr("VID-01", "VID-01-entrada-sumergirse", DeviceProfile.TV, RoutineSeeds.Lista, "Workout; Entrenamiento libre a los 0,25 s (2,5 s)",
                s => GoToWorkout(s));
            Video(vid01, 2.5).At(0.25, h => Start(h, "workout-free"));
            vid01.Video!.FrameValues = (h, t) => EntryValues(h);
            vid01.Video.FrameCheck = (h, t) => EntryCheck(h);
            all.Add(vid01);

            var vid02 = Exr("VID-02", "VID-02-salida-ultima-foto", DeviceProfile.TV, RoutineSeeds.Lista, "Libre a los 12 s; Terminar → Terminar (2,5 s)", s => Free(s, 12));
            // QA-V4b-07: los valores por cuadro de la salida (ImmersiveExitMotion y la página) y su comparación con la línea de tiempo
            // pura (± 0,01), además de que nunca aparezca 00:00. La página que sale ya no es h.Immersive (PageStack.Top salta las que
            // salen): se guarda al tocar Terminar y se mira mientras sigue en el árbol.
            ImmersiveWorkoutPage? leaving = null;
            Video(vid02, 2.5).At(0.1, h =>
            {
                leaving = h.Immersive;
                h.Tap("hud-finish");
            }).At(0.5, h => h.Tap("dialog-confirm"));
            vid02.Video!.FrameValues = (h, t) => ExitValues(h, leaving);
            vid02.Video.FrameCheck = (h, t) => leaving != null && leaving.Root.panel != null && h.TextOf("hud-time-value") == "00:00" ? "00:00 en la salida" : ExitCheck(leaving);
            all.Add(vid02);

            var vid03 = Exr("VID-03", "VID-03-entrada-con-rutina", DeviceProfile.W1280, RoutineSeeds.F4A, "Workout → Prueba 4a → Iniciar (2,5 s)",
                s => s.TapRoutine("Prueba 4a"));
            Video(vid03, 2.5).At(0.25, h => Start(h, "dialog-confirm"));
            vid03.Video!.FrameValues = (h, t) => EntryValues(h);
            all.Add(vid03);

            var vid04 = Exr("VID-04", "VID-04-pildoras-cuenta-y-sello", DeviceProfile.TV, RoutineSeeds.F4B, "Estados de los 4,5 a los 23 s (18,5 s)",
                s => Routine(s, "Estados", 4.5));
            Video(vid04, 18.5);
            vid04.Video!.FrameValues = (h, t) => h.Immersive == null ? null
                : "fill=" + F(Row(h).Fill) + " halo=" + F(Row(h).HaloAlpha) + " count=" + (h.TextOf("hud-big-countdown") ?? "") + " ring=" + F(Hud(h).Callouts.Motion.RingFraction)
                  + " seal=" + F(Seal(h).Motion.Current.BarHeight);
            vid04.Video.FrameCheck = (h, t) =>
            {
                if (h.Immersive == null || !h.IsShown("hud-big-countdown") || Hud(h).Callouts.Motion.Shown != CenterDisc.Count)
                {
                    // Mientras la cuenta "se suelta" (200 ms) se ve la última cifra: el dato ya cambió.
                    return null;
                }

                var countdown = h.TextOf("hud-countdown") ?? "";
                var big = h.TextOf("hud-big-countdown") ?? "";
                return countdown == "0:0" + big ? null : "la cuenta grande " + big + " y la de la barra " + countdown + " no cambian en el mismo cuadro";
            };
            all.Add(vid04);

            var vid05 = Exr("VID-05", "VID-05-fin-y-dialogo", DeviceProfile.TV, RoutineSeeds.F4A, "Corta de los 26 a los 34 s; Terminar del diálogo a los 33 s (8 s)",
                s => Routine(s, "Corta", 26));
            Video(vid05, 8).At(7, h => h.Tap("completion-finish"));
            vid05.Video!.FrameValues = (h, t) => h.Completion == null ? null
                : "check=" + F(h.Completion.Check.DiscScale) + "/" + F(h.Completion.Check.Stroke) + " time=" + h.Completion.TimeValue.Text + " distance="
                  + h.Completion.DistanceValue.Text + " kcal=" + h.Completion.CaloriesValue.Text;
            vid05.Video.FrameCheck = (h, t) =>
            {
                var dialog = h.Completion;
                if (dialog == null || dialog.IsClosing)
                {
                    return null;
                }

                var expected = CountUp.ValueAt(dialog.Completion.TimeValue, dialog.Motion.ElapsedMs, CountUp.StartMs(0));
                return dialog.TimeValue.Text == WorkoutFormats.SessionClock(expected) ? null : "el conteo del tiempo no sigue CountUp (" + dialog.TimeValue.Text + ")";
            };
            all.Add(vid05);

            var vid06 = Exr("VID-06", "VID-06-pausa-y-velo", DeviceProfile.TV, RoutineSeeds.Lista, "Libre: Pausa a 1 s, Reanudar a 3 s (5 s)", s => Free(s, 3));
            Video(vid06, 5).At(1, h => h.Tap("hud-pause")).At(3, h => h.Tap("hud-resume"));
            vid06.Video!.FrameValues = (h, t) => h.Immersive == null ? null : "veil=" + F(Hud(h).Backdrop.Veil) + " chip=" + h.TextOf("hud-phase-label");
            all.Add(vid06);

            var vid07 = Exr("VID-07", "VID-07-kcal-que-se-desliza", DeviceProfile.TV, RoutineSeeds.Lista, "Libre con pulso; llave → Simular caída del pulsómetro a 1 s (6 s)",
                s => Steps(ConnectHrm(s), Free(s, 3), s.Tap("sim-open"), s.ScrollTo("sim-drop-hrm")), simulator: true);
            Video(vid07, 6).At(1, h => h.Tap("sim-drop-hrm"));
            vid07.Video!.FrameValues = (h, t) => h.Immersive == null ? null : "kcalShift=" + F(Hud(h).KcalSlide.ShiftDp) + " bpm=" + F(Hud(h).KcalSlide.Current.BpmOpacity);
            all.Add(vid07);

            var vid08 = Exr("VID-08", "VID-08-entrada-reducir-movimiento", DeviceProfile.TV, RoutineSeeds.Lista, "VID-01 con reducir movimiento (2 s)",
                s => GoToWorkout(s));
            vid08.Host.ReduceMotion = true;
            Video(vid08, 2).At(0.25, h => Start(h, "workout-free"));
            vid08.Video!.FrameValues = (h, t) => EntryValues(h);
            all.Add(vid08);
        }

        private static VideoSpec Video(SnapshotScenario scenario, double seconds)
        {
            scenario.Kind = SnapshotKind.Video;
            scenario.Video = new VideoSpec(seconds);
            return scenario.Video;
        }

        // El toque que empieza la sesión en un video, con la hora de pared de la sesión (§5.2 punto 5).
        private static void Start(UiTestHost h, string element)
        {
            var shift = UiHostOptions.WorkoutStartUtc - h.Scheduler.UtcNow;
            h.Scheduler.ShiftUtc(shift);
            h.Tap(element);
        }

        private static string? EntryValues(UiTestHost h)
        {
            var page = h.Immersive;
            if (page == null)
            {
                return null;
            }

            var cascade = page.Cascade;
            return "page=" + F(page.Root.resolvedStyle.opacity) + " backdrop=" + F(cascade.BackdropOpacity) + " topbar=" + F(cascade.PieceOpacity(HudPiece.TopBar))
                   + " spm=" + F(cascade.PieceOpacity(HudPiece.Spm)) + " controls=" + F(cascade.PieceOpacity(HudPiece.Controls)) + " t=" + cascade.ElapsedMs;
        }

        // La salida (VID-02): el HUD, el fondo y la página (su opacidad, la de PageTransition); antes de la salida, el reloj y el chip.
        private static string? ExitValues(UiTestHost h, ImmersiveWorkoutPage? page)
        {
            if (page == null || page.Root.panel == null)
            {
                return null;
            }

            if (!page.IsExiting)
            {
                return "time=" + h.TextOf("hud-time-value") + " chip=" + h.TextOf("hud-phase-label");
            }

            var exit = page.Exit.Current;
            return "hud=" + F(exit.Hud) + " backdrop=" + F(exit.Backdrop) + " page=" + F(page.Root.style.opacity.value) + " time=" + h.TextOf("hud-time-value")
                   + " chip=" + h.TextOf("hud-phase-label") + " t=" + page.Exit.ElapsedMs;
        }

        // La salida con la línea de tiempo pura en el mismo instante (± 0,01): el HUD, el fondo y la opacidad de la página.
        private static string? ExitCheck(ImmersiveWorkoutPage? page)
        {
            if (page == null || page.Root.panel == null || !page.IsExiting || !page.Exit.IsRunning)
            {
                return null;
            }

            var t = page.Exit.ElapsedMs;
            var expected = ImmersiveTimeline.Exit(t, ExitStart.Full, false);
            var actual = page.Exit.Current;
            var root = page.Root.style.opacity.value;
            if (Math.Abs(actual.Hud - expected.Hud) > 0.01 || Math.Abs(actual.Backdrop - expected.Backdrop) > 0.01 || Math.Abs(root - expected.Page) > 0.01)
            {
                return "la salida en hud " + F(actual.Hud) + ", fondo " + F(actual.Backdrop) + ", página " + F(root) + " (la línea de tiempo dice " + F(expected.Hud) + ", "
                       + F(expected.Backdrop) + ", " + F(expected.Page) + " a los " + t + " ms)";
            }

            return null;
        }

        // Cada pieza con el valor de su línea de tiempo pura en el mismo instante (± 0,01).
        private static string? EntryCheck(UiTestHost h)
        {
            var page = h.Immersive;
            if (page == null || !page.Cascade.IsRunning)
            {
                return null;
            }

            var t = page.Cascade.ElapsedMs;
            foreach (var piece in new[] { HudPiece.TopBar, HudPiece.Spm, HudPiece.SplitWatts, HudPiece.Band, HudPiece.Vitals, HudPiece.Controls })
            {
                var expected = ImmersiveTimeline.Entry(piece, t, false).Opacity;
                if (Math.Abs(page.Cascade.PieceOpacity(piece) - expected) > 0.01)
                {
                    return piece + " en " + F(page.Cascade.PieceOpacity(piece)) + " (la línea de tiempo dice " + F(expected) + " a los " + t + " ms)";
                }
            }

            return null;
        }

        // ── Armado ──

        private static SnapshotScenario Exr(string id, string stem, DeviceProfile profile, RoutineSeed seed, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                            string language = "es-AR", double textScale = 1, Func<int, RowingData>? reading = null, bool simulator = false)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = simulator ? HostBackends.Simulator(false) : HostBackends.TestRower(reading ?? RowerScripts.R4),
                SimulatorLayer = simulator ? SimulatorLayerMode.Full : SimulatorLayerMode.ChromeOnly,
                LanguageTags = new[] { language },
                TextScale = textScale,
                Routines = HostRoutines.Seeded(seed),
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = simulator ? "Sim" : "R-4", ExactInstant = true };
        }

        private static IEnumerator Routine(ScenarioScript s, string name, double at)
        {
            yield return s.Tap("tab-workout");
            yield return s.Do(h => ScrollToRoutine(h, name));
            yield return s.StartRoutine(name);
            yield return s.AdvanceTo(at);
        }

        // La tarjeta de la rutina a la vista (con letra 1,3 en L640 queda debajo de la barra de pestañas).
        private static void ScrollToRoutine(UiTestHost h, string name)
        {
            var list = h.WorkoutScreen?.List;
            if (list == null)
            {
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var card = list.CardAt(i);
                if (card != null && card.Routine.Name == name)
                {
                    h.ScrollTo("workout-routine-" + i.ToString(CultureInfo.InvariantCulture));
                    return;
                }
            }
        }

        private static IEnumerator Free(ScenarioScript s, double at)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(at);
        }

        private static IEnumerator GoToWorkout(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
        }

        // El pulsómetro simulado desde Dispositivo (Buscar y la fila del sensor, como en la It. 2).
        private static IEnumerator ConnectHrm(ScenarioScript s)
        {
            yield return s.TapInView("hrm-search");
            yield return s.TapInView(SnapshotCatalog.HrmRow);
        }

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        // ── Las sondas de aspecto (WP-D2; 04-design-notes §7.1 y §7.2; DN4b-11, DN4b-28) ──
        // Los colores esperados salen de los tokens, compuestos en sRGB como la tubería GammaRt (sin literales: ADR 016), sobre el
        // vidrio del HUD encima del cielo provisorio: la barra de pasos, la libre y el disco del centro alto quedan sobre el cielo en
        // los cinco perfiles. Tolerancias de §7.1: fondos ± 1 por canal; trazos, bordes finos y el brillo ± 3; el halo ± 4 (es un
        // degradé). "Al centro" de una píldora es su centro en x y en y; el borde de la hueca se mide a 0,75·s de su borde de
        // arriba; el brillo, en el medio de su franja de 1·s (≈ 1 px debajo del borde de arriba del relleno). Las sondas no hacen
        // fallar la toma (aspect: true), salvo las dos que §5.3 pone entre las comprobaciones: el divisor de la banda en CAP-13
        // (DD4b-01, ± 2) y el "…" blanco de la pastilla en CAP-31 (DD4b-03, ± 3). La DEMO es una secuencia (sin sondas) y los videos
        // se revisan cuadro por cuadro (04-design-notes §7.4).
        private const int BackgroundTolerance = 1;
        private const int StrokeTolerance = 3;
        // El halo es un degradé: en TV y TH cambia ≈ 9 por canal de un píxel al siguiente a 1·s de la píldora, y el punto de la
        // sonda cae a ± ½ px de esa distancia (el redondeo del píxel). Con ± 4 (04-design-notes §7.1, primera versión) fallaba por
        // el píxel de al lado con el perfil bien (medido en la columna de CAP-02: 0,424 en el primer píxel afuera, 0,346 a 1·s,
        // 0 a los 8·s); ± 10 sigue separando "sin halo" (80), el halo sin latido (46) y un aro o un corte.
        private const int GlowTolerance = 10;

        private static void AddAspectProbes(List<SnapshotScenario> all)
        {
            var byId = new Dictionary<string, SnapshotScenario>(StringComparer.Ordinal);
            foreach (var scenario in all)
            {
                byId[scenario.Id] = scenario;
            }

            var sky = Tokens.BackdropSky.Rgb.ToHex();
            var water = Tokens.BackdropWater.Rgb.ToHex();
            var glass = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky);
            var glassWater = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropWater);
            var work = Tokens.StepWork;
            var rest = Tokens.StepRest;
            var warmup = Tokens.StepWarmup;
            var cooldown = Tokens.StepCooldown;

            // Las píldoras, el halo y la fila (CAP-01 a CAP-06, CAP-12, CAP-21, CAP-22, CAP-24).
            Aspect(byId["CAP-01"],
                PillPoint("0 sólida step.work @85 %", 0, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("1 hueca: interior step.work @18 %", 1, 0.5f, Pending(work, glass), BackgroundTolerance),
                PillBorder("1 hueca: borde step.work @85 %", 1, HollowBorder(work, glass)),
                PillPoint("2 sólida step.rest @85 %", 2, 0.5f, Done(rest, glass), BackgroundTolerance),
                PillPoint("3 relleno step.work (al 25 %)", 3, 0.25f, work.Rgb.ToHex(), BackgroundTolerance),
                PillHighlight("3 brillo blanco @25 % sobre el relleno", 3, 0.25f, Highlight(work)),
                PillPoint("3 pista step.work @30 % (al 80 %)", 3, 0.8f, Track(work, glass), BackgroundTolerance),
                PillPoint("4 pendiente step.rest @18 %", 4, 0.5f, Pending(rest, glass), BackgroundTolerance),
                PixelProbe.Point("vidrio de la fila", "hud-topbar", new Vector2(0.5f, 0.92f), glass, BackgroundTolerance),
                PixelProbe.Point("pastilla step.work @20 %", "hud-step-target", new Vector2(0.05f, 0.5f), Over(work, 0.20, glass), BackgroundTolerance),
                ClockIcon(glass));

            foreach (var id in new[] { "CAP-02", "CAP-25", "CAP-29" })
            {
                Aspect(byId[id],
                    PillHalo("halo a 1·s arriba de la 3 (α 0,70)", 3, HaloAbove(work, glass)),
                    PixelProbe.PointIn("disco glass.fallback", "hud-big-countdown", CountDisc, new Vector2(0.5f, 0.12f), glass, BackgroundTolerance),
                    PixelProbe.PointIn("arco step.work (a las 6)", "hud-big-countdown", CountDisc, RingPoint(180), work.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.PointIn("pista del aro step.work @20 % (a los 36°)", "hud-big-countdown", CountDisc, RingPoint(36),
                        Over(work, HudGeometry.BigCountRingTrackAlpha, glass), StrokeTolerance),
                    PixelProbe.Dominant("cifra text.primary", "hud-big-countdown", Tokens.TextPrimary.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.Dominant("cuenta step.work.hud", "hud-countdown", Tokens.StepWorkHud.Rgb.ToHex(), StrokeTolerance));
            }

            Aspect(byId["CAP-03"],
                PixelProbe.Dominant("título del sello state.success", "hud-seal-title", Tokens.StateSuccess.Rgb.ToHex(), StrokeTolerance),
                PixelProbe.Dominant("disco del ícono state.success", "hud-seal-icon", Tokens.StateSuccess.Rgb.ToHex(), StrokeTolerance),
                PixelProbe.Dominant("detalle text.primary", "hud-seal-text", Tokens.TextPrimary.Rgb.ToHex(), StrokeTolerance),
                PixelProbe.Point("vidrio de la fila del sello", "hud-topbar", new Vector2(0.03f, (float)(70 / HudGeometry.TopBarHeightSeal)), glass, BackgroundTolerance),
                PixelProbe.Point("cielo entre la barra y las tarjetas", "hud-backdrop", SkyGap, sky, BackgroundTolerance),
                PillPoint("0 sólida step.work @85 %", 0, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("1 pista step.work @30 %", 1, 0.8f, Track(work, glass), BackgroundTolerance));

            foreach (var id in new[] { "CAP-04", "CAP-26", "CAP-30" })
            {
                Aspect(byId[id],
                    PixelProbe.Dominant("título del sello phase.paused", "hud-seal-title", Tokens.PhasePaused.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.Dominant("aro del ícono phase.paused", "hud-seal-icon-missed", Tokens.PhasePaused.Rgb.ToHex(), StrokeTolerance),
                    PillPoint("1 hueca: interior step.work @18 %", 1, 0.5f, Pending(work, glass), BackgroundTolerance));
            }

            // El borde de la hueca, en TV y TH (3,9 px). En L640 (1 px por dp) mide 1,4 px y ningún píxel queda entero: se mira en
            // la toma, no con una sonda (lo mismo en CAP-21).
            Aspect(byId["CAP-04"], PillBorder("1 hueca: borde step.work @85 %", 1, HollowBorder(work, glass)));
            Aspect(byId["CAP-26"], PillBorder("1 hueca: borde step.work @85 %", 1, HollowBorder(work, glass)));

            Aspect(byId["CAP-05"],
                PillPoint("0 sólida step.warmup @85 % (a los 3/4: la cinta tapa el comienzo)", 0, 0.75f, Done(warmup, glass), BackgroundTolerance),
                PillPoint("1 sólida step.work @85 %", 1, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("2 en curso, la pista sola (Q-51)", 2, 0.5f, Track(work, glass), BackgroundTolerance),
                PillPoint("3 descanso pendiente", 3, 0.5f, Pending(rest, glass), BackgroundTolerance));

            Aspect(byId["CAP-06"],
                PillPoint("3 relleno step.rest al 100 %", 3, 0.5f, rest.Rgb.ToHex(), BackgroundTolerance),
                PillHighlight("3 brillo blanco @25 % (la llena en curso no es la sólida)", 3, 0.5f, Highlight(rest)),
                PillPoint("0 sólida step.work @85 %", 0, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("2 sólida step.work @85 %", 2, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("1 sólida step.rest @85 %", 1, 0.5f, Done(rest, glass), BackgroundTolerance));

            Aspect(byId["CAP-12"],
                PillPoint("1 en curso por distancia: la pista sola", 1, 0.5f, Track(work, glass), BackgroundTolerance),
                PillPoint("0 sólida step.work @85 %", 0, 0.5f, Done(work, glass), BackgroundTolerance));

            Aspect(byId["CAP-21"],
                PillPoint("0 sólida step.warmup @85 %", 0, 0.5f, Done(warmup, glass), BackgroundTolerance),
                PillPoint("1 hueca: interior step.work @18 %", 1, 0.5f, Pending(work, glass), BackgroundTolerance),
                PillPoint("2 sólida step.rest @85 %", 2, 0.5f, Done(rest, glass), BackgroundTolerance),
                PillPoint("4 pendiente step.rest @18 %", 4, 0.5f, Pending(rest, glass), BackgroundTolerance),
                PillPoint("5 pendiente step.cooldown @18 %", 5, 0.5f, Pending(cooldown, glass), BackgroundTolerance));

            Aspect(byId["CAP-22"],
                PillPoint("tramo 98 sólido step.work @85 %", 98, 0.5f, Done(work, glass), BackgroundTolerance),
                PillPoint("tramo 97 sólido step.rest @85 % (el pulgar tapa el centro de la 99)", 97, 0.5f, Done(rest, glass), BackgroundTolerance),
                PillPoint("pulgar: relleno step.work (en su cuarto izquierdo)", 100, 0.25f, work.Rgb.ToHex(), BackgroundTolerance),
                PillPoint("pulgar: pista step.work @30 % (en su cuarto derecho)", 100, 0.75f, Track(work, glass), BackgroundTolerance),
                PillPoint("tramo 150 pendiente step.work @18 %", 150, 0.5f, Pending(work, glass), BackgroundTolerance),
                PillPoint("tramo 151 pendiente step.rest @18 %", 151, 0.5f, Pending(rest, glass), BackgroundTolerance));

            Aspect(byId["CAP-24"],
                PillPoint("0 sólida step.warmup @85 %", 0, 0.5f, Done(warmup, glass), BackgroundTolerance),
                PillPoint("1 pista step.work @30 % (al 80 %)", 1, 0.8f, Track(work, glass), BackgroundTolerance));

            // El diálogo de completado (CAP-07 a CAP-09 y CAP-11; CAP-10 solo con Audit) y el aviso del cierre que falla (CAP-28).
            var scrim = MotionSpecs.DialogScrimOpacity;
            Aspect(byId["CAP-07"],
                PixelProbe.Point("velo sobre la barra", "hud-topbar", new Vector2(0.5f, 0.92f), Over(Tokens.OverlayScrim, scrim, glass), BackgroundTolerance),
                PixelProbe.Point("velo sobre el cielo", "hud-backdrop", new Vector2(0.5f, 90.6f / 914.29f), Over(Tokens.OverlayScrim, scrim, sky), BackgroundTolerance),
                PixelProbe.Point("velo sobre el agua", "hud-backdrop", new Vector2(0.5f, 650f / 914.29f), Over(Tokens.OverlayScrim, scrim, water), BackgroundTolerance));
            foreach (var id in new[] { "CAP-07", "CAP-08", "CAP-09", "CAP-11" })
            {
                var box = Over(Tokens.TextPrimary, CompletionDialog.SummaryAlpha, Tokens.SurfaceOverlay.Rgb.ToHex());
                Aspect(byId[id],
                    PixelProbe.Point("diálogo surface.overlay", "completion-dialog", new Vector2(0.5f, 0.97f), Tokens.SurfaceOverlay.Rgb.ToHex(), BackgroundTolerance),
                    PixelProbe.Point("caja blanco @4 %", "completion-time", new Vector2(0.45f, 0.5f), box, BackgroundTolerance),
                    PixelProbe.Point("disco del check state.success", "completion-check", new Vector2(0.5f, 0.15f), Tokens.StateSuccess.Rgb.ToHex(), BackgroundTolerance),
                    PixelProbe.Point("trazo del check surface.overlay", "completion-check", CheckStrokePoint(), Tokens.SurfaceOverlay.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.DominantIn("ícono de la fila accent.primary", "completion-time", RowIcon, Tokens.AccentPrimary.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.DominantIn("rótulo text.secondary", "completion-time", RowLabel, OverToken(Tokens.TextSecondary, box), StrokeTolerance),
                    PixelProbe.Dominant("valor text.primary", "completion-time-value", Tokens.TextPrimary.Rgb.ToHex(), StrokeTolerance),
                    PixelProbe.Point("Terminar accent.primary", "completion-finish", new Vector2(0.06f, 0.5f), Tokens.AccentPrimary.Rgb.ToHex(), BackgroundTolerance),
                    PixelProbe.Dominant("texto de Terminar text.onAccent", "completion-finish .rm-button__label", Tokens.TextOnAccent.Rgb.ToHex(), StrokeTolerance));
            }

            Aspect(byId["CAP-28"], PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.03f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), BackgroundTolerance));

            // La barra libre y el chip (CAP-13 a CAP-15, CAP-17, CAP-18).
            var running = Tokens.PhaseRunning;
            var dot = LiveDot.At(42200, false);
            foreach (var id in new[] { "CAP-13", "CAP-17", "CAP-18" })
            {
                // En W1280 (1 px por dp) el borde del chip mide 1,6 px y ningún píxel queda entero: va solo en TV.
                Aspect(byId[id], ChipProbes(running, running, glass, dot, id != "CAP-17"));
                Aspect(byId[id],
                    PixelProbe.Point("vidrio de la barra libre", "hud-topbar", new Vector2(0.5f, 0.12f), glass, BackgroundTolerance),
                    ClockIcon(glass));
            }

            // DD4b-01 (QA-V4a-03): el divisor de la banda, blanco @12 % sobre el vidrio y el agua. Comprobación de CAP-13 (§5.3).
            var divider = PixelProbe.Point("divisor de la banda blanco @12 %", "hud-band-divider", new Vector2(0.5f, 0.5f),
                Over(Tokens.TextPrimary, HudBand.DividerAlpha, glassWater), 2);
            byId["CAP-13"].WithProbe(divider);
            byId["CAP-17"].WithProbe(divider, true);
            byId["CAP-18"].WithProbe(divider, true);

            var veiledSky = Veiled(Tokens.BackdropSky);
            var veiledWater = Veiled(Tokens.BackdropWater);
            var glassVeiled = OverToken(Tokens.GlassFallback, veiledSky);
            var paused = Tokens.PhasePaused;
            Aspect(byId["CAP-14"],
                PixelProbe.Point("cielo con el velo", "hud-backdrop", new Vector2(0.5f, 72f / 914.29f), veiledSky, BackgroundTolerance),
                PixelProbe.Point("agua con el velo", "hud-backdrop", new Vector2(0.5f, 640f / 914.29f), veiledWater, BackgroundTolerance),
                PixelProbe.Point("vidrio de SPM sobre el velo", "hud-spm", new Vector2(0.03f, 0.5f), glassVeiled, BackgroundTolerance),
                PixelProbe.Point("Reanudar state.success", "hud-resume", new Vector2(0.06f, 0.5f), Tokens.StateSuccess.Rgb.ToHex(), BackgroundTolerance));
            Aspect(byId["CAP-14"], PausedDisc(glassVeiled));
            Aspect(byId["CAP-14"],
                PixelProbe.Point("chip PAUSADO: tinte @15 %", "hud-phase-chip", new Vector2(0.5f, 0.15f), Over(paused, HudGeometry.PhaseChipTintAlpha, glassVeiled), BackgroundTolerance),
                PixelProbe.Dominant("chip PAUSADO: texto phase.paused", "hud-phase-label", paused.Rgb.ToHex(), StrokeTolerance));

            Aspect(byId["CAP-15"], ChipProbes(Tokens.PhaseFinished, Tokens.PhaseFinishedHud, glass, -1, true));

            Aspect(byId["CAP-27"],
                PillHalo("halo a 1·s arriba de la 3 (sobre el velo)", 3, HaloAbove(work, glassVeiled)),
                PixelProbe.Dominant("cuenta step.work.hud", "hud-countdown", Tokens.StepWorkHud.Rgb.ToHex(), StrokeTolerance),
                PixelProbe.Point("cielo con el velo", "hud-backdrop", SkyGap, veiledSky, BackgroundTolerance));
            Aspect(byId["CAP-27"], PausedDisc(glassVeiled));

            // DD4b-03: el "…" de la pastilla cortada por fragmentos, en text.primary. Comprobación de CAP-31 (§5.3).
            byId["CAP-31"].WithProbe(PixelProbe.DominantIn("el … blanco de la pastilla", "hud-step-target", PillEnd, Tokens.TextPrimary.Rgb.ToHex(), StrokeTolerance));
        }

        private static void Aspect(SnapshotScenario scenario, params PixelProbe[] probes)
        {
            foreach (var probe in probes)
            {
                scenario.WithProbe(probe, true);
            }
        }

        // El chip de fase: el tinte @15 %, el borde @50 % sobre el tinte, el texto y, si dotOpacity ≥ 0, el punto en vivo con la
        // opacidad de su latido en el instante de la toma (LiveDot, la misma línea de tiempo pura que lo mueve).
        private static PixelProbe[] ChipProbes(ColorToken phase, ColorToken text, string glass, double dotOpacity, bool border)
        {
            var tint = Over(phase, HudGeometry.PhaseChipTintAlpha, glass);
            var probes = new List<PixelProbe>
            {
                PixelProbe.Point("chip: tinte @15 %", "hud-phase-chip", new Vector2(0.5f, 0.15f), tint, BackgroundTolerance),
                PixelProbe.Dominant("chip: texto " + text.Name, "hud-phase-label", text.Rgb.ToHex(), StrokeTolerance),
            };
            if (border)
            {
                probes.Add(PixelProbe.PointIn("chip: borde @50 % sobre el tinte", "hud-phase-chip", TopBorder, new Vector2(0.5f, 0.5f),
                    Over(phase, HudGeometry.PhaseChipBorderAlpha, tint), StrokeTolerance));
            }

            if (dotOpacity >= 0)
            {
                probes.Add(PixelProbe.Point("punto en vivo phase.running × " + F(dotOpacity), "hud-phase-dot", new Vector2(0.5f, 0.5f), Over(phase, dotOpacity, tint), StrokeTolerance));
            }

            return probes.ToArray();
        }

        // PAUSA en su disco (DN4b-14): el vidrio, el aro a las 12 en phase.paused y la barra izquierda del glifo, al centro.
        private static PixelProbe[] PausedDisc(string glassVeiled)
        {
            var d = (float)HudGeometry.BigCountDiameter;
            return new[]
            {
                PixelProbe.Point("disco de PAUSA glass.fallback", "hud-paused-label", new Vector2(0.5f, 0.15f), glassVeiled, BackgroundTolerance),
                PixelProbe.Point("aro de PAUSA phase.paused (a las 12)", "hud-paused-label", RingPoint(0), Tokens.PhasePaused.Rgb.ToHex(), StrokeTolerance),
                // Las barras: 4,67 × 16,33 separadas 4,67, de −14,5 a +1,8 desde el centro; la izquierda, centrada en (−4,67; −6,33).
                PixelProbe.Point("glifo de pausa text.primary (la barra izquierda)", "hud-paused-label", new Vector2(0.5f - 4.67f / d, 0.5f - 6.33f / d),
                    Tokens.TextPrimary.Rgb.ToHex(), StrokeTolerance),
            };
        }

        private static PixelProbe ClockIcon(string glass)
        {
            return PixelProbe.DominantIn("ícono del reloj text.primary @70 %", "hud-wall-clock", PixelProbe.PrefixOf,
                Over(Tokens.TextPrimary, HudGeometry.WallClockIconAlpha, glass), StrokeTolerance);
        }

        private static PixelProbe PillPoint(string name, int index, float rx, string hex, int tolerance)
        {
            return PixelProbe.PointIn(name, "hud-pill-row", e => PillRect(e, index, float.NaN, 0), new Vector2(rx, 0.5f), hex, tolerance);
        }

        // El borde de 1,5·s de la hueca, por dentro: en el medio de su franja (a 0,75·s de su borde de arriba).
        private static PixelProbe PillBorder(string name, int index, string hex)
        {
            return PixelProbe.PointIn(name, "hud-pill-row", e => PillRect(e, index, 0, (float)StepPills.MissedBorderWidth), new Vector2(0.5f, 0.5f), hex, StrokeTolerance);
        }

        private static PixelProbe PillHighlight(string name, int index, float rx, string hex)
        {
            return PixelProbe.PointIn(name, "hud-pill-row", e => PillRect(e, index, 0, (float)MotionSpecs.PillHighlightDp), new Vector2(rx, 0.5f), hex, StrokeTolerance);
        }

        // El halo, 1 base por arriba del borde de la píldora, en su centro en x.
        private static PixelProbe PillHalo(string name, int index, string hex)
        {
            return PixelProbe.PointIn(name, "hud-pill-row", e => PillRect(e, index, -1.5f, 1), new Vector2(0.5f, 0.5f), hex, GlowTolerance);
        }

        // La píldora n (el pulgar en las angostas) en coordenadas del panel; con bandTop, una franja desde su borde de arriba (dp base).
        private static Rect PillRect(VisualElement element, int index, float bandTop, float bandHeight)
        {
            if (!(element is PillRowView row) || index < 0 || index >= row.Count)
            {
                return Rect.zero;
            }

            var rect = row.RectOf(index);
            if (!float.IsNaN(bandTop))
            {
                rect = new Rect(rect.x, rect.y + bandTop, rect.width, bandHeight);
            }

            return row.LocalToWorld(rect);
        }

        // El disco de la cuenta grande: la placa que dibuja su TabularNumber, centrada en su caja, de 72 base × la escala del HUD.
        private static Rect CountDisc(VisualElement element)
        {
            var bounds = element.worldBound;
            var k = element.layout.height > 0 ? bounds.height / element.layout.height : 1;
            var d = (float)HudGeometry.BigCountDiameter * k;
            return new Rect(bounds.center.x - d / 2, bounds.center.y - d / 2, d, d);
        }

        // Un punto de la línea central del aro (a 34,5 base del centro) a "degrees" de las 12, en el sentido del reloj.
        private static Vector2 RingPoint(double degrees)
        {
            var r = HudGeometry.BigCountRingRadius / HudGeometry.BigCountDiameter;
            var a = degrees * Math.PI / 180;
            return new Vector2((float)(0.5 + r * Math.Sin(a)), (float)(0.5 - r * Math.Cos(a)));
        }

        // El medio del tramo largo del check (de P1 a P2, CheckGlyph), en la caja de 32 (radio 16, y hacia abajo).
        private static Vector2 CheckStrokePoint()
        {
            var x = (CheckGlyph.P1X + CheckGlyph.P2X) / 2;
            var y = (CheckGlyph.P1Y + CheckGlyph.P2Y) / 2;
            return new Vector2((float)(0.5 + x / 2), (float)(0.5 + y / 2));
        }

        // El cielo en el hueco entre SPM y Split, 4 dp arriba de las tarjetas (x 245 de 411): sobre SPM cae su halo de objetivo.
        private static readonly Vector2 SkyGap = new Vector2(245f / 411.43f, 90.6f / 914.29f);

        // El borde de arriba de un elemento (su ancho resuelto × la escala del elemento).
        private static Rect TopBorder(VisualElement element)
        {
            var bounds = element.worldBound;
            var k = element.layout.height > 0 ? bounds.height / element.layout.height : 1;
            return new Rect(bounds.x, bounds.y, bounds.width, element.resolvedStyle.borderTopWidth * k);
        }

        // El ícono de una fila del diálogo (los primeros 20 dp) y su rótulo (desde los 32 hasta la mitad de la fila).
        private static Rect RowIcon(VisualElement row)
        {
            var bounds = row.worldBound;
            return new Rect(bounds.x, bounds.y, CompletionDialog.RowIconDp, bounds.height);
        }

        private static Rect RowLabel(VisualElement row)
        {
            var bounds = row.worldBound;
            var start = CompletionDialog.RowIconDp + CompletionDialog.RowIconGapDp;
            return new Rect(bounds.x + start, bounds.y, bounds.width / 2 - start, bounds.height);
        }

        // Los últimos 7 dp del contenido de la pastilla: donde va el "…" pegado al último fragmento entero.
        private static Rect PillEnd(VisualElement pill)
        {
            var bounds = pill.worldBound;
            var k = pill.layout.width > 0 ? bounds.width / pill.layout.width : 1;
            var right = bounds.xMax - pill.resolvedStyle.paddingRight * k;
            return new Rect(right - 7 * k, bounds.y, 7 * k, bounds.height);
        }

        private static string Done(ColorToken step, string glass)
        {
            return Over(step, StepPills.DoneAlpha, glass);
        }

        private static string Pending(ColorToken step, string glass)
        {
            return Over(step, StepPills.PendingAlpha, glass);
        }

        private static string Track(ColorToken step, string glass)
        {
            return Over(step, StepPills.CurrentTrackAlpha, glass);
        }

        // El borde de la hueca (@85 %) sobre su interior (@18 %), como el Border del legacy sobre su color.
        private static string HollowBorder(ColorToken step, string glass)
        {
            return Over(step, StepPills.DoneAlpha, Pending(step, glass));
        }

        private static string Highlight(ColorToken step)
        {
            return Over(Tokens.TextPrimary, MotionSpecs.PillHighlightAlpha, step.Rgb.ToHex());
        }

        // El halo con α = 0,70 (p = 1) a 1 base del borde de arriba: 0,65·α·(1 − 1/8)² (DN4b-17).
        private static string HaloAbove(ColorToken step, string glass)
        {
            var falloff = 1 - 1 / MotionSpecs.PillGlowExtentTopDp;
            return Over(step, MotionSpecs.PillGlowEdgeFactor * MotionSpecs.PillGlowMax * falloff * falloff, glass);
        }

        // El fondo con el velo de pausa: el tinte de la textura, cada canal × (1 − 0,15).
        private static string Veiled(ColorToken backdrop)
        {
            var k = 1 - MotionSpecs.PauseVeilAlpha;
            var c = backdrop.Rgb;
            return new Rgb(Scale(c.R, k), Scale(c.G, k), Scale(c.B, k)).ToHex();
        }

        private static byte Scale(byte channel, double k)
        {
            return (byte)Math.Floor(channel * k + 0.5);
        }

        private static string OverToken(ColorToken top, string underHex)
        {
            return Over(top, top.Alpha, underHex);
        }

        // El color de un token con alfa sobre otro ya compuesto, en sRGB como la tubería GammaRt.
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
#endif

        // ── Comprobaciones ──

        private static IEnumerable<string> FreeChecks(UiTestHost h, string title, string chip, bool dot, double titleScale)
        {
            return Expect(
                ("el inmersivo libre", () => h.Immersive != null && Hud(h).Free != null),
                ("#hud-free-title " + title + " entero", () => h.TextOf("hud-free-title-text") == title && !Hud(h).Free!.TitleTruncated),
                // titleScale < 0: se achica, entre 0,8 y 1 (las notas daban 0,818 en TV con fontTools y sin aire; con el aire de 2 entre el
                // título y el reloj, ≈ 0,805: QA-V4b-02).
                (titleScale < 0 ? "achicado entre 0,8 y 1 (es " + F(Hud(h).Free!.TitleScale) + ")" : "a escala " + F(titleScale),
                    () => titleScale < 0 ? Hud(h).Free!.TitleScale >= 0.8 - 1e-6 && Hud(h).Free!.TitleScale < 1 : Near(Hud(h).Free!.TitleScale, titleScale, 0.005)),
                ("#hud-wall-clock 10:00", () => h.TextOf("hud-wall-clock") == "10:00"),
                ("#hud-phase-chip " + chip, () => h.TextOf("hud-phase-label") == chip),
                ("el punto en vivo", () => h.IsShown("hud-phase-dot") == dot),
                ("el chip de su ancho fijo", () => Near(h.BoundsOf("hud-phase-chip").width / (float)Layout(h).Scale, Hud(h).Free!.Chip.FixedWidth, 0.5)),
                // QA-V4b-02 (O-P03): el ancho fijo contra el de las notas (04-design-notes §3.5), no solo contra el que midió la vista.
                ("el ancho fijo de las notas (" + F(ChipWidthOfTheNotes(chip)) + " ± 1)", () => Near(Hud(h).Free!.Chip.FixedWidth, ChipWidthOfTheNotes(chip), 1)),
                ("el reloj y el chip enteros en la barra", () => h.BoundsOf("hud-phase-chip").xMax <= h.BoundsOf("hud-topbar").xMax + 0.5f
                                                                && h.BoundsOf("hud-wall-clock").xMin >= h.BoundsOf("hud-free-title").xMax - 0.5f),
                ("el título a " + F(FreeTopBar.TitleClockGapDp) + " o más del reloj (B-14, QA-V4b-02)", () => h.BoundsOf("hud-wall-clock").xMin - h.BoundsOf("hud-free-title").xMax
                                                                >= FreeTopBar.TitleClockGapDp * (float)Layout(h).Scale - 0.5f));
        }

        // El ancho fijo del chip en base (04-design-notes §3.5): 101,8 en español y 74,1 en inglés.
        private static double ChipWidthOfTheNotes(string chip)
        {
            return chip == "EN MARCHA" || chip == "PAUSADO" || chip == "FINALIZADO" ? 101.8 : 74.1;
        }

        private static IEnumerable<string> Looks(UiTestHost h, params StepPillLook[] looks)
        {
            var problems = new List<string>();
            var row = Row(h);
            if (row.Count != looks.Length)
            {
                problems.Add("no se cumple: " + looks.Length + " píldoras (son " + row.Count + ")");
                return problems;
            }

            for (var i = 0; i < looks.Length; i++)
            {
                if (row.LookAt(i) != looks[i])
                {
                    problems.Add("no se cumple: la " + i + " " + looks[i] + " (es " + row.LookAt(i) + ")");
                }
            }

            return problems;
        }

        private static bool EqualWidths(UiTestHost h)
        {
            var row = Row(h);
            var px = row.scaledPixelsPerPoint * (float)Layout(h).Scale;
            var first = row.RectOf(0).width * px;
            for (var i = 1; i < row.Count; i++)
            {
                if (Math.Abs(row.RectOf(i).width * px - first) > 1.01f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DiscCentered(UiTestHost h)
        {
            var disc = Hud(h).Callouts.Disc;
            var window = h.Root.worldBound;
            var box = h.BoundsOf("hud-big-countdown");
            return Near(box.center.x - window.xMin, disc.Left + disc.Width / 2, 1) && Near(box.center.y - window.yMin, disc.Top + disc.Height / 2, 1);
        }

        private static bool DiscRect(UiTestHost h, string name)
        {
            var disc = Hud(h).Callouts.Disc;
            var window = h.Root.worldBound;
            var box = h.BoundsOf(name);
            return Near(box.xMin - window.xMin, disc.Left, 0.5) && Near(box.yMin - window.yMin, disc.Top, 0.5) && Near(box.width, disc.Width, 0.5);
        }

        // Los totales que muestra el diálogo (para el mensaje de la comprobación).
        private static string Totals(UiTestHost h)
        {
            var dialog = h.Completion;
            return dialog == null ? "sin diálogo" : "es " + dialog.TimeValue.Text + ", " + dialog.DistanceValue.Text + " " + dialog.DistanceValue.SuffixText + ", " + dialog.CaloriesValue.Text;
        }

        private static HudView Hud(UiTestHost h)
        {
            return h.Immersive?.View ?? throw new InvalidOperationException("no hay inmersivo");
        }

        private static PillRowView Row(UiTestHost h)
        {
            return Hud(h).Steps?.PillRow ?? throw new InvalidOperationException("no hay barra de pasos");
        }

        private static SealRow Seal(UiTestHost h)
        {
            return Hud(h).Steps?.Seal ?? throw new InvalidOperationException("no hay barra de pasos");
        }

        private static HudLayout Layout(UiTestHost h)
        {
            return h.Immersive?.Layout ?? throw new InvalidOperationException("no hay disposición del HUD");
        }

        private static bool ColorIs(Color color, ColorToken token)
        {
            var c = (Color32)color;
            var t = token.Rgb;
            return Math.Abs(c.r - t.R) <= 1 && Math.Abs(c.g - t.G) <= 1 && Math.Abs(c.b - t.B) <= 1;
        }

        private static bool Inside(UiTestHost h, string name)
        {
            var window = h.Root.worldBound;
            var b = h.BoundsOf(name);
            return h.IsShown(name) && b.xMin >= window.xMin - 0.5f && b.yMin >= window.yMin - 0.5f && b.xMax <= window.xMax + 0.5f && b.yMax <= window.yMax + 0.5f;
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        private static bool Near(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> Expect(params (string What, Func<bool> Ok)[] checks)
        {
            var problems = new List<string>();
            foreach (var (what, ok) in checks)
            {
                bool passed;
                try
                {
                    passed = ok();
                }
                catch (Exception error)
                {
                    problems.Add("no se cumple: " + what + " (" + error.GetType().Name + ": " + error.Message + ")");
                    continue;
                }

                if (!passed)
                {
                    problems.Add("no se cumple: " + what);
                }
            }

            return problems;
        }

        private static IEnumerable<string> Concat(params IEnumerable<string>[] parts)
        {
            var all = new List<string>();
            foreach (var part in parts)
            {
                all.AddRange(part);
            }

            return all;
        }
    }
}
