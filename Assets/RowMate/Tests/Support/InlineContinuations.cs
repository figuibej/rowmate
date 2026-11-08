using System;
using System.Threading;

namespace RowMate.Tests.Support
{
    // Saca el SynchronizationContext del hilo mientras dura (y lo devuelve al disponerse), así las continuaciones de los await corren
    // en línea dentro de FakeScheduler.Advance también en el Unity Test Framework (It. 10a). En el EditMode de Unity el hilo del test
    // tiene el UnitySynchronizationContext; una tarea que se cancela desde un callback de CancellationTokenSource (el plazo de 15 s,
    // el fallo del escaneo) completa fuera de ese contexto y Mono postea la continuación para el próximo tick del Editor, cuando la
    // prueba ya miró el estado. En dotnet no hay contexto: da lo mismo. En la app, la continuación corre un cuadro después (ADR 005).
    public sealed class InlineContinuations : IDisposable
    {
        private readonly SynchronizationContext? _previous;
        private bool _disposed;

        public InlineContinuations()
        {
            _previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            SynchronizationContext.SetSynchronizationContext(_previous);
        }
    }
}
