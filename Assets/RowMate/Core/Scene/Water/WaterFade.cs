namespace RowMate.Core.Scene.Water
{
    // El fundido entre la avanzada y la degradada (AC-6B-07, -08; DT6b-12): _RmDegradedBlend va de 1 (se ve la degradada) a 0 (la
    // avanzada) o al revés en 0,6 s lineales; con "reducir movimiento", en el acto. Un Start a mitad de camino sigue desde el valor
    // de ese momento (sin saltos). Sin Start: 1 (la degradada, mientras la avanzada se prepara).
    public struct WaterFade
    {
        public const double Seconds = 0.6;

        private bool _started;
        private double _start;
        private double _from;
        private double _to;
        private bool _instant;

        public void Start(double now, bool toAdvanced, bool reduceMotion)
        {
            var current = DegradedBlend(now);
            _started = true;
            _start = now;
            _from = current;
            _to = toAdvanced ? 0 : 1;
            _instant = reduceMotion;
        }

        // Directo al final, sin fundido (la avanzada no puede dibujar: el corte es en el acto, DT6b-12).
        public void Snap(bool toAdvanced)
        {
            _started = true;
            _start = double.NegativeInfinity;
            _from = toAdvanced ? 0 : 1;
            _to = _from;
            _instant = true;
        }

        public double DegradedBlend(double now)
        {
            if (!_started)
            {
                return 1;
            }

            if (_instant)
            {
                return _to;
            }

            var t = SceneMath.Clamp01((now - _start) / Seconds);
            return _from + (_to - _from) * t;
        }

        public bool Running(double now)
        {
            return _started && !_instant && now - _start < Seconds && _from != _to;
        }

        // Hacia dónde va (o fue) el último fundido.
        public bool TowardAdvanced => _started && _to == 0;
    }
}
