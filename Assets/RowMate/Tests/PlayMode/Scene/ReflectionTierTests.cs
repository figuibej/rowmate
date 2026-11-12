using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // RTT · El reflejo por nivel (F-SCN-12; 06b-agua-avanzada/02-tech-design §4.1, §4.2, §5; DT6b-05; AC-6B-12; #13): en Alto el reflejo
    // planar (½ × ½ de la RT de la escena, con el bote y el remero) se dibuja cada cuadro con la avanzada a la vista; en Medio y en Bajo no
    // hay planar (Medio: el cielo analítico con la silueta; Bajo: solo el cielo). LowerReflection baja el peso a 0 en 0,6 s y deja de dibujar
    // el espejo. Las sondas de color (el casco reflejado, la orilla, la banda) van en las capturas de it6b.
    [Category("GPU")]
    public sealed class ReflectionTierTests : SceneTestBase
    {
        private IEnumerator Enter(QualityTier tier)
        {
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = tier;
                o.SceneWater = WaterForce.Auto;
            });
            yield return EnterFree();
            yield return AdvancedWaterTests.UntilAdvanced(this, SceneHost, H);
        }

        [UnityTest, Category("AC-6B-12")]
        public IEnumerator HighDrawsBoatAndRower()
        {
            AssumeGpu();
            yield return Enter(QualityTier.High);
            var reflection = SceneHost.World!.Reflection;
            Assert.That(reflection, Is.Not.Null, "el espejo existe en Alto");
            var before = reflection!.Renders;
            yield return Frames(5);
            H.Step();
            yield return null;
            Assert.That(SceneHost.ReflectionDrawn, Is.True);
            Assert.That(reflection.Renders, Is.GreaterThan(before), "un dibujo del espejo por cuadro");
            var rt = reflection.Texture!;
            var scene = SceneHost.Output!;
            Assert.That(rt.width, Is.EqualTo(Mathf.Max(16, scene.width / 2)), "½ por eje");
            Assert.That(rt.useMipMap, Is.True, "con mips");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-12")]
        public IEnumerator MediumAndLowHaveNoPlanar([Values(QualityTier.Medium, QualityTier.Low)] QualityTier tier)
        {
            AssumeGpu();
            yield return Enter(tier);
            Assert.That(SceneHost.World!.Reflection, Is.Null, "sin espejo (DT6b-05)");
            Assert.That(SceneHost.ReflectionDrawn, Is.False);
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-31")]
        public IEnumerator LowerReflectionFadesTheMirrorOut()
        {
            AssumeGpu();
            yield return Enter(QualityTier.High);
            SceneHost.LowerReflection();
            H.Step();
            yield return null;
            Assert.That(SceneHost.ReflectionMix, Is.LessThan(1).And.GreaterThan(0), "baja en 0,6 s");
            yield return Advance(0.7);
            Assert.That(SceneHost.ReflectionMix, Is.EqualTo(0).Within(1e-6));
            H.Step();
            yield return null;
            Assert.That(SceneHost.ReflectionDrawn, Is.False, "sin peso, sin espejo");
            Assert.That(SceneHost.ShowingAdvancedWater, Is.True, "la avanzada sigue");
            AssertClean(H);
        }
    }
}
