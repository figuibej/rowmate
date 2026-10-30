using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // Qué disco se ve en el lugar "centro alto" (HudLayout.BigCount; DN4b-14).
    public enum CenterDisc
    {
        None,
        Count,
        Paused,
    }

    // La cuenta grande y PAUSA en el mismo disco, uno por vez (04b-hud-exr/02-tech-design §3.5, §7.4; 04-design-notes §4.6,
    // §4.8; AC-WKT-14-18, AC-WKT-13-12; D-ANI-05). La vista (CenterCallouts, WP-B1) le pasa la cuenta (BigCountdown.ValueOf: 3,
    // 2, 1 o null) y si está en pausa; esto decide solo cómo se ve (CenterDiscTimeline).
    // - En pausa no hay cuenta grande: va PAUSA (la pausa manda sobre la cuenta, así el orden de SetCount y SetPaused en el mismo
    //   estado no importa).
    // - La cuenta entra en 200 (opacidad ease.enter y escala 0,85 → 1) y sale en 200 ("se suelta": 1 → 1,10) con el aro ya vacío;
    //   PAUSA entra y sale en 240 (0,96 ↔ 1). Cambio de disco (pausar con la cuenta a la vista; reanudar cuando la cuenta vuelve):
    //   el que sale funde a 0 en 0-120 sin escala (desde la opacidad que tenía: si nunca llegó a verse, no se ve; DT4b-18) y el que
    //   entra empieza a los 120 con su entrada de siempre. Nunca hay dos discos a la vista.
    // - La cifra que cambia (3 → 2 → 1) la rueda su TabularNumber; el dato no espera al disco.
    // - El aro (RingFraction) es lo que falta del paso / 3, del PillProgress que comparte con PillRowMotion: lleno en el tick del
    //   3, continuo entre ticks y vacío en el cambio. Mientras la cuenta se ve, esto sigue en el MotionDriver (C) y avisa apply en
    //   cada cuadro en que el aro se movió, para que la vista lo vuelva a dibujar.
    // - Fases: F en la entrada, la salida y el cambio de disco; C con la cuenta a la vista (el aro); sin disco o con PAUSA quieto,
    //   sale. Reducir movimiento: solo opacidad (la cuenta 160 / 120; PAUSA 240; el cambio, con los mismos tiempos y sin escala).
    //   Instant (volver del segundo plano, despertar) o sin armar: el disco que corresponde, sin entrada.
    public sealed class CenterDiscMotion : HudMotion
    {
        private enum Transition
        {
            None,
            CountIn,
            CountOut,
            PausedIn,
            PausedOut,
            SwapToPaused,
            SwapToCount,
        }

        private readonly PillProgress _progress;
        private readonly Action<DiscVisual> _apply;
        private int? _count;
        private bool _paused;
        private bool _initialized;
        private Transition _transition;
        private double _start;
        private bool _reduced;
        private double _fromOpacity = 1;
        private double _fromScale = 1;
        private long _endMs;
        private DiscVisual _visual = Rest(CenterDisc.None);
        private double _lastRing = double.NaN;

        public CenterDiscMotion(MotionDriver driver, PillProgress progress, Action<DiscVisual> apply)
            : base(driver)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public override bool IsFinite => IsRunning && _transition != Transition.None;

        // El disco al que va (o que ya se ve).
        public CenterDisc Shown { get; private set; }

        public int? Count => _count;

        // La última cifra no nula (la que se ve mientras la cuenta se va).
        public int? LastCount { get; private set; }

        public bool IsPaused => _paused;

        public DiscVisual Current => _visual;

        // La fracción del aro, en [0; 1]: 0 mientras la cuenta se va (el aro ya vacío) y sin cuenta a la vista.
        public double RingFraction
        {
            get
            {
                if (_transition == Transition.CountOut || _visual.CountOpacity <= 0 && Shown != CenterDisc.Count)
                {
                    return 0;
                }

                return CenterDiscTimeline.RingFraction(_progress.RemainingAt(Now));
            }
        }

        public void SetCount(int? value, bool instant)
        {
            Set(value, _paused, instant);
        }

        public void SetPaused(bool paused, bool instant)
        {
            Set(_count, paused, instant);
        }

        public void Set(int? count, bool paused, bool instant)
        {
            _count = count;
            if (count.HasValue)
            {
                LastCount = count;
            }

            _paused = paused;
            var desired = paused ? CenterDisc.Paused : count.HasValue ? CenterDisc.Count : CenterDisc.None;
            var animate = _initialized && CanAnimate(instant);
            _initialized = true;
            if (!animate)
            {
                Snap(desired);
                return;
            }

            if (desired == Shown)
            {
                return;
            }

            var previous = Shown;
            var v = _visual;
            Shown = desired;
            _reduced = ReduceMotion;
            _start = Now;
            switch (desired)
            {
                case CenterDisc.Count:
                    if (previous == CenterDisc.Paused || v.PausedOpacity > 0)
                    {
                        Begin(Transition.SwapToCount, v.PausedOpacity, v.PausedScale);
                    }
                    else
                    {
                        Begin(Transition.CountIn, 1, 1);
                    }

                    break;
                case CenterDisc.Paused:
                    if (previous == CenterDisc.Count || v.CountOpacity > 0)
                    {
                        Begin(Transition.SwapToPaused, v.CountOpacity, v.CountScale);
                    }
                    else
                    {
                        Begin(Transition.PausedIn, 1, 1);
                    }

                    break;
                default:
                    if (previous == CenterDisc.Count || v.CountOpacity > 0 && v.CountOpacity >= v.PausedOpacity)
                    {
                        Begin(Transition.CountOut, v.CountOpacity, v.CountScale);
                    }
                    else if (v.PausedOpacity > 0)
                    {
                        Begin(Transition.PausedOut, v.PausedOpacity, v.PausedScale);
                    }
                    else
                    {
                        Snap(CenterDisc.None);
                    }

                    break;
            }
        }

        protected override bool Advance(double nowSeconds)
        {
            if (_transition == Transition.None)
            {
                // La cuenta a la vista: el aro avanza con el PillProgress.
                if (Shown != CenterDisc.Count)
                {
                    return false;
                }

                ApplyRing();
                return true;
            }

            var t = HudTimelines.Ms(nowSeconds - _start);
            if (HudTimelines.Reached(t, _endMs))
            {
                // El último cuadro es el final de su línea de tiempo (lo que se va queda en opacidad 0 con su escala final).
                var last = VisualAt(_endMs);
                _transition = Transition.None;
                Apply(in last);
                return Shown == CenterDisc.Count;
            }

            Apply(VisualAt(t));
            return true;
        }

        protected override void OnStopped()
        {
            if (_transition != Transition.None)
            {
                _transition = Transition.None;
            }
        }

        private void Begin(Transition transition, double fromOpacity, double fromScale)
        {
            _transition = transition;
            _fromOpacity = Clamp01(fromOpacity);
            _fromScale = _reduced ? 1 : fromScale;
            _endMs = EndMs(transition, _reduced);
            Apply(VisualAt(0));
            Run();
        }

        private void Snap(CenterDisc disc)
        {
            Shown = disc;
            _transition = Transition.None;
            Apply(Rest(disc));
            if (disc == CenterDisc.Count)
            {
                Run();
            }
            else
            {
                Leave();
            }
        }

        private DiscVisual VisualAt(long tMs)
        {
            var reduced = _reduced;
            switch (_transition)
            {
                case Transition.CountIn:
                    return CenterDiscTimeline.CountIn(tMs, reduced);
                case Transition.CountOut:
                {
                    var v = CenterDiscTimeline.CountOut(tMs, reduced);
                    return new DiscVisual(v.CountOpacity * _fromOpacity, v.CountScale * _fromScale, 0, 1);
                }
                case Transition.PausedIn:
                    return CenterDiscTimeline.PausedIn(tMs, reduced);
                case Transition.PausedOut:
                {
                    var v = CenterDiscTimeline.PausedOut(tMs, reduced);
                    return new DiscVisual(0, 1, v.PausedOpacity * _fromOpacity, v.PausedScale * _fromScale);
                }
                case Transition.SwapToPaused:
                {
                    var v = CenterDiscTimeline.Swap(tMs, true, reduced);
                    return new DiscVisual(v.CountOpacity * _fromOpacity, _fromScale, v.PausedOpacity, v.PausedScale);
                }
                case Transition.SwapToCount:
                {
                    var v = CenterDiscTimeline.Swap(tMs, false, reduced);
                    return new DiscVisual(v.CountOpacity, v.CountScale, v.PausedOpacity * _fromOpacity, _fromScale);
                }
                default:
                    return Rest(Shown);
            }
        }

        private static long EndMs(Transition transition, bool reduced)
        {
            switch (transition)
            {
                case Transition.CountIn:
                    return CenterDiscTimeline.CountInEndMs(reduced);
                case Transition.CountOut:
                    return CenterDiscTimeline.CountOutEndMs(reduced);
                case Transition.PausedIn:
                    return (long)MotionSpecs.PausedLabelInMs;
                case Transition.PausedOut:
                    return (long)MotionSpecs.PausedLabelOutMs;
                case Transition.SwapToPaused:
                    return CenterDiscTimeline.SwapEndMs(true, reduced);
                case Transition.SwapToCount:
                    return CenterDiscTimeline.SwapEndMs(false, reduced);
                default:
                    return 0;
            }
        }

        private static DiscVisual Rest(CenterDisc disc)
        {
            switch (disc)
            {
                case CenterDisc.Count:
                    return new DiscVisual(1, 1, 0, 1);
                case CenterDisc.Paused:
                    return new DiscVisual(0, 1, 1, 1);
                default:
                    return new DiscVisual(0, 1, 0, 1);
            }
        }

        private void Apply(in DiscVisual visual)
        {
            _visual = visual;
            _lastRing = RingFraction;
            _apply(visual);
        }

        // Con la cuenta quieta a la vista, apply solo si el aro se movió.
        private void ApplyRing()
        {
            var ring = RingFraction;
            if (Math.Abs(ring - _lastRing) < 1e-6)
            {
                return;
            }

            _lastRing = ring;
            _apply(_visual);
        }
    }
}
