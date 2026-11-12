using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it7 (07-remero-palada/01-po-spec §6 → 02-tech-design §5; por defecto desde la 7): CAP-01 a CAP-21 (CAP-17 y CAP-18 con sus
    // 10 poses sueltas y la hoja de 5 × 2; CAP-19 en dos tomas), VID-01 a VID-07 y DEMO-01 a -06 en una secuencia. Reglas de la 6a §5.1:
    // la escena real en nivel Alto, el Editor sin -nographics, la compilación de shaders sincrónica, la escena en pasos de 1/60 s, el reloj
    // de prueba (13:00 UTC, UTC−03:00, 2026-10-28), español, una MemoryPreferences por toma, el remo de prueba R-4 (o el simulador sin ruido)
    // y la regla de toma de la 4b. El remero: el maniquí, salvo las tomas de Y Bot (CAP-18, CAP-21, DEMO-06: NO VERIFICABLE sin el FBX).
    // Escena sola = HUD oculto, sin degradés, respiración 0, tiempo de escena 3,2 s, E-920. Los valores del remero (RowerMarks) van al manifest.
    public static class RowerCatalog
    {
        public const double FrozenSceneTime = 3.2;
        public const double SoloDistance = 920;

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddPoses(all);
                AddEvents(all);
                AddShadows(all);
                AddHud(all);
                AddBuoy(all);
                all.Add(SideSheet("CAP-17", "CAP-17-poses-maniqui", RowerCharacterKind.Mannequin, "maniquí"));
                all.Add(SideSheet("CAP-18", "CAP-18-poses-ybot", RowerCharacterKind.Slot, "Y Bot"));
                AddVideos(all);
                all.Add(Demo());
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

#if ROWMATE_SIMULATOR
        // ── La escena sola con la fase forzada (CAP-01, -02, -19, -20, -21) ──
        private static void AddPoses(List<SnapshotScenario> all)
        {
            all.Add(Alone("CAP-01", "CAP-01-drive-tv", DeviceProfile.TV, 12, 0.18, "Drive: 12:00, c = 0,18", RowerCharacterKind.Mannequin,
                (h, v) => Expect(("centro de pala −0,13 (± 0,005)", () => Near(h.Scene!.Frame.Rower.Pose.Left.CurveBladeY, -0.13, 0.005)),
                    ("pala vertical (plumeo 0)", () => Near(h.Scene!.Frame.Rower.Pose.Left.FeatherDeg, 0, 1e-6)))));
            all.Add(Alone("CAP-02", "CAP-02-recuperacion-tv", DeviceProfile.TV, 12, 0.60, "Recuperación: 12:00, c = 0,60", RowerCharacterKind.Mannequin,
                (h, v) => Expect(("plumeo 90°", () => Near(h.Scene!.Frame.Rower.Pose.Left.FeatherDeg, 90, 0.5)),
                    ("centro de pala +0,09 (± 0,005)", () => Near(h.Scene!.Frame.Rower.Pose.Left.CurveBladeY, 0.09, 0.005)))));
            all.Add(Alone("CAP-19a", "CAP-19a-catch-v390", DeviceProfile.V390, 12, 0, "Composición del legacy: V390, 12:00, c = 0 (contra LEG-01)",
                RowerCharacterKind.Mannequin, (h, v) => HeadOnScreen(h, v)));
            all.Add(Alone("CAP-19b", "CAP-19b-finish-v390", DeviceProfile.V390, 12, 0.36, "Composición del legacy: V390, 12:00, c = 0,36 (contra LEG-02)",
                RowerCharacterKind.Mannequin, (h, v) => HeadOnScreen(h, v)));
            all.Add(Alone("CAP-20", "CAP-20-maniqui-tv", DeviceProfile.TV, 12, 0.24, "El maniquí: 12:00, c = 0,24", RowerCharacterKind.Mannequin,
                (h, v) => Expect(("el maniquí", () => !h.Scene!.World!.Rower!.FromSlot))));
            all.Add(Alone("CAP-21", "CAP-21-ybot-tv", DeviceProfile.TV, 12, 0.24, "Y Bot: 12:00, c = 0,24", RowerCharacterKind.Slot,
                (h, v) => Expect(("Y Bot (NO VERIFICABLE si falta el FBX)", () => h.Scene!.World!.Rower!.FromSlot))));
        }

        // ── Los eventos de la palada con R-4 (CAP-03, -04) ──
        private static void AddEvents(List<SnapshotScenario> all)
        {
            var cap03 = Scene("CAP-03", "CAP-03-ataque-tv", DeviceProfile.TV, "Ataque: R-4, 12:00, 0,1 s después de la primera entrada de pala desde los 30 s",
                s => Steps(Free(s, 30), UntilEvent(s, StrokeEvents.BladeEntry), StepFor(s, 0.1), Act(s, h => LiveAlone(h, 12))));
            cap03.MotionMayRun = true;
            cap03.Inspect = (h, frame, values) =>
            {
                var spray = h.Scene!.Frame.Rower.Spray!;
                values.Add(RowerMarks.Values(h, Since(h)));
                values.Add("gotas vivas: " + spray.CountOf(0) + " (corona " + spray.CountOf(1) + ", trazos " + spray.CountOf(2) + ")");
                return Expect(("≥ 6 gotas por pala (≥ 12)", () => spray.CountOf(0) >= 12), ("la corona de las dos palas", () => spray.CountOf(1) >= 2));
            };
            all.Add(cap03);

            // It. 6b (QA-V6b-03; AC-6B-17): la toma en la salida de pala (antes, 1 s después): el par de charcos de la palada anterior queda a
            // ≈ 4,9 m de la cámara y a la vista en TV (1 s después estaba a 1,1 m, debajo del cuadro, y la sonda no lo encontraba).
            var cap04 = Scene("CAP-04", "CAP-04-charcos-tv", DeviceProfile.TV, "Charcos: R-4, 12:00, en la primera salida de pala desde los 59,2 s",
                s => Steps(Free(s, 59.2), UntilEvent(s, StrokeEvents.BladeExit), Act(s, h => LiveAlone(h, 12))));
            cap04.MotionMayRun = true;
            // It. 6b (AC-6B-17; DT6b-24): la CAP-04 con el agua avanzada y el charco proyectado (lo demás de it7, con la degradada fija).
            cap04.Water = RowMate.Core.Scene.Water.WaterForce.Auto;
            cap04.Inspect = (h, frame, values) =>
            {
                var f = h.Scene!.Frame;
                var puddles = f.Rower.Puddles!;
                values.Add(RowerMarks.Values(h, Since(h)));
                for (var i = 0; i < puddles.Count; i++)
                {
                    values.Add("charco " + i + ": x " + F(puddles[i].X) + ", relZ " + F(puddles[i].WorldZ - f.Distance) + ", edad " + F(puddles[i].Age) + ", alfa " + F(puddles[i].Alpha));
                }

                return Expect(("un par de charcos a la vista", () => h.Scene!.World!.Director!.WaterMarks!.Puddles >= 2));
            };
            // QA-V6b-03 (AC-6B-17): el charco proyectado a la pantalla, su contraste y el ángulo del par de remolinos.
            cap04.SceneProbes = WaterSpecProbes.PuddlesOnly();
            all.Add(cap04);
        }

        // ── La sombra por hora y nivel (CAP-05 a -09) ──
        private static void AddShadows(List<SnapshotScenario> all)
        {
            foreach (var (id, hour) in new[] { ("CAP-05", 9), ("CAP-06", 12), ("CAP-07", 17), ("CAP-08", 22) })
            {
                var h0 = hour;
                all.Add(Alone(id, id + "-sombra-" + hour.ToString("00", CultureInfo.InvariantCulture) + "-th", DeviceProfile.TH, hour, 0.5,
                    "Sombra a las " + hour.ToString("00", CultureInfo.InvariantCulture) + ":00, c = 0,5", RowerCharacterKind.Mannequin, (h, v) => ShadowProbe(h, v, h0, false)));
            }

            var low = Alone("CAP-09", "CAP-09-ovalo-bajo-th", DeviceProfile.TH, 9, 0.5, "Nivel Bajo, 09:00: el óvalo del legacy", RowerCharacterKind.Mannequin,
                (h, v) => ShadowProbe(h, v, 9, true));
            low.Host.SceneTier = QualityTier.Low;
            low.Host.SceneUrpTier = QualityTier.Low;
            all.Add(low);
        }

        // ── El HUD sobre el remero (CAP-10 a -14) ──
        private static void AddHud(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, profile) in new[] { ("CAP-10", "CAP-10-hud-tv", DeviceProfile.TV), ("CAP-11", "CAP-11-hud-th", DeviceProfile.TH) })
            {
                var scenario = Scene(id, stem, profile, "Libre (R-4) a los 60,2 s, con el contorno de la zona", s => Free(s, 60.2), outline: true);
                scenario.Inspect = (h, frame, values) => ZoneProbe(h, values);
                all.Add(scenario);
            }

            foreach (var (id, c) in new[] { ("CAP-12", 0.0), ("CAP-13", 0.36) })
            {
                var phase = c;
                var scenario = Scene(id, id + "-series-c" + (c == 0 ? "0" : "036") + "-tv", DeviceProfile.TV,
                    "Series 600 a los 250,2 s, los dos paneles abiertos, c = " + F(c) + ", con el contorno",
                    s => Steps(Routine(s, "Series 600", 250.2), Act(s, h => h.Scene!.ForceCanonicalForTest(phase))), seed: RoutineSeeds.F5, outline: true, panels: true);
                scenario.Inspect = (h, frame, values) => PanelsProbe(h, values);
                all.Add(scenario);
            }

            var cap14 = Scene("CAP-14", "CAP-14-llave-l640", DeviceProfile.L640, "Libre (simulador) a los 60,2 s con la llave, con el contorno",
                s => Free(s, 60.2), simulator: true, outline: true);
            cap14.Inspect = (h, frame, values) =>
            {
                var body = RowerMarks.Body(h);
                var wrench = h.BoundsOf("sim-open");
                values.Add(RowerMarks.Values(h, Since(h)));
                values.Add("cuerpo " + Describe(body) + "; llave " + Describe(wrench));
                RowerMarks.Outline(h, "snapshot-body", body, Tokens.DevSimulator);
                return Expect(("la llave no tapa el cuerpo (0 px)", () => !wrench.Overlaps(body)));
            };
            all.Add(cap14);
        }

        // ── La boya de 500 m (CAP-15, -16) ──
        private static void AddBuoy(List<SnapshotScenario> all)
        {
            // A los 150,2 y 155,2 s (k + 0,2: la regla de toma de la 4b; el reloj del HUD cambia en el segundo entero).
            foreach (var (id, at, laps) in new[] { ("CAP-15", 150.2, 0), ("CAP-16", 155.2, 1) })
            {
                var expected = laps;
                var scenario = Scene(id, id + "-boya-" + F(at) + "-tv", DeviceProfile.TV, "Series 600 con R-4 a los " + F(at) + " s",
                    s => Routine(s, "Series 600", at, true), seed: RoutineSeeds.F5);
                scenario.Inspect = (h, frame, values) =>
                {
                    var f = h.Scene!.Frame;
                    var buoy = f.Rower.Buoy!;
                    var lapCount = SceneInputs.From(h.App.Workout.State.Value).Buoy.LapCount;
                    values.Add(RowerMarks.Values(h, Since(h)));
                    values.Add("boya: visible " + buoy.Visible + ", relZ " + F(buoy.WorldZ - f.Distance) + ", fija " + buoy.Fixed + "; vueltas cerradas " + lapCount);
                    return Expect(("la boya a la vista", () => buoy.Visible), ("vueltas cerradas " + expected, () => lapCount == expected));
                };
                all.Add(scenario);
            }
        }

        // ── La hoja de poses con la cámara lateral (CAP-17, -18): las 10 poses sueltas y la hoja de 5 × 2 ──
        public static readonly double[] TablePhases = { 0, 0.09, 0.18, 0.24, 0.36, 0.395, 0.6, 0.75, 0.85, 0.96 };

        private static SnapshotScenario SideSheet(string id, string stem, RowerCharacterKind kind, string who)
        {
            var shots = new List<Texture2D>();
            var scenario = Scene(id, stem, DeviceProfile.TH, "Cámara lateral, regla de 1 m, " + who, s => SideSteps(s, id, stem, kind, shots), character: kind);
            scenario.Kind = SnapshotKind.Sequence;
            for (var i = 0; i < TablePhases.Length; i++)
            {
                scenario.Shot(id + "-" + (i + 1).ToString("00", CultureInfo.InvariantCulture), stem + "-" + (i + 1).ToString("00", CultureInfo.InvariantCulture),
                    who + ", c = " + F(TablePhases[i]) + " (ANIMATION_BIBLE §3.4)");
            }

            scenario.Shot(id, stem, "Hoja de 5 × 2 con las 10 poses (" + who + ")");
            scenario.ShotInspect = (h, frame, values) =>
            {
                var problems = RowerMarks.RigProbe(h, values);
                if (h.Root.Q(RowerMarks.SheetName) == null && shots.Count < TablePhases.Length)
                {
                    var copy = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                    copy.SetPixels32(frame.GetPixels32());
                    copy.Apply(false, false);
                    shots.Add(copy);
                }

                if (kind == RowerCharacterKind.Slot && !h.Scene!.World!.Rower!.FromSlot)
                {
                    problems.Add("NO VERIFICABLE: falta YBot.fbx (el remero es el maniquí)");
                }

                return problems;
            };
            return scenario;
        }

        private static IEnumerator SideSteps(ScenarioScript s, string id, string stem, RowerCharacterKind kind, List<Texture2D> shots)
        {
            yield return Free(s, 2.2);
            yield return s.Do(h =>
            {
                var scene = h.Scene!;
                scene.ForceHourForTest(12);
                scene.ForceDistanceForTest(SoloDistance);
                scene.FreezeSceneTimeForTest(FrozenSceneTime);
                scene.SetScrimsForTest(false);
                scene.SideCameraForTest(true);
                scene.ShowRulerForTest = true;
                h.Immersive?.View.HideForCapture();
            });
            for (var i = 0; i < TablePhases.Length; i++)
            {
                var c = TablePhases[i];
                yield return s.Do(h =>
                {
                    h.Scene!.ForceCanonicalForTest(c);
                    h.SceneTick();
                    h.SceneTick();
                    RowerMarks.Ruler(h);
                });
                yield return s.Frames(3);
                yield return s.Shot(id + "-" + (i + 1).ToString("00", CultureInfo.InvariantCulture));
            }

            Texture2D? sheet = null;
            yield return s.Do(h => sheet = RowerMarks.Sheet(h, shots, 5, 2));
            yield return s.Frames(3);
            yield return s.Shot(id);
            yield return s.Do(h =>
            {
                h.Root.Q(RowerMarks.SheetName)?.RemoveFromHierarchy();
                UnityEngine.Object.DestroyImmediate(sheet);
                foreach (var shot in shots)
                {
                    UnityEngine.Object.DestroyImmediate(shot);
                }

                shots.Clear();
            });
        }

        // ── Los videos (VID-01 a -07; 30 cuadros por segundo; los valores del remero por cuadro en el manifest) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            foreach (var (id, stem, preset, spm) in new[] { ("VID-01", "VID-01-suave-tv", "sim-preset-suave", 20.0), ("VID-02", "VID-02-fuerte-tv", "sim-preset-fuerte", 30.0) })
            {
                var target = spm;
                var name = preset;
                var vid = Scene(id, stem, DeviceProfile.TV, "Libre con " + (spm < 25 ? "Suave (20 spm)" : "Fuerte (30 spm)") + " en régimen, de los 20 a los 32 s",
                    s => Steps(s.StartFree(), s.AdvanceTo(3), s.Tap("sim-open"), s.TapInView(name), s.Tap("sim-close"), s.AdvanceTo(20)), simulator: true);
                var period = new PeriodTracker(60 / target);
                Video(vid, 12).FrameCheck = (h, t) => period.Check(h, t);
                all.Add(vid);
            }

            var vid03 = Scene("VID-03", "VID-03-suave-a-fuerte-tv", DeviceProfile.TV, "Suave → Fuerte a los 3 s del video (8 s)",
                s => Steps(s.StartFree(), s.AdvanceTo(3), s.Tap("sim-open"), s.TapInView("sim-preset-suave"), s.Tap("sim-close"), s.AdvanceTo(20)), simulator: true);
            var ratio = new RatioTracker();
            Video(vid03, 8).At(3, h =>
            {
                h.Simulator!.Simulator.TargetSpm = 30;
                h.Simulator.Simulator.TargetWatts = 280;
            }).FrameCheck = (h, t) => ratio.Check(h);
            all.Add(vid03);

            var vid04 = Scene("VID-04", "VID-04-pausa-th", DeviceProfile.TH, "R-4: Pausa a los 3 s y Reanudar a los 8 s (12 s)", s => Free(s, 20));
            Video(vid04, 12).At(3, h => h.Tap("hud-pause")).At(8, h => h.Tap("hud-resume")).FrameCheck = (h, t) =>
                t > 3.6 && t < 8 && h.Scene!.Frame.Rower.PhaseSpeed >= 0.01 ? "el remero no frenó a los 0,6 s de la pausa (ω " + F(h.Scene.Frame.Rower.PhaseSpeed) + ")"
                : t > 6.0 && t < 8 && h.Scene!.Frame.Rower.IdleWeight < 0.5 ? "no respira a los 3 s de la pausa (peso " + F(h.Scene.Frame.Rower.IdleWeight) + ")"
                : null;
            all.Add(vid04);

            var vid05 = Scene("VID-05", "VID-05-spm-cero-tv", DeviceProfile.TV, "Simulador con la llave: Remando apagado (SPM 0) a los 2 s (10 s)", s => Free(s, 20),
                simulator: true);
            Video(vid05, 10).At(2, h => h.Simulator!.Simulator.Rowing = false).FrameCheck = (h, t) =>
                t > 9.5 && !Near(h.Scene!.Frame.Rower.Canonical, 0, 1e-9) ? "no quedó en el catch (c " + F(h.Scene.Frame.Rower.Canonical) + ")" : null;
            all.Add(vid05);

            var vid06 = Scene("VID-06", "VID-06-lateral-th", DeviceProfile.TH, "R-4 con la cámara lateral (6 s)",
                s => Steps(Free(s, 20), Act(s, h =>
                {
                    h.Scene!.SetScrimsForTest(false);
                    h.Scene.SideCameraForTest(true);
                    h.Immersive?.View.HideForCapture();
                })));
            Video(vid06, 6);
            all.Add(vid06);

            var vid07 = Scene("VID-07", "VID-07-boya-tv", DeviceProfile.TV, "Series 600 con R-4, de los 145 a los 160 s (15 s)", s => Routine(s, "Series 600", 145, true),
                seed: RoutineSeeds.F5);
            var pass = new BuoyTracker7();
            Video(vid07, 15).FrameCheck = (h, t) => pass.Check(h, 145 + t);
            all.Add(vid07);
        }

        // ── La demo (DEMO-01 a -06, una secuencia; spec §1) ──
        private static SnapshotScenario Demo()
        {
            var demo = Scene("DEMO", "DEMO", DeviceProfile.W1280, "La demo de la It. 7", DemoSteps, seed: RoutineSeeds.F5, simulator: true);
            demo.Kind = SnapshotKind.Sequence;
            foreach (var (id, stem, state) in new[]
                     {
                         ("DEMO-01", "DEMO-01-suave", "Libre → llave → Suave; la toma a los 20,2 s"),
                         ("DEMO-02", "DEMO-02-fuerte", "Fuerte; la toma a los 40,2 s"),
                         ("DEMO-03", "DEMO-03-pausa", "Pausa; la toma 6 s después (el remero respira)"),
                         ("DEMO-04", "DEMO-04-sombra-17", "Reanudar; Hora escena 09:00 y 17:00; la toma 1,6 s después de las 17:00"),
                         ("DEMO-05", "DEMO-05-boya", "Series 600: la boya pasa cuando el panel cierra #1"),
                         ("DEMO-06", "DEMO-06-ybot", "Con YBot.fbx: el remero es Y Bot (NO VERIFICABLE sin el FBX)"),
                     })
            {
                demo.Shot(id, stem, state);
            }

            demo.ShotInspect = (h, frame, values) =>
            {
                values.Add(RowerMarks.Values(h, Since(h)));
                return new List<string>();
            };
            return demo;
        }

        private static IEnumerator DemoSteps(ScenarioScript s)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(3);
            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-preset-suave");
            yield return s.Tap("sim-close");
            yield return s.AdvanceTo(20.2);
            yield return s.Shot("DEMO-01", h => Expect(("20 spm", () => Near(h.App.Workout.State.Value.Data.StrokeRate, 20, 0.5)),
                ("charcos a la vista", () => h.Scene!.World!.Director!.WaterMarks!.Puddles >= 1)));

            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-preset-fuerte");
            yield return s.Tap("sim-close");
            yield return s.AdvanceTo(40.2);
            yield return s.Shot("DEMO-02", h => Expect(("30 spm", () => Near(h.App.Workout.State.Value.Data.StrokeRate, 30, 0.5))));

            yield return s.Tap("hud-pause");
            yield return s.Advance(6);
            yield return s.Shot("DEMO-03", h => Expect(("el remero quieto", () => h.Scene!.Frame.Rower.PhaseSpeed < 0.01),
                ("respira", () => h.Scene!.Frame.Rower.IdleWeight > 0.5)));

            yield return s.Tap("hud-resume");
            yield return s.Do(h => h.App.Scene.HourOverride.Value = 9);
            yield return s.Advance(2);
            yield return s.Do(h => h.App.Scene.HourOverride.Value = 17);
            yield return s.Advance(1.6);
            yield return s.Shot("DEMO-04", h => Expect(("hora visible 17", () => Near(h.Scene!.Frame.VisibleHour, 17, 1e-6))));

            yield return s.Do(h => h.App.Scene.HourOverride.Value = null);
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Advance(2);
            yield return s.StartRoutine("Series 600");
            yield return FastStepTo(s, 60);
            yield return UntilLap(s);
            yield return AtNextTick(s);
            yield return s.Shot("DEMO-05", h => Expect(("la boya a la vista", () => h.Scene!.Frame.Rower.Buoy!.Visible),
                ("el panel cerró #1", () => SceneInputs.From(h.App.Workout.State.Value).Buoy.LapCount >= 1)));

            yield return s.Do(h => h.Scene!.ForceCharacterForTest(RowerCharacterKind.Slot));
            yield return s.Advance(1);
            yield return s.Shot("DEMO-06", h => Expect(("Y Bot (NO VERIFICABLE si falta el FBX)", () => h.Scene!.World!.Rower!.FromSlot)));
        }

        // Cuadro a cuadro hasta que el panel cierra la vuelta (tope 240 s).
        private static IEnumerator UntilLap(ScenarioScript s)
        {
            for (var i = 0; i < 240 * 60; i++)
            {
                if (SceneInputs.From(s.Host.App.Workout.State.Value).Buoy.LapCount >= 1)
                {
                    break;
                }

                s.Host.Step();
                if (i % 30 == 0)
                {
                    yield return null;
                }
            }

            yield return s.Frames(2);
        }

        // ── Armado ──

        private static SnapshotScenario Scene(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange,
                                              RoutineSeed? seed = null, bool simulator = false, bool outline = false, bool panels = false,
                                              RowerCharacterKind character = RowerCharacterKind.Mannequin)
        {
            var preferences = new MemoryPreferences();
            if (panels)
            {
                preferences.SetBool(SeriesPanelPrefs.LapsKey, true);
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
                SceneCharacter = character,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = simulator ? "Sim" : "R-4", ExactInstant = true };
        }

        // Una toma de la escena sola con la fase forzada.
        private static SnapshotScenario Alone(string id, string stem, DeviceProfile profile, double hour, double canonical, string state, RowerCharacterKind character,
                                              Func<UiTestHost, List<string>, IEnumerable<string>> probe)
        {
            var scenario = Scene(id, stem, profile, state, s => AloneArrange(s, hour, canonical), character: character);
            scenario.Inspect = (h, frame, values) =>
            {
                values.Add(RowerMarks.Values(h, Since(h)));
                var problems = new List<string>(RowerMarks.RigProbe(h, values));
                problems.AddRange(probe(h, values));
                return problems;
            };
            return scenario;
        }

        private static IEnumerator AloneArrange(ScenarioScript s, double hour, double canonical)
        {
            yield return Free(s, 2.2);
            yield return s.Frames(1);
            var h = s.Host;
            var scene = h.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");
            scene.ForceDistanceForTest(SoloDistance);
            scene.ForceHourForTest(hour);
            scene.FreezeSceneTimeForTest(FrozenSceneTime);
            scene.BreathingOffForTest(true);
            scene.SetScrimsForTest(false);
            scene.ForceCanonicalForTest(canonical);
            h.Immersive?.View.HideForCapture();
            h.SceneTick();
            h.SceneTick();
            yield return s.Frames(3);
        }

        // La escena sola sin congelar el tiempo (las tomas de eventos: la salpicadura y los charcos siguen vivos).
        private static void LiveAlone(UiTestHost h, double hour)
        {
            var scene = h.Scene!;
            scene.ForceHourForTest(hour);
            scene.BreathingOffForTest(true);
            scene.SetScrimsForTest(false);
            h.Immersive?.View.HideForCapture();
            h.SceneTick();
        }

        // Cuadro a cuadro hasta el evento (tope 10 s).
        private static IEnumerator UntilEvent(ScenarioScript s, StrokeEvents ev)
        {
            for (var i = 0; i < 600; i++)
            {
                s.Host.Step();
                yield return null;
                if ((s.Host.Scene!.Frame.Rower.Events & ev) != 0)
                {
                    break;
                }
            }
        }

        private static IEnumerator StepFor(ScenarioScript s, double seconds)
        {
            var frames = (int)Math.Round(seconds * 60);
            for (var i = 0; i < frames; i++)
            {
                s.Host.Step();
                yield return null;
            }
        }

        private static IEnumerator Free(ScenarioScript s, double at)
        {
            yield return s.StartFree();
            yield return s.AdvanceTo(at);
        }

        // stepped: cuadro a cuadro (la boya sigue las fotos del motor: con el salto de AdvanceTo la escena integraba todo el tramo con la
        // foto del final, QA-V6a-04).
        private static IEnumerator Routine(ScenarioScript s, string name, double at, bool stepped = false)
        {
            s.ReadingZeroAtStart = true;
            yield return s.Tap("tab-workout");
            yield return s.StartRoutine(name);
            yield return stepped ? FastStepTo(s, at) : s.AdvanceTo(at);
        }

        // Hasta `at` segundos de la sesión de a 1/60 s (la escena ve cada foto del motor), con un cuadro real cada 60 pasos.
        private static IEnumerator FastStepTo(ScenarioScript s, double at)
        {
            var target = s.SessionStart + at;
            var n = 0;
            while (target - s.Host.Scheduler.MonotonicSeconds > UiTestHost.FrameSeconds + 1e-9)
            {
                s.Host.Step();
                if (++n % 60 == 0)
                {
                    yield return null;
                }
            }

            var rest = target - s.Host.Scheduler.MonotonicSeconds;
            if (rest > 1e-9)
            {
                s.Host.Advance(rest);
            }

            yield return s.Frames(2);
        }

        // El k + 0,2 siguiente de la sesión (la regla de toma de la 4b), cuadro a cuadro.
        private static IEnumerator AtNextTick(ScenarioScript s)
        {
            var elapsed = s.Host.Scheduler.MonotonicSeconds - s.SessionStart;
            yield return FastStepTo(s, Math.Ceiling(elapsed - 0.2 + 1e-9) + 0.2);
        }

        private static IEnumerator Act(ScenarioScript s, Action<UiTestHost> action)
        {
            yield return s.Frames(1);
            action(s.Host);
            yield return s.Frames(2);
        }

        private static VideoSpec Video(SnapshotScenario scenario, double seconds)
        {
            scenario.Kind = SnapshotKind.Video;
            scenario.Video = new VideoSpec(seconds) { FrameValues = (h, t) => RowerMarks.Values(h, t) };
            return scenario.Video;
        }

        // ── Sondas ──

        private static IEnumerable<string> ShadowProbe(UiTestHost h, List<string> values, int hour, bool low)
        {
            var f = h.Scene!.Frame;
            var r = f.Rower;
            var oval = h.Scene.World!.Boat!.Shadow.enabled;
            values.Add("sombra " + r.ShadowMode + "; óvalo " + (oval ? "a la vista" : "apagado") + " lado " + F(r.ShadowSide) + " alfa " + F(r.ShadowAlpha)
                       + "; astro x " + F(f.LegacyLight.X) + " (centro " + F(f.Window.CenterX) + "); fuerza de la luz " + F(h.Scene.World.Lighting!.ShadowStrength));
            return Expect(("el óvalo solo sin mapa", () => oval == (r.ShadowMode == BoatShadowMode.Oval)),
                ("Bajo: el óvalo", () => !low || r.ShadowMode == BoatShadowMode.Oval),
                ("el óvalo del lado opuesto al astro", () => r.ShadowMode != BoatShadowMode.Oval
                                                            || Near(r.ShadowSide, BoatShadowRule.LegacySide(f.LegacyLight.X, f.Window.CenterX), 1e-6)),
                ("hora " + hour, () => Near(f.VisibleHour, hour, 1e-6)));
        }

        private static IEnumerable<string> ZoneProbe(UiTestHost h, List<string> values)
        {
            values.Add(RowerMarks.Values(h, Since(h)));
            var zone = h.BoundsOf("hud-boat-zone");
            var boat = RowerMarks.BoatToPins(h);
            var body = RowerMarks.Body(h);
            values.Add("zona " + Describe(zone) + "; bote hasta los pernos " + Describe(boat) + "; cuerpo " + Describe(body));
            RowerMarks.Outline(h, "snapshot-body", body, Tokens.DevSimulator);
            return Expect(("el cuerpo dentro de la zona", () => Inside(body, zone)), ("el bote hasta los pernos dentro de la zona", () => Inside(boat, zone, 2)));
        }

        private static IEnumerable<string> PanelsProbe(UiTestHost h, List<string> values)
        {
            values.Add(RowerMarks.Values(h, Since(h)));
            var body = RowerMarks.Body(h);
            var laps = h.BoundsOf("hud-laps-panel");
            var reps = h.BoundsOf("hud-reps-panel");
            values.Add("cuerpo " + Describe(body) + "; vueltas " + Describe(laps) + "; series anteriores " + Describe(reps));
            RowerMarks.Outline(h, "snapshot-body", body, Tokens.DevSimulator);
            return Expect(("los dos paneles a la vista", () => h.IsShown("hud-laps-panel") && h.IsShown("hud-reps-panel")),
                ("Series anteriores no tapa el cuerpo (0 px)", () => !reps.Overlaps(body)), ("Vueltas no tapa el cuerpo (0 px)", () => !laps.Overlaps(body)));
        }

        // CAP-19: dónde queda la cabeza del remero en la pantalla (contra el legacy: LEG-01/02 del oráculo).
        private static IEnumerable<string> HeadOnScreen(UiTestHost h, List<string> values)
        {
            var world = h.Scene!.World!;
            var head = world.Rower!.Bones[RowerJoint.Head]!.position;
            var v = world.Rig!.Camera.WorldToViewportPoint(head);
            values.Add("cabeza del remero en (" + F(v.x * h.Profile.WidthDp) + "; " + F((1 - v.y) * h.Profile.HeightDp) + ") dp; punto del remero del contrato y "
                       + F(h.Scene.Frame.Window.RowerY));
            return new List<string>();
        }

        private static bool Inside(Rect inner, Rect outer, float slack = 0.5f)
        {
            return inner.xMin >= outer.xMin - slack && inner.xMax <= outer.xMax + slack && inner.yMin >= outer.yMin - slack && inner.yMax <= outer.yMax + slack;
        }

        // VID-01/02: el período entre catches (c que vuelve a 0) contra 60/spm ± 0,05 s, después del primero completo.
        private sealed class PeriodTracker
        {
            private readonly double _expected;
            private double _lastC = double.NaN;
            private double _lastCatch = double.NaN;

            public PeriodTracker(double expected)
            {
                _expected = expected;
            }

            public string? Check(UiTestHost h, double t)
            {
                var c = h.Scene!.Frame.Rower.Canonical;
                string? problem = null;
                if (!double.IsNaN(_lastC) && c < _lastC - 0.5)
                {
                    if (!double.IsNaN(_lastCatch) && Math.Abs(t - _lastCatch - _expected) > 0.05 + 1.0 / 30)
                    {
                        problem = "período de palada " + F(t - _lastCatch) + " s (se esperaba " + F(_expected) + " ± 0,05)";
                    }

                    _lastCatch = t;
                }

                _lastC = c;
                return problem;
            }
        }

        // VID-03: r cambia solo en un catch.
        private sealed class RatioTracker
        {
            private double _lastC = double.NaN;
            private double _lastR = double.NaN;

            public string? Check(UiTestHost h)
            {
                var r = h.Scene!.Frame.Rower;
                string? problem = null;
                if (!double.IsNaN(_lastR) && Math.Abs(r.Ratio - _lastR) > 1e-12 && !(r.Canonical < _lastC - 0.5))
                {
                    problem = "r cambió a mitad de palada (" + F(_lastR) + " → " + F(r.Ratio) + ", c " + F(r.Canonical) + ")";
                }

                _lastR = r.Ratio;
                _lastC = r.Canonical;
                return problem;
            }
        }

        // VID-07: el instante en que relZ de la boya cruza 9,5 (el remero) contra el cierre de #1 (155 ± 1 s).
        private sealed class BuoyTracker7
        {
            private double _lastRel = double.NaN;

            public string? Check(UiTestHost h, double t)
            {
                var f = h.Scene!.Frame;
                var buoy = f.Rower.Buoy!;
                if (!buoy.Visible)
                {
                    _lastRel = double.NaN;
                    return null;
                }

                var rel = buoy.WorldZ - f.Distance;
                string? problem = null;
                if (!double.IsNaN(_lastRel) && _lastRel > SceneCamera.BoatZ && rel <= SceneCamera.BoatZ && Math.Abs(t - 155) > 1 + 1.0 / 30)
                {
                    problem = "la boya pasa al remero a los " + F(t) + " s (155 ± 1)";
                }

                _lastRel = rel;
                return problem;
            }
        }
#endif

        private static string Describe(Rect r)
        {
            return "[" + F(r.xMin) + "; " + F(r.yMin) + " – " + F(r.xMax) + "; " + F(r.yMax) + "]";
        }

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
