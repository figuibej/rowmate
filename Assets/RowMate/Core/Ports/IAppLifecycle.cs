using RowMate.Core.Common;

namespace RowMate.Core.Ports
{
    // Ciclo de vida de la app (ARCHITECTURE §13; ADR 017; 02-tech-design §4.2 de la It. 4a). Segundo plano = Android o iOS con
    // la actividad pausada (el loop se detiene). En escritorio, la ventana minimizada o sin foco NO es segundo plano: el loop
    // sigue (runInBackground). El foco es el de la ventana (en Windows, minimizar también lo saca). "El usuario la ve" =
    // adelante y con foco (spec §0.4, §12 punto 13): sin el usuario delante, los avisos se retienen (WorkoutStore).
    // En la app lo mueve AppRoot (Platform/Lifecycle/AppLifecycle); en dotnet y en las pruebas que no lo usan,
    // ForegroundLifecycle.Instance (siempre adelante y con foco); FakeAppLifecycle en las que sí (Tests/Support).
    public interface IAppLifecycle
    {
        IReadOnlyObservable<bool> InBackground { get; }

        IReadOnlyObservable<bool> HasFocus { get; }

        // !InBackground.Value && HasFocus.Value.
        bool IsUserFacing { get; }

        // It. 4b (P-17 de la 4a; 02-tech-design §2.9.4; ADR 020): el equipo despertó (FrameScheduler.Woke, en el cuadro que lo
        // trae, después de correr lo vencido). El motor publica una foto Instant y el reloj de pared salta sin rodillo. Por
        // defecto nunca avisa (NeverWakes: Android, iOS y quien no tiene la señal); AppLifecycle de Platform la implementa en
        // WP-B2 y FakeAppLifecycle en las pruebas. La implementación por defecto deja compilar a Platform hasta entonces.
        IEventSource<SystemWake> Woke => NeverWakes.Instance;
    }
}
