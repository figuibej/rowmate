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
    }
}
