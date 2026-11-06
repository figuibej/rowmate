namespace RowMate.Presentation.Motion
{
    // Una animación por código que avanza con el reloj de MotionDriver (02-tech-design §3.9, D-14).
    public interface IMotion
    {
        // Finita (rodillo, destello, transiciones) o continua (latido, spinner). UiTestHost.Settle y la regla de toma del
        // arnés esperan solo a las finitas (DN-21).
        bool IsFinite { get; }

        // Avanza al instante nowSeconds (reloj monótono del IClock). false = terminó (MotionDriver la saca).
        bool Tick(double nowSeconds);
    }

    // It. 6a (06a-escena-lago/02-tech-design §2.16, B6a-19): una finita que se repite con cada lectura del remo (los rodillos de las
    // cifras y el destello de una tarjeta). No retiene el armado de la escena en el inmersivo (MotionDriver.HasFiniteNonRecurringMotions):
    // con lecturas cada segundo siempre hay una corriendo y la escena demorada no llegaba nunca. Para Settle y la regla de toma del
    // arnés siguen siendo finitas.
    public interface IRecurringMotion
    {
    }

    // Una animación que necesita el layout del cuadro siguiente para empezar (lo nuevo que crece: hasta que UI Toolkit no le
    // da su alto natural, no hay a qué crecer). Mientras espera, su línea de tiempo no avanza; UiTestHost.Settle y el arnés
    // dejan pasar un cuadro real en lugar de gastar reloj de prueba (03-rutinas/02-tech-design §5.1, regla de toma).
    public interface ILayoutBoundMotion
    {
        bool IsWaitingForLayout { get; }
    }
}
