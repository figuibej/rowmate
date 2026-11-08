using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it8 (08-entornos-selector/02-tech-design §6; por defecto desde la 8) y el horneado de las miniaturas ("thumbs"). Reglas de
    // la 6a §5.1 y la 7 §5: la escena real en nivel Alto, el Editor sin -nographics, la compilación de shaders sincrónica, pasos de 1/60 s,
    // el reloj de prueba, español, una MemoryPreferences por toma con el escenario forzado (scene.environment), el remo de prueba R-4 y el
    // maniquí. Matriz = libre R-4 con el HUD, tiempo de escena 3,2 s, c = 0,18, la hora fijada y la distancia del escenario (lago E-920,
    // río E-250, costa E-500, regata E-180). Escena sola = HUD oculto, sin degradés y respiración 0. Las sondas (escenario del kit, luces
    // de la noche: issue #9, ola, puente) van al manifest.
    public static class EnvironmentSnapshotCatalog
    {
        public const double FrozenSceneTime = 3.2;
        public const double MatrixPhase = 0.18;
        public const string ThumbnailsFolder = "Assets/RowMate/Art/Textures/Thumbnails";

        private static readonly EnvironmentId[] Order = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };
        private static readonly int[] Hours = { 6, 12, 18, 22 };

        public static double DistanceFor(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.River:
                    return 250;
                case EnvironmentId.Coast:
                    return 500;
                case EnvironmentId.Regatta:
                    return 180;
                default:
                    return 920;
            }
        }

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddMatrix(all);
                AddThumbnailCopies(all);
                AddPicker(all);
                AddSolo(all);
                AddMarks(all);
                AddVideos(all);
                AddRestart(all);
                all.Add(Demo());
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // El horneado (tools/unity/bake-thumbnails.ps1): las 4 miniaturas a Art/Textures/Thumbnails con su thumbnails.json.
        public static IReadOnlyList<SnapshotScenario> Thumbnails
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                for (var i = 0; i < Order.Length; i++)
                {
                    var id = Order[i];
                    var scenario = Scene("THUMB-" + (i + 1), "THUMB-" + EnvironmentKeys.ToKey(id), DeviceProfile.TV, id,
                        "Miniatura horneada de " + EnvironmentKeys.ToKey(id) + " (ThumbnailRule: 12:00, 20 m, sin bote, relZ < 150)", s => ThumbArrange(s));
                    scenario.Inspect = (h, frame, values) => Bake(h, frame, values, id, true);
                    all.Add(scenario);
                }
#endif
                return all;
            }
        }

#if ROWMATE_SIMULATOR
        // ── CAP-01 a -32: la matriz (4 escenarios × 4 horas × TV y TH) ──
        private static void AddMatrix(List<SnapshotScenario> all)
        {
            var n = 1;
            foreach (var id in Order)
            {
                foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.TH })
                {
                    foreach (var hour in Hours)
                    {
                        var env = id;
                        var hh = hour;
                        var key = EnvironmentKeys.ToKey(id);
                        var stem = "CAP-" + n.ToString("00", CultureInfo.InvariantCulture) + "-" + key + "-" + hour.ToString("00", CultureInfo.InvariantCulture)
                                   + "-" + profile.Code.ToLowerInvariant();
                        var scenario = Scene("CAP-" + n.ToString("00", CultureInfo.InvariantCulture), stem, profile, id,
                            "Matriz: " + key + ", " + hour.ToString("00", CultureInfo.InvariantCulture) + ":00, libre R-4 con el HUD, E-" + F(DistanceFor(id)),
                            s => MatrixArrange(s, env, hh, false));
                        scenario.Inspect = (h, frame, values) => Probe(h, values, env, hh);
                        if (hour == 22)
                        {
                            // Issue #9 (QA-V6a-08): la ventana y el frente de la cabaña, las ventanas de edificios y clubes, el farol, el faro.
                            scenario.SceneProbes = EnvironmentMarks.NightProbes;
                        }

                        all.Add(scenario);
                        n++;
                    }
                }
            }
        }

        // ── CAP-33 a -36: las miniaturas horneadas, 1:1 (la copia del PNG versionado) ──
        private static void AddThumbnailCopies(List<SnapshotScenario> all)
        {
            for (var i = 0; i < Order.Length; i++)
            {
                var id = Order[i];
                var scenario = Scene("CAP-" + (33 + i).ToString(CultureInfo.InvariantCulture), "CAP-" + (33 + i).ToString(CultureInfo.InvariantCulture)
                    + "-miniatura-" + EnvironmentKeys.ToKey(id), DeviceProfile.TV, id, "La miniatura horneada de " + EnvironmentKeys.ToKey(id) + " (1:1)",
                    s => ThumbArrange(s));
                scenario.Inspect = (h, frame, values) => CopyThumbnail(frame, values, id);
                all.Add(scenario);
            }
        }

        // ── CAP-37 a -39: el selector en Workout en reposo ──
        private static void AddPicker(List<SnapshotScenario> all)
        {
            var cap37 = Rest("CAP-37", "CAP-37-selector-tv", DeviceProfile.TV, EnvironmentId.Lake, false, "es-AR",
                "Workout en reposo, TV, sin remo, lago elegido", null);
            cap37.Inspect = (h, frame, values) => PickerProbe(h, values, EnvironmentId.Lake);
            all.Add(cap37);
            var cap38 = Rest("CAP-38", "CAP-38-selector-w1280", DeviceProfile.W1280, EnvironmentId.River, true, "es-AR",
                "Workout en reposo, W1280, con remo, río elegido (scene.environment = river)", null);
            cap38.Inspect = (h, frame, values) => PickerProbe(h, values, EnvironmentId.River);
            all.Add(cap38);
            var cap39 = Rest("CAP-39", "CAP-39-selector-tv-en", DeviceProfile.TV, EnvironmentId.Coast, true, "en-US",
                "Workout en reposo, TV, inglés, costa elegida, foco de teclado en Regatta course", s => FocusRegatta(s));
            cap39.Inspect = (h, frame, values) => PickerProbe(h, values, EnvironmentId.Coast);
            all.Add(cap39);
        }

        // ── CAP-40 a -43: la escena sola ──
        private static void AddSolo(List<SnapshotScenario> all)
        {
            all.Add(Solo("CAP-40", "CAP-40-regata-cartel-tv", DeviceProfile.TV, EnvironmentId.Regatta, 12, 220, "Regata sola, TV 12:00, E-220: el cartel de 250 m"));
            var cap41 = Solo("CAP-41", "CAP-41-regata-noche-th", DeviceProfile.TH, EnvironmentId.Regatta, 22, 720, "Regata sola, TH 22:00, E-720: el cartel de 750 m de noche");
            cap41.SceneProbes = EnvironmentMarks.NightProbes;
            all.Add(cap41);
            all.Add(Solo("CAP-42", "CAP-42-costa-ola-th", DeviceProfile.TH, EnvironmentId.Coast, 12, 500, "Costa sola, TH 12:00, E-500: la ola en el casco"));
            all.Add(Solo("CAP-43", "CAP-43-rio-puente-tv", DeviceProfile.TV, EnvironmentId.River, 12, 266, "Río solo, TV 12:00, E-266: bajo el puente"));
        }

        // ── CAP-44 a -46: la escena sola en V390 con las marcas del legacy, al lado de LEG-01 a -03 (AC-SCN-14-10) ──
        private static void AddMarks(List<SnapshotScenario> all)
        {
            var envs = new[] { (EnvironmentId.River, "rio"), (EnvironmentId.Coast, "costa"), (EnvironmentId.Regatta, "regata") };
            for (var i = 0; i < envs.Length; i++)
            {
                var env = envs[i].Item1;
                var id = "CAP-" + (44 + i).ToString(CultureInfo.InvariantCulture);
                var scenario = Scene(id, id + "-" + envs[i].Item2 + "-marcas-v390", DeviceProfile.V390, env,
                    "Escena sola, V390 12:00, E-" + F(DistanceFor(env)) + ", con las marcas del legacy (LEG-0" + (i + 1).ToString(CultureInfo.InvariantCulture) + ")",
                    s => SoloArrange(s, 12, DistanceFor(env)));
                scenario.Inspect = (h, frame, values) =>
                {
                    var problems = new List<string>(Probe(h, values, env, 12));
                    problems.AddRange(EnvironmentMarks.Marks(h, frame, values, env));
                    return problems;
                };
                all.Add(scenario);
            }
        }

        // ── VID-06: elegir Costa, cerrar y abrir la app (el anfitrión se desarma y se arma con la misma carpeta: DT8-17) y remar ──
        private static void AddRestart(List<SnapshotScenario> all)
        {
            var scenario = Scene("VID-06", "VID-06-reinicio-costa-w1280", DeviceProfile.W1280, EnvironmentId.Lake,
                "W1280, 30 s: Costa en el selector, la app se cierra y se abre sobre las mismas preferencias, Entrenamiento libre", s => RestArrange(s));
            WithPreferencesFile(scenario);
            scenario.Kind = SnapshotKind.Video;
            scenario.MotionMayRun = true;
            scenario.SceneMayBeLoading = true;
            scenario.Video = new VideoSpec(30)
                .At(2, h => h.Tap("env-card-coast"))
                .At(8, h =>
                {
                    h.FlushPreferences();
                    h.Reopen();
                })
                .At(9, h => h.Tap("tab-workout"))
                .At(14, h => h.Tap("workout-free"));
            scenario.Video.FrameValues = (h, t) =>
            {
                var scene = h.Scene;
                return "t " + F(t) + "; generación " + h.Generation + "; elegido " + h.App.SceneEnvironment.Selected.Value + "; tarjeta costa "
                       + ((h.Find("env-card-coast") as RowMate.Presentation.Workout.EnvironmentCard)?.IsSelected.ToString() ?? "—") + "; kit "
                       + (scene?.KitEnvironment.ToString() ?? "—") + "; sesión " + (scene?.Session?.Environment.Id.ToString() ?? "—") + "; "
                       + (scene?.Readiness.Value.ToString() ?? "sin escena");
            };
            scenario.Video.FrameCheck = (h, t) =>
            {
                if (h.Generation >= 1 && h.App.SceneEnvironment.Selected.Value != EnvironmentId.Coast)
                {
                    return "después de reabrir, el elegido es " + h.App.SceneEnvironment.Selected.Value + " (no Coast)";
                }

                var session = h.Scene?.Session;
                if (h.Generation >= 1 && session != null && session.Environment.Id != EnvironmentId.Coast)
                {
                    return "la sesión después de reabrir es " + session.Environment.Id + " (no Coast)";
                }

                return null;
            };
            all.Add(scenario);
        }

        // ── DEMO-01 a -05: la demo de la spec §1 en una secuencia W1280 (el paso 5 reabre sobre las mismas preferencias) ──
        private static SnapshotScenario Demo()
        {
            var demo = Scene("DEMO", "DEMO", DeviceProfile.W1280, EnvironmentId.Lake, "Secuencia de la demo (spec §1)", DemoScript);
            WithPreferencesFile(demo);
            demo.Kind = SnapshotKind.Sequence;
            demo.SceneMayBeLoading = true;
            demo.Shot("DEMO-01", "DEMO-01-selector-lago", "Workout en reposo: Escenario con las 4 tarjetas, Lago alpino elegido")
                .Shot("DEMO-02", "DEMO-02-rio-noche", "Río urbano → Entrenamiento libre; E-262 (el puente de 270 m) y Hora escena 22:00")
                .Shot("DEMO-03", "DEMO-03-costa", "Terminar; Costa → Entrenamiento libre; E-520 (el faro de 530 m), 12:00")
                .Shot("DEMO-04", "DEMO-04-regata", "Terminar; Canal de regata → Entrenamiento libre; E-205 (tribuna de 210 m, cartel de 250 m), 12:00")
                .Shot("DEMO-05", "DEMO-05-reabierta-regata", "Terminar; cerrar y abrir la app: Canal de regata elegido desde el primer cuadro");
            return demo;
        }

        private static IEnumerator DemoScript(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Frames(3);
            yield return s.Shot("DEMO-01", h => Expect(("Lago alpino elegido", () => Card(h, EnvironmentId.Lake)?.IsSelected == true),
                ("las 4 tarjetas", () => Card(h, EnvironmentId.River) != null && Card(h, EnvironmentId.Coast) != null && Card(h, EnvironmentId.Regatta) != null)));

            yield return DemoRow(s, EnvironmentId.River, 262, 22);
            yield return s.Shot("DEMO-02", h => SessionIs(h, EnvironmentId.River, 22));
            yield return DemoFinish(s);

            yield return DemoRow(s, EnvironmentId.Coast, 520, 12);
            yield return s.Shot("DEMO-03", h => SessionIs(h, EnvironmentId.Coast, 12));
            yield return DemoFinish(s);

            yield return DemoRow(s, EnvironmentId.Regatta, 205, 12);
            yield return s.Shot("DEMO-04", h => SessionIs(h, EnvironmentId.Regatta, 12));
            yield return DemoFinish(s);

            yield return s.Do(h => h.FlushPreferences());
            yield return s.Reopen();
            yield return s.Tap("tab-workout");
            yield return s.Frames(3);
            yield return s.Shot("DEMO-05", h => Expect(("Canal de regata elegido", () => Card(h, EnvironmentId.Regatta)?.IsSelected == true),
                ("la preferencia leída del archivo", () => h.App.SceneEnvironment.Selected.Value == EnvironmentId.Regatta), ("la app reabierta", () => h.Generation == 1)));
        }

        // Elegir la tarjeta, Entrenamiento libre, la distancia y la hora (Hora escena, como el simulador) y 2 s más.
        private static IEnumerator DemoRow(ScenarioScript s, EnvironmentId env, double distance, int hour)
        {
            yield return s.Tap("tab-workout");
            yield return s.Tap("env-card-" + EnvironmentKeys.ToKey(env));
            yield return s.StartFree();
            yield return s.AdvanceTo(3.2);
            yield return s.Do(h =>
            {
                h.Scene!.ForceDistanceForTest(distance);
                h.App.Scene.HourOverride.Value = hour;
            });
            yield return s.Advance(2);
        }

        private static IEnumerator DemoFinish(ScenarioScript s)
        {
            yield return s.Do(h => h.App.Scene.HourOverride.Value = null);
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Advance(1);
        }

        private static IEnumerable<string> SessionIs(UiTestHost h, EnvironmentId env, int hour)
        {
            return Expect(("la sesión en " + env, () => h.Scene?.Session?.Environment.Id == env), ("el kit de " + env, () => h.Scene?.KitEnvironment == env),
                ("hora visible " + F(hour), () => Math.Abs((h.Scene?.Frame.VisibleHour ?? -1) - hour) < 1e-6));
        }

        private static RowMate.Presentation.Workout.EnvironmentCard? Card(UiTestHost h, EnvironmentId env)
        {
            return h.Find("env-card-" + EnvironmentKeys.ToKey(env)) as RowMate.Presentation.Workout.EnvironmentCard;
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
                catch (Exception e)
                {
                    problems.Add(what + ": " + e.GetType().Name + " " + e.Message);
                    continue;
                }

                if (!passed)
                {
                    problems.Add(what);
                }
            }

            return problems;
        }

        private static void WithPreferencesFile(SnapshotScenario scenario)
        {
            // El almacén real (JsonFilePreferences) en la carpeta temporal del anfitrión: Reopen lo vuelve a leer (DT8-17).
            scenario.Host.Preferences = null;
            scenario.Host.PreferencesFile = true;
        }

        // ── VID-01 a -05: 20 s (10 s el quinto) remando por cada escenario ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            AddVideo(all, "VID-01", "VID-01-lago-tv", DeviceProfile.TV, EnvironmentId.Lake, 900, 20, false, "Lago, TV, 20 s desde E-900");
            AddVideo(all, "VID-02", "VID-02-rio-th", DeviceProfile.TH, EnvironmentId.River, 200, 20, false, "Río, TH, 20 s desde E-200 (el puente de 270 m)");
            AddVideo(all, "VID-03", "VID-03-costa-tv", DeviceProfile.TV, EnvironmentId.Coast, 470, 20, false, "Costa, TV, 20 s desde E-470 (el faro de 530 m, gaviotas, ola)");
            AddVideo(all, "VID-04", "VID-04-regata-th", DeviceProfile.TH, EnvironmentId.Regatta, 190, 20, false, "Regata, TH, 20 s desde E-190 (tribuna, cartel de 250 m)");
            AddVideo(all, "VID-05", "VID-05-costa-pausa-th", DeviceProfile.TH, EnvironmentId.Coast, 500, 10, true, "Costa sola, TH, 10 s en pausa: la ola mueve el bote");
        }

        private static void AddVideo(List<SnapshotScenario> all, string id, string stem, DeviceProfile profile, EnvironmentId env, double from, double seconds,
                                     bool paused, string state)
        {
            var scenario = Scene(id, stem, profile, env, state, s => VideoArrange(s, from, paused));
            scenario.Kind = SnapshotKind.Video;
            scenario.MotionMayRun = true;
            scenario.Video = new VideoSpec(seconds)
            {
                FrameValues = (h, t) =>
                {
                    var f = h.Scene!.Frame;
                    return "t " + F(t) + "; d " + F(f.Distance) + "; arfada " + F(f.Rower.SwellHeaveM) + " m; cabeceo " + F(f.Rower.SwellPitchRad) + " rad; puente relZ "
                           + F(f.BridgeStartRelZ) + "; bajo el puente " + F(f.UnderBridgeAtBoat);
                },
            };
            all.Add(scenario);
        }

        // ── Armado ──

        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, EnvironmentId env, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                              bool connected = true, string language = "es-AR")
        {
            var preferences = new MemoryPreferences();
            if (env != EnvironmentId.Lake)
            {
                // El escenario forzado (spec §0): scene.environment en las preferencias de la toma.
                preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(env));
            }

            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = connected ? HostBackends.TestRower(RowerScripts.R4) : HostBackends.Unavailable,
                SimulatorLayer = SimulatorLayerMode.None,
                LanguageTags = new[] { language },
                Routines = HostRoutines.Seeded(RoutineSeeds.F4A),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                Preferences = preferences,
                Scene = SceneMode.Real,
                SceneTier = QualityTier.High,
                SceneCharacter = RowerCharacterKind.Mannequin,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = connected ? "R-4" : "—", ExactInstant = true };
        }

        private static SnapshotScenario Rest(string id, string stem, DeviceProfile profile, EnvironmentId env, bool connected, string language, string state,
                                             Func<ScenarioScript, IEnumerator>? arrange)
        {
            var scenario = Scene(id, stem, profile, env, state, arrange ?? (s => RestArrange(s)), connected, language);
            scenario.SceneMayBeLoading = true;
            return scenario;
        }

        private static SnapshotScenario Solo(string id, string stem, DeviceProfile profile, EnvironmentId env, double hour, double distance, string state)
        {
            var scenario = Scene(id, stem, profile, env, state, s => SoloArrange(s, hour, distance));
            scenario.Inspect = (h, frame, values) => Probe(h, values, env, hour);
            return scenario;
        }

        private static IEnumerator RestArrange(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Frames(3);
        }

        private static IEnumerator FocusRegatta(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Frames(2);
            yield return s.Do(h =>
            {
                var card = h.Find("env-card-regatta");
                h.Root.AddToClassList("rm-keyboard-nav");
                card?.Focus();
            });
            yield return s.Frames(3);
        }

        private static IEnumerator Free(ScenarioScript s, double at)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(at);
        }

        private static IEnumerator MatrixArrange(ScenarioScript s, EnvironmentId env, double hour, bool solo)
        {
            yield return Free(s, 2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ForceDistanceForTest(DistanceFor(env));
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.ForceCanonicalForTest(MatrixPhase);
            if (solo)
            {
                scene.SetScrimsForTest(false);
                h.Immersive?.View.HideForCapture();
            }

            h.SceneTick();
            h.SceneTick();
            yield return s.Frames(3);
        }

        private static IEnumerator SoloArrange(ScenarioScript s, double hour, double distance)
        {
            yield return Free(s, 2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ForceDistanceForTest(distance);
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.ForceCanonicalForTest(MatrixPhase);
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            h.SceneTick();
            h.SceneTick();
            yield return s.Frames(3);
        }

        private static IEnumerator VideoArrange(ScenarioScript s, double from, bool paused)
        {
            yield return Free(s, 2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ForceDistanceForTest(from);
            scene.ForceHourForTest(12);
            if (paused)
            {
                scene.SetScrimsForTest(false);
                h.Immersive?.View.HideForCapture();
                h.App.Workout.Pause();
                // QA-V8-10: el bote quieto desde el primer cuadro (sin el deslizamiento de la velocidad que traía).
                scene.StopBoatForTest();
            }

            h.SceneTick();
            yield return s.Frames(2);
        }
#endif

        // La miniatura (ThumbnailRule): la ventana de la tarjeta a 4× (1 824 × 1 152 px = 152 × 96 dp), 12:00, d = 20, tiempo 0, sin bote.
        private static IEnumerator ThumbArrange(ScenarioScript s)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ThumbnailModeForTest(true);
            scene.ViewportForTest(ThumbnailRule.RenderWidth, ThumbnailRule.RenderHeight, ThumbnailRule.RenderWidth / ThumbnailRule.CardWidthDp);
            scene.ForceDistanceForTest(ThumbnailRule.Distance);
            scene.ForceHourForTest(ThumbnailRule.Hour);
            scene.FreezeSceneTimeForTest(ThumbnailRule.SceneTime);
            scene.BreathingOffForTest(true);
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            h.SceneTick();
            h.SceneTick();
            yield return s.Frames(3);
        }

        // ── Las miniaturas ──

        // Dibuja la miniatura a 1 824 × 1 152, la reduce 4× (BoxDownsample de Core) y, con write, la guarda en Art/Textures/Thumbnails
        // con thumbnails.json (DT8-16). La toma del arnés pasa a ser la miniatura misma.
        private static IEnumerable<string> Bake(UiTestHost h, Texture2D frame, List<string> values, EnvironmentId id, bool write)
        {
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            var problems = new List<string>();
            if (scene.KitEnvironment != id)
            {
                problems.Add("el kit armado es " + scene.KitEnvironment + ", no " + id);
            }

            var w = ThumbnailRule.RenderWidth;
            var hgt = ThumbnailRule.RenderHeight;
            var rt = new RenderTexture(new RenderTextureDescriptor(w, hgt, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 1 })
            {
                name = "RowMate.ThumbnailBake",
                hideFlags = HideFlags.HideAndDontSave,
            };
            rt.Create();
            var big = new Texture2D(w, hgt, TextureFormat.RGBA32, false, false);
            try
            {
                scene.RenderToForTest(rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                big.ReadPixels(new Rect(0, 0, w, hgt), 0, 0, false);
                big.Apply(false, false);
                RenderTexture.active = previous;
                var source = big.GetRawTextureData();
                var small = new byte[ThumbnailRule.Width * ThumbnailRule.Height * 4];
                BoxDownsample.Apply(source, w, hgt, ThumbnailRule.Downsample, small);
                for (var i = 3; i < small.Length; i += 4)
                {
                    small[i] = 255;
                }

                frame.Reinitialize(ThumbnailRule.Width, ThumbnailRule.Height, TextureFormat.RGBA32, false);
                frame.LoadRawTextureData(small);
                frame.Apply(false, false);
                values.Add("miniatura " + EnvironmentKeys.ToKey(id) + ": " + ThumbnailRule.Width + " × " + ThumbnailRule.Height + " (de " + w + " × " + hgt
                           + "); objetos a menos de 150 m: " + scene.Frame.VisibleProps.Count + "; segmentos del kit: "
                           + (scene.World?.Director?.Kit?.SegmentsDrawn ?? 0));
                if (write)
                {
                    Directory.CreateDirectory(ThumbnailsFolder);
                    File.WriteAllBytes(Path.Combine(ThumbnailsFolder, EnvironmentKeys.ToKey(id) + ".png"), frame.EncodeToPNG());
                    File.WriteAllText(Path.Combine(ThumbnailsFolder, "thumbnails.json"), ManifestJson(), new UTF8Encoding(false));
                }
            }
            finally
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(big);
            }

            return problems;
        }

        // CAP-33 a -36: la toma es el PNG versionado, tal cual; la huella tiene que estar al día (DT8-16).
        private static IEnumerable<string> CopyThumbnail(Texture2D frame, List<string> values, EnvironmentId id)
        {
            var problems = new List<string>();
            var path = Path.Combine(ThumbnailsFolder, EnvironmentKeys.ToKey(id) + ".png");
            if (!File.Exists(path))
            {
                problems.Add("falta " + path.Replace('\\', '/') + ": correr tools/unity/bake-thumbnails.ps1");
                return problems;
            }

            frame.LoadImage(File.ReadAllBytes(path));
            var json = Path.Combine(ThumbnailsFolder, "thumbnails.json");
            var expected = Fingerprint();
            var upToDate = File.Exists(json) && File.ReadAllText(json).Contains("\"fingerprint\": \"" + expected + "\"");
            values.Add("miniatura " + EnvironmentKeys.ToKey(id) + ": " + frame.width + " × " + frame.height + "; huella " + expected
                       + (upToDate ? " al día" : " VIEJA: volver a hornear"));
            if (!upToDate)
            {
                problems.Add("miniatura vieja: volver a hornear (tools/unity/bake-thumbnails.ps1)");
            }

            if (frame.width != ThumbnailRule.Width || frame.height != ThumbnailRule.Height)
            {
                problems.Add("la miniatura no mide " + ThumbnailRule.Width + " × " + ThumbnailRule.Height);
            }

            return problems;
        }

        public static string ManifestJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"rule\": ").Append(ThumbnailRule.Version.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"width\": ").Append(ThumbnailRule.Width.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"height\": ").Append(ThumbnailRule.Height.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"hour\": ").Append(ThumbnailRule.Hour.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"distance\": ").Append(ThumbnailRule.Distance.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("  \"environments\": [\"lake\", \"river\", \"coast\", \"regatta\"],\n");
            sb.Append("  \"fingerprint\": \"").Append(Fingerprint()).Append("\"\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        // La huella de DT8-16 (la misma cuenta que el test de dotnet: Art/Shaders/** con la ruta relativa y '/').
        public static string Fingerprint()
        {
            var root = Path.GetFullPath("Assets/RowMate/Art/Shaders");
            var shaders = new List<KeyValuePair<string, string>>();
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.Ordinal))
                {
                    continue;
                }

                var rel = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                shaders.Add(new KeyValuePair<string, string>(rel, File.ReadAllText(file)));
            }

            return ThumbnailFingerprint.Compute(shaders);
        }

        // ── Sondas ──

        private static IEnumerable<string> Probe(UiTestHost h, List<string> values, EnvironmentId env, double hour)
        {
            var scene = h.Scene!;
            var f = scene.Frame;
            var lit = 0;
            var lights = new StringBuilder();
            var props = f.VisibleProps.Items;
            for (var i = 0; i < props.Length; i++)
            {
                if (f.PropLight[i] > 0.001)
                {
                    lit++;
                    if (lit <= 6)
                    {
                        lights.Append(props[i].Kind).Append(" (").Append(F(props[i].X)).Append("; relZ ").Append(F(props[i].Z - f.Distance)).Append(") ")
                            .Append(F(f.PropLight[i])).Append("; ");
                    }
                }
            }

            values.Add("escenario " + env + "; kit " + scene.KitEnvironment + "; sesión " + (scene.Session?.Environment.Id.ToString() ?? "—") + "; d " + F(f.Distance)
                       + "; hora " + F(f.VisibleHour) + "; objetos visibles " + props.Length + "; segmentos del kit " + (scene.World?.Director?.Kit?.SegmentsDrawn ?? 0));
            values.Add("luces de noche (sonda del issue #9): " + lit + " encendidas" + (lit > 0 ? ": " + lights : ""));
            values.Add("agua: arriba " + Hex(f.WaterTop) + ", medio " + Hex(f.WaterMid) + ", abajo " + Hex(f.WaterBottom) + "; ola " + (f.Swell ? "sí" : "no")
                       + " (arfada " + F(f.Rower.SwellHeaveM) + " m, cabeceo " + F(f.Rower.SwellPitchRad) + " rad); puente relZ " + F(f.BridgeStartRelZ)
                       + " a " + F(f.BridgeEndRelZ) + ", bajo el puente " + F(f.UnderBridgeAtBoat));
            var problems = new List<string>();
            if (scene.KitEnvironment != env || scene.Session?.Environment.Id != env)
            {
                problems.Add("el escenario de la escena no es el forzado (" + env + ")");
            }

            if (!scene.IsShowing)
            {
                problems.Add("la escena no se está dibujando");
            }

            if (hour >= 22 && env == EnvironmentId.Lake && lit == 0)
            {
                problems.Add("22:00 en el lago sin ninguna luz encendida (issue #9)");
            }

            return problems;
        }

        private static IEnumerable<string> PickerProbe(UiTestHost h, List<string> values, EnvironmentId selected)
        {
            var problems = new List<string>();
            var title = h.BoundsOf("workout-scene-title");
            var picker = h.BoundsOf("env-picker");
            var free = h.BoundsOf("workout-free");
            values.Add("título y " + F(title.y) + ", selector y " + F(picker.y) + " (alto " + F(picker.height) + "), Entrenamiento libre y " + F(free.y));
            var sb = new StringBuilder();
            foreach (var id in Order)
            {
                var card = h.Find("env-card-" + EnvironmentKeys.ToKey(id)) as RowMate.Presentation.Workout.EnvironmentCard;
                if (card == null)
                {
                    problems.Add("falta la tarjeta " + id);
                    continue;
                }

                var r = h.BoundsOf(card.name);
                sb.Append(id).Append(card.IsSelected ? " (elegida)" : "").Append(": x ").Append(F(r.x)).Append(", ancho ").Append(F(r.width)).Append("; ");
                if (card.IsSelected != (id == selected))
                {
                    problems.Add("la tarjeta elegida no es " + id);
                }
            }

            values.Add(sb.ToString());
            if (!(title.y < picker.y && picker.y < free.y))
            {
                problems.Add("el orden no es título, selector, Entrenamiento libre (AC-SCN-22-01)");
            }

            var draws = h.Scene?.Draws ?? 0;
            values.Add("dibujos de la escena en reposo: " + draws);
            return problems;
        }

        private static string Hex(SceneColor c)
        {
            return "#" + ((int)Math.Round(c.R * 255)).ToString("X2", CultureInfo.InvariantCulture) + ((int)Math.Round(c.G * 255)).ToString("X2", CultureInfo.InvariantCulture)
                   + ((int)Math.Round(c.B * 255)).ToString("X2", CultureInfo.InvariantCulture);
        }

        private static string F(double value)
        {
            return double.IsNaN(value) ? "—" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
