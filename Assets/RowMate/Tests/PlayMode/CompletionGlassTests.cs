using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using RowMate.Tests.PlayMode.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // CGT · El diálogo de completado de vidrio (AC-6B-27; P-19; 06b-agua-avanzada/02-tech-design §4.4): con la escena, la caja lleva
    // .rm-dialog--glass y el mismo compuesto desenfocado que el HUD; el velo del modal no cambia.
    [Category("GPU")]
    public sealed class CompletionGlassTests : SceneTestBase
    {
        [UnityTest, Category("AC-6B-27")]
        public IEnumerator TheCompletionDialogIsGlass()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV, o =>
            {
                o.SceneTier = QualityTier.Medium;
                o.SceneWater = WaterForce.Auto;
            });
            yield return EnterFree();
            yield return AdvanceTo(4);
            var dialog = Immersive.OpenCompletionForTest(new RowMate.App.Workout.HudCompletion("20:00", 1200, "4000 m", 4000, "300", 300));
            yield return Settle();
            Assert.That(dialog.Box.ClassListContains("rm-dialog--glass"), Is.True);
            Assert.That(Hud.Glass!.Pieces, Does.Contain(dialog.Box), "el mismo vidrio que el HUD");
            Assert.That(dialog.Box.resolvedStyle.backgroundImage.renderTexture, Is.Not.Null, "el compuesto desenfocado");
            AssertClean(H);
        }
    }
}
