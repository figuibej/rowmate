namespace RowMate.Core.Design
{
    // El indicador de carga de la escena en el primer entrenamiento (It. 14, #21, AC-DEBT-09; DT14-30; ART_BIBLE §3.10): solo si la
    // escena no estaba lista al entrar; aparece a los 120 ms de gracia con un fundido de 160 ms y, con la escena lista, se va con
    // otro fundido de 160 ms desde la opacidad que tenía. Un solo Spinner de 32 dp con workoutSceneLoading. El HUD, el reloj y la
    // sesión no esperan (nada toca el dato). Los tiempos llegan por argumento.
    public static class SceneLoadingSpecs
    {
        public const int GraceMs = 120;
        public const int FadeMs = 160;
        public const int IndicatorDp = 32;

        public static bool NeedsIndicator(bool sceneReadyAtEnter)
        {
            return !sceneReadyAtEnter;
        }

        // msSinceEnter: desde el toque que abrió el inmersivo. msSinceReady: desde que la escena quedó lista (NaN si todavía no).
        public static double Opacity(bool sceneReadyAtEnter, double msSinceEnter, double msSinceReady)
        {
            if (sceneReadyAtEnter || double.IsNaN(msSinceEnter))
            {
                return 0;
            }

            if (double.IsNaN(msSinceReady))
            {
                return FadeIn(msSinceEnter);
            }

            var atReady = FadeIn(msSinceEnter - msSinceReady);
            if (atReady <= 0 || msSinceReady >= FadeMs)
            {
                return 0;
            }

            return atReady * (1 - msSinceReady / FadeMs);
        }

        // ¿El indicador sigue en el árbol? (cuenta para el tope del HUD mientras se ve o se funde).
        public static bool IsVisible(bool sceneReadyAtEnter, double msSinceEnter, double msSinceReady)
        {
            return Opacity(sceneReadyAtEnter, msSinceEnter, msSinceReady) > 0;
        }

        private static double FadeIn(double msSinceEnter)
        {
            if (msSinceEnter <= GraceMs)
            {
                return 0;
            }

            var t = (msSinceEnter - GraceMs) / FadeMs;
            return t >= 1 ? 1 : t;
        }
    }
}
