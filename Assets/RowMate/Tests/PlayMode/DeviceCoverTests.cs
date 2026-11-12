using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // DCT · Dispositivo no dibuja debajo del inmersivo (AC-6B-05; 06b-agua-avanzada/02-tech-design §2.4): con el negro de la entrada tapando
    // (UiContext.ImmersiveCover, desde el cuadro del armado del HUD hasta que en la salida empieza a verse Workout), cada lectura solo marca
    // sucio (antes, ≈ 20 asignaciones por lectura); al destaparse, si quedó sucio, dibuja en ese mismo cuadro, antes de que se vea.
    public sealed class DeviceCoverTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-6B-05")]
        public IEnumerator NoRenderUnderImmersive()
        {
            CreateWorkout();
            yield return StartFree();
            yield return AdvanceTo(1);
            var device = H.DeviceScreen;
            Assert.That(device, Is.Not.Null);
            Assert.That(H.Shell.Ui.ImmersiveCover.Value, Is.True, "el negro del inmersivo tapa");
            var before = device!.Renders;
            // 60 s a 1 Hz.
            yield return AdvanceTo(61);
            Assert.That(device.Renders, Is.EqualTo(before), "0 dibujos de Dispositivo debajo del inmersivo");
            Assert.That(device.Dirty, Is.True, "las lecturas quedaron marcadas");
            AssertClean(H);
        }

        [UnityTest, Category("AC-6B-05")]
        public IEnumerator FirstVisibleFrameShowsLastReading()
        {
            CreateWorkout();
            yield return StartFree();
            yield return AdvanceTo(5);
            var device = H.DeviceScreen!;
            var before = device.Renders;
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            var revealed = false;
            for (var i = 0; i < 120 && !revealed; i++)
            {
                H.Step();
                yield return null;
                revealed = !H.Shell.Ui.ImmersiveCover.Value;
            }

            Assert.That(revealed, Is.True, "la salida destapó");
            Assert.That(device.Renders, Is.EqualTo(before + 1), "un solo dibujo, en el cuadro en que se destapa");
            Assert.That(device.Dirty, Is.False);
            AssertClean(H);
        }
    }
}
