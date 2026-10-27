using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Ícono de Material Symbols (ASSET_SLOTS §4): un Label con .rm-icon (y .rm-icon--filled) cuyo texto es el codepoint de
    // Core.Design.Icons. La fuente la pone Typography.uss; el tamaño (caja cuadrada y letra) lo pone el código, y el color,
    // el USS del componente. flex-shrink 0 (DN-20).
    // Si el FontAsset de íconos falta o no tiene el codepoint, dibuja el cuadrado redondeado genérico de ASSET_SLOTS §4.4 en
    // el color resuelto y avisa [SLOT] ui.icons una vez por sesión (D-07).
    public sealed class IconView : Label
    {
        public const string UssClass = "rm-icon";
        public const string FilledClass = "rm-icon--filled";

        private static bool _warned;

        private string _codepoint = "";
        private bool _fallback;
        private bool _checked;

        public IconView(string codepoint, bool filled, float sizeDp)
        {
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            SetSize(sizeDp);
            SetGlyph(codepoint, filled);
            generateVisualContent += DrawFallback;
            RegisterCallback<GeometryChangedEvent>(_ => CheckGlyph());
        }

        public string Codepoint => _codepoint;

        public bool IsFilled => ClassListContains(FilledClass);

        public bool UsesFallback => _fallback;

        public void SetSize(float sizeDp)
        {
            style.width = sizeDp;
            style.height = sizeDp;
            style.minWidth = sizeDp;
            style.minHeight = sizeDp;
            style.fontSize = sizeDp;
        }

        public void SetGlyph(string codepoint, bool filled)
        {
            EnableInClassList(FilledClass, filled);
            if (_codepoint == codepoint)
            {
                return;
            }

            _codepoint = codepoint ?? "";
            text = _fallback ? "" : _codepoint;
            _checked = false;
            CheckGlyph();
        }

        private void CheckGlyph()
        {
            if (_checked || panel == null)
            {
                return;
            }

            var font = resolvedStyle.unityFontDefinition.fontAsset;
            if (font == null && resolvedStyle.unityFontDefinition.font == null)
            {
                return; // estilos todavía sin resolver
            }

            _checked = true;
            var missing = font == null || _codepoint.Length == 0 || !font.HasCharacter(_codepoint[0], false, true);
            if (missing == _fallback)
            {
                return;
            }

            _fallback = missing;
            text = _fallback ? "" : _codepoint;
            MarkDirtyRepaint();
            if (_fallback && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[SLOT] ui.icons: falta el FontAsset de íconos o el glifo U+" + ((int)(_codepoint.Length > 0 ? _codepoint[0] : '?')).ToString("X4")
                    + "; se dibuja el ícono genérico (ASSET_SLOTS §4.4).");
            }
        }

        private void DrawFallback(MeshGenerationContext context)
        {
            if (!_fallback)
            {
                return;
            }

            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height) * 0.75f;
            if (size <= 0)
            {
                return;
            }

            var x = rect.x + (rect.width - size) / 2;
            var y = rect.y + (rect.height - size) / 2;
            var r = size * 0.2f;
            var painter = context.painter2D;
            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x + r, y));
            painter.ArcTo(new Vector2(x + size, y), new Vector2(x + size, y + size), r);
            painter.ArcTo(new Vector2(x + size, y + size), new Vector2(x, y + size), r);
            painter.ArcTo(new Vector2(x, y + size), new Vector2(x, y), r);
            painter.ArcTo(new Vector2(x, y), new Vector2(x + size, y), r);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
