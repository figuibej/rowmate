using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Tests.Support;

namespace RowMate.Tests.UiSupport
{
    // Las sesiones de un UiTestHost (02-tech-design §7.1 de la It. 4a), con el reloj y el registro del host y sus rutinas. El
    // repositorio lo libera quien lo creó: el LocalStore de las rutinas (SameFileAsRoutines) o nadie (en memoria y de guion,
    // sin recursos).
    public delegate ISessionRepository HostSessionFactory(IScheduler scheduler, IAppLogger logger, HostRoutineSet routines);

    public static class HostSessions
    {
        // Por defecto: el mismo archivo (y la misma conexión y ejecutor) que las rutinas si son SQLite, como la app
        // (LocalStore); en memoria si no.
        public static readonly HostSessionFactory SameFileAsRoutines = (scheduler, logger, routines) =>
            routines.Store != null ? routines.Store.Sessions : (ISessionRepository)new InMemorySessionRepository();

        // En memoria (CAP-16: el remo de prueba con los valores largos, sin base).
        public static readonly HostSessionFactory InMemory = (scheduler, logger, routines) => new InMemorySessionRepository();

        // La base de guion: lenta o con fallas a pedido al crear, al guardar un lote o al cerrar (CAP-29, WPS).
        public static HostSessionFactory Scripted(Action<ScriptedSessionRepository>? configure = null)
        {
            return (scheduler, logger, routines) =>
            {
                var repository = new ScriptedSessionRepository(scheduler);
                configure?.Invoke(repository);
                return repository;
            };
        }
    }
}
