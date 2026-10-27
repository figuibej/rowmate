namespace RowMate.Presentation.Motion
{
    // Ajustes de motion vigentes (AC-APP-05-08): "reducir movimiento" del sistema, desde AppContext.Accessibility. Lo
    // actualiza AppShellView; las animaciones lo leen al empezar (no cambian a mitad de camino).
    public sealed class MotionSettings
    {
        public bool ReduceMotion { get; set; }
    }
}
