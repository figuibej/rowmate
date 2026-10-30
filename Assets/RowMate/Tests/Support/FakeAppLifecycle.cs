using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Ciclo de vida de prueba (spec §0.4; 02-tech-design §4.5 de la It. 4a): la prueba pasa la app a segundo plano y la trae
    // de vuelta, y le saca y le devuelve el foco de la ventana. Sin un pedido, adelante y con foco (también en las corridas
    // sin ventana). Como el AppLifecycle de Platform: dos ObservableValue<bool> que avisan solo si cambian.
    // It. 4b (P-17 de la 4a; §2.9): + Woke, que avisa SignalWake (LifecycleHarness lo conecta con FrameScheduler.Woke).
    public sealed class FakeAppLifecycle : IAppLifecycle
    {
        private readonly ObservableValue<bool> _inBackground;
        private readonly ObservableValue<bool> _hasFocus;
        private readonly EventStream<SystemWake> _woke;

        public FakeAppLifecycle(IAppLogger? logger = null)
        {
            var log = logger ?? NullAppLogger.Instance;
            _inBackground = new ObservableValue<bool>(false, log);
            _hasFocus = new ObservableValue<bool>(true, log);
            _woke = new EventStream<SystemWake>(log);
            InBackground = new ReadOnlyObservable<bool>(_inBackground);
            HasFocus = new ReadOnlyObservable<bool>(_hasFocus);
        }

        public IReadOnlyObservable<bool> InBackground { get; }

        public IReadOnlyObservable<bool> HasFocus { get; }

        public bool IsUserFacing => !_inBackground.Value && _hasFocus.Value;

        public IEventSource<SystemWake> Woke => _woke;

        // Cuántos despertares avisó.
        public int WakeCount { get; private set; }

        public void SetBackground(bool inBackground)
        {
            _inBackground.Value = inBackground;
        }

        public void SetFocus(bool hasFocus)
        {
            _hasFocus.Value = hasFocus;
        }

        // El equipo despertó (como AppLifecycle.SignalWake de Platform).
        public void SignalWake(SystemWake wake)
        {
            WakeCount++;
            _woke.Emit(wake);
        }
    }
}
