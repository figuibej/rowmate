using System.Collections;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Presentation.Workout;
using RowMate.Tests.PlayMode.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HGT · El vidrio desenfocado del HUD (F-WKT-17; 06b-agua-avanzada/02-tech-design §4.4; DT6b-08 a -11; AC-6B-23, -25, -26): con la escena a
    // la vista en Alto y Medio, cada pieza de vidrio (las rm-hud-glass y la placa de pausa) muestra el compuesto desenfocado _RmHudGlass
    // alineado con scene-surface, con el peso que llega a 1 en 200 ms; en Bajo, el respaldo plano (peso 0, sin desenfoque); al bajar a Bajo
    // funde en 200 ms; no depende del agua; 0 elementos nuevos (HudBudgetTests sin cambios).
    [Category("GPU")]
    public sealed class HudGlassTests : SceneTestBase
    {
        private IEnumerator Enter(QualityTier tier, WaterForce water = WaterForce.Auto)
        {
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = tier;
                o.SceneWater = water;
            });
            yield return EnterFree();
            yield return Advance(0.5);
        }

        private static bool ShowsGlass(VisualElement piece)
        {
            var image = piece.resolvedStyle.backgroundImage;
            return image.renderTexture != null;
        }

        [UnityTest, Category("AC-6B-23")]
        public IEnumerator EveryGlassPieceShowsTheBlur([Values(QualityTier.High, QualityTier.Medium)] QualityTier tier)
        {
            AssumeGpu();
            yield return Enter(tier);
            var glass = Hud.Glass;
            Assert.That(glass, Is.Not.Null, "con escena, el HUD arma el vidrio");
            Assert.That(glass!.Pieces.Count, Is.GreaterThanOrEqualTo(6), "las tarjetas, la banda, la barra de arriba y la placa de pausa");
            Assert.That(SceneHost.GlassInUse, Is.True);
            Assert.That(SceneHost.GlassWeight, Is.EqualTo(1).Within(1e-6), "a pleno");
            Assert.That(SceneHost.BlurRuns, Is.GreaterThan(0));
            Assert.That(SceneHost.GlassTexture, Is.Not.Null);
            foreach (var piece in glass.Pieces.Where(p => p.panel != null && p.resolvedStyle.display == DisplayStyle.Flex))
            {
                Assert.That(ShowsGlass(piece), Is.True, piece.name + ": el compuesto de fondo");
                Assert.That(piece.resolvedStyle.unityBackgroundImageTintColor.a, Is.EqualTo(1).Within(1e-3), piece.name);
            }

            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-25")]
        public IEnumerator LowUsesFallback()
        {
            AssumeGpu();
            yield return Enter(QualityTier.Low);
            Assert.That(SceneHost.GlassWeight, Is.EqualTo(0).Within(1e-6), "en Bajo, el respaldo plano");
            Assert.That(SceneHost.BlurRuns, Is.EqualTo(0), "sin desenfoque");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-25")]
        public IEnumerator TierChangeCrossfades200Ms()
        {
            AssumeGpu();
            yield return Enter(QualityTier.Medium);
            Assert.That(SceneHost.GlassWeight, Is.EqualTo(1).Within(1e-6));
            Assert.That(SceneHost.LowerTier(), Is.True, "Medio → Bajo");
            H.Step();
            yield return null;
            H.Step();
            yield return null;
            var middle = SceneHost.GlassWeight;
            Assert.That(middle, Is.GreaterThan(0).And.LessThan(1), "funde (200 ms)");
            yield return Advance(0.25);
            Assert.That(SceneHost.GlassWeight, Is.EqualTo(0).Within(1e-6), "en Bajo, el respaldo");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-25")]
        public IEnumerator BlurIndependentOfWater()
        {
            AssumeGpu();
            yield return Enter(QualityTier.Medium, WaterForce.Degraded);
            Assert.That(SceneHost.WaterChoice.Showing, Is.EqualTo(WaterKind.Degraded));
            Assert.That(SceneHost.GlassWeight, Is.EqualTo(1).Within(1e-6), "el vidrio no depende del agua");
            AssertClean(H);
        }
    }
}
