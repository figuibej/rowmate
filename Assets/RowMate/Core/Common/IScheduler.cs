using System;
using System.Threading;
using System.Threading.Tasks;

namespace RowMate.Core.Common
{
    // Programador inyectable (ADR 005): equivalente a Timer, Timer.periodic y Future.delayed del legacy.
    // En los tests se usa FakeScheduler (Tests/Support); en Unity, UnityScheduler (Platform, It. 2).
    public interface IScheduler : IClock
    {
        // = Timer(delay, callback). Disponer el resultado cancela el timer.
        IDisposable Schedule(TimeSpan delay, Action callback);

        // = Timer.periodic(period, callback). Disponer el resultado lo detiene.
        IDisposable SchedulePeriodic(TimeSpan period, Action callback);

        // = Future.delayed(delay).
        Task Delay(TimeSpan delay, CancellationToken ct = default);

        // Verdadero mientras el programador alcanza el tiempo perdido en segundo plano (ADR 017):
        // quien escribe al hardware no debe escribir "en el pasado".
        bool IsCatchingUp { get; }
    }
}
