using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Indicador de espera (04-design-notes §4.7): arco de 270° con Painter2D, 16 dp, trazo 2 con extremos redondeados, una
    // vuelta cada 900 ms lineal por MotionDriver (continua: no cuenta para Settle). El color es el resuelto del elemento
    // (USS: accent.primary en la tarjeta, text.primary en el botón primario). Sigue girando con reducir movimiento (indica
    // espera). Gira solo mientras está en el panel y visible.
    public sealed class Spinner : VisualElement, IMotion
    {
        public const string UssClass = "rm-spinner";
        public const float SizeDp = 16;
        public const float StrokeDp = 2;

        private readonly MotionDriver? _motion;
        private bool _shown = true;

        public Spinner(MotionDriver? motion)
        {
            _motion = motion;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.width = SizeDp;
            style.height = SizeDp;
            style.flexShrink = 0;
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => UpdateRegistration());
            RegisterCallback<DetachFromPanelEvent>(_ => _motion?.Remove(this));
        }

        public bool IsFinite => false;

        public bool IsShown => _shown;

        // Mostrar u ocultar (display) y dejar de girar oculto: un spinner oculto no pide 60 fps.
        public void SetShown(bool shown)
        {
            _shown = shown;
            style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateRegistration();
        }

        private void UpdateRegistration()
        {
            if (_motion == null)
            {
                return;
            }

            if (_shown && panel != null)
            {
                _motion.Add(this);
            }
            else
            {
                _motion.Remove(this);
            }
        }

        public bool Tick(double nowSeconds)
        {
            if (panel == null || !_shown)
            {
                return false;
            }

            var turns = nowSeconds * 1000.0 / MotionSpecs.SpinnerTurnMs;
            var degrees = (float)((turns - System.Math.Floor(turns)) * 360.0);
            style.rotate = new Rotate(new Angle(degrees, AngleUnit.Degree));
            return true;
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var radius = Mathf.Min(rect.width, rect.height) / 2 - StrokeDp / 2;
            if (radius <= 0)
            {
                return;
            }

            var painter = context.painter2D;
            painter.strokeColor = resolvedStyle.color;
            painter.lineWidth = StrokeDp;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(rect.center, radius, new Angle(-90, AngleUnit.Degree),
                new Angle(-90 + (float)MotionSpecs.SpinnerArcDegrees, AngleUnit.Degree), ArcDirection.Clockwise);
            painter.Stroke();
        }
    }
}
