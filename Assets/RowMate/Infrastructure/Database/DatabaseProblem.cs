namespace RowMate.Infrastructure.Database
{
    // Por qué no se pudo abrir la base (AC-HIS-17-05; 02-tech-design §2.3). En los tres primeros casos el archivo solo se
    // abrió en solo lectura (o ni eso): queda con los mismos bytes, también su -wal.
    public enum DatabaseProblem
    {
        // No es una base de SQLite (F-ROTA, un archivo de 1 a 99 bytes, o SQLite dice NotADb).
        Unreadable,

        // user_version mayor que 8 (de un build más nuevo), en el encabezado o en el WAL.
        TooNew,

        // Una base que la app no reconoce como suya: user_version 1 a 7 (como la del legacy) o 0 con tablas (DT-09).
        Unrecognized,

        // Cualquier otra falla de E/S o de SQLite al abrir (bloqueada, sin permiso, disco).
        Io,
    }
}
