using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Workout;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SLC · El ciclo de vida de la escena (06a-escena-lago/02-tech-design §3.1, §3.4, §3.9, §6.3; LT-78 a LT-81, LT-83 a LT-87;
    // AC-SCN-01-03 a -07, AC-SCN-11-01, AC-SCN-11-07, AC-SCN-13-01, -04, AC-SCN-21-01, AC-WKT-02-08).
    [Category("GPU")]
    public sealed class SceneLifecycleTests : SceneTestBase
    {
        // LT-78 a LT-81 y LT-84 a LT-87: los nombres de las pruebas del legacy (test/scene/sky_shore_painter_test.dart:16 y
        // test/scene/scene_view_test.dart:11), a 390 × 844 y 844 × 390, a las 12 y a las 23 h.
        public static IEnumerable<TestCaseData> Paints()
        {
            yield return new TestCaseData(390, 844, 12).Returns(null).SetName("EnvironmentId.lake 390x844 12.0h pinta sin excepciones")
                .SetCategory("LT-78").SetCategory("AC-SCN-11-07").SetCategory("AC-SCN-21-01");
            yield return new TestCaseData(390, 844, 23).Returns(null).SetName("EnvironmentId.lake 390x844 23.0h pinta sin excepciones")
                .SetCategory("LT-79").SetCategory("AC-SCN-11-07");
            yield return new TestCaseData(844, 390, 12).Returns(null).SetName("EnvironmentId.lake 844x390 12.0h pinta sin excepciones")
                .SetCategory("LT-80").SetCategory("AC-SCN-11-07");
            yield return new TestCaseData(844, 390, 23).Returns(null).SetName("EnvironmentId.lake 844x390 23.0h pinta sin excepciones")
                .SetCategory("LT-81").SetCategory("AC-SCN-11-07");
        }

        public static IEnumerable<TestCaseData> Animates()
        {
            yield return new TestCaseData(390, 844, 12).Returns(null).SetName("EnvironmentId.lake 390x844 12.0h anima sin excepciones")
                .SetCategory("LT-84").SetCategory("AC-SCN-01-03");
            yield return new TestCaseData(390, 844, 23).Returns(null).SetName("EnvironmentId.lake 390x844 23.0h anima sin excepciones")
                .SetCategory("LT-85");
            yield return new TestCaseData(844, 390, 12).Returns(null).SetName("EnvironmentId.lake 844x390 12.0h anima sin excepciones")
                .SetCategory("LT-86");
            yield return new TestCaseData(844, 390, 23).Returns(null).SetName("EnvironmentId.lake 844x390 23.0h anima sin excepciones")
                .SetCategory("LT-87");
        }

        public static IEnumerable<TestCaseData> FreshWater()
        {
            yield return new TestCaseData().Returns(null).SetName("sin shader pinta el fallback sin excepciones")
                .SetCategory("LT-83").SetCategory("AC-SCN-13-01").SetCategory("AC-SCN-13-04");
        }

        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.lake 390x844 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.lake 390x844 23.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.lake 844x390 12.0h pinta sin excepciones'
        // legacy: test/scene/sky_shore_painter_test.dart:16 'EnvironmentId.lake 844x390 23.0h pinta sin excepciones'
        // La escena del lago a 0 y a 1 234 m, con el tiempo de escena 3,2, se dibuja sin excepciones ni errores en el registro y no queda
        // vacía (el cielo arriba).
        [TestCaseSource(nameof(Paints))]
        public IEnumerator LakePaintsWithoutExceptions(int width, int height, int hour)
        {
            AssumeGpu();
            CreateScene(width < height ? DeviceProfile.V390 : DeviceProfile.H844);
            yield return EnterFree();
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready));
            Assert.That(SceneHost.IsShowing, Is.True, "la escena se ve después del vuelo");
            foreach (var distance in new[] { 0.0, 1234.0 })
            {
                yield return SceneAlone(distance, hour);
                var frame = Read();
                Dump(frame, "lake-" + width + "-" + hour + "-" + distance);
                var top = At(frame, frame.width / 2, 4);
                Assert.That(top.r + top.g + top.b, Is.GreaterThan(0), "el cielo arriba no es negro");
                Destroy(frame);
            }

            AssertClean(H);
            Assert.That(SceneHost.Draws, Is.GreaterThan(0));
            Assert.That(SceneHost.WarmupDraws, Is.EqualTo(1), "el único dibujo de calentamiento (AC-SCN-01-06)");
        }

        // legacy: test/scene/water_painter_test.dart:11 'sin shader pinta el fallback sin excepciones'
        // El agua degradada se dibuja en una escena recién armada (sin agua avanzada en la 6a); AC-SCN-13-01, -04.
        [TestCaseSource(nameof(FreshWater))]
        public IEnumerator DegradedWaterPaintsOnAFreshScene()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.V390);
            yield return EnterFree();
            yield return SceneAlone(0, 12);
            var frame = Read();
            // Debajo del horizonte (0,40 del alto) y sobre la zona del bote, el agua: azul (B > R).
            var water = At(frame, frame.width / 2, (int)(frame.height * 0.62));
            Assert.That(water.b, Is.GreaterThan(water.r), "el agua degradada a la vista: " + water);
            Destroy(frame);
            AssertClean(H);
        }

        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.lake 390x844 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.lake 390x844 23.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.lake 844x390 12.0h anima sin excepciones'
        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.lake 844x390 23.0h anima sin excepciones'
        // SPM 24, split 110, 300 m, en marcha, hora forzada; 6 cuadros de 100 ms. AC-SCN-01-03: la escena arranca de cero en cada inmersivo.
        [TestCaseSource(nameof(Animates))]
        public IEnumerator LakeAnimatesWithoutExceptions(int width, int height, int hour)
        {
            AssumeGpu();
            CreateScene(width < height ? DeviceProfile.V390 : DeviceProfile.H844);
            H.App.Scene.HourOverride.Value = hour;
            yield return EnterFree();
            SceneHost.ForceDistanceForTest(300);
            var before = SceneHost.Draws;
            for (var i = 0; i < 6; i++)
            {
                H.Advance(0.1);
                yield return null;
            }

            Assert.That(SceneHost.Draws - before, Is.GreaterThanOrEqualTo(6), "un dibujo por cuadro");
            Assert.That(SceneHost.Frame.Speed, Is.GreaterThan(0), "remando, el bote avanza");
            AssertClean(H);
        }

        // AC-SCN-01-06 (DEMO-01): fuera del inmersivo no se dibuja nada después de Ready; al salir, 120 cuadros sin dibujos de la escena.
        [UnityTest, Category("AC-SCN-01-06"), Category("AC-WKT-13-09")]
        public IEnumerator NothingDrawsOutsideTheImmersive()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return GoToWorkout();
            for (var i = 0; i < 30; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready), "la precarga en reposo");
            Assert.That(SceneHost.Draws, Is.EqualTo(0), "0 dibujos después de Ready fuera del inmersivo");
            Assert.That(SceneHost.WarmupDraws, Is.EqualTo(1));
            yield return EnterFree();
            Assert.That(SceneHost.Draws, Is.GreaterThan(0));
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            for (var i = 0; i < 60; i++)
            {
                H.Step();
                yield return null;
            }
            var draws = SceneHost.Draws;
            for (var i = 0; i < 120; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.Draws, Is.EqualTo(draws), "0 dibujos después de la salida");
            Assert.That(SceneHost.IsOpen, Is.False);
            AssertClean(H);
        }

        // AC-SCN-01-07 (CAP-24, VID-11): la escena demorada. El inmersivo entra con el fondo provisorio teñido por la hora; la escena se
        // prepara por tramos (ninguna unidad pasa de su tramo + 1 ms del Editor) y aparece en su lugar con su fundido de 0,6 s, al día
        // (la sesión corrió desde el toque); al terminar el fundido, el provisorio en display: none.
        [UnityTest, Category("AC-SCN-01-07"), Category("AC-SCN-01-01"), Category("AC-WKT-13-15")]
        public IEnumerator TheLateSceneFadesInUpToDate()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return GoToWorkout();
            H.SceneTick();
            SceneHost.DelayReadinessForTest(1.5);
            yield return StartFree();
            yield return AdvanceTo(0.6);
            var view = Hud;
            Assert.That(SceneHost.Readiness.Value, Is.Not.EqualTo(SceneReadiness.Ready), "a los 0,6 s todavía no");
            Assert.That(view.SceneShown, Is.False, "el fondo provisorio");
            Assert.That(H.IsShown("hud-backdrop"), Is.True);
            yield return AdvanceTo(1.6);
            Assert.That(SceneHost.Readiness.Value, Is.EqualTo(SceneReadiness.Ready));
            Assert.That(view.SceneShown, Is.True, "aparece");
            Assert.That(SceneHost.Session!.Camera.Mode, Is.EqualTo(CameraMode.Contract), "en su lugar, sin vuelo");
            Assert.That(SceneHost.Frame.SceneTime, Is.GreaterThan(1.4), "al día: corrió desde el toque");
            yield return AdvanceTo(2.4);
            Assert.That(H.IsShown("hud-backdrop"), Is.False, "el provisorio en display: none al terminar el fundido");
            AssertClean(H);
        }

        // QA-V6a-01 (AC-SCN-01-07, AC-APP-05-08; VID-11, D2-05): el fundido de 0,6 s de la escena demorada se VE: a mitad del fundido cada
        // píxel de la pantalla (la RT de la UI) queda entre el fondo provisorio y la escena, no igual al provisorio. Se compara una grilla
        // de puntos donde el provisorio y la escena difieren mucho: antes de Ready (solo el provisorio), a mitad del fundido y al final
        // (solo la escena). Antes de la corrección el provisorio, opaco y encima, tapaba la escena hasta el último cuadro.
        [UnityTest, Category("AC-SCN-01-07"), Category("AC-APP-05-08")]
        public IEnumerator TheLateSceneBlendsOverTheBackdrop()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return GoToWorkout();
            H.SceneTick();
            SceneHost.DelayReadinessForTest(1.5);
            yield return StartFree();
            yield return AdvanceTo(1.2);
            yield return Frames(3);
            Assert.That(Hud.SceneShown, Is.False, "a los 1,2 s, el provisorio solo");
            var before = Read();
            var surface = Hud.SceneSurface!;
            for (var i = 0; i < 180 && !(Hud.SceneShown && SurfaceOpacity(surface) >= 0.4); i++)
            {
                H.Step();
                yield return null;
            }

            var midOpacity = SurfaceOpacity(surface);
            Assert.That(Hud.SceneShown, Is.True, "la escena llegó");
            Assert.That(midOpacity, Is.InRange(0.4, 0.8), "a mitad del fundido");
            Assert.That(H.IsShown("hud-backdrop"), Is.True, "en el fundido se ven los dos");
            yield return Frames(3);
            var mid = Read();
            for (var i = 0; i < 90 && H.IsShown("hud-backdrop"); i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(H.IsShown("hud-backdrop"), Is.False, "al terminar, el provisorio en display: none");
            yield return Frames(3);
            var after = Read();
            var eligible = 0;
            var blended = 0;
            var stuck = 0;
            for (var gy = 1; gy < 20; gy++)
            {
                for (var gx = 1; gx < 12; gx++)
                {
                    var x = before.width * gx / 12;
                    var y = before.height * gy / 20;
                    var b = At(before, x, y);
                    var m = At(mid, x, y);
                    var a = At(after, x, y);
                    var total = Diff(a, b);
                    if (total < 60)
                    {
                        continue;
                    }

                    eligible++;
                    if (Diff(m, b) >= 0.2 * total && Diff(a, m) >= 0.2 * total)
                    {
                        blended++;
                    }
                    else if (Diff(m, b) <= 6)
                    {
                        stuck++;
                    }
                }
            }

            Dump(before, "late-fade-before");
            Dump(mid, "late-fade-mid");
            Dump(after, "late-fade-after");
            Destroy(before);
            Destroy(mid);
            Destroy(after);
            Debug.Log("[SLC] fundido de la escena demorada a " + midOpacity.ToString("0.00") + ": " + blended + " de " + eligible + " puntos mezclados, " + stuck
                      + " iguales al provisorio");
            Assert.That(eligible, Is.GreaterThanOrEqualTo(40), "puntos donde el provisorio y la escena difieren");
            Assert.That(blended, Is.GreaterThanOrEqualTo((int)(0.8 * eligible)), "a mitad del fundido, la mezcla (" + stuck + " iguales al provisorio)");
            AssertClean(H);
        }

        private static double SurfaceOpacity(UnityEngine.UIElements.VisualElement element)
        {
            if (element.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None)
            {
                return 0;
            }

            var style = element.style.opacity;
            return style.keyword == UnityEngine.UIElements.StyleKeyword.Undefined ? style.value : 1;
        }

        private static int Diff(Color32 a, Color32 b)
        {
            return Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
        }

        // QA-V6a-REN-08 (AC-SCN-01-05: "se cierra sin errores aunque todavía esté cargando"): un armado cortado en el tramo de las mallas
        // (el MeshUpload con sus búferes nativos abierto) por Application.lowMemory o por el cierre de la escena no deja búferes vivos.
        [UnityTest, Category("AC-SCN-01-05")]
        public IEnumerator ClosingWhileLoadingLeavesNoNativeBuffers()
        {
            AssumeGpu();
            var leakMode = Unity.Collections.NativeLeakDetection.Mode;
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
            try
            {
                CreateScene(DeviceProfile.TV);
                yield return null;
                var host = SceneHost;
                var baseline = RowMate.Scene.MeshUpload.LiveForTest;
                foreach (var cut in new[] { "lowMemory", "Dispose" })
                {
                    host.Preload();
                    var now = H.Scheduler.MonotonicSeconds;
                    for (var guard = 0; guard < 5000 && !host.BuildingMeshesForTest; guard++)
                    {
                        // Una unidad por llamada (tramo mínimo: RunAnother corre al menos una).
                        host.Tick(now, 1e-6);
                    }

                    Assert.That(host.BuildingMeshesForTest, Is.True, cut + ": a mitad de las mallas");
                    Assert.That(RowMate.Scene.MeshUpload.LiveForTest, Is.EqualTo(baseline + 1), cut + ": el MeshUpload del armado, vivo");
                    if (cut == "lowMemory")
                    {
                        host.SimulateLowMemoryForTest();
                        Assert.That(host.Readiness.Value, Is.EqualTo(SceneReadiness.NotLoaded), "lowMemory desarma el mundo a medias");
                    }
                    else
                    {
                        host.Dispose();
                    }

                    Assert.That(RowMate.Scene.MeshUpload.LiveForTest, Is.EqualTo(baseline), cut + " a mitad de las mallas: ningún búfer nativo vivo");
                    Assert.That(host.BuildingMeshesForTest, Is.False, cut + ": el armado descartado");
                }

                yield return null;
                AssertClean(H);
            }
            finally
            {
                Unity.Collections.NativeLeakDetection.Mode = leakMode;
            }
        }

        // B6a-19 (la escena demorada en el player): con el tramo del player (2 ms por cuadro en el inmersivo, no todo en el acto como en
        // el arnés) el armado avanza aunque los rodillos de cada lectura sigan corriendo, y la escena llega. Antes, con cualquier finita
        // en curso el tramo era 0 y en el build la escena demorada no aparecía nunca ([PERF]: "escena Loading" toda la sesión).
        [UnityTest, Category("AC-SCN-01-07")]
        public IEnumerator TheDelayedSceneArrivesWithTheReadingsRunning()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o => o.ScenePlayerWarmup = true);
            yield return GoToWorkout();
            SceneHost.DelayReadinessForTest(1.5);
            yield return StartFree();
            var arrived = double.NaN;
            var rollers = 0;
            var motions = new System.Collections.Generic.List<RowMate.Presentation.Motion.IMotion>();
            for (var i = 0; i < 8 * 60 && double.IsNaN(arrived); i++)
            {
                H.Step();
                H.Handle.Motion.CopyMotions(motions);
                foreach (var motion in motions)
                {
                    if (motion is RowMate.Presentation.Motion.IRecurringMotion && motion.IsFinite)
                    {
                        rollers++;
                    }
                }

                if (SceneHost.Readiness.Value == SceneReadiness.Ready)
                {
                    arrived = H.Scheduler.MonotonicSeconds;
                }

                yield return null;
            }

            Assert.That(rollers, Is.GreaterThan(0), "los rodillos de las lecturas corrieron mientras se preparaba");
            Assert.That(double.IsNaN(arrived), Is.False, "la escena demorada llegó en el inmersivo con el tramo del player");
            Assert.That(SceneHost.WorstWarmupMs, Is.GreaterThan(0), "el armado corrió por tramos");
            AssertClean(H);
        }

        // AC-SCN-01-05: tres entrenamientos seguidos sin avisos nuevos ni errores (sin el pack: 0 [SLOT]).
        [UnityTest, Category("AC-SCN-01-05")]
        public IEnumerator ThreeSessionsLeaveACleanLog()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            for (var session = 0; session < 3; session++)
            {
                yield return EnterFree();
                yield return AdvanceTo(6);
                yield return Tap("hud-finish");
                yield return Frames();
                H.Tap("dialog-confirm");
                for (var i = 0; i < 60; i++)
                {
                    H.Step();
                    yield return null;
                }
            }

            Assert.That(SceneHost.SlotLog.Warnings, Is.EqualTo(0), "sin el pack: el procedural, sin avisos");
            AssertClean(H);
            foreach (var line in H.Logger.Lines)
            {
                Assert.That(line, Does.Not.Contain("[SLOT]"));
            }
        }

        // §5.1 (QA-G6aT-12): la cámara es determinista (sin el dithering de URP, sin TAA, sin grano) y las colas son las del contrato
        // (cielo 1000 < estrellas 1001 < nubes 1002 < cordilleras 1003; AC-SCN-11-01).
        [UnityTest, Category("AC-SCN-11-01")]
        public IEnumerator CameraIsDeterministicAndQueuesAreOrdered()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var world = SceneHost.World!;
            Assert.That(world.Rig!.Data.dithering, Is.False);
            Assert.That(world.Rig.Data.antialiasing, Is.Not.EqualTo(UnityEngine.Rendering.Universal.AntialiasingMode.TemporalAntiAliasing));
            Assert.That(world.Rig.Camera.enabled, Is.False, "solo a pedido");
            Assert.That(world.Lighting!.Profile.TryGet<UnityEngine.Rendering.Universal.FilmGrain>(out var grain) && grain.intensity.value == 0, Is.True,
                "sin grano");
            Assert.That(world.Sky!.renderQueue, Is.LessThan(world.Stars!.renderQueue));
            Assert.That(world.Stars.renderQueue, Is.LessThan(world.Clouds!.renderQueue));
            Assert.That(world.Clouds.renderQueue, Is.LessThan(world.HorizonFarMaterial!.renderQueue));
        }

        // AC-SCN-11-01 (QA-G6aT-08): las estrellas detrás del astro. A las 22 h, con las estrellas prendidas y apagadas, adentro del disco
        // de la luna no cambia nada.
        [UnityTest, Category("AC-SCN-11-01")]
        public IEnumerator StarsHideBehindTheDisk()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.V390);
            yield return EnterFree();
            yield return SceneAlone(920, 22);
            var host = SceneHost;
            var cam = new SceneCamera(390, 844);
            var sky = host.Frame.Celestial;
            SceneMath.Direction(sky.DiskAzimuthDeg, sky.DiskElevationDeg, out var dx, out var dy, out var dz);
            Assert.That(cam.TryProject(dx * 1000, 2.2 + dy * 1000, dz * 1000, out var sx, out var sy), Is.True);
            var with = Read();
            host.SetStarsForTest(false);
            H.Step();
            yield return Frames(2);
            var without = Read();
            host.SetStarsForTest(true);
            var radius = (int)(host.Frame.DiskRadiusRad * cam.Focal * 0.8);
            var differ = 0;
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radius * radius)
                    {
                        continue;
                    }

                    var a = At(with, (int)sx + x, (int)sy + y);
                    var b = At(without, (int)sx + x, (int)sy + y);
                    differ += Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 0 ? 1 : 0;
                }
            }

            Assert.That(differ, Is.EqualTo(0), "ninguna estrella adentro del disco");
            Destroy(with);
            Destroy(without);
        }
    }
}
