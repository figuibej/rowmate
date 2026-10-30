using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // PVT · El velo de pausa (AC-WKT-13-12, AC-WKT-16-03, AC-WKT-14-06; 04b-hud-exr/02-tech-design §3.7; DD4b-14): el fondo se
    // oscurece un 15 % (el tinte de su textura, sin elementos nuevos), PAUSA en su disco, el chip en el cuadro de la fase y las
    // tarjetas y la barra al 100 %.
    public sealed class PauseVeilTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-13-12"), Category("AC-WKT-16-03")]
        public IEnumerator VeilPausedLabelAndCardsAtFullOpacity()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(5.2);
            var children = Hud.childCount;
            yield return Tap("hud-pause");
            Assert.That(H.TextOf("hud-phase-label"), Is.EqualTo("PAUSADO"), "el chip en el cuadro de la fase");
            yield return Settle();
            Assert.That(Hud.Backdrop.Veil, Is.EqualTo(MotionSpecs.PauseVeilAlpha).Within(1e-6), "el fondo, un 15 % más oscuro");
            Assert.That(Hud.childCount, Is.EqualTo(children), "sin elementos nuevos: es el tinte de la textura");
            Assert.That(H.IsShown("hud-paused-label"), Is.True, "PAUSA en su disco");
            foreach (var name in new[] { "hud-topbar", "hud-spm", "hud-split", "hud-watts", "hud-band" })
            {
                Assert.That(H.Find(name)!.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f), name + " al 100 %");
            }

            yield return Tap("hud-resume");
            yield return Settle();
            Assert.That(Hud.Backdrop.Veil, Is.EqualTo(0).Within(1e-6), "al reanudar todo vuelve");
            Assert.That(H.IsShown("hud-paused-label"), Is.False);
        }

        [UnityTest, Category("AC-WKT-14-06")]
        public IEnumerator PulsesKeepGoingInPause()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(6.2);
            yield return Tap("hud-pause");
            yield return Settle();
            var pulse = Hud.Steps!.Pulse;
            Assert.That(pulse.IsPulsing, Is.True, "Próximo sigue latiendo en pausa");
            Assert.That(H.Handle.Motion.Contains(pulse), Is.True);
        }
    }
}
