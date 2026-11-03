using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SSM: los parciales con el remo simulado (05-series-parciales/02-tech-design §6.3; AC-BLE-15-09): Series 600 con el simulador
    // sin ruido a 150 W y "Simular desconexión" en un trabajo: durante el corte la vuelta en curso no suma metros y su tiempo sí;
    // al reconectar sigue desde los mismos metros (la física del simulador no se reinicia: no avanza sin lecturas); ninguna
    // vuelta inventada; nada negativo. La rutina se arma acá (los pasos de Series 600 de la spec §0.4).
    [TestFixture]
    public sealed class SeriesSimulatorTests
    {
        [Test, Category("AC-BLE-15-09")]
        public void ADisconnectDuringAWorkKeepsTheLapMeters()
        {
            var scheduler = new FakeScheduler();
            var logger = new RecordingLogger();
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false, random: new SequenceRandom());
            var sessions = new InMemorySessionRepository();
            var lifecycle = new FakeAppLifecycle(logger);
            using (var engine = new WorkoutEngine(backend.Rower, backend.HeartRate, sessions, scheduler, lifecycle, logger))
            {
                backend.Rower.Start();
                scheduler.Advance(TimeSpan.FromSeconds(1.5));
                Assert.That(engine.StartWithRoutine(SeriesSeiscientos()), Is.EqualTo(StartOutcome.Started));
                var seconds = new List<(int Second, int Meters, double Seconds, int Laps)>();
                using (engine.State.Subscribe(s =>
                       {
                           var series = s.Series;
                           if (series != null && series.StepIndex.HasValue && (seconds.Count == 0 || seconds[seconds.Count - 1].Second != s.TotalElapsedSeconds))
                           {
                               seconds.Add((s.TotalElapsedSeconds, series.CurrentLapMeters, series.CurrentLapSeconds, series.Laps.Count));
                           }
                       }, false))
                {
                    scheduler.Advance(TimeSpan.FromSeconds(60));
                    var before = engine.Series.State;
                    Assert.That(before.IsWorkStep, Is.True, "en el trabajo de la rep 1 (desde los 31 s)");
                    Assert.That(before.CurrentLapMeters, Is.GreaterThan(0));
                    var cutAt = seconds.Count;
                    backend.SimRower.SimulateDisconnect();
                    scheduler.Advance(TimeSpan.FromSeconds(3.5));
                    var during = seconds.GetRange(cutAt, seconds.Count - cutAt);
                    Assert.That(during.Count, Is.GreaterThanOrEqualTo(2), "el reloj sigue durante el corte");
                    foreach (var s in during)
                    {
                        Assert.That(s.Meters, Is.EqualTo(before.CurrentLapMeters), "sin lecturas, la vuelta en curso no suma metros");
                    }

                    Assert.That(during[during.Count - 1].Seconds, Is.GreaterThan(before.CurrentLapSeconds), "y su tiempo sí");
                    var reconnected = seconds.Count;
                    scheduler.Advance(TimeSpan.FromSeconds(5));
                    var after = seconds.GetRange(reconnected, seconds.Count - reconnected);
                    Assert.That(after[after.Count - 1].Meters, Is.GreaterThan(before.CurrentLapMeters), "sigue sumando después");
                    Assert.That(after[after.Count - 1].Meters - before.CurrentLapMeters, Is.LessThanOrEqualTo(4 * 5),
                        "desde los mismos metros: ningún salto");
                    scheduler.Advance(TimeSpan.FromSeconds(200));
                }

                var previous = -1;
                foreach (var s in seconds)
                {
                    Assert.That(s.Meters, Is.GreaterThanOrEqualTo(0), "nada negativo");
                    Assert.That(s.Seconds, Is.GreaterThanOrEqualTo(0));
                    Assert.That(s.Laps, Is.LessThanOrEqualTo(1), "a ≈ 3,77 m/s, en un trabajo de 600 m cabe una sola vuelta");
                    previous = s.Second;
                }

                Assert.That(previous, Is.GreaterThan(200));
                var rep = engine.Series.State.PreviousReps;
                Assert.That(rep.Count, Is.GreaterThanOrEqualTo(1), "la rep 1 terminó");
                Assert.That(rep[rep.Count - 1].WorkMeters, Is.InRange(599, 610), "600 m de trabajo, ninguno inventado");
                Assert.That(rep[rep.Count - 1].WorkSplitSeconds, Is.InRange(133.0, 140.0), "el split con los segundos del corte (2:13 sin él)");
            }

            backend.Dispose();
        }

        private static Routine SeriesSeiscientos()
        {
            const string g = "g-series-600";
            var steps = new[]
            {
                new IntervalStep(0, 0, StepType.Warmup) { DurationSeconds = 30 },
                new IntervalStep(0, 1, StepType.Work) { DistanceMeters = 600, GroupId = g, GroupRepeatCount = 3 },
                new IntervalStep(0, 2, StepType.Rest) { DurationSeconds = 20, GroupId = g, GroupRepeatCount = 3 },
                new IntervalStep(0, 3, StepType.Cooldown) { DurationSeconds = 30 },
            };
            return new Routine("Series 600", new DateTimeOffset(2026, 10, 29, 13, 8, 0, TimeSpan.Zero)) { Steps = steps };
        }
    }
}
