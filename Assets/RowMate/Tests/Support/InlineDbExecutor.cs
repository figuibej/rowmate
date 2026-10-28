using System;
using System.Threading.Tasks;
using RowMate.Infrastructure.Threading;

namespace RowMate.Tests.Support
{
    // Ejecutor de la base para pruebas (02-tech-design §2.6): corre cada trabajo EN el hilo que llama, sobre SQLite real, y
    // devuelve una tarea ya terminada (o fallada con la excepción del trabajo). Así cada cuadro de PlayMode y cada toma del
    // arnés son deterministas, y en dotnet los tests de repositorio no necesitan bombear un SynchronizationContext.
    // Después de Dispose, Run devuelve una tarea fallada con ObjectDisposedException (como SerialExecutor).
    public sealed class InlineDbExecutor : IDbExecutor
    {
        private bool _disposed;

        // Trabajos ejecutados (también los que fallaron).
        public int Runs { get; private set; }

        public bool IsDisposed => _disposed;

        public Task<T> Run<T>(Func<T> work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            if (_disposed)
            {
                return Task.FromException<T>(new ObjectDisposedException(nameof(InlineDbExecutor)));
            }

            Runs++;
            try
            {
                return Task.FromResult(work());
            }
            catch (Exception error)
            {
                return Task.FromException<T>(error);
            }
        }

        public Task Run(Action work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            return Run(() =>
            {
                work();
                return true;
            });
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
