using RowMate.Core.Ports;

namespace RowMate.Platform.SystemUi
{
    // Las barras del sistema en Windows, el Editor y macOS de escritorio (F-APP-15; AC-APP-15-02; 04b-hud-exr/02-tech-design §4.2):
    // sin efecto, como el legacy (immersive_workout_screen.dart:34 no hace nada en escritorio). La ventana queda como estaba: no
    // pasa a pantalla completa y no cambian la barra de tareas, la de título, el tamaño ni la posición (la pantalla completa real
    // es Q-88, del backlog). En la 4b también iOS (la barra de estado es de la 10b).
    public sealed class DesktopSystemUi : ISystemUi
    {
        public static readonly DesktopSystemUi Instance = new DesktopSystemUi();

        // Cuántos pedidos llegaron (para el registro y las pruebas): no hacen nada.
        public int Requests { get; private set; }

        public void SetImmersive(bool immersive)
        {
            Requests++;
        }
    }
}
