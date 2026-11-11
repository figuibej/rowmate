namespace RowMate.Core.Design
{
    // Las líneas de tiempo de la escena en el inmersivo (06a-escena-lago/02-tech-design §2.2, §3.9, §7.4; AC-SCN-02-05 a -07,
    // AC-SCN-01-07, AC-WKT-13-06, -08, -12, AC-WKT-17-07; DT6a-06). Puras: los IMotion de WP-D1 y la coreografía de WP-B2 solo las
    // evalúan. Milisegundos desde el instante que dice cada una. Los tiempos salen de MotionSpecs (el bloque de la 6a).
    public static class SceneTimelines
    {
        // El vuelo, anclado a los 220 ms del toque (no al primer cuadro): termina a los 1 820 aunque un cuadro se demore.
        public static double FlightStartSeconds(double tapSeconds)
        {
            return tapSeconds + MotionSpecs.FlightStartMs / 1000;
        }

        public static long FlightEndMs => (long)(MotionSpecs.FlightStartMs + MotionSpecs.FlightDurationMs);

        // La respiración entra en S(0; 0,6 s) desde el fin del vuelo: a los 2 420 ms vale 1.
        public static long BreathingFullMs => FlightEndMs + (long)(MotionSpecs.BreathingRampSeconds * 1000);

        // La fracción del vuelo (0 antes de los 220, 1 desde los 1 820), en ms desde el toque.
        public static double FlightProgress(long tMs)
        {
            return HudTimelines.Linear(tMs, MotionSpecs.FlightStartMs, MotionSpecs.FlightDurationMs);
        }

        // La escena aparece desde el negro en 220-400 (ease.enter), como el fondo de la 4b. Con reducir movimiento, 1: la página
        // funde lineal en 120 con todo adentro (P-15).
        public static double SceneAppear(long tMs, bool reduced)
        {
            return ImmersiveTimeline.EntryBackdrop(tMs, reduced);
        }

        // La salida (AC-SCN-02-06, AC-WKT-13-08): la escena funde a negro en 200-400 con el fondo; a los 400, Close (detrás del
        // negro). Con reducir movimiento la página funde en 120 y la escena se cierra ahí.
        public static double SceneExit(long tMs, double from, bool reduced)
        {
            return ImmersiveTimeline.Exit(tMs, new ExitStart(from, 1), reduced).Backdrop;
        }

        public static long CloseMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.ReducedImmersiveFadeMs : MotionSpecs.ImmersiveBlackOutEndMs);
        }

        // La escena demorada (AC-SCN-01-07): entra en 0,6 s (ease.enter) cuando queda lista, también con reducir movimiento (es un
        // fundido).
        public static double LateScene(long tMsSinceReady)
        {
            return HudTimelines.Ease(Tokens.EaseEnter, tMsSinceReady, 0, MotionSpecs.SceneLateFadeMs);
        }

        // El peso del velo para la escena (0-1): el alfa del velo de la 4b (0 → 0,15) sobre su máximo (AC-WKT-13-12).
        public static double VeilWeight(double veilAlpha)
        {
            var w = veilAlpha / MotionSpecs.PauseVeilAlpha;
            return w < 0 ? 0 : (w > 1 ? 1 : w);
        }

        // El HUD al cambiar de clase de ventana (DT6a-06): entra en 200 (ease.enter) en el layout nuevo; con reducir movimiento, en
        // el acto.
        public static double HudReflow(long tMsSinceChange, bool reduced)
        {
            return reduced ? 1 : HudTimelines.Ease(Tokens.EaseEnter, tMsSinceChange, 0, MotionSpecs.HudReflowMs);
        }

        // It. 15 (AC-D15-32, DT15-11): la entrada de un grupo del HUD en la cascada (0 a HudReflowGroups − 1): el progreso con
        // ease.enter de 0 a 1, que arranca a los grupo × 40 ms y dura 200. La opacidad es el progreso y el traslado, (1 − progreso) ×
        // 8 dp × la escala del HUD (HudReflowShift). Con reducir movimiento, 1 (la regla de los 400 ms la aplica el dueño).
        public static double HudReflowGroup(int group, long tMsSinceChange, bool reduced)
        {
            if (reduced)
            {
                return 1;
            }

            var g = group < 0 ? 0 : group >= MotionSpecs.HudReflowGroups ? MotionSpecs.HudReflowGroups - 1 : group;
            var start = g * MotionSpecs.HudReflowGroupDelayMs;
            return HudTimelines.Ease(Tokens.EaseEnter, tMsSinceChange, start, MotionSpecs.HudReflowMs);
        }

        // El traslado hacia abajo (dp) de un grupo con ese progreso.
        public static double HudReflowShift(double progress, double hudScale)
        {
            var p = progress < 0 ? 0 : progress > 1 ? 1 : progress;
            return (1 - p) * MotionSpecs.HudReflowShiftDp * hudScale;
        }

        // La toma aérea empieza con el diálogo de completado: a los 300 ms del fin (AC-WKT-21-01).
        public static long AerialStartMsAfterEnd => (long)MotionSpecs.CompletedDialogDelayMs;

        // La toma aérea terminó (6 s): desde ahí el diálogo quieto puede bajar a 30 fps (P-07).
        public static bool AerialSettled(long tMsSinceAerialStart)
        {
            return tMsSinceAerialStart >= (long)(MotionSpecs.AerialSeconds * 1000);
        }
    }
}
