using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WKT · La estela con historia (DV-151; AC-6B-14; F-SCN-03; 00-arte-agua §6.1): 48 puntos cada 0,25 s (12 s), la intensidad del legacy
    // por punto al nacer, con el surge de la palada, y el apagado con la inercia.
    [TestFixture]
    public sealed class WakeTrailTests
    {
        private static void Row(WakeTrail w, ref double now, ref double d, double seconds, Func<double, double> speed)
        {
            var frames = (int)(seconds * 60 + 0.5);
            for (var i = 0; i < frames; i++)
            {
                now += 1.0 / 60;
                var v = speed(now);
                d += v / 60;
                w.Step(now, d, v);
            }
        }

        [Test, Category("AC-6B-14")]
        public void IntensityFromSpeed()
        {
            var w = new WakeTrail();
            var now = 0.0;
            var d = 0.0;
            Row(w, ref now, ref d, 2, _ => 0);
            Assert.That(w.HeadIntensity, Is.EqualTo(0), "con el bote quieto no hay estela");
            for (var k = 0; k < w.Count; k++)
            {
                w.Point(k, out _, out var i0, out _, out _);
                Assert.That(i0, Is.EqualTo(0));
            }

            Row(w, ref now, ref d, 2, _ => 2);
            Assert.That(w.HeadIntensity, Is.EqualTo(0.5), "2 m/s → 0,5");
            Row(w, ref now, ref d, 2, _ => 4.2);
            Assert.That(w.HeadIntensity, Is.EqualTo(1), "Fuerte (≥ 4 m/s): a pleno");
            w.Point(0, out var relZ, out var newest, out var age, out var back);
            Assert.That(newest, Is.EqualTo(1));
            Assert.That(age, Is.LessThan(WakeTrail.Interval + 1e-9));
            Assert.That(relZ, Is.EqualTo(WaterModel.SternRelZ - back).Within(1e-9), "nace en la popa a 5,3 m y se queda en el mundo");
        }

        [Test, Category("AC-6B-14")]
        public void PersistsTwelveSecondsAndOpens()
        {
            var w = new WakeTrail();
            var now = 0.0;
            var d = 0.0;
            Row(w, ref now, ref d, 20, _ => 4);
            Assert.That(w.Count, Is.EqualTo(WakeTrail.Capacity), "48 puntos vivos");
            w.Point(WakeTrail.Capacity - 1, out var relZ, out _, out var age, out var back);
            Assert.That(age, Is.InRange(11.75, 12.0), "el más viejo, casi 12 s (47 intervalos más lo que pasó desde el último)");
            Assert.That(back, Is.EqualTo(4 * age).Within(0.1), "a 4 m/s la V mide ≈ 48 m");
            Assert.That(relZ, Is.LessThan(0), "ya quedó detrás de la cámara (se ve en la toma aérea)");
            Assert.That(WaterModel.WakeHalfWidth(back), Is.GreaterThan(WaterModel.WakeHalfWidth(1)), "la V se abre");
            Assert.That(WakeTrail.ArmWidth(age), Is.GreaterThan(WakeTrail.ArmWidth(0)));
            Assert.That(WakeTrail.CenterWidth(12), Is.EqualTo(2.5).Within(1e-12));
            Assert.That(WakeTrail.CenterWidth(0), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(WakeTrail.Alpha(1, 0), Is.EqualTo(0.35).Within(1e-12), "espuma como mucho 35 %");
            Assert.That(WakeTrail.Alpha(1, 12), Is.LessThan(0.02), "a los 12 s vale < 0,02 y el punto se recicla");
        }

        [Test, Category("AC-6B-14")]
        public void SurgeBeats()
        {
            // La velocidad del casco con el surge (ANIMATION_BIBLE §4.5) cambia la intensidad al nacer de cada punto: la estela "late".
            var w = new WakeTrail();
            var now = 0.0;
            var d = 0.0;
            var amplitude = BoatSurge.Amplitude(24, 0.6);
            Row(w, ref now, ref d, 13, t => 3.0 * (1 + amplitude * BoatSurge.SpeedFactor(t * 24 / 60 % 1.0, 0.35)));
            double min = double.MaxValue, max = double.MinValue;
            for (var k = 0; k < w.Count; k++)
            {
                w.Point(k, out _, out var i0, out _, out _);
                min = Math.Min(min, i0);
                max = Math.Max(max, i0);
            }

            Assert.That(max - min, Is.GreaterThan(0.05), "cada drive deja su marca");
            Assert.That(max, Is.LessThanOrEqualTo(1));
        }

        [Test, Category("AC-6B-14")]
        public void FadesWithInertia()
        {
            // Al pausar el bote frena con 1,5 s de inercia: los puntos nuevos nacen cada vez más débiles y los viejos se apagan; después de
            // 12 s quieto no queda estela (y el reloj de la escena sigue: el agua sigue ondulando).
            var w = new WakeTrail();
            var now = 0.0;
            var d = 0.0;
            Row(w, ref now, ref d, 12, _ => 4.2);
            var stop = now;
            Row(w, ref now, ref d, 3, t => 4.2 * Math.Exp(-(t - stop) / 1.5));
            w.Point(0, out _, out var fresh, out _, out _);
            Assert.That(fresh, Is.LessThan(0.2));
            Row(w, ref now, ref d, 12, _ => 0);
            var packed = new float[WakeTrail.Capacity * WakeTrail.PackedStride];
            w.CopyTo(packed);
            for (var k = 0; k < WakeTrail.Capacity; k++)
            {
                var alpha = WakeTrail.Alpha(packed[k * 4 + 1], packed[k * 4 + 2]);
                Assert.That(alpha, Is.LessThan(0.02), "punto " + k);
            }
        }

        [Test, Category("AC-6B-14")]
        public void PrimeFillsTwelveSecondsAtTheSteadySpeed()
        {
            // G6b-15: las tomas con distancia forzada precargan la estela.
            var w = new WakeTrail();
            w.Prime(3.2, 920, 4.2);
            Assert.That(w.Count, Is.EqualTo(WakeTrail.Capacity));
            w.Point(0, out var relZ0, out var i0, out var age0, out _);
            Assert.That(relZ0, Is.EqualTo(WaterModel.SternRelZ).Within(1e-9));
            Assert.That(i0, Is.EqualTo(1));
            Assert.That(age0, Is.EqualTo(0));
            w.Point(47, out var relZ47, out _, out var age47, out var back47);
            Assert.That(age47, Is.EqualTo(11.75).Within(1e-9));
            Assert.That(back47, Is.EqualTo(4.2 * 11.75).Within(1e-9));
            Assert.That(relZ47, Is.EqualTo(WaterModel.SternRelZ - back47).Within(1e-9));
        }

        [Test, Category("AC-6B-14")]
        public void TimeBackwardsStartsOver()
        {
            var w = new WakeTrail();
            w.Step(10, 0, 4);
            w.Step(11, 4, 4);
            w.Step(1, 0, 4);
            Assert.That(w.Count, Is.EqualTo(1), "otra sesión de escena (Open): la estela arranca de cero");
            var packed = new float[WakeTrail.Capacity * WakeTrail.PackedStride];
            w.CopyTo(packed);
            // QA-V6b-02: el lugar 0 es la cabeza viva y el 1 el único punto guardado; desde el 2, vacíos.
            Assert.That(packed[4 + 1], Is.EqualTo(1f), "el punto guardado");
            Assert.That(packed[8 + 1], Is.EqualTo(0f), "los lugares vacíos van sin intensidad");
            Assert.Throws<ArgumentException>(() => w.CopyTo(new float[10]));
        }

        [Test, Category("AC-6B-14")]
        public void TheHeadIsTheDrawnSternEveryFrame()
        {
            // QA-V6b-02: con la historia real, el punto más nuevo se emite cada 0,25 s y queda hasta ≈ 1 m detrás de la popa; la cinta
            // tiene que arrancar en la popa que se dibuja (con el surge) en TODOS los cuadros, no en el último punto emitido.
            var w = new WakeTrail();
            var now = 0.0;
            var d = 0.0;
            var packed = new float[WakeTrail.Capacity * WakeTrail.PackedStride];
            for (var i = 0; i < 15 * 60; i++)
            {
                now += 1.0 / 60;
                d += 4.0 / 60;
                var surge = 0.08 * Math.Sin(now * 2.5);
                w.Step(now, d, 4.0, surge);
                w.CopyTo(packed);
                Assert.That(packed[0], Is.EqualTo((float)(WaterModel.SternRelZ + surge)).Within(1e-5f), "la cabeza en la popa que se dibuja (cuadro " + i + ")");
                Assert.That(packed[2], Is.EqualTo(0f), "edad 0");
                Assert.That(packed[1], Is.EqualTo(1f), "la intensidad de ahora");
                Assert.That(packed[4], Is.LessThanOrEqualTo(packed[0] + 1e-5f), "el punto guardado más nuevo, detrás de la cabeza");
                Assert.That(packed[0] - packed[4], Is.LessThanOrEqualTo(4.0f * (float)WakeTrail.Interval + 0.2f), "sin hueco: a lo sumo un intervalo");
            }
        }

        [Test, Category("AC-6B-14")]
        public void TheHeadFadesInFromTheStern()
        {
            // QA-V6b-02: sin borde recto: el alfa nace en 0 en la popa y llega a pleno a 1,2 m detrás, de forma monótona.
            Assert.That(WakeTrail.ShapedAlpha(1, 0, 0), Is.EqualTo(0).Within(1e-12));
            Assert.That(WakeTrail.ShapedAlpha(1, 0, -0.05), Is.EqualTo(0).Within(1e-12), "el surge hacia adelante");
            Assert.That(WakeTrail.ShapedAlpha(1, 0.3, 1.2), Is.EqualTo(WakeTrail.Alpha(1, 0.3)).Within(1e-12));
            var last = -1.0;
            for (var b = 0.0; b <= 1.2; b += 0.05)
            {
                var r = WakeTrail.HeadRamp(b);
                Assert.That(r, Is.GreaterThanOrEqualTo(last));
                last = r;
            }

            Assert.That(WakeTrail.HeadRamp(0.3), Is.LessThan(0.25), "a 0,3 m de la popa, menos de un cuarto");
        }
    }
}
