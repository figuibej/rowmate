using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Tests.Support
{
    // Una sesión de prueba del TCX con sus puntos (en orden, por el tiempo del monitor).
    public sealed class TcxSeed
    {
        public TcxSeed(string name, WorkoutSession session, IReadOnlyList<DataPoint> points)
        {
            Name = name;
            Session = session;
            Points = points;
        }

        public string Name { get; }

        public WorkoutSession Session { get; }

        public IReadOnlyList<DataPoint> Points { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    // Las sesiones de prueba del TCX (12-exportar-tcx/01-po-spec §6; 02-tech-design §6.2). Todos los datos son inventados.
    // - T-1: rutina con los 5 caracteres de XML, terminada, 600 s y 100 kcal; 6 puntos con 3 vueltas (el índice 0 vuelve). Es la
    //   del golden (Tests/Golden/tcx/T-1.tcx, en UTC−03:00).
    // - T-2: libre, un punto en 0 s con 10 m (los desfases). T-2M: libre, en el fin del horario de verano de Madrid.
    // - T-3: Corta, sin fin y totales 0: la base de las calorías sale de los puntos (1, 2 y 3 kcal → 15 s y 3 kcal).
    // - T-5: libre, un punto en 0 s con 0 m (no se exporta). T-6: libre, un punto en 0 s con 10 m y totales 0 (una vuelta en 0).
    // - ThreeHours: 2 160 puntos cada 5 s con todos los campos y un paso nuevo cada 10 min (AC-STR-09-12).
    public static class TcxSeeds
    {
        public const string T1RoutineName = "Escalera & <Co> \"A\" 'B'";

        public static TcxSeed T1
        {
            get
            {
                var start = new DateTimeOffset(2026, 11, 5, 22, 2, 0, 750, TimeSpan.Zero);
                var session = new WorkoutSession(start)
                {
                    Id = 1, RoutineId = 1, RoutineName = T1RoutineName, FinishedAt = start.AddSeconds(600), TotalTimeSeconds = 600,
                    TotalCalories = 100, TotalDistanceMeters = 121,
                };
                var points = new[]
                {
                    Point(1, 5, 20, 24.4, 150, 0, 0),
                    Point(1, 10, 41, 24.6, 0, 140, 0),
                    Point(1, 15, 62, 0, 155, 141, 0),
                    Point(1, 20, 80, 22.5, 160, 142, 1),
                    Point(1, 25, 100, 23.5, 0, 0, 1),
                    Point(1, 30, 121, 24.0, 150, 143, 0),
                };
                return new TcxSeed("T-1", session, points);
            }
        }

        public static TcxSeed T2 =>
            new TcxSeed("T-2", new WorkoutSession(new DateTimeOffset(2026, 11, 5, 22, 2, 0, TimeSpan.Zero)) { Id = 2 },
                new[] { new DataPoint(2, 0) { DistanceMeters = 10 } });

        public static TcxSeed T2M =>
            new TcxSeed("T-2M", new WorkoutSession(new DateTimeOffset(2026, 10, 25, 0, 59, 50, TimeSpan.Zero)) { Id = 22 },
                new[] { new DataPoint(22, 5) { DistanceMeters = 10 }, new DataPoint(22, 15) { DistanceMeters = 30 } });

        public static TcxSeed T3 =>
            new TcxSeed("T-3", new WorkoutSession(new DateTimeOffset(2026, 11, 5, 22, 2, 0, TimeSpan.Zero)) { Id = 3, RoutineId = 3, RoutineName = "Corta" },
                new[]
                {
                    new DataPoint(3, 5) { DistanceMeters = 20, Calories = 1, StepIndex = 0 },
                    new DataPoint(3, 10) { DistanceMeters = 41, Calories = 2, StepIndex = 0 },
                    new DataPoint(3, 15) { DistanceMeters = 62, Calories = 3, StepIndex = 0 },
                });

        public static TcxSeed T5 =>
            new TcxSeed("T-5", new WorkoutSession(new DateTimeOffset(2026, 11, 5, 22, 2, 0, TimeSpan.Zero)) { Id = 5 },
                new[] { new DataPoint(5, 0) });

        public static TcxSeed T6 =>
            new TcxSeed("T-6", new WorkoutSession(new DateTimeOffset(2026, 11, 5, 22, 2, 0, TimeSpan.Zero)) { Id = 6 },
                new[] { new DataPoint(6, 0) { DistanceMeters = 10 } });

        public static TcxSeed ThreeHours
        {
            get
            {
                var start = new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);
                const int count = 2160;
                var points = new DataPoint[count];
                double distance = 0;
                for (var i = 0; i < count; i++)
                {
                    var t = (i + 1) * 5;
                    var pace = 120 + (t * 7) % 23;
                    distance += 5 * 500.0 / pace;
                    points[i] = new DataPoint(7, t)
                    {
                        PowerWatts = 100 + (t * 37) % 81,
                        StrokeRate = 18 + ((t * 13) % 70) / 10.0,
                        StrokeCount = t / 3,
                        Pace500mSeconds = pace,
                        DistanceMeters = (int)DartMath.Round(distance),
                        Calories = t / 12,
                        HeartRate = 120 + (t * 11) % 41,
                        StepIndex = t / 600,
                        StepTypeName = (t / 600) % 2 == 0 ? "work" : "rest",
                    };
                }

                var session = new WorkoutSession(start)
                {
                    Id = 7, RoutineId = 7, RoutineName = "Tres horas", FinishedAt = start.AddSeconds(count * 5), TotalTimeSeconds = count * 5,
                    TotalCalories = count * 5 / 12, TotalDistanceMeters = points[count - 1].DistanceMeters,
                };
                return new TcxSeed("3 h", session, points);
            }
        }

        // Un punto de la tabla de §6: (s, m, spm, W, pulso, paso).
        public static DataPoint Point(long sessionId, int seconds, int meters, double spm, int watts, int heartRate, int? step)
        {
            return new DataPoint(sessionId, seconds)
            {
                DistanceMeters = meters, StrokeRate = spm, PowerWatts = watts, HeartRate = heartRate, StepIndex = step,
            };
        }
    }
}
