using System;
using System.Threading.Tasks;

namespace RowMate.Infrastructure.Threading
{
    // Dónde corre el trabajo de la base (ADR 005; 02-tech-design §2.6). Devuelve Task: quien hace await desde el hilo
    // principal vuelve al hilo principal por el SynchronizationContext capturado (UnitySynchronizationContext en la app;
    // TestMainThreadContext en dotnet). Nadie en Core, App ni Infrastructure usa ConfigureAwait(false) (PureCodeRulesTests).
    // En la app, SerialExecutor (un hilo propio con cola FIFO); en pruebas de pantalla y en el arnés, InlineDbExecutor
    // (Tests/Support: en el hilo que llama, determinista cuadro a cuadro).
    public interface IDbExecutor : IDisposable
    {
        Task<T> Run<T>(Func<T> work);

        Task Run(Action work);

        // Espera acotada a que se termine todo lo encolado hasta ahora (It. 4a: al pasar a segundo plano, AC-WKT-06-15).
        // Bloquea al que llama, que no es el hilo de la base. true = quedó todo hecho antes del tope.
        bool WaitIdle(TimeSpan timeout);
    }
}
