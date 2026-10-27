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
}
