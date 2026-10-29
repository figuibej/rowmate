using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // TEL: la telemetría y el guardado de la sesión desde el motor (F-WKT-06 + F-HIS-01 a -03; 02-tech-design §2.8, §2.9 de
    // la It. 4a; lib/features/workout/workout_provider.dart:133-173, :252-296), con R-4 y la base en memoria.
    [TestFixture]
    public sealed class TelemetryTests
    {
        [Test, Category("AC-WKT-06-01")]
        public void StartCreatesTheRow()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                var session = h.Session()!;
                Assert.That(session.Id, Is.EqualTo(1));
                Assert.That(session.StartedAt, Is.EqualTo(WorkoutHarness.SessionStart));
                Assert.That(session.FinishedAt, Is.Null);
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(0));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(0));
                Assert.That(session.AvgPowerWatts, Is.EqualTo(0));
                Assert.That(session.StravaActivityId, Is.Null);
                h.Engine.FinishAsync();
                Assert.That(h.Session()!.FinishedAt.HasValue, Is.True, "la sesión existe aunque no se reme");
            }
        }

        [Test, Category("AC-WKT-06-02")]
        public void PointEveryFiveActiveSeconds()
        {
            // La tabla de la spec (libre con R-4): segundo 5 → (305, 24.0, 105, 1020, 125, 150, 40, 0); 10 → (310, …, 1040, …, 41).
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(4.9);
                Assert.That(h.Points(), Is.Empty);
                h.At(10);
                var points = h.Points();
                Assert.That(points.Count, Is.EqualTo(2));
                AssertPoint(points[0], 305, 24.0, 105, 1020, 125, 150, 40, 0);
                AssertPoint(points[1], 310, 24.0, 110, 1040, 125, 150, 41, 0);
                Assert.That(points[0].StepIndex, Is.Null, "libre: vacíos");
                Assert.That(points[0].StepTypeName, Is.Null);
                Assert.That(points[0].SessionId, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-06-03")]
        public void PointBelongsToTheStepThatEnds()
        {
            // Corta con R-4: 7 puntos (5, 10, 15, 20, 25 y 30, más el final) con índices 0, 0, 1, 2, 2, 3, 3.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(31);
                Assert.That(WorkoutHarness.Describe(h.Points()), Is.EqualTo(
                    "305/1020/40/0/work 310/1040/41/0/work 315/1060/41/1/rest 320/1080/42/2/work 325/1100/42/2/work 330/1120/43/3/rest 330/1120/43/3/rest"));
            }
        }

        [Test, Category("AC-WKT-06-04")]
        public void NoSamplingInPause()
        {
            // 7 s en marcha, 30 s de pausa y 5 s más: los puntos caen en los segundos en marcha 5 y 10, ninguno en la pausa.
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(7);
                h.Engine.Pause();
                h.At(37);
                Assert.That(h.Points().Count, Is.EqualTo(1));
                h.Engine.Resume();
                h.At(42.5);
                h.Engine.FinishAsync();
                Assert.That(WorkoutHarness.Describe(h.Points()), Is.EqualTo("305/1020/40/-/- 340/1160/44/-/- 343/1172/44/-/-"),
                    "el Anexo A: (305), (340), (343)");
            }
        }

        [Test, Category("AC-WKT-06-06")]
        public void FinishWritesTheFinalPointAndTotals()
        {
            // Libre con R-4, terminada a los 12,3 s: 3 puntos (305, 310, 312) y la sesión con 1048 m, 312 s, 150 W, 24.0 y 41 kcal.
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(12.3);
                h.Engine.FinishAsync();
                Assert.That(WorkoutHarness.Describe(h.Points()), Is.EqualTo("305/1020/40/-/- 310/1040/41/-/- 312/1048/41/-/-"));
                var session = h.Session()!;
                Assert.That(session.FinishedAt, Is.EqualTo(new DateTimeOffset(2026, 10, 28, 13, 0, 12, 300, TimeSpan.Zero)));
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(1048));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(312));
                Assert.That(session.AvgPowerWatts, Is.EqualTo(150));
                Assert.That(session.AvgStrokeRate, Is.EqualTo(24.0));
                Assert.That(session.TotalCalories, Is.EqualTo(41));
                Assert.That(session.StravaActivityId, Is.Null);
                Assert.That(h.Memory!.Operations, Is.EqualTo(new[] { "create", "points:1", "points:1", "finish:1" }),
                    "cada punto en el acto; el final y el cierre en una transacción");
            }
        }

        [Test, Category("AC-WKT-06-07")]
        public void LastTwoPointsEqualWhenEndingOnAFive()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(31);
                var points = h.Points();
                Assert.That(points.Count, Is.EqualTo(7));
                Assert.That(points[6] with { Id = null }, Is.EqualTo(points[5] with { Id = null }), "el punto final repite al del segundo 30");
                var session = h.Session()!;
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(1120));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(330));
            }
        }

        [Test, Category("AC-WKT-06-08")]
        public void MonitorCountersAreSaved()
        {
            // Q-15: los contadores del monitor, no lo remado desde el inicio; sin tiempo del monitor (0), la sesión guarda 0.
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(12);
                h.Engine.FinishAsync();
                Assert.That(h.Session()!.TotalDistanceMeters, Is.EqualTo(1048));
                Assert.That(h.Session()!.TotalTimeSeconds, Is.EqualTo(312));
            }

            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(elapsedSeconds: 0)))
            {
                h.Free();
                h.At(12);
                h.Engine.FinishAsync();
                Assert.That(h.Session()!.TotalTimeSeconds, Is.EqualTo(0), "aunque el HUD mostró un reloj");
            }
        }

        [Test, Category("AC-WKT-06-09")]
        public void ShortAndUnrowedSessions()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(3);
                h.Engine.FinishAsync();
                Assert.That(WorkoutHarness.Describe(h.Points()), Is.EqualTo("303/1012/40/-/-"), "menos de 5 s: un solo punto, el final");
            }

            using (var h = new WorkoutHarness(RowerScripts.R4P))
            {
                h.Free();
                h.At(20);
                h.Engine.FinishAsync();
                var session = h.Session()!;
                Assert.That(session.FinishedAt.HasValue, Is.True, "sin remar existe igual");
                Assert.That(h.Points().Count, Is.EqualTo(5));
                Assert.That(session.AvgPowerWatts, Is.EqualTo(0));
                Assert.That(session.AvgStrokeRate, Is.EqualTo(0));
            }
        }

        [Test, Category("AC-WKT-05-05")]
        public void PointsCarryTheFusedPulse()
        {
            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(heartRate: 88)))
            {
                h.Free();
                h.At(5);
                h.HeartRate.EmitBpm(147);
                h.At(10);
                h.HeartRate.EmitBpm(150);
                h.HeartRate.SetStatus(RowMate.Core.Ble.HrmStatus.Disconnected);
                h.At(14.7);
                h.Engine.FinishAsync();
                var points = h.Points();
                Assert.That(points[0].HeartRate, Is.EqualTo(88), "sin sensor, el del monitor");
                Assert.That(points[1].HeartRate, Is.EqualTo(147), "el sensor manda");
                Assert.That(points[2].HeartRate, Is.EqualTo(88), "el sensor se fue: la lectura siguiente del remo repone el suyo");
            }
        }

        [Test, Category("AC-WKT-06-02")]
        public void TelemetryPointCopiesTheReading()
        {
            var reading = RowerScripts.R4(7).CopyWith(heartRate: 133);
            var free = TelemetryPoint.From(9, reading, null, null);
            Assert.That(free.SessionId, Is.EqualTo(9));
            Assert.That(free.ElapsedSeconds, Is.EqualTo(307), "el tiempo del monitor (Q-15, D-12)");
            Assert.That(free.Calories, Is.EqualTo(40), "las kcal totales");
            Assert.That(free.HeartRate, Is.EqualTo(133));
            Assert.That(free.StepIndex, Is.Null);
            var step = new IntervalStep(1, 0, StepType.Cooldown) { DurationSeconds = 30 };
            var routine = TelemetryPoint.From(9, reading, 5, step);
            Assert.That(routine.StepIndex, Is.EqualTo(5));
            Assert.That(routine.StepTypeName, Is.EqualTo("cooldown"));
            Assert.That(TelemetryPoint.From(9, reading, 6, null).StepTypeName, Is.Null, "índice fuera de rango: tipo vacío (:270-272)");
        }

        private static void AssertPoint(DataPoint p, int elapsed, double spm, int strokes, int distance, int pace, int watts, int kcal, int hr)
        {
            Assert.That(new object[] { p.ElapsedSeconds, p.StrokeRate, p.StrokeCount, p.DistanceMeters, p.Pace500mSeconds, p.PowerWatts, p.Calories, p.HeartRate },
                Is.EqualTo(new object[] { elapsed, spm, strokes, distance, pace, watts, kcal, hr }));
        }
    }
}
