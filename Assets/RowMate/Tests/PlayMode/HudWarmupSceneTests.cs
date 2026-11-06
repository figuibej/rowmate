using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Workout;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // HWS · scene-surface solo muestra (06a-escena-lago/02-tech-design §3.3; QA-G6aT-16; AC-SCN-01-05, AC-WKT-13-09): el precalentado
    // del HUD (HudWarmup, que arma y desarma dos HudView en el arranque) con una escena de prueba deja 0 suscriptores de OutputChanged y
    // de Drawn y 0 llamadas a Preload u Open; un HUD que se arma y se cierra, lo mismo.
    public sealed class HudWarmupSceneTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-SCN-01-05"), Category("AC-WKT-13-09")]
        public IEnumerator TheWarmupLeavesNoSubscriptions()
        {
            var fake = new FakeImmersiveScene(new RowMate.Tests.Support.RecordingLogger());
            CreateWorkout(null, o => o.FakeScene = fake);
            yield return Frames();
            var warmup = new HudWarmup(H.Shell.Ui, H.Root);
            warmup.Start();
            for (var i = 0; i < 10 && !warmup.Tick(); i++)
            {
                yield return null;
            }

            warmup.Dispose();
            yield return Frames();
            Assert.That(fake.OutputSubscribers, Is.EqualTo(0), "suscriptores de OutputChanged");
            Assert.That(fake.DrawnSubscribers, Is.EqualTo(0), "suscriptores de Drawn");
            Assert.That(fake.Preloads, Is.EqualTo(0));
            Assert.That(fake.Opens, Is.EqualTo(0));
        }

        [UnityTest, Category("AC-WKT-13-09")]
        public IEnumerator AClosedHudLeavesNoSubscriptions()
        {
            var fake = new FakeImmersiveScene(new RowMate.Tests.Support.RecordingLogger());
            CreateWorkout(null, o => o.FakeScene = fake);
            yield return StartFree();
            yield return AdvanceTo(3);
            Assert.That(fake.Opens, Is.EqualTo(1), "el inmersivo abre la escena en el toque");
            Assert.That(fake.OutputSubscribers, Is.EqualTo(1), "scene-surface a la vista: una suscripción");
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Settle();
            yield return Frames(4);
            Assert.That(fake.OutputSubscribers, Is.EqualTo(0));
            Assert.That(fake.DrawnSubscribers, Is.EqualTo(0));
            Assert.That(fake.Closes, Is.EqualTo(1), "cerrada detrás del negro");
        }
    }
}
