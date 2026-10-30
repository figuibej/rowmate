using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Motion.Hud;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // CCT · La cuenta grande y PAUSA en el disco del lugar "centro alto" (AC-WKT-14-18, AC-WKT-13-12, AC-WKT-17-09; D-ANI-05;
    // 04b-hud-exr/02-tech-design §2.4, §3.5; DN4b-14): un disco por vez, en HudLayout.BigCount, fuera de la zona del bote.
    public sealed class CenterCalloutTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-14-18"), Category("AC-WKT-17-09")]
        public IEnumerator BigCountInItsDiscWithTheBandAndTheCountdown()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(6.6);
            Assert.That(H.IsShown("hud-big-countdown"), Is.False, "a 4 s del cambio, todavía no");
            yield return AdvanceTo(7.6);
            yield return Settle();
            Assert.That(H.IsShown("hud-big-countdown"), Is.True, "en el tick del 3");
            Assert.That(H.TextOf("hud-big-countdown"), Is.EqualTo("3"));
            Assert.That(H.TextOf("hud-countdown"), Is.EqualTo("0:03"), "en la misma foto que la cuenta de la barra");
            var callouts = Hud.Callouts;
            Assert.That(callouts.Motion.Shown, Is.EqualTo(CenterDisc.Count));
            Assert.That(callouts.Count.FontSize, Is.GreaterThanOrEqualTo(Hud.Spm.ValueSizeDp), "al menos el tamaño del valor de SPM");
            var disc = Immersive.Layout!.BigCount;
            var window = H.Root.worldBound;
            var box = callouts.Count.worldBound;
            Assert.That(box.center.x - window.xMin, Is.EqualTo((float)(disc.Left + disc.Width / 2)).Within(1f), "centrado en el disco (x)");
            Assert.That(box.center.y - window.yMin, Is.EqualTo((float)(disc.Top + disc.Height / 2)).Within(1f), "centrado en el disco (y)");
            var boat = Immersive.Layout.BoatZone;
            Assert.That(disc.Bottom, Is.LessThanOrEqualTo(boat.Top + 0.5), "fuera de la zona del bote");
            Assert.That(callouts.Motion.RingFraction, Is.EqualTo(0.80).Within(0.02), "el aro en 0,80 a los 7,6 s");
            yield return AdvanceTo(10.6);
            yield return Settle();
            Assert.That(H.IsShown("hud-big-countdown"), Is.False, "se fue con el cambio de paso");
        }

        [UnityTest, Category("AC-WKT-13-12"), Category("AC-WKT-14-18")]
        public IEnumerator PausedTakesTheDiscOneAtATime()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(7.6);
            yield return Settle();
            yield return Tap("hud-pause");
            yield return Settle();
            Assert.That(H.IsShown("hud-big-countdown"), Is.False, "en pausa no hay cuenta grande");
            Assert.That(H.IsShown("hud-paused-label"), Is.True, "va PAUSA en el mismo disco");
            Assert.That(H.TextOf("hud-paused-label"), Is.EqualTo("PAUSA"));
            var disc = Immersive.Layout!.BigCount;
            var window = H.Root.worldBound;
            var paused = Bounds("hud-paused-label");
            Assert.That(paused.xMin - window.xMin, Is.EqualTo((float)disc.Left).Within(0.5f), "el disco de HudLayout.BigCount");
            Assert.That(paused.yMin - window.yMin, Is.EqualTo((float)disc.Top).Within(0.5f));
            Assert.That(paused.width, Is.EqualTo((float)disc.Width).Within(0.5f));
            Assert.That(H.Find("hud-paused-label")!.focusable, Is.False, "sin foco");
            // QA-V4b-02: el tracking de 0,08 em va en la unidad de TextCore (8 = 0,08 em), no en dp. Con él, PAUSED (el más ancho)
            // sigue entrando en la cuerda del aro a la altura de su base (59,3 base; 04-design-notes §3.6: 53,9 y 2,7 por lado).
            var label = Immersive.View.Callouts.Paused;
            Assert.That(label.resolvedStyle.letterSpacing, Is.EqualTo(8f).Within(0.001f), "tracking 0,08 em = 8 en la unidad de TextCore");
            var pausedEn = label.MeasureTextSize("PAUSED", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            var pausedEs = label.MeasureTextSize("PAUSA", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.That(pausedEn, Is.EqualTo(53.9f).Within(1.2f), "PAUSED con su tracking (base)");
            Assert.That(pausedEs, Is.EqualTo(45.7f).Within(1.2f), "PAUSA con su tracking (base)");
            Assert.That(pausedEn, Is.LessThanOrEqualTo(59.3f), "PAUSED entra en la cuerda del aro");
            yield return Tap("hud-resume");
            yield return Settle();
            Assert.That(H.IsShown("hud-paused-label"), Is.False, "al reanudar se va");
            Assert.That(H.IsShown("hud-big-countdown"), Is.True, "y la cuenta vuelve con la cifra del momento");
        }
    }
}
