using System;
using RowMate.Core.Text;

namespace RowMate.Infrastructure.Database
{
    // La base no se pudo abrir (AC-HIS-17-05, AC-RUT-09-04): el problema, el código de SQLite (si lo hubo) y el NOMBRE del
    // archivo, nunca su carpeta (QA-G3-20: el repo es público y los informes copian el registro). Por eso no lleva la
    // SQLiteException original como InnerException: sqlite-net le pone la ruta completa al mensaje al fallar la apertura
    // ("Could not open database file: <ruta> (…)").
    public sealed class DatabaseOpenException : Exception
    {
        public DatabaseOpenException(DatabaseProblem problem, int? sqliteCode, string fileName)
            : base(Describe(problem, sqliteCode, fileName))
        {
            Problem = problem;
            SqliteCode = sqliteCode;
            FileName = fileName;
        }

        public DatabaseProblem Problem { get; }

        // El código de resultado de SQLite (26 = NotADb), o null si la falla no vino de SQLite.
        public int? SqliteCode { get; }

        // Solo el nombre ("rower_app.db").
        public string FileName { get; }

        private static string Describe(DatabaseProblem problem, int? sqliteCode, string fileName)
        {
            return "no se pudo abrir " + fileName + ": " + problem + (sqliteCode.HasValue ? " (SQLite " + Fmt.Integer(sqliteCode.Value) + ")" : "");
        }
    }
}
