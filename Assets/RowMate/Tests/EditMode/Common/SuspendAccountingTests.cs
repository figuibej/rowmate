using System;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // SUS: el reloj despierto (P-17 de la 4a; AC-WKT-02-11, -04; 02-tech-design §2.9 de la It. 4b; ADR 020): SuspendAccounting
    // resta del monótono crudo lo dormido que el crudo contó y avisa el despertar, con la medida del sistema, con el aviso de
    // "se suspende" / "reanuda" o con los dos. Sin evidencia, un cuadro tarde es un cuadro tarde (la 4a).
    [TestFixture]
    public sealed class SuspendAccountingTests
    {
        private const double Frame = 1.0 / 60;
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

        [Test, Category("AC-WKT-02-11")]
        public void MeasureWithARawThatCounts()
        {
            // Con medida (ΔAsleep ≥ 0,5) y un crudo que contó (ΔRaw ≥ 0,9·ΔAsleep): se restan min(ΔAsleep; ΔRaw).
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 10);
            clock.Sleep(600, rawCounts: true, measured: true);
            var awake = accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke.HasValue, Is.True);
            Assert.That(woke!.Value.Evidence, Is.EqualTo(SleepEvidence.Measure));
            Assert.That(woke.Value.AsleepSeconds, Is.EqualTo(600).Within(1e-9));
            Assert.That(woke.Value.DiscountedSeconds, Is.EqualTo(600).Within(1e-9));
            Assert.That(accounting.Bias, Is.EqualTo(600).Within(1e-9));
            Assert.That(awake, Is.EqualTo(10 + Frame).Within(1e-6), "el lapso dormido no existe");
        }

        [Test, Category("AC-WKT-02-11")]
        public void MeasureWithARawThatDoesNotCount()
        {
            // El crudo no contó (QA-V4a-ROB-05: no está medido qué hace el de Windows): nada que restar, igual es un despertar.
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 10);
            clock.Sleep(600, rawCounts: false, measured: true);
            var awake = accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke!.Value.Evidence, Is.EqualTo(SleepEvidence.Measure));
            Assert.That(woke.Value.DiscountedSeconds, Is.EqualTo(0));
            Assert.That(accounting.Bias, Is.EqualTo(0));
            Assert.That(awake, Is.EqualTo(10 + Frame).Within(1e-6));
        }

        [Test, Category("AC-WKT-02-11")]
        public void SignalAndGap()
        {
            // Sin medida, con un "se suspende" pendiente: un cuadro de ≥ 1 s es sueño (ΔRaw menos un cuadro nominal), con Signal.
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 10);
            clock.Suspend();
            clock.Run(accounting, 1);
            Assert.That(accounting.SuspendPending, Is.True);
            clock.Sleep(600, rawCounts: true, measured: false);
            clock.Resume();
            var awake = accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke!.Value.Evidence, Is.EqualTo(SleepEvidence.Signal));
            Assert.That(woke.Value.DiscountedSeconds, Is.EqualTo(600).Within(1e-9));
            Assert.That(awake, Is.EqualTo(11 + Frame).Within(1e-6));
            Assert.That(accounting.SuspendPending, Is.False, "el reanuda lo cerró");
        }

        [Test, Category("AC-WKT-02-11")]
        public void SignalAndWallJump()
        {
            // Con el aviso y un crudo que no contó: ΔRaw normal y la hora de pared salta; nada que restar, Signal | WallJump.
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 10);
            clock.Suspend();
            clock.Run(accounting, 1);
            clock.Sleep(600, rawCounts: false, measured: false);
            accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke!.Value.Evidence, Is.EqualTo(SleepEvidence.Signal | SleepEvidence.WallJump));
            Assert.That(woke.Value.DiscountedSeconds, Is.EqualTo(0));
            Assert.That(woke.Value.AsleepSeconds, Is.EqualTo(600).Within(0.01));
        }

        [Test, Category("AC-WKT-02-11")]
        public void ResumeBeforeOrAfterTheFirstAwakeFrame()
        {
            // QA-G4bT-05 (b): el "reanuda" ancla el despertar; da igual si llega antes o después del primer cuadro despierto.
            foreach (var before in new[] { true, false })
            {
                var clock = new Clock();
                var accounting = new SuspendAccounting(clock.Now());
                clock.Run(accounting, 10);
                clock.Suspend();
                clock.Run(accounting, 1);
                clock.Sleep(600, rawCounts: true, measured: false);
                if (before)
                {
                    clock.Resume();
                }

                var awake = accounting.Awake(clock.Now(), out var woke);
                Assert.That(woke.HasValue, Is.True, before ? "antes" : "después");
                Assert.That(awake, Is.EqualTo(11 + Frame).Within(1e-6));
                if (!before)
                {
                    clock.Resume();
                    clock.Tick();
                    accounting.Awake(clock.Now(), out var second);
                    Assert.That(second.HasValue, Is.False, "el reanuda después no avisa otro despertar");
                }

                Assert.That(accounting.SuspendPending, Is.False);
                Assert.That(accounting.Bias, Is.EqualTo(600).Within(1e-6));
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void ResumeWithoutAnyGapIsTheWake()
        {
            // Si ningún cuadro mostró el sueño (el crudo no contó, sin salto de pared visible), el reanuda es el despertar.
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 10);
            clock.Suspend();
            clock.Run(accounting, 1);
            clock.Resume();
            clock.Tick();
            accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke!.Value.Evidence, Is.EqualTo(SleepEvidence.Signal));
            Assert.That(woke.Value.DiscountedSeconds, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-02-11")]
        public void StallBetweenTheSignalAndTheSleep()
        {
            // QA-G4bT-05 (b): un tirón de 1,2 s entre el aviso y el sueño de verdad se descuenta (a lo sumo lo que duró, y queda en
            // el registro) y el sueño de después también: el aviso no se consume con el tirón.
            var logger = new RecordingLogger();
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now(), logger);
            clock.Run(accounting, 10);
            clock.Suspend();
            clock.Run(accounting, 1);
            clock.Stall(1.2);
            accounting.Awake(clock.Now(), out var stall);
            Assert.That(stall!.Value.DiscountedSeconds, Is.EqualTo(1.2 - Frame).Within(1e-9));
            Assert.That(accounting.SuspendPending, Is.True, "sigue pendiente");
            Assert.That(logger.Contains("con aviso de suspensión: se descuentan"), Is.True);
            clock.Sleep(600, rawCounts: true, measured: false);
            clock.Resume();
            accounting.Awake(clock.Now(), out var sleep);
            Assert.That(sleep!.Value.DiscountedSeconds, Is.EqualTo(600).Within(1e-9));
            Assert.That(accounting.Bias, Is.EqualTo(601.2 - Frame).Within(1e-6));
        }

        [Test, Category("AC-WKT-02-11")]
        public void SignalThatExpires()
        {
            // Un "se suspende" sin sueño ni "reanuda" en 30 s despiertos (cancelado) se descarta y se anota.
            var logger = new RecordingLogger();
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now(), logger);
            clock.Suspend();
            clock.Run(accounting, 31);
            Assert.That(accounting.SuspendPending, Is.False);
            Assert.That(logger.Contains("se descarta"), Is.True);
            clock.Stall(5);
            accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke.HasValue, Is.False, "un tirón después ya no es sueño");
            Assert.That(accounting.Bias, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-02-04")]
        public void NoiseBelowTheThreshold()
        {
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 2);
            clock.Sleep(0.4, rawCounts: true, measured: true);
            accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke.HasValue, Is.False, "menos de 0,5 s es ruido");
            Assert.That(accounting.Bias, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-02-04")]
        public void StallWithoutEvidenceCounts()
        {
            // Sin medida ni aviso un cuadro tarde es un cuadro tarde (AC-WKT-02-04, la conducta de la 4a), y desde 30 s se anota.
            var logger = new RecordingLogger();
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now(), logger);
            clock.Run(accounting, 2);
            clock.Stall(3600);
            var awake = accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke.HasValue, Is.False);
            Assert.That(awake, Is.EqualTo(2 + 3600).Within(1e-6), "cuenta");
            Assert.That(logger.Contains("sin aviso de suspensión: cuenta (AC-WKT-02-04)"), Is.True);
        }

        [Test, Category("AC-WKT-02-04")]
        public void WallJumpWithoutSignalIsAClockChange()
        {
            // Un salto de la hora de pared sin aviso es un cambio de hora (AC-WKT-19-04), no una suspensión.
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            clock.Run(accounting, 2);
            clock.ShiftWall(3 * 3600);
            clock.Tick();
            accounting.Awake(clock.Now(), out var woke);
            Assert.That(woke.HasValue, Is.False);
            Assert.That(accounting.Bias, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-02-11")]
        public void AwakeNeverGoesDown()
        {
            var clock = new Clock();
            var accounting = new SuspendAccounting(clock.Now());
            var last = accounting.Current;
            for (var i = 0; i < 200; i++)
            {
                if (i == 50)
                {
                    clock.Sleep(100, rawCounts: true, measured: true);
                }

                if (i == 120)
                {
                    clock.Back(5);
                }

                clock.Tick();
                var awake = accounting.Awake(clock.Now(), out _);
                Assert.That(awake, Is.GreaterThanOrEqualTo(last));
                last = awake;
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void ResumeFromBackgroundUsesTheSameAwake()
        {
            // QA-G4bT-05 (a): la vuelta del segundo plano arma el FrameClock como un cuadro y el alcance va hasta el monótono
            // despierto: el lapso dormido no se procesa.
            var logger = new RecordingLogger();
            var scheduler = new FrameScheduler(0, Start, TimeSpan.FromHours(-3), logger);
            var fired = 0;
            scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), () => fired++);
            SystemWake? woke = null;
            scheduler.Woke += w => woke = w;
            scheduler.Tick(new FrameClock(10, Start.AddSeconds(10), TimeSpan.FromHours(-3), 0));
            fired = 0;
            scheduler.EnterBackground();
            scheduler.ResumeFromBackground(new FrameClock(630, Start.AddSeconds(630), TimeSpan.FromHours(-3), 600));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(30).Within(1e-9), "despierto: 10 + 20");
            Assert.That(fired, Is.EqualTo(20), "solo los 20 s despiertos en segundo plano");
            Assert.That(woke.HasValue, Is.True, "Woke después del alcance");
            Assert.That(scheduler.SleepBias, Is.EqualTo(600).Within(1e-9));
        }

        [Test, Category("AC-WKT-02-04"), Category("AC-WKT-02-11")]
        public void BackgroundReturnIsNotLoggedAsALateFrame()
        {
            // QA-V4b-MOT-04: sin medida ni aviso (Android e iOS, NullSleepSource) la vuelta del segundo plano cuenta (Q-91), como
            // siempre, pero no queda en el registro como un "cuadro tarde": esa línea es la que dice qué faltó si la demo (c) falla.
            var logger = new RecordingLogger();
            var scheduler = new FrameScheduler(0, Start, TimeSpan.FromHours(-3), logger);
            scheduler.Tick(FrameClock.Plain(10, Start.AddSeconds(10), TimeSpan.FromHours(-3)));
            scheduler.EnterBackground();
            scheduler.ResumeFromBackground(FrameClock.Plain(610, Start.AddSeconds(610), TimeSpan.FromHours(-3)));
            Assert.That(scheduler.MonotonicSeconds, Is.EqualTo(610).Within(1e-9), "el segundo plano cuenta (Q-91)");
            Assert.That(logger.Contains("cuadro de"), Is.False, "no es un cuadro tarde");
            Assert.That(logger.Contains("vuelta del segundo plano de 600.0 s sin aviso de suspensión: cuenta (Q-91)"), Is.True, "lo dice como es");

            // Un cuadro tarde de verdad (sin segundo plano) se sigue anotando como antes.
            scheduler.Tick(FrameClock.Plain(650, Start.AddSeconds(650), TimeSpan.FromHours(-3)));
            Assert.That(logger.Contains("cuadro de 40.0 s sin aviso de suspensión: cuenta (AC-WKT-02-04)"), Is.True);
        }

        // El reloj crudo y la hora de pared de la prueba, con la medida del sueño y los contadores de avisos.
        private sealed class Clock
        {
            private double _raw;
            private double _wall;
            private double _asleep;
            private int _suspends;
            private int _resumes;

            public FrameClock Now()
            {
                return new FrameClock(_raw, Start.AddSeconds(_wall), TimeSpan.FromHours(-3), _asleep, _suspends, _resumes);
            }

            public void Tick()
            {
                _raw += Frame;
                _wall += Frame;
            }

            public void Run(SuspendAccounting accounting, double seconds)
            {
                var frames = (int)Math.Round(seconds * 60);
                for (var i = 0; i < frames; i++)
                {
                    Tick();
                    accounting.Awake(Now(), out _);
                }
            }

            public void Sleep(double seconds, bool rawCounts, bool measured)
            {
                if (rawCounts)
                {
                    _raw += seconds;
                }

                _wall += seconds;
                if (measured)
                {
                    _asleep += seconds;
                }

                Tick();
            }

            public void Stall(double seconds)
            {
                _raw += seconds;
                _wall += seconds;
            }

            public void Suspend()
            {
                _suspends++;
            }

            public void Resume()
            {
                _resumes++;
            }

            public void ShiftWall(double seconds)
            {
                _wall += seconds;
            }

            public void Back(double seconds)
            {
                _raw -= seconds;
            }
        }
    }
}
