using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;

namespace RowMate.Snapshots
{
    // Catálogo it6b (06b-agua-avanzada/02-tech-design §8; spec §5; por defecto desde la 6b): el agua avanzada en los 4 escenarios, la
    // degradada forzada, los niveles, el vidrio, el diálogo, la distancia de maratón, la popa con su reflejo y los charcos. Reglas de la 6a
    // §5.1 y de la 8 §6: la escena real (Alto salvo donde se indica), pasos de 1/60 s, el reloj de prueba, una MemoryPreferences por toma con el
    // escenario forzado, el remo de prueba R-4 y el maniquí; libre con el HUD (salvo "sola"); tiempo de escena 3,2 s, c = 0,18, la hora fija y
    // la distancia de referencia (lago E-920, río E-250, costa E-500, regata E-180); la estela precargada a la velocidad estable (G6b-15).
    // El agua de cada toma: Auto (la regla: con la avanzada lista y a la vista) salvo CAP-21 a -24 (la degradada); CheckWater la anota en el
    // manifiesto y hace fallar la toma que no tiene la pedida. Las sondas de escena se leen sobre la RT de la escena del mismo cuadro.
    // Ronda de verificación (QA-V6b-02 a -05, -07): CAP-27 con Series 600 y los dos paneles, CAP-29 con Corta hasta el diálogo de los 36,3 s y
    // la toma aérea, CAP-32 con los micro-anillos y el hervidero a la vista, las sondas de la spec (WaterSpecProbes) y VID-01 a -05.
    public static class WaterCatalog
    {
        public const double FrozenSceneTime = 3.2;
        public const double MatrixPhase = 0.18;
        public const double SteadySpeedMps = 4.4;

        private static readonly EnvironmentId[] Order = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };
        private static readonly int[] Hours = { 6, 12, 18, 22 };

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                var n = 1;
                foreach (var id in Order)
                {
                    foreach (var hour in Hours)
                    {
                        all.Add(Matrix(n++, id, hour, DeviceProfile.TV, null, QualityTier.High));
                    }
                }

                foreach (var id in Order)
                {
                    all.Add(Matrix(n++, id, 12, DeviceProfile.TH, null, QualityTier.High));
                }

                foreach (var id in Order)
                {
                    all.Add(Matrix(n++, id, 12, DeviceProfile.TV, WaterForce.Degraded, QualityTier.High));
                }

                // QA-V6b-05 (AC-6B-04 c): el halo de las 18 h con el disco a la vista (en TV queda detrás de los pinos de la derecha).
                all.Add(Matrix(33, EnvironmentId.Lake, 18, DeviceProfile.TH, null, QualityTier.High));
                all.Add(Matrix(25, EnvironmentId.Lake, 12, DeviceProfile.TV, null, QualityTier.Medium));
                all.Add(Matrix(26, EnvironmentId.Lake, 12, DeviceProfile.TV, null, QualityTier.Low));
                all.Add(SeriesGlass());
                all.Add(Glass(28, EnvironmentId.River, 18, DeviceProfile.W1280, "Vidrio sobre el atardecer: río 18:00, W1280"));
                all.Add(Completion());
                all.Add(Marathon());
                all.Add(Stern());
                all.Add(Puddles());
                AddVideos(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // DT6b-24: lo que se ve tiene que ser lo pedido (toda toma con la escena real; las de los sets viejos, la degradada).
        public static IEnumerable<string> CheckWater(SnapshotScenario scenario, UiTestHost host, List<string> values)
        {
            var scene = host.Scene;
            if (scene == null || !scene.IsShowing)
            {
                yield break;
            }

            var wanted = scenario.Host.SceneWater;
            var showing = scene.ShowingAdvancedWater ? WaterKind.Advanced : WaterKind.Degraded;
            values.Add("water: " + showing + " (pedida " + wanted + "; " + scene.WaterChoice + "; fundido " + F(scene.DegradedBlend) + ")");
            if (scenario.SceneMayBeLoading)
            {
                yield break;
            }

            if (wanted == WaterForce.Degraded && showing != WaterKind.Degraded)
            {
                yield return "la toma pide la degradada y se ve la avanzada (DT6b-24)";
            }
            else if (wanted != WaterForce.Degraded && showing != WaterKind.Advanced)
            {
                yield return "la toma pide la avanzada y se ve la degradada";
            }
        }

#if ROWMATE_SIMULATOR
        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, EnvironmentId env, QualityTier tier, string state,
                                              Func<ScenarioScript, IEnumerator> arrange, bool panels = false)
        {
            var preferences = new MemoryPreferences();
            if (panels)
            {
                preferences.SetBool(SeriesPanelPrefs.LapsKey, true);
                preferences.SetBool(SeriesPanelPrefs.RepsKey, true);
            }

            if (env != EnvironmentId.Lake)
            {
                preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(env));
            }

            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = HostBackends.TestRower(RowerScripts.R4),
                SimulatorLayer = SimulatorLayerMode.None,
                LanguageTags = new[] { "es-AR" },
                Routines = HostRoutines.Seeded(RoutineSeeds.F5),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                Preferences = preferences,
                Scene = SceneMode.Real,
                SceneTier = tier,
                SceneCharacter = RowerCharacterKind.Mannequin,
                SceneWater = WaterForce.Auto,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "R-4", ExactInstant = true };
        }

        private static string Cap(int n)
        {
            return "CAP-" + n.ToString("00", CultureInfo.InvariantCulture);
        }

        // CAP-01 a -26: la matriz (escenario × hora en TV; TH a las 12; la degradada forzada; Medio y Bajo).
        private static SnapshotScenario Matrix(int n, EnvironmentId env, int hour, DeviceProfile profile, WaterForce? water, QualityTier tier)
        {
            var key = EnvironmentKeys.ToKey(env);
            var suffix = water == WaterForce.Degraded ? "-degradada" : tier != QualityTier.High ? "-" + tier.ToString().ToLowerInvariant() : "";
            var stem = Cap(n) + "-" + key + "-" + hour.ToString("00", CultureInfo.InvariantCulture) + "-" + profile.Code.ToLowerInvariant() + suffix;
            var scenario = Scene(Cap(n), stem, profile, env, tier,
                "Agua " + (water == WaterForce.Degraded ? "degradada (-rowmate-water degraded)" : "avanzada") + ": " + key + ", "
                + hour.ToString("00", CultureInfo.InvariantCulture) + ":00, nivel " + tier + ", libre R-4 con el HUD, E-" + F(EnvironmentSnapshotCatalog.DistanceFor(env)),
                s => Arrange(s, env, hour, EnvironmentSnapshotCatalog.DistanceFor(env), false));
            scenario.Water = water;
            scenario.Inspect = (h, frame, values) => Probe(h, values, env, hour);
            // WP-D: las sondas de color (WaterProbes): la costura en CAP-01 a -16, las 10 líneas con la degradada y el camino de brillo a
            // las 12 h con el astro sobre la normal (Alto y Medio).
            var degraded = water == WaterForce.Degraded;
            var basic = WaterProbes.For(n <= 16, degraded, hour == 12 && !degraded && tier != QualityTier.Low, false, env == EnvironmentId.Coast);
            // QA-V6b-07: las sondas de la spec (WaterSpecProbes) en las tomas de Alto con la avanzada.
            var spec = !degraded && tier == QualityTier.High ? WaterSpecProbes.ForMatrix(n, env, hour, profile) : null;
            scenario.SceneProbes = spec == null ? basic : (h, v) => Concat(basic(h, v), spec(h, v));
            return scenario;
        }

        private static SnapshotScenario Glass(int n, EnvironmentId env, int hour, DeviceProfile profile, string state)
        {
            var key = EnvironmentKeys.ToKey(env);
            var scenario = Scene(Cap(n), Cap(n) + "-vidrio-" + key + "-" + profile.Code.ToLowerInvariant(), profile, env, QualityTier.Medium, state,
                s => Arrange(s, env, hour, EnvironmentSnapshotCatalog.DistanceFor(env), false));
            scenario.Inspect = (h, frame, values) => GlassProbe(h, values);
            return scenario;
        }

        // CAP-27 (QA-V6b-07): Series 600 con los dos paneles (vueltas y repeticiones), lago 12:00, Alto: el vidrio sobre la orilla.
        private static SnapshotScenario SeriesGlass()
        {
            var scenario = Scene("CAP-27", "CAP-27-vidrio-lake-tv", DeviceProfile.TV, EnvironmentId.Lake, QualityTier.High,
                "Vidrio sobre la orilla: Series 600 a los 50,2 s con los paneles de vueltas y repeticiones, lago 12:00, Alto",
                s => RoutineArrange(s, "Series 600", 50.2, 12), panels: true);
            scenario.Inspect = (h, frame, values) => GlassProbe(h, values);
            scenario.SceneProbes = WaterSpecProbes.GlassVariation();
            return scenario;
        }

        // CAP-29 (QA-V6b-04): Corta hasta el diálogo de completado de los 36,3 s con la toma aérea (lago 12:00, Alto), con los datos de la sesión.
        private static SnapshotScenario Completion()
        {
            var scenario = Scene("CAP-29", "CAP-29-dialogo-vidrio-tv", DeviceProfile.TV, EnvironmentId.Lake, QualityTier.High,
                "Corta a los 36,3 s: el diálogo de completado de vidrio con la toma aérea (lago 12:00, Alto)", s => CompletionArrange(s));
            scenario.MotionMayRun = true;
            scenario.Inspect = (h, frame, values) => GlassProbe(h, values);
            scenario.SceneProbes = WaterSpecProbes.SternUnderDialog();
            return scenario;
        }

        private static SnapshotScenario Marathon()
        {
            var scenario = Scene("CAP-30", "CAP-30-lago-42k-w1280", DeviceProfile.W1280, EnvironmentId.Lake, QualityTier.High,
                "Lago a E-42K (42 195 m), 12:00, W1280: sin saltos de las fases en double", s => Arrange(s, EnvironmentId.Lake, 12, 42195, false));
            scenario.Inspect = (h, frame, values) => Probe(h, values, EnvironmentId.Lake, 12);
            return scenario;
        }

        private static SnapshotScenario Stern()
        {
            var scenario = Scene("CAP-31", "CAP-31-popa-reflejo-w1280", DeviceProfile.W1280, EnvironmentId.Lake, QualityTier.High,
                "La popa (#85) y su reflejo planar (#13): lago 12:00, W1280", s => Arrange(s, EnvironmentId.Lake, 12, 920, false));
            scenario.Inspect = (h, frame, values) => Probe(h, values, EnvironmentId.Lake, 12);
            scenario.SceneProbes = WaterProbes.For(false, false, false, true);
            return scenario;
        }

        private static SnapshotScenario Puddles()
        {
            var scenario = Scene("CAP-32", "CAP-32-charcos-anillos-th", DeviceProfile.TH, EnvironmentId.Lake, QualityTier.High,
                "Escena sola, lago 12:00, fase forzada y charcos de 1,0 s: hervidero, charcos, remolinos a 40° y micro-anillos (#12)",
                s => PuddleArrange(s));
            scenario.Inspect = (h, frame, values) => Probe(h, values, EnvironmentId.Lake, 12);
            scenario.SceneProbes = WaterSpecProbes.PuddlesAndBoil();
            return scenario;
        }

        private static IEnumerator Arrange(ScenarioScript s, EnvironmentId env, double hour, double distance, bool solo)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            scene.ForceDistanceForTest(distance);
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.ForceCanonicalForTest(MatrixPhase);
            scene.PrimeWakeForTest(SteadySpeedMps);
            if (solo)
            {
                scene.SetScrimsForTest(false);
                h.Immersive?.View.HideForCapture();
            }

            // El fundido de 0,6 s de la avanzada hasta el final.
            for (var i = 0; i < 45; i++)
            {
                h.SceneTick();
            }

            yield return s.Frames(3);
        }

        // Una rutina hasta "at" con la escena de la toma: el agua avanzada lista, la hora fija, el tiempo de escena y la fase forzados y la
        // estela precargada (como Arrange), sin forzar la distancia (la de la rutina).
        private static IEnumerator RoutineArrange(ScenarioScript s, string name, double at, double hour)
        {
            s.ReadingZeroAtStart = true;
            yield return s.Tap("tab-workout");
            yield return s.StartRoutine(name);
            yield return s.AdvanceTo(2.2);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            yield return s.AdvanceTo(at);
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.ForceCanonicalForTest(MatrixPhase);
            scene.PrimeWakeForTest(SteadySpeedMps);
            for (var i = 0; i < 45; i++)
            {
                h.SceneTick();
            }

            yield return s.Frames(3);
        }

        // CAP-29: Corta de verdad (el diálogo con los datos de la sesión), la hora en 12 y la avanzada lista desde el principio; la toma aérea
        // arranca a los 30,3 s y queda quieta desde los 36,3 s (VID-08 de la 6a).
        private static IEnumerator CompletionArrange(ScenarioScript s)
        {
            s.ReadingZeroAtStart = true;
            yield return s.Tap("tab-workout");
            yield return s.StartRoutine("Corta");
            yield return s.AdvanceTo(2.2);
            var scene = s.Host.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            scene.ForceHourForTest(12);
            yield return s.AdvanceTo(36.3);
            yield return s.Frames(3);
        }

        private static IEnumerator PuddleArrange(ScenarioScript s)
        {
            yield return Arrange(s, EnvironmentId.Lake, 12, 920, true);
            var scene = s.Host.Scene!;
            scene.InjectPuddleForTest(1.2, 920 + 7.0, 1.0);
            scene.InjectPuddleForTest(-1.2, 920 + 7.0, 1.0);
            // QA-V6b-03: los micro-anillos de las gotas (0,25 s) y del ataque (0,4 s) al lado de los charcos (con la palada congelada no nacen).
            scene.InjectRingForTest(1.75, 920 + 7.7, 0.25, 0);
            scene.InjectRingForTest(-1.75, 920 + 7.6, 0.25, 0);
            scene.InjectRingForTest(2.2, 920 + 8.8, 0.4, 1);
            scene.InjectRingForTest(-2.2, 920 + 8.8, 0.4, 1);
            s.Host.SceneTick();
            yield return s.Frames(3);
        }

        // ── Los videos (spec §5; QA-V6b-07): 30 cuadros por segundo, el reloj de prueba, los valores del agua por cuadro en el manifiesto ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            // VID-01 (AC-6B-20 b): lago W1280, el bote cruzando 4096 m (desde 4090 m, R-4 a ≈ 3,8 m/s); la diferencia entre cuadros del agua
            // cercana no tiene un pico > 1,5 × la mediana.
            var vid01 = Scene("VID-01", "VID-01-cruce-4096-w1280", DeviceProfile.W1280, EnvironmentId.Lake, QualityTier.High,
                "Lago 12:00, W1280, Alto: R-4 desde 4090 m cruzando 4096 m (3 s)", s => LiveArrange(s, 12, 4090, false));
            var jumps = new WaterFrameDiff();
            Video(vid01, 3).FrameCheck = (h, t) => jumps.Check(h, t, 3);
            all.Add(vid01);

            // VID-02 (AC-6B-07, -08): TV, lago 12:00, la avanzada a la vista y la falla simulada a los 1,0 s: el fundido de 0,6 s a la degradada
            // con la sesión que sigue (4 s). La vuelta a la avanzada es al arrancar la app sin falla (todas las demás tomas).
            var vid02 = Scene("VID-02", "VID-02-falla-a-degradada-tv", DeviceProfile.TV, EnvironmentId.Lake, QualityTier.High,
                "Lago 12:00, TV: falla simulada del agua avanzada a 1,0 s del video y el fundido a la degradada (4 s)", s => LiveArrange(s, 12, double.NaN, false));
            var fallback = new FallbackTracker();
            Video(vid02, 4).At(1.0, h => h.Scene!.ReportWaterFailure("falla simulada del arnés")).FrameCheck = (h, t) => fallback.Check(h, t);
            all.Add(vid02);

            // VID-03 (AC-6B-14; QA-V6b-02): TV, lago 12:00, la estela con la historia REAL (sin precarga): R-4 desde el toque hasta los 9 s, Pausa a
            // los 9 s y 5 s más (14 s): nace en la popa, se arma con la velocidad y se apaga con la inercia mientras el agua sigue.
            var vid03 = Scene("VID-03", "VID-03-estela-tv", DeviceProfile.TV, EnvironmentId.Lake, QualityTier.High,
                "Lago 12:00, TV: R-4 desde la entrada, Pausa a los 9 s del video (14 s), la estela sin precarga", s => FromStartArrange(s, 12));
            var wake = new WakeTracker();
            Video(vid03, 14).At(0, h => Start(h, "workout-free")).At(0.5, h => h.Scene?.CompleteWaterForTest()).At(9, h => h.Tap("hud-pause"))
                .FrameCheck = (h, t) => wake.Check(h, t);
            all.Add(vid03);

            // VID-04 (AC-6B-18, -34): TV, río E-250 libre, Hora escena 18 → 22 a los 0,5 s (1,5 s por el camino más corto): las columnas de luz se
            // encienden con las luces (6 s).
            var vid04 = Scene("VID-04", "VID-04-columnas-rio-tv", DeviceProfile.TV, EnvironmentId.River, QualityTier.High,
                "Río, TV: Hora escena 18:00 → 22:00 a los 0,5 s (6 s)", s => HourArrange(s, 18));
            var columns = new ColumnTracker();
            Video(vid04, 6).At(0.5, h => h.App.Scene.HourOverride.Value = 22).FrameCheck = (h, t) => columns.Check(h, t);
            all.Add(vid04);

            // VID-05 (AC-6B-19): TH, costa, Series 600 de los 145 a los 153 s (la boya de 500 m pasa a la altura del remero a los 155 s): el mar de
            // fondo, el casco y la boya con la misma ola.
            var vid05 = Scene("VID-05", "VID-05-costa-boya-th", DeviceProfile.TH, EnvironmentId.Coast, QualityTier.High,
                "Costa 12:00, TH: Series 600 de los 145 a los 153 s (8 s)", s => RoutineLive(s, "Series 600", 145, 12));
            var swell = new SwellTracker();
            Video(vid05, 8).FrameCheck = (h, t) => swell.Check(h, t);
            all.Add(vid05);
        }

        private static VideoSpec Video(SnapshotScenario scenario, double seconds)
        {
            scenario.Kind = SnapshotKind.Video;
            scenario.Video = new VideoSpec(seconds) { FrameValues = (h, t) => WaterValues(h, t) };
            return scenario.Video;
        }

        // Libre con la escena viva (sin congelar el tiempo ni precargar la estela): el agua lista, la hora, y la distancia si se pide.
        private static IEnumerator LiveArrange(ScenarioScript s, double hour, double distance, bool solo)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(2.2);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            scene.ForceHourForTest(hour);
            yield return s.AdvanceTo(12);
            if (!double.IsNaN(distance))
            {
                scene.ForceDistanceForTest(distance);
            }

            for (var i = 0; i < 45; i++)
            {
                h.Step();
                yield return null;
            }
        }

        // VID-03: desde el toque (la estela nace con la sesión).
        private static IEnumerator FromStartArrange(ScenarioScript s, int hour)
        {
            yield return s.Do(h => h.App.Scene.HourOverride.Value = hour);
            yield return s.Tap("tab-workout");
        }

        private static void Start(UiTestHost h, string element)
        {
            h.Scheduler.ShiftUtc(UiHostOptions.WorkoutStartUtc - h.Scheduler.UtcNow);
            h.Tap(element);
        }

        private static IEnumerator HourArrange(ScenarioScript s, int hour)
        {
            yield return s.Do(h => h.App.Scene.HourOverride.Value = hour);
            yield return s.StartFree();
            yield return s.AdvanceTo(2.2);
            var scene = s.Host.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            yield return s.AdvanceTo(8);
            scene.ForceDistanceForTest(EnvironmentSnapshotCatalog.DistanceFor(EnvironmentId.River));
            for (var i = 0; i < 45; i++)
            {
                s.Host.Step();
                yield return null;
            }
        }

        private static IEnumerator RoutineLive(ScenarioScript s, string name, double at, double hour)
        {
            s.ReadingZeroAtStart = true;
            yield return s.Tap("tab-workout");
            yield return s.StartRoutine(name);
            yield return s.AdvanceTo(2.2);
            var scene = s.Host.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.CompleteWaterForTest();
            scene.ForceHourForTest(hour);
            yield return s.AdvanceTo(at);
        }

        private static string WaterValues(UiTestHost h, double t)
        {
            var scene = h.Scene;
            if (scene?.Session == null)
            {
                return "t " + F(t) + " s; sin escena";
            }

            var f = scene.Frame;
            var wake = scene.World?.Director?.Wake;
            var marks = scene.World?.Director?.WaterMarks;
            return "t " + F(t) + " s; d " + F(f.Distance) + " m; v " + F(f.Speed) + " m/s; hora " + F(f.VisibleHour) + "; agua " + (scene.ShowingAdvancedWater ? "avanzada" : "degradada")
                   + " (fundido " + F(scene.DegradedBlend) + "); estela " + (wake != null ? wake.LivePoints + " puntos, cabeza " + F(wake.HeadIntensity) : "—")
                   + "; columnas " + (marks?.Columns ?? 0) + "; boya arfada " + F(f.Water.BuoyHeave) + " m; φ₁ " + F(f.Water.Phases.Phase1);
        }

        private static List<ProbeRecord> Concat(List<ProbeRecord> a, List<ProbeRecord> b)
        {
            a.AddRange(b);
            return a;
        }

        // Lo de cada toma en el manifiesto (informativo: las sondas de color de la spec las cierra WP-D en su ronda de ajuste).
        private static IEnumerable<string> Probe(UiTestHost h, List<string> values, EnvironmentId env, double hour)
        {
            var scene = h.Scene!;
            var f = scene.Frame;
            var marks = scene.World?.Director?.WaterMarks;
            var wake = scene.World?.Director?.Wake;
            values.Add("escenario " + env + "; kit " + scene.KitEnvironment + "; d " + F(f.Distance) + "; hora " + F(f.VisibleHour) + "; nivel " + scene.Tier);
            values.Add("reflejo planar " + scene.ReflectionDrawn + " (peso " + F(scene.ReflectionMix) + "); estela " + (wake != null ? wake.LivePoints + " puntos, intensidad " + F(wake.HeadIntensity) : "—")
                       + "; marcas " + (marks != null ? marks.Puddles + " charcos, " + marks.Boils + " hervideros, " + marks.Rings + " anillos, " + marks.Columns + " columnas" : "—")
                       + "; boya arfada " + F(f.Water.BuoyHeave) + " m; fases φ₁ " + F(f.Water.Phases.Phase1) + ", φ₂ " + F(f.Water.Phases.Phase2));
            var problems = new List<string>();
            if (scene.KitEnvironment != env)
            {
                problems.Add("el escenario de la escena no es el forzado (" + env + ")");
            }

            return problems;
        }

        private static IEnumerable<string> GlassProbe(UiTestHost h, List<string> values)
        {
            var scene = h.Scene!;
            var glass = h.Immersive?.View.Glass;
            values.Add("vidrio: peso " + F(scene.GlassWeight) + ", corridas " + scene.BlurRuns + ", piezas " + (glass?.Pieces.Count ?? 0) + ", textura "
                       + (scene.GlassTexture != null ? scene.GlassTexture.width + "×" + scene.GlassTexture.height : "—"));
            var problems = new List<string>();
            if (scene.Tier != QualityTier.Low && scene.GlassWeight < 0.999)
            {
                problems.Add("el vidrio no llegó a pleno (peso " + F(scene.GlassWeight) + ")");
            }

            return problems;
        }
#endif

        private static string F(double value)
        {
            return double.IsNaN(value) ? "—" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
