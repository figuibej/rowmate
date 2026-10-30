using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Tamaño de una tarjeta del HUD (ART_BIBLE §3.2, D-ART-02; 04-design-notes §3.2): grande (SPM), mediana (Split y Watts) y
    // chica (kcal y BPM). Todo a tamaño BASE (la zona escala por hudScale).
    public enum HudCardSize
    {
        Large,
        Medium,
        Small,
    }

    // Una tarjeta de vidrio del HUD (AC-WKT-17-01 a -06, AC-WKT-10-05; 02-tech-design §6.5 de la It. 4a; 04-design-notes §3.2,
    // §4.4, §5.3):
    //   #hud-<métrica> .rm-hud-card .rm-hud-glass --large|--medium|--small [--target] [--off]
    //   │   (el halo, 12 por fuera, solo con objetivo: GlowTexture en 9 partes, color base @20 %, lo dibuja la tarjeta: M4)
    //   ├── .rm-hud-card__label     rótulo 13 / 12 / 11 /600, tracking 0,08 em, color base (pulso metric.hr.hud)
    //   ├── [.rm-hud-card__heart]   solo BPM: ♥ 11, absoluto a la izquierda del rótulo, a 4 (M6)
    //   ├── .rm-hud-card__number (TabularNumber num.hero / num.l / num.m)   con margin-left = el HUECO de la flecha (0,48 em)
    //   │       la flecha "▲" / "▼" (Inter 700 a 0,4 em, en el hueco, su línea de base por código) es su PREFIJO (M1) y, en Watts,
    //   │       4 + "W" 13/500 en la variante .hud @80 %, en la línea de base de las cifras (DV-31), su SUFIJO (M2)
    //   └── #hud-target-<métrica> .rm-hud-card__target   2 + "obj. N" 12/600 text.secondary, margin-left = el hueco
    // Con objetivo (Q-60, DV-32): borde del color base @70 % de 1,5; halo por fuera; el hueco existe toda la duración del paso
    // (el número no se corre al cruzar el umbral, DN4a-15); el valor blanco si cumple o state.offTarget.hud con la flecha si
    // está fuera. Sin objetivo: el valor en la variante .hud de su métrica (AC-APP-06-01), borde 1 @10 % y sin hueco.
    // Medidas en em y líneas de base por código (USS no tiene em ni baseline: QA-G4aT-14). El achique de las cifras (los topes
    // T1 a T5 de HudGeometry, D-10) va por el tamaño de letra de la fila del valor (cifras, unidad, hueco y flecha juntos), así la
    // tarjeta no suma dos elementos por valor (presupuesto de 60, 02-tech-design §6.13); el alto de la fila no cambia.
    // It. 4b (04b-hud-exr/02-tech-design §3.11, el presupuesto del HUD; R4a-03), sin cambiar lo que se ve:
    //   M1 la flecha la dibuja su TabularNumber como PREFIJO en el hueco (Inter 700 a 0,4 em, con su línea de base, color, alfa
    //      y desplazamiento): sale .rm-hud-card__arrow;
    //   M2 la unidad W la dibuja su TabularNumber como SUFIJO (Inter 500 13, @80 %, a 4, en la línea de base de las cifras): salen
    //      .rm-hud-card__value y .rm-hud-card__unit;
    //   M4 el halo de objetivo lo dibuja la tarjeta (el mismo GlowTexture en 9 partes, por fuera, en su generateVisualContent):
    //      sale .rm-hud-card__glow;
    //   M6 el corazón de BPM va en posición absoluta en su tarjeta (centrado en el alto del rótulo): sale .rm-hud-card__label-row.
    public sealed class HudCard : VisualElement
    {
        public const string UssClass = "rm-hud-card";
        public const float LabelGapDp = 2;
        public const float TargetGapDp = 2;
        public const float UnitGapDp = 4;
        public const float UnitSizeDp = 13;
        public const float TargetSizeDp = 12;
        public const float HeartSizeDp = 11;
        public const float HeartGapDp = 4;
        public const float BorderRestDp = 1;
        public const float BorderRestAlpha = 0.10f;
        public const float BorderTargetAlpha = 0.70f;
        public const float GlowAlpha = 0.20f;
        public const float GlowOutsideDp = 12;
        public const string Arrow = "▲";
        public const string ArrowDown = "▼";

        private readonly UiContext _ui;
        private readonly bool _withGlow;
        private readonly bool _withUnit;
        private readonly bool _withArrow;
        private readonly Label? _target;
        private readonly IconView? _heart;
        private readonly OffTargetMotion? _offTarget;
        private readonly Vertex[] _glowVertices = new Vertex[16];
        private static readonly ushort[] GlowIndices = BuildGlowIndices();
        private Texture2D? _glowTexture;
        private Color _glowTint;
        private float _glowOpacity;
        private string _arrowGlyph = Arrow;
        private readonly TargetDecor? _decor;
        private bool? _hasTarget;
        private bool _off;
        private float _fit = 1;
        private string _labelText = "";
        private string _unitText = "";
        // Lo que se ve del objetivo (04-design-notes §5.5): 0 sin objetivo … 1 con el hueco abierto y el borde de 1,5.
        private double _decorLevel;
        // El tamaño base que pide HudZones, el que se ve y desde dónde crece mientras corre el cambio de paso.
        private Vector2 _sizeTo = new Vector2(float.NaN, float.NaN);
        private Vector2 _sizeShown = new Vector2(float.NaN, float.NaN);
        private Vector2 _sizeFrom = new Vector2(float.NaN, float.NaN);
        private double _widthMix = 1;
        private double _heightMix = 1;
        // La flecha que espera a que el hueco termine de abrirse (una tarjeta que empieza el paso ya fuera).
        private bool _arrowDeferred;
        private Color _offColor;
        private float _arrowDirection = -1f;

        public HudCard(UiContext ui, string elementName, MetricKind kind, HudCardSize size, bool withTarget, bool withUnit, bool heart)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Kind = kind;
            Size = size;
            name = elementName;
            AddToClassList(UssClass);
            AddToClassList("rm-hud-glass");
            AddToClassList(UssClass + "--" + SizeName(size));
            pickingMode = PickingMode.Ignore;
            ValueSizeDp = ValueSizeFor(size);
            LabelSizeDp = LabelSizeFor(size);
            style.minWidth = MinWidthFor(size);

            // M4: el halo lo dibuja la tarjeta, por fuera (generateVisualContent, antes que los hijos).
            _withGlow = withTarget;
            if (withTarget)
            {
                _glowTint = HudText.WithAlpha(TokenColors.ToColor(MetricPalette.For(kind)), GlowAlpha);
                generateVisualContent += DrawGlow;
            }

            Label = HudText.Label(elementName + "-label", "rm-type-label", LabelSizeDp);
            Label.AddToClassList(UssClass + "__label");
            Label.style.color = TokenColors.ToColor(MetricPalette.LabelFor(kind));
            Add(Label);
            if (heart)
            {
                // M6: el corazón va en posición absoluta, a la izquierda del rótulo y centrado en su alto (como la fila de la 4a,
                // sin la fila): el rótulo deja su lugar con su margen izquierdo.
                _heart = new IconView(Icons.Favorite, true, HeartSizeDp);
                _heart.AddToClassList(UssClass + "__heart");
                _heart.pickingMode = PickingMode.Ignore;
                _heart.style.position = Position.Absolute;
                _heart.style.color = TokenColors.ToColor(Tokens.MetricHrHud);
                Label.style.marginLeft = HeartSizeDp + HeartGapDp;
                Heartbeat = new HeartbeatAnimator(ui.Motion, _heart);
                Add(_heart);
                Label.RegisterCallback<GeometryChangedEvent>(_ => PlaceHeart());
            }

            Number = new TabularNumber(ui.Motion) { name = elementName + "-value" };
            Number.AddToClassList(UssClass + "__number");
            Number.AddToClassList(NumClassFor(size));
            Number.style.fontSize = ValueSizeDp;
            Number.style.color = TokenColors.ToColor(MetricPalette.HudFor(kind));
            _withArrow = withTarget;
            if (withTarget)
            {
                // M1: la flecha es el prefijo del número, en el hueco; empieza oculta.
                Number.SetGutterPrefix(Arrow, "rm-type-header-s", (float)(HudGeometry.ArrowEm * ValueSizeDp), TokenColors.ToColor(Tokens.StateOffTargetHud),
                    (float)(HudGeometry.ArrowGutterEm * ValueSizeDp), (float)(HudGeometry.ArrowUpBaselineEm * ValueSizeDp));
                Number.SetPrefixVisual(0, 0);
                _offTarget = new OffTargetMotion(ui.Motion, Number);
            }

            // M2: la unidad (W) es el sufijo del número.
            _withUnit = withUnit;
            Add(Number);

            if (withTarget)
            {
                _target = HudText.Label("hud-target-" + MetricName(kind), "rm-type-button-s", TargetSizeDp);
                _target.AddToClassList(UssClass + "__target");
                _target.style.color = TokenColors.ToColor(Tokens.TextSecondary);
                _target.style.display = DisplayStyle.None;
                _target.style.marginTop = TargetGapDp;
                // Su alto crece desde 0 en el cambio de paso: recorta su propio texto (sin un slot más: presupuesto, §6.13).
                _target.style.overflow = Overflow.Hidden;
                Add(_target);
                _decor = new TargetDecor(ui.Motion, this);
            }

            Number.RegisterCallback<GeometryChangedEvent>(_ => PlaceArrowAndUnit());
            SetTarget(false, true);
            ApplyFit(1);
        }

        public MetricKind Kind { get; }

        public HudCardSize Size { get; }

        public float ValueSizeDp { get; }

        public float LabelSizeDp { get; }

        public Label Label { get; }

        public TabularNumber Number { get; }

        // M2: la unidad es el sufijo del número ("" sin unidad).
        public string UnitText => _withUnit ? Number.SuffixText : "";

        public Label? Target => _target;

        // M1: la flecha es el prefijo del número; se ve con alfa > 0.
        public bool HasArrow => _withArrow;

        public bool ArrowShown => _withArrow && Number.PrefixAlpha > 0.001f;

        public string ArrowGlyph => _withArrow ? Number.PrefixText : "";

        // M4: el halo que dibuja la tarjeta, su opacidad (0 sin objetivo) y su tinte (color base @20 %).
        public bool HasGlow => _withGlow;

        public float GlowOpacity => _withGlow ? _glowOpacity : 0;

        public Color GlowTint => _glowTint;

        public IconView? Heart => _heart;

        public HeartbeatAnimator? Heartbeat { get; }

        public OffTargetMotion? OffTarget => _offTarget;

        public bool HasTarget => _hasTarget == true;

        public bool IsOff => _off;

        public float Fit => _fit;

        // El hueco de la flecha en dp base (0 sin objetivo).
        public float GutterDp => HasTarget ? (float)(HudGeometry.ArrowGutterEm * ValueSizeDp) : 0f;

        // ── Anchos naturales a tamaño BASE para HudGeometry (DN4a-13) ──

        // Las cifras (Watts con 4 + "W", el sufijo de la caja), sin el hueco.
        public double NaturalValueWidth => HudText.BaseBoxWidth(Number, _fit);

        // Con el corazón (BPM), el rótulo con el corazón de 11 y su aire de 4 (HudNaturalWidths).
        public double NaturalLabelWidth => _heart != null ? HeartSizeDp + HeartGapDp + Label.layout.width : Label.layout.width;

        public double NaturalTargetWidth => _target != null && HasTarget ? _target.layout.width : 0;

        public void SetLabel(string text)
        {
            if (_labelText != text)
            {
                _labelText = text;
                Label.text = text;
            }
        }

        public void SetUnit(string text)
        {
            if (_withUnit && _unitText != text)
            {
                _unitText = text;
                ApplyUnit();
            }
        }

        // M2: la W como sufijo, a la escala de las cifras.
        private void ApplyUnit()
        {
            if (!_withUnit)
            {
                return;
            }

            var color = HudText.WithAlpha(TokenColors.ToColor(MetricPalette.HudFor(Kind)), (float)HudGeometry.UnitAlpha);
            Number.SetSuffix(_unitText, "rm-type-caption", UnitSizeDp * _fit, color, UnitGapDp * _fit);
        }

        // Un valor sin semáforo (kcal, BPM).
        public void SetValue(string text, double value, bool instant)
        {
            Number.SetValue(text, value, !instant);
        }

        // Un valor con semáforo posible (SPM, Split, Watts). stepChanged: el paso cambió (el empujón no sale al empezar un
        // paso: el borde, el halo y obj. que aparecen ya llaman la atención).
        public void SetMetric(HudMetric metric, bool stepChanged, bool instant)
        {
            Number.SetValue(metric.Value, metric.RollValue, !instant);
            var wasOff = _off && HasTarget;
            SetTarget(metric.HasTarget, instant);
            if (_target != null)
            {
                var text = metric.TargetText ?? "";
                if (_target.text != text)
                {
                    _target.text = text;
                }
            }

            if (stepChanged)
            {
                _offTarget?.ResetBumpWindow();
            }

            _off = metric.HasTarget && metric.OffTarget;
            EnableInClassList(UssClass + "--off", _off);
            var color = !metric.HasTarget ? TokenColors.ToColor(MetricPalette.HudFor(Kind))
                : _off ? TokenColors.ToColor(Tokens.StateOffTargetHud)
                : TokenColors.ToColor(Tokens.TextHudPrimary);
            if (_withArrow)
            {
                var down = metric.Arrow == TargetArrow.Down;
                var glyph = down ? ArrowDown : Arrow;
                if (_arrowGlyph != glyph)
                {
                    _arrowGlyph = glyph;
                    PlaceArrowAndUnit();
                }
            }

            if (_offTarget != null)
            {
                var direction = metric.Arrow == TargetArrow.Down ? 1f : -1f;
                var bump = _off && !wasOff && !stepChanged && HasTarget;
                // Una tarjeta que gana objetivo ya fuera: el valor cruza al rojo en el acto (150) y la flecha entra cuando el
                // hueco terminó de abrirse, así nunca se dibuja encima de la primera cifra ni fuera de la tarjeta (WP-D, §5.5).
                var opening = _decor != null && _decor.DefersArrow;
                _arrowDeferred = opening && _off;
                _offColor = color;
                _arrowDirection = direction;
                _offTarget.Set(color, _off && !opening, direction, bump, instant);
            }
            else
            {
                Number.style.color = color;
            }
        }

        // La escala de las cifras (1 = entran; HudLayout.ValueScale). El alto de la fila no cambia.
        public void ApplyFit(double fit)
        {
            var f = (float)(fit > 0 ? Math.Min(1.0, fit) : 0.01);
            _fit = f;
            var size = ValueSizeDp * f;
            Number.SetFontSize(size);
            var spare = (ValueSizeDp - size) / 2f;
            Number.style.marginTop = LabelGapDp + spare;
            Number.style.marginBottom = spare;
            ApplyUnit();
            ApplyGutter();
            PlaceArrowAndUnit();
        }

        // Con objetivo: borde, halo, hueco y obj. (el hueco existe toda la duración de un paso con objetivo). El estado cambia en
        // el acto (HasTarget); cómo se ve, con el cambio de paso de 04-design-notes §5.5 (TargetDecor): en el acto al armar,
        // con Instant (al volver del segundo plano) y sin el reloj armado.
        private void SetTarget(bool hasTarget, bool instant)
        {
            if (_hasTarget == hasTarget)
            {
                return;
            }

            // Un cambio a mitad del anterior: ese salta a su final (con el estado de antes) y este arranca desde ahí.
            _decor?.Finish();
            var first = _hasTarget == null;
            _hasTarget = hasTarget;
            EnableInClassList(UssClass + "--target", hasTarget);
            if (_decor != null && !first && !instant && _ui.Motion.Armed)
            {
                _sizeFrom = _sizeShown;
                _decor.Play(hasTarget);
                return;
            }

            var level = hasTarget ? 1.0 : 0.0;
            ApplyDecor(level, level, level, level);
            ApplySizeMix(1, 1);
        }

        // El tamaño base de la tarjeta (el rectángulo de HudGeometry / s, lo pone HudZones). Mientras corre el cambio de paso,
        // la tarjeta va hacia él con el mismo tiempo que su hueco y su obj. (la columna se estira sin salto, §5.5); si no, en el
        // acto (el primer cálculo, una rotación o un cambio de ventana).
        public void ApplyBaseSize(float width, float height)
        {
            _sizeTo = new Vector2(width, height);
            if (_decor != null && _decor.IsRunning)
            {
                ApplySizeMix(_widthMix, _heightMix);
                return;
            }

            ApplySizeMix(1, 1);
        }

        // Para las pruebas y el arnés: el cambio de paso de esta tarjeta en curso.
        public bool IsDecorAnimating => _decor != null && _decor.IsRunning;

        // decor: color del borde y opacidad del halo; layout: el hueco y el grosor del borde; objHeight y objText: el alto y la
        // opacidad de obj. Todo en 0 … 1 (0 = sin objetivo).
        private void ApplyDecor(double decor, double layout, double objHeight, double objText)
        {
            _decorLevel = layout;
            var rest = HudText.WithAlpha(TokenColors.ToColor(Tokens.TextPrimary), BorderRestAlpha);
            var target = HudText.WithAlpha(TokenColors.ToColor(MetricPalette.For(Kind)), BorderTargetAlpha);
            var width = BorderRestDp + ((float)Tokens.StrokeTarget.Value - BorderRestDp) * (float)layout;
            SetBorder(TokenColors.Lerp(rest, target, decor), width);
            if (_withGlow)
            {
                var opacity = decor > 0.0005 ? (float)Math.Min(1.0, decor) : 0f;
                if (Math.Abs(opacity - _glowOpacity) > 1e-5f)
                {
                    _glowOpacity = opacity;
                    MarkDirtyRepaint();
                }
            }

            if (_target != null)
            {
                if (objHeight <= 0 && objText <= 0)
                {
                    _target.style.display = DisplayStyle.None;
                    _target.style.height = StyleKeyword.Null;
                    _target.style.opacity = StyleKeyword.Null;
                    _target.style.marginTop = TargetGapDp;
                }
                else if (objHeight >= 1 && objText >= 1)
                {
                    _target.style.display = DisplayStyle.Flex;
                    _target.style.height = StyleKeyword.Null;
                    _target.style.opacity = StyleKeyword.Null;
                    _target.style.marginTop = TargetGapDp;
                }
                else
                {
                    _target.style.display = DisplayStyle.Flex;
                    _target.style.height = (float)(TargetSizeDp * HudGeometry.LineBox * objHeight);
                    _target.style.marginTop = (float)(TargetGapDp * objHeight);
                    _target.style.opacity = (float)objText;
                }
            }

            ApplyGutter();
        }

        // widthMix, heightMix: de 0 (el tamaño de antes del cambio) a 1 (el que pide HudZones).
        private void ApplySizeMix(double widthMix, double heightMix)
        {
            _widthMix = widthMix;
            _heightMix = heightMix;
            if (float.IsNaN(_sizeTo.x))
            {
                return;
            }

            var size = _sizeTo;
            if (!float.IsNaN(_sizeFrom.x) && (widthMix < 1 || heightMix < 1))
            {
                size = new Vector2(Mathf.LerpUnclamped(_sizeFrom.x, _sizeTo.x, (float)widthMix),
                    Mathf.LerpUnclamped(_sizeFrom.y, _sizeTo.y, (float)heightMix));
            }

            if (size != _sizeShown)
            {
                _sizeShown = size;
                style.width = size.x;
                style.height = size.y;
            }
        }

        // El cambio de paso terminó: el estado final limpio y, si la tarjeta empezó el paso fuera, entra la flecha. overrunMs:
        // cuánto se pasó del final el cuadro en que terminó (TimedMotion.OverrunMs). Si ya pasó también lo que dura la entrada
        // de la flecha (un cuadro muy tarde, o el reloj de prueba que avanza segundos de una vez), la flecha queda en su lugar
        // en el acto: lo que se ve es el presente, sin una entrada que empieza tarde.
        private void DecorFinished(double overrunMs)
        {
            var level = _hasTarget == true ? 1.0 : 0.0;
            ApplyDecor(level, level, level, level);
            ApplySizeMix(1, 1);
            if (_arrowDeferred && _offTarget != null)
            {
                _arrowDeferred = false;
                _offTarget.Set(_offColor, _off, _arrowDirection, false, overrunMs >= MotionSpecs.OffTargetArrowMs);
            }
        }

        // El halo usa la textura que generó el HUD al armarse.
        public void SetGlowTexture(Texture2D texture)
        {
            if (_withGlow)
            {
                _glowTexture = texture;
                MarkDirtyRepaint();
            }
        }

        // M4: el halo en 9 partes con GlowTexture (transparente por dentro): del borde de afuera de la tarjeta hacia afuera 12 dp,
        // con las esquinas de 26 dp (12 + el radio 14) y los bordes estirados; el centro no se dibuja. Tinte = color base @20 %
        // por la opacidad del objetivo (el cambio de paso lo funde). Sin asignaciones: los vértices son del elemento.
        private void DrawGlow(MeshGenerationContext context)
        {
            if (_glowTexture == null || !(_glowOpacity > 0.0005f))
            {
                return;
            }

            var rect = new Rect(0, 0, layout.width, layout.height);
            if (!(rect.width > 0) || !(rect.height > 0))
            {
                return;
            }

            var outside = GlowOutsideDp;
            var corner = GlowTexture.GlowDp + GlowTexture.RadiusDp;
            var x0 = -outside;
            var y0 = -outside;
            var x3 = rect.width + outside;
            var y3 = rect.height + outside;
            var x1 = x0 + corner;
            var y1 = y0 + corner;
            var x2 = x3 - corner;
            var y2 = y3 - corner;
            // Las UV van en [0; 1] de la textura: en Unity 6.3 el renderer las lleva al atlas solo (uvRegion quedó obsoleto).
            var mesh = context.Allocate(16, GlowIndices.Length, _glowTexture);
            var slice = (float)GlowTexture.SlicePx / GlowTexture.SizePx;
            var tint = _glowTint;
            tint.a *= _glowOpacity;
            Color32 tint32 = tint;
            var xs = new Vector4(x0, x1, x2, x3);
            var ys = new Vector4(y0, y1, y2, y3);
            var us = new Vector4(0, slice, 1 - slice, 1);
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var uv = new Vector2(us[column], 1 - us[row]);
                    _glowVertices[row * 4 + column] = new Vertex
                    {
                        position = new Vector3(xs[column], ys[row], Vertex.nearZ),
                        tint = tint32,
                        uv = uv,
                    };
                }
            }

            mesh.SetAllVertices(_glowVertices);
            mesh.SetAllIndices(GlowIndices);
        }

        // Las 8 partes del borde del 9-slice (sin el centro), en sentido horario (UI Toolkit dibuja los triángulos en ese orden).
        private static ushort[] BuildGlowIndices()
        {
            var indices = new ushort[8 * 6];
            var k = 0;
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    if (row == 1 && column == 1)
                    {
                        continue;
                    }

                    var a = (ushort)(row * 4 + column);
                    var b = (ushort)(a + 1);
                    var c = (ushort)(a + 4);
                    var d = (ushort)(c + 1);
                    indices[k++] = a;
                    indices[k++] = b;
                    indices[k++] = d;
                    indices[k++] = a;
                    indices[k++] = d;
                    indices[k++] = c;
                }
            }

            return indices;
        }

        // M6: el corazón centrado en el alto del rótulo, a la izquierda (donde estaba en la fila de la 4a).
        private void PlaceHeart()
        {
            if (_heart == null)
            {
                return;
            }

            var label = Label.layout;
            var heartHeight = _heart.layout.height > 0 ? _heart.layout.height : HeartSizeDp;
            _heart.style.left = label.x - HeartSizeDp - HeartGapDp;
            _heart.style.top = label.y + (label.height - heartHeight) / 2f;
        }

        private void SetBorder(Color color, float width)
        {
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
            // El borde va DENTRO del relleno: la tarjeta mide lo de HudGeometry (CardHeight, 04-design-notes §3.2) y el contenido
            // no se corre al pasar de 1 a 1,5 dp.
            var padH = PadHFor(Size) - width;
            var padV = PadVFor(Size) - width;
            style.paddingLeft = padH;
            style.paddingRight = padH;
            style.paddingTop = padV;
            style.paddingBottom = padV;
        }

        // El hueco de la flecha: 0,48 em del valor (con la escala de las cifras) por lo que se ve del objetivo (0 … 1).
        private void ApplyGutter()
        {
            var gutter = (float)(HudGeometry.ArrowGutterEm * ValueSizeDp * _fit * _decorLevel);
            Number.style.marginLeft = gutter;
            if (_target != null)
            {
                _target.style.marginLeft = gutter;
            }
        }

        // La flecha en el hueco, pegada a la izquierda y con su línea de base 0,196 em (▲) o 0,214 em (▼) sobre la de las cifras
        // (04-design-notes §3.2); la unidad en la línea de base de las cifras.
        private void PlaceArrowAndUnit()
        {
            // M1: la flecha, en el hueco y con su línea de base sobre la de las cifras; M2: la unidad va en la línea de base de
            // las cifras (TabularNumber.SetSuffix). El color y la opacidad de la flecha los conserva el prefijo.
            var size = Number.FontSize > 0 ? Number.FontSize : ValueSizeDp * _fit;
            if (_withArrow)
            {
                var arrowSize = (float)(HudGeometry.ArrowEm * size);
                var lift = (float)((_arrowGlyph == ArrowDown ? HudGeometry.ArrowDownBaselineEm : HudGeometry.ArrowUpBaselineEm) * size);
                Number.SetGutterPrefix(_arrowGlyph, "rm-type-header-s", arrowSize, TokenColors.ToColor(Tokens.StateOffTargetHud),
                    (float)(HudGeometry.ArrowGutterEm * size), lift);
            }
        }

        public static string MetricName(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.Spm:
                    return "spm";
                case MetricKind.Split:
                    return "split";
                case MetricKind.Watts:
                    return "watts";
                case MetricKind.Calories:
                    return "kcal";
                case MetricKind.HeartRate:
                    return "bpm";
                default:
                    return kind.ToString().ToLowerInvariant();
            }
        }

        public static float ValueSizeFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? (float)HudGeometry.LargeValue : size == HudCardSize.Medium ? (float)HudGeometry.MediumValue : (float)HudGeometry.SmallValue;
        }

        public static float LabelSizeFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? (float)HudGeometry.LargeLabel : size == HudCardSize.Medium ? (float)HudGeometry.MediumLabel : (float)HudGeometry.SmallLabel;
        }

        public static float MinWidthFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? (float)HudGeometry.LargeMinWidth : size == HudCardSize.Medium ? (float)HudGeometry.MediumMinWidth : (float)HudGeometry.SmallCardMinWidth;
        }

        public static float PadHFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? (float)HudGeometry.LargePadH : size == HudCardSize.Medium ? (float)HudGeometry.MediumPadH : (float)HudGeometry.SmallPadH;
        }

        public static float PadVFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? (float)HudGeometry.LargePadV : size == HudCardSize.Medium ? (float)HudGeometry.MediumPadV : (float)HudGeometry.SmallPadV;
        }

        private static string NumClassFor(HudCardSize size)
        {
            return size == HudCardSize.Large ? "rm-type-num-hero" : size == HudCardSize.Medium ? "rm-type-num-l" : "rm-type-num-m";
        }

        private static string SizeName(HudCardSize size)
        {
            return size == HudCardSize.Large ? "large" : size == HudCardSize.Medium ? "medium" : "small";
        }

        // El cambio de paso de una tarjeta que gana o pierde objetivo (04-design-notes §5.5; ANIMATION_BIBLE §2.7; MotionSpecs):
        // - gana: borde (1 dp @10 % → 1,5 del color base @70 %, grosor y color), halo (0 → 1), hueco de la flecha (0 → 0,48 em)
        //   y alto de obj. (0 → 16,9 base) en 0-240 ease.move; el texto de obj. funde en 60-240 ease.enter; la tarjeta crece con
        //   el hueco (ancho) y con obj. (alto). La flecha, si el paso empieza fuera, entra al terminar (DecorFinished).
        // - pierde: obj. funde en 0-120 ease.exit y cierra su alto en 80-280 ease.move; borde, halo y hueco vuelven en 0-240.
        // - reducir movimiento: el hueco, el grosor y los altos en el acto; solo funden el borde, el halo y el texto de obj.
        // Sin empujón (§5.3) y sin asignaciones por cuadro (estructuras de estilo).
        private sealed class TargetDecor : TimedMotion
        {
            private readonly HudCard _card;
            private bool _entering;
            private bool _reduced;

            public TargetDecor(MotionDriver driver, HudCard card)
                : base(driver)
            {
                _card = card;
            }

            // La flecha espera mientras el hueco se abre (con reducir movimiento ya está abierto).
            public bool DefersArrow => IsRunning && _entering && !_reduced;

            protected override double TotalMs => _entering ? MotionSpecs.TargetDecorMs : MotionSpecs.CollapseEndMs;

            public void Play(bool entering)
            {
                Finish();
                _entering = entering;
                _reduced = Driver.Settings.ReduceMotion;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var move = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, MotionSpecs.TargetDecorMs));
                if (_entering)
                {
                    var text = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, MotionSpecs.TargetTextInStartMs, MotionSpecs.TargetTextInEndMs));
                    var layout = _reduced ? 1 : move;
                    _card.ApplyDecor(move, layout, layout, text);
                    _card.ApplySizeMix(layout, layout);
                    return;
                }

                var textOut = 1 - Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.CollapseFadeMs));
                var height = 1 - Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, MotionSpecs.CollapseStartMs, MotionSpecs.CollapseEndMs));
                var decor = 1 - move;
                if (_reduced)
                {
                    _card.ApplyDecor(decor, 0, 0, 0);
                    _card.ApplySizeMix(1, 1);
                    return;
                }

                _card.ApplyDecor(decor, decor, height, textOut);
                _card.ApplySizeMix(move, 1 - height);
            }

            protected override void OnFinished()
            {
                _card.DecorFinished(OverrunMs);
            }
        }
    }
}
