namespace RowMate.Core.Text
{
    // El tracking de los rótulos del HUD por CONTENIDO (It. 15, #76, AC-D15-24, DT15-10; arte A15-07): un rótulo con al menos una
    // minúscula (Split 500m, Vatios/Watts, kcal) lleva 0,04 em; uno todo en mayúsculas (SPM, BPM, EN MARCHA) sigue en 0,08 em (arte de
    // la 14 §3.2). Vale igual en es y en. HudCard.SetLabel lo evalúa solo cuando cambia el texto (nunca por cuadro) y alterna la clase
    // MixedClass, que la USS pone en letter-spacing: 4px (TextCore: 0,04 em = 4). Sin asignar.
    public static class LabelTracking
    {
        public const double UpperEm = 0.08;
        public const double MixedEm = 0.04;

        // La clase modificadora de la USS (WP-D) para los rótulos con minúsculas.
        public const string MixedClass = "rm-hud-card__label--mixed";

        public static bool HasLowercase(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (var i = 0; i < text!.Length; i++)
            {
                if (char.IsLower(text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static double TrackingEm(string? text)
        {
            return HasLowercase(text) ? MixedEm : UpperEm;
        }
    }
}
