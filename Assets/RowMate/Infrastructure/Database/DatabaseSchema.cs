using System;
using System.Collections.Generic;
using RowMate.Core.Text;
using SQLite;

namespace RowMate.Infrastructure.Database
{
    // Esquema v8 (AC-HIS-17-02; 02-tech-design §2.1): el DDL v7 del legacy (lib/core/database/database_service.dart:76-138),
    // con workout_sessions.origin al final y el índice de data_points (ADR 008). Se ejecuta TAL CUAL con Execute (nunca con
    // CreateTable<T>, que generaría otro esquema); solo cambia la sangría del texto del legacy, sin efecto en el esquema.
    // Literales SQL solo entre comillas simples: el SQLite de Unity compila con SQLITE_DQS 0 (QA-G3-08).
    // Las cláusulas ON DELETE CASCADE quedan por paridad, pero foreign_keys = OFF (el legacy nunca las activa, DC-06): la baja
    // de una rutina borra sus pasos explícitamente (Q-17).
    internal static class DatabaseSchema
    {
        public static readonly IReadOnlyList<string> Statements = new[]
        {
            "CREATE TABLE routines (\n"
            + "  id          INTEGER PRIMARY KEY AUTOINCREMENT,\n"
            + "  name        TEXT NOT NULL,\n"
            + "  description TEXT NOT NULL DEFAULT '',\n"
            + "  created_at  TEXT NOT NULL\n"
            + ")",
            "CREATE TABLE interval_steps (\n"
            + "  id                INTEGER PRIMARY KEY AUTOINCREMENT,\n"
            + "  routine_id        INTEGER NOT NULL,\n"
            + "  step_order        INTEGER NOT NULL,\n"
            + "  type              TEXT NOT NULL,\n"
            + "  duration_seconds  INTEGER,\n"
            + "  distance_meters   INTEGER,\n"
            + "  target_watts_min  INTEGER,\n"
            + "  target_watts_max  INTEGER,\n"
            + "  target_spm            INTEGER,\n"
            + "  target_split_seconds  INTEGER,\n"
            + "  group_id              TEXT,\n"
            + "  group_repeat_count    INTEGER,\n"
            + "  progression_spm       INTEGER,\n"
            + "  progression_watts     INTEGER,\n"
            + "  progression_split_seconds INTEGER,\n"
            + "  FOREIGN KEY (routine_id) REFERENCES routines(id) ON DELETE CASCADE\n"
            + ")",
            "CREATE TABLE workout_sessions (\n"
            + "  id                    INTEGER PRIMARY KEY AUTOINCREMENT,\n"
            + "  routine_id            INTEGER,\n"
            + "  routine_name          TEXT,\n"
            + "  started_at            TEXT NOT NULL,\n"
            + "  finished_at           TEXT,\n"
            + "  total_distance_meters INTEGER NOT NULL DEFAULT 0,\n"
            + "  total_time_seconds    INTEGER NOT NULL DEFAULT 0,\n"
            + "  avg_power_watts       INTEGER NOT NULL DEFAULT 0,\n"
            + "  avg_stroke_rate       REAL    NOT NULL DEFAULT 0,\n"
            + "  total_calories        INTEGER NOT NULL DEFAULT 0,\n"
            + "  strava_activity_id    TEXT,\n"
            + "  origin                TEXT    NOT NULL DEFAULT 'app'\n"
            + ")",
            "CREATE TABLE data_points (\n"
            + "  id                INTEGER PRIMARY KEY AUTOINCREMENT,\n"
            + "  session_id        INTEGER NOT NULL,\n"
            + "  elapsed_seconds   INTEGER NOT NULL,\n"
            + "  stroke_rate       REAL    NOT NULL DEFAULT 0,\n"
            + "  stroke_count      INTEGER NOT NULL DEFAULT 0,\n"
            + "  distance_meters   INTEGER NOT NULL DEFAULT 0,\n"
            + "  pace_500m_seconds INTEGER NOT NULL DEFAULT 0,\n"
            + "  power_watts       INTEGER NOT NULL DEFAULT 0,\n"
            + "  calories          INTEGER NOT NULL DEFAULT 0,\n"
            + "  heart_rate        INTEGER NOT NULL DEFAULT 0,\n"
            + "  step_index        INTEGER,\n"
            + "  step_type         TEXT,\n"
            + "  FOREIGN KEY (session_id) REFERENCES workout_sessions(id) ON DELETE CASCADE\n"
            + ")",
            "CREATE INDEX idx_data_points_session ON data_points(session_id, elapsed_seconds)",
        };

        // Base nueva: las 5 sentencias y user_version = 8 en UNA transacción (en modo rollback): un corte deja user_version 0
        // y la próxima apertura vuelve a crear (§2.3, paso 6).
        public static void Create(SQLiteConnection connection)
        {
            connection.RunInTransaction(() =>
            {
                for (var i = 0; i < Statements.Count; i++)
                {
                    connection.Execute(Statements[i]);
                }

                connection.Execute("PRAGMA user_version = " + Fmt.Integer(DatabaseFiles.SchemaVersion));
            });
        }

        // Pragmas de cada conexión de escritura, en este orden y SIEMPRE, aunque coincidan con el valor por defecto: los dos
        // SQLite no compilan igual (QA-G3-08). El de Unity trae SQLITE_DEFAULT_WAL_SYNCHRONOUS 1 (WAL en NORMAL): sin el
        // pragma, un commit podría perderse ante un corte de luz (DT-07, H-05). journal_mode = WAL queda en el archivo.
        public static void WritePragmas(SQLiteConnection connection)
        {
            connection.ExecuteScalar<string>("PRAGMA journal_mode = WAL");
            connection.Execute("PRAGMA synchronous = FULL");
            connection.Execute("PRAGMA foreign_keys = OFF");
            connection.BusyTimeout = TimeSpan.FromSeconds(5);
        }
    }
}
