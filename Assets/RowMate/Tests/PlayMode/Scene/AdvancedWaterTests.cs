using System.Collections;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // AWT · El agua avanzada en los 4 escenarios (F-SCN-12, -13; 06b-agua-avanzada/02-tech-design §4, §5; AC-6B-06, -07, -16, -34): con el
    // agua en Auto, la avanzada queda lista después de Ready (fase 13) y se ve con su fundido; la malla en anillos ≤ 20 k triángulos; las
    // marcas por nivel (micro-anillos en Alto y Medio, no en Bajo); la hora cambia sin saltos (las columnas de luz aparecen a la noche).
    [Category("GPU")]
    public sealed class AdvancedWaterTests : SceneTestBase
    {
        internal static IEnumerator UntilAdvanced(SceneTestBase test, SceneHost host, UiTestHost h, int frames = 120)
        {
            host.CompleteWaterForTest();
            for (var i = 0; i < frames && !host.ShowingAdvancedWater; i++)
            {
                h.Step();
                yield return null;
            }

            // El fundido de 0,6 s hasta el final.
            for (var i = 0; i < 45; i++)
            {
                h.Step();
                yield return null;
            }
        }

        private IEnumerator EnterAdvanced(QualityTier tier, EnvironmentId environment = EnvironmentId.Lake)
        {
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = tier;
                o.SceneWater = WaterForce.Auto;
            });
            SceneHost.ForceEnvironmentForTest(environment);
            yield return EnterFree();
            yield return UntilAdvanced(this, SceneHost, H);
        }

        [UnityTest, Category("AC-6B-06"), Category("AC-6B-07")]
        public IEnumerator EachTierShowsTheAdvancedWater([Values(QualityTier.High, QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            yield return EnterAdvanced(tier);
            Assert.That(SceneHost.AdvancedWaterReady, Is.True, "lista después de Ready");
            Assert.That(SceneHost.WaterChoice.Kind, Is.EqualTo(WaterKind.Advanced));
            Assert.That(SceneHost.WaterChoice.Showing, Is.EqualTo(WaterKind.Advanced));
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "el renderer avanzado se dibuja");
            Assert.That(SceneHost.DegradedBlend, Is.EqualTo(0).Within(1e-6), "el fundido terminó");
            var world = SceneHost.World!;
            Assert.That(world.Director!.WaterSurfaces!.Degraded.Renderer.enabled, Is.False, "nunca las dos (G6b-14)");
            var triangles = world.WaterRing!.triangles.Length / 3;
            Assert.That(triangles, Is.LessThanOrEqualTo(20000), "la malla en anillos");
            var keywordHigh = world.AdvancedWater!.Material.IsKeywordEnabled(SceneShaderIds.WaterHighKeyword);
            var keywordLite = world.AdvancedWater.Material.IsKeywordEnabled(SceneShaderIds.WaterLiteKeyword);
            Assert.That(keywordHigh, Is.EqualTo(tier == QualityTier.High));
            Assert.That(keywordLite, Is.EqualTo(tier == QualityTier.Low));
            Assert.That(SceneHost.WaterLogLines, Is.EqualTo(0), "ninguna línea del agua: hay soporte");
            Debug.Log("[AWT] " + tier + ": " + triangles + " triángulos; peor unidad del agua " + SceneHost.WorstWaterUnitMs.ToString("0.00") + " ms");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-06")]
        public IEnumerator EveryEnvironmentShowsTheAdvancedWater([Values(EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta)] EnvironmentId environment)
        {
            AssumeGpu();
            yield return EnterAdvanced(QualityTier.Medium, environment);
            Assert.That(SceneHost.KitEnvironment, Is.EqualTo(environment));
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True, environment.ToString());
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-16")]
        public IEnumerator MarksByTier([Values(QualityTier.High, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            yield return EnterAdvanced(tier);
            yield return AdvanceTo(8);
            var marks = SceneHost.World!.Director!.WaterMarks!;
            Assert.That(marks.RingsEnabled, Is.EqualTo(tier != QualityTier.Low), "micro-anillos en Alto y Medio");
            Assert.That(marks.Count, Is.LessThanOrEqualTo(RowMate.Scene.WaterInteractionRenderer.Capacity));
            Debug.Log("[AWT] marcas " + tier + ": " + marks.Puddles + " charcos, " + marks.Boils + " hervideros, " + marks.Rings + " anillos, " + marks.Columns + " columnas");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-34"), Category("AC-6B-18")]
        public IEnumerator HourChangeWithoutJumps()
        {
            AssumeGpu();
            yield return EnterAdvanced(QualityTier.Medium, EnvironmentId.River);
            foreach (var hour in new[] { 18.0, 20.0, 22.0 })
            {
                SceneHost.ForceHourForTest(hour);
                for (var i = 0; i < 10; i++)
                {
                    H.Step();
                    yield return null;
                    Assert.That(SceneHost.ShowingAdvancedWater, Is.True, hour + " h: la avanzada sigue");
                }
            }

            Assert.That(SceneHost.World!.Director!.WaterMarks!.Columns, Is.GreaterThan(0), "las columnas de los faroles a las 22 h");
            AssertClean(H);
        }
    }
}
