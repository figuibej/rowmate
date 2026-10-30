using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Ports;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // SBU · Las barras del sistema en pantalla (F-APP-15; AC-APP-15-01, -02, -04; 04b-hud-exr/02-tech-design §4): ocultas en el
    // cuadro del toque, visibles al terminar la salida (640), con el doble FakeSystemUi; los márgenes no se mueven; el escritorio no
    // toca la ventana.
    public sealed class SystemBarsTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-APP-15-04"), Category("AC-APP-15-01")]
        public IEnumerator RequestsInOrder()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            Assert.That(H.SystemUi.Requests.Count, Is.EqualTo(0), "en reposo, ningún pedido");
            yield return Frames();
            H.Tap("workout-free");
            Assert.That(H.SystemUi.Immersive, Is.True, "ocultas en el cuadro del toque");
            yield return Settle();
            yield return AdvanceTo(5.2);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Advance(0.5);
            yield return Frames();
            Assert.That(H.SystemUi.Immersive, Is.True, "durante la salida siguen ocultas");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.SystemUi.Immersive, Is.False, "visibles al terminar la salida");
            Assert.That(H.SystemUi.Requests.Count, Is.EqualTo(2), "un pedido de cada uno: " + H.SystemUi.Describe());
        }

        [UnityTest, Category("AC-APP-15-01")]
        public IEnumerator InsetsDoNotMoveWithTheBars()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Insets = new SafeInsets(0, 24, 0, 48));
            yield return StartFree();
            var spm = Bounds("hud-spm");
            Assert.That(H.SystemUi.Immersive, Is.True);
            yield return Advance(1);
            Assert.That(Bounds("hud-spm").yMin, Is.EqualTo(spm.yMin).Within(0.01f), "el HUD no se mueve con las barras");
        }

        [UnityTest, Category("AC-APP-15-02")]
        public IEnumerator DesktopDoesNotTouchTheWindow()
        {
            var mode = UnityEngine.Screen.fullScreenMode;
            var width = UnityEngine.Screen.width;
            var height = UnityEngine.Screen.height;
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(2.2);
            Assert.That(UnityEngine.Screen.fullScreenMode, Is.EqualTo(mode), "no pasa a pantalla completa");
            Assert.That(UnityEngine.Screen.width, Is.EqualTo(width));
            Assert.That(UnityEngine.Screen.height, Is.EqualTo(height));
            Assert.That(H.SystemUi.Immersive, Is.True, "el pedido se hizo; en escritorio no tiene efecto (DesktopSystemUi)");
        }
    }
}
