using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Text;
using RowMate.Infrastructure.Threading;
using SQLite;

namespace RowMate.Infrastructure.Database
{
    // La base local propia (F-HIS-17; 02-tech-design §2.3, §2.6). Abre en el primer uso (la carga del arranque) y conserva la
    // conexión; si la apertura falla, la próxima operación lo intenta de nuevo desde cero (Reintentar, AC-RUT-09-04). Todo
    // lo que toca SQLite corre en el ejecutor (el hilo de la base); la conexión solo se usa ahí.
    // Una base que no se entiende no se toca (AC-HIS-17-05):
    //  1. Si el archivo no existe → se crea (6).
    //  2. Encabezado (100 bytes, E/S común): 0 bytes → base vacía, se crea (6); 1 a 99 bytes, o sin la firma → Unreadable;
    //     user_version del encabezado > 8 → TooNew. Hasta acá SQLite no abrió el archivo.
    //  3. Sonda de SOLO LECTURA: user_version y la cantidad de objetos de sqlite_master. Lee lo que haya en el -wal (un -wal
    //     puede traer una versión más nueva que el encabezado). NotADb → Unreadable; otra falla → Io. Se cierra siempre.
    //  4. 8 → se abre (5); 0 sin tablas → se crea (6); > 8 → TooNew; 0 con tablas o 1 a 7 → Unrecognized (no hay migraciones:
    //     la base nace en v8, DT-09).
    //  5. Lectura y escritura sin Create, con los pragmas de DatabaseSchema. Abrir y leer no escriben (AC-HIS-17-04).
    //  6. Se crea en modo rollback (DDL + user_version en una transacción) y después se pasa a WAL con los pragmas.
    // Con Unreadable, TooNew o Unrecognized el archivo solo se abrió en solo lectura (o ni eso): mismos bytes al terminar, y
    // ningún archivo nuevo en la carpeta (el -wal vacío y el -shm que deja la sonda se quitan).
    // Ninguna ruta va al registro ni a los mensajes: solo el nombre del archivo (QA-G3-20).
    public sealed class RowMateDatabase : IDisposable
    {
        private const string Tag = "DB";

        // SQLITE_TRANSIENT: SQLite copia el texto al ligarlo.
        private static readonly IntPtr Transient = new IntPtr(-1);

        private readonly IDbExecutor _executor;
        private readonly IAppLogger _logger;
        private SQLiteConnection? _connection;
        private bool _disposed;

        public RowMateDatabase(string path, IDbExecutor executor, IAppLogger logger)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("RowMateDatabase: falta la ruta del archivo.", nameof(path));
            }

            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // La ruta completa: nunca va al registro (FileName sí).
        public string Path { get; }

        // Solo el nombre ("rower_app.db").
        public string FileName { get; }

        // El trabajo con la conexión de escritura, en el hilo de la base; abre (§2.3) si hace falta.
        internal Task<T> RunAsync<T>(Func<SQLiteConnection, T> work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            if (_disposed)
            {
                return Task.FromException<T>(new ObjectDisposedException(nameof(RowMateDatabase)));
            }

            return _executor.Run(() => work(EnsureOpen()));
        }

        // Semillas de prueba que la app no deja guardar (F-RARA) y triggers de falla (Tests/Support, por InternalsVisibleTo).
        // Argumentos: null, long/int, double o string.
        internal Task ExecuteRawForTestsAsync(string sql, params object?[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            // Mismo camino que la consulta: las filas que devuelva (un PRAGMA) se descartan.
            return QueryRawForTestsAsync(sql, args ?? new object?[0]);
        }

        // Solo para tests (QA-G3-07): las filas de cualquier consulta, sobre la MISMA conexión de escritura (abre si hace
        // falta). Cada celda vuelve como long, double, string, byte[] o null según el tipo de almacenamiento de SQLite: ningún
        // tipo de sqlite-net sale de Infrastructure. La API de bajo nivel tiene los mismos nombres en sqlite-net-pcl y en el
        // SQLite.cs de gilzoide, pero el handle es de otro tipo en cada una: por eso va en `var` y nunca se nombra.
        internal Task<IReadOnlyList<object?[]>> QueryRawForTestsAsync(string sql, params object?[] args)
        {
            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            var values = args ?? new object?[0];
            return RunAsync<IReadOnlyList<object?[]>>(connection =>
            {
                var rows = new List<object?[]>();
                var statement = SQLite3.Prepare2(connection.Handle, sql);
                try
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        var value = values[i];
                        var index = i + 1;
                        if (value == null)
                        {
                            SQLite3.BindNull(statement, index);
                        }
                        else if (value is long || value is int || value is short || value is byte)
                        {
                            SQLite3.BindInt64(statement, index, Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else if (value is double || value is float)
                        {
                            SQLite3.BindDouble(statement, index, Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else if (value is string text)
                        {
                            SQLite3.BindText(statement, index, text, -1, Transient);
                        }
                        else
                        {
                            throw new ArgumentException("RowMateDatabase: tipo de argumento no admitido: " + value.GetType().Name, nameof(args));
                        }
                    }

                    while (true)
                    {
                        var result = SQLite3.Step(statement);
                        if (result == SQLite3.Result.Done)
                        {
                            break;
                        }

                        if (result != SQLite3.Result.Row)
                        {
                            throw SQLiteException.New(result, SQLite3.GetErrmsg(connection.Handle));
                        }

                        var count = SQLite3.ColumnCount(statement);
                        var row = new object?[count];
                        for (var c = 0; c < count; c++)
                        {
                            switch (SQLite3.ColumnType(statement, c))
                            {
                                case SQLite3.ColType.Integer:
                                    row[c] = SQLite3.ColumnInt64(statement, c);
                                    break;
                                case SQLite3.ColType.Float:
                                    row[c] = SQLite3.ColumnDouble(statement, c);
                                    break;
                                case SQLite3.ColType.Text:
                                    row[c] = SQLite3.ColumnString(statement, c);
                                    break;
                                case SQLite3.ColType.Blob:
                                    row[c] = SQLite3.ColumnByteArray(statement, c);
                                    break;
                                default:
                                    row[c] = null;
                                    break;
                            }
                        }

                        rows.Add(row);
                    }
                }
                finally
                {
                    SQLite3.Finalize(statement);
                }

                return rows;
            });
        }

        // Espera acotada a que el ejecutor termine lo encolado (It. 4a: al pasar a segundo plano, AC-WKT-06-15). Después de
        // Dispose, true (Dispose ya terminó lo encolado).
        public bool WaitForPendingWork(TimeSpan timeout)
        {
            if (_disposed)
            {
                return true;
            }

            return _executor.WaitIdle(timeout);
        }

        // Cierra la conexión EN el hilo de la base y dispone el ejecutor (que termina lo encolado: un guardado en vuelo se
        // completa antes). Idempotente.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            var close = _executor.Run(() =>
            {
                var connection = _connection;
                _connection = null;
                connection?.Dispose();
            });
            _executor.Dispose();
            if (close.IsFaulted && !(close.Exception?.InnerException is ObjectDisposedException))
            {
                _logger.Log(LogLevel.Warning, Tag, "cierre de " + FileName + ": " + close.Exception?.InnerException?.GetType().Name);
            }
        }

        private SQLiteConnection EnsureOpen()
        {
            if (_connection == null)
            {
                _connection = Open();
            }

            return _connection;
        }

        private SQLiteConnection Open()
        {
            if (!File.Exists(Path))
            {
                return Create();
            }

            DatabaseHeader header;
            try
            {
                header = DatabaseHeader.Read(Path);
            }
            catch (IOException)
            {
                throw Problem(DatabaseProblem.Io, null);
            }
            catch (UnauthorizedAccessException)
            {
                throw Problem(DatabaseProblem.Io, null);
            }

            if (header.IsEmpty)
            {
                return Create();
            }

            if (!header.IsComplete || !header.HasSignature)
            {
                throw Problem(DatabaseProblem.Unreadable, null);
            }

            if (header.UserVersion > DatabaseFiles.SchemaVersion)
            {
                throw Problem(DatabaseProblem.TooNew, null);
            }

            // La sonda de solo lectura de una base WAL que se cerró bien crea un -wal vacío y un -shm (memoria compartida):
            // medido en los dos SQLite. Si no se va a abrir la base, se quitan los que no estaban (RemoveProbeLeftovers).
            var hadWal = File.Exists(Path + "-wal");
            var hadShm = File.Exists(Path + "-shm");
            int version;
            long objects;
            try
            {
                using (var probe = Connect(SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.FullMutex))
                {
                    version = probe.ExecuteScalar<int>("PRAGMA user_version");
                    objects = probe.ExecuteScalar<long>("SELECT count(*) FROM sqlite_master");
                }
            }
            catch (SQLiteException error)
            {
                RemoveProbeLeftovers(hadWal, hadShm);
                throw Problem(error.Result == SQLite3.Result.NonDBFile ? DatabaseProblem.Unreadable : DatabaseProblem.Io, (int)error.Result);
            }

            if (version == DatabaseFiles.SchemaVersion)
            {
                return OpenExisting();
            }

            if (version == 0 && objects == 0)
            {
                return Create();
            }

            RemoveProbeLeftovers(hadWal, hadShm);
            throw Problem(version > DatabaseFiles.SchemaVersion ? DatabaseProblem.TooNew : DatabaseProblem.Unrecognized, null);
        }

        // Una base que no se abre queda como estaba, también la carpeta: se borran el -wal (solo si está vacío: no tiene
        // cuadros) y el -shm que creó la sonda, si antes no existían. Con la sonda ya cerrada nadie de la app los tiene abiertos;
        // si otro proceso los usa, Windows no deja borrarlos y quedan (nunca se pierde contenido).
        private void RemoveProbeLeftovers(bool hadWal, bool hadShm)
        {
            var wal = Path + "-wal";
            var shm = Path + "-shm";
            try
            {
                if (!hadWal && File.Exists(wal) && new FileInfo(wal).Length == 0)
                {
                    File.Delete(wal);
                }

                if (!hadShm && File.Exists(shm) && !File.Exists(wal))
                {
                    File.Delete(shm);
                }
            }
            catch (IOException)
            {
                // en uso: se dejan
            }
            catch (UnauthorizedAccessException)
            {
                // sin permiso: se dejan
            }
        }

        // §2.3, paso 5.
        private SQLiteConnection OpenExisting()
        {
            SQLiteConnection? connection = null;
            try
            {
                connection = Connect(SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
                DatabaseSchema.WritePragmas(connection);
            }
            catch (SQLiteException error)
            {
                connection?.Dispose();
                throw Problem(error.Result == SQLite3.Result.NonDBFile ? DatabaseProblem.Unreadable : DatabaseProblem.Io, (int)error.Result);
            }

            _logger.Log(LogLevel.Info, Tag, "base " + FileName + " abierta (v" + Fmt.Integer(DatabaseFiles.SchemaVersion) + ")");
            return connection;
        }

        // §2.3, paso 6.
        private SQLiteConnection Create()
        {
            try
            {
                var folder = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            catch (IOException)
            {
                throw Problem(DatabaseProblem.Io, null);
            }
            catch (UnauthorizedAccessException)
            {
                throw Problem(DatabaseProblem.Io, null);
            }

            SQLiteConnection? connection = null;
            try
            {
                connection = Connect(SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                DatabaseSchema.Create(connection);
                DatabaseSchema.WritePragmas(connection);
            }
            catch (SQLiteException error)
            {
                connection?.Dispose();
                throw Problem(error.Result == SQLite3.Result.NonDBFile ? DatabaseProblem.Unreadable : DatabaseProblem.Io, (int)error.Result);
            }

            _logger.Log(LogLevel.Info, Tag, "base " + FileName + " creada (v" + Fmt.Integer(DatabaseFiles.SchemaVersion) + ")");
            return connection;
        }

        // sqlite-net pasa la ruta a UTF-8 antes de sqlite3_open_v2 (una carpeta con ñ o tildes, como un perfil de Windows).
        private SQLiteConnection Connect(SQLiteOpenFlags flags)
        {
            return new SQLiteConnection(new SQLiteConnectionString(Path, flags, false));
        }

        private DatabaseOpenException Problem(DatabaseProblem problem, int? sqliteCode)
        {
            return new DatabaseOpenException(problem, sqliteCode, FileName);
        }
    }
}
