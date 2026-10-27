namespace RowMate.Presentation.Surface
{
    // Canal de color de la UI (D-01, R-T1; 02-tech-design §3.1, ADR 019). Medido en S-0 (Unity 6000.3.27f1):
    // - Overlay: el panel se dibuja directo en la pantalla (Screen Space Overlay). Unity compone la UI en espacio LINEAL:
    //   glass.fallback sobre blanco = #6A6A6C, text.secondary sobre surface.card = #DADBDC (forceGammaRendering no tiene
    //   efecto en Overlay).
    // - GammaRt: el panel se dibuja en una RT R8G8B8A8_UNorm con PanelSettings.forceGammaRendering = true (composición
    //   sRGB, como el legacy: #2A2F37 y #BBC1C8) y un presentador la lleva a la pantalla sin volver a convertir los colores.
    // El arnés de capturas usa la misma tubería que la app (UiTestHost, §4.1).
    public enum UiColorPipeline
    {
        Overlay,
        GammaRt
    }
}
