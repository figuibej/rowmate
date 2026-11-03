namespace RowMate.Core.Workout
{
    // Una lectura saneada (series_tracker.dart:120-127): lo que había antes, lo que sumó y el Δt. Sin asignar. La usa el
    // SeriesTracker para interpolar el cruce de cada marca de 500 m y para los segundos de trabajo, sin recalcular nada
    // (02-tech-design §2.1 de la It. 5).
    public readonly struct DistanceSample
    {
        public DistanceSample(double metersBefore, double timeBefore, double delta, double dt, double time)
        {
            MetersBefore = metersBefore;
            TimeBefore = timeBefore;
            Delta = delta;
            Dt = dt;
            Time = time;
        }

        // _distance antes de la lectura.
        public double MetersBefore { get; }

        // _lastTime antes de la lectura.
        public double TimeBefore { get; }

        // dd: lo que sumó (0 si el salto no cuenta).
        public double Delta { get; }

        // t − _lastTime: cuenta aunque el salto no sume (:121, :131).
        public double Dt { get; }

        // t.
        public double Time { get; }

        // = Meters después (:126).
        public double MetersAfter => MetersBefore + Delta;
    }

    // Los metros saneados de la sesión (Q-55, DV-26; AC-WKT-03-03, AC-WKT-07-06): el saneamiento de los parciales del legacy.
    // En cada lectura suma lo que avanzó el monitor desde la anterior, salvo que sea negativo o más de 10 m por segundo
    // (un paquete sin distancia, un reinicio del monitor, remar en pausa): entonces no suma y esa lectura pasa a ser la
    // referencia nueva. Al reanudar, Rebase fija la referencia sin contar el salto.
    // It. 5: hay UNO por sesión y es del SeriesTracker (spec §12 punto 2); el motor lee SeriesTracker.Meters para el paso por
    // distancia. Update devuelve la DistanceSample que el tracker necesita (la regla no cambió).
    // legacy: lib/features/workout/series_tracker.dart:43-46 (tope), :86-102 (begin), :104-108 (rebase), :118-127 y :143.
    public sealed class SanitizedDistance
    {
        // :43-46: el mejor remero del mundo ronda los 6,5 m/s; más que esto en una sola lectura es una falla.
        public const double MaxSpeedMetersPerSecond = 10.0;

        private double _lastRaw;

        // Metros saneados desde Begin (_distance del legacy).
        public double Meters { get; private set; }

        // El reloj activo de la última lectura (_lastTime del legacy, :52).
        public double LastTime { get; private set; }

        // La última distancia cruda (_lastRawDistance, :50).
        public double LastRaw => _lastRaw;

        // :88-102: la distancia cruda de referencia y el reloj activo en ese momento; Meters = 0.
        public void Begin(int rawMeters, int activeSeconds)
        {
            _lastRaw = rawMeters;
            Meters = 0;
            LastTime = activeSeconds;
        }

        // :106-108: al reanudar, lo remado en pausa no cuenta.
        public void Rebase(int rawMeters)
        {
            _lastRaw = rawMeters;
        }

        // :120-127, :143: dt = t − último; Δ = crudo − último crudo; Δ < 0 o Δ > 10 · max(dt; 1) → no suma. Las expresiones,
        // en el orden del legacy (ADR 012).
        public DistanceSample Update(int rawMeters, int activeSeconds)
        {
            double t = activeSeconds;
            var before = Meters;
            var timeBefore = LastTime;
            var dt = t - timeBefore;
            var rawDelta = rawMeters - _lastRaw;
            var maxDelta = MaxSpeedMetersPerSecond * (dt < 1 ? 1 : dt);
            var delta = rawDelta < 0 || rawDelta > maxDelta ? 0.0 : rawDelta;
            Meters = before + delta;
            _lastRaw = rawMeters;
            LastTime = t;
            return new DistanceSample(before, timeBefore, delta, dt, t);
        }
    }
}
