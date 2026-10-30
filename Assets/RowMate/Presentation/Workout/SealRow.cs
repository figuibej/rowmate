using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El sello del paso que cerró solo con objetivos (AC-WKT-09-08, -09; D-ANI-05; 04b-hud-exr/02-tech-design §2.5, §3.6;
    // 04-design-notes §3.1, §3.7, §4.7; DN4b-02): la TERCERA fila de la barra de pasos, en el mismo vidrio.
    //   #hud-seal .rm-hud-seal          slot que recorta (overflow: hidden): alto 0 → 22 con la barra (52 → 74); relleno 12 a los
    //   │                                lados; el contenido (17) quieto arriba del slot y 5 de aire abajo: se descubre al abrirse
    //   ├── #hud-seal-icon .rm-hud-seal__icon   IconView 14: check_circle relleno (cumplido) o error de contorno (no alcanzado): dos
    //   │                                        IconView (#hud-seal-icon y #hud-seal-icon-missed), uno a la vista por vez (cambiar de glifo y de fuente en el mismo IconView lo
    //   │                                        dejaba en el cuadrado genérico: el estilo de la otra fuente todavía no estaba resuelto)
    //   ├── #hud-seal-title .rm-hud-seal__text  el título 13/700 (Inter 700) en el color del veredicto, a 5 del ícono
    //   └── #hud-seal-text .rm-hud-seal__text   " · " + el detalle 12/600 (Inter 600) en blanco
    // El título y el detalle van en DOS etiquetas (el plan B de la sonda S-B4b, §7.0: el texto enriquecido con dos pesos de Inter
    // en un solo Label necesita una tabla de pesos que los FontAsset del repo no tienen): +1 elemento con el sello, 59 en el peor
    // caso (§3.11). Colores: Objetivo cumplido en state.success (#06D6A0), No alcanzado en phase.paused (#FFD166), el detalle en
    // blanco. Si no entra, el contenido se ACHICA con una escala pareja desde su centro (en el acto), nunca "…".
    // El movimiento es SealMotion (WP-D1): t = 0 en HudSeal.At (el instante monótono del segundo que cerró el paso); todo dentro de
    // los 2 s. La barra (StepInfoBar) toma el alto que le avisa barHeight. Sin panel no mide ni se mueve.
    public sealed class SealRow : VisualElement, IDisposable
    {
        public const string UssClass = "rm-hud-seal";
        public const float IconSizeDp = 14;
        public const float TitleSizeDp = 13;
        public const float DetailSizeDp = 12;
        public const float IconGapDp = 5;
        public const string Separator = " · ";

        private readonly UiContext _ui;
        private readonly SealMotion _motion;
        private readonly Action<double> _barHeight;
        private HudSeal? _shownSeal;
        private HudSeal? _requested;
        private HudSeal? _pending;
        private float _fit = 1;
        private float _iconScale = 1;
        private bool _disposed;

        public SealRow(UiContext ui, Action<double> barHeight)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _barHeight = barHeight ?? throw new ArgumentNullException(nameof(barHeight));
            name = "hud-seal";
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.FlexStart;
            style.overflow = Overflow.Hidden;
            style.flexShrink = 0;
            style.height = 0;
            style.paddingLeft = (float)HudGeometry.SealRowPadH;
            style.paddingRight = (float)HudGeometry.SealRowPadH;

            MetIcon = NewIcon("hud-seal-icon", Icons.CheckCircle, true, TokenColors.ToColor(Tokens.StateSuccess));
            MissedIcon = NewIcon("hud-seal-icon-missed", Icons.Error, false, TokenColors.ToColor(Tokens.PhasePaused));
            Add(MetIcon);
            Add(MissedIcon);

            Title = HudText.Label("hud-seal-title", "rm-type-header-s", TitleSizeDp);
            Title.AddToClassList(UssClass + "__text");
            Title.style.whiteSpace = WhiteSpace.NoWrap;
            Title.style.flexShrink = 0;
            Title.style.height = (float)HudGeometry.SealRowHeight;
            Title.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(Title);

            Detail = HudText.Label("hud-seal-text", "rm-type-button-s", DetailSizeDp);
            Detail.AddToClassList(UssClass + "__text");
            Detail.style.whiteSpace = WhiteSpace.NoWrap;
            Detail.style.flexShrink = 0;
            Detail.style.height = (float)HudGeometry.SealRowHeight;
            Detail.style.unityTextAlign = TextAnchor.MiddleLeft;
            Detail.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Add(Detail);

            _motion = new SealMotion(ui.Motion, ApplyVisual, ApplyContent);
            RegisterCallback<GeometryChangedEvent>(_ => Fit());
            RegisterCallback<DetachFromPanelEvent>(_ => _motion.Stop());
            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            ApplyContent(null);
        }

        // El ícono que se ve (el de cumplido o el de no alcanzado).
        public IconView Icon => _met ? MetIcon : MissedIcon;

        public IconView MetIcon { get; }

        public IconView MissedIcon { get; }

        private bool _met = true;

        public Label Title { get; }

        public Label Detail { get; }

        public SealMotion Motion => _motion;

        // El sello que se ve (null sin sello).
        public HudSeal? Shown => _shownSeal;

        public bool IsShowing => _motion.IsShowing;

        // La escala de ajuste del contenido (1 si entra).
        public float FitScale => _fit;

        // El texto como se lee: el título y el detalle (para las pruebas y el arnés).
        public string Text => _shownSeal == null ? "" : Title.text + Detail.text;

        // Un estado del HUD: el sello del último paso cerrado con objetivos (null = ninguno nuevo; el que se ve sigue su reloj).
        public void Apply(HudSeal? seal, bool instant)
        {
            if (_disposed || seal == null)
            {
                return;
            }

            if (panel == null)
            {
                _pending = seal;
                return;
            }

            if (_requested != null && _requested.StepIndex == seal.StepIndex && _requested.At.Equals(seal.At))
            {
                return;
            }

            _requested = seal;
            _motion.Show(seal, instant);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _motion.Stop();
        }

        private void OnAttach()
        {
            if (_pending == null)
            {
                return;
            }

            var seal = _pending;
            _pending = null;
            Apply(seal, true);
        }

        private void ApplyVisual(SealVisual visual)
        {
            var open = Math.Max(0, visual.BarHeight - HudGeometry.TopBarHeightSteps);
            style.height = (float)open;
            _barHeight(visual.BarHeight);
            var opacity = visual.OldContentOpacity > 0 ? visual.OldContentOpacity : visual.ContentOpacity;
            var o = (float)Math.Max(0, Math.Min(1, opacity));
            MetIcon.style.opacity = o;
            MissedIcon.style.opacity = o;
            Title.style.opacity = o;
            Detail.style.opacity = o;
            _iconScale = (float)visual.IconScale;
            ApplyScales();
        }

        private void ApplyContent(HudSeal? seal)
        {
            _shownSeal = seal;
            if (seal == null)
            {
                Title.text = "";
                Detail.text = "";
                return;
            }

            var met = seal.Verdict == StepResult.Met;
            var color = TokenColors.ToColor(met ? Tokens.StateSuccess : Tokens.PhasePaused);
            _met = met;
            MetIcon.style.display = met ? DisplayStyle.Flex : DisplayStyle.None;
            MissedIcon.style.display = met ? DisplayStyle.None : DisplayStyle.Flex;
            Title.text = seal.Title;
            Title.style.color = color;
            Detail.text = Separator + seal.Detail;
            Fit();
        }

        // Lo que no entra se achica (escala pareja desde el centro del slot, en el acto); nunca "…".
        private void Fit()
        {
            if (panel == null || _shownSeal == null)
            {
                return;
            }

            var available = contentRect.width;
            if (!(available > 0))
            {
                return;
            }

            var natural = IconSizeDp + IconGapDp + Measure(Title) + Measure(Detail);
            var fit = natural > available ? available / natural : 1f;
            if (Math.Abs(fit - _fit) < 0.0001f)
            {
                return;
            }

            _fit = fit;
            ApplyScales();
        }

        private void ApplyScales()
        {
            var center = layout.width / 2f;
            Scale(MetIcon, _fit * _iconScale, center);
            Scale(MissedIcon, _fit * _iconScale, center);
            Scale(Title, _fit, center);
            Scale(Detail, _fit, center);
        }

        // Cada pieza escala alrededor del centro del slot (el contenido se achica junto, sin un elemento más que lo envuelva).
        private static void Scale(VisualElement piece, float scale, float slotCenterX)
        {
            if (Math.Abs(scale - 1) < 0.0001f)
            {
                piece.style.scale = StyleKeyword.Null;
                piece.style.transformOrigin = StyleKeyword.Null;
                return;
            }

            var originX = slotCenterX - piece.layout.x;
            piece.style.transformOrigin = new TransformOrigin(new Length(originX, LengthUnit.Pixel), new Length(50, LengthUnit.Percent));
            piece.style.scale = new Scale(new Vector3(scale, scale, 1));
        }

        private static IconView NewIcon(string elementName, string codepoint, bool filled, Color color)
        {
            var icon = new IconView(codepoint, filled, IconSizeDp) { name = elementName };
            icon.AddToClassList(UssClass + "__icon");
            icon.style.marginTop = (float)((HudGeometry.SealRowHeight - IconSizeDp) / 2);
            icon.style.marginRight = IconGapDp;
            icon.style.color = color;
            icon.style.display = filled ? DisplayStyle.Flex : DisplayStyle.None;
            return icon;
        }

        private static float Measure(Label label)
        {
            return string.IsNullOrEmpty(label.text) ? 0 : label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
        }
    }
}
