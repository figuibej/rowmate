using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El chip de fase de la barra libre al cambiar la fase (04b-hud-exr/02-tech-design §3.7, §7.4; 04-design-notes §4.9;
    // AC-WKT-16-03): el tinte, el borde y el color del texto cruzan en 150 (ease.move; ColorMix 0 viejo → 1 nuevo) y la etiqueta
    // se reemplaza (la vieja funde en 0-80, la nueva entra en 80-200 ya en su color): PhaseChipTimeline.At. El texto nuevo ya es
    // el bueno en el cuadro de la fase (lo pone la vista); esto es solo cómo se ve el cruce.
    // - Un cambio con otro en curso vuelve a empezar desde 0 (la vista mezcla desde el color que se ve).
    // - Fases: F en 0-200; sale. Reducir movimiento: igual (son fundidos). Sin armar o con Instant: el final en el acto.
    public sealed class PhaseChipMotion : HudMotion
    {
        private static readonly ChipVisual Settled = PhaseChipTimeline.At(PhaseChipTimeline.EndMs);

        private readonly Action<ChipVisual> _apply;
        private double _start;

        public PhaseChipMotion(MotionDriver driver, Action<ChipVisual> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Current = Settled;
        }

        public override bool IsFinite => IsRunning;

        public ChipVisual Current { get; private set; }

        public void Change(bool instant)
        {
            if (!CanAnimate(instant))
            {
                Leave();
                Apply(Settled);
                return;
            }

            _start = Now;
            Apply(PhaseChipTimeline.At(0));
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, PhaseChipTimeline.EndMs))
            {
                Apply(Settled);
                return false;
            }

            Apply(PhaseChipTimeline.At(t));
            return true;
        }

        private void Apply(in ChipVisual visual)
        {
            Current = visual;
            _apply(visual);
        }
    }
}
