using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // PPG: el relleno continuo de la píldora en curso y el aro de la cuenta grande (02-tech-design §2.3 de la It. 4b; ANIMATION_BIBLE
    // §2.6, §7.1; 04b/00-deuda-diseno §4.5; DD4b-09; QA-G4bT-04). Mejora EXR: el legacy llena por segundo entero
    // (workout_provider.dart:31-36).
    [TestFixture]
    public sealed class PillProgressTests
    {
        private const double Frame = 1.0 / 60;

        [Test, Category("AC-WKT-14-10"), Category("AC-WKT-13-14")]
        public void TableOfTheDesignBase()
        {
            // D = 30 s; ticks a los 1,000, 2,000 y 3,040 s; pausa a los 3,600, reanuda a los 5,000 y el tick siguiente a los 6,000;
            // cuadros cada 1/60 s. 3,560 es el cuadro de la pausa, que avanza antes de aplicarla (la grilla de 1/60).
            var expected = new (double At, double Seconds)[]
            {
                (0.5, 0.500), (1.0, 1.000), (1.5, 1.500), (3.0, 3.000), (3.04, 3.000), (3.3, 3.260), (3.6, 3.560), (4.5, 3.560),
                (5.3, 3.560), (5.6, 3.600), (5.9, 3.900), (6.0, 4.000), (7.0, 5.000),
            };
            var events = new (double At, int Elapsed, bool Running)[]
            {
                (1.0, 1, true), (2.0, 2, true), (3.04, 3, true), (3.6, 3, false), (5.0, 3, true), (6.0, 4, true), (7.0, 5, true),
            };
            var progress = new PillProgress();
            progress.OnState(0, 0, 30, true, 0, false);
            var next = 0;
            var check = 0;
            for (var frame = 1; frame <= 7 * 60; frame++)
            {
                var now = frame * Frame;
                while (next < events.Length && events[next].At < now - 1e-9)
                {
                    // Un estado que llega entre dos cuadros (el tick tarde de 3,04): su propio instante.
                    var e = events[next++];
                    progress.OnState(0, e.Elapsed, 30, e.Running, e.At, false);
                    Probe(progress, e.At, expected, ref check);
                }

                if (next < events.Length && Math.Abs(events[next].At - now) < 1e-9)
                {
                    var e = events[next++];
                    progress.OnState(0, e.Elapsed, 30, e.Running, now, false);
                }

                Probe(progress, now, expected, ref check);
            }

            Assert.That(check, Is.EqualTo(expected.Length), "pasaron todos los instantes de la tabla");
        }

        [Test, Category("AC-WKT-13-14")]
        public void NeverPassesTheNextTick()
        {
            // Con el tick que no llega (el motor trabado), el relleno se queda en (transcurrido + 1) / D: la animación nunca adelanta
            // el reloj (ANIMATION_BIBLE §0.1).
            var progress = new PillProgress();
            progress.OnState(0, 10, 60, true, 100, false);
            Assert.That(progress.ValueAt(100.5) * 60, Is.EqualTo(10.5).Within(1e-9));
            Assert.That(progress.ValueAt(103) * 60, Is.EqualTo(11).Within(1e-9), "el tope: el valor del tick siguiente");
            Assert.That(progress.ValueAt(200) * 60, Is.EqualTo(11).Within(1e-9));
        }

        [Test, Category("AC-WKT-14-10")]
        public void NeverGoesBackWithinAStepAndStillInPause()
        {
            var progress = new PillProgress();
            progress.OnState(0, 0, 20, true, 0, false);
            var last = 0.0;
            for (var frame = 1; frame <= 600; frame++)
            {
                var now = frame * Frame;
                if (frame % 60 == 0)
                {
                    // En pausa el motor no cuenta segundos (los estados siguen llegando con el mismo transcurrido). Un segundo nuevo
                    // lleva al presente del motor (QA-V4b-P02), así que esta prueba ya no manda segundos que el motor no daría.
                    progress.OnState(0, Math.Min(frame / 60, 5), 20, frame < 300, now, false);
                }

                var value = progress.ValueAt(now);
                Assert.That(value, Is.GreaterThanOrEqualTo(last), "nunca baja dentro del paso (" + now + ")");
                last = value;
            }

            Assert.That(last * 20, Is.EqualTo(5).Within(1e-9), "quieto en pausa desde el cuadro de la pausa");
        }

        [Test, Category("AC-WKT-14-10")]
        public void StepChangeAppliesInTheAct()
        {
            var progress = new PillProgress();
            progress.OnState(0, 9, 10, true, 9, false);
            Assert.That(progress.ValueAt(9.9), Is.EqualTo(0.99).Within(1e-9));
            progress.OnState(1, 0, 20, true, 10, false);
            Assert.That(progress.StepIndex, Is.EqualTo(1));
            Assert.That(progress.ValueAt(10), Is.EqualTo(0), "el paso nuevo: transcurrido / D en el acto");
            Assert.That(progress.ValueAt(10.5), Is.EqualTo(0.5 / 20).Within(1e-9));
        }

        [Test, Category("AC-WKT-14-10")]
        public void InstantInTheSameStepNeverGoesBack()
        {
            // QA-G4bT-04, el despertar en marcha: D = 100 s, el equipo duerme a los 62,4 s y el primer cuadro despierto trae Instant
            // con 62 (el reloj despierto sigue continuo: 62,4 + 1/60). Con la regla anterior bajaba a 62,000 un cuadro.
            var progress = new PillProgress();
            progress.OnState(0, 62, 100, true, 62.0, false);
            var asleep = progress.ValueAt(62.4);
            Assert.That(asleep * 100, Is.EqualTo(62.4).Within(1e-9));
            var wake = 62.4 + Frame;
            progress.OnState(0, 62, 100, true, wake, true);
            Assert.That(progress.ValueAt(wake) * 100, Is.EqualTo(62.417).Within(0.001), "el cuadro de la vuelta");
            Assert.That(progress.ValueAt(wake + Frame) * 100, Is.EqualTo(62.433).Within(0.001), "y sigue");

            // La pausa con segundo plano: Instant con 3 a los 4,5 s; queda en 3,56 y al reanudar espera al motor.
            var paused = new PillProgress();
            paused.OnState(0, 3, 30, true, 3.04, false);
            paused.OnState(0, 3, 30, false, 3.6, false);
            paused.OnState(0, 3, 30, false, 4.5, true);
            Assert.That(paused.ValueAt(4.5) * 30, Is.EqualTo(3.56).Within(1e-9), "no retrocede a 3,000");
            paused.OnState(0, 3, 30, true, 5.0, false);
            Assert.That(paused.ValueAt(5.3) * 30, Is.EqualTo(3.56).Within(1e-9), "espera al motor");
            Assert.That(paused.ValueAt(5.6) * 30, Is.EqualTo(3.6).Within(1e-9), "y vuelve a subir desde los 5,56");

            // Con Instant y un segundo nuevo (la vuelta del segundo plano), el presente en el acto.
            var back = new PillProgress();
            back.OnState(0, 8, 30, true, 8, false);
            back.OnState(0, 20, 30, true, 20, true);
            Assert.That(back.ValueAt(20) * 30, Is.EqualTo(20).Within(1e-9));
        }

        // QA-V4b-P02 (Q-52, AC-WKT-14-13): el motor entrega cada segundo en el primer cuadro con hora ≥ su vencimiento, así que dos
        // segundos seguidos pueden llegar a menos de 1 s (un cuadro menos). La foto final trae el reloj quieto (running = false):
        // antes, el relleno se congelaba en (D − 1 + gap) / D y la última píldora quedaba al 98,4-99,9 %. Con un segundo nuevo el
        // relleno vale al menos transcurrido / D, el presente del motor: nunca adelanta el reloj.
        [TestCase(1, 0.984, TestName = "LastTimedStepEndsFull_D1_gap0_984")]
        [TestCase(5, 0.984, TestName = "LastTimedStepEndsFull_D5_gap0_984")]
        [TestCase(1, 0.999, TestName = "LastTimedStepEndsFull_D1_gap0_999")]
        [TestCase(5, 1.016, TestName = "LastTimedStepEndsFull_D5_gap1_016")]
        [Category("AC-WKT-14-13"), Category("AC-WKT-14-10")]
        public void LastTimedStepEndsFull(int duration, double gap)
        {
            var progress = new PillProgress();
            var t = 100.0;
            progress.OnState(3, 0, duration, true, t, false);
            for (var k = 1; k < duration; k++)
            {
                FramesUntil(progress, t, t + 1);
                t += 1;
                progress.OnState(3, k, duration, true, t, false);
            }

            // El último segundo llega a `gap` del anterior, con la fase ya terminada (el reloj quieto).
            FramesUntil(progress, t, t + gap);
            var before = progress.ValueAt(t + gap);
            Assert.That(before, Is.LessThanOrEqualTo(1.0), "nunca pasa de 1");
            progress.OnState(3, duration, duration, false, t + gap, false);
            Assert.That(progress.ValueAt(t + gap), Is.EqualTo(1.0).Within(1e-12), "llena en el cuadro de la foto final");
            Assert.That(progress.ValueAt(t + gap + 5), Is.EqualTo(1.0).Within(1e-12), "y queda llena");

            // En el medio, un segundo que llega antes del vencimiento lleva al presente del motor, nunca más allá del tick siguiente.
            var early = new PillProgress();
            early.OnState(0, 0, 10, true, 0, false);
            FramesUntil(early, 0, 0.984);
            early.OnState(0, 1, 10, true, 0.984, false);
            Assert.That(early.ValueAt(0.984) * 10, Is.EqualTo(1.0).Within(1e-9), "el presente del motor: 1 de 10");
            Assert.That(early.ValueAt(0.984 + Frame) * 10, Is.LessThanOrEqualTo(2.0), "nunca más allá del tick siguiente");
        }

        private static void FramesUntil(PillProgress progress, double from, double to)
        {
            for (var now = from + Frame; now < to - 1e-9; now += Frame)
            {
                progress.ValueAt(now);
            }
        }

        [Test, Category("AC-WKT-14-09")]
        public void NoFillByDistanceOrWithoutDuration()
        {
            var distance = new PillProgress();
            distance.OnState(0, 10, null, true, 0, false);
            Assert.That(distance.Fills, Is.False);
            Assert.That(distance.ValueAt(0.5), Is.EqualTo(0));
            Assert.That(distance.RemainingAt(0.5), Is.EqualTo(0));
            var zero = new PillProgress();
            zero.OnState(0, 0, 0, true, 0, false);
            Assert.That(zero.ValueAt(1), Is.EqualTo(0));
            var negative = new PillProgress();
            negative.OnState(0, 0, -5, true, 0, false);
            Assert.That(negative.Fills, Is.False);
        }

        // Los instantes de las tomas (DN4b-25, +0,2 s; CAP-06 a los 30,25), con ticks en el instante de cada segundo.
        [TestCase(30.0, 20, 40.2, 0.510, TestName = "CaptureFill_CAP01_Estados_40_2")]
        [TestCase(120.0, 60, 150.2, 0.503, TestName = "CaptureFill_CAP21_Prueba4a_150_2")]
        [TestCase(3000.0, 30, 3016.6, 0.553, TestName = "CaptureFill_CAP22_SerieLarga_3016_6")]
        [TestCase(30.0, 60, 50.2, 0.337, TestName = "CaptureFill_CAP23_Prueba4a_50_2")]
        [TestCase(0.0, 30, 18.2, 0.607, TestName = "CaptureFill_DEMO05_Prueba4a_18_2")]
        public void CaptureFills(double stepStart, int duration, double at, double fill)
        {
            var progress = Run(stepStart, duration, at);
            Assert.That(progress.ValueAt(at), Is.EqualTo(fill).Within(0.0005));
        }

        [TestCase(30.0, 20, 47.6, 0.80, TestName = "RingAtK6OfTheThree_CAP02")]
        [TestCase(210.0, 30, 239.2, 0.267, TestName = "RingAt239_2_DEMO10")]
        public void RingFractions(double stepStart, int duration, double at, double ring)
        {
            // El aro de la cuenta grande es lo que falta del paso / 3 (§3.5).
            var progress = Run(stepStart, duration, at);
            Assert.That(CenterDiscTimeline.RingFraction(progress.RemainingAt(at)), Is.EqualTo(ring).Within(0.0005));
        }

        [Test, Category("AC-WKT-14-18")]
        public void RingIsFullAtTheThreeAndEmptyAtTheChange()
        {
            var progress = new PillProgress();
            progress.OnState(0, 17, 20, true, 47.0, false);
            Assert.That(CenterDiscTimeline.RingFraction(progress.RemainingAt(47.0)), Is.EqualTo(1).Within(1e-9), "lleno en el tick del 3");
            progress.OnState(0, 18, 20, true, 48.0, false);
            Assert.That(CenterDiscTimeline.RingFraction(progress.RemainingAt(48.0)), Is.EqualTo(2.0 / 3).Within(1e-9));
            progress.OnState(0, 19, 20, true, 49.0, false);
            Assert.That(CenterDiscTimeline.RingFraction(progress.RemainingAt(49.0)), Is.EqualTo(1.0 / 3).Within(1e-9));
            Assert.That(CenterDiscTimeline.RingFraction(progress.RemainingAt(49.99)), Is.GreaterThan(0), "nunca se vacía antes del tick");
        }

        private static PillProgress Run(double stepStart, int duration, double at)
        {
            var progress = new PillProgress();
            progress.OnState(0, 0, duration, true, stepStart, false);
            var elapsed = 0;
            for (var frame = (long)Math.Ceiling(stepStart * 60); frame <= (long)Math.Floor(at * 60); frame++)
            {
                var now = frame * Frame;
                var second = (int)Math.Floor(now - stepStart + 1e-9);
                if (second > elapsed && second <= duration)
                {
                    elapsed = second;
                    progress.OnState(0, elapsed, duration, true, stepStart + elapsed, false);
                }

                progress.ValueAt(now);
            }

            progress.ValueAt(at);
            return progress;
        }

        private static void Probe(PillProgress progress, double now, (double At, double Seconds)[] expected, ref int check)
        {
            foreach (var e in expected)
            {
                if (Math.Abs(e.At - now) < 1e-9)
                {
                    Assert.That(progress.ValueAt(now) * 30, Is.EqualTo(e.Seconds).Within(0.0005), "a los " + e.At + " s");
                    check++;
                }
            }
        }
    }
}
