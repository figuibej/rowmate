using NUnit.Framework;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Workout
{
    // SDI: SanitizedDistance = el saneamiento de los parciales (Q-55, DV-26; lib/features/workout/series_tracker.dart:43-46,
    // :86-108, :118-127, :143). Los cuatro casos del grupo _hardening de test/series_tracker_test.dart:155-220 se portan acá
    // en sus afirmaciones de METROS (QA-G4a-P06): las de vueltas y repeticiones son de la It. 5, que les da su LT. Parciales:
    // completan en la It. 5. El alimentador es el _Raw del legacy (:133-152): una lectura por segundo activo, t = 1, 2, 3…
    [TestFixture]
    public sealed class SanitizedDistanceTests
    {
        [Test, Category("Q-55"), Category("AC-WKT-03-03")]
        public void NegativeAndTooFastJumpsDoNotCountAndBecomeTheReference()
        {
            var meter = new SanitizedDistance();
            meter.Begin(1000, 0);
            meter.Update(1004, 1);
            Assert.That(meter.Meters, Is.EqualTo(4));
            meter.Update(1000, 2);
            Assert.That(meter.Meters, Is.EqualTo(4), "negativo: no suma");
            meter.Update(1010, 3);
            Assert.That(meter.Meters, Is.EqualTo(14), "la lectura de 1000 fue la referencia nueva: +10 entra");
            meter.Update(1021, 4);
            Assert.That(meter.Meters, Is.EqualTo(14), "11 m en 1 s: más de 10 m/s, no suma");
            meter.Update(1040, 6);
            Assert.That(meter.Meters, Is.EqualTo(33), "19 m en 2 s: entra (10 · max(dt; 1))");
            meter.Update(1040, 6);
            Assert.That(meter.Meters, Is.EqualTo(33), "dt 0: el tope es de 10 m");
            meter.Update(1050, 6);
            Assert.That(meter.Meters, Is.EqualTo(43), "con dt < 1 el tope es 10");
            Assert.That(SanitizedDistance.MaxSpeedMetersPerSecond, Is.EqualTo(10.0));
        }

        [Test, Category("Q-55"), Category("AC-WKT-01-05")]
        public void RebaseIgnoresWhatWasRowedInPause()
        {
            var meter = new SanitizedDistance();
            meter.Begin(1000, 0);
            for (var t = 1; t <= 10; t++)
            {
                meter.Update(1000 + 4 * t, t);
            }

            Assert.That(meter.Meters, Is.EqualTo(40));
            meter.Rebase(1120);
            meter.Update(1124, 11);
            Assert.That(meter.Meters, Is.EqualTo(44), "lo remado en pausa (80 m) no cuenta");
        }

        [Test, Category("Q-55")]
        public void PacketWithoutDistanceLosesTwoReadings()
        {
            // DV-26: un paquete aislado sin distancia hace perder los metros de esa lectura y la siguiente (R-4Z).
            var meter = new SanitizedDistance();
            meter.Begin(1000, 0);
            for (var t = 1; t <= 12; t++)
            {
                meter.Update(t == 10 ? 0 : 1000 + 4 * t, t);
            }

            Assert.That(meter.Meters, Is.EqualTo(40), "36 hasta el 9, + 0 (el 0), + 0 (el salto de vuelta), + 4");
        }

        // legacy: test/series_tracker_test.dart:155 'una lectura de 0 dentro del paso no genera metros negativos ni dobles'
        [Test, Category("Q-55")]
        public void ZeroReadingInsideTheStep()
        {
            var r = new Raw(0);
            for (var i = 1; i <= 250; i++)
            {
                r.Feed(i * 4);
                Assert.That(r.Meter.Meters, Is.GreaterThanOrEqualTo(0));
            }

            r.Feed(0);
            Assert.That(r.Meter.Meters, Is.GreaterThanOrEqualTo(0));
            for (var raw = 1004; raw <= 1100; raw += 4)
            {
                r.Feed(raw);
                Assert.That(r.Meter.Meters, Is.GreaterThanOrEqualTo(0));
            }

            // Ni negativos ni dobles: dentro del rango de metros de trabajo del legacy (1060 a 1110).
            Assert.That(r.Meter.Meters, Is.EqualTo(1096));
            Assert.That(r.Meter.Meters, Is.InRange(1060, 1110));
        }

        // legacy: test/series_tracker_test.dart:180 'un primer salto desde 0 no crea vueltas falsas'
        [Test, Category("Q-55")]
        public void FirstJumpFromZero()
        {
            var r = new Raw(0);
            r.Feed(5000);
            Assert.That(r.Meter.Meters, Is.EqualTo(0), "el salto desde 0 no suma");
            for (var i = 1; i <= 140; i++)
            {
                r.Feed(5000 + i * 4);
            }

            Assert.That(r.Meter.Meters, Is.EqualTo(560));
        }

        // legacy: test/series_tracker_test.dart:191 'un reinicio del monitor no genera valores negativos'
        [Test, Category("Q-55")]
        public void MonitorResetNeverGoesNegative()
        {
            var r = new Raw(0);
            var previous = 0.0;
            for (var i = 1; i <= 200; i++)
            {
                r.Feed(i * 4);
                Assert.That(r.Meter.Meters, Is.GreaterThanOrEqualTo(previous));
                previous = r.Meter.Meters;
            }

            for (var i = 0; i <= 60; i++)
            {
                r.Feed(i * 4);
                Assert.That(r.Meter.Meters, Is.GreaterThanOrEqualTo(previous), "nunca baja");
                previous = r.Meter.Meters;
            }

            Assert.That(r.Meter.Meters, Is.EqualTo(1040));
        }

        // legacy: test/series_tracker_test.dart:211 'rebase no cuenta el salto al reanudar'
        [Test, Category("Q-55")]
        public void RebaseIgnoresTheJumpOnResume()
        {
            var r = new Raw(0);
            for (var i = 1; i <= 5; i++)
            {
                r.Feed(i * 4);
            }

            Assert.That(r.Meter.Meters, Is.EqualTo(20));
            r.Meter.Rebase(9000);
            r.Feed(9004);
            Assert.That(r.Meter.Meters, Is.EqualTo(24));
        }

        // El _Raw del legacy (test/series_tracker_test.dart:133-152): begin(startRaw) y una lectura por segundo.
        private sealed class Raw
        {
            private int _t;

            public Raw(int startRaw)
            {
                Meter.Begin(startRaw, 0);
            }

            public SanitizedDistance Meter { get; } = new SanitizedDistance();

            public void Feed(int raw)
            {
                _t++;
                Meter.Update(raw, _t);
            }
        }
    }
}
