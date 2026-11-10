using System;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.App.Composition
{
    // El IHaptics que arma AppComposer sobre el de la plataforma (It. 14, DT14-23): aplica HapticPolicy (250 ms entre dos de
    // cualquier origen, Success sobre Medium, nada en segundo plano) con el reloj monótono del programador y el ciclo de vida de
    // la app. Lo comparten DeviceStore ("Listo" y la reconexión) y WorkoutHaptics.
    public sealed class HapticGate : IHaptics
    {
        private readonly IHaptics _inner;
        private readonly IClock _clock;
        private readonly IAppLifecycle _lifecycle;
        private readonly HapticPolicy _policy = new HapticPolicy();

        public HapticGate(IHaptics inner, IClock clock, IAppLifecycle lifecycle)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public void Confirm()
        {
            Play(HapticKind.Confirm);
        }

        public void Play(HapticKind kind)
        {
            if (_policy.TryPlay(kind, _clock.MonotonicSeconds, _lifecycle.InBackground.Value))
            {
                _inner.Play(kind);
            }
        }
    }
}
