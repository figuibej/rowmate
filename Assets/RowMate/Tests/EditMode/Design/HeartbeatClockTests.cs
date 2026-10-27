using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // HeartbeatClock = el latido a 60/bpm (AC-HRM-13-12; ANIMATION_BIBLE §2.9; 04-design-notes §5.3, DN-10), con el reloj
    // que se le pasa: cuadro a cuadro a 60 fps.
    [TestFixture]
    public sealed class HeartbeatClockTests
    {
        private const double Frame = 1.0 / 60;

        [Test, Category("AC-HRM-13-12")]
        public void TwentyBeatsInTenSecondsAt120()
        {
            var clock = new HeartbeatClock();
            clock.SetBpm(120, 0);
            for (var t = 0.0; t < 10; t += Frame)
            {
                clock.ScaleAt(t);
            }

            Assert.That(clock.BeatsStarted, Is.InRange(19, 21), "20 ± 1 latidos en 10 s");
        }

        [Test, Category("AC-HRM-13-12")]
        public void TwentyThreeOrFourBeatsInTenSecondsAt142()
        {
            // VID-01: corazón a 142 por minuto, ≈ 23-24 latidos en 10 s.
            var clock = new HeartbeatClock();
            clock.SetBpm(142, 0);
            for (var t = 0.0; t < 10; t += Frame)
            {
                clock.ScaleAt(t);
            }

            Assert.That(clock.BeatsStarted, Is.InRange(23, 24));
        }

        [Test, Category("AC-HRM-13-12")]
        public void NewPeriodFromTheNextBeat()
        {
            var clock = new HeartbeatClock();
            clock.SetBpm(120, 0);          // latidos en 0 y 0,5
            clock.ScaleAt(0.2);
            clock.SetBpm(60, 0.2);         // el latido en curso termina con su período: el siguiente sigue en 0,5
            Assert.That(clock.BeatsStarted, Is.EqualTo(1));
            clock.ScaleAt(0.49);
            Assert.That(clock.BeatsStarted, Is.EqualTo(1));
            clock.ScaleAt(0.5);
            Assert.That(clock.BeatsStarted, Is.EqualTo(2), "sin salto de fase");
            clock.ScaleAt(1.49);
            Assert.That(clock.BeatsStarted, Is.EqualTo(2), "desde ahí, uno por segundo");
            clock.ScaleAt(1.5);
            Assert.That(clock.BeatsStarted, Is.EqualTo(3));
        }

        [Test, Category("AC-HRM-13-12")]
        public void FirstBeatStartsWithTheFirstPositiveBpm()
        {
            var clock = new HeartbeatClock();
            clock.SetBpm(0, 0);
            Assert.That(clock.ScaleAt(1), Is.EqualTo(1.0), "con -- queda quieto");
            Assert.That(clock.BeatsStarted, Is.EqualTo(0));
            clock.SetBpm(120, 2.0);
            Assert.That(clock.BeatsStarted, Is.EqualTo(1), "el corazón despierta con el número");
            Assert.That(clock.ScaleAt(2.0), Is.EqualTo(1.0).Within(1e-12), "el latido arranca en 1");
            Assert.That(clock.ScaleAt(2.09), Is.EqualTo(1.16).Within(1e-6), "pico del golpe principal a los 90 ms");
        }

        [Test, Category("AC-HRM-13-12")]
        public void ShapeOfOneBeat()
        {
            // 1 + 0,16·p₁ + 0,06·p₂: p₁ sube 0-90 y baja 90-290; p₂ sube 170-240 y baja 240-390 (T ≥ 420 ms, sin compresión).
            const double t = 0.5;
            Assert.That(HeartbeatClock.Shape(0.090, t), Is.EqualTo(1.16).Within(1e-9));
            Assert.That(HeartbeatClock.Shape(0.240, t), Is.GreaterThan(1.06), "el rebote: el segundo golpe sobre lo que queda del primero");
            Assert.That(HeartbeatClock.Shape(0.240, t), Is.LessThan(1.10));
            Assert.That(HeartbeatClock.Shape(0.390, t), Is.EqualTo(1.0).Within(1e-9), "en reposo a los 390 ms");
            Assert.That(HeartbeatClock.Shape(0.450, t), Is.EqualTo(1.0), "reposo hasta el latido siguiente");
            Assert.That(HeartbeatClock.Shape(-0.1, t), Is.EqualTo(1.0));
        }

        [Test, Category("AC-HRM-13-12")]
        public void CompressionAboveOneHundredFortyThree()
        {
            // A 180 bpm, T = 333 ms < 420: los tiempos × T/420 (0,79); los picos no cambian; nunca se encima con el siguiente.
            var period = 60.0 / 180;
            var factor = period / 0.420;
            Assert.That(HeartbeatClock.Shape(0.090 * factor, period), Is.EqualTo(1.16).Within(1e-9), "el pico llega antes");
            Assert.That(HeartbeatClock.Shape(0.390 * factor, period), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(0.390 * factor, Is.LessThan(period), "termina antes del latido siguiente");
            Assert.That(HeartbeatClock.Shape(0.090, 60.0 / 142), Is.EqualTo(1.16).Within(1e-9), "a 142 bpm (T = 422,5 ms) no comprime");
        }

        [Test, Category("AC-HRM-13-12")]
        public void StopReturnsToOneIn120Ms()
        {
            var clock = new HeartbeatClock();
            clock.SetBpm(120, 0);
            var peak = clock.ScaleAt(0.09);
            Assert.That(peak, Is.GreaterThan(1.1));
            clock.SetBpm(0, 0.09); // desconexión o caída
            Assert.That(clock.ScaleAt(0.09), Is.EqualTo(peak).Within(1e-9), "desde donde está");
            var middle = clock.ScaleAt(0.15);
            Assert.That(middle, Is.LessThan(peak));
            Assert.That(middle, Is.GreaterThan(1.0));
            Assert.That(clock.ScaleAt(0.22), Is.EqualTo(1.0), "pasados los 120 ms, en reposo");
            Assert.That(clock.IsAtRest(0.211), Is.True);
            var started = clock.BeatsStarted;
            clock.ScaleAt(5);
            Assert.That(clock.BeatsStarted, Is.EqualTo(started), "sin latidos nuevos");
            Assert.That(clock.IsBeating, Is.False);
        }

        [Test, Category("AC-HRM-13-12")]
        public void AtRestBetweenBeatsForTheSnapshots()
        {
            // DN-21: la foto fija se toma con el corazón en reposo (|escala − 1| ≤ 0,002): a 120 bpm, de 390 a 500 ms.
            var clock = new HeartbeatClock();
            clock.SetBpm(120, 0);
            Assert.That(clock.IsAtRest(0.09), Is.False);
            Assert.That(clock.IsAtRest(0.45), Is.True);
            Assert.That(clock.IsAtRest(0.59), Is.False, "el latido siguiente ya empezó");
            Assert.That(Math.Abs(clock.ScaleAt(0.95) - 1), Is.LessThan(MotionSpecs.BeatRestTolerance));
        }
    }
}
