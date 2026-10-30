using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion
{
    // Base de las animaciones finitas con duración fija (destello, alto, transiciones). Begin arranca (o reinicia) desde el
    // instante actual del reloj de MotionDriver; Finish salta al final en el acto (un cambio nuevo en medio, AC-BLE-11-11).
    // OnUpdate recibe los ms transcurridos (0 … TotalMs); al terminar se llama una vez con TotalMs y después OnFinished.
    // It. 4b (02-tech-design §3.1, §7.4; QA-G4bT-07 (b)): el final se compara en ms enteros (HudTimelines.Ms y .Reached), así
    // 30,2 − 30,0 = 0,1999999999999993 s cuenta como los 200 ms y termina en ese cuadro; IsDoneAt es lo que mira la regla de toma.
    public abstract class TimedMotion : IMotion
    {
        private double _start;

        protected TimedMotion(MotionDriver driver)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool IsRunning { get; private set; }

        // Cuánto se pasó del final el cuadro en que terminó (0 si terminó con Finish): lo que sigue puede arrancar en el instante
        // exacto del final y no en el cuadro siguiente (el vacío después del colapso, 00-deuda-diseno de la It. 4a §2.1).
        public double OverrunMs { get; private set; }

        public virtual bool IsFinite => true;

        protected MotionDriver Driver { get; }

        protected abstract double TotalMs { get; }

        // Llegó a su final en ese instante (o no corre).
        public bool IsDoneAt(double nowSeconds)
        {
            return !IsRunning || HudTimelines.Reached(HudTimelines.Ms(nowSeconds - _start), TotalMs);
        }

        public bool Tick(double nowSeconds)
        {
            if (!IsRunning)
            {
                return false;
            }

            var elapsed = (nowSeconds - _start) * 1000.0;
            if (HudTimelines.Reached(HudTimelines.Ms(nowSeconds - _start), TotalMs))
            {
                IsRunning = false;
                OverrunMs = elapsed > TotalMs ? elapsed - TotalMs : 0;
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
            OverrunMs = 0;
            Driver.Remove(this);
            OnUpdate(TotalMs);
            OnFinished();
        }

        protected void Begin()
        {
            OverrunMs = 0;
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
