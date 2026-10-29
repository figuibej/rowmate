using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Presentation.Controls
{
    // Número de cifras tabulares (ARCHITECTURE §7.8, 02-tech-design §3.7, DN-08; AC-APP-05-04, AC-BLE-11-11). Un elemento
    // por número que dibuja cada carácter en su celda con MeshGenerationContext.DrawText:
    // - Ancho de celda de un dígito = avance máximo de 0-9 en el FontAsset y el tamaño resueltos (USS .rm-type-num-*);
    //   ':' y '.' en celdas de 0,32 em; '-' con su avance real (≈ 0,37 em en Barlow SC: "--" se lee como dos guiones); el
    //   resto (la 'm' de los metros) con su avance. Cada glifo va centrado en su celda; las celdas, alineadas a la derecha.
    // - Caja de alto 1 em con la banda de mayúsculas centrada (capLine y línea de base del FontAsset; DN-08 a).
    // - SetValue(texto, valor): las celdas que cambian ruedan (DigitRoll, 180 ms ease.enter; reducir movimiento: fundido de
    //   80 ms); si llega otro valor, el rodillo en curso salta al final. En u ≥ 1 se ve exactamente el texto (AC-BLE-11-11).
    //   Si cambia el largo, el ancho interpola 180 ms con las celdas pegadas a la derecha (DN-08 c).
    // - Text: el valor que muestra (para UiQuery, QA-G2-14); GetCellRect(i) para los tests de AC-APP-05-04.
    // - Sin FontAsset (slot vacío) cae a un Label interno y avisa [SLOT] ui.font.numeric una vez.
    // Cadenas de un carácter precalculadas: actualizar no asigna memoria.
    public sealed class TabularNumber : VisualElement, IMotion
    {
        public const string UssClass = "rm-tabular-number";
        private const int MaxCells = 16;
        private const long FallbackCheckDelayMs = 250;

        private static readonly string[] CharStrings = BuildCharStrings();
        private static bool _warnedSlot;

        private readonly MotionDriver? _motion;
        private readonly float[] _cellWidths = new float[MaxCells];
        private readonly float[] _previousCellWidths = new float[MaxCells];

        private string _text = "";
        private double _value;
        private string _previousText = "";
        private DigitRollPlan _plan;
        private bool _rolling;
        private double _rollStart;
        private bool _reducedRoll;
        private float _fromWidth;
        private float _toWidth;

        private FontAsset? _font;
        private float _fontSize;
        private float _scale;
        private float _digitAdvance;
        private float _ascent;
        private float _capHeight;
        private Label? _fallback;

        public TabularNumber(MotionDriver? motion)
        {
            _motion = motion;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(_ => UpdateMetrics(true));
            RegisterCallback<GeometryChangedEvent>(_ => UpdateMetrics(true));
            // Al adjuntarse los estilos todavía no están resueltos (sin FontAsset todavía no quiere decir slot vacío; en el
            // build salía el aviso con el FontAsset puesto): la decisión de caer al Label espera al primer layout o, si no lo
            // hay (caja vacía), a un cuarto de segundo después.
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateMetrics(false);
                schedule.Execute(() => UpdateMetrics(true)).StartingIn(FallbackCheckDelayMs);
            });
        }

        public bool IsFinite => true;

        // El texto que se ve (el de la última lectura, desde el cuadro 0 del rodillo).
        public string Text => _text;

        public double Value => _value;

        public bool IsRolling => _rolling;

        public float FontSize => _fontSize;

        // Ancho natural del texto actual (el que queda al terminar el rodillo).
        public float NaturalWidth => _toWidth;

        // Línea de base, desde el borde superior de la caja (para alinear la unidad "bpm", DN-09).
        public float BaselineFromTop => _fontSize <= 0 ? 0 : (_fontSize + _capHeight) / 2f;

        public FontAsset? Font => _font;

        public void SetValue(string text, double value)
        {
            SetValue(text, value, true);
        }

        // It. 4a (AC-WKT-02-08; 02-tech-design §6.5): animate = false aplica el valor sin rodillo (el primer estado después de un
        // alcance: ningún número pasa por valores intermedios); un rodillo en curso salta a su final.
        public void SetValue(string text, double value, bool animate)
        {
            text = text ?? "";
            if (text.Length > MaxCells)
            {
                text = text.Substring(text.Length - MaxCells);
            }

            if (text == _text)
            {
                _value = value;
                return;
            }

            animate = animate && _motion != null && _motion.Armed && _font != null && _text.Length > 0;
            if (_rolling)
            {
                FinishRoll();
            }

            var previous = _text;
            var previousValue = _value;
            Array.Copy(_cellWidths, _previousCellWidths, MaxCells);
            _text = text;
            _value = value;
            var fromWidth = _toWidth;
            ComputeCells();
            if (_fallback != null)
            {
                _fallback.text = _text;
            }

            if (animate)
            {
                _previousText = previous;
                _plan = DigitRoll.Plan(previous, text, previousValue, value);
                _rolling = _plan.HasChanges;
                _reducedRoll = _motion!.Settings.ReduceMotion;
                _rollStart = _motion.Now;
                _fromWidth = fromWidth;
                if (_rolling)
                {
                    _motion.Add(this);
                }
            }
            else
            {
                _fromWidth = _toWidth;
            }

            ApplySize(_rolling ? _fromWidth : _toWidth);
            MarkDirtyRepaint();
        }

        // It. 4a (02-tech-design §6.4): el tamaño en línea (el HUD no sigue la letra del sistema y achica sus cifras con los
        // topes de HudGeometry). Las métricas se recalculan en el acto, sin esperar a que se resuelvan los estilos.
        public void SetFontSize(float sizeDp)
        {
            if (!(sizeDp > 0))
            {
                return;
            }

            style.fontSize = sizeDp;
            _sizeOverride = sizeDp;
            UpdateMetrics(false);
        }

        // Rectángulo de la celda i del texto actual, en coordenadas locales (AC-APP-05-04).
        public Rect GetCellRect(int index)
        {
            if (index < 0 || index >= _text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var width = contentRect.width;
            var x = width;
            for (var i = _text.Length - 1; i >= index; i--)
            {
                x -= _cellWidths[i];
            }

            return new Rect(x, 0, _cellWidths[index], _fontSize);
        }

        public bool Tick(double nowSeconds)
        {
            if (!_rolling)
            {
                return false;
            }

            var elapsed = (nowSeconds - _rollStart) * 1000.0;
            ApplySize(_plan.Length == _plan.PreviousLength ? _toWidth : (float)DigitRoll.WidthAt(elapsed, _fromWidth, _toWidth));
            MarkDirtyRepaint();
            if (DigitRoll.IsFinished(elapsed, _reducedRoll))
            {
                FinishRoll();
                return false;
            }

            return true;
        }

        private void FinishRoll()
        {
            _rolling = false;
            _motion?.Remove(this);
            ApplySize(_toWidth);
            MarkDirtyRepaint();
        }

        private float _sizeOverride;

        private void UpdateMetrics(bool allowFallback)
        {
            var font = resolvedStyle.unityFontDefinition.fontAsset;
            var size = _sizeOverride > 0 ? _sizeOverride : resolvedStyle.fontSize;
            if (font == null)
            {
                if (!allowFallback || resolvedStyle.unityFontDefinition.font != null || panel == null)
                {
                    return;
                }

                EnsureFallback();
                return;
            }

            if (ReferenceEquals(font, _font) && Mathf.Approximately(size, _fontSize))
            {
                return;
            }

            _font = font;
            _fontSize = size;
            var face = font.faceInfo;
            _scale = face.pointSize > 0 ? size / face.pointSize * (face.scale > 0 ? face.scale : 1f) : 0;
            _ascent = face.ascentLine * _scale;
            _capHeight = face.capLine * _scale;
            font.TryAddCharacters("0123456789:.-m");
            _digitAdvance = 0;
            for (var c = '0'; c <= '9'; c++)
            {
                _digitAdvance = Mathf.Max(_digitAdvance, Advance(c));
            }

            if (_fallback != null)
            {
                _fallback.RemoveFromHierarchy();
                _fallback = null;
            }

            ComputeCells();
            if (!_rolling)
            {
                _fromWidth = _toWidth;
            }

            ApplySize(_rolling ? Mathf.Max(_fromWidth, _toWidth) : _toWidth);
            style.height = size;
            MarkDirtyRepaint();
        }

        private void EnsureFallback()
        {
            if (_fallback != null)
            {
                return;
            }

            _fallback = new Label(_text);
            _fallback.pickingMode = PickingMode.Ignore;
            Add(_fallback);
            style.width = StyleKeyword.Null;
            style.minWidth = StyleKeyword.Null;
            _appliedWidth = -1;
            style.height = StyleKeyword.Null;
            if (!_warnedSlot)
            {
                _warnedSlot = true;
                Debug.LogWarning("[SLOT] ui.font.numeric: sin FontAsset numérico; TabularNumber usa un Label (sin cifras tabulares).");
            }
        }

        private void ComputeCells()
        {
            EnsureGlyphs();
            var total = 0f;
            for (var i = 0; i < _text.Length; i++)
            {
                _cellWidths[i] = CellWidth(_text[i]);
                total += _cellWidths[i];
            }

            _toWidth = total;
        }

        // Los caracteres del texto que no son cifras (el × del chip de la serie, la m de los metros, el -) tienen que estar en el
        // atlas antes de medir su celda: si no, Advance cae a 0,5 em y el ancho dependía de si otro texto ya los había dibujado
        // (en la It. 3 el × de CAP-10 medía 0,5 em y el de CAP-11, su avance real: QA-V3-06, 04a/00-deuda-it3 §6). Solo la
        // primera vez de cada carácter en la sesión: después ya está y no se asigna nada.
        private void EnsureGlyphs()
        {
            if (_font == null)
            {
                return;
            }

            for (var i = 0; i < _text.Length; i++)
            {
                var c = _text[i];
                if ((c >= '0' && c <= '9') || c == ':' || c == '.')
                {
                    continue;
                }

                if (!_font.characterLookupTable.ContainsKey(c))
                {
                    _font.TryAddCharacters(CharString(c));
                }
            }
        }

        private float CellWidth(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return _digitAdvance;
            }

            if (c == ':' || c == '.')
            {
                return (float)(MotionSpecs.ColonCellEm * _fontSize);
            }

            return Advance(c);
        }

        private float Advance(char c)
        {
            if (_font == null)
            {
                return 0;
            }

            Character character;
            if (!_font.characterLookupTable.TryGetValue(c, out character) || character.glyph == null)
            {
                return 0.5f * _fontSize;
            }

            return character.glyph.metrics.horizontalAdvance * _scale * character.glyph.scale;
        }

        private void ApplySize(float width)
        {
            if (_fallback != null || _font == null || _fontSize <= 0)
            {
                return;
            }

            // Redondeado hacia arriba a la grilla de píxeles: el layout redondea a px físicos y no puede achicar la caja. Se
            // compara con el último valor aplicado (no con resolvedStyle, que antes del primer layout puede estar viejo: en P800
            // el número quedaba con ancho 0) y min-width impide que algo lo achique.
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            width = Mathf.Ceil(width * ppp - 0.001f) / ppp;
            if (Mathf.Abs(_appliedWidth - width) > 0.001f)
            {
                _appliedWidth = width;
                style.width = width;
                style.minWidth = width;
            }
        }

        private float _appliedWidth = -1;

        private void Draw(MeshGenerationContext context)
        {
            if (_font == null || _text.Length == 0 || _fontSize <= 0)
            {
                return;
            }

            var color = MeshColor.ForMesh(resolvedStyle.color);
            var right = contentRect.width;
            var baselineY = BaselineFromTop;
            var top = baselineY - _ascent;
            double elapsed = 0;
            if (_rolling && _motion != null)
            {
                elapsed = (_motion.Now - _rollStart) * 1000.0;
            }

            DigitRoll.Evaluate(elapsed, _plan.Direction, _reducedRoll, out var outOffset, out var outAlpha, out var inOffset, out var inAlpha);

            var x = right;
            for (var i = _text.Length - 1; i >= 0; i--)
            {
                var cell = _cellWidths[i];
                x -= cell;
                var incoming = _text[i];
                if (_rolling && _plan.Length == _text.Length && _plan.IsChanged(i))
                {
                    var outgoing = _plan.OutgoingAt(i);
                    if (outgoing != '\0' && outAlpha > 0.001)
                    {
                        DrawChar(context, outgoing, x, CellWidthOf(outgoing), top + (float)(outOffset * _fontSize), color, (float)outAlpha);
                    }

                    if (inAlpha > 0.001)
                    {
                        DrawChar(context, incoming, x, cell, top + (float)(inOffset * _fontSize), color, (float)inAlpha);
                    }
                }
                else
                {
                    DrawChar(context, incoming, x, cell, top, color, 1f);
                }
            }

            // Celdas que se van (el texto se acortó): solo salen, a la izquierda.
            if (_rolling && _plan.RemovedCells > 0 && outAlpha > 0.001)
            {
                for (var k = _plan.RemovedCells - 1; k >= 0; k--)
                {
                    var c = _previousText[k];
                    var w = CellWidthOf(c);
                    x -= w;
                    DrawChar(context, c, x, w, top + (float)(outOffset * _fontSize), color, (float)outAlpha);
                }
            }
        }

        private float CellWidthOf(char c)
        {
            return CellWidth(c);
        }

        private void DrawChar(MeshGenerationContext context, char c, float cellX, float cellWidth, float top, Color color, float alpha)
        {
            var advance = Advance(c);
            var glyphX = cellX + (cellWidth - advance) / 2f;
            color.a *= alpha;
            context.DrawText(CharString(c), new Vector2(glyphX, top), _fontSize * scaledPixelsPerPoint, color, _font);
        }

        private static string CharString(char c)
        {
            return c < CharStrings.Length ? CharStrings[c] : c.ToString();
        }

        // ASCII y Latin-1 (el × del chip de la serie también, sin asignar en cada dibujo).
        private static string[] BuildCharStrings()
        {
            var strings = new string[256];
            for (var i = 0; i < strings.Length; i++)
            {
                strings[i] = ((char)i).ToString();
            }

            return strings;
        }
    }
}
