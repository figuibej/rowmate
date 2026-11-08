using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // El cambio de estado de una tarjeta del selector de escenario (08-entornos-selector/00-arte-entornos §8.2; 02-tech-design §4;
    // AC-SCN-22-03; WP-D1). Nada cambia el layout (D8-11): solo opacidades y escalas de elementos absolutos.
    //   la nueva:   anillo de color 0 → 1 en 160 (ease.enter) con escala 1,04 → 1 en 260 (ease.enter, el spring.snappy sin rebote);
    //               velo 0,15 → 0 en 160; miniatura 1 → 1,03 en 240 (ease.enter, por las uv: la caja no crece); nombre 400 → 700 por fundido cruzado en 120;
    //               check desde 60: escala 0,6 → 1 en 200 (ease.emphasis) y opacidad 0 → 1 en 120.
    //   la que se deja: anillo 1 → 0 en 160 (ease.exit); check 1 → 0 y 1 → 0,6 en 120; velo 0 → 0,15 en 160; miniatura 1,03 → 1 en
    //               240 (ease.move); nombre 700 → 400 en 120.
    // Un toque nuevo en medio arranca desde lo que se ve. Con reducir movimiento: sin escalas (en el acto) y opacidades en 120 lineal.
    // Sin armar (el primer cuadro) o instant: en el acto. Mientras corre, MotionDriver pide 60 fps (FramePacingPolicy). Lo que se anima
    // va en línea solo mientras corre (Style) y al terminar se suelta (Rest): en reposo manda la hoja de B. Sin reglas nuevas en el tema:
    // un .uss más, reglas nuevas en Base.uss o estilos en línea en reposo hacían asignar al panel del inmersivo en HudBudgetTests. Sin
    // asignaciones por cuadro.
    public sealed class EnvironmentCardMotion : TimedMotion
    {
        public const double TotalDurationMs = 280;
        public const float VeilOpacity = 0.15f, SelectedThumbScale = 1.03f, RingStartScale = 1.04f, CheckStartScale = 0.6f;
        public const float RingWidth = 2.5f, Radius = 12f;

        private const int Channels = 7;
        private const int RingOpacity = 0, RingScale = 1, CheckOpacity = 2, CheckScale = 3, Veil = 4, ThumbScale = 5, BoldName = 6;

        private readonly double[] _from = new double[Channels];
        private readonly double[] _to = new double[Channels];
        private readonly double[] _now = new double[Channels];
        private readonly double[] _delay = new double[Channels];
        private readonly double[] _duration = new double[Channels];
        private readonly EasingToken[] _easing = new EasingToken[Channels];
        private VisualElement? _ring;
        private VisualElement? _check;
        private VisualElement? _veil;
        private Image? _thumb;
        private VisualElement? _name;
        private VisualElement? _bold;
        private VisualElement? _thin;
        private bool _styled;
        private bool _known;

        public EnvironmentCardMotion(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => TotalDurationMs;

        // Lleva la tarjeta al estado elegido / no elegido. La clase se pone en el acto (los tests y la accesibilidad leen el estado);
        // la animación sale de lo que se ve.
        public void Apply(VisualElement card, bool selected, bool instant)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            Bind(card);
            card.EnableInClassList("rm-env-card--selected", selected);
            for (var i = 0; i < Channels; i++)
            {
                _from[i] = _now[i];
            }

            Target(selected);
            if (instant || !_known || !Driver.Armed)
            {
                Finish();
                Snap();
                return;
            }

            var reduce = Driver.Settings.ReduceMotion;

            var linear = Tokens.EaseLinear;
            if (selected)
            {
                Channel(RingOpacity, 0, 160, Tokens.EaseEnter);
                Channel(RingScale, 0, 260, Tokens.EaseEnter);
                Channel(CheckOpacity, 60, 120, Tokens.EaseEnter);
                Channel(CheckScale, 60, 200, Tokens.EaseEmphasis);
                Channel(Veil, 0, 160, Tokens.EaseMove);
                Channel(ThumbScale, 0, 240, Tokens.EaseEnter);
                Channel(BoldName, 0, 120, Tokens.EaseMove);
            }
            else
            {
                Channel(RingOpacity, 0, 160, Tokens.EaseExit);
                Channel(RingScale, 0, 160, Tokens.EaseExit);
                Channel(CheckOpacity, 0, 120, Tokens.EaseExit);
                Channel(CheckScale, 0, 120, Tokens.EaseExit);
                Channel(Veil, 0, 160, Tokens.EaseMove);
                Channel(ThumbScale, 0, 240, Tokens.EaseMove);
                Channel(BoldName, 0, 120, Tokens.EaseMove);
            }

            if (reduce)
            {
                // Sin escalas; las opacidades en 120 lineal y sin demora.
                foreach (var i in new[] { RingOpacity, CheckOpacity, Veil, BoldName })
                {
                    Channel(i, 0, 120, linear);
                }

                foreach (var i in new[] { RingScale, CheckScale, ThumbScale })
                {
                    _from[i] = _to[i];
                    Channel(i, 0, 1, linear);
                }
            }

            Finish();
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            for (var i = 0; i < Channels; i++)
            {
                var t = _duration[i] <= 0 ? 1 : Math.Max(0, Math.Min(1, (elapsedMs - _delay[i]) / _duration[i]));
                _now[i] = _from[i] + (_to[i] - _from[i]) * Easing.Evaluate(_easing[i], t);
            }

            Write();
        }

        protected override void OnFinished()
        {
            for (var i = 0; i < Channels; i++)
            {
                _now[i] = _to[i];
            }

            Rest();
        }

        private void Channel(int i, double delay, double duration, EasingToken easing)
        {
            _delay[i] = delay;
            _duration[i] = duration;
            _easing[i] = easing;
        }

        private void Target(bool selected)
        {
            _to[RingOpacity] = selected ? 1 : 0;
            _to[RingScale] = 1;
            _to[CheckOpacity] = selected ? 1 : 0;
            _to[CheckScale] = selected ? 1 : CheckStartScale;
            _to[Veil] = selected ? 0 : VeilOpacity;
            _to[ThumbScale] = selected ? SelectedThumbScale : 1;
            _to[BoldName] = selected ? 1 : 0;
            if (selected && _from[RingOpacity] < 0.5)
            {
                // La nueva entra con el anillo un poco más grande (desde lo que se ve: si ya estaba a medio salir, no salta).
                _from[RingScale] = RingStartScale - (RingStartScale - 1) * _from[RingOpacity];
            }
        }

        private void Snap()
        {
            for (var i = 0; i < Channels; i++)
            {
                _now[i] = _to[i];
            }

            _known = true;
            Rest();
        }

        private void Bind(VisualElement card)
        {
            if (_thumb != null)
            {
                return;
            }

            _ring = card.Q(className: "rm-env-card__ring-accent");
            _check = card.Q(className: "rm-env-card__check");
            _veil = card.Q(className: "rm-env-card__veil");
            _thumb = card.Q<Image>(className: "rm-env-card__thumb");
            _name = card.Q(className: "rm-env-card__name");
            _bold = card.Q(className: "rm-env-card__name--bold");
            _thin = card.Q(className: "rm-env-card__ring");
        }

        // Mientras anima: el velo y el check visibles (display flex) con su opacidad, el anillo fino en 1 border.default y el de color
        // armado encima (2,5 accent.primary, radio 12): así se funde sin animar el ancho del borde (D8-11).
        private void Style()
        {
            if (_styled)
            {
                return;
            }

            _styled = true;
            if (_thin != null)
            {
                Border(_thin, 1, TokenColors.ToColor(Tokens.BorderDefault));
            }

            if (_ring != null)
            {
                _ring.style.position = Position.Absolute;
                _ring.style.left = 0;
                _ring.style.top = 0;
                _ring.style.right = 0;
                _ring.style.bottom = 0;
                _ring.style.borderTopLeftRadius = Radius;
                _ring.style.borderTopRightRadius = Radius;
                _ring.style.borderBottomLeftRadius = Radius;
                _ring.style.borderBottomRightRadius = Radius;
                Border(_ring, RingWidth, TokenColors.ToColor(Tokens.AccentPrimary));
            }

            if (_veil != null)
            {
                _veil.style.display = DisplayStyle.Flex;
            }

            if (_check != null)
            {
                _check.style.display = DisplayStyle.Flex;
            }
        }

        private static void Border(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private void Write()
        {
            Style();
            Opacity(_ring, _now[RingOpacity]);
            Scale(_ring, _now[RingScale]);
            Opacity(_check, _now[CheckOpacity]);
            Scale(_check, _now[CheckScale]);
            Opacity(_veil, _now[Veil]);
            Zoom(_thumb, _now[ThumbScale]);
            Opacity(_bold, _now[BoldName]);
            Opacity(_name, 1 - _now[BoldName]);
        }

        // En reposo: la miniatura con su acercamiento en las uv (no es un estilo) y TODO lo demás suelto (StyleKeyword.Null), así manda la
        // hoja de B (Base.uss): elegida = anillo de 2,5 accent.primary, sin velo, con check, nombre 700; la otra al revés, que es el final
        // de cada canal. Nada en línea en reposo: los estilos en línea (o reglas nuevas en el tema) hacían asignar al panel del inmersivo
        // cada vez que se vuelve a resolver el estilo (HudBudgetTests.LongSeriesNoAllocationsPerFrame).
        private void Rest()
        {
            Zoom(_thumb, _now[ThumbScale]);
            if (!_styled)
            {
                return;
            }

            _styled = false;
            foreach (var element in new[] { _ring, _check, _veil, _bold, _name, _thin })
            {
                if (element == null)
                {
                    continue;
                }

                element.style.opacity = StyleKeyword.Null;
                element.style.scale = StyleKeyword.Null;
                element.style.display = StyleKeyword.Null;
                element.style.position = StyleKeyword.Null;
                element.style.left = StyleKeyword.Null;
                element.style.top = StyleKeyword.Null;
                element.style.right = StyleKeyword.Null;
                element.style.bottom = StyleKeyword.Null;
                element.style.borderTopLeftRadius = StyleKeyword.Null;
                element.style.borderTopRightRadius = StyleKeyword.Null;
                element.style.borderBottomLeftRadius = StyleKeyword.Null;
                element.style.borderBottomRightRadius = StyleKeyword.Null;
                element.style.borderTopWidth = StyleKeyword.Null;
                element.style.borderRightWidth = StyleKeyword.Null;
                element.style.borderBottomWidth = StyleKeyword.Null;
                element.style.borderLeftWidth = StyleKeyword.Null;
                element.style.borderTopColor = StyleKeyword.Null;
                element.style.borderRightColor = StyleKeyword.Null;
                element.style.borderBottomColor = StyleKeyword.Null;
                element.style.borderLeftColor = StyleKeyword.Null;
            }
        }

        // El acercamiento de la miniatura dentro de su recorte, por las uv de la imagen: el rectángulo no cambia (ni el layout ni la caja
        // que mira la auditoría de las tomas).
        private static void Zoom(Image? image, double scale)
        {
            if (image == null)
            {
                return;
            }

            var size = (float)(1 / Math.Max(scale, 1e-3));
            var offset = (1 - size) * 0.5f;
            image.uv = new Rect(offset, offset, size, size);
        }

        private static void Opacity(VisualElement? element, double value)
        {
            if (element != null)
            {
                element.style.opacity = (float)value;
            }
        }

        private static void Scale(VisualElement? element, double value)
        {
            if (element != null)
            {
                element.style.scale = new Scale(new Vector2((float)value, (float)value));
            }
        }
    }
}
