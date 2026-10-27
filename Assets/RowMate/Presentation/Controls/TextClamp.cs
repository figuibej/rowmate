using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Corta un Label a N líneas con "…" (AC-HRM-13-07, D-21: UI Toolkit 6.3 no tiene un límite de líneas). Si el texto
    // completo mide más de N líneas para el ancho del Label, busca por bisección el prefijo más largo que entra con "…".
    // Se recalcula al cambiar el ancho (GeometryChangedEvent) y al cambiar el texto. FullText es el texto sin cortar.
    public sealed class TextClamp
    {
        public const string Ellipsis = "…";

        private readonly Label _label;
        private readonly int _maxLines;
        private string _full = "";
        private float _lastWidth = -1;

        public TextClamp(Label label, int maxLines)
        {
            _label = label;
            _maxLines = maxLines < 1 ? 1 : maxLines;
            _label.style.whiteSpace = WhiteSpace.Normal;
            _label.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        public string FullText => _full;

        public bool IsClamped => _label.text != _full;

        public void SetText(string text)
        {
            _full = text ?? "";
            _lastWidth = -1;
            _label.text = _full;
            Apply();
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            if (Mathf.Abs(evt.newRect.width - _lastWidth) > 0.5f)
            {
                Apply();
            }
        }

        private void Apply()
        {
            var width = _label.contentRect.width;
            if (!(width > 0) || _label.panel == null)
            {
                return;
            }

            _lastWidth = _label.layout.width;
            var lineHeight = Measure("Ag", width).y;
            if (!(lineHeight > 0))
            {
                return;
            }

            var limit = lineHeight * _maxLines + lineHeight * 0.5f;
            if (Measure(_full, width).y <= limit)
            {
                if (_label.text != _full)
                {
                    _label.text = _full;
                }

                return;
            }

            var low = 0;
            var high = _full.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (Measure(_full.Substring(0, mid).TrimEnd() + Ellipsis, width).y <= limit)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            var clamped = _full.Substring(0, low).TrimEnd() + Ellipsis;
            if (_label.text != clamped)
            {
                _label.text = clamped;
            }
        }

        private Vector2 Measure(string text, float width)
        {
            return _label.MeasureTextSize(text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
        }
    }
}
