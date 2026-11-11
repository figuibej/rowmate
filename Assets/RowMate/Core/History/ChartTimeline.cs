using System;
using RowMate.Core.Design;

namespace RowMate.Core.History
{
    // Cómo se ve el gráfico en un instante (lo que ChartGeometry.WriteFrame dibuja): el marco (franjas, tira, separadores, base),
    // el revelado de izquierda a derecha, el área, las dos curvas del fundido cruzado, el yMax animado, las etiquetas y la
    // grilla, y los objetivos, viejos y nuevos. Alfas en [0; 1].
    public readonly struct ChartVisual
    {
        public ChartVisual(double staticAlpha, double reveal, double areaAlpha, double newSeriesAlpha, double oldSeriesAlpha, double yMax,
                           double newLabelsAlpha, double oldLabelsAlpha, double newTargetsAlpha, double oldTargetsAlpha, bool finished)
        {
            StaticAlpha = staticAlpha;
            Reveal = reveal;
            AreaAlpha = areaAlpha;
            NewSeriesAlpha = newSeriesAlpha;
            OldSeriesAlpha = oldSeriesAlpha;
            YMax = yMax;
            NewLabelsAlpha = newLabelsAlpha;
            OldLabelsAlpha = oldLabelsAlpha;
            NewTargetsAlpha = newTargetsAlpha;
            OldTargetsAlpha = oldTargetsAlpha;
            Finished = finished;
            _selected = 0;
            _leaving = 0;
            SelectedT = 0;
            LeavingT = 0;
        }

        private ChartVisual(in ChartVisual other, int selectedStep, double selectedT, int leavingStep, double leavingT)
            : this(other.StaticAlpha, other.Reveal, other.AreaAlpha, other.NewSeriesAlpha, other.OldSeriesAlpha, other.YMax, other.NewLabelsAlpha,
                other.OldLabelsAlpha, other.NewTargetsAlpha, other.OldTargetsAlpha, other.Finished)
        {
            _selected = selectedStep < 0 ? 0 : selectedStep + 1;
            _leaving = leavingStep < 0 ? 0 : leavingStep + 1;
            SelectedT = Clamp01(selectedT);
            LeavingT = Clamp01(leavingT);
        }

        // It. 15 (#33, AC-D15-02, DT15-02): el paso marcado (índice + 1; 0 = ninguno, así default(ChartVisual) no marca nada) y el
        // que se desmarca, cada uno con su t = cuánto de la marca tiene (0 = franja al 10 %, 1 = al 22 %): el que entra va de 0 a 1 y
        // el que sale de 1 a 0 (StepHighlightMotion, ease.enter en 160 ms). Solo cambia el alfa de las franjas: no se vuelve a teselar.
        private readonly int _selected;
        private readonly int _leaving;

        public double StaticAlpha { get; }

        public double Reveal { get; }

        // Factor del área de la serie (multiplica el alfa de cada curva).
        public double AreaAlpha { get; }

        public double NewSeriesAlpha { get; }

        public double OldSeriesAlpha { get; }

        public double YMax { get; }

        // Etiquetas Y y grilla de la métrica nueva y de la vieja.
        public double NewLabelsAlpha { get; }

        public double OldLabelsAlpha { get; }

        public double NewTargetsAlpha { get; }

        public double OldTargetsAlpha { get; }

        public bool Finished { get; }

        // El índice del paso marcado, o -1.
        public int SelectedStep => _selected - 1;

        public double SelectedT { get; }

        // El índice del paso que se desmarca, o -1.
        public int LeavingStep => _leaving - 1;

        public double LeavingT { get; }

        // El mismo cuadro con la marca de paso (sin asignar: es un struct).
        public ChartVisual WithSteps(int selectedStep, double selectedT, int leavingStep, double leavingT)
        {
            return new ChartVisual(this, selectedStep, selectedT, leavingStep, leavingT);
        }

        // Cuánto de la marca tiene un paso en este cuadro (0 a 1): el máximo entre la que entra y la que sale.
        public double StepEmphasis(int stepIndex)
        {
            if (stepIndex < 0)
            {
                return 0;
            }

            var emphasis = 0.0;
            if (stepIndex == SelectedStep)
            {
                emphasis = SelectedT;
            }

            if (stepIndex == LeavingStep && LeavingT > emphasis)
            {
                emphasis = LeavingT;
            }

            return emphasis;
        }

        private static double Clamp01(double value)
        {
            return value > 0 ? value < 1 ? value : 1 : 0;
        }

        // Quieto: todo a la vista, solo la métrica actual.
        public static ChartVisual Rest(double yMax)
        {
            return new ChartVisual(1, 1, 1, 1, 0, yMax, 1, 0, 1, 0, true);
        }
    }

    // El tooltip en un instante: opacidad y escala (desde el punto).
    public readonly struct TooltipVisual
    {
        public TooltipVisual(double opacity, double scale, bool finished)
        {
            Opacity = opacity;
            Scale = scale;
            Finished = finished;
        }

        public double Opacity { get; }

        public double Scale { get; }

        public bool Finished { get; }
    }

    // Las líneas de tiempo del gráfico como funciones puras de t en ms (A9-11; ANIMATION_BIBLE §2.12; arte §5.1, §5.5, §5.6;
    // 02-tech-design §2.10, DT9-11). Las corre SessionChartView con el reloj del MotionDriver (deterministas para las tomas). La
    // animación no cambia ningún dato. Con reducir movimiento (gate M-01: AC-HIS-12-02 y spec §9 piden "al instante"), Entry y
    // Switch devuelven su estado final ya en t = 0; el tooltip solo cambia de opacidad.
    public static class ChartTimeline
    {
        // Entrada (arte §5.1): marco, franjas, grilla y etiquetas 0 → 1 en 0-200 (ease.enter); revelado en 100-700
        // (ease.enter); el área funde en 150-450 (ease.enter, el arte no nombra la curva).
        public static ChartVisual Entry(double tMs, double yMax, bool reduceMotion)
        {
            if (reduceMotion || tMs >= ChartSpecs.EntryRevealEndMs)
            {
                return ChartVisual.Rest(yMax);
            }

            var frame = Ease(Tokens.EaseEnter, Phase(tMs, 0, ChartSpecs.EntryFrameEndMs));
            var reveal = Ease(Tokens.EaseEnter, Phase(tMs, ChartSpecs.EntryRevealStartMs, ChartSpecs.EntryRevealEndMs));
            var area = Ease(Tokens.EaseEnter, Phase(tMs, ChartSpecs.EntryAreaStartMs, ChartSpecs.EntryAreaEndMs));
            return new ChartVisual(frame, reveal, area, 1, 0, yMax, frame, 0, 1, 0, false);
        }

        // Cambio de métrica (DT9-11): fundido cruzado lineal de 220 de la curva vieja y la nueva; yMax de la vieja a la nueva en
        // 300 (ease.move); etiquetas y grilla viejas 0-120 (ease.exit) y nuevas 100-220 (ease.enter); objetivos viejos 0-120 y
        // nuevos 100-300. Las franjas, la tira y los separadores no cambian. Un segundo cambio a mitad arranca con el yMax que
        // se ve (fromYMax).
        public static ChartVisual Switch(double tMs, double fromYMax, double toYMax, bool reduceMotion)
        {
            if (reduceMotion || tMs >= ChartSpecs.SwitchRescaleMs)
            {
                return ChartVisual.Rest(toYMax);
            }

            var cross = Phase(tMs, 0, ChartSpecs.SwitchCrossfadeMs);
            var rescale = Ease(Tokens.EaseMove, Phase(tMs, 0, ChartSpecs.SwitchRescaleMs));
            var labelsOut = 1 - Ease(Tokens.EaseExit, Phase(tMs, 0, ChartSpecs.SwitchLabelsOutEndMs));
            var labelsIn = Ease(Tokens.EaseEnter, Phase(tMs, ChartSpecs.SwitchLabelsInStartMs, ChartSpecs.SwitchLabelsInEndMs));
            var targetsOut = 1 - Ease(Tokens.EaseExit, Phase(tMs, 0, ChartSpecs.SwitchTargetsOutEndMs));
            var targetsIn = Ease(Tokens.EaseEnter, Phase(tMs, ChartSpecs.SwitchTargetsInStartMs, ChartSpecs.SwitchTargetsInEndMs));
            var yMax = fromYMax + (toYMax - fromYMax) * rescale;
            return new ChartVisual(1, 1, 1, cross, 1 - cross, yMax, labelsIn, labelsOut, targetsIn, targetsOut, false);
        }

        // El tooltip entra en 90 (ease.enter) con escala 0,96 → 1; con reducir movimiento, solo la opacidad.
        public static TooltipVisual TooltipIn(double tMs, bool reduceMotion)
        {
            var u = Ease(Tokens.EaseEnter, Phase(tMs, 0, ChartSpecs.TooltipInMs));
            var scale = reduceMotion ? 1 : ChartSpecs.TooltipInScale + (1 - ChartSpecs.TooltipInScale) * u;
            return new TooltipVisual(u, scale, tMs >= ChartSpecs.TooltipInMs);
        }

        // Y se va en 160 (ease.exit) desde la opacidad que tenga, sin escala.
        public static TooltipVisual TooltipOut(double tMs, double fromOpacity)
        {
            var u = Ease(Tokens.EaseExit, Phase(tMs, 0, ChartSpecs.TooltipOutMs));
            return new TooltipVisual(fromOpacity * (1 - u), 1, tMs >= ChartSpecs.TooltipOutMs);
        }

        // (t − start) / (end − start) recortado a [0; 1].
        public static double Phase(double tMs, double startMs, double endMs)
        {
            if (double.IsNaN(tMs) || tMs <= startMs)
            {
                return 0;
            }

            if (tMs >= endMs)
            {
                return 1;
            }

            return (tMs - startMs) / (endMs - startMs);
        }

        private static double Ease(EasingToken curve, double u)
        {
            return Easing.Evaluate(curve, u);
        }
    }
}
