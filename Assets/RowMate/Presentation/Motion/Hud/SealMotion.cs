using System;
using RowMate.App.Workout;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // El sello "Objetivo cumplido / No alcanzado" en la tercera fila de la barra de pasos (04b-hud-exr/02-tech-design §3.6, §7.4;
    // 04-design-notes §4.7; AC-WKT-09-08; D-ANI-05). t = 0 en HudSeal.At (LastOutcomeAt: el instante monótono del segundo que
    // cerró el paso, del mismo reloj que el MotionDriver), y todo pasa dentro de los 2 s (SealTimeline.At): la barra crece de 52
    // a 74 en 0-240, el contenido funde en 40-240, el ícono se estampa en 60-300, el contenido se va en 1800-1920 y la barra
    // vuelve a 52 en 1800-2000.
    // - Un sello nuevo con el anterior a la vista (pasos de 2 s o menos) lo reemplaza (SealTimeline.Swap): el viejo funde en 0-80,
    //   el nuevo entra en 80-200 con el ícono que se estampa otra vez, la barra sigue abierta y los 2 s vuelven a empezar.
    // - content avisa qué sello tiene que mostrar la etiqueta (su texto y su ícono): el nuevo al empezar, el viejo hasta los 80
    //   de un reemplazo, y null al terminar.
    // - En pausa sigue sus 2 s (es el reloj de la interfaz). Con Instant (volver del segundo plano, despertar) o sin armar: uno
    //   cuyos 2 s ya pasaron no se muestra; uno que todavía tiene tiempo se ve en el acto, sin entrada, por lo que le queda.
    // - Fases: F en la entrada (0-300) y en el reemplazo (0-320); C en el sostén (300-1800); F en la salida (1800-2000); sale.
    //   Reducir movimiento: el alto en el acto, el contenido en 160 / 120, el ícono sin escala.
    // - Clear: sin Instant, la salida desde ahora (1800-2000); con Instant, cerrado en el acto.
    public sealed class SealMotion : HudMotion
    {
        private static readonly long DurationMs = SealTimeline.EndMs;

        private readonly Action<SealVisual> _apply;
        private readonly Action<HudSeal?>? _content;
        private double _origin;
        private bool _swapping;
        private bool _skipEntry;
        private bool _reduced;
        private bool _showing;
        private bool _hasVisual;
        private SealVisual _visual;

        public SealMotion(MotionDriver driver, Action<SealVisual> apply, Action<HudSeal?>? content = null)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _content = content;
        }

        public override bool IsFinite
        {
            get
            {
                if (!IsRunning)
                {
                    return false;
                }

                var t = ElapsedMs;
                if (_swapping)
                {
                    return true;
                }

                if (!_skipEntry && t < SealTimeline.EntryEndMs)
                {
                    return true;
                }

                return t >= (long)MotionSpecs.SealContentOutStartMs && t < DurationMs;
            }
        }

        // Dentro de sus 2 s.
        public bool IsShowing => _showing;

        // El sello que corre (el nuevo, durante un reemplazo).
        public HudSeal? Seal { get; private set; }

        // El que tiene que mostrar la etiqueta ahora.
        public HudSeal? Displayed { get; private set; }

        public SealVisual Current => _visual;

        // Los ms desde el instante del sello, en el último cuadro.
        public long ElapsedMs { get; private set; }

        public void Show(HudSeal seal, bool instant)
        {
            if (seal == null)
            {
                throw new ArgumentNullException(nameof(seal));
            }

            // El mismo sello que ya corre: nada (los estados del HUD lo repiten mientras dura). Si se había detenido (la vista
            // salió del panel), sigue por lo que le queda, sin entrada.
            var same = _showing && Seal != null && Seal.StepIndex == seal.StepIndex && Seal.At.Equals(seal.At);
            if (same && IsRunning)
            {
                return;
            }

            var now = Now;
            var origin = seal.At <= now ? seal.At : now;
            var t = HudTimelines.Ms(now - origin);
            if (HudTimelines.Reached(t, DurationMs))
            {
                // Sus 2 s ya pasaron (volver del segundo plano): no se muestra.
                if (_showing)
                {
                    Clear(true);
                }

                return;
            }

            // El anterior sigue a la vista (antes de su salida): reemplazo.
            var replace = !same && _showing && Displayed != null && ElapsedMs < (long)MotionSpecs.SealContentOutStartMs;
            Seal = seal;
            _origin = origin;
            _reduced = ReduceMotion;
            _showing = true;
            _skipEntry = same || !CanAnimate(instant);
            _swapping = !_skipEntry && replace;
            Evaluate(t);
            SyncDisplayed(t);
            Run();
        }

        public void Clear(bool instant)
        {
            if (!_showing)
            {
                return;
            }

            if (!CanAnimate(instant))
            {
                Close();
                Leave();
                return;
            }

            if (ElapsedMs >= (long)MotionSpecs.SealContentOutStartMs)
            {
                return;
            }

            // La salida desde ahora.
            _swapping = false;
            _skipEntry = true;
            _origin = Now - MotionSpecs.SealContentOutStartMs / 1000.0;
            Evaluate((long)MotionSpecs.SealContentOutStartMs);
            Run();
        }

        protected override bool Advance(double nowSeconds)
        {
            if (!_showing)
            {
                return false;
            }

            var t = HudTimelines.Ms(nowSeconds - _origin);
            if (HudTimelines.Reached(t, DurationMs))
            {
                Close();
                return false;
            }

            Evaluate(t);
            SyncDisplayed(t);
            return true;
        }

        // La etiqueta muestra el sello que corre, salvo los primeros 80 de un reemplazo (el viejo funde con su texto).
        private void SyncDisplayed(long tMs)
        {
            if (!ReferenceEquals(Displayed, Seal) && (!_swapping || HudTimelines.Reached(tMs, (long)MotionSpecs.SealSwapOutMs)))
            {
                SetDisplayed(Seal);
            }
        }

        protected override void OnStopped()
        {
            _swapping = false;
        }

        private void Evaluate(long tMs)
        {
            ElapsedMs = tMs;
            if (_swapping && HudTimelines.Reached(tMs, SealTimeline.SwapEndMs))
            {
                _swapping = false;
            }

            SealVisual visual;
            if (_swapping)
            {
                visual = SealTimeline.Swap(tMs, _reduced);
            }
            else if (_skipEntry && tMs < SealTimeline.EntryEndMs)
            {
                visual = SealTimeline.At(SealTimeline.EntryEndMs, _reduced);
            }
            else
            {
                visual = SealTimeline.At(tMs, _reduced);
            }

            Apply(in visual);
        }

        // Cerrado: la barra en 52 y sin contenido.
        private void Close()
        {
            _showing = false;
            _swapping = false;
            ElapsedMs = DurationMs;
            var closed = SealTimeline.At(DurationMs, _reduced);
            Apply(in closed);
            Seal = null;
            SetDisplayed(null);
        }

        private void Apply(in SealVisual visual)
        {
            if (_hasVisual && visual.BarHeight == _visual.BarHeight && visual.ContentOpacity == _visual.ContentOpacity
                && visual.IconScale == _visual.IconScale && visual.OldContentOpacity == _visual.OldContentOpacity)
            {
                return;
            }

            _hasVisual = true;
            _visual = visual;
            _apply(visual);
        }

        private void SetDisplayed(HudSeal? seal)
        {
            if (ReferenceEquals(Displayed, seal))
            {
                return;
            }

            Displayed = seal;
            _content?.Invoke(seal);
        }
    }
}
