using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-16: sesión de entrenamiento (legacy: workout_session.dart:2-67; cierre: workout_provider.dart:276-292).
    [TestFixture]
    public sealed class WorkoutSessionTests
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Zona fija UTC−3 (no depende del equipo), como pide 02-tech-design §3.3 (Fechas).
        private static readonly TimeZoneInfo UtcMinus3 =
            TimeZoneInfo.CreateCustomTimeZone("RowMate/UTC-3", TimeSpan.FromHours(-3), "UTC-3", "UTC-3");

        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 27, 9, 37, 38, 628, TimeSpan.Zero);

        // AC-RUT-16-01: atributos y valores por defecto.
        [Test, Category("AC-RUT-16-01")]
        public void AttributesAndDefaults()
        {
            var session = new WorkoutSession(Start);
            Assert.That(session.StartedAt, Is.EqualTo(Start));
            Assert.That(session.Id, Is.Null);
            Assert.That(session.RoutineId, Is.Null, "entrenamiento libre: sin rutina");
            Assert.That(session.RoutineName, Is.Null);
            Assert.That(session.FinishedAt, Is.Null, "en curso");
            Assert.That(session.TotalDistanceMeters, Is.EqualTo(0));
            Assert.That(session.TotalTimeSeconds, Is.EqualTo(0));
            Assert.That(session.AvgPowerWatts, Is.EqualTo(0));
            Assert.That(session.AvgStrokeRate, Is.EqualTo(0.0));
            Assert.That(session.TotalCalories, Is.EqualTo(0));
            Assert.That(session.StravaActivityId, Is.Null, "no se subió");
            Assert.That(typeof(WorkoutSession).GetProperty("AvgStrokeRate")!.PropertyType, Is.EqualTo(typeof(double)), "SPM decimal");
            Assert.That(typeof(WorkoutSession).GetConstructors().Length, Is.EqualTo(1));
            Assert.That(typeof(WorkoutSession).GetConstructors()[0].GetParameters().Length, Is.EqualTo(1), "solo el inicio es obligatorio");
        }

        // AC-RUT-16-02 (Q-14): "promedio" = P99 (potencia redondeada, SPM decimal); distancia, tiempo y calorías = máximos.
        [Test, Category("AC-RUT-16-02")]
        public void ClosedStoresP99AndMaxima()
        {
            var points = new List<DataPoint>();
            for (var i = 0; i < 100; i++)
            {
                points.Add(new DataPoint(1, i * 6)
                {
                    PowerWatts = 2 * (i + 1), Pace500mSeconds = 100 + i, StrokeRate = 20 + 0.1 * i, DistanceMeters = 20 * i, Calories = i / 2,
                });
            }

            var stats = SessionStats.Compute(points);
            var session = new WorkoutSession(Start) { Id = 4, RoutineId = 3, RoutineName = "Escalera SPM", StravaActivityId = "999" };
            var finished = Start.AddMinutes(10);
            var closed = session.Closed(stats, finished);
            Assert.That(closed.AvgPowerWatts, Is.EqualTo(198), "P99 de vatios, no el promedio (101)");
            Assert.That(closed.AvgStrokeRate, Is.EqualTo(stats.P99Spm));
            Assert.That(closed.AvgStrokeRate, Is.EqualTo(29.8).Within(1e-9));
            Assert.That(closed.TotalDistanceMeters, Is.EqualTo(1980));
            Assert.That(closed.TotalTimeSeconds, Is.EqualTo(594));
            Assert.That(closed.TotalCalories, Is.EqualTo(49));
            Assert.That(closed.FinishedAt, Is.EqualTo(finished));
            Assert.That(closed.Id, Is.EqualTo(4L));
            Assert.That(closed.RoutineId, Is.EqualTo(3L));
            Assert.That(closed.RoutineName, Is.EqualTo("Escalera SPM"));
            Assert.That(closed.StartedAt, Is.EqualTo(Start));
            Assert.That(closed.StravaActivityId, Is.Null, "el cierre arma la sesión sin el id de la actividad subida");
            Assert.That(session.FinishedAt, Is.Null, "la original no cambia");
        }

        // AC-RUT-16-03: duración como AC-BLE-01-03.
        [TestCase(59, "00:59")]
        [TestCase(3661, "1:01:01")]
        [TestCase(0, "00:00")]
        [TestCase(3600, "1:00:00")]
        [Category("AC-RUT-16-03")]
        public void Duration(int seconds, string expected)
        {
            Assert.That(new WorkoutSession(Start) { TotalTimeSeconds = seconds }.DurationFormatted, Is.EqualTo(expected));
        }

        // AC-RUT-16-04: al guardar, UTC ISO-8601 con Z, milisegundos siempre y microsegundos solo si no son 0.
        [Test, Category("AC-RUT-16-04")]
        public void DatesUtcIsoWithMicrosOnlyIfNonZero()
        {
            Assert.That(WorkoutSessionCodec.ToRecord(new WorkoutSession(Start.AddTicks(537 * 10))).StartedAt, Is.EqualTo("2026-10-27T09:37:38.628537Z"));
            Assert.That(WorkoutSessionCodec.ToRecord(new WorkoutSession(Start)).StartedAt, Is.EqualTo("2026-10-27T09:37:38.628Z"));
            var whole = new DateTimeOffset(2026, 10, 27, 9, 37, 38, TimeSpan.Zero);
            Assert.That(WorkoutSessionCodec.ToRecord(new WorkoutSession(whole)).StartedAt, Is.EqualTo("2026-10-27T09:37:38.000Z"));
            // Una hora local con su offset se guarda en UTC.
            var local = new DateTimeOffset(2026, 10, 27, 6, 37, 38, 628, TimeSpan.FromHours(-3));
            var rec = WorkoutSessionCodec.ToRecord(new WorkoutSession(local) { FinishedAt = local.AddSeconds(90) });
            Assert.That(rec.StartedAt, Is.EqualTo("2026-10-27T09:37:38.628Z"));
            Assert.That(rec.FinishedAt, Is.EqualTo("2026-10-27T09:39:08.628Z"));
            Assert.That(WorkoutSessionCodec.ToRecord(new WorkoutSession(local)).FinishedAt, Is.Null, "en curso");
        }

        // AC-RUT-16-04: al leer, pasa a la hora local (con UTC−3, 09:37:38,628Z es el 27/10/2026 a las 06:37:38,628).
        [Test, Category("AC-RUT-16-04")]
        public void LocalConversionUtcMinus3()
        {
            var session = WorkoutSessionCodec.FromRecord(new WorkoutSessionRecord("2026-10-27T09:37:38.628Z"), UtcMinus3);
            Assert.That(session.StartedAt.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
            Assert.That(session.StartedAt.DateTime, Is.EqualTo(new DateTime(2026, 10, 27, 6, 37, 38, 628)));
            Assert.That(session.StartedAt, Is.EqualTo(Start), "el mismo instante");
            var withEnd = WorkoutSessionCodec.FromRecord(new WorkoutSessionRecord("2026-10-27T09:37:38.628Z") { FinishedAt = "2026-10-27T10:00:00.000Z" }, UtcMinus3);
            Assert.That(withEnd.FinishedAt!.Value.DateTime, Is.EqualTo(new DateTime(2026, 10, 27, 7, 0, 0)));
            // Ida y vuelta: guardar y leer da el mismo instante.
            var original = new WorkoutSession(Start.AddTicks(5370)) { FinishedAt = Start.AddHours(1) };
            var back = WorkoutSessionCodec.FromRecord(WorkoutSessionCodec.ToRecord(original), UtcMinus3);
            Assert.That(back.StartedAt.UtcTicks, Is.EqualTo(original.StartedAt.UtcTicks));
            Assert.That(back.FinishedAt!.Value.UtcTicks, Is.EqualTo(original.FinishedAt!.Value.UtcTicks));
        }

        // AC-RUT-16-05: un registro con solo la fecha de inicio → 0, 0.0 y vacíos.
        [Test, Category("AC-RUT-16-05")]
        public void TolerantRead()
        {
            var session = WorkoutSessionCodec.FromRecord(new WorkoutSessionRecord("2026-10-27T09:37:38.628Z"), UtcMinus3);
            Assert.That(session.TotalDistanceMeters, Is.EqualTo(0));
            Assert.That(session.TotalTimeSeconds, Is.EqualTo(0));
            Assert.That(session.AvgPowerWatts, Is.EqualTo(0));
            Assert.That(session.AvgStrokeRate, Is.EqualTo(0.0));
            Assert.That(session.TotalCalories, Is.EqualTo(0));
            Assert.That(session.RoutineId, Is.Null);
            Assert.That(session.RoutineName, Is.Null);
            Assert.That(session.StravaActivityId, Is.Null);
            Assert.That(session.FinishedAt, Is.Null);
            Assert.That(session.Id, Is.Null);
            Assert.Throws<ArgumentNullException>(() => new WorkoutSessionRecord(null!), "started_at es obligatoria");
            Assert.Throws<FormatException>(() => WorkoutSessionCodec.FromRecord(new WorkoutSessionRecord("no es una fecha"), UtcMinus3));
            Assert.Throws<OverflowException>(() => WorkoutSessionCodec.FromRecord(
                new WorkoutSessionRecord("2026-10-27T09:37:38Z") { TotalDistanceMeters = 1L << 40 }, UtcMinus3), "un entero que no entra se avisa");
        }

        [Test, Category("AC-RUT-16-01")]
        public void RecordMapsEveryColumn()
        {
            var session = new WorkoutSession(Start)
            {
                Id = 7, RoutineId = 3, RoutineName = "Escalera SPM", FinishedAt = Start.AddMinutes(30), TotalDistanceMeters = 6123, TotalTimeSeconds = 1800,
                AvgPowerWatts = 198, AvgStrokeRate = 29.8, TotalCalories = 412, StravaActivityId = "123456789",
            };
            var rec = WorkoutSessionCodec.ToRecord(session);
            Assert.That(rec.Id, Is.EqualTo(7L));
            Assert.That(rec.RoutineId, Is.EqualTo(3L));
            Assert.That(rec.RoutineName, Is.EqualTo("Escalera SPM"));
            Assert.That(rec.TotalDistanceMeters, Is.EqualTo(6123L));
            Assert.That(rec.TotalTimeSeconds, Is.EqualTo(1800L));
            Assert.That(rec.AvgPowerWatts, Is.EqualTo(198L));
            Assert.That(rec.AvgStrokeRate, Is.EqualTo(29.8));
            Assert.That(rec.TotalCalories, Is.EqualTo(412L));
            Assert.That(rec.StravaActivityId, Is.EqualTo("123456789"));
            var back = WorkoutSessionCodec.FromRecord(rec, UtcMinus3);
            Assert.That(back with { StartedAt = session.StartedAt, FinishedAt = session.FinishedAt }, Is.EqualTo(session));
            Assert.That(back.StartedAt.UtcTicks, Is.EqualTo(session.StartedAt.UtcTicks));
        }

        // AC-RUT-16-04 + AC-RUT-16-05 + AC-BASE-03: toMap y fromMap del legacy (oráculo).
        [Test, Category("AC-RUT-16-04"), Category("AC-RUT-16-05"), Category("AC-BASE-03")]
        public void MatchesOracle()
        {
            var golden = GoldenVectors.Load("session.json");
            var problems = new List<string>();
            foreach (var item in golden.Section("toMap").Items)
            {
                var name = item["name"].AsString();
                var input = item["input"];
                var session = new WorkoutSession(FromMicros(input["startedAtMicros"].AsLong()))
                {
                    Id = LongOrNull(input["id"]),
                    RoutineId = LongOrNull(input["routineId"]),
                    RoutineName = input["routineName"].IsNull ? null : input["routineName"].AsString(),
                    FinishedAt = input["finishedAtMicros"].IsNull ? (DateTimeOffset?)null : FromMicros(input["finishedAtMicros"].AsLong()),
                    TotalDistanceMeters = (int)input["totalDistanceMeters"].AsLong(),
                    TotalTimeSeconds = (int)input["totalTimeSeconds"].AsLong(),
                    AvgPowerWatts = (int)input["avgPowerWatts"].AsLong(),
                    AvgStrokeRate = input["avgStrokeRate"].AsF64(),
                    TotalCalories = (int)input["totalCalories"].AsLong(),
                };
                var rec = WorkoutSessionCodec.ToRecord(session);
                var map = item["map"];
                Expect(problems, name, "id", map.TryGet("id", out var id) ? LongOrNull(id) : null, rec.Id);
                Expect(problems, name, "routine_id", LongOrNull(map["routine_id"]), rec.RoutineId);
                Expect(problems, name, "routine_name", TextOrNull(map["routine_name"]), rec.RoutineName);
                Expect(problems, name, "started_at", map["started_at"].AsString(), rec.StartedAt);
                Expect(problems, name, "finished_at", TextOrNull(map["finished_at"]), rec.FinishedAt);
                Expect(problems, name, "total_distance_meters", LongOrNull(map["total_distance_meters"]), rec.TotalDistanceMeters);
                Expect(problems, name, "total_time_seconds", LongOrNull(map["total_time_seconds"]), rec.TotalTimeSeconds);
                Expect(problems, name, "avg_power_watts", LongOrNull(map["avg_power_watts"]), rec.AvgPowerWatts);
                Expect(problems, name, "avg_stroke_rate", Bits(map["avg_stroke_rate"].AsF64()), rec.AvgStrokeRate.HasValue ? Bits(rec.AvgStrokeRate.Value) : null);
                Expect(problems, name, "total_calories", LongOrNull(map["total_calories"]), rec.TotalCalories);
                // El oráculo no nombra esta columna (guardia R-02 de run-oracle.ps1), pero toMap la escribe igual.
                Expect(problems, name, "strava_activity_id", TextOrNull(map["strava_activity_id"]), rec.StravaActivityId);
                Assert.That(map.Properties.Count, Is.EqualTo(map.TryGet("id", out _) ? 11 : 10), name + ": columnas de toMap");
            }

            foreach (var item in golden.Section("fromMap").Items)
            {
                var name = item["name"].AsString();
                var map = item["map"];
                var rec = new WorkoutSessionRecord(map["started_at"].AsString())
                {
                    Id = map.TryGet("id", out var id) ? LongOrNull(id) : null,
                    RoutineId = map.TryGet("routine_id", out var rid) ? LongOrNull(rid) : null,
                    RoutineName = map.TryGet("routine_name", out var rn) ? TextOrNull(rn) : null,
                    FinishedAt = map.TryGet("finished_at", out var fa) ? TextOrNull(fa) : null,
                    TotalDistanceMeters = map.TryGet("total_distance_meters", out var td) ? LongOrNull(td) : null,
                    TotalTimeSeconds = map.TryGet("total_time_seconds", out var tt) ? LongOrNull(tt) : null,
                    AvgPowerWatts = map.TryGet("avg_power_watts", out var ap) ? LongOrNull(ap) : null,
                    AvgStrokeRate = map.TryGet("avg_stroke_rate", out var asr) && !asr.IsNull ? asr.AsF64() : (double?)null,
                    TotalCalories = map.TryGet("total_calories", out var tc) ? LongOrNull(tc) : null,
                };
                var session = WorkoutSessionCodec.FromRecord(rec, UtcMinus3);
                var result = item["result"];
                Expect(problems, name, "startedAtMicros", result["startedAtMicros"].AsLong(), Micros(session.StartedAt));
                Expect(problems, name, "finishedAtMicros", LongOrNull(result["finishedAtMicros"]), session.FinishedAt.HasValue ? Micros(session.FinishedAt.Value) : (long?)null);
                Expect(problems, name, "startedAtOffset", UtcMinus3.BaseUtcOffset.Ticks, session.StartedAt.Offset.Ticks);
                Expect(problems, name, "id", LongOrNull(result["id"]), session.Id);
                Expect(problems, name, "routineId", LongOrNull(result["routineId"]), session.RoutineId);
                Expect(problems, name, "routineName", TextOrNull(result["routineName"]), session.RoutineName);
                Expect(problems, name, "totalDistanceMeters", result["totalDistanceMeters"].AsLong(), session.TotalDistanceMeters);
                Expect(problems, name, "totalTimeSeconds", result["totalTimeSeconds"].AsLong(), session.TotalTimeSeconds);
                Expect(problems, name, "avgPowerWatts", result["avgPowerWatts"].AsLong(), session.AvgPowerWatts);
                Expect(problems, name, "avgStrokeRate", Bits(result["avgStrokeRate"].AsF64()), Bits(session.AvgStrokeRate));
                Expect(problems, name, "totalCalories", result["totalCalories"].AsLong(), session.TotalCalories);
                Expect(problems, name, "durationFormatted", result["durationFormatted"].AsString(), session.DurationFormatted);
                Assert.That(result["startedAtIsUtc"].AsBool(), Is.False, name + ": el legacy lo pasa a hora local");
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static void Expect(List<string> problems, string name, string field, object? expected, object? actual)
        {
            if (!Equals(expected, actual) && !(expected is long e && actual is int a && e == a))
            {
                problems.Add(name + "." + field + ": legacy " + (expected ?? "null") + ", obtenido " + (actual ?? "null"));
            }
        }

        private static DateTimeOffset FromMicros(long micros)
        {
            return Epoch.AddTicks(micros * 10);
        }

        private static long Micros(DateTimeOffset value)
        {
            return (value.UtcTicks - Epoch.UtcTicks) / 10;
        }

        private static long? LongOrNull(JsonNode node)
        {
            return node.IsNull ? (long?)null : node.AsLong();
        }

        private static string? TextOrNull(JsonNode node)
        {
            return node.IsNull ? null : node.AsString();
        }

        private static string Bits(double x)
        {
            return GoldenJsonExtensions.ToF64Bits(x) + " (" + Fmt.DartToString(x) + ")";
        }
    }
}
