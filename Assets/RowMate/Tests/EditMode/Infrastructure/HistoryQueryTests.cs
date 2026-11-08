using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Infrastructure.Database;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // HQ · La consulta del historial sobre SQLite real (02-tech-design DT9-01, §4 de la It. 9; database_service.dart:248-252,
    // :273-321): todas las sesiones por inicio descendente con el id mayor primero en un empate, las estadísticas de los PUNTOS
    // (no de los totales de la fila), la cortada, la sin puntos, el pulso que falta y una sesión escrita como en las It. 4a a 8.
    [TestFixture]
    public sealed class HistoryQueryTests
    {
        private RecordingLogger _logger = null!;
        private TempDatabase _temp = null!;
        private LocalStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _temp = new TempDatabase();
            _store = _temp.Store(_logger, localZone: HistorySeeds.Zone);
        }

        [TearDown]
        public void TearDown()
        {
            _store.Dispose();
            _temp.Dispose();
        }

        private IReadOnlyList<HistoryEntry> History()
        {
            return _store.Sessions.GetHistoryAsync().GetAwaiter().GetResult();
        }

        private long Create(DateTimeOffset startedAt, string? routine = null)
        {
            return _store.Sessions.CreateAsync(new WorkoutSession(startedAt) { RoutineName = routine }).GetAwaiter().GetResult();
        }

        [Test, Category("AC-HIS-07-02"), Category("AC-HIS-19-03")]
        public void OrderAndAll()
        {
            var old = Create(HistorySeeds.Local(2026, 10, 1, 10, 0), "vieja");
            var tieA = Create(HistorySeeds.Local(2026, 11, 2, 12, 0), "empate A");
            var tieB = Create(HistorySeeds.Local(2026, 11, 2, 12, 0), "empate B");
            var finished = HistorySeeds.AddFinished(_store.Sessions, HistorySeeds.Local(2026, 10, 15, 9, 30));
            _store.Sessions.AddPointsAsync(HistorySeeds.Points(3, tieA)).GetAwaiter().GetResult(); // cortada, con puntos
            var history = History();
            Assert.That(history.Select(e => e.Session.Id), Is.EqualTo(new long?[] { tieB, tieA, finished, old }),
                "la más nueva arriba; con el mismo inicio, el id mayor primero");
            Assert.That(history.Count(e => e.Session.FinishedAt == null), Is.EqualTo(3), "entran las cortadas y las sin puntos");
        }

        [Test, Category("AC-HIS-07-05"), Category("AC-RUT-18-05")]
        public void StatsFromPoints()
        {
            var id = Create(HistorySeeds.Local(2026, 11, 3, 19, 2), "Escalera");
            var points = HistorySeeds.Points(1, id);
            _store.Sessions.AddPointsAsync(points.Take(points.Count - 1).ToList()).GetAwaiter().GetResult();
            // El cierre con totales que no son los de los puntos: la lista NO los usa.
            var closed = new WorkoutSession(HistorySeeds.Local(2026, 11, 3, 19, 2))
            {
                Id = id, RoutineName = "Escalera", FinishedAt = HistorySeeds.Local(2026, 11, 3, 19, 13), TotalDistanceMeters = 99999,
                TotalTimeSeconds = 1, AvgPowerWatts = 7, AvgStrokeRate = 1.5,
            };
            _store.Sessions.FinishAsync(new[] { points[points.Count - 1] }, closed).GetAwaiter().GetResult();
            var entry = History().Single();
            Assert.That(entry.Session.TotalDistanceMeters, Is.EqualTo(99999), "la fila guardada");
            Assert.That(entry.Stats, Is.EqualTo(SessionStats.Compute(points)), "las estadísticas, de los puntos");
            Assert.That(entry.Stats.TotalDistance, Is.EqualTo(2253));
            Assert.That(entry.Stats.AvgHeartRate, Is.EqualTo(139));
            Assert.That(entry.Stats.MaxHeartRate, Is.EqualTo(150));
        }

        [Test, Category("AC-HIS-19-03")]
        public void DefaultsWithoutPoints()
        {
            Create(HistorySeeds.Local(2026, 11, 2, 12, 0));
            var entry = History().Single();
            Assert.That(entry.Stats, Is.SameAs(SessionStats.Empty));
            Assert.That(entry.Stats.HasHeartRate, Is.False);
        }

        [Test, Category("AC-HIS-19-03")]
        public void MissingHr()
        {
            var id = Create(HistorySeeds.Local(2026, 11, 2, 12, 0));
            // Puntos escritos sin pulso (la columna toma su valor por defecto) y con lo mínimo: cuentan como 0.
            _store.Database.ExecuteRawForTestsAsync("INSERT INTO data_points (session_id, elapsed_seconds, power_watts, pace_500m_seconds) "
                + "VALUES (?, 5, 150, 133)", id).GetAwaiter().GetResult();
            _store.Database.ExecuteRawForTestsAsync("INSERT INTO data_points (session_id, elapsed_seconds) VALUES (?, 10)", id).GetAwaiter().GetResult();
            var stats = History().Single().Stats;
            Assert.That(stats.HasHeartRate, Is.False, "el pulso que falta cuenta como 0 (sin columna de pulso)");
            Assert.That(stats.P99Watts, Is.EqualTo(150));
            Assert.That(stats.TotalTimeSeconds, Is.EqualTo(10));
        }

        [Test, Category("AC-HIS-19-03"), Category("AC-HIS-05-01")]
        public void OldSessions()
        {
            // Una fila escrita como las de las It. 4a a 8 (origin 'app', con totales P99 y el id de Strava vacío) y sus puntos
            // con paso: se lee igual que una nueva, con la fecha en la hora local.
            _store.Database.ExecuteRawForTestsAsync("INSERT INTO workout_sessions (routine_id, routine_name, started_at, finished_at, "
                + "total_distance_meters, total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id, origin) "
                + "VALUES (3, 'Intervalos', '2026-10-02T16:51:00.000Z', '2026-10-02T17:00:00.000Z', 1200, 540, 180, 26.5, 40, NULL, 'app')")
                .GetAwaiter().GetResult();
            _store.Database.ExecuteRawForTestsAsync("INSERT INTO data_points (session_id, elapsed_seconds, stroke_rate, stroke_count, distance_meters, "
                + "pace_500m_seconds, power_watts, calories, heart_rate, step_index, step_type) VALUES (1, 540, 26.5, 230, 1200, 120, 180, 40, 151, 0, 'work')")
                .GetAwaiter().GetResult();
            var entry = History().Single();
            Assert.That(entry.Session.RoutineName, Is.EqualTo("Intervalos"));
            Assert.That(entry.Session.StartedAt.Offset, Is.EqualTo(TimeSpan.FromHours(-3)), "la zona de LocalStore (DT9-16)");
            Assert.That(entry.Session.StartedAt.Hour, Is.EqualTo(13));
            Assert.That(entry.Stats.TotalDistance, Is.EqualTo(1200));
            Assert.That(entry.Stats.AvgHeartRate, Is.EqualTo(151));
        }

        [Test, Category("AC-HIS-19-03")]
        public void ManySessionsInOneRead()
        {
            var seed = HistorySeeds.Apply(_store, HistorySeeds.F9);
            var history = History();
            Assert.That(history.Count, Is.EqualTo(6));
            Assert.That(history.Select(e => e.Session.Id), Is.EqualTo(Enumerable.Range(1, 6).Select(n => (long?)seed.Session(n))));
            foreach (var entry in history)
            {
                var points = _store.Sessions.GetPointsAsync(entry.Session.Id!.Value).GetAwaiter().GetResult();
                Assert.That(entry.Stats, Is.EqualTo(points.Count == 0 ? SessionStats.Empty : SessionStats.Compute(points)), "sesión " + entry.Session.Id);
            }
        }
    }
}
