using System;
using System.Collections.Generic;
using System.IO;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Text;
using RowMate.Infrastructure.Database;

namespace RowMate.Tests.Support
{
    // Lo que dejó una siembra del historial: el id de la rutina Escalera (0 si el conjunto no la tiene) y el id de cada sesión
    // por su número de la spec (SessionIds[n − 1] = la n.º n de §6; en F-9P, de la más nueva a la más vieja).
    public sealed class HistorySeedResult
    {
        internal HistorySeedResult(string set, long routineId, long[] sessionIds)
        {
            Set = set;
            RoutineId = routineId;
            SessionIds = sessionIds;
        }

        public string Set { get; }

        public long RoutineId { get; }

        public IReadOnlyList<long> SessionIds { get; }

        public long Session(int number)
        {
            return SessionIds[number - 1];
        }
    }

    // Las bases de prueba del historial (It. 9; 01-po-spec §6; 02-tech-design DT9-15), armadas POR CÓDIGO con los repositorios
    // de la app (alta, lotes de puntos y cierre con el punto final repetido, como el legacy), nunca en las bases del usuario:
    // - F-9: la rutina Escalera y las 6 sesiones de §6 (zona UTC−03:00, puntos cada 5 s). Los números de la spec son el oráculo
    //   de HistorySeedsTests (con las erratas E-01 y E-02 del gate M-03: ceros en 425 y 430 s).
    // - F-9V: la base vacía. F-9P: 200 sesiones como la n.º 2, una por día (la más nueva el 04/11/2026).
    // - Stress60MinAt1Hz: una sesión libre de 60 min a 1 Hz con valores que varían (el presupuesto de la malla, A9-10).
    // Valores por tipo de paso (constantes): warmup y cooldown 100 W, 20.0 spm, split 150 y pulso 120 / 125; work 150 W, 24.1 spm,
    // split 133 y pulso 150; rest 60 W, 18.0 spm, split 180 y pulso 130. La distancia de cada punto es el redondeo de la exacta
    // acumulada al ritmo de su tramo (500 / split m por segundo).
    public static class HistorySeeds
    {
        public const string F9 = "F-9";
        public const string F9V = "F-9V";
        public const string F9P = "F-9P";

        public const string EscaleraName = "Escalera";
        public const string DeletedRoutineName = "Rutina borrada";

        // El id de la rutina de la n.º 6, que no existe (foreign_keys = OFF: la fila queda igual, AC-RUT-20-01).
        public const long DeletedRoutineId = 9999;

        public const int PerformanceSessions = 200;

        private const int Interval = 5;

        // UTC−03:00 fija (sin horario de verano), la zona de las pruebas (spec §0).
        public static readonly TimeZoneInfo Zone =
            TimeZoneInfo.CreateCustomTimeZone("RowMate-UTC-3-History", TimeSpan.FromHours(-3), "UTC-03:00", "UTC-03:00");

        public static TimeSpan ZoneOffset => TimeSpan.FromHours(-3);

        // Un instante a la hora local de las pruebas.
        public static DateTimeOffset Local(int year, int month, int day, int hour, int minute)
        {
            return new DateTimeOffset(year, month, day, hour, minute, 0, ZoneOffset);
        }

        // Escalera (§6): calentamiento 60 s; serie ×3 [trabajo 120 s, 140–160 W +10, SPM 18 +2, split 2:10 −2; descanso 60 s];
        // enfriamiento 60 s. 8 pasos expandidos, 660 s.
        public static Routine Escalera()
        {
            return RoutineSeeds.Make(EscaleraName, new DateTimeOffset(2026, 10, 30, 13, 0, 0, TimeSpan.Zero),
                RoutineSeeds.Step(StepType.Warmup, dur: 60),
                RoutineSeeds.Step(StepType.Work, dur: 120, wMin: 140, wMax: 160, spm: 18, split: 130, g: "g1762000000000_1", rep: 3, pSpm: 2,
                    pW: 10, pSplit: -2),
                RoutineSeeds.Step(StepType.Rest, dur: 60, g: "g1762000000000_1", rep: 3),
                RoutineSeeds.Step(StepType.Cooldown, dur: 60));
        }

        // Las 6 sesiones de F-9 (§6), con su inicio local, si terminó y sus puntos (sessionId 0: lo pone la siembra).
        public static DateTimeOffset StartOf(int number)
        {
            switch (number)
            {
                case 1:
                    return Local(2026, 11, 5, 18, 40);
                case 2:
                    return Local(2026, 11, 4, 7, 15);
                case 3:
                    return Local(2026, 11, 3, 19, 2);
                case 4:
                    return Local(2026, 11, 2, 12, 0);
                case 5:
                    return Local(2026, 10, 2, 13, 51);
                case 6:
                    return Local(2026, 10, 1, 21, 5);
                default:
                    throw new ArgumentOutOfRangeException(nameof(number), number, "F-9 tiene las sesiones 1 a 6.");
            }
        }

        public static string? RoutineNameOf(int number)
        {
            return number == 1 || number == 3 ? EscaleraName : number == 6 ? DeletedRoutineName : null;
        }

        // Terminadas: 1, 2, 5 y 6 (con el punto final repetido); la 3 cortada y la 4 sin fin ni puntos.
        public static bool IsFinished(int number)
        {
            return number == 1 || number == 2 || number == 5 || number == 6;
        }

        // Los puntos de la sesión n de F-9, en orden y con el punto final repetido si terminó.
        public static IReadOnlyList<DataPoint> Points(int number, long sessionId = 0)
        {
            List<DataPoint> points;
            switch (number)
            {
                case 1:
                    points = Sample(sessionId, EscaleraPlan(), 0, Interval, 660, 0);
                    // E-02 (gate M-03): la fila 6 (índice 5) tiene 0 W, 0 spm y split 0 en 425 y 430 s; la distancia sigue.
                    for (var i = 0; i < points.Count; i++)
                    {
                        if (points[i].ElapsedSeconds == 425 || points[i].ElapsedSeconds == 430)
                        {
                            points[i] = points[i] with { PowerWatts = 0, StrokeRate = 0, Pace500mSeconds = 0 };
                        }
                    }

                    break;
                case 2:
                    points = Sample(sessionId, new[] { Free(224, 2024, 160, 22.0, 130, 145) }, 224, 224, 2024, 1000);
                    break;
                case 3:
                    points = Sample(sessionId, EscaleraPlan(), 0, Interval, 180, 0);
                    break;
                case 4:
                    points = new List<DataPoint>();
                    break;
                case 5:
                    points = Sample(sessionId, new[] { Free(0, 3725, 140, 20.0, 135, 0) }, 0, Interval, 3725, 0);
                    break;
                case 6:
                    points = Sample(sessionId, new[]
                    {
                        Step(0, StepType.Warmup, 0, 60), Step(1, StepType.Work, 60, 180), Step(2, StepType.Cooldown, 180, 240),
                    }, 0, Interval, 240, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(number), number, "F-9 tiene las sesiones 1 a 6.");
            }

            if (IsFinished(number) && points.Count > 0)
            {
                points.Add(points[points.Count - 1]); // el punto final repetido al cerrar (legacy)
            }

            return points;
        }

        // Siembra el conjunto en la base local (rutinas y sesiones por los repositorios de la app).
        public static HistorySeedResult Apply(LocalStore store, string set)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return Apply(store.Routines, store.Sessions, set);
        }

        public static HistorySeedResult Apply(IRoutineRepository routines, ISessionRepository sessions, string set)
        {
            if (routines == null)
            {
                throw new ArgumentNullException(nameof(routines));
            }

            if (sessions == null)
            {
                throw new ArgumentNullException(nameof(sessions));
            }

            switch (set)
            {
                case F9V:
                    return new HistorySeedResult(set, 0, new long[0]);
                case F9:
                {
                    var routine = Wait(routines.InsertAsync(Escalera()));
                    var routineId = routine.Id!.Value;
                    var ids = new long[6];
                    // De la más vieja a la más nueva: los ids crecen con la fecha.
                    for (var number = 6; number >= 1; number--)
                    {
                        long? rid = number == 1 || number == 3 ? routineId : number == 6 ? DeletedRoutineId : (long?)null;
                        ids[number - 1] = AddSession(sessions, StartOf(number), rid, RoutineNameOf(number), number, IsFinished(number));
                    }

                    return new HistorySeedResult(set, routineId, ids);
                }

                case F9P:
                {
                    var ids = new long[PerformanceSessions];
                    for (var day = PerformanceSessions - 1; day >= 0; day--)
                    {
                        ids[day] = AddSession(sessions, StartOf(2).AddDays(-day), null, null, 2, true);
                    }

                    return new HistorySeedResult(set, 0, ids);
                }

                default:
                    throw new ArgumentException("HistorySeeds: conjunto desconocido \"" + set + "\" (F-9, F-9V o F-9P).", nameof(set));
            }
        }

        // Una base NUEVA con el conjunto en el archivo pedido (si ya existe, la reemplaza con su -wal y su -shm), como
        // RoutineSeeds.WriteFile: pasa por TempDatabase.Guard (nunca en la carpeta de datos de la app ni con el nombre de su base
        // fuera de la carpeta de pruebas). Lo usan el comando seed de RowMate.SessionList y el arnés.
        public static HistorySeedResult WriteFile(string path, string set)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var full = Path.GetFullPath(path);
            TempDatabase.Guard(full);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                if (File.Exists(full + suffix))
                {
                    File.Delete(full + suffix);
                }
            }

            var logger = new RecordingLogger();
            using (var store = new LocalStore(new RowMateDatabase(full, new InlineDbExecutor(), logger), logger, Zone))
            {
                var result = Apply(store, set);
                Wait(store.Sessions.ListAsync()); // la base se abre al primer uso: F-9V también deja su archivo con el esquema
                return result;
            }
        }

        // VID-01 y las pruebas de recarga: una sesión libre terminada de `seconds` (150 W, 24.1 spm, split 133, pulso 150), como al
        // terminar un entrenamiento. Devuelve su id.
        public static long AddFinished(ISessionRepository sessions, DateTimeOffset startedAt, int seconds = 120)
        {
            if (sessions == null)
            {
                throw new ArgumentNullException(nameof(sessions));
            }

            var id = Wait(sessions.CreateAsync(new WorkoutSession(startedAt)));
            var points = Sample(id, new[] { Free(0, seconds, 150, 24.1, 133, 150) }, 0, Interval, seconds, 0);
            points.Add(points[points.Count - 1]);
            Finish(sessions, id, startedAt, null, null, points);
            return id;
        }

        // Una sesión libre de 60 min a 1 Hz (3 600 puntos, 1 a 3 600 s) con valores que cambian en cada punto y una parada cada
        // 10 min: el peor caso de la reducción y de la malla (A9-10; 02-tech-design §6). Determinista (sin azar ni trigonometría).
        public static IReadOnlyList<DataPoint> Stress60MinAt1Hz(long sessionId = 0)
        {
            var points = new DataPoint[3600];
            double distance = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var t = i + 1;
                var stopped = t % 600 < 3;
                var pace = stopped ? 0 : 120 + (t * 7) % 23;
                if (pace > 0)
                {
                    distance += 500.0 / pace;
                }

                points[i] = new DataPoint(sessionId, t)
                {
                    PowerWatts = stopped ? 0 : 100 + (t * 37) % 81,
                    StrokeRate = stopped ? 0 : 18 + ((t * 13) % 70) / 10.0,
                    Pace500mSeconds = pace,
                    DistanceMeters = (int)DartMath.Round(distance),
                    HeartRate = 120 + (t * 11) % 41,
                };
            }

            return points;
        }

        // ── Siembra ──
        private static long AddSession(ISessionRepository sessions, DateTimeOffset startedAt, long? routineId, string? routineName, int number,
                                       bool finished)
        {
            var id = Wait(sessions.CreateAsync(new WorkoutSession(startedAt) { RoutineId = routineId, RoutineName = routineName }));
            var points = new List<DataPoint>(Points(number, id));
            if (points.Count == 0)
            {
                return id;
            }

            if (!finished)
            {
                Wait(sessions.AddPointsAsync(points));
                return id;
            }

            Finish(sessions, id, startedAt, routineId, routineName, points);
            return id;
        }

        // Los lotes (todo menos el punto final) y el cierre con el punto final repetido y las estadísticas de los puntos.
        private static void Finish(ISessionRepository sessions, long id, DateTimeOffset startedAt, long? routineId, string? routineName,
                                   List<DataPoint> points)
        {
            var last = points[points.Count - 1];
            var batch = points.GetRange(0, points.Count - 1);
            if (batch.Count > 0)
            {
                Wait(sessions.AddPointsAsync(batch));
            }

            var session = new WorkoutSession(startedAt) { Id = id, RoutineId = routineId, RoutineName = routineName };
            var closed = session.Closed(SessionStats.Compute(points), startedAt.AddSeconds(last.ElapsedSeconds));
            Wait(sessions.FinishAsync(new[] { last }, closed));
        }

        // ── Puntos ──
        // Un tramo: de From a To s (el punto en t pertenece al tramo con From < t ≤ To), con su paso (o ninguno) y sus valores.
        private readonly struct Segment
        {
            public Segment(int? stepIndex, string? typeName, int from, int to, int watts, double spm, int split, int heartRate)
            {
                StepIndex = stepIndex;
                TypeName = typeName;
                From = from;
                To = to;
                Watts = watts;
                Spm = spm;
                Split = split;
                HeartRate = heartRate;
            }

            public int? StepIndex { get; }

            public string? TypeName { get; }

            public int From { get; }

            public int To { get; }

            public int Watts { get; }

            public double Spm { get; }

            public int Split { get; }

            public int HeartRate { get; }
        }

        private static Segment Free(int from, int to, int watts, double spm, int split, int heartRate)
        {
            return new Segment(null, null, from, to, watts, spm, split, heartRate);
        }

        private static Segment Step(int index, StepType type, int from, int to)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return new Segment(index, StepTypeNames.Warmup, from, to, 100, 20.0, 150, 120);
                case StepType.Cooldown:
                    return new Segment(index, StepTypeNames.Cooldown, from, to, 100, 20.0, 150, 125);
                case StepType.Work:
                    return new Segment(index, StepTypeNames.Work, from, to, 150, 24.1, 133, 150);
                default:
                    return new Segment(index, StepTypeNames.Rest, from, to, 60, 18.0, 180, 130);
            }
        }

        // Los 8 pasos de Escalera en el tiempo: (0,60] calentamiento, (60,180] trabajo, (180,240] descanso … (600,660] enfriamiento.
        private static Segment[] EscaleraPlan()
        {
            var plan = new List<Segment> { Step(0, StepType.Warmup, 0, 60) };
            var t = 60;
            for (var rep = 0; rep < 3; rep++)
            {
                plan.Add(Step(plan.Count, StepType.Work, t, t + 120));
                t += 120;
                plan.Add(Step(plan.Count, StepType.Rest, t, t + 60));
                t += 60;
            }

            plan.Add(Step(plan.Count, StepType.Cooldown, t, t + 60));
            return plan.ToArray();
        }

        // Un punto cada 5 s desde first hasta last (inclusive), con el tramo de su tiempo (el primero si t ≤ su From); la
        // distancia exacta se acumula desde origin (con startDistance) al ritmo de cada tramo y se redondea (DartMath.Round).
        private static List<DataPoint> Sample(long sessionId, Segment[] plan, int origin, int first, int last, double startDistance)
        {
            var points = new List<DataPoint>();
            for (var t = first; t <= last; t += Interval)
            {
                var segment = SegmentAt(plan, t);
                var distance = startDistance + DistanceBetween(plan, origin, t);
                points.Add(new DataPoint(sessionId, t)
                {
                    PowerWatts = segment.Watts,
                    StrokeRate = segment.Spm,
                    Pace500mSeconds = segment.Split,
                    HeartRate = segment.HeartRate,
                    DistanceMeters = (int)DartMath.Round(distance),
                    StepIndex = segment.StepIndex,
                    StepTypeName = segment.TypeName,
                });
            }

            return points;
        }

        private static Segment SegmentAt(Segment[] plan, int t)
        {
            for (var i = 0; i < plan.Length; i++)
            {
                if (t <= plan[i].To)
                {
                    return plan[i];
                }
            }

            return plan[plan.Length - 1];
        }

        // ∫ de origin a t de 500 / split (m por segundo), tramo por tramo.
        private static double DistanceBetween(Segment[] plan, int origin, int t)
        {
            double meters = 0;
            for (var i = 0; i < plan.Length; i++)
            {
                var from = Math.Max(origin, plan[i].From);
                var to = Math.Min(t, plan[i].To);
                if (to > from && plan[i].Split > 0)
                {
                    meters += (to - from) * 500.0 / plan[i].Split;
                }
            }

            return meters;
        }

        private static T Wait<T>(System.Threading.Tasks.Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        private static void Wait(System.Threading.Tasks.Task task)
        {
            task.GetAwaiter().GetResult();
        }
    }
}
