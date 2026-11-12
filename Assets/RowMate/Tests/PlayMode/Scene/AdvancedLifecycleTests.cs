using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // ALC · La escena viva en los 4 escenarios con el agua avanzada (AC-6B-22; QA-V6b-06; LT-84 a LT-87 y LT-127 a LT-138 con la avanzada):
    // los 16 casos de scene_view_test.dart:11 (4 escenarios × 390 × 844 y 844 × 390 × 12 y 23 h; SPM 24, split 110, 300 m, en marcha,
    // hora forzada) con el agua en Auto animan 6 cuadros de 100 ms con la avanzada a la vista y se desmontan sin excepciones ni avisos; y
    // el cierre (terminar el entrenamiento o desmontar) mientras la avanzada se prepara (a mitad de sus unidades) o llegó pero todavía no se
    // ve (HoldAdvancedWaterForTest) no deja excepciones, avisos ni las RT del agua vivas. Las de la 6a y la 8 siguen con la degradada fija.
    [Category("GPU")]
    public sealed class AdvancedLifecycleTests : SceneTestBase
    {
        private static readonly EnvironmentId[] Environments = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        // Los nombres de las RT del agua avanzada (el espejo de Alto y la del sub-dibujo de calentamiento).
        private static readonly string[] WaterTargets = { "RowMate.WaterReflection", "RowMate.WaterWarmup" };

        public static IEnumerable<TestCaseData> Animates()
        {
            foreach (var id in Environments)
            {
                foreach (var size in new[] { new[] { 390, 844 }, new[] { 844, 390 } })
                {
                    foreach (var hour in new[] { 12, 23 })
                    {
                        yield return new TestCaseData(id, size[0], size[1], hour).Returns(null)
                            .SetName("EnvironmentId." + EnvironmentKeys.ToKey(id) + " " + size[0] + "x" + size[1] + " " + hour + ".0h anima sin excepciones con el agua avanzada")
                            .SetCategory("AC-6B-22");
                    }
                }
            }
        }

        private UiTestHost CreateIn(EnvironmentId id, DeviceProfile profile, QualityTier tier = QualityTier.High)
        {
            var preferences = new MemoryPreferences();
            if (id != EnvironmentId.Lake)
            {
                preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
            }

            return CreateScene(profile, o =>
            {
                o.Preferences = preferences;
                o.SceneTier = tier;
                o.SceneWater = WaterForce.Auto;
            });
        }

        private static int LiveWaterTargets()
        {
            var count = 0;
            foreach (var texture in Resources.FindObjectsOfTypeAll<RenderTexture>())
            {
                if (texture != null && System.Array.IndexOf(WaterTargets, texture.name) >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        // legacy: test/scene/scene_view_test.dart:11 'EnvironmentId.<escenario> <ancho>x<alto> <hora>.0h anima sin excepciones' (16 casos)
        [TestCaseSource(nameof(Animates))]
        public IEnumerator AnimatesWithTheAdvancedWaterAndTearsDown(EnvironmentId id, int width, int height, int hour)
        {
            AssumeGpu();
            var baseline = LiveWaterTargets();
            CreateIn(id, width < height ? DeviceProfile.V390 : DeviceProfile.H844);
            H.App.Scene.HourOverride.Value = hour;
            yield return EnterFree();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "con el agua avanzada a la vista");
            SceneHost.ForceDistanceForTest(300);
            var before = SceneHost.Draws;
            for (var i = 0; i < 6; i++)
            {
                H.Advance(0.1);
                yield return null;
                Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "la avanzada en cada cuadro (" + i + ")");
            }

            Assert.That(SceneHost.Draws - before, Is.GreaterThanOrEqualTo(6), "un dibujo por cuadro");
            Assert.That(SceneHost.Frame.Speed, Is.GreaterThan(0), "remando, el bote avanza");
            Assert.That(SceneHost.Frame.Environment, Is.EqualTo(id));
            Assert.That(SceneHost.WaterChoice.Kind, Is.EqualTo(WaterKind.Advanced));
            Assert.That(SceneHost.WaterLogLines, Is.EqualTo(0), "sin caída");
            AssertClean(H);
            DisposeHost();
            yield return Frames(2);
            LogAssert.NoUnexpectedReceived();
            Assert.That(LiveWaterTargets(), Is.EqualTo(baseline), "ninguna RT del agua viva después de desmontar");
        }

        // El cierre mientras la avanzada se prepara: "preparando" = sus unidades paradas antes de "lista" (con el espejo y la RT del sub-dibujo
        // ya creados); "retenida" = lista pero todavía sin mostrarse (HoldAdvancedWaterForTest). "terminar" = Terminar y confirmar (la escena
        // queda precargada para la próxima) y después desmontar; "desmontar" = el anfitrión se desmonta con el inmersivo abierto.
        [UnityTest, Category("AC-6B-22"), Category("AC-SCN-01-05")]
        public IEnumerator ClosingWhileTheAdvancedWaterIsPreparing([Values(QualityTier.High, QualityTier.Medium)] QualityTier tier,
                                                                  [Values("preparando", "retenida")] string state,
                                                                  [Values("terminar", "desmontar")] string how)
        {
            AssumeGpu();
            yield return Frames(2);
            var baseline = LiveWaterTargets();
            CreateIn(EnvironmentId.Lake, DeviceProfile.TV, tier);
            if (state == "preparando")
            {
                SceneHost.StopWaterWarmupAtForTest(4);
            }

            SceneHost.HoldAdvancedWaterForTest(true);
            yield return EnterFree();
            yield return AdvanceTo(4);
            SceneHost.CompleteWaterForTest();
            for (var i = 0; i < 10; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(SceneHost.ShowingAdvancedWater, Is.False, "la avanzada todavía no se ve");
            Assert.That(SceneHost.IsShowing, Is.True, "la escena a la vista con la degradada");
            if (state == "preparando")
            {
                Assert.That(SceneHost.WaterWarmupPending, Is.True, "a mitad de la preparación");
                Assert.That(SceneHost.WaterWarmupStepForTest, Is.EqualTo(4), "el sub-dibujo hecho, sin 'lista'");
                Assert.That(SceneHost.AdvancedWaterReady, Is.False);
            }
            else
            {
                Assert.That(SceneHost.AdvancedWaterReady, Is.True, "lista y retenida");
            }

            Assert.That(SceneHost.World!.Reflection != null, Is.EqualTo(tier == QualityTier.High), "el espejo solo en Alto");
            Assert.That(LiveWaterTargets(), Is.GreaterThan(baseline), "las RT del agua existen al cerrar");
            if (how == "terminar")
            {
                yield return Tap("hud-finish");
                yield return Frames();
                H.Tap("dialog-confirm");
                for (var i = 0; i < 60; i++)
                {
                    H.Step();
                    yield return null;
                }

                Assert.That(SceneHost.IsOpen, Is.False, "el inmersivo se cerró");
                Assert.That(SceneHost.ShowingAdvancedWater, Is.False);
            }

            AssertClean(H);
            DisposeHost();
            yield return Frames(2);
            LogAssert.NoUnexpectedReceived();
            Assert.That(LiveWaterTargets(), Is.EqualTo(baseline), "ninguna RT del agua viva después de desmontar");
        }
    }
}
