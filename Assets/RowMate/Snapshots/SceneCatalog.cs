using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it6a (06a-escena-lago/01-po-spec §7 → 02-tech-design §5): CAP-01 a CAP-24, DEMO-01 a -08 en una sola secuencia, VID-01 a
    // -11 y dos tomas de evidencia que no son de la spec (it6a-low, QA-G6aT-21; it6a-brillo, la sonda del brillo de DN6a-21). Todas con
    // la escena REAL (SceneMode.Real, nivel Alto salvo it6a-low), el Editor sin -nographics, la compilación de shaders sincrónica
    // (SnapshotCli y UiTestHost), la orilla armada en el acto y la escena en pasos de 1/60 s mientras avanza el reloj de prueba.
    // Salvo que diga otra cosa: español (es-AR), el remo de prueba R-4 sin la llave ni la cinta, F-4A, una MemoryPreferences por toma,
    // el reloj de prueba (13:00 UTC, UTC−03:00: 10:00 en la pared), sin región (la luna del norte) y la regla de toma de la 4b (el
    // instante exacto; falla si algo finito corre) con la escena lista y la orilla armada (SnapshotTakeRule.SceneBlockers).
    // Escena sola (CAP-01 a -12, VID-05, -06, -10): HudView.HideForCapture, sin los degradés de pantalla, tiempo de escena 3,2 s,
    // respiración en 0, E-920 y la hora con el control de prueba (en el acto, con los fundidos en su valor final); la distancia y la hora
    // se fuerzan con la escena quieta (un Tick sin tiempo), así la toma queda EXACTAMENTE en E-n. Las sondas (Inspect) van al manifest:
    // los valores del cuadro, el astro con la cámara real, las estrellas, las nubes, las marcas del legacy (CAP-02, -05, -07) y el
    // temblor (CAP-12, it6a-low).
    public static class SceneCatalog
    {
        public const double FrozenSceneTime = 3.2;
        public const double DiskToleranceDp = 2;

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddSceneAlone(all);
                AddHud(all);
                all.Add(Demo());
                AddVideos(all);
                AddEvidence(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

#if ROWMATE_SIMULATOR
        // ── La escena sola (CAP-01 a CAP-12; §5.2) ──
        private static void AddSceneAlone(List<SnapshotScenario> all)
        {
            all.Add(Alone("CAP-01", "CAP-01-amanecer-v390", DeviceProfile.V390, 920, 6, "06:00, E-920 (LEG-01)"));
            var cap02 = Alone("CAP-02", "CAP-02-mediodia-v390-marcas", DeviceProfile.V390, 920, 12, "12:00, E-920, con marcas (LEG-02)", marks: true,
                disk: (195.0, 254.7), stars: 0, clouds: true);
            var cap02Alone = cap02.Inspect!;
            // Las sondas de contrato del cielo (04-design-notes §10.4): la pasada sin post, contra SkyBackdrop.Gradient(q).
            cap02.Inspect = (h, frame, values) => Concat(cap02Alone(h, frame, values),
                SceneMarks.Contract(h, values, ("cielo de arriba", 195, 8, "0B4FA8", 3), ("cielo a media altura", 195, 168.8, "2C70BE", 3)));
            all.Add(cap02);
            all.Add(Alone("CAP-03", "CAP-03-atardecer-v390", DeviceProfile.V390, 920, 18, "18:00, E-920 (LEG-03)"));
            all.Add(Alone("CAP-04", "CAP-04-noche-v390", DeviceProfile.V390, 920, 22, "22:00, E-920 (LEG-04)", disk: (155.2, 264.3), stars: 80));
            all.Add(Alone("CAP-05", "CAP-05-mediodia-h844-marcas", DeviceProfile.H844, 920, 12, "12:00, E-920, con marcas (LEG-05)", marks: true,
                disk: (422.0, 80.9), stars: 0));
            all.Add(Alone("CAP-06", "CAP-06-noche-h844", DeviceProfile.H844, 920, 22, "22:00, E-920 (LEG-06)", disk: (382.2, 90.5), stars: 80));
            all.Add(Alone("CAP-07", "CAP-07-arranque-v390-marcas", DeviceProfile.V390, 0, 12, "12:00, E-0, con marcas (LEG-07)", marks: true, stars: 0));
            all.Add(Alone("CAP-08", "CAP-08-puesta-th", DeviceProfile.TH, 920, 19, "19:00, E-920", stars: 80));
            all.Add(Alone("CAP-09", "CAP-09-luna-saliendo-th", DeviceProfile.TH, 920, 20, "20:00, E-920", stars: 80));
            all.Add(Alone("CAP-10", "CAP-10-medianoche-tv", DeviceProfile.TV, 920, 0, "00:00, E-920", disk: (205.5, 278.2), stars: 80));
            all.Add(Alone("CAP-11", "CAP-11-tarde-tv", DeviceProfile.TV, 920, 15, "15:00, E-920", disk: (268.8, 300.3), stars: 0));
            all.Add(Alone("CAP-12", "CAP-12-maraton-w1280", DeviceProfile.W1280, 42195, 12, "12:00, E-42K (42 195 m); sin temblor", still: true, stars: 0));
        }

        // ── El HUD sobre la escena (CAP-13 a CAP-24) ──
        private static void AddHud(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, profile) in new[]
                     {
                         ("CAP-13", "CAP-13-hud-lago-tv", DeviceProfile.TV), ("CAP-14", "CAP-14-hud-lago-th", DeviceProfile.TH),
                         ("CAP-15", "CAP-15-hud-lago-l640", DeviceProfile.L640), ("CAP-16", "CAP-16-hud-lago-p360", DeviceProfile.P360),
                         ("CAP-17", "CAP-17-hud-lago-w1280", DeviceProfile.W1280),
                     })
            {
                var scenario = Scene(id, stem, profile, "Libre a los 60,2 s, 10:00, con el contorno", s => Free(s, 60.2), outline: true);
                scenario.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(
                    Check("la banda dice 1240 m", () => h.TextOf("hud-distance-value") == "1240"),
                    Check("distancia de escena ≈ 234 m (± 1)", () => Near(h.Scene!.Frame.Distance, 234, 1)),
                    Check("los degradés de pantalla a la vista (DV-67)", () => h.Scene!.World!.Director!.Overlay.Enabled),
                    Check("el punto del remero en el centro de la zona del bote ± 2 dp", () => RowerInZone(h, values))));
                all.Add(scenario);
            }

            var cap18 = Scene("CAP-18", "CAP-18-series-atardecer-tv", DeviceProfile.TV, "Series 600 a los 250,2 s, los dos paneles abiertos, 18:00, con el contorno",
                s => Routine(s, "Series 600", 250.2, 18), seed: RoutineSeeds.F5, outline: true, laps: true, reps: true);
            cap18.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(
                Check("los dos paneles abiertos", () => h.SeriesColumn != null && h.SeriesColumn.Expanded.Laps && h.SeriesColumn.Expanded.Reps),
                Check("hora visible 18", () => Near(h.Scene!.Frame.VisibleHour, 18, 1e-6))));
            all.Add(cap18);

            var cap19 = Scene("CAP-19", "CAP-19-pildoras-nube-tv", DeviceProfile.TV, "Prueba 4a a los 90,6 s, 12:00, una nube detrás de la barra de píldoras",
                s => Steps(Routine(s, "Prueba 4a", 90.6, 12), Act(s, CloudBehindTheBar)));
            cap19.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(
                Check("hora visible 12", () => Near(h.Scene!.Frame.VisibleHour, 12, 1e-6)),
                Check("la barra de píldoras a la vista", () => h.IsShown("hud-pill-row"))));
            all.Add(cap19);

            foreach (var (id, stem, profile) in new[] { ("CAP-20", "CAP-20-velo-tv", DeviceProfile.TV), ("CAP-21", "CAP-21-velo-th", DeviceProfile.TH) })
            {
                var scenario = Scene(id, stem, profile, "Libre: Pausa a los 60,2 s; la toma a los 62,2 s", Paused);
                scenario.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(
                    Check("el velo en 1", () => Near(h.Scene!.Frame.Post.VeilWeight, 1, 1e-3)),
                    Check("velocidad ≈ 1,05 m/s (± 0,15)", () => Near(h.Scene!.Frame.Speed, 1.05, 0.15)),
                    Check("PAUSA en su disco", () => h.IsShown("hud-paused-label"))));
                all.Add(scenario);
            }

            // QA-V6a-04 (ronda 1): de los 29 s a la toma cuadro a cuadro, como VID-08 (con el salto de AdvanceTo la escena integraba los
            // 36,5 s con las lecturas del final y quedaba a 4,5 m y quieta).
            var cap22 = Scene("CAP-22", "CAP-22-toma-aerea-tv", DeviceProfile.TV, "Corta: el diálogo a los 30,3 s; la toma a los 36,5 s",
                s => Steps(Routine(s, "Corta", 29, null), s.StepTo(36.5)));
            // A los 36,5 s llega la lectura L(37) (las de R-4 caen en los k − 0,5 de la sesión): los rodillos de la distancia arrancan en ese
            // instante (progreso 0, todavía con la cifra de antes). Se anotan en inFlight y no bloquean la toma de la spec.
            cap22.MotionMayRun = true;
            cap22.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(
                Check("el diálogo de fin", () => h.Completion != null),
                Check("cámara en modo aéreo", () => h.Scene!.Frame.CameraMode == CameraMode.Aerial),
                Check("cabeceo −4° (± 0,05)", () => Near(h.Scene!.Frame.Pose.PitchDeg, -4, 0.05)),
                Check("respiración 0", () => Near(h.Scene!.Frame.BreathingMultiplier, 0, 1e-6)),
                Check("el horizonte en 336,9 ± 1 dp", () => Near(h.Scene!.Frame.Pose.HorizonDp - h.Scene.Frame.HorizonDeviationDp, 336.9, 1)),
                Check("la popa en 552,9 ± 2 dp, dentro de la zona del bote", () => SternInZone(h, values))));
            all.Add(cap22);

            var cap23 = Scene("CAP-23", "CAP-23-noche-w1280", DeviceProfile.W1280, "Libre a los 60,2 s, 22:00", s => Steps(Hour(s, 22), Free(s, 60.2)));
            cap23.Inspect = (h, frame, values) => HudInspect(h, frame, values, Concat(Check("hora visible 22", () => Near(h.Scene!.Frame.VisibleHour, 22, 1e-6))));
            all.Add(cap23);

            var cap24 = Scene("CAP-24", "CAP-24-escena-demorada-tv", DeviceProfile.TV, "Libre con la escena demorada 1,5 s, a los 0,6 s", Delayed);
            cap24.SceneMayBeLoading = true;
            cap24.MotionMayRun = true;
            cap24.Inspect = (h, frame, values) =>
            {
                values.Add(SceneMarks.Values(h, Since(h)));
                var backdrop = h.Immersive!.View.Backdrop;
                values.Add("fondo provisorio: cielo #" + ColorUtility.ToHtmlStringRGB(backdrop.SkyColor) + ", agua #" + ColorUtility.ToHtmlStringRGB(backdrop.WaterColor));
                return Concat(
                    Check("scene-surface oculto", () => SceneMarks.SurfaceOpacity(h) == 0),
                    Check("la escena preparándose", () => h.Scene!.Readiness.Value != RowMate.Scene.SceneReadiness.Ready),
                    Check("el fondo teñido por las 10 h (#1E61BA / #145395 ± 2)", () => SameColor(backdrop.SkyColor, "1E61BA") && SameColor(backdrop.WaterColor, "145395")));
            };
            all.Add(cap24);
        }

        // ── La demo (DEMO-01 a -08, una secuencia; spec §7.4) ──
        private static SnapshotScenario Demo()
        {
            var demo = Scene("DEMO", "DEMO", DeviceProfile.W1280, "La demo de la It. 6a", DemoSteps, simulator: true);
            demo.Kind = SnapshotKind.Sequence;
            foreach (var (id, stem, state) in new[]
                     {
                         ("DEMO-01", "DEMO-01-workout", "Workout en reposo (la escena preparada y sin dibujar)"),
                         ("DEMO-02", "DEMO-02-entrada", "Entrenamiento libre; la toma a los 2,2 s"),
                         ("DEMO-03", "DEMO-03-muelle", "Llave → Fuerte a los 3 s; la toma a los 36,2 s (el muelle a la izquierda, acercándose)"),
                         ("DEMO-04", "DEMO-04-atardecer", "Hora escena hasta 18:00; la toma 1,6 s después"),
                         ("DEMO-05", "DEMO-05-luna-saliendo", "+ a 19:00 y a 20:00"),
                         ("DEMO-06", "DEMO-06-noche", "+ a 22:00"),
                         ("DEMO-07", "DEMO-07-pausa-noche", "Pausa"),
                         ("DEMO-08", "DEMO-08-salida", "Hora real → Reanudar → Terminar → Terminar"),
                     })
            {
                demo.Shot(id, stem, state);
            }

            return demo;
        }

        private static long _demoDraws;

        private static IEnumerator DemoSteps(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Shot("DEMO-01", h => Expect(("Workout en reposo", () => h.Immersive == null),
                ("0 dibujos de la escena después de Ready", () => h.Scene != null && h.Scene.Draws == 0),
                ("a lo sumo el dibujo de calentamiento", () => h.Scene != null && h.Scene.WarmupDraws <= 1)));

            yield return s.StartFree();
            // A los 2,2 s (el reloj de la banda cambia en el segundo entero: la regla de toma de la 4b toma en k + 0,2).
            yield return s.AdvanceTo(2.2);
            yield return s.Shot("DEMO-02", h => Expect(("la cámara en su lugar (modo Contract)", () => h.Scene!.Frame.CameraMode == CameraMode.Contract),
                ("el HUD entero", () => h.Immersive != null && !h.Immersive.Cascade.IsRunning), ("la luz de las 10", () => Near(h.Scene!.Frame.VisibleHour, 10, 0.02))));

            yield return s.AdvanceTo(3);
            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-preset-fuerte");
            yield return s.Tap("sim-close");
            // QA-V6a-R2-03 (= QA-V6a-05; 01-po-spec §7.4 corregida): a los 36,2 s (k + 0,2) el muelle está a la izquierda y se acerca; a los
            // 39,2 s ya pasó a la par. Después la secuencia sigue desde los 39,2 s, como antes: DEMO-04 en adelante no cambian.
            yield return s.AdvanceTo(36.2);
            yield return s.Shot("DEMO-03", h => Expect(("el muelle a la izquierda, acercándose", () => PierOnTheLeft(h))));
            yield return s.AdvanceTo(39.2);

            yield return s.Tap("sim-open");
            yield return s.TapTimes("sim-hour-plus", 8);
            yield return s.Tap("sim-close");
            yield return After(s, 1.6);
            yield return s.Shot("DEMO-04", h => Expect(("hora visible 18", () => Near(h.Scene!.Frame.VisibleHour, 18, 1e-6)),
                ("el reloj de pared en la hora real", () => (h.TextOf("hud-wall-clock") ?? "").StartsWith("10:", StringComparison.Ordinal))));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-hour-plus");
            yield return s.Advance(2);
            yield return s.TapInView("sim-hour-plus");
            yield return s.Tap("sim-close");
            yield return After(s, 1.6);
            yield return s.Shot("DEMO-05", h => Expect(("hora visible 20", () => Near(h.Scene!.Frame.VisibleHour, 20, 1e-6)),
                ("la luna", () => h.Scene!.Frame.Celestial.DiskIsMoon)));

            yield return s.Tap("sim-open");
            yield return s.TapTimes("sim-hour-plus", 2);
            yield return s.Tap("sim-close");
            yield return After(s, 1.6);
            yield return s.Shot("DEMO-06", h => Expect(("hora visible 22", () => Near(h.Scene!.Frame.VisibleHour, 22, 1e-6)),
                ("80 estrellas", () => VisibleStars(h) == 80)));

            yield return s.Tap("hud-pause");
            yield return After(s, 0.5);
            yield return s.Shot("DEMO-07", h => Expect(("el velo en 1", () => Near(h.Scene!.Frame.Post.VeilWeight, 1, 1e-3)),
                ("el bote deslizándose", () => h.Scene!.Frame.Speed > 0.2)));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-hour-real");
            yield return s.Tap("sim-close");
            yield return s.Tap("hud-resume");
            yield return After(s, 1.6);
            yield return s.Do(h => _demoDraws = h.Scene!.Draws);
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Shot("DEMO-08", h => Expect(("Workout en reposo", () => h.Immersive == null && h.TopDialog == null),
                ("la escena cerrada y sin dibujar", () => h.Scene != null && !h.Scene.IsOpen && !h.Scene.IsShowing),
                ("0 dibujos al final", () => NoMoreDraws(h))));
        }

        // Al menos `seconds` más y en el k + 0,2 siguiente de la sesión (lejos del cambio de segundo del reloj y de las lecturas).
        private static IEnumerator After(ScenarioScript s, double seconds)
        {
            var elapsed = s.Host.Scheduler.MonotonicSeconds - s.SessionStart + seconds;
            yield return s.AdvanceTo(Math.Ceiling(elapsed - 0.2 - 1e-9) + 0.2);
        }

        // ── Los videos (VID-01 a -11; spec §7.5; 30 cuadros por segundo; los valores de la escena por cuadro en el manifest) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Scene("VID-01", "VID-01-entrada", DeviceProfile.TV, "Entrenamiento libre (2,5 s)", s => s.Tap("tab-workout"));
            Video(vid01, 2.5).At(0, h => Start(h, "workout-free"));
            var flight = new FlightTracker();
            vid01.Video!.FrameCheck = (h, t) => flight.Check(h, t);
            all.Add(vid01);

            var vid02 = Scene("VID-02", "VID-02-entrada-reducida", DeviceProfile.TV, "VID-01 con reducir movimiento (1 s)", s => s.Tap("tab-workout"));
            vid02.Host.ReduceMotion = true;
            Video(vid02, 1).At(0, h => Start(h, "workout-free"));
            vid02.Video!.FrameCheck = (h, t) => h.Scene?.Session != null && h.Scene.Frame.CameraMode != CameraMode.Contract && h.Scene.Frame.CameraMode != CameraMode.Hidden
                ? "con reducir movimiento la cámara no está en Contract (" + h.Scene.Frame.CameraMode + ")"
                : null;
            all.Add(vid02);

            var vid03 = Scene("VID-03", "VID-03-bote-avanzando-w1280", DeviceProfile.W1280, "Libre con Fuerte (desde los 3 s), de los 30 a los 42 s",
                s => Steps(s.StartFree(), s.AdvanceTo(3), s.Tap("sim-open"), s.TapInView("sim-preset-fuerte"), s.Tap("sim-close"), s.AdvanceTo(30)), simulator: true);
            Video(vid03, 12).FrameCheck = (h, t) => Horizon(h, 3);
            all.Add(vid03);

            var vid04 = Scene("VID-04", "VID-04-pausa-th", DeviceProfile.TH, "Libre con R-4: Pausa a los 20 s y Reanudar a los 32 s (14 s)", s => Free(s, 19));
            Video(vid04, 14).At(1, h => h.Tap("hud-pause")).At(13, h => h.Tap("hud-resume"));
            vid04.Video!.FrameCheck = (h, t) => t > 1.35 && t < 13 && !Near(h.Scene!.Frame.Post.VeilWeight, 1, 1e-3) ? "el velo no llegó a 1 en 300 ms"
                : t > 13.3 && h.Scene!.Frame.Post.VeilWeight > 1e-3 ? "el velo no se fue en 240 ms" : null;
            all.Add(vid04);

            all.Add(HourVideo("VID-05", "VID-05-anochecer-th", "Escena sola, E-920: 17:00 y + cada 2 s hasta 23:00 (14 s)", 14, 17, 23));
            all.Add(HourVideo("VID-06", "VID-06-amanecer-th", "Escena sola, E-920: 03:00 y + cada 2 s hasta 07:00 (10 s)", 10, 3, 7));

            var vid07 = Scene("VID-07", "VID-07-salida-tv", DeviceProfile.TV, "Libre a los 20 s: Terminar → Terminar (1,2 s)",
                s => Steps(Free(s, 20), s.Tap("hud-finish")));
            Video(vid07, 1.2).At(0, h => h.Tap("dialog-confirm"));
            var exit = new ExitTracker();
            vid07.Video!.FrameCheck = (h, t) => exit.Check(h, t);
            all.Add(vid07);

            var vid08 = Scene("VID-08", "VID-08-final-aereo-tv", DeviceProfile.TV, "Corta de los 29 a los 38 s (9 s)", s => Routine(s, "Corta", 29, null));
            Video(vid08, 9);
            var aerial = new AerialTracker();
            vid08.Video!.FrameCheck = (h, t) => aerial.Check(h, t);
            all.Add(vid08);

            var vid09 = Scene("VID-09", "VID-09-rotar-tv-th", DeviceProfile.TV, "Libre a los 20 s: rotar de TV a TH (2 s)", s => Free(s, 20));
            Video(vid09, 2).At(0.5, h => h.Resize(DeviceProfile.TH));
            vid09.Video!.CanvasWidth = DeviceProfile.TH.WidthPx;
            vid09.Video.CanvasHeight = DeviceProfile.TV.HeightPx;
            var distance = new DistanceTracker();
            vid09.Video.FrameCheck = (h, t) => distance.Check(h);
            all.Add(vid09);

            var vid10 = Scene("VID-10", "VID-10-respiracion-tv", DeviceProfile.TV, "Escena sola, libre con R-4 sin tocar nada, con la línea del contrato (20 s)",
                s => Steps(Free(s, 2.5), Act(s, Breathing)));
            Video(vid10, 20).FrameCheck = (h, t) => Horizon(h, 3);
            all.Add(vid10);

            var vid11 = Scene("VID-11", "VID-11-escena-demorada-tv", DeviceProfile.TV, "Libre con la escena demorada 1,5 s (3 s)",
                s => Steps(s.Tap("tab-workout"), s.Do(h => h.Scene!.DelayReadinessForTest(1.5))));
            Video(vid11, 3).At(0, h => Start(h, "workout-free"));
            vid11.Video!.FrameCheck = (h, t) => h.Scene?.Session != null && h.Scene.Readiness.Value != RowMate.Scene.SceneReadiness.Ready && SceneMarks.SurfaceOpacity(h) > 0
                ? "la escena se ve antes de estar lista"
                : null;
            all.Add(vid11);
        }

        // ── Evidencia (no son de la spec) ──
        private static void AddEvidence(List<SnapshotScenario> all)
        {
            // QA-G6aT-21: el nivel Bajo, dos cuadros seguidos con el bote quieto: 0 píxeles distintos (en D3D11; GLES, NO VERIFICABLE).
            // QA-V6a-REN-16 (ronda 1): con el asset de URP de Bajo (TierScope: sin mapa de sombras, FXAA) además del perfil de la escena.
            var low = Alone("it6a-low", "it6a-low-w1280", DeviceProfile.W1280, 920, 12,
                "Nivel Bajo (perfil y asset de URP), 12:00, E-920; sin temblor (QA-G6aT-21; en Direct3D 11, con Z invertida: la profundidad de GLES queda NO VERIFICABLE hasta el teléfono, It. 14)",
                still: true);
            low.Host.SceneTier = QualityTier.Low;
            low.Host.SceneUrpTier = QualityTier.Low;
            all.Add(low);

            // DN6a-21 (AC-SCN-11-05): el brillo del sol de mediodía en TV, nivel Alto.
            var glow = Alone("it6a-brillo", "it6a-brillo-tv", DeviceProfile.TV, 920, 12, "12:00, E-920; la sonda del brillo (DN6a-21)", glow: true);
            all.Add(glow);
        }

        // ── Armado ──

        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                              RoutineSeed? seed = null, bool simulator = false, bool outline = false, bool laps = false, bool reps = false)
        {
            var preferences = new MemoryPreferences();
            if (laps)
            {
                preferences.SetBool(SeriesPanelPrefs.LapsKey, true);
            }

            if (reps)
            {
                preferences.SetBool(SeriesPanelPrefs.RepsKey, true);
            }

            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = simulator ? HostBackends.Simulator(false) : HostBackends.TestRower(RowerScripts.R4),
                SimulatorLayer = simulator ? SimulatorLayerMode.Full : SimulatorLayerMode.None,
                LanguageTags = new[] { "es-AR" },
                Routines = HostRoutines.Seeded(seed ?? RoutineSeeds.F4A),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                BoatZoneOutline = outline,
                Preferences = preferences,
                Scene = SceneMode.Real,
                SceneTier = QualityTier.High,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = simulator ? "Sim" : "R-4", ExactInstant = true };
        }

        // Una toma de la escena sola: libre, el vuelo terminado y la escena quieta en la distancia y la hora pedidas.
        private static SnapshotScenario Alone(string id, string stem, DeviceProfile profile, double distance, double hour, string state, bool marks = false,
                                              (double X, double Y)? disk = null, int? stars = null, bool clouds = false, bool still = false, bool glow = false)
        {
            var scenario = Scene(id, stem, profile, state, s => AloneArrange(s, distance, hour));
            scenario.Inspect = (h, frame, values) => AloneInspect(h, frame, values, distance, hour, marks, disk, stars, clouds, still, glow);
            return scenario;
        }

        private static IEnumerator AloneArrange(ScenarioScript s, double distance, double hour)
        {
            // A los 2,2 s: el vuelo terminó (1,82 s) y los rodillos de la lectura de los 1,5 s ya pasaron su mitad (la de los 2,5 s todavía no llegó).
            yield return Free(s, 2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ForceDistanceForTest(distance);
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            // Dos Tick sin tiempo: la distancia queda EXACTAMENTE en E-n (la hora y los fundidos, en el acto).
            h.SceneTick();
            h.SceneTick();
            yield return s.Frames(3);
        }

        private static IEnumerable<string> AloneInspect(UiTestHost h, Texture2D frame, List<string> values, double distance, double hour, bool marks,
                                                        (double X, double Y)? disk, int? stars, bool clouds, bool still, bool glow)
        {
            var problems = new List<string>();
            var scene = h.Scene!;
            values.Add(SceneMarks.Values(h, Since(h)));
            if (!Near(scene.Frame.Distance, distance, 1e-6))
            {
                problems.Add("distancia de escena " + F(scene.Frame.Distance) + " (pedida " + F(distance) + ")");
            }

            if (!Near(scene.Frame.VisibleHour, hour, 1e-6))
            {
                problems.Add("hora visible " + F(scene.Frame.VisibleHour) + " (pedida " + F(hour) + ")");
            }

            if (!Near(scene.Frame.BreathingMultiplier, 0, 1e-9) || !Near(scene.Frame.SceneTime, FrozenSceneTime, 1e-9))
            {
                problems.Add("la escena sola no quedó quieta (respiración " + F(scene.Frame.BreathingMultiplier) + ", tiempo " + F(scene.Frame.SceneTime) + ")");
            }

            // Antes de pintar las marcas: el astro mira los píxeles.
            var (ex, ey, _) = SceneMarks.Disk(h, frame, values);
            if (disk.HasValue && (!Near(ex, disk.Value.X, DiskToleranceDp) || !Near(ey, disk.Value.Y, DiskToleranceDp)))
            {
                problems.Add("el astro en (" + F(ex) + "; " + F(ey) + ") dp y no en (" + F(disk.Value.X) + "; " + F(disk.Value.Y) + ") ± " + F(DiskToleranceDp));
            }

            var visible = SceneMarks.Stars(h, values);
            if (stars.HasValue && visible != stars.Value)
            {
                problems.Add(visible + " estrellas a la vista (se esperaban " + stars.Value + ")");
            }

            var cloudError = SceneMarks.Clouds(h, values);
            if (clouds && !(cloudError <= 1))
            {
                problems.Add("las nubes a " + F(cloudError) + " dp de su punto del legacy (tope 1)");
            }

            if (glow)
            {
                var diameter = SceneMarks.Glow(h, frame, values);
                if (!(diameter >= 30))
                {
                    // Sonda de aspecto (04-design-notes §10.4, §21.4): se anota y no hace fallar la toma (la ajusta el Designer).
                    values.Add("brillo: FALLA de aspecto (" + F(diameter) + " dp; mínimo 30)");
                }
            }

            if (still)
            {
                var differing = SceneMarks.Still(h, values);
                if (differing != 0)
                {
                    problems.Add("temblor: " + differing + " píxeles distintos entre dos dibujos con el bote quieto");
                }
            }

            if (marks)
            {
                var worst = SceneMarks.Marks(h, frame, values);
                if (!(worst <= SceneMarks.MarkToleranceDp))
                {
                    problems.Add("marcas: |Δx| = " + F(worst) + " dp (tope " + F(SceneMarks.MarkToleranceDp) + ")");
                }
            }

            return problems;
        }

        // El HUD sobre la escena: los valores del cuadro, el astro y las estrellas, y las comprobaciones de la toma.
        private static IEnumerable<string> HudInspect(UiTestHost h, Texture2D frame, List<string> values, List<string> problems)
        {
            values.Add(SceneMarks.Values(h, Since(h)));
            if (h.Scene?.World?.Rig != null && h.Scene.IsShowing)
            {
                SceneMarks.Disk(h, frame, values);
                SceneMarks.Stars(h, values);
            }

            return problems;
        }

        // Una acción sin Settle (el reloj de prueba no se mueve: la toma sigue en el instante pedido).
        private static IEnumerator Act(ScenarioScript s, Action<UiTestHost> action)
        {
            yield return s.Frames(1);
            action(s.Host);
            yield return s.Frames(2);
        }

        private static IEnumerator Free(ScenarioScript s, double at)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(at);
        }

        private static IEnumerator Hour(ScenarioScript s, int hour)
        {
            yield return s.Do(h => h.App.Scene.HourOverride.Value = hour);
        }

        private static IEnumerator Routine(ScenarioScript s, string name, double at, int? hour)
        {
            s.ReadingZeroAtStart = true;
            if (hour.HasValue)
            {
                yield return Hour(s, hour.Value);
            }

            yield return s.Tap("tab-workout");
            yield return s.StartRoutine(name);
            yield return s.AdvanceTo(at);
        }

        private static IEnumerator Paused(ScenarioScript s)
        {
            yield return Free(s, 60.2);
            yield return s.Do(h => h.Tap("hud-pause"));
            yield return s.AdvanceTo(62.2);
        }

        // CAP-24: la escena demorada y la toma a los 0,6 s del toque (sin Settle: la cascada del HUD todavía corre).
        private static IEnumerator Delayed(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Do(h => h.Scene!.DelayReadinessForTest(1.5));
            yield return s.StartFree(false);
            yield return s.AdvanceTo(0.6);
        }

        // CAP-19: el tiempo de escena con la nube más cercana a la barra de píldoras centrada detrás de ella (la nube de la spec).
        private static void CloudBehindTheBar(UiTestHost h)
        {
            var scene = h.Scene!;
            var bar = h.BoundsOf("hud-pill-row");
            var cam = scene.Frame.Window;
            var best = 0;
            var bestGap = double.MaxValue;
            for (var i = 0; i < CloudModel.Count; i++)
            {
                var gap = Math.Abs(CloudModel.Y(i, cam.HorizonY) - bar.center.y);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }

            var spec = CloudModel.Legacy[best];
            // X(i, ancho, t) = ((x₀ + t·0,004·v) mod 1,2)·ancho − 0,1·ancho = ancho/2 ⇔ fase 0,6.
            var t = (0.6 - spec.X0) / (0.004 * spec.Speed);
            while (t < 0)
            {
                t += 1.2 / (0.004 * spec.Speed);
            }

            scene.FreezeSceneTimeForTest(t);
            h.SceneTick();
        }

        // VID-10: la escena sola con la respiración (sin forzar nada) y la línea del contrato marcada encima.
        private static void Breathing(UiTestHost h)
        {
            var scene = h.Scene!;
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            var line = new VisualElement { name = "snapshot-contract-line", pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute;
            line.style.left = 0;
            line.style.right = 0;
            line.style.top = (float)(scene.Frame.Window.HorizonY - 0.5);
            line.style.height = 1;
            line.style.backgroundColor = RowMate.Presentation.Controls.TokenColors.ToColor(Tokens.DevSimulator);
            h.Root.Add(line);
        }

        private static SnapshotScenario HourVideo(string id, string stem, string state, double seconds, int from, int to)
        {
            var scenario = Scene(id, stem, DeviceProfile.TH, state, s => Steps(Hour(s, from), Free(s, 2.5), Act(s, h => SceneAloneLive(h, 920))), simulator: false);
            var video = Video(scenario, seconds);
            for (var hour = from + 1; hour <= to; hour++)
            {
                var value = hour;
                video.At(2 * (hour - from), h => h.App.Scene.HourOverride.Value = value);
            }

            video.BeforeFrame = h => h.Scene?.ForceDistanceForTest(920);
            return scenario;
        }

        // La escena sola para los videos de la hora: la hora la lleva Hora escena (con su transición), la distancia queda en E-n.
        private static void SceneAloneLive(UiTestHost h, double distance)
        {
            var scene = h.Scene!;
            scene.ForceDistanceForTest(distance);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            h.SceneTick();
        }

        private static VideoSpec Video(SnapshotScenario scenario, double seconds)
        {
            scenario.Kind = SnapshotKind.Video;
            scenario.Video = new VideoSpec(seconds) { FrameValues = (h, t) => SceneMarks.Values(h, t) };
            return scenario.Video;
        }

        private static void Start(UiTestHost h, string element)
        {
            var shift = UiHostOptions.WorkoutStartUtc - h.Scheduler.UtcNow;
            h.Scheduler.ShiftUtc(shift);
            h.Tap(element);
        }

        // ── Sondas ──

        private static string? Horizon(UiTestHost h, double toleranceDp)
        {
            var scene = h.Scene;
            if (scene?.Session == null || scene.Frame.CameraMode != CameraMode.Contract)
            {
                return null;
            }

            return Math.Abs(scene.Frame.HorizonDeviationDp) > toleranceDp
                ? "el horizonte a " + F(scene.Frame.HorizonDeviationDp) + " dp de su línea (tope " + F(toleranceDp) + ")"
                : null;
        }

        private static bool RowerInZone(UiTestHost h, List<string> values)
        {
            var layout = h.Immersive?.Layout;
            if (layout == null)
            {
                return false;
            }

            var cam = h.Scene!.Frame.Window;
            var zone = layout.BoatZone;
            var center = (zone.Top + zone.Bottom) / 2;
            values.Add("zona del bote: y " + F(zone.Top) + "-" + F(zone.Bottom) + " (centro " + F(center) + "); el punto del remero en y " + F(cam.RowerY));
            return Near(cam.RowerY, center, 2);
        }

        // CAP-22: la popa (la punta de atrás del casco a la altura de la borda, como la cuenta de 04-design-notes §11) con la cámara real.
        private static bool SternInZone(UiTestHost h, List<string> values)
        {
            var scene = h.Scene!;
            var layout = h.Immersive?.Layout;
            if (scene.World?.Rig == null || layout == null)
            {
                return false;
            }

            var stern = new Vector3(0, (float)RowMate.Core.Scene.Art.LakeArt.HullFreeboard, (float)RowMate.Core.Scene.Art.LakeArt.HullStern);
            var v = scene.World.Rig.Camera.WorldToViewportPoint(stern);
            var y = (1 - v.y) * h.Profile.HeightDp;
            var zone = layout.BoatZone;
            values.Add("popa: y " + F(y) + " dp (552,9 ± 2); zona del bote y " + F(zone.Top) + "-" + F(zone.Bottom));
            return Near(y, 552.9, 2) && y >= zone.Top - 0.5 && y <= zone.Bottom + 0.5;
        }

        private static bool PierOnTheLeft(UiTestHost h)
        {
            var frame = h.Scene!.Frame;
            var props = frame.VisibleProps;
            for (var i = 0; i < props.Count; i++)
            {
                var p = props[i];
                if (p.Kind == PropKind.Pier && p.X < 0 && Math.Abs(p.Z - frame.Distance) < 40)
                {
                    return true;
                }
            }

            return false;
        }

        private static int VisibleStars(UiTestHost h)
        {
            var stars = h.Scene!.Frame.Stars;
            var count = 0;
            for (var i = 0; i < stars.Length; i++)
            {
                if (stars[i].Alpha > 0)
                {
                    count++;
                }
            }

            return count;
        }

        // DEMO-08: después de la salida, cuadros que pasan sin un dibujo nuevo de la escena.
        private static bool NoMoreDraws(UiTestHost h)
        {
            var draws = h.Scene!.Draws;
            for (var i = 0; i < 6; i++)
            {
                h.Step();
            }

            return h.Scene.Draws == draws && draws >= _demoDraws;
        }

        private static bool SameColor(Color color, string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out var expected))
            {
                return false;
            }

            Color32 a = color;
            Color32 b = expected;
            return Math.Abs(a.r - b.r) <= 2 && Math.Abs(a.g - b.g) <= 2 && Math.Abs(a.b - b.b) <= 2;
        }

        // VID-01: negro hasta los 220 ms, la escena a pleno a los 400, la velocidad de la cámara en el vuelo ≤ 17,5 m/s y la respiración
        // entrando con S(0; 0,6 s) desde los 1 820 ms.
        private sealed class FlightTracker
        {
            private double _lastT = double.NaN;
            private double _x, _y, _z;
            private CameraMode _lastMode = CameraMode.Hidden;

            public string? Check(UiTestHost h, double t)
            {
                var scene = h.Scene;
                if (scene?.Session == null)
                {
                    return null;
                }

                var f = scene.Frame;
                var problems = new List<string>();
                var opacity = SceneMarks.SurfaceOpacity(h);
                if (t < 0.22 - 1e-6 && opacity > 0)
                {
                    problems.Add("la escena se ve antes de los 220 ms (opacidad " + F(opacity) + ")");
                }

                if (t > 0.4 + 1e-6 && opacity < 0.99)
                {
                    problems.Add("la escena no está a pleno después de los 400 ms (opacidad " + F(opacity) + ")");
                }

                // La velocidad entre dos cuadros del vuelo (el primero sale de la pose oculta: no cuenta).
                if (f.CameraMode == CameraMode.Flight && _lastMode == CameraMode.Flight && !double.IsNaN(_lastT) && t > _lastT)
                {
                    var p = f.Pose;
                    var speed = Math.Sqrt((p.X - _x) * (p.X - _x) + (p.Y - _y) * (p.Y - _y) + (p.Z - _z) * (p.Z - _z)) / (t - _lastT);
                    if (speed > 17.5)
                    {
                        problems.Add("la cámara a " + F(speed) + " m/s en el vuelo");
                    }
                }

                if (t <= 1.82 + 1e-6 && f.BreathingMultiplier > 1e-9)
                {
                    problems.Add("la respiración antes de los 1 820 ms (" + F(f.BreathingMultiplier) + ")");
                }

                if (t >= 2.42 - 1e-6 && f.BreathingMultiplier < 0.999)
                {
                    problems.Add("la respiración no llegó a 1 a los 2 420 ms (" + F(f.BreathingMultiplier) + ")");
                }

                _lastT = t;
                _lastMode = f.CameraMode;
                _x = f.Pose.X;
                _y = f.Pose.Y;
                _z = f.Pose.Z;
                return problems.Count == 0 ? null : string.Join("; ", problems);
            }
        }

        // VID-07: el modo Exit desde el cuadro 0 y ningún dibujo de la escena después de los 400 ms.
        private sealed class ExitTracker
        {
            private long _drawsAt400 = -1;

            public string? Check(UiTestHost h, double t)
            {
                var scene = h.Scene;
                if (scene == null)
                {
                    return null;
                }

                if (scene.Session != null && scene.IsOpen && scene.Frame.CameraMode != CameraMode.Exit)
                {
                    return "la cámara no está en Exit (" + scene.Frame.CameraMode + ")";
                }

                if (t >= 0.4 - 1e-6)
                {
                    if (_drawsAt400 < 0)
                    {
                        _drawsAt400 = scene.Draws;
                    }
                    else if (scene.Draws != _drawsAt400)
                    {
                        return "la escena se dibujó después de los 400 ms (" + (scene.Draws - _drawsAt400) + " dibujos)";
                    }
                }

                return null;
            }
        }

        // VID-08: el modo Aerial desde los 30,3 s (1,3 s del video) y la cámara quieta desde los 36,3 s (7,3 s).
        private sealed class AerialTracker
        {
            private double _x = double.NaN, _y, _z, _pitch;

            public string? Check(UiTestHost h, double t)
            {
                var scene = h.Scene;
                if (scene?.Session == null)
                {
                    return null;
                }

                var f = scene.Frame;
                string? problem = null;
                if (t > 1.3 + 1.0 / 30 && f.CameraMode != CameraMode.Aerial)
                {
                    problem = "la cámara no está en Aerial a los " + F(29 + t) + " s (" + f.CameraMode + ")";
                }
                else if (t > 7.3 + 1.0 / 30 && !double.IsNaN(_x)
                         && (!Near(f.Pose.X, _x, 1e-6) || !Near(f.Pose.Y, _y, 1e-6) || !Near(f.Pose.Z, _z, 1e-6) || !Near(f.Pose.PitchDeg, _pitch, 1e-6)))
                {
                    problem = "la cámara se mueve después de los 36,3 s";
                }

                _x = f.Pose.X;
                _y = f.Pose.Y;
                _z = f.Pose.Z;
                _pitch = f.Pose.PitchDeg;
                return problem;
            }
        }

        // VID-09: la distancia sigue (sin reinicio ni salto) al rotar.
        private sealed class DistanceTracker
        {
            private double _last = double.NaN;

            public string? Check(UiTestHost h)
            {
                var scene = h.Scene;
                if (scene?.Session == null)
                {
                    return null;
                }

                var d = scene.Frame.Distance;
                var previous = _last;
                _last = d;
                if (double.IsNaN(previous))
                {
                    return null;
                }

                return d < previous - 1e-9 || d - previous > 0.5 ? "la distancia saltó de " + F(previous) + " a " + F(d) + " m" : null;
            }
        }
#endif

        private static double Since(UiTestHost h)
        {
            return h.Scheduler.MonotonicSeconds;
        }

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        private static (string What, Func<bool> Ok) Check(string what, Func<bool> ok)
        {
            return (what, ok);
        }

        private static List<string> Concat(IEnumerable<string> a, IEnumerable<string> b)
        {
            var all = new List<string>(a);
            all.AddRange(b);
            return all;
        }

        private static List<string> Concat(params (string What, Func<bool> Ok)[] checks)
        {
            return new List<string>(Expect(checks));
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

        private static bool Near(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
