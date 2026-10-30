namespace RowMate.Presentation.Motion
{
    // Lo que le falta a un resorte de la 4a para llegar a su destino, en dp de pantalla (04b-hud-exr/02-tech-design §5.2, §7.4;
    // QA-G4bT-07 (d)): el ancho de Próximo y los desplazamientos (SpringMotion), la escala de la cuenta por su ancho (StepPulse)
    // y la de FitBox por el ancho de su contenido. La regla de toma del arnés (SnapshotTakeRule, WP-B3) lo usa para no tratar
    // igual a todo lo finito: un resorte bloquea una toma solo a más de TakeThresholdDp de su destino (sin umbral, un resorte
    // nunca llega exactamente y cualquier toma posterior a un cambio de ancho fallaría). 0 = quieto (o fuera del MotionDriver).
    public interface ISettlingMotion
    {
        double RemainingDp { get; }
    }

    public static class SettlingMotion
    {
        // 0,15 dp: menos de un quinto de píxel en W1280 y menos de medio en TV (02-tech-design, "Cambios por gate QA", punto 1).
        public const double TakeThresholdDp = 0.15;
    }
}
