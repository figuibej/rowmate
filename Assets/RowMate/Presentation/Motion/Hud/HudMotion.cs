using System;

namespace RowMate.Presentation.Motion.Hud
{
    // Lo común de los IMotion del HUD de la It. 4b (04b-hud-exr/02-tech-design §7.4, WP-D1). El contrato:
    // - No conocen las vistas: reciben el MotionDriver y callbacks con los structs de valores de las líneas de tiempo puras
    //   (Core/Design/HudTimelines.cs, WP-A). Las vistas de WP-B los construyen, los llaman y aplican lo que reciben.
    // - Nada del motion decide cuándo pasa algo (AC-WKT-13-14): el dato ya cambió cuando la vista llama; esto es solo cómo se ve.
    //   Nunca adelantan ni frenan el reloj ni las métricas: leen el reloj del MotionDriver (el despierto del programador, el
    //   mismo del motor) y nunca lo mueven.
    // - Sin armar (antes del primer cuadro presentado) o con Instant (segundo plano, despertar, cuadro tarde): el estado final en
    //   el acto. Reducir movimiento se lee al empezar cada fase (MotionSettings: no cambia a mitad de camino).
    // - El tiempo pasa a ms enteros una sola vez, con HudTimelines.Ms (QA-G4bT-07 (b)), y los finales se comparan con Reached.
    // - IsFinite por fase (§7.4): F = una transición (bloquea una toma y la espera Settle); C = continua (en el MotionDriver, sin
    //   bloquear). Fuera del MotionDriver, ni una ni otra.
    // - Stop() sale del MotionDriver en el acto (y con eso deja de pedir 60 fps), sin avisar nada: la vista lo llama en su Dispose
    //   y en su DetachFromPanelEvent (QA-G4bT-08 (b); la lección de QA-V4a-02 = ROB-02: nada queda animando). Lo que se ve queda
    //   donde estaba; al volver al panel la vista aplica el estado en el acto.
    // - Sin asignaciones por cuadro: structs, arreglos propios y callbacks creados una vez.
    public abstract class HudMotion : IMotion
    {
        private bool _inDriver;
        private int _generation;

        protected HudMotion(MotionDriver driver)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        // En el MotionDriver: una fase finita o continua en curso.
        public bool IsRunning => _inDriver;

        // La fase en curso es una transición (§7.4). false fuera del MotionDriver.
        public abstract bool IsFinite { get; }

        protected MotionDriver Driver { get; }

        protected double Now => Driver.Now;

        // Si lo nuevo se anima: armado y sin Instant.
        protected bool CanAnimate(bool instant)
        {
            return !instant && Driver.Armed;
        }

        protected bool ReduceMotion => Driver.Settings.ReduceMotion;

        public bool Tick(double nowSeconds)
        {
            if (!_inDriver)
            {
                return false;
            }

            var generation = _generation;
            var keep = Advance(nowSeconds);
            if (generation != _generation)
            {
                // Un aviso de este cuadro volvió a arrancarlo (Run otra vez): sigue si quedó en el MotionDriver.
                return _inDriver;
            }

            if (!keep)
            {
                _inDriver = false;
            }

            return keep;
        }

        public void Stop()
        {
            Leave();
            OnStopped();
        }

        // Avanza al instante; false = sale del MotionDriver. Los avisos (callbacks de fin) van al final, después de dejar el
        // estado listo: pueden volver a arrancarlo.
        protected abstract bool Advance(double nowSeconds);

        protected virtual void OnStopped()
        {
        }

        // Entra (o sigue) en el MotionDriver.
        protected void Run()
        {
            _generation++;
            if (_inDriver)
            {
                return;
            }

            _inDriver = true;
            Driver.Add(this);
        }

        // Sale del MotionDriver (sin avisos).
        protected void Leave()
        {
            _generation++;
            if (!_inDriver)
            {
                return;
            }

            _inDriver = false;
            Driver.Remove(this);
        }

        protected static double Clamp01(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
