namespace RowMate.Core.Design
{
    // Rol de contraste de un color (02-tech-design §4.2). ContrastTests aplica el umbral de cada rol.
    public enum ColorRole
    {
        // Sin chequeo de contraste.
        None,

        // Fondos y vidrios.
        Surface,

        // Rellenos, bordes, acentos, colores base de métricas y pasos (≥ 3:1 donde el Designer lo pide).
        Graphic,

        // Texto de interfaz sobre surface.card: ≥ 4,5:1.
        UiText,

        // Texto primario sobre surface.card: ≥ 7:1.
        UiTextPrimary,

        // Valor del HUD sobre el peor vidrio: ≥ 7:1.
        HudValue,

        // Familia roja del HUD (D-ART-01): ≥ 4,5:1.
        HudValueRed,

        // Texto chico del HUD (barra de pasos, chips): ≥ 4,5:1.
        HudText
    }
}
