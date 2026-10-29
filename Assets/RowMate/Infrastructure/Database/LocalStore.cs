using System;
using RowMate.Core.Common;
using RowMate.Infrastructure.Routines;
using RowMate.Infrastructure.Sessions;

namespace RowMate.Infrastructure.Database
{
    // La base local de la app (02-tech-design §3.5 de la It. 4a): una RowMateDatabase (una conexión, un SerialExecutor) para
    // rutinas y sesiones. AppRoot y las pruebas la crean y la disponen al final, después de la app y del backend: el ejecutor
    // termina lo encolado (también lo que mandó un motor que se dispuso con una sesión en curso) y cierra la conexión en su
    // hilo (It. 3 §2.6). Los repositorios no son dueños de la base.
    public sealed class LocalStore : IDisposable
    {
        private bool _disposed;

        public LocalStore(RowMateDatabase database, IAppLogger logger)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Routines = new SqliteRoutineRepository(database, logger, ownsDatabase: false);
            Sessions = new SqliteSessionRepository(database, logger);
        }

        public RowMateDatabase Database { get; }

        public SqliteRoutineRepository Routines { get; }

        public SqliteSessionRepository Sessions { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Routines.Dispose();
            Database.Dispose();
        }
    }
}
