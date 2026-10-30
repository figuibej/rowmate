using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Las seis zonas del HUD en la cascada de la entrada (§3.2), más las piezas del centro (la cuenta grande, el sello y PAUSA:
    // van con el tramo de SPM si están al entrar, 04-design-notes §4.1).
    public enum HudPiece
    {
        TopBar,
        Spm,
        SplitWatts,
        Band,
        Vitals,
        Controls,
        Center,
    }

    // Una pieza de la cascada: opacidad y subida en dp base (+8 → 0, por translate; nunca layout).
    public readonly struct PieceVisual
    {
        public PieceVisual(double opacity, double shiftDp)
        {
            Opacity = opacity;
            ShiftDp = shiftDp;
        }

        public double Opacity { get; }

        public double ShiftDp { get; }
    }

    // Desde dónde arranca la salida (la entrada puede estar a medias, AC-WKT-13-07): las opacidades del fondo y de la página.
    public readonly struct ExitStart
    {
        public ExitStart(double backdrop, double page)
        {
            Backdrop = backdrop;
            Page = page;
        }

        public static ExitStart Full => new ExitStart(1, 1);

        public double Backdrop { get; }

        public double Page { get; }
    }

    // La salida: el factor del HUD (multiplica la opacidad de cada pieza), el fondo sobre el negro y la página negra.
    public readonly struct ExitVisual
    {
        public ExitVisual(double hud, double backdrop, double page)
        {
            Hud = hud;
            Backdrop = backdrop;
            Page = page;
        }

        public double Hud { get; }

        public double Backdrop { get; }

        public double Page { get; }
    }

    // El cambio de paso de las píldoras (04-design-notes §4.5): lo que le faltaba de relleno a la que termina (0 → 1), el
    // destello blanco, la opacidad de la sólida (1 → 0,85), el brillo del relleno que se va (1 → 0), el interior de la hueca
    // (1 → 0,18), el trazo de su borde (0 → 1 del contorno), la opacidad de ese borde, la pista de la nueva (0,18 → 0,30) y la
    // rampa del halo de la que termina (1 → 0).
    public readonly struct PillChangeVisual
    {
        public PillChangeVisual(double completingFill, double flash, double solidOpacity, double highlight, double hollowInterior,
                                double hollowStroke, double hollowBorderOpacity, double newTrack, double endingHaloRamp)
        {
            CompletingFill = completingFill;
            Flash = flash;
            SolidOpacity = solidOpacity;
            Highlight = highlight;
            HollowInterior = hollowInterior;
            HollowStroke = hollowStroke;
            HollowBorderOpacity = hollowBorderOpacity;
            NewTrack = newTrack;
            EndingHaloRamp = endingHaloRamp;
        }

        public double CompletingFill { get; }

        public double Flash { get; }

        public double SolidOpacity { get; }

        public double Highlight { get; }

        public double HollowInterior { get; }

        public double HollowStroke { get; }

        public double HollowBorderOpacity { get; }

        public double NewTrack { get; }

        public double EndingHaloRamp { get; }
    }

    // El pulgar que pasa (DN4b-18): alto (base) y opacidad del que se va y del que llega. Nunca hay dos pulgares enteros.
    public readonly struct ThumbVisual
    {
        public ThumbVisual(double outgoingHeight, double outgoingOpacity, double incomingHeight, double incomingOpacity)
        {
            OutgoingHeight = outgoingHeight;
            OutgoingOpacity = outgoingOpacity;
            IncomingHeight = incomingHeight;
            IncomingOpacity = incomingOpacity;
        }

        public double OutgoingHeight { get; }

        public double OutgoingOpacity { get; }

        public double IncomingHeight { get; }

        public double IncomingOpacity { get; }
    }

    // Los aires de la fila de información y el relleno de Próximo (DN4b-16), en dp base.
    public readonly struct InfoRowVisual
    {
        public InfoRowVisual(double countdownGap, double clockGap, double upNextPadH)
        {
            CountdownGap = countdownGap;
            ClockGap = clockGap;
            UpNextPadH = upNextPadH;
        }

        public double CountdownGap { get; }

        public double ClockGap { get; }

        public double UpNextPadH { get; }
    }

    // El disco del lugar centro alto (DN4b-14): la cuenta grande y PAUSA, cada una con su opacidad y su escala.
    public readonly struct DiscVisual
    {
        public DiscVisual(double countOpacity, double countScale, double pausedOpacity, double pausedScale)
        {
            CountOpacity = countOpacity;
            CountScale = countScale;
            PausedOpacity = pausedOpacity;
            PausedScale = pausedScale;
        }

        public double CountOpacity { get; }

        public double CountScale { get; }

        public double PausedOpacity { get; }

        public double PausedScale { get; }
    }

    // El sello (04-design-notes §4.7): el alto de la barra (base, 52 ↔ 74), la opacidad del contenido, la escala del ícono y, en
    // el reemplazo, la opacidad del contenido viejo.
    public readonly struct SealVisual
    {
        public SealVisual(double barHeight, double contentOpacity, double iconScale, double oldContentOpacity)
        {
            BarHeight = barHeight;
            ContentOpacity = contentOpacity;
            IconScale = iconScale;
            OldContentOpacity = oldContentOpacity;
        }

        public double BarHeight { get; }

        public double ContentOpacity { get; }

        public double IconScale { get; }

        public double OldContentOpacity { get; }
    }

    // El chip al cambiar la fase (04-design-notes §4.9): la mezcla de los colores (0 viejo → 1 nuevo) y las dos etiquetas.
    public readonly struct ChipVisual
    {
        public ChipVisual(double colorMix, double oldLabelOpacity, double newLabelOpacity)
        {
            ColorMix = colorMix;
            OldLabelOpacity = oldLabelOpacity;
            NewLabelOpacity = newLabelOpacity;
        }

        public double ColorMix { get; }

        public double OldLabelOpacity { get; }

        public double NewLabelOpacity { get; }
    }

    // kcal que se desliza (FLIP, 04a/04-design-notes §5.6): lo que le falta del desplazamiento (1 → 0) y la opacidad de BPM.
    public readonly struct KcalVisual
    {
        public KcalVisual(double kcalShift, double bpmOpacity)
        {
            KcalShift = kcalShift;
            BpmOpacity = bpmOpacity;
        }

        public double KcalShift { get; }

        public double BpmOpacity { get; }
    }

    // El diálogo de completado (04-design-notes §4.12): la escala del disco del check, su trazo (0 → 1) y la fracción de
    // ease.enter de cada total.
    public readonly struct CompletionVisual
    {
        public CompletionVisual(double checkScale, double checkStroke, double timeFraction, double distanceFraction, double caloriesFraction)
        {
            CheckScale = checkScale;
            CheckStroke = checkStroke;
            TimeFraction = timeFraction;
            DistanceFraction = distanceFraction;
            CaloriesFraction = caloriesFraction;
        }

        public double CheckScale { get; }

        public double CheckStroke { get; }

        public double TimeFraction { get; }

        public double DistanceFraction { get; }

        public double CaloriesFraction { get; }
    }

    // Lo común de las líneas de tiempo puras del HUD de la It. 4b (02-tech-design §3.1). Cada función recibe los MS ENTEROS desde
    // el comienzo (long), si se reduce el movimiento y, cuando corresponde, desde dónde arranca, y devuelve un struct: ninguna
    // asigna memoria. Los IMotion (WP-D1) pasan de segundos a ms una sola vez con Ms, así 30,2 − 30,0 = 0,1999999999999993 s da
    // 200 ms y Reached no depende de la coma flotante (QA-G4bT-07 (b)). Nada de esto decide cuándo pasa algo (AC-WKT-13-14).
    public static class HudTimelines
    {
        // (long)redondeo((now − start)·1000), la mitad lejos del cero (DartMath; ADR 012).
        public static long Ms(double seconds)
        {
            return DartMath.Round(seconds * 1000);
        }

        // El final de una fase finita: tMs ≥ endMs.
        public static bool Reached(long tMs, long endMs)
        {
            return tMs >= endMs;
        }

        public static bool Reached(long tMs, double endMs)
        {
            return tMs >= (long)endMs;
        }

        // La curva del token entre start y start + duration (antes 0, después 1).
        internal static double Ease(EasingToken curve, long tMs, double startMs, double durationMs)
        {
            if (durationMs <= 0)
            {
                return tMs >= startMs ? 1 : 0;
            }

            return Easing.Evaluate(curve, (tMs - startMs) / durationMs);
        }

        internal static double Linear(long tMs, double startMs, double durationMs)
        {
            return Ease(Tokens.EaseLinear, tMs, startMs, durationMs);
        }

        internal static double Lerp(double from, double to, double u)
        {
            return from + (to - from) * u;
        }
    }

    // Entrada "sumergirse" y salida con la última foto (§3.2, §3.3; 04-design-notes §4.1, §4.2; AC-WKT-13-06, -08).
    public static class ImmersiveTimeline
    {
        // Cuándo empieza el tramo de cada pieza (ms desde el toque).
        public static double CascadeStartMs(HudPiece piece)
        {
            switch (piece)
            {
                case HudPiece.TopBar:
                    return MotionSpecs.ImmersiveCascadeTopBarMs;
                case HudPiece.Spm:
                case HudPiece.Center:
                    return MotionSpecs.ImmersiveCascadeSpmMs;
                case HudPiece.SplitWatts:
                    return MotionSpecs.ImmersiveCascadeSplitWattsMs;
                case HudPiece.Band:
                    return MotionSpecs.ImmersiveCascadeBandMs;
                case HudPiece.Vitals:
                    return MotionSpecs.ImmersiveCascadeVitalsMs;
                default:
                    return MotionSpecs.ImmersiveCascadeControlsMs;
            }
        }

        // Cada pieza en 280 (ease.enter): opacidad 0 → 1 y +8 → 0 de translate. Con reducir movimiento, a 1 desde el cuadro 0
        // (la página funde con el HUD ya puesto).
        public static PieceVisual Entry(HudPiece piece, long tMs, bool reduced)
        {
            if (reduced)
            {
                return new PieceVisual(1, 0);
            }

            var e = HudTimelines.Ease(Tokens.EaseEnter, tMs, CascadeStartMs(piece), MotionSpecs.ImmersivePieceInMs);
            return new PieceVisual(e, MotionSpecs.ImmersivePieceShiftDp * (1 - e));
        }

        // La página negra: 0 → 1 en 220 (ease.enter): "Workout funde a negro". Con reducir movimiento, lineal en 120.
        public static double EntryPage(long tMs, bool reduced)
        {
            return reduced
                ? HudTimelines.Linear(tMs, 0, MotionSpecs.ReducedImmersiveFadeMs)
                : HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.ImmersiveBlackInMs);
        }

        // El fondo sobre el negro: 0 → 1 en 220-400 (ease.enter). Con reducir movimiento, 1.
        public static double EntryBackdrop(long tMs, bool reduced)
        {
            if (reduced)
            {
                return 1;
            }

            return HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.ImmersiveBackdropInStartMs,
                MotionSpecs.ImmersiveBackdropInEndMs - MotionSpecs.ImmersiveBackdropInStartMs);
        }

        // Los controles responden desde que empieza su tramo (840; P-15), al toque y al teclado; con reducir movimiento, desde el
        // cuadro 0. Las demás piezas no tienen interacción.
        public static bool EntryInteractive(HudPiece piece, long tMs, bool reduced)
        {
            return reduced || HudTimelines.Reached(tMs, CascadeStartMs(piece));
        }

        // Todo quieto (la llave entra): 1120; con reducir movimiento, 120.
        public static long EntryEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.ReducedImmersiveFadeMs : MotionSpecs.ImmersiveEntryEndMs);
        }

        // La llave al terminar la entrada: opacidad en 160 (ease.enter; la escala 0,85 → 1 la pone la capa del simulador).
        public static double WrenchIn(long tMs, bool reduced)
        {
            return HudTimelines.Ease(Tokens.EaseEnter, tMs, EntryEndMs(reduced), MotionSpecs.ImmersiveWrenchInMs);
        }

        // La salida desde la última foto: el HUD 1 → 0 en 0-200 (ease.exit), el fondo a negro en 200-400 (ease.exit) y la página
        // negra 1 → 0 en 400-640 (ease.enter). Con reducir movimiento, la página funde lineal en 120 con todo adentro.
        public static ExitVisual Exit(long tMs, in ExitStart from, bool reduced)
        {
            if (reduced)
            {
                var page = from.Page * (1 - HudTimelines.Linear(tMs, 0, MotionSpecs.ReducedImmersiveFadeMs));
                return new ExitVisual(1, from.Backdrop, page);
            }

            var hud = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.ImmersiveHudOutMs);
            var backdrop = from.Backdrop * (1 - HudTimelines.Ease(Tokens.EaseExit, tMs, MotionSpecs.ImmersiveBlackOutStartMs,
                MotionSpecs.ImmersiveBlackOutEndMs - MotionSpecs.ImmersiveBlackOutStartMs));
            var pageOpacity = from.Page * (1 - HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.ImmersiveRevealStartMs, MotionSpecs.ImmersiveRevealMs));
            return new ExitVisual(hud, backdrop, pageOpacity);
        }

        // 640; con reducir movimiento, 120.
        public static long ExitEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.ReducedImmersiveFadeMs : MotionSpecs.ImmersiveRevealStartMs + MotionSpecs.ImmersiveRevealMs);
        }

        // Cuándo empieza a verse Workout (los avisos del final, DN4b-24): 400; con reducir movimiento, 0.
        public static long RevealStartMs(bool reduced)
        {
            return (long)(reduced ? 0 : MotionSpecs.ImmersiveRevealStartMs);
        }
    }

    // La barra de pasos (§3.4; 04-design-notes §4.4, §4.5): el cambio de paso, el halo y el pulgar que pasa.
    public static class StepBarTimeline
    {
        // t = 0 en el cuadro en que el estado trae el paso nuevo. met: la que termina queda sólida (cumplida o sin objetivo);
        // false: hueca. Con reducir movimiento, sin destello ni trazo (AC-APP-05-08; QA-G4bT-03): la sólida solo funde, el borde
        // de la hueca funde de 0 a 1 en 160 y el brillo se va con su fundido.
        public static PillChangeVisual PillChange(long tMs, bool met, bool reduced)
        {
            var completing = HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PillFillCompleteMs);
            var solidifyDuration = MotionSpecs.PillSolidifyEndMs - MotionSpecs.PillSolidifyStartMs;
            var solidify = HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.PillSolidifyStartMs, solidifyDuration);
            var highlight = 1 - solidify;
            var newTrack = HudTimelines.Lerp(0.18, 0.30, HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PillTrackInMs));
            var haloRamp = 1 - HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PillGlowRampMs);
            if (met)
            {
                var flash = 0.0;
                if (!reduced && tMs >= MotionSpecs.PillSolidifyStartMs && tMs < MotionSpecs.PillSolidifyEndMs)
                {
                    flash = MotionSpecs.PillFlashPeak * (1 - HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.PillSolidifyStartMs, solidifyDuration));
                }

                var opacity = 1 - (1 - 0.85) * solidify;
                return new PillChangeVisual(completing, flash, opacity, highlight, 1, 0, 0, newTrack, haloRamp);
            }

            var missedDuration = MotionSpecs.PillMissedEndMs - MotionSpecs.PillMissedStartMs;
            var missed = HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.PillMissedStartMs, missedDuration);
            var interior = 1 - (1 - 0.18) * missed;
            double stroke;
            double borderOpacity;
            if (reduced)
            {
                stroke = 1;
                borderOpacity = HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.PillMissedStartMs, MotionSpecs.ReducedPillBorderFadeMs);
            }
            else
            {
                stroke = missed;
                borderOpacity = missed > 0 ? 1 : 0;
            }

            return new PillChangeVisual(completing, 0, 1, highlight, interior, stroke, borderOpacity, newTrack, haloRamp);
        }

        // 340 la sólida, 440 la hueca.
        public static long PillChangeEndMs(bool met)
        {
            return (long)(met ? MotionSpecs.PillSolidifyEndMs : MotionSpecs.PillMissedEndMs);
        }

        // La rampa del halo en 150 (ease.move): sube en el tick que deja el restante en 5 y baja en el cambio de paso.
        public static double HaloRamp(long tMs, bool rising)
        {
            var e = HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PillGlowRampMs);
            return rising ? e : 1 - e;
        }

        // α = a·(0,30 + 0,40·p), con p el reloj común de la barra (StepPulse.PulseAt); con reducir movimiento, a·0,70.
        public static double Halo(double ramp, double pulse, bool reduced)
        {
            var p = pulse < 0 ? 0 : pulse > 1 ? 1 : pulse;
            return ramp * (reduced ? MotionSpecs.PillGlowMax : MotionSpecs.PillGlowMin + (MotionSpecs.PillGlowMax - MotionSpecs.PillGlowMin) * p);
        }

        // Lo que se ve del halo en el borde de la píldora: 0,65·α (lo del BoxShadow del legacy, DN4b-17).
        public static double HaloAtEdge(double alpha)
        {
            return MotionSpecs.PillGlowEdgeFactor * alpha;
        }

        // El pulgar que pasa en 150 (ease.move): el que se va baja de 10 a la franja de 8 y se apaga; el que llega sube y se
        // enciende. Con reducir movimiento, fundido cruzado de 150 sin subir ni bajar.
        public static ThumbVisual ThumbSwap(long tMs, bool reduced)
        {
            var e = HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PillThumbSwapMs);
            var full = PillRowGeometry.ThumbHeight;
            var band = PillRowGeometry.Height;
            if (reduced)
            {
                return new ThumbVisual(full, 1 - e, full, e);
            }

            return new ThumbVisual(HudTimelines.Lerp(full, band, e), 1 - e, HudTimelines.Lerp(band, full, e), e);
        }
    }

    // La fila de información (§2.15; DN4b-16): el cambio de modo y la pastilla que se va al entrar Próximo.
    public static class InfoRowTimeline
    {
        // Con el cambio de paso, los aires y el relleno pasan al modo nuevo en 0-240 (ease.move); con reducir movimiento, en el
        // acto.
        public static InfoRowVisual ModeChange(long tMs, in InfoRowPlan from, in InfoRowPlan to, bool reduced)
        {
            var u = reduced ? 1 : HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.InfoRowModeChangeMs);
            return new InfoRowVisual(HudTimelines.Lerp(from.CountdownGap, to.CountdownGap, u), HudTimelines.Lerp(from.ClockGap, to.ClockGap, u),
                HudTimelines.Lerp(from.UpNextPadH, to.UpNextPadH, u));
        }

        // La pastilla que no entra con Próximo funde a 0 en 120 (ease.exit); con reducir movimiento, el mismo fundido.
        public static double TargetOut(long tMs, bool reduced)
        {
            return 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.TargetPillOutMs);
        }
    }

    // El disco del lugar centro alto (§3.5; 04-design-notes §4.6, §4.8; DN4b-14): la cuenta grande y PAUSA, uno por vez.
    public static class CenterDiscTimeline
    {
        // La cuenta entra (tick del 3): 200, opacidad ease.enter y escala 0,85 → 1. Con reducir movimiento, 160 solo opacidad.
        public static DiscVisual CountIn(long tMs, bool reduced)
        {
            if (reduced)
            {
                return new DiscVisual(HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.ReducedBigCountInMs), 1, 0, 1);
            }

            var e = HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.BigCountInMs);
            return new DiscVisual(e, HudTimelines.Lerp(MotionSpecs.BigCountInScaleFrom, 1, e), 0, 1);
        }

        // La cuenta sale (tick del cambio de paso o fin): 200, opacidad ease.exit y escala 1 → 1,10 (ease.enter, "se suelta").
        // Con reducir movimiento, 120 solo opacidad.
        public static DiscVisual CountOut(long tMs, bool reduced)
        {
            if (reduced)
            {
                return new DiscVisual(1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.ReducedBigCountOutMs), 1, 0, 1);
            }

            var opacity = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.BigCountOutMs);
            var scale = HudTimelines.Lerp(1, MotionSpecs.BigCountOutScaleTo, HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.BigCountOutMs));
            return new DiscVisual(opacity, scale, 0, 1);
        }

        // PAUSA entra: 240, opacidad ease.enter y escala 0,96 → 1; con reducir movimiento, sin escala.
        public static DiscVisual PausedIn(long tMs, bool reduced)
        {
            var e = HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.PausedLabelInMs);
            return new DiscVisual(0, 1, e, reduced ? 1 : HudTimelines.Lerp(MotionSpecs.PausedLabelScaleFrom, 1, e));
        }

        // PAUSA sale: 240, opacidad ease.exit y escala 1 → 0,96; con reducir movimiento, sin escala.
        public static DiscVisual PausedOut(long tMs, bool reduced)
        {
            var e = HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.PausedLabelOutMs);
            return new DiscVisual(0, 1, 1 - e, reduced ? 1 : HudTimelines.Lerp(1, MotionSpecs.PausedLabelOutScaleTo, e));
        }

        // Un disco por vez (DN4b-14): el que sale funde a 0 en 0-120 (ease.exit, sin escala) y el que entra empieza a los 120 con
        // su entrada de siempre. toPaused: pausar con la cuenta a la vista; si no, reanudar cuando la cuenta vuelve. Con reducir
        // movimiento, los mismos tiempos, sin escala.
        public static DiscVisual Swap(long tMs, bool toPaused, bool reduced)
        {
            var outgoing = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.CalloutSwapOutMs);
            var inT = tMs - (long)MotionSpecs.CalloutSwapInStartMs;
            if (toPaused)
            {
                var paused = inT < 0 ? new DiscVisual(0, 1, 0, reduced ? 1 : MotionSpecs.PausedLabelScaleFrom) : PausedIn(inT, reduced);
                return new DiscVisual(outgoing, 1, paused.PausedOpacity, paused.PausedScale);
            }

            var count = inT < 0 ? new DiscVisual(0, reduced ? 1 : MotionSpecs.BigCountInScaleFrom, 0, 1) : CountIn(inT, reduced);
            return new DiscVisual(count.CountOpacity, count.CountScale, outgoing, 1);
        }

        // El final de cada fase finita.
        public static long CountInEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.ReducedBigCountInMs : MotionSpecs.BigCountInMs);
        }

        public static long CountOutEndMs(bool reduced)
        {
            return (long)(reduced ? MotionSpecs.ReducedBigCountOutMs : MotionSpecs.BigCountOutMs);
        }

        public static long SwapEndMs(bool toPaused, bool reduced)
        {
            return (long)MotionSpecs.CalloutSwapInStartMs + (toPaused ? (long)MotionSpecs.PausedLabelInMs : CountInEndMs(reduced));
        }

        // La fracción del aro: lo que falta del paso / 3 (PillProgress.RemainingAt / 3), en [0; 1].
        public static double RingFraction(double remainingSeconds)
        {
            var f = remainingSeconds / 3;
            return f < 0 ? 0 : f > 1 ? 1 : f;
        }
    }

    // El sello (§3.6; 04-design-notes §4.7): t = 0 en LastOutcomeAt; todo dentro de los 2 s (AC-WKT-09-08).
    public static class SealTimeline
    {
        public const double BarHeightClosed = 52;
        public const double BarHeightOpen = 74;

        public static SealVisual At(long tMs, bool reduced)
        {
            if (tMs >= (long)MotionSpecs.SealDurationMs || tMs < 0)
            {
                return new SealVisual(BarHeightClosed, 0, reduced ? 1 : MotionSpecs.SealIconScaleFrom, 0);
            }

            if (reduced)
            {
                var content = tMs < MotionSpecs.SealContentOutStartMs
                    ? HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.ReducedSealContentInMs)
                    : 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, MotionSpecs.SealContentOutStartMs, MotionSpecs.ReducedSealContentOutMs);
                return new SealVisual(BarHeightOpen, content, 1, 0);
            }

            double height;
            double opacity;
            if (tMs < MotionSpecs.SealCollapseStartMs)
            {
                height = HudTimelines.Lerp(BarHeightClosed, BarHeightOpen, HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.SealGrowMs));
                opacity = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SealContentInStartMs,
                    MotionSpecs.SealContentInEndMs - MotionSpecs.SealContentInStartMs);
            }
            else
            {
                height = HudTimelines.Lerp(BarHeightOpen, BarHeightClosed, HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.SealCollapseStartMs,
                    MotionSpecs.SealCollapseEndMs - MotionSpecs.SealCollapseStartMs));
                opacity = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, MotionSpecs.SealContentOutStartMs,
                    MotionSpecs.SealContentOutEndMs - MotionSpecs.SealContentOutStartMs);
            }

            var icon = HudTimelines.Lerp(MotionSpecs.SealIconScaleFrom, 1, HudTimelines.Ease(Tokens.EaseEmphasis, tMs, MotionSpecs.SealIconStartMs,
                MotionSpecs.SealIconEndMs - MotionSpecs.SealIconStartMs));
            return new SealVisual(height, opacity, icon, 0);
        }

        // Un sello nuevo reemplaza al visible (pasos de 2 s o menos): el viejo funde en 0-80 (ease.exit), el nuevo entra en
        // 80-200 (ease.enter) con el ícono que se estampa otra vez en 80-320; la barra sigue abierta. Después de 320 vale At.
        public static SealVisual Swap(long tMs, bool reduced)
        {
            var old = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.SealSwapOutMs);
            var content = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.SealSwapOutMs, MotionSpecs.SealSwapInEndMs - MotionSpecs.SealSwapOutMs);
            var icon = reduced
                ? 1
                : HudTimelines.Lerp(MotionSpecs.SealIconScaleFrom, 1, HudTimelines.Ease(Tokens.EaseEmphasis, tMs, MotionSpecs.SealSwapOutMs,
                    MotionSpecs.SealSwapIconEndMs - MotionSpecs.SealSwapOutMs));
            return new SealVisual(BarHeightOpen, content, icon, old);
        }

        // Las fases finitas: la entrada hasta 300, el reemplazo hasta 320, la salida de 1800 a 2000; el sostén es continuo.
        public static long EntryEndMs => (long)MotionSpecs.SealIconEndMs;

        public static long SwapEndMs => (long)MotionSpecs.SealSwapIconEndMs;

        public static long EndMs => (long)MotionSpecs.SealDurationMs;
    }

    // El velo de pausa (§3.7; 04-design-notes §4.8): el tinte de la textura del fondo, de 0 a 0,15 en 300 (ease.move) al pausar
    // y de vuelta en 240 al reanudar, desde el valor que tenga. Con reducir movimiento, el mismo fundido.
    public static class PauseTimeline
    {
        public static double Veil(long tMs, bool pausing, double from, bool reduced)
        {
            if (pausing)
            {
                return HudTimelines.Lerp(from, MotionSpecs.PauseVeilAlpha, HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PauseVeilInMs));
            }

            return from * (1 - HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PauseVeilOutMs));
        }

        public static long EndMs(bool pausing)
        {
            return (long)(pausing ? MotionSpecs.PauseVeilInMs : MotionSpecs.PauseVeilOutMs);
        }
    }

    // El chip de fase al cambiar (§3.7; 04-design-notes §4.9): los colores cruzan en 150 (ease.move); la etiqueta vieja funde en
    // 0-80 (ease.exit) y la nueva entra en 80-200 (ease.enter), ya en su color. Igual con reducir movimiento (son fundidos).
    public static class PhaseChipTimeline
    {
        public static ChipVisual At(long tMs)
        {
            var mix = HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.PhaseChipColorMs);
            var old = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.ToastTextOutMs);
            var fresh = HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.ToastTextOutMs, MotionSpecs.ToastTextInEndMs - MotionSpecs.ToastTextOutMs);
            return new ChipVisual(mix, old, fresh);
        }

        public static long EndMs => (long)MotionSpecs.ToastTextInEndMs;
    }

    // El punto en vivo del chip (§3.7; DN4b-26): late entre 0,35 y 1, q de 1 a 0 en 800 y de vuelta en 800 (ease.pulse),
    // opacidad 0,35 + 0,65·q, y empieza en 1, bajando, cada vez que el chip pasa a EN MARCHA (t = 0 ahí). Con reducir
    // movimiento, quieto en 1.
    public static class LiveDot
    {
        public static double At(long tMs, bool reduced)
        {
            if (reduced || tMs <= 0)
            {
                return 1;
            }

            var cycle = (long)(2 * MotionSpecs.LiveDotHalfMs);
            var phase = tMs % cycle;
            double q;
            if (phase < MotionSpecs.LiveDotHalfMs)
            {
                q = 1 - Easing.Evaluate(Tokens.EasePulse, phase / MotionSpecs.LiveDotHalfMs);
            }
            else
            {
                q = Easing.Evaluate(Tokens.EasePulse, (phase - MotionSpecs.LiveDotHalfMs) / MotionSpecs.LiveDotHalfMs);
            }

            return MotionSpecs.LiveDotMinOpacity + (1 - MotionSpecs.LiveDotMinOpacity) * q;
        }
    }

    // kcal que se desliza (§3.8; 04a/04-design-notes §5.6), un FLIP: el layout cambia en el acto y kcal se dibuja con un
    // translate que va de su lugar viejo a 0 (KcalShift 1 → 0). Aparece BPM: kcal en 0-240 (ease.move) y BPM funde en 120-360
    // (ease.enter); se va BPM: BPM funde en 0-120 (ease.exit) y kcal se mueve en 120-360. Con reducir movimiento, kcal en el acto
    // y BPM funde en 240 / 120.
    public static class KcalSlideTimeline
    {
        public static KcalVisual At(long tMs, bool appearing, bool reduced)
        {
            if (appearing)
            {
                var bpmIn = reduced
                    ? HudTimelines.Ease(Tokens.EaseEnter, tMs, 0, MotionSpecs.BpmMoveMs)
                    : HudTimelines.Ease(Tokens.EaseEnter, tMs, MotionSpecs.BpmFadeInStartMs, MotionSpecs.BpmFadeInEndMs - MotionSpecs.BpmFadeInStartMs);
                var shiftIn = reduced ? 0 : 1 - HudTimelines.Ease(Tokens.EaseMove, tMs, 0, MotionSpecs.BpmMoveMs);
                return new KcalVisual(shiftIn, bpmIn);
            }

            var bpmOut = 1 - HudTimelines.Ease(Tokens.EaseExit, tMs, 0, MotionSpecs.BpmFadeOutMs);
            var shiftOut = reduced
                ? 0
                : 1 - HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.BpmMoveOutStartMs, MotionSpecs.BpmMoveOutEndMs - MotionSpecs.BpmMoveOutStartMs);
            return new KcalVisual(shiftOut, bpmOut);
        }

        public static long EndMs(bool appearing, bool reduced)
        {
            if (reduced)
            {
                return (long)(appearing ? MotionSpecs.BpmMoveMs : MotionSpecs.BpmFadeOutMs);
            }

            return (long)(appearing ? MotionSpecs.BpmFadeInEndMs : MotionSpecs.BpmMoveOutEndMs);
        }
    }

    // El diálogo de completado (§3.9; 04-design-notes §4.12): t = 0 cuando entra el diálogo (a los 300 del fin). El disco del
    // check de 0,6 a 1 en 60-300 (ease.emphasis), su trazo en 180-500 (ease.move) y los totales en 700 desde los 220, de a 120
    // (ease.enter). Con reducir movimiento, el check entero y los totales finales desde el primer cuadro.
    public static class CompletionTimeline
    {
        public static CompletionVisual At(long tMs, bool reduced)
        {
            if (reduced)
            {
                return new CompletionVisual(1, 1, 1, 1, 1);
            }

            var scale = HudTimelines.Lerp(MotionSpecs.CompletedCheckScaleFrom, 1, HudTimelines.Ease(Tokens.EaseEmphasis, tMs, MotionSpecs.CompletedCheckStartMs,
                MotionSpecs.CompletedCheckEndMs - MotionSpecs.CompletedCheckStartMs));
            var stroke = HudTimelines.Ease(Tokens.EaseMove, tMs, MotionSpecs.CompletedCheckStrokeStartMs,
                MotionSpecs.CompletedCheckStrokeEndMs - MotionSpecs.CompletedCheckStrokeStartMs);
            return new CompletionVisual(scale, stroke, Total(tMs, 0), Total(tMs, 1), Total(tMs, 2));
        }

        // El conteo termina a los 1160 del diálogo; con reducir movimiento, en el cuadro 0.
        public static long EndMs(bool reduced)
        {
            return reduced ? 0 : CountUp.StartMs(2) + (long)MotionSpecs.TotalsCountMs;
        }

        private static double Total(long tMs, int index)
        {
            return HudTimelines.Ease(Tokens.EaseEnter, tMs, CountUp.StartMs(index), MotionSpecs.TotalsCountMs);
        }
    }
}
