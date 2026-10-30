using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El diálogo de rutina completada que entra con peso (04b-hud-exr/02-tech-design §3.9, §7.4; 04-design-notes §4.12;
    // AC-WKT-21-05): t = 0 cuando entra el diálogo (a los 300 del fin; el velo y la caja los mueve la transición de la It. 3). El
    // disco del check de 0,6 a 1 en 60-300 (ease.emphasis), su trazo en 180-500 (ease.move) y los totales que cuentan de 0 a su
    // valor en 700 (ease.enter) desde los 220, escalonados 120 (CompletionTimeline.At). Los totales son los únicos números que
    // cuentan (ANIMATION_BIBLE §2.5): ValueOf da lo que se ve de cada uno (CountUp, ⌊final·e(u)⌋, con el formato del legacy en
    // la vista y sin rodillo) y SIEMPRE termina en el valor exacto, a los 1160. Los valores son fijos (HudCompletion, DV-43).
    // - Fases: F de 0 a 1160; sale. CompletionDialog.OnClosing (el cuadro 0 de su salida) y su DetachFromPanelEvent llaman a
    //   Stop: con Terminar durante el conteo nada sigue contando ni pide 60 fps (QA-G4bT-08 (a)).
    // - Reducir movimiento, sin armar o con Instant (el diálogo ya abierto al volver del segundo plano, AC-WKT-21-09): el check
    //   entero y los totales finales desde el primer cuadro. Finish: salta al final.
    public sealed class CompletionMotion : HudMotion
    {
        private static readonly CompletionVisual Settled = CompletionTimeline.At(CompletionTimeline.EndMs(false), false);

        private readonly Action<CompletionVisual> _apply;
        private double _start;
        private bool _reduced;

        public CompletionMotion(MotionDriver driver, Action<CompletionVisual> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Current = Settled;
            ElapsedMs = CompletionTimeline.EndMs(false);
        }

        public override bool IsFinite => IsRunning;

        public CompletionVisual Current { get; private set; }

        // Los ms desde que entró el diálogo, en el último cuadro (el final: 1160).
        public long ElapsedMs { get; private set; }

        // Lo que se ve del total index (0 = tiempo, 1 = distancia, 2 = calorías) que termina en final.
        public long ValueOf(long final, int index)
        {
            return CountUp.ValueAt(final, ElapsedMs, CountUp.StartMs(index), _reduced);
        }

        public void Play(bool instant)
        {
            _reduced = ReduceMotion;
            _start = Now;
            if (_reduced || !CanAnimate(instant))
            {
                Leave();
                ElapsedMs = CompletionTimeline.EndMs(false);
                Apply(Settled);
                return;
            }

            ElapsedMs = 0;
            Apply(CompletionTimeline.At(0, false));
            Run();
        }

        public void Finish()
        {
            if (!IsRunning)
            {
                return;
            }

            Leave();
            ElapsedMs = CompletionTimeline.EndMs(false);
            Apply(Settled);
        }

        protected override bool Advance(double nowSeconds)
        {
            var end = CompletionTimeline.EndMs(false);
            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, end))
            {
                ElapsedMs = end;
                Apply(Settled);
                return false;
            }

            ElapsedMs = t;
            Apply(CompletionTimeline.At(t, false));
            return true;
        }

        private void Apply(in CompletionVisual visual)
        {
            Current = visual;
            _apply(visual);
        }
    }
}
