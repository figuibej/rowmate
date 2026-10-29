using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Routines;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.UiSupport
{
    // Las rutinas de un UiTestHost (03-rutinas/02-tech-design §5.2): el repositorio y lo que haya que liberar al final (la base
    // temporal, que se borra). Reopen() lo vuelve a abrir sobre el MISMO archivo (reabrir la app: DEMO-10, AC-RUT-08-09).
    // It. 4a (02-tech-design §3.5, §7.1): con SQLite el repositorio es el de un LocalStore (TempDatabase.Store), así las
    // sesiones del motor van al MISMO archivo, con la misma conexión y el mismo ejecutor que las rutinas, como en la app
    // (HostSessions.SameFileAsRoutines).
    public sealed class HostRoutineSet : IDisposable
    {
        private readonly Func<IAppLogger, IRoutineRepository>? _reopen;
        private readonly Func<IAppLogger, LocalStore>? _reopenStore;
        private readonly TempDatabase? _ownedDatabase;
        private bool _disposed;

        public HostRoutineSet(IRoutineRepository repository, TempDatabase? database = null, string? filePath = null,
                              Func<IAppLogger, IRoutineRepository>? reopen = null, bool ownsDatabase = true)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Database = database;
            FilePath = filePath;
            _reopen = reopen;
            _ownedDatabase = ownsDatabase ? database : null;
        }

        // Rutinas y sesiones sobre la misma base (LocalStore), como la app.
        public HostRoutineSet(LocalStore store, TempDatabase? database, string? filePath, Func<IAppLogger, LocalStore>? reopen,
                              bool ownsDatabase = true)
            : this((store ?? throw new ArgumentNullException(nameof(store))).Routines, database, filePath, null, ownsDatabase)
        {
            Store = store;
            _reopenStore = reopen;
        }

        public IRoutineRepository Repository { get; private set; }

        // La base de la app armada sobre el archivo (null con un repositorio en memoria o de guion).
        public LocalStore? Store { get; private set; }

        // La carpeta temporal (null con un repositorio en memoria o de guion).
        public TempDatabase? Database { get; }

        // El archivo de la base (null sin SQLite).
        public string? FilePath { get; }

        public ScriptedRoutineRepository? Scripted => Repository as ScriptedRoutineRepository;

        public InMemoryRoutineRepository? Memory => Repository as InMemoryRoutineRepository;

        public SqliteRoutineRepository? Sqlite => Repository as SqliteRoutineRepository;

        public bool CanReopen => _reopen != null || _reopenStore != null;

        // Cierra el repositorio (y su base) y abre otro sobre el mismo archivo.
        public IRoutineRepository Reopen(IAppLogger logger)
        {
            if (_reopenStore != null)
            {
                Store?.Dispose();
                Store = _reopenStore(logger);
                Repository = Store.Routines;
                return Repository;
            }

            if (_reopen == null)
            {
                throw new InvalidOperationException("HostRoutineSet: este repositorio no se puede reabrir (no es SQLite).");
            }

            (Repository as IDisposable)?.Dispose();
            Repository = _reopen(logger);
            return Repository;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (Store != null)
            {
                Store.Dispose();
            }
            else
            {
                (Repository as IDisposable)?.Dispose();
            }

            _ownedDatabase?.Dispose();
        }
    }

    // Fábrica de las rutinas del host, con el reloj y el registro del host (QA-G2-05: el host no elige entre variantes fijas).
    public delegate HostRoutineSet HostRoutineFactory(IScheduler scheduler, IAppLogger logger);

    // Fábricas de repositorio de prueba (03-rutinas/02-tech-design §5.2). Ninguna toca la carpeta de datos de la app
    // (TempDatabase tiene su guardia, AC-HIS-20-04).
    public static class HostRoutines
    {
        // En memoria y vacío: el valor por defecto (las pruebas de la It. 2 no usan rutinas).
        public static readonly HostRoutineFactory Empty = (scheduler, logger) => new HostRoutineSet(new InMemoryRoutineRepository());

        public static HostRoutineFactory InMemory(IEnumerable<Routine>? initial = null)
        {
            return (scheduler, logger) => new HostRoutineSet(new InMemoryRoutineRepository(initial));
        }

        // TempDatabase + semilla + LocalStore con InlineDbExecutor (SQLite real, determinista cuadro a cuadro). Las sesiones
        // del motor van al mismo archivo (It. 4a).
        public static HostRoutineFactory Seeded(RoutineSeed seed, string fileName = TempDatabase.DefaultFileName)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            return (scheduler, logger) =>
            {
                var database = new TempDatabase(fileName);
                try
                {
                    var store = database.Store(logger, null, fileName);
                    RoutineSeeds.Apply(store.Routines, seed);
                    var path = database.PathOf(fileName);
                    return new HostRoutineSet(store, database, path, l => database.Store(l, null, fileName));
                }
                catch
                {
                    database.Dispose();
                    throw;
                }
            };
        }

        // F-ROTA: un archivo con el nombre de la base que no es una base de SQLite (el texto queda igual: lo mira CAP-03).
        public static HostRoutineFactory Rota(string fileName = TempDatabase.DefaultFileName)
        {
            return (scheduler, logger) =>
            {
                var database = new TempDatabase(fileName);
                var path = database.PathOf(fileName);
                RoutineSeeds.WriteRota(path);
                return new HostRoutineSet(database.Store(logger, null, fileName), database, path, l => database.Store(l, null, fileName));
            };
        }

        // La base lenta o que falla a pedido (LoadDelay, SaveDelay con el FakeScheduler; Never = no termina).
        public static HostRoutineFactory Scripted(Action<ScriptedRoutineRepository>? configure = null, IEnumerable<Routine>? initial = null)
        {
            return (scheduler, logger) =>
            {
                var repository = new ScriptedRoutineRepository(scheduler, initial);
                configure?.Invoke(repository);
                return new HostRoutineSet(repository);
            };
        }

        // Otro archivo dentro de una carpeta de prueba que ya existe (las dos variantes sobre la misma carpeta:
        // RPS.VariantsDoNotMix). La carpeta la libera quien la creó.
        public static HostRoutineFactory InFolder(TempDatabase database, string fileName)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            return (scheduler, logger) => new HostRoutineSet(database.Store(logger, null, fileName), database, database.PathOf(fileName),
                l => database.Store(l, null, fileName), false);
        }

        // El ejecutor real de la app (un hilo propio), para rendimiento (PRF) y durabilidad: rutinas y sesiones en el mismo
        // LocalStore con un SerialExecutor.
        public static HostRoutineFactory Serial(RoutineSeed seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            return (scheduler, logger) =>
            {
                var database = new TempDatabase();
                try
                {
                    using (var seeding = database.Repository(logger))
                    {
                        RoutineSeeds.Apply(seeding, seed);
                    }

                    var store = database.Store(logger, new SerialExecutor("RowMate.Db.Test", logger));
                    return new HostRoutineSet(store, database, database.FilePath,
                        l => database.Store(l, new SerialExecutor("RowMate.Db.Test", l)));
                }
                catch
                {
                    database.Dispose();
                    throw;
                }
            };
        }
    }
}
