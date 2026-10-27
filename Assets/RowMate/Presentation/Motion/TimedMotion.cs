using System;

namespace RowMate.Presentation.Motion
{
    // Base de las animaciones finitas con duración fija (destello, alto, transiciones). Begin arranca (o reinicia) desde el
    // instante actual del reloj de MotionDriver; Finish salta al final en el acto (un cambio nuevo en medio, AC-BLE-11-11).
    // OnUpdate recibe los ms transcurridos (0 … TotalMs); al terminar se llama una vez con TotalMs y después OnFinished.
    public abstract class TimedMotion : IMotion
    {
        private double _start;

        protected TimedMotion(MotionDriver driver)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool IsRunning { get; private set; }

        public virtual bool IsFinite => true;

        protected MotionDriver Driver { get; }

        protected abstract double TotalMs { get; }

        public bool Tick(double nowSeconds)
        {
            if (!IsRunning)
            {
                return false;
            }

            var elapsed = (nowSeconds - _start) * 1000.0;
            if (elapsed >= TotalMs)
            {
                IsRunning = false;
                OnUpdate(TotalMs);
                OnFinished();
                return false;
            }

            OnUpdate(elapsed < 0 ? 0 : elapsed);
            return true;
        }

        // Salta al final (estado de reposo) si está corriendo.
        public void Finish()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            Driver.Remove(this);
            OnUpdate(TotalMs);
            OnFinished();
        }

        protected void Begin()
        {
            _start = Driver.Now;
            IsRunning = true;
            Driver.Add(this);
            OnUpdate(0);
        }

        protected abstract void OnUpdate(double elapsedMs);

        protected virtual void OnFinished()
        {
        }

        protected static double Progress(double elapsedMs, double startMs, double endMs)
        {
            if (endMs <= startMs)
            {
                return elapsedMs >= endMs ? 1 : 0;
            }

            var u = (elapsedMs - startMs) / (endMs - startMs);
            return u <= 0 ? 0 : u >= 1 ? 1 : u;
        }
    }
}
