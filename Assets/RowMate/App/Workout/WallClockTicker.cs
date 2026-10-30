using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // El estado del reloj de pared (F-WKT-19): el texto HH:mm, el minuto del día (hora·60 + minuto, para el sentido del
    // rodillo: hacia arriba, y hacia abajo a medianoche) y si va sin rodillo. Instant: el cambio no fue "el minuto siguiente"
    // (cambio de zona o de hora a mano, despertar, vuelta del segundo plano, primer estado).
    public sealed record WallClockState(string Text, int MinuteOfDay, bool Instant);

    // El reloj de pared, con su propio ritmo (series_panels.dart:57-82: un Timer.periodic de 1 s que redibuja solo cuando cambia
    // el minuto; 02-tech-design §2.8 de la It. 4b). No depende del reloj de la sesión ni lo mueve; sigue en pausa, terminada y
    // con el diálogo abierto (AC-WKT-19-03); la hora es la del equipo (IScheduler.LocalNow, con la zona que lee Platform), en
    // HH:mm de 24 h siempre (WorkoutFormats.WallClock, Q-06). Un periódico de 250 ms (el legacy mira cada 1 s; así el minuto
    // nuevo se ve antes de 1 s, AC-WKT-19-02) compara la hora y el minuto con los últimos publicados, sin asignar. Sin periódico
    // en segundo plano: el alcance no lo dispara (QA-G4bT-08 (c)); al volver y al despertar publica una vez, con Instant. Lo crea
    // y lo dispone el store del HUD, uno por inmersivo.
    public sealed class WallClockTicker : IDisposable
    {
        public const double CheckPeriodSeconds = 0.25;
        public const string Tag = "WORKOUT";

        private readonly IScheduler _scheduler;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<WallClockState> _state;
        private readonly IDisposable _backgroundSubscription;
        private readonly IDisposable _wokeSubscription;
        private readonly Action _onCheck;
        private IDisposable? _periodic;
        private int _minute;
        private bool _disposed;

        public WallClockTicker(IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onCheck = Check;
            var now = scheduler.LocalNow;
            _minute = MinuteOf(now);
            _state = new ObservableValue<WallClockState>(new WallClockState(WorkoutFormats.WallClock(now), _minute, true), logger);
            State = new ReadOnlyObservable<WallClockState>(_state);
            _backgroundSubscription = lifecycle.InBackground.Subscribe(OnBackground, emitCurrent: false);
            _wokeSubscription = lifecycle.Woke.Subscribe(_ => PublishNow());
            if (!lifecycle.InBackground.Value)
            {
                Arm();
            }
        }

        // Publica SOLO cuando cambia el HH:mm (no "late" cada segundo), y una vez al volver o al despertar.
        public IReadOnlyObservable<WallClockState> State { get; }

        // Hay periódico (no en segundo plano ni dispuesto): para las pruebas.
        public bool IsTicking => _periodic != null;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _periodic?.Dispose();
            _periodic = null;
            _backgroundSubscription.Dispose();
            _wokeSubscription.Dispose();
        }

        // hora·60 + minuto.
        public static int MinuteOf(DateTimeOffset local)
        {
            return local.Hour * 60 + local.Minute;
        }

        private void Arm()
        {
            _periodic?.Dispose();
            _periodic = _scheduler.SchedulePeriodic(TimeSpan.FromSeconds(CheckPeriodSeconds), _onCheck);
        }

        private void OnBackground(bool inBackground)
        {
            if (_disposed)
            {
                return;
            }

            if (inBackground)
            {
                _periodic?.Dispose();
                _periodic = null;
                return;
            }

            Arm();
            PublishNow();
        }

        // El periódico: solo si cambió el minuto. Sin rodillo si no es el siguiente (cambio de zona o de hora, AC-WKT-19-04).
        private void Check()
        {
            if (_disposed)
            {
                return;
            }

            var now = _scheduler.LocalNow;
            var minute = MinuteOf(now);
            if (minute == _minute)
            {
                return;
            }

            var next = (_minute + 1) % (24 * 60);
            var instant = minute != next;
            if (instant)
            {
                _logger.Log(LogLevel.Debug, Tag, "reloj de pared: salto de hora");
            }

            _minute = minute;
            _state.Value = new WallClockState(WorkoutFormats.WallClock(now), minute, instant);
        }

        // Al volver del segundo plano o al despertar (AC-WKT-19-03): la hora actual, sin rodillo.
        private void PublishNow()
        {
            if (_disposed)
            {
                return;
            }

            var now = _scheduler.LocalNow;
            _minute = MinuteOf(now);
            _state.Value = new WallClockState(WorkoutFormats.WallClock(now), _minute, true);
        }
    }
}
