using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // WRT · La estela en la escena (F-SCN-12; 06b-agua-avanzada/02-tech-design §4.3; DT6b-23; AC-6B-14): la historia de WakeTrail (Core) llega
    // a _RmWake cada cuadro sin asignar; la cinta se dibuja con el material de WaterMarks (el mismo que el lote de marcas: 0 SetPass nuevos)
    // solo con puntos vivos; con la degradada se apaga (_RmWakeFade = 1 − el fundido). La intensidad y la forma están en WakeTrailTests.
    [Category("GPU")]
    public sealed class WakeRendererTests : SceneTestBase
    {
        [UnityTest, Category("AC-6B-14")]
        public IEnumerator TheWakeFollowsTheBoatWithTheMarksMaterial()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.Medium;
                o.SceneWater = WaterForce.Auto;
            });
            yield return EnterFree();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
            yield return AdvanceTo(10);
            var director = SceneHost.World!.Director!;
            var wake = director.Wake!;
            Assert.That(wake, Is.Not.Null);
            Assert.That(wake.LivePoints, Is.GreaterThan(0), "remando, la estela tiene puntos vivos");
            Assert.That(wake.HeadIntensity, Is.GreaterThan(0));
            Assert.That(Shader.GetGlobalFloat("_RmWakeFade"), Is.EqualTo(1).Within(1e-4), "con la avanzada, a pleno");
            Debug.Log("[WRT] estela: " + wake.LivePoints + " puntos vivos, intensidad " + wake.HeadIntensity.ToString("0.00"));
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-14"), Category("AC-6B-09")]
        public IEnumerator DegradedWaterHasNoWake()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o => o.SceneTier = QualityTier.Medium);
            yield return EnterFree();
            yield return AdvanceTo(6);
            Assert.That(SceneHost.WaterChoice.Showing, Is.EqualTo(WaterKind.Degraded), "lo de antes, con la degradada fija (DT6b-24)");
            Assert.That(Shader.GetGlobalFloat("_RmWakeFade"), Is.EqualTo(0).Within(1e-4), "sin estela con la degradada");
            AssertClean(H);
        }
    }
}
