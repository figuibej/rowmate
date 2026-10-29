namespace RowMate.Core.Workout
{
    // Los metros saneados del paso por distancia (Q-55, DV-26; AC-WKT-03-03): el saneamiento de los parciales del legacy.
    // En cada lectura suma lo que avanzó el monitor desde la anterior, salvo que sea negativo o más de 10 m por segundo
    // (un paquete sin distancia, un reinicio del monitor, remar en pausa): entonces no suma y esa lectura pasa a ser la
    // referencia nueva. Al reanudar, Rebase fija la referencia sin contar el salto. Lo reusa el SeriesTracker de la It. 5.
    // legacy: lib/features/workout/series_tracker.dart:43-46 (tope), :86-102 (begin), :104-108 (rebase), :118-127 y :143.
    public sealed class SanitizedDistance
    {
        // :43-46: el mejor remero del mundo ronda los 6,5 m/s; más que esto en una sola lectura es una falla.
        public const double MaxSpeedMetersPerSecond = 10.0;

        private double _lastRaw;
        private double _lastTime;

        // Metros saneados desde Begin (_distance del legacy).
        public double Meters { get; private set; }

        // :88-102: la distancia cruda de referencia y el reloj activo en ese momento; Meters = 0.
        public void Begin(int rawMeters, int activeSeconds)
        {
            _lastRaw = rawMeters;
            Meters = 0;
            _lastTime = activeSeconds;
        }

        // :106-108: al reanudar, lo remado en pausa no cuenta.
        public void Rebase(int rawMeters)
        {
            _lastRaw = rawMeters;
        }

        // :120-127, :143: dt = t − último; Δ = crudo − último crudo; Δ < 0 o Δ > 10 · max(dt; 1) → no suma.
        public void Update(int rawMeters, int activeSeconds)
        {
            double t = activeSeconds;
            var dt = t - _lastTime;
            var rawDelta = rawMeters - _lastRaw;
            var maxDelta = MaxSpeedMetersPerSecond * (dt < 1 ? 1 : dt);
            var delta = rawDelta < 0 || rawDelta > maxDelta ? 0.0 : rawDelta;
            Meters += delta;
            _lastRaw = rawMeters;
            _lastTime = t;
        }
    }
}
