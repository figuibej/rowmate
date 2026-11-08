using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // La ilustración de la vista de estado del Bluetooth (00-arte §3.1; D10-06, D10-07; A10-07): un elemento con malla propia (dos anillos
    // quietos de radio 64 y 84 con trazo 1,5 en text.primary al 8 % y al 5 %, y un disco de 112 en surface.card) y los íconos hijos:
    // bluetooth_disabled 64 (state.danger; text.tertiary sin Bluetooth LE) y bluetooth (accent.primary), que solo se ve en "se despierta".
    //   #device-bt-art .rm-bt-art (176 × 176; 112 con alto compacto: disco 72, ícono 48, anillos de 44 y 56)
    //   ├── #device-bt-icon        (IconView; respira 1 → 0,72 → 1 en 3 200 ms: apagado y sin permiso)
    //   └── #device-bt-icon-wake   (IconView bluetooth, opacidad 0 salvo en "se despierta")
    // La respiración es continua (no cuenta para Settle) y solo corre con la vista a la vista; con reducir movimiento, quieta en 1.
    // "Se despierta" (BleMotion.Wake): el color del ícono de state.danger a accent.primary en 0-240, el glifo cruza en 80-200, el disco
    // 1 → 1,06 en 0-360 y sale un anillo 56 → 84 (alfa 0,45 → 0, 900 ms).
    public sealed class BluetoothIllustration : VisualElement, IMotion
    {
        private const int Segments = BleMotionSpecs.SonarSegments;

        private readonly MotionDriver? _motion;
        private double _breathStart;
        private bool _breathes;
        private bool _compact;
        private bool _danger;
        private double _discScale = 1;
        private double _wakeRing = -1;

        public BluetoothIllustration(MotionDriver? motion)
        {
            _motion = motion;
            name = "device-bt-art";
            AddToClassList("rm-bt-art");
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            Icon = new IconView(Icons.BluetoothDisabled, false, (float)BleMotionSpecs.IllustrationIconDp) { name = "device-bt-icon" };
            Icon.AddToClassList("rm-bt-art__icon");
            Icon.AddToClassList("rm-device-state__icon");
            Add(Icon);
            WakeIcon = new IconView(Icons.Bluetooth, false, (float)BleMotionSpecs.IllustrationIconDp) { name = "device-bt-icon-wake" };
            WakeIcon.AddToClassList("rm-bt-art__icon");
            WakeIcon.AddToClassList("rm-bt-art__icon--wake");
            WakeIcon.style.opacity = 0;
            Add(WakeIcon);
            SetCompact(false);
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => UpdateRegistration());
            RegisterCallback<DetachFromPanelEvent>(_ => _motion?.Remove(this));
        }

        public IconView Icon { get; }

        public IconView WakeIcon { get; }

        public bool IsFinite => false;

        public bool Breathes => _breathes;

        public bool IsCompact => _compact;

        // El disco (para la entrada 0,94 → 1 de BleMotion.DiscIn): la ilustración entera escala desde el centro.
        public VisualElement Disc => this;

        public void SetIcon(string glyph, bool danger, bool breathes)
        {
            Icon.SetGlyph(glyph, false);
            _danger = danger;
            Icon.EnableInClassList("rm-device-state__icon--danger", danger);
            if (breathes != _breathes)
            {
                _breathes = breathes;
                _breathStart = _motion?.Now ?? 0;
                Icon.style.opacity = StyleKeyword.Null;
                UpdateRegistration();
            }
        }

        // Alto compacto (L640, TH): la ilustración baja a 112.
        public void SetCompact(bool compact)
        {
            _compact = compact;
            var box = (float)(compact ? BleMotionSpecs.CompactIllustrationBoxDp : BleMotionSpecs.IllustrationBoxDp);
            var icon = (float)(compact ? BleMotionSpecs.CompactIllustrationIconDp : BleMotionSpecs.IllustrationIconDp);
            style.width = box;
            style.height = box;
            Icon.SetSize(icon);
            WakeIcon.SetSize(icon);
            MarkDirtyRepaint();
        }

        public bool Tick(double nowSeconds)
        {
            if (panel == null || !_breathes)
            {
                return false;
            }

            if (_motion != null && _motion.Settings.ReduceMotion)
            {
                Icon.style.opacity = StyleKeyword.Null;
                return true;
            }

            if (!BleMotion.IsDisplayed(this))
            {
                return true;
            }

            var o = BleMotionSpecs.StateBreathOpacity((nowSeconds - _breathStart) * 1000.0);
            Icon.style.opacity = (float)o;
            return true;
        }

        // Un cuadro de "se despierta" (BleMotion.Wake), elapsedMs desde el cambio.
        public void ApplyWake(double elapsedMs, bool reduced)
        {
            var from = TokenColors.ToColor(_danger ? Tokens.StateDanger : Tokens.TextTertiary);
            var to = TokenColors.ToColor(Tokens.AccentPrimary);
            if (reduced)
            {
                var g = BleMotionSpecs.Segment(elapsedMs, 0, BleMotionSpecs.ReducedWakeGlyphMs);
                Icon.style.color = TokenColors.Lerp(from, to, g);
                WakeIcon.style.color = to;
                Icon.style.opacity = (float)(1 - g);
                WakeIcon.style.opacity = (float)g;
                return;
            }

            var c = Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(elapsedMs, 0, BleMotionSpecs.WakeColorEndMs));
            var color = TokenColors.Lerp(from, to, c);
            var glyph = BleMotionSpecs.Segment(elapsedMs, BleMotionSpecs.WakeGlyphStartMs, BleMotionSpecs.WakeGlyphEndMs);
            Icon.style.color = color;
            WakeIcon.style.color = color;
            Icon.style.opacity = (float)(1 - glyph);
            WakeIcon.style.opacity = (float)glyph;
            var d = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(elapsedMs, 0, BleMotionSpecs.WakeDiscEndMs));
            _discScale = 1 + (BleMotionSpecs.WakeDiscScale - 1) * d;
            _wakeRing = BleMotionSpecs.Segment(elapsedMs, 0, BleMotionSpecs.WakeRingMs);
            MarkDirtyRepaint();
        }

        // Al terminar (o al saltar al final): el estado de reposo.
        public void ResetWake()
        {
            Icon.style.color = StyleKeyword.Null;
            Icon.style.opacity = StyleKeyword.Null;
            WakeIcon.style.color = StyleKeyword.Null;
            WakeIcon.style.opacity = 0;
            _discScale = 1;
            _wakeRing = -1;
            _breathStart = _motion?.Now ?? 0;
            MarkDirtyRepaint();
        }

        private void UpdateRegistration()
        {
            if (_motion == null)
            {
                return;
            }

            if (panel != null && _breathes)
            {
                _motion.Add(this);
            }
            else
            {
                _motion.Remove(this);
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0)
            {
                return;
            }

            var center = rect.center;
            var unit = rect.width / (float)(_compact ? BleMotionSpecs.CompactIllustrationBoxDp : BleMotionSpecs.IllustrationBoxDp);
            var inner = (float)(_compact ? BleMotionSpecs.CompactIllustrationInnerRingDp : BleMotionSpecs.IllustrationInnerRingDp);
            var outer = (float)(_compact ? BleMotionSpecs.CompactIllustrationOuterRingDp : BleMotionSpecs.IllustrationOuterRingDp);
            var disc = (float)(_compact ? BleMotionSpecs.CompactIllustrationDiscDp : BleMotionSpecs.IllustrationDiscDp) / 2;
            var stroke = (float)BleMotionSpecs.IllustrationRingStrokeDp;
            var ring = TokenColors.ToColor(Tokens.TextPrimary);
            MeshShapes.Ring(context, center, (outer - stroke / 2) * unit, stroke * unit, WithAlpha(ring, BleMotionSpecs.IllustrationOuterRingAlpha), Segments);
            MeshShapes.Ring(context, center, (inner - stroke / 2) * unit, stroke * unit, WithAlpha(ring, BleMotionSpecs.IllustrationInnerRingAlpha), Segments);
            MeshShapes.Disc(context, center, (float)(disc * _discScale) * unit, TokenColors.ToColor(Tokens.SurfaceCard), Segments);
            if (_wakeRing >= 0 && _wakeRing < 1)
            {
                var k = 1 - _wakeRing;
                var r = BleMotionSpecs.WakeRingFromRadiusDp + (BleMotionSpecs.WakeRingToRadiusDp - BleMotionSpecs.WakeRingFromRadiusDp) * (1 - k * k * k);
                if (_compact)
                {
                    r *= BleMotionSpecs.CompactIllustrationBoxDp / BleMotionSpecs.IllustrationBoxDp;
                }

                MeshShapes.Ring(context, center, (float)r * unit, (float)BleMotionSpecs.SonarRingStrokeDp * unit,
                    WithAlpha(TokenColors.ToColor(Tokens.AccentPrimary), BleMotionSpecs.SonarRingAlpha * k), Segments);
            }
        }

        private static Color WithAlpha(Color color, double alpha)
        {
            color.a = (float)(color.a * alpha);
            return color;
        }
    }

    // Anillos y discos con Painter2D (borde con antialias; sin asignaciones en el camino quieto: lo mide BleAllocationTests).
    internal static class MeshShapes
    {
        private static readonly Angle Start = new Angle(0, AngleUnit.Degree);
        private static readonly Angle End = new Angle(360, AngleUnit.Degree);

        // Un anillo de radio medio radius y trazo width.
        public static void Ring(MeshGenerationContext context, Vector2 center, float radius, float width, Color color, int segments)
        {
            if (radius <= 0 || width <= 0 || color.a <= 0.002f)
            {
                return;
            }

            var painter = context.painter2D;
            painter.lineWidth = width;
            painter.strokeColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Start, End, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Stroke();
        }

        public static void Disc(MeshGenerationContext context, Vector2 center, float radius, Color color, int segments)
        {
            if (radius <= 0 || color.a <= 0.002f)
            {
                return;
            }

            var painter = context.painter2D;
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, Start, End, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
