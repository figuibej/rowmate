using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Diagnostics;
using RowMate.Infrastructure.Routines;
using RowMate.Tests.Support;
using static RowMate.Tests.EditMode.Infrastructure.DatabaseFileState;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // DSC: el esquema v8 (AC-HIS-17-02; lib/core/database/database_service.dart:75-139 + origin + el índice, ADR 008) y los
    // pragmas de la conexión de escritura (QA-G3-08), leídos de vuelta con QueryRawForTestsAsync (sin tipos de sqlite-net:
    // QA-G3-07). Corre en dotnet y en Unity EditMode, contra los dos SQLite (R3-14).
    [TestFixture]
    public sealed class DatabaseSchemaTests
    {
        private RecordingLogger _logger = null!;
        private TempDatabase _db = null!;
        private SqliteRoutineRepository _repository = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _db = new TempDatabase();
            _repository = _db.Repository(_logger);
            Done(_repository.GetAllAsync());
        }

        [TearDown]
        public void TearDown()
        {
            _repository.Dispose();
            _db.Dispose();
        }

        // AC-HIS-17-02: exactamente las 4 tablas del legacy (con sqlite_sequence) y el índice, con el SQL al carácter.
        [Test, Category("AC-HIS-17-02")]
        public void SchemaIsV8()
        {
            var master = Query("SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY rowid");
            var objects = new List<string>();
            foreach (var row in master)
            {
                objects.Add(row[0] + " " + row[1]);
            }

            Assert.That(objects, Is.EqualTo(new[]
            {
                "table routines", "table sqlite_sequence", "table interval_steps", "table workout_sessions", "table data_points",
                "index idx_data_points_session",
            }));
            Assert.That(master[0][3], Is.EqualTo(DatabaseSchema.Statements[0]), "routines al carácter");
            Assert.That(master[2][3], Is.EqualTo(DatabaseSchema.Statements[1]));
            Assert.That(master[3][3], Is.EqualTo(DatabaseSchema.Statements[2]));
            Assert.That(master[4][3], Is.EqualTo(DatabaseSchema.Statements[3]));
            Assert.That(master[5][3], Is.EqualTo(DatabaseSchema.Statements[4]));
            Assert.That(master[5][2], Is.EqualTo("data_points"));
            Assert.That(Query("PRAGMA user_version")[0][0], Is.EqualTo(8L));
            Assert.That(DatabaseFiles.SchemaVersion, Is.EqualTo(8));
        }

        // AC-HIS-17-02: columnas, tipos, obligatorios, valores por defecto y claves (las del legacy + origin).
        [Test, Category("AC-HIS-17-02")]
        public void ColumnsTypesAndDefaults()
        {
            Columns("routines",
                "id INTEGER 0 - pk", "name TEXT 1 -", "description TEXT 1 ''", "created_at TEXT 1 -");
            Columns("interval_steps",
                "id INTEGER 0 - pk", "routine_id INTEGER 1 -", "step_order INTEGER 1 -", "type TEXT 1 -", "duration_seconds INTEGER 0 -",
                "distance_meters INTEGER 0 -", "target_watts_min INTEGER 0 -", "target_watts_max INTEGER 0 -", "target_spm INTEGER 0 -",
                "target_split_seconds INTEGER 0 -", "group_id TEXT 0 -", "group_repeat_count INTEGER 0 -", "progression_spm INTEGER 0 -",
                "progression_watts INTEGER 0 -", "progression_split_seconds INTEGER 0 -");
            Columns("workout_sessions",
                "id INTEGER 0 - pk", "routine_id INTEGER 0 -", "routine_name TEXT 0 -", "started_at TEXT 1 -", "finished_at TEXT 0 -",
                "total_distance_meters INTEGER 1 0", "total_time_seconds INTEGER 1 0", "avg_power_watts INTEGER 1 0", "avg_stroke_rate REAL 1 0",
                "total_calories INTEGER 1 0", "strava_activity_id TEXT 0 -", "origin TEXT 1 'app'");
            Columns("data_points",
                "id INTEGER 0 - pk", "session_id INTEGER 1 -", "elapsed_seconds INTEGER 1 -", "stroke_rate REAL 1 0", "stroke_count INTEGER 1 0",
                "distance_meters INTEGER 1 0", "pace_500m_seconds INTEGER 1 0", "power_watts INTEGER 1 0", "calories INTEGER 1 0",
                "heart_rate INTEGER 1 0", "step_index INTEGER 0 -", "step_type TEXT 0 -");
        }

        // AC-HIS-17-02: las cascadas del DDL (se conservan por paridad, con foreign_keys = OFF) y el índice por sesión y tiempo.
        [Test, Category("AC-HIS-17-02")]
        public void ForeignKeysAndIndex()
        {
            Assert.That(ForeignKey("interval_steps"), Is.EqualTo("routines routine_id id CASCADE"));
            Assert.That(ForeignKey("data_points"), Is.EqualTo("workout_sessions session_id id CASCADE"));
            Assert.That(Query("PRAGMA foreign_key_list(routines)"), Is.Empty);
            var indexes = Query("PRAGMA index_list(data_points)");
            Assert.That(indexes.Count, Is.EqualTo(1));
            Assert.That(indexes[0][1], Is.EqualTo("idx_data_points_session"));
            Assert.That(indexes[0][2], Is.EqualTo(0L), "no único");
            var columns = Query("PRAGMA index_info(idx_data_points_session)");
            Assert.That(columns.Count, Is.EqualTo(2));
            Assert.That(columns[0][2], Is.EqualTo("session_id"));
            Assert.That(columns[1][2], Is.EqualTo("elapsed_seconds"));
            foreach (var table in new[] { "routines", "interval_steps", "workout_sessions" })
            {
                Assert.That(Query("PRAGMA index_list(" + table + ")"), Is.Empty, table + ": sin índices de más");
            }
        }

        // QA-G3-08 (AC-RUT-08-07, DT-07): la conexión de escritura queda en WAL, synchronous = FULL (2) y sin foreign keys, en
        // los dos SQLite (el de Unity trae WAL en NORMAL por defecto).
        [Test, Category("AC-HIS-17-02"), Category("AC-RUT-08-07")]
        public void WriteConnectionIsWalFullAndNoForeignKeys()
        {
            Assert.That(Query("PRAGMA journal_mode")[0][0], Is.EqualTo("wal"));
            Assert.That(Query("PRAGMA synchronous")[0][0], Is.EqualTo(2L), "FULL");
            Assert.That(Query("PRAGMA foreign_keys")[0][0], Is.EqualTo(0L));
            Assert.That(Query("PRAGMA busy_timeout")[0][0], Is.EqualTo(5000L));
            _repository.Dispose();
            _repository = _db.Repository(_logger);
            Done(_repository.GetAllAsync());
            Assert.That(Query("PRAGMA synchronous")[0][0], Is.EqualTo(2L), "también al abrir una base existente");
            Assert.That(Query("PRAGMA journal_mode")[0][0], Is.EqualTo("wal"));
        }

        // La versión de SQLite de cada mundo (DT-16): 3.49.1 en dotnet (SQLitePCLRaw.bundle_green 2.1.11) y 3.53.4 en Unity
        // (com.gilzoide.sqlite-net 1.3.3). Reemplaza a SQU: Tests/UnityEditMode no ve los internos de Infrastructure.
        [Test, Category("AC-HIS-17-02")]
        public void SqliteVersionIsTheExpectedOne()
        {
#if UNITY_EDITOR
            const string expected = "3.53.4";
#else
            const string expected = "3.49.1";
#endif
            Assert.That(Query("SELECT sqlite_version()")[0][0], Is.EqualTo(expected));
        }

        // §2.3 paso 6: se crea en modo rollback y después pasa a WAL, así user_version = 8 queda en el archivo principal
        // desde el primer commit (bytes 18/19 = 2: WAL).
        [Test, Category("AC-HIS-17-02")]
        public void CreationLeavesTheVersionInTheMainFile()
        {
            var header = DatabaseHeader.Read(_db.FilePath);
            Assert.That(header.IsComplete && header.HasSignature, Is.True);
            Assert.That(header.UserVersion, Is.EqualTo(8));
            var bytes = new byte[100];
            using (var stream = new FileStream(_db.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Assert.That(stream.Read(bytes, 0, 100), Is.EqualTo(100));
            }

            Assert.That(bytes[18], Is.EqualTo(2), "write version: WAL");
            Assert.That(bytes[19], Is.EqualTo(2), "read version: WAL");
            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[DB] Info: base " + TempDatabase.DefaultFileName + " creada (v8)" }), "solo el nombre del archivo");
        }

        // R3-01 (§6.1): la prueba de humo de un player crea, escribe, lee y borra en una subcarpeta temporal y no deja nada;
        // su línea no lleva rutas (QA-G3-20).
        [Test, Category("AC-HIS-17-02")]
        public void SmokeCreatesWritesReadsAndDeletes()
        {
            var logger = new RecordingLogger();
            var ok = false;
            TestMainThreadContext.Run(async () => ok = await DatabaseSmoke.RunAsync(_db.Folder, logger));
            Assert.That(ok, Is.True);
            Assert.That(logger.Lines[logger.Lines.Count - 1], Is.EqualTo("[DB] Info: humo: ok"));
            Assert.That(Directory.GetDirectories(_db.Folder), Is.Empty, "borró su subcarpeta");
            foreach (var line in logger.Lines)
            {
                Assert.That(line, Does.Not.Contain(_db.Folder));
                Assert.That(line, Does.Not.Contain(TempDatabase.Root));
            }
        }

        private IReadOnlyList<object?[]> Query(string sql)
        {
            return Done(_repository.Database.QueryRawForTestsAsync(sql));
        }

        // "nombre TIPO obligatorio defecto [pk]"; "-" = sin valor por defecto.
        private void Columns(string table, params string[] expected)
        {
            var actual = new List<string>();
            foreach (var row in Query("PRAGMA table_info(" + table + ")"))
            {
                actual.Add(row[1] + " " + row[2] + " " + row[3] + " " + (row[4] ?? "-") + ((long)row[5]! == 1 ? " pk" : ""));
            }

            Assert.That(actual, Is.EqualTo(expected), table);
        }

        private string ForeignKey(string table)
        {
            var rows = Query("PRAGMA foreign_key_list(" + table + ")");
            Assert.That(rows.Count, Is.EqualTo(1), table);
            return rows[0][2] + " " + rows[0][3] + " " + rows[0][4] + " " + rows[0][6];
        }
    }
}
