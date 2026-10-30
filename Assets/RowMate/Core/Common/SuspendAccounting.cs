using System;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // P-17 de la 4a (AC-WKT-02-11, -04; 02-tech-design §2.9 de la It. 4b; ADR 020): el reloj "despierto" de la app. Entre el
    // monótono crudo (el Stopwatch, que puede contar la suspensión del equipo o no: no está medido, QA-V4a-ROB-05) y
    // FrameScheduler, resta lo dormido que el crudo contó y avisa el despertar. Como todo (el motor, el remo simulado, los topes
    // de espera, el reloj de pared y el motion) mide con este reloj, el lapso dormido no existe para la app: sin segundos, sin
    // puntos, sin lecturas de objetivos y sin falsa demora del cierre. El legacy cuenta disparos de un Timer.periodic(1 s)
    // (workout_provider.dart:147, :247) que no corre con el equipo dormido: el lapso tampoco existe para él.
    // Pura: recibe todo por argumento (ni Stopwatch ni la hora del sistema). La usa FrameScheduler en el cuadro y en la vuelta
    // del segundo plano, con el mismo Awake (QA-G4bT-05 (a)). Hilo principal.
    //   1. Con medida del sistema (ΔAsleep ≥ 0,5 s): durmió ΔAsleep. Si ΔRaw ≥ 0,9·ΔAsleep el crudo lo contó y se restan
    //      min(ΔAsleep; ΔRaw); si no, no se resta nada. Despertar con Measure (y Signal si había un aviso pendiente, que se cierra).
    //   2. Sin medida, con un "se suspende" pendiente: todo cuadro con ΔRaw ≥ 1 s se descuenta como sueño (ΔRaw menos un cuadro
    //      nominal) y avisa un despertar con Signal; el aviso no se consume con ese cuadro (un tirón antes del sueño de verdad se
    //      descuenta y el sueño de después también, QA-G4bT-05 (b)). Un salto de la hora de pared (ΔUtc − ΔRaw ≥ 0,5 s) con un
    //      ΔRaw normal: el crudo no contó, nada que restar, despertar con Signal | WallJump. El primer cuadro con un "reanuda"
    //      nuevo cierra el pendiente, después de aplicar lo de ese cuadro; si ningún cuadro mostró el sueño, ese es el
    //      despertar (Signal, 0 descontado). Un "se suspende" sin sueño ni "reanuda" en 30 s despiertos se descarta y se anota.
    //   3. Sin medida ni aviso: nada. Un cuadro tarde es un cuadro tarde (AC-WKT-02-04, la conducta de la 4a) y se anota desde
    //      30 s; un salto de la hora de pared sin aviso es un cambio de hora (AC-WKT-19-04), no una suspensión.
    // El monótono despierto nunca baja.
    public sealed class SuspendAccounting
    {
        public const string Tag = "TIME";

        // Menos es ruido (los contadores del sistema van de a 15,6 ms).
        public const double MinSleepSeconds = 0.5;

        // El crudo "contó" el sueño si avanzó al menos el 90 % de lo dormido.
        public const double CountedRatio = 0.9;

        // Con un aviso pendiente, un cuadro de 1 s o más es sueño.
        public const double SignalGapSeconds = 1;

        // Un aviso sin sueño ni "reanuda" después (cancelado) se descarta a los 30 s despiertos.
        public const double SignalExpirySeconds = 30;

        // Lo que se deja del cuadro que trae el sueño (un cuadro a 60 fps).
        public const double NominalFrameSeconds = 1.0 / 60;

        // Un cuadro tarde sin evidencia cuenta (la 4a) y, desde 30 s, queda en el registro.
        public const double LateFrameLogSeconds = 30;

        private readonly IAppLogger _logger;
        private double _lastRaw;
        private DateTimeOffset _lastUtc;
        private double _lastAsleep;
        private int _lastSuspend;
        private int _lastResume;
        private double _bias;
        private double _awake;
        private bool _pending;
        private bool _pendingReported;
        private double _pendingSince;

        public SuspendAccounting(in FrameClock first, IAppLogger? logger = null)
        {
            _logger = logger ?? NullAppLogger.Instance;
            _lastRaw = first.RawMonotonic;
            _lastUtc = first.Utc;
            _lastAsleep = first.SystemAsleepSeconds;
            _lastSuspend = first.SuspendSignals;
            _lastResume = first.ResumeSignals;
            _awake = first.RawMonotonic;
        }

        // Segundos restados en total.
        public double Bias => _bias;

        // Llegó un "se suspende" y todavía no se cerró con un "reanuda" (o con la medida).
        public bool SuspendPending => _pending;

        // El último monótono despierto devuelto.
        public double Current => _awake;

        // El monótono despierto de este cuadro (o de la vuelta del segundo plano). woke: el despertar, si lo hubo.
        public double Awake(in FrameClock clock, out SystemWake? woke)
        {
            return Awake(clock, out woke, false);
        }

        // fromBackground: lo llama la vuelta del segundo plano (FrameScheduler.ResumeFromBackground), no un cuadro. La conducta es
        // la misma; cambia solo lo que se anota en el caso 3 (QA-V4b-MOT-04): el segundo plano normal cuenta (Q-91) y no es un
        // "cuadro tarde".
        public double Awake(in FrameClock clock, out SystemWake? woke, bool fromBackground)
        {
            woke = null;
            var raw = clock.RawMonotonic;
            var dRaw = raw - _lastRaw;
            if (double.IsNaN(dRaw) || dRaw < 0)
            {
                dRaw = 0;
            }

            var dUtc = (clock.Utc - _lastUtc).TotalSeconds;
            var newSuspend = clock.SuspendSignals > _lastSuspend;
            var newResume = clock.ResumeSignals > _lastResume;
            if (newSuspend)
            {
                _pending = true;
                _pendingReported = false;
                _pendingSince = _awake;
            }

            var discount = 0.0;
            var asleep = 0.0;
            var evidence = SleepEvidence.None;
            var wake = false;
            var dAsleep = clock.HasMeasure && !double.IsNaN(_lastAsleep) ? clock.SystemAsleepSeconds - _lastAsleep : double.NaN;
            if (!double.IsNaN(dAsleep) && dAsleep >= MinSleepSeconds)
            {
                // 1 · la medida del sistema.
                asleep = dAsleep;
                if (dRaw >= CountedRatio * dAsleep)
                {
                    discount = Math.Min(dAsleep, dRaw);
                }

                evidence = SleepEvidence.Measure | (_pending ? SleepEvidence.Signal : SleepEvidence.None);
                wake = true;
                _pending = false;
            }
            else if (_pending)
            {
                // 2 · el aviso pendiente.
                if (dRaw >= SignalGapSeconds)
                {
                    discount = Math.Max(0, dRaw - NominalFrameSeconds);
                    evidence = SleepEvidence.Signal;
                    wake = true;
                    _pendingReported = true;
                }
                else if (dUtc - dRaw >= MinSleepSeconds)
                {
                    asleep = dUtc - dRaw;
                    evidence = SleepEvidence.Signal | SleepEvidence.WallJump;
                    wake = true;
                    _pendingReported = true;
                }
            }
            else if (dRaw >= LateFrameLogSeconds)
            {
                // 3 · sin evidencia: cuenta.
                _logger.Log(LogLevel.Info, Tag, fromBackground
                    ? "vuelta del segundo plano de " + Fmt.Fixed(dRaw, 1) + " s sin aviso de suspensión: cuenta (Q-91)"
                    : "cuadro de " + Fmt.Fixed(dRaw, 1) + " s sin aviso de suspensión: cuenta (AC-WKT-02-04)");
            }

            _bias += discount;
            var awake = raw - _bias;
            if (double.IsNaN(awake) || awake < _awake)
            {
                awake = _awake;
            }

            _awake = awake;
            if (_pending && wake)
            {
                _pendingSince = _awake;
            }

            if (newResume && _pending)
            {
                _pending = false;
                if (!_pendingReported && !wake)
                {
                    wake = true;
                    evidence = SleepEvidence.Signal;
                }
            }

            if (_pending && _awake - _pendingSince >= SignalExpirySeconds)
            {
                _pending = false;
                if (!_pendingReported)
                {
                    _logger.Log(LogLevel.Info, Tag, "aviso de suspensión sin sueño ni reanudación en 30 s: se descarta");
                }
            }

            if (discount > 0 && evidence == SleepEvidence.Signal && asleep == 0 && dRaw < SignalExpirySeconds)
            {
                // Un tirón con el aviso pendiente (QA-G4bT-05 (b)): se descuenta y queda en el registro.
                _logger.Log(LogLevel.Info, Tag, "cuadro de " + Fmt.Fixed(dRaw, 3) + " s con aviso de suspensión: se descuentan " + Fmt.Fixed(discount, 3) + " s");
            }

            _lastRaw = raw > _lastRaw ? raw : _lastRaw;
            _lastUtc = clock.Utc;
            if (clock.HasMeasure)
            {
                _lastAsleep = clock.SystemAsleepSeconds;
            }

            _lastSuspend = Math.Max(_lastSuspend, clock.SuspendSignals);
            _lastResume = Math.Max(_lastResume, clock.ResumeSignals);
            if (wake)
            {
                woke = new SystemWake(asleep, discount, evidence);
            }

            return _awake;
        }
    }
}
