namespace RowMate.Core.Ble.Native
{
    // El reloj de los eventos nativos en el reloj de la app (10a 02-tech-design §5.2; QA-G10a-04, R-06, DT10-18). La DLL usa QPC y
    // Kotlin System.nanoTime() (CLOCK_MONOTONIC): la misma base que el Stopwatch de UnityScheduler, así el desfase entre los dos
    // relojes es constante. Se estima solo en los drenajes de PRIMER PLANO: como el drenaje va antes del Tick, la hora del cuadro
    // es la del cuadro anterior y cada muestra (app − nativo) queda por debajo del desfase real, como mucho un cuadro; el máximo de
    // las muestras converge al real. En el drenaje de la vuelta del segundo plano NO se calibra (la hora del programador sigue en
    // el último cuadro antes de la pausa: medir ahí correría todo hacia atrás lo que duró la pausa): se usa el desfase congelado.
    // Reset al iniciar y con IAppLifecycle.Woke (en un sueño del sistema el reloj despierto de la app descuenta lo dormido).
    public sealed class BleEventClock
    {
        private double _offset;

        public bool IsCalibrated { get; private set; }

        // El desfase vigente (app − nativo).
        public double Offset => _offset;

        public void Calibrate(double appFrameSeconds, double nativeNowSeconds)
        {
            var sample = appFrameSeconds - nativeNowSeconds;
            if (!IsCalibrated || sample > _offset)
            {
                _offset = sample;
            }

            IsCalibrated = true;
        }

        public void Reset()
        {
            _offset = 0;
            IsCalibrated = false;
        }

        public double ToApp(double nativeSeconds)
        {
            return nativeSeconds + _offset;
        }
    }
}
