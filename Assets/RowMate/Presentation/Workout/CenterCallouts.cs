using System;
using System.Globalization;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El lugar "centro alto" del HUD: la cuenta grande y PAUSA en el MISMO disco, uno por vez (AC-WKT-14-18, AC-WKT-13-12;
    // D-ANI-05; 04b-hud-exr/02-tech-design §2.4, §3.5; 04-design-notes §3.6, §4.6, §4.8; DN4b-14, DN4b-20; DT4b-04, M7). Dos
    // elementos, hermanos de las zonas del HUD, centrados en HudLayout.BigCount (un disco de 72·s):
    //   #hud-big-countdown .rm-hud-big-countdown   UN TabularNumber num.hero 64/800 blanco, centrado por la banda de las
    //                                              mayúsculas; su PLACA (lo dibuja el mismo elemento): el disco glass.fallback y el
    //                                              aro de 3 (línea central a 34,5) con la pista al 20 % y el arco al 100 % del color
    //                                              base del paso. El arco es lo que falta del paso / 3 (PillProgress): va de
    //                                              360°·(1 − f) a las 12, y el borde que se mueve avanza en el sentido del reloj
    //   #hud-paused-label .rm-hud-paused-label     UN Label hudPausedLabel 12/700, tracking 0,08 em, blanco, con su disco
    //                                              glass.fallback y el aro ENTERO de 3 en phase.paused (fondo y borde del mismo
    //                                              elemento, en px por código) y el glifo de pausa (dos barras de 4,67 × 16,33
    //                                              separadas 4,67, radio 1,17) que dibuja su generateVisualContent
    // Sin eventos ni foco. Quietos: no laten. Un disco por vez (CenterDiscMotion, WP-D1): en pausa no hay cuenta grande; el que
    // sale funde en 0-120 y el que entra empieza a los 120. Al volver del segundo plano o despertar, el disco que corresponde, sin
    // entrada. La escala del HUD (s) va junto con la del disco (la entrada 0,85 → 1, la salida 1 → 1,10, PAUSA 0,96 ↔ 1), con el
    // origen en el centro del disco.
    public sealed class CenterCallouts : IDisposable
    {
        public const float DigitSizeDp = 64;
        public const float PausedSizeDp = 12;

        private readonly UiContext _ui;
        private readonly CenterDiscMotion _motion;
        private Color _ringColor;
        private float _scale = 1;
        private DpRect _disc;
        private bool _placed;
        private DiscVisual _visual;
        private bool _disposed;

        public CenterCallouts(UiContext ui, PillProgress progress)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            Count = new TabularNumber(ui.Motion) { name = "hud-big-countdown" };
            Count.AddToClassList("rm-type-num-hero");
            Count.AddToClassList("rm-hud-big-countdown");
            Count.SetFontSize(DigitSizeDp);
            Count.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Count.style.position = Position.Absolute;
            Count.style.display = DisplayStyle.None;
            Count.focusable = false;
            Count.SetPlate(DrawCountPlate);
            Count.RegisterCallback<GeometryChangedEvent>(_ => PlaceCount());
            _ringColor = TokenColors.ToColor(Tokens.TextPrimary);

            Paused = HudText.Label("hud-paused-label", "rm-type-header-s", PausedSizeDp);
            Paused.AddToClassList("rm-hud-paused-label");
            Paused.style.position = Position.Absolute;
            // 0,08 em en la unidad de TextCore (8), no en dp (QA-V4b-02).
            Paused.style.letterSpacing = (float)TypographyUssWriter.LetterSpacingUnits(HudGeometry.PausedLabelTrackingEm);
            Paused.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Paused.style.unityTextAlign = TextAnchor.MiddleCenter;
            Paused.style.whiteSpace = WhiteSpace.NoWrap;
            var diameter = (float)HudGeometry.BigCountDiameter;
            Paused.style.width = diameter;
            Paused.style.height = diameter;
            var radius = diameter / 2f;
            Paused.style.borderTopLeftRadius = radius;
            Paused.style.borderTopRightRadius = radius;
            Paused.style.borderBottomLeftRadius = radius;
            Paused.style.borderBottomRightRadius = radius;
            var ring = (float)HudGeometry.BigCountRingStroke;
            Paused.style.borderLeftWidth = ring;
            Paused.style.borderRightWidth = ring;
            Paused.style.borderTopWidth = ring;
            Paused.style.borderBottomWidth = ring;
            var paused = TokenColors.ToColor(Tokens.PhasePaused);
            Paused.style.borderLeftColor = paused;
            Paused.style.borderRightColor = paused;
            Paused.style.borderTopColor = paused;
            Paused.style.borderBottomColor = paused;
            Paused.style.backgroundColor = TokenColors.ToColor(Tokens.GlassFallback);
            // El texto debajo del glifo: las mayúsculas de +5,8 a +14,5 desde el centro (el centro de la línea baja 10,15).
            Paused.style.paddingTop = (float)(2 * 10.15);
            Paused.style.paddingBottom = 0;
            Paused.style.paddingLeft = 0;
            Paused.style.paddingRight = 0;
            Paused.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
            Paused.style.display = DisplayStyle.None;
            Paused.focusable = false;
            Paused.generateVisualContent += DrawPausedGlyph;

            _motion = new CenterDiscMotion(ui.Motion, progress, ApplyVisual);
            Count.RegisterCallback<DetachFromPanelEvent>(_ => _motion.Stop());
        }

        public TabularNumber Count { get; }

        public Label Paused { get; }

        public CenterDiscMotion Motion => _motion;

        // El disco del último lugar (HudLayout.BigCount), en dp de ventana.
        public DpRect Disc => _disc;

        public void RenderTexts(Localizer texts)
        {
            Paused.text = texts.Get(L10nKeys.HudPausedLabel);
        }

        // La cuenta (BigCountdown.ValueOf: 3, 2, 1 o null), si está en pausa y el tipo del paso (el color del aro).
        public void Apply(int? count, bool paused, StepType? type, bool instant)
        {
            if (_disposed)
            {
                return;
            }

            if (type.HasValue)
            {
                _ringColor = TokenColors.ToColor(StepTypeInfo.Color(type.Value));
            }

            if (count.HasValue)
            {
                Count.SetValue(count.Value.ToString(CultureInfo.InvariantCulture), count.Value, !instant);
            }

            _motion.Set(count, paused, instant);
        }

        // El lugar (HudLayout.BigCount, dp de ventana) y la escala del HUD.
        public void Place(DpRect disc, double scale)
        {
            _disc = disc;
            _scale = (float)scale;
            _placed = true;
            var size = (float)HudGeometry.BigCountDiameter;
            var cx = (float)(disc.Left + disc.Width / 2);
            var cy = (float)(disc.Top + disc.Height / 2);
            Paused.style.left = cx - size / 2f;
            Paused.style.top = cy - size / 2f;
            PlaceCount();
            ApplyScales();
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

        private void PlaceCount()
        {
            if (!_placed)
            {
                return;
            }

            var cx = (float)(_disc.Left + _disc.Width / 2);
            var cy = (float)(_disc.Top + _disc.Height / 2);
            var width = Count.layout.width > 0 ? Count.layout.width : Count.NaturalBoxWidth;
            var height = Count.FontSize > 0 ? Count.FontSize : DigitSizeDp;
            // El centro de la banda de las mayúsculas es el centro de la caja (alto 1 em).
            Count.style.left = cx - width / 2f;
            Count.style.top = cy - height / 2f;
            Count.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
        }

        private void ApplyVisual(DiscVisual visual)
        {
            _visual = visual;
            Show(Count, visual.CountOpacity);
            Show(Paused, visual.PausedOpacity);
            ApplyScales();
            Count.MarkDirtyRepaint();
        }

        private void ApplyScales()
        {
            var count = _scale * (float)(_visual.CountScale > 0 ? _visual.CountScale : 1);
            var paused = _scale * (float)(_visual.PausedScale > 0 ? _visual.PausedScale : 1);
            Count.style.scale = new Scale(new Vector3(count, count, 1));
            Paused.style.scale = new Scale(new Vector3(paused, paused, 1));
        }

        private static void Show(VisualElement element, double opacity)
        {
            if (opacity <= 0.0005)
            {
                element.style.display = DisplayStyle.None;
                element.style.opacity = StyleKeyword.Null;
                return;
            }

            element.style.display = DisplayStyle.Flex;
            element.style.opacity = opacity >= 0.9995 ? (StyleFloat)StyleKeyword.Null : (float)opacity;
        }

        // La placa de la cuenta grande (M7): el disco y el aro, centrados en la caja de la cifra (en dp base del elemento).
        private void DrawCountPlate(MeshGenerationContext context, Rect box)
        {
            var painter = context.painter2D;
            var center = new Vector2(Count.layout.width / 2f, Count.layout.height / 2f);
            var radius = (float)(HudGeometry.BigCountDiameter / 2);
            painter.fillColor = TokenColors.ToColor(Tokens.GlassFallback);
            painter.BeginPath();
            painter.Arc(center, radius, 0, 360);
            painter.ClosePath();
            painter.Fill();

            var ringRadius = (float)HudGeometry.BigCountRingRadius;
            var track = _ringColor;
            track.a *= (float)HudGeometry.BigCountRingTrackAlpha;
            painter.strokeColor = track;
            painter.lineWidth = (float)HudGeometry.BigCountRingStroke;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            painter.Arc(center, ringRadius, 0, 360);
            painter.Stroke();

            var fraction = _motion.RingFraction;
            var arc = PillShapes.RingArc(fraction);
            if (arc.IsEmpty)
            {
                return;
            }

            painter.strokeColor = _ringColor;
            painter.BeginPath();
            // Painter2D mide los ángulos desde las 3 en el sentido del reloj (y hacia abajo); el aro, desde las 12.
            painter.Arc(center, ringRadius, (float)(arc.StartDegrees - 90), (float)(arc.EndDegrees - 90));
            painter.Stroke();
        }

        // El glifo de pausa (el pause de Material, 28 base): dos barras de 4,67 × 16,33 separadas 4,67, radio 1,17, de −14,5 a
        // +1,8 desde el centro del disco, en blanco.
        private void DrawPausedGlyph(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var w = Paused.layout.width;
            var h = Paused.layout.height;
            var cx = w / 2f;
            var cy = h / 2f;
            const float bar = 4.67f;
            const float tall = 16.33f;
            const float gap = 4.67f;
            const float r = 1.17f;
            var top = cy - 14.5f;
            painter.fillColor = TokenColors.ToColor(Tokens.TextPrimary);
            RoundedRect(painter, cx - gap / 2f - bar, top, bar, tall, r);
            RoundedRect(painter, cx + gap / 2f, top, bar, tall, r);
        }

        private static void RoundedRect(Painter2D painter, float x, float y, float width, float height, float radius)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(x + radius, y));
            painter.LineTo(new Vector2(x + width - radius, y));
            painter.ArcTo(new Vector2(x + width, y), new Vector2(x + width, y + radius), radius);
            painter.LineTo(new Vector2(x + width, y + height - radius));
            painter.ArcTo(new Vector2(x + width, y + height), new Vector2(x + width - radius, y + height), radius);
            painter.LineTo(new Vector2(x + radius, y + height));
            painter.ArcTo(new Vector2(x, y + height), new Vector2(x, y + height - radius), radius);
            painter.LineTo(new Vector2(x, y + radius));
            painter.ArcTo(new Vector2(x, y), new Vector2(x + radius, y), radius);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
