namespace RowMate.Core.Ports
{
    // Ajustes de accesibilidad del sistema en un instante (ARCHITECTURE §13.2, AC-APP-05-05, AC-APP-05-08).
    // TextScale: escala de letra del sistema (1 = normal; Android Configuration.fontScale, Windows TextScaleFactor / 100);
    // la app la cuantiza con TextScaleSteps (tope 1,3, P-03). ReduceMotion: "reducir movimiento" del sistema.
    public sealed record AccessibilitySnapshot(double TextScale, bool ReduceMotion)
    {
        public static readonly AccessibilitySnapshot Default = new AccessibilitySnapshot(1.0, false);
    }
}
