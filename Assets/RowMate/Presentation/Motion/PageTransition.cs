using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Cómo entra y sale una página apilada: Slide (el editor, It. 3), Fade (el inmersivo de la It. 4a) o Immerse (el inmersivo
    // de la It. 4b: "sumergirse").
    public enum PageTransitionKind
    {
        Slide,
        Fade,
        Immerse,
    }

    // Abrir y cerrar una página apilada (03-rutinas/04-design-notes §5.1; MotionSpecs.Page*):
    // - Push: la página entra con x +24 → 0 en 0-240 (ease.enter) y opacidad 0 → 1 en 0-160 (ease.enter); lo de atrás
    //   (pestañas y barra) va de x 0 a −24 y el oscurecido de 0 a 0,08 en 0-240 (ease.enter).
    // - Pop: la página va de x 0 a +24 en 0-200 (ease.exit) y de opacidad 1 a 0 en 40-200 (ease.exit); lo de atrás vuelve de
    //   x −24 a 0 y el oscurecido de 0,08 a 0 en 0-200 (ease.enter).
    // - Un Pop a mitad de la entrada arranca desde los valores actuales (sin saltos).
    // - Reducir movimiento: la página funde en 120 (ease.linear) y el oscurecido llega a su valor en el mismo tiempo, sin
    //   desplazamientos.
    // Al terminar llama a onDone (PageStack decide qué ocultar o sacar del árbol).
    // It. 4a (DN4a-05; 02-tech-design §6.3, §6.11; 04-design-notes §5.1): PageTransitionKind.Fade, la del inmersivo. Entra con
    // opacidad 0 → 1 en 240 (ease.enter) y sale 1 → 0 en 200 (ease.exit), entera, sin desplazamientos ni oscurecido; con
    // reducir movimiento, 120 lineal. La salida arranca desde la opacidad que tiene (la sesión volvió a reposo a mitad de la
    // entrada). La entrada "sumergirse" es de la 4b.
    // It. 4b (04b-hud-exr/02-tech-design §3.2, §3.3; AC-WKT-13-06, -08; DN4b-01, -09): PageTransitionKind.Immerse. Entra la página
    // NEGRA de 0 a 1 en 220 (ease.enter): Workout "funde a negro" (el fondo y la cascada los hace el HUD, ImmersiveCascadeMotion).
    // Sale DESPUÉS de la salida del HUD: queda a 1 los primeros 400 (el HUD se va en 0-200 y el fondo funde a negro en 200-400,
    // ImmersiveExitMotion) y funde de 1 a 0 en 400-640 (ease.enter): vuelve Workout. Con reducir movimiento, 120 lineal en las dos.
    public sealed class PageTransition : TimedMotion
    {
        private VisualElement? _page;
        private VisualElement? _behindA;
        private VisualElement? _behindB;
        private VisualElement? _dim;
        private Action? _onDone;
        private bool _entering;
        private bool _reduced;
        private bool _fade;
        private bool _immerse;
        private double _fromPageX;
        private double _fromPageOpacity;
        private double _fromBehindX;
        private double _fromDim;

        public PageTransition(MotionDriver driver, PageTransitionKind kind = PageTransitionKind.Slide)
            : base(driver)
        {
            Kind = kind;
            _fade = kind == PageTransitionKind.Fade || kind == PageTransitionKind.Immerse;
            _immerse = kind == PageTransitionKind.Immerse;
        }

        public PageTransitionKind Kind { get; }

        public bool IsEntering => IsRunning && _entering;

        protected override double TotalMs => _immerse
            ? _reduced ? MotionSpecs.ReducedImmersiveFadeMs : _entering ? MotionSpecs.ImmersiveBlackInMs : MotionSpecs.ImmersiveRevealStartMs + MotionSpecs.ImmersiveRevealMs
            : _fade
            ? _reduced ? MotionSpecs.ReducedImmersiveFadeMs : _entering ? MotionSpecs.ImmersiveInMs : MotionSpecs.ImmersiveOutMs
            : _reduced ? MotionSpecs.ReducedPageFadeMs : _entering ? MotionSpecs.PageInMs : MotionSpecs.PageOutMs;

        public void PlayIn(VisualElement page, VisualElement? behindA, VisualElement? behindB, VisualElement dim, Action onDone)
        {
            Interrupt();
            _page = page;
            _behindA = behindA;
            _behindB = behindB;
            _dim = dim;
            _onDone = onDone;
            _entering = true;
            _reduced = Driver.Settings.ReduceMotion;
            _fromPageX = MotionSpecs.PageShiftDp;
            _fromPageOpacity = 0;
            _fromBehindX = 0;
            _fromDim = 0;
            if (!Driver.Armed)
            {
                Apply(1);
                Done();
                return;
            }

            Begin();
        }

        public void PlayOut(VisualElement page, VisualElement? behindA, VisualElement? behindB, VisualElement dim, Action onDone)
        {
            // Desde los valores actuales (un Pop a mitad de la entrada no salta).
            var wasEntering = IsRunning && _entering && ReferenceEquals(_page, page);
            var pageX = wasEntering ? CurrentX(page) : 0;
            var pageOpacity = wasEntering ? page.resolvedStyle.opacity : 1;
            var behindX = wasEntering && behindA != null ? CurrentX(behindA) : -MotionSpecs.PageShiftDp;
            var dimOpacity = wasEntering ? dim.resolvedStyle.opacity : MotionSpecs.PageDimOpacity;
            Interrupt();
            _page = page;
            _behindA = behindA;
            _behindB = behindB;
            _dim = dim;
            _onDone = onDone;
            _entering = false;
            _reduced = Driver.Settings.ReduceMotion;
            _fromPageX = _reduced ? 0 : pageX;
            _fromPageOpacity = pageOpacity;
            _fromBehindX = _reduced ? 0 : behindX;
            _fromDim = dimOpacity;
            if (!Driver.Armed)
            {
                Apply(1);
                Done();
                return;
            }

            Apply(0);
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            Apply(elapsedMs);
        }

        protected override void OnFinished()
        {
            Done();
        }

        private void Interrupt()
        {
            // Sin OnFinished: la transición nueva arranca desde donde quedó la anterior.
            if (IsRunning)
            {
                _onDone = null;
                Finish();
            }
        }

        private void Done()
        {
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        // elapsedMs ≥ TotalMs = estado final.
        private void Apply(double elapsedMs)
        {
            if (_page == null || _dim == null)
            {
                return;
            }

            if (_fade)
            {
                ApplyFade(elapsedMs);
                return;
            }

            var shift = MotionSpecs.PageShiftDp;
            if (_entering)
            {
                if (_reduced)
                {
                    var u = Easing.Evaluate(Tokens.EaseLinear, Progress(elapsedMs, 0, MotionSpecs.ReducedPageFadeMs));
                    SetX(_page, 0);
                    _page.style.opacity = (float)u;
                    _dim.style.opacity = (float)(MotionSpecs.PageDimOpacity * u);
                    SetX(_behindA, 0);
                    SetX(_behindB, 0);
                    return;
                }

                var move = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.PageInMs));
                var fade = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.PageInFadeMs));
                SetX(_page, _fromPageX * (1 - move));
                _page.style.opacity = (float)fade;
                SetX(_behindA, -shift * move);
                SetX(_behindB, -shift * move);
                _dim.style.opacity = (float)(MotionSpecs.PageDimOpacity * move);
                return;
            }

            if (_reduced)
            {
                var u = Easing.Evaluate(Tokens.EaseLinear, Progress(elapsedMs, 0, MotionSpecs.ReducedPageFadeMs));
                SetX(_page, 0);
                _page.style.opacity = (float)(_fromPageOpacity * (1 - u));
                _dim.style.opacity = (float)(_fromDim * (1 - u));
                SetX(_behindA, 0);
                SetX(_behindB, 0);
                return;
            }

            var exit = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.PageOutMs));
            var exitFade = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, MotionSpecs.PageOutFadeStartMs, MotionSpecs.PageOutMs));
            var back = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.PageOutMs));
            SetX(_page, _fromPageX + (shift - _fromPageX) * exit);
            _page.style.opacity = (float)(_fromPageOpacity * (1 - exitFade));
            SetX(_behindA, _fromBehindX * (1 - back));
            SetX(_behindB, _fromBehindX * (1 - back));
            _dim.style.opacity = (float)(_fromDim * (1 - back));
        }

        // El fundido del inmersivo: solo la opacidad de la página (lo de atrás ni se corre ni se oscurece).
        private void ApplyFade(double elapsedMs)
        {
            SetX(_page, 0);
            SetX(_behindA, 0);
            SetX(_behindB, 0);
            _dim!.style.opacity = 0;
            if (_reduced)
            {
                var u = Easing.Evaluate(Tokens.EaseLinear, Progress(elapsedMs, 0, MotionSpecs.ReducedImmersiveFadeMs));
                _page!.style.opacity = (float)(_entering ? u : _fromPageOpacity * (1 - u));
                return;
            }

            if (_immerse)
            {
                if (_entering)
                {
                    _page!.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.ImmersiveBlackInMs));
                    return;
                }

                var reveal = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, MotionSpecs.ImmersiveRevealStartMs,
                    MotionSpecs.ImmersiveRevealStartMs + MotionSpecs.ImmersiveRevealMs));
                _page!.style.opacity = (float)(_fromPageOpacity * (1 - reveal));
                return;
            }

            if (_entering)
            {
                _page!.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.ImmersiveInMs));
                return;
            }

            var exit = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.ImmersiveOutMs));
            _page!.style.opacity = (float)(_fromPageOpacity * (1 - exit));
        }

        private static double CurrentX(VisualElement element)
        {
            return element.resolvedStyle.translate.x;
        }

        private static void SetX(VisualElement? element, double x)
        {
            if (element == null)
            {
                return;
            }

            element.style.translate = Math.Abs(x) < 1e-4 ? new Translate(0, 0) : new Translate((float)x, 0);
        }
    }
}
