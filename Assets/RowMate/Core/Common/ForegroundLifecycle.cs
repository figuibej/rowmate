using RowMate.Core.Ports;

namespace RowMate.Core.Common
{
    // El objeto nulo del ciclo de vida (02-tech-design §4.2): siempre adelante y con foco, nunca avisa (tampoco un despertar:
    // It. 4b, NeverWakes). Lo usan AppComposer cuando AppServices no trae ciclo de vida (dotnet, pruebas de pantalla que no lo
    // prueban) y quien no necesita otro.
    public sealed class ForegroundLifecycle : IAppLifecycle
    {
        public static readonly ForegroundLifecycle Instance = new ForegroundLifecycle();

        private ForegroundLifecycle()
        {
            InBackground = new ReadOnlyObservable<bool>(new ObservableValue<bool>(false, NullAppLogger.Instance));
            HasFocus = new ReadOnlyObservable<bool>(new ObservableValue<bool>(true, NullAppLogger.Instance));
        }

        public IReadOnlyObservable<bool> InBackground { get; }

        public IReadOnlyObservable<bool> HasFocus { get; }

        public bool IsUserFacing => true;

        public IEventSource<SystemWake> Woke => NeverWakes.Instance;
    }
}
