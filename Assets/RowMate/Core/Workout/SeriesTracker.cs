using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Vuelta de 500 m completada dentro de un paso de trabajo (series_tracker.dart:4-13). Number: 1, 2, 3… dentro del paso;
    // Seconds: el tiempo en remar esos 500 m.
    public readonly struct Lap : IEquatable<Lap>
    {
        public Lap(int number, double seconds)
        {
            Number = number;
            Seconds = seconds;
        }

        public int Number { get; }

        public double Seconds { get; }

        public bool Equals(Lap other)
        {
            return Number == other.Number && Seconds.Equals(other.Seconds);
        }

        public override bool Equals(object? obj)
        {
            return obj is Lap other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Seconds);
        }
    }

    // Resumen de una repetición completa de la serie (:15-34). TotalMeters: todos los pasos de la repetición (trabajo,
    // enfriamiento…); WorkMeters y WorkSeconds: los de sus pasos de trabajo. WorkSplitSeconds = WorkSeconds / WorkMeters × 500,
    // o null sin metros de trabajo (:31-33; LT-54).
    public readonly struct RepSummary : IEquatable<RepSummary>
    {
        public RepSummary(int rep, int totalMeters, int workMeters, double workSeconds)
        {
            Rep = rep;
            TotalMeters = totalMeters;
            WorkMeters = workMeters;
            WorkSeconds = workSeconds;
        }

        public int Rep { get; }

        public int TotalMeters { get; }

        public int WorkMeters { get; }

        public double WorkSeconds { get; }

        // :32-33, en el orden del legacy (ADR 012).
        public double? WorkSplitSeconds => WorkMeters > 0 ? WorkSeconds / WorkMeters * SeriesTracker.LapMeters : (double?)null;

        public bool Equals(RepSummary other)
        {
            return Rep == other.Rep && TotalMeters == other.TotalMeters && WorkMeters == other.WorkMeters && WorkSeconds.Equals(other.WorkSeconds);
        }

        public override bool Equals(object? obj)
        {
            return obj is RepSummary other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rep, TotalMeters, WorkMeters, WorkSeconds);
        }
    }

    // La foto del tracker (02-tech-design §2.2 de la It. 5): inmutable; la rehace el tracker solo cuando cambia (Begin, Update).
    // Laps y PreviousReps son arreglos que el tracker REEMPLAZA cuando cierra una vuelta, entra a un paso o cambia de repetición
    // (copia al escribir): una foto nueva por segundo cuesta un objeto, no una copia de las listas. Los getters son los del
    // legacy (:69-84) evaluados en el momento de la foto.
    public sealed class SeriesState
    {
        private static readonly Lap[] NoLaps = new Lap[0];
        private static readonly RepSummary[] NoReps = new RepSummary[0];

        // Antes de iniciar ninguna rutina (y el valor inicial del tracker).
        public static readonly SeriesState Empty = new SeriesState(0, null, null, false, NoLaps, 0, 0, NoReps, 0, 0);

        internal SeriesState(long revision, int? stepIndex, StepPosition? position, bool isWorkStep, IReadOnlyList<Lap> laps,
                             int currentLapMeters, double currentLapSeconds, IReadOnlyList<RepSummary> previousReps, int repMeters,
                             double stepMeters)
        {
            Revision = revision;
            StepIndex = stepIndex;
            Position = position;
            IsWorkStep = isWorkStep;
            Laps = laps;
            CurrentLapMeters = currentLapMeters;
            CurrentLapSeconds = currentLapSeconds;
            PreviousReps = previousReps;
            RepMeters = repMeters;
            StepMeters = stepMeters;
        }

        // Sube con cada Begin y cada Update (la caché del HUD la usa de clave).
        public long Revision { get; }

        // El paso de la última lectura; null antes de la primera (:54). DV-48 lo mira (SeriesPanels).
        public int? StepIndex { get; }

        // :55: la posición del paso de la última lectura (null antes de la primera).
        public StepPosition? Position { get; }

        // :56, :70.
        public bool IsWorkStep { get; }

        // Las del paso, en orden (:61, :69).
        public IReadOnlyList<Lap> Laps { get; }

        // :73-75: ⌊(d − inicio del paso) − 500·vueltas⌋ en trabajo; 0 si no.
        public int CurrentLapMeters { get; }

        // :78: t − fin de la última vuelta (o inicio del paso) en trabajo; 0 si no.
        public double CurrentLapSeconds { get; }

        // :80: Position?.GroupId != null.
        public bool InSeries => Position.HasValue && Position.Value.GroupId != null;

        // :81 (0 sin posición).
        public int Rep => Position.HasValue ? Position.Value.Rep : 0;

        // :82 (0 sin posición).
        public int RepCount => Position.HasValue ? Position.Value.RepCount : 0;

        // :83: ⌊d − inicio de la repetición⌋.
        public int RepMeters { get; }

        // :67, :84: la más reciente primero, hasta 3.
        public IReadOnlyList<RepSummary> PreviousReps { get; }

        // d − inicio del paso: los mismos metros que cuenta el paso por distancia del motor (AC-WKT-07-09).
        public double StepMeters { get; }
    }

    // Una llamada al tracker, para la prueba de la alimentación del motor (SeriesGoldenTests, AC-WKT-07-12; DT5-07). Solo
    // pruebas: el delegado SeriesTracker.Trace es interno y nulo en la app.
    internal enum SeriesCallKind
    {
        Begin,
        Rebase,
        Update,
    }

    internal readonly struct SeriesCall
    {
        public SeriesCall(SeriesCallKind kind, int stepIndex, IntervalStep? step, StepPosition position, int rawMeters, int activeSeconds)
        {
            Kind = kind;
            StepIndex = stepIndex;
            Step = step;
            Position = position;
            RawMeters = rawMeters;
            ActiveSeconds = activeSeconds;
        }

        public SeriesCallKind Kind { get; }

        public int StepIndex { get; }

        public IntervalStep? Step { get; }

        public StepPosition Position { get; }

        public int RawMeters { get; }

        // −1 en Rebase.
        public int ActiveSeconds { get; }
    }

    // Lleva las vueltas de 500 m de los pasos de trabajo y los totales de cada repetición de la serie en curso
    // (series_tracker.dart:36-183; F-WKT-07, F-WKT-08). El motor lo alimenta una vez por segundo con la distancia del monitor y
    // el reloj activo (sin pausas), ANTES de evaluar el fin del paso (workout_provider.dart:176-186). Es dueño del ÚNICO
    // SanitizedDistance de la sesión: el motor lee Meters para el paso por distancia (DV-26; spec §12 punto 2; DT5-06), así
    // los parciales y el paso cuentan los mismos metros, con la misma regla, en el mismo segundo.
    // Puro, sin reloj del sistema: el tiempo llega por argumento. Las expresiones de coma flotante, en el orden del legacy
    // (ADR 012), así el oráculo compara con 1e-9 sin sorpresas. No conoce la presentación (4 vueltas a la vista, DV-48, textos).
    public sealed class SeriesTracker
    {
        // :40.
        public const int LapMeters = 500;

        // :41.
        public const int MaxPreviousReps = 3;

        private static readonly Lap[] NoLaps = new Lap[0];
        private static readonly RepSummary[] NoReps = new RepSummary[0];

        private readonly SanitizedDistance _meter = new SanitizedDistance();

        private int? _stepIndex;
        private StepPosition? _position;
        private bool _isWork;

        // Paso actual (:59-61).
        private double _stepStartDistance;
        private double _lastLapEndTime;
        private Lap[] _laps = NoLaps;

        // Repetición actual (:64-67).
        private double _repStartDistance;
        private double _repWorkMeters;
        private double _repWorkSeconds;
        private RepSummary[] _previousReps = NoReps;
        private long _revision;

        // La foto vigente: la misma instancia hasta el próximo Begin o Update.
        public SeriesState State { get; private set; } = SeriesState.Empty;

        // Los metros saneados desde Begin (_distance, :51): los que lee el paso por distancia del motor.
        public double Meters => _meter.Meters;

        // Solo pruebas (SeriesGoldenTests): cada llamada, en orden. Nulo en la app (sin costo).
        internal Action<SeriesCall>? Trace { get; set; }

        // :86-102: reinicia todo. rawMeters es la distancia del monitor al iniciar; activeSeconds, el reloj activo en ese
        // momento (0).
        public void Begin(int rawMeters, int activeSeconds = 0)
        {
            _meter.Begin(rawMeters, activeSeconds);
            _stepIndex = null;
            _position = null;
            _isWork = false;
            _stepStartDistance = _meter.Meters;
            _lastLapEndTime = _meter.LastTime;
            _laps = NoLaps;
            _repStartDistance = _meter.Meters;
            _repWorkMeters = 0;
            _repWorkSeconds = 0;
            _previousReps = NoReps;
            Publish();
            Trace?.Invoke(new SeriesCall(SeriesCallKind.Begin, -1, null, default, rawMeters, activeSeconds));
        }

        // :104-108: fija la distancia cruda de referencia sin contar ningún salto. Se llama al reanudar tras una pausa (LT-58).
        // La foto no cambia.
        public void Rebase(int rawMeters)
        {
            _meter.Rebase(rawMeters);
            Trace?.Invoke(new SeriesCall(SeriesCallKind.Rebase, -1, null, default, rawMeters, -1));
        }

        // :110-145: una lectura del paso stepIndex. Si el paso cambió, primero entra (:147-182). Después sanea
        // (SanitizedDistance.Update) y, en trabajo, suma los metros que cuentan y TODO el Δt (:129-131) y cierra cada marca de
        // 500 que se cruzó, interpolando (:132-140): crossTime = dd > 0 ? t0 + (marca − d0) / dd · dt : t.
        public void Update(int stepIndex, IntervalStep step, StepPosition position, int rawMeters, int activeSeconds)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (stepIndex != _stepIndex)
            {
                EnterStep(stepIndex, step, position);
            }

            var sample = _meter.Update(rawMeters, activeSeconds);
            var dd = sample.Delta;
            var dt = sample.Dt;
            var d = sample.MetersAfter;
            if (_isWork)
            {
                if (dd > 0)
                {
                    _repWorkMeters += dd;
                }

                _repWorkSeconds += dt;
                // Puede completarse más de una vuelta en una lectura.
                while (d - _stepStartDistance >= (_laps.Length + 1) * LapMeters)
                {
                    var target = _stepStartDistance + (_laps.Length + 1) * LapMeters;
                    // Interpolación lineal del instante en que se cruzó la marca (:136-137).
                    var crossTime = dd > 0 ? sample.TimeBefore + (target - sample.MetersBefore) / dd * dt : sample.Time;
                    var laps = new Lap[_laps.Length + 1];
                    Array.Copy(_laps, laps, _laps.Length);
                    laps[_laps.Length] = new Lap(_laps.Length + 1, crossTime - _lastLapEndTime);
                    _laps = laps;
                    _lastLapEndTime = crossTime;
                }
            }

            Publish();
            Trace?.Invoke(new SeriesCall(SeriesCallKind.Update, stepIndex, step, position, rawMeters, activeSeconds));
        }

        // :147-182. Repetición nueva si cambia la serie o el número (:149-151). Con la misma serie, el resumen de la que
        // terminó va al frente (:154-166) y si quedan más de 3 se va la más vieja; si no, la lista se vacía (:167-170). El
        // paso nuevo empieza con la distancia y el reloj de la lectura ANTERIOR (:178-181).
        private void EnterStep(int stepIndex, IntervalStep step, StepPosition position)
        {
            var prev = _position;
            var isNewRep = !prev.HasValue || !string.Equals(position.GroupId, prev.Value.GroupId, StringComparison.Ordinal)
                           || position.Rep != prev.Value.Rep;
            if (isNewRep)
            {
                var sameSeries = prev.HasValue && prev.Value.GroupId != null
                                 && string.Equals(position.GroupId, prev.Value.GroupId, StringComparison.Ordinal);
                if (sameSeries && prev.HasValue)
                {
                    var summary = new RepSummary(prev.Value.Rep, Floor(_meter.Meters - _repStartDistance), Floor(_repWorkMeters), _repWorkSeconds);
                    var count = Math.Min(_previousReps.Length + 1, MaxPreviousReps);
                    var reps = new RepSummary[count];
                    reps[0] = summary;
                    Array.Copy(_previousReps, 0, reps, 1, count - 1);
                    _previousReps = reps;
                }
                else
                {
                    // La comparación es dentro de la serie en curso.
                    _previousReps = NoReps;
                }

                _repStartDistance = _meter.Meters;
                _repWorkMeters = 0;
                _repWorkSeconds = 0;
            }

            _stepIndex = stepIndex;
            _position = position;
            _isWork = step.Type == StepType.Work;
            _stepStartDistance = _meter.Meters;
            _lastLapEndTime = _meter.LastTime;
            _laps = NoLaps;
        }

        private void Publish()
        {
            var distance = _meter.Meters;
            var currentLapMeters = _isWork ? Floor(distance - _stepStartDistance - _laps.Length * LapMeters) : 0;
            var currentLapSeconds = _isWork ? _meter.LastTime - _lastLapEndTime : 0;
            State = new SeriesState(++_revision, _stepIndex, _position, _isWork, _laps, currentLapMeters, currentLapSeconds, _previousReps,
                Floor(distance - _repStartDistance), distance - _stepStartDistance);
        }

        // double.floor() de Dart → int (los metros nunca bajan de 0 dentro del paso ni de la repetición: LT-57; con los extremos
        // de FTMS, 16 777 215, cabe en int).
        private static int Floor(double value)
        {
            return (int)Math.Floor(value);
        }
    }
}
