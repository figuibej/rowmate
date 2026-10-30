using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Presentation.Controls
{
    // Dónde va un afijo de TabularNumber (It. 4b, §3.11): adentro de la caja (cuenta en su ancho: la unidad, el ícono del
    // reloj de pared) o en el hueco de la izquierda, fuera de la caja (la flecha del semáforo, M1: no cambia el ancho).
    public enum AffixPlacement
    {
        Inside,
        Gutter,
    }

    // Cómo se alinea un afijo en vertical: por su línea de base (la de las cifras menos un ascenso) o con la tinta centrada en
    // la banda de las mayúsculas de las cifras (el ícono schedule del reloj de pared, 04-design-notes §3.4).
    public enum AffixAlign
    {
        Baseline,
        CapCenter,
    }

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
    // It. 4b (04b-hud-exr/02-tech-design §3.11, M1, M2, M7; §5.2):
    // - AFIJOS: un prefijo y un sufijo que dibuja el mismo elemento con OTRO FontAsset (el de una clase de tipo, que resuelve
    //   un hijo oculto sin caja: no cuenta en el presupuesto), con su tamaño, color, alfa, línea de base, aire y desplazamiento.
    //   Adentro de la caja suman su ancho (la W de Watts, la m de la banda y del diálogo, el ícono del reloj de pared); en el
    //   hueco (la flecha del semáforo) se dibujan a la izquierda de la caja, sin cambiar su ancho. Así la flecha, la unidad y el
    //   ícono no son elementos (el presupuesto de 60, ARCHITECTURE §7.8).
    // - PLACA: lo que el mismo elemento dibuja debajo de las cifras (el disco y el aro de la cuenta grande, M7).
    // - RollProgress: 0 → 1 del rodillo en curso (1 sin rodillo), para la regla de toma (un rodillo antes de su mitad bloquea).
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

        // El texto que se ve, en celdas propias (It. 4b, QA-V4b-MOT-01: el conteo del diálogo de completado lo escribe sin cadena
        // nueva por cuadro). _textString es la cadena de la última SetValue(string) o, después de SetValue(char[]), la que arma
        // Text la primera vez que se lee (las pruebas y el arnés; nunca en un camino caliente).
        private readonly char[] _chars = new char[MaxCells];
        private int _length;
        private string? _textString = "";
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

        private Affix? _prefix;
        private Affix? _suffix;
        private Action<MeshGenerationContext, Rect>? _plate;

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
                // De vuelta en un panel (QA-V4a-05): la caja que no se pudo medir afuera se mide con el DPI de este panel.
                if (_appliedWidth < 0 && _font != null)
                {
                    ApplySize(_toWidth);
                }

                schedule.Execute(() => UpdateMetrics(true)).StartingIn(FallbackCheckDelayMs);
            });
            // QA-V4a-05: el rodillo de un número que sale del panel (el HUD que se va con las cifras rodando) termina en el acto,
            // sin medir: fuera de un panel no hay DPI, y scaledPixelsPerPoint anotaba "Trying to access the DPI setting of a visual
            // element that is not on a panel." en cada cuadro del rodillo (88 avisos en el player de la ronda 2 de la 4a).
            RegisterCallback<DetachFromPanelEvent>(_ => StopOffPanel());
        }

        public bool IsFinite => true;

        // El texto que se ve (el de la última lectura, desde el cuadro 0 del rodillo). Sin los afijos.
        public string Text => _textString ??= new string(_chars, 0, _length);

        public double Value => _value;

        public bool IsRolling => _rolling;

        // It. 4b (§5.2): cuánto lleva el rodillo en curso, de 0 a 1 (1 sin rodillo). Un rodillo antes de su mitad bloquea una toma.
        public double RollProgress
        {
            get
            {
                if (!_rolling || _motion == null)
                {
                    return 1;
                }

                var elapsed = (_motion.Now - _rollStart) * 1000.0;
                var total = _reducedRoll ? MotionSpecs.ReducedFadeMs : DigitRoll.DurationMs;
                var u = elapsed / total;
                return u < 0 ? 0 : u > 1 ? 1 : u;
            }
        }

        public float FontSize => _fontSize;

        // Ancho natural de las CIFRAS del texto actual (el que queda al terminar el rodillo), sin los afijos.
        public float NaturalWidth => _toWidth;

        // It. 4b: el ancho natural de la caja (las cifras y los afijos de adentro, con sus aires).
        public float NaturalBoxWidth => _toWidth + InsideSpan(_prefix) + InsideSpan(_suffix);

        // Línea de base, desde el borde superior de la caja (para alinear la unidad "bpm", DN-09).
        public float BaselineFromTop => _fontSize <= 0 ? 0 : (_fontSize + _capHeight) / 2f;

        // La banda de las mayúsculas de las cifras (desde el borde de arriba de la caja): de la línea de las mayúsculas a la base.
        public float CapTopFromTop => _fontSize <= 0 ? 0 : (_fontSize - _capHeight) / 2f;

        public FontAsset? Font => _font;

        // ── Afijos (It. 4b) ──

        public string PrefixText => _prefix?.Text ?? "";

        public string SuffixText => _suffix?.Text ?? "";

        public float PrefixAlpha => _prefix?.Alpha ?? 0;

        public float PrefixShiftY => _prefix?.ShiftY ?? 0;

        public Color PrefixColor => _prefix?.Color ?? Color.clear;

        public Color SuffixColor => _suffix?.Color ?? Color.clear;

        public FontAsset? PrefixFont => _prefix != null ? FontOf(_prefix) : null;

        public FontAsset? SuffixFont => _suffix != null ? FontOf(_suffix) : null;

        public float PrefixSizeDp => _prefix?.SizeDp ?? 0;

        public float SuffixSizeDp => _suffix?.SizeDp ?? 0;

        // La línea de base del prefijo y del sufijo, desde el borde de arriba de la caja (las pruebas de M1 y M2).
        public float PrefixBaselineFromTop => _prefix != null ? AffixBaseline(_prefix) : 0;

        public float SuffixBaselineFromTop => _suffix != null ? AffixBaseline(_suffix) : 0;

        // El rectángulo del prefijo y del sufijo en coordenadas locales (su avance × su línea de texto), o Rect.zero sin él.
        public Rect PrefixLocalRect => _prefix != null && _prefix.Text.Length > 0 ? AffixRect(_prefix, true) : Rect.zero;

        public Rect SuffixLocalRect => _suffix != null && _suffix.Text.Length > 0 ? AffixRect(_suffix, false) : Rect.zero;

        // La unidad (la W de Watts, la m de la banda y del diálogo): adentro de la caja, a gapDp de las cifras, en su línea de
        // base. fontClass: la clase de tipo que da el FontAsset (rm-type-caption = Inter 500).
        public void SetSuffix(string text, string fontClass, float sizeDp, Color color, float gapDp)
        {
            _suffix = Configure(_suffix, text, fontClass, sizeDp, color, gapDp, AffixPlacement.Inside, AffixAlign.Baseline, 0, 0);
            Relayout();
        }

        public void ClearSuffix()
        {
            if (_suffix == null)
            {
                return;
            }

            _suffix.Holder.RemoveFromHierarchy();
            _suffix = null;
            Relayout();
        }

        // Un prefijo adentro de la caja (el ícono schedule del reloj de pared): a gapDp de las cifras.
        public void SetPrefix(string text, string fontClass, float sizeDp, Color color, float gapDp, AffixAlign align)
        {
            _prefix = Configure(_prefix, text, fontClass, sizeDp, color, gapDp, AffixPlacement.Inside, align, 0, 0);
            Relayout();
        }

        // Un prefijo en el hueco de la izquierda (la flecha, M1): su borde izquierdo a gutterDp a la izquierda de la caja y su
        // línea de base liftDp por encima de la de las cifras. No cambia el ancho de la caja.
        public void SetGutterPrefix(string text, string fontClass, float sizeDp, Color color, float gutterDp, float liftDp)
        {
            var alpha = _prefix?.Alpha ?? 1f;
            var shift = _prefix?.ShiftY ?? 0f;
            _prefix = Configure(_prefix, text, fontClass, sizeDp, color, 0, AffixPlacement.Gutter, AffixAlign.Baseline, gutterDp, liftDp);
            _prefix.Alpha = alpha;
            _prefix.ShiftY = shift;
            Relayout();
        }

        // Cómo se ve el prefijo (la entrada y la salida de la flecha, OffTargetMotion): alfa y desplazamiento en y (dp).
        public void SetPrefixVisual(float alpha, float shiftY)
        {
            if (_prefix == null)
            {
                return;
            }

            if (Mathf.Abs(_prefix.Alpha - alpha) < 1e-5f && Mathf.Abs(_prefix.ShiftY - shiftY) < 1e-5f)
            {
                return;
            }

            _prefix.Alpha = alpha;
            _prefix.ShiftY = shiftY;
            MarkDirtyRepaint();
        }

        public void SetPrefixColor(Color color)
        {
            if (_prefix == null || _prefix.Color == color)
            {
                return;
            }

            _prefix.Color = color;
            MarkDirtyRepaint();
        }

        public void SetSuffixColor(Color color)
        {
            if (_suffix == null || _suffix.Color == color)
            {
                return;
            }

            _suffix.Color = color;
            MarkDirtyRepaint();
        }

        // La placa (M7): se dibuja antes que las cifras, en el mismo elemento (el disco y el aro de la cuenta grande).
        public void SetPlate(Action<MeshGenerationContext, Rect>? plate)
        {
            _plate = plate;
            MarkDirtyRepaint();
        }

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

            if (Matches(text))
            {
                _value = value;
                _textString ??= text;
                return;
            }

            animate = animate && _motion != null && _motion.Armed && _font != null && _length > 0 && panel != null;
            var previous = animate ? Text : "";
            for (var i = 0; i < text.Length; i++)
            {
                _chars[i] = text[i];
            }

            Commit(text.Length, text, previous, value, animate);
        }

        // It. 4b (QA-V4b-MOT-01): las cifras de chars[0..length) sin cadena nueva y sin rodillo (el conteo del diálogo de completado,
        // que cambia de valor en cada cuadro: ANIMATION_BIBLE §2.5). Si es lo mismo que se ve, solo cambia el valor.
        public void SetValue(char[] chars, int length, double value)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }

            var start = length > MaxCells ? length - MaxCells : 0;
            var count = length - start;
            if (Matches(chars, start, count))
            {
                _value = value;
                return;
            }

            for (var i = 0; i < count; i++)
            {
                _chars[i] = chars[start + i];
            }

            Commit(count, null, "", value, false);
        }

        private bool Matches(string text)
        {
            if (text.Length != _length)
            {
                return false;
            }

            for (var i = 0; i < _length; i++)
            {
                if (_chars[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool Matches(char[] chars, int start, int count)
        {
            if (count != _length)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (_chars[i] != chars[start + i])
                {
                    return false;
                }
            }

            return true;
        }

        // Las celdas nuevas ya están en _chars[0..length): el resto de SetValue (text: la cadena, si se tiene; previous: la de antes,
        // solo con rodillo).
        private void Commit(int length, string? text, string previous, double value, bool animate)
        {
            if (_rolling)
            {
                FinishRoll();
            }

            var previousValue = _value;
            Array.Copy(_cellWidths, _previousCellWidths, MaxCells);
            _length = length;
            _textString = text;
            _value = value;
            var fromWidth = _toWidth;
            ComputeCells();
            if (_fallback != null)
            {
                _fallback.text = Text;
            }

            if (animate)
            {
                text = Text;
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

        // It. 4b (§2.15): el ancho de las cifras de otro texto con las métricas actuales (la cuenta "M:SS" de los últimos 15 s
        // para el plan de la fila de información). 0 sin FontAsset todavía. Solo cifras, ':' y '.' (sin asignar).
        public float MeasureDigits(string text)
        {
            if (_font == null || text == null)
            {
                return 0;
            }

            var total = 0f;
            for (var i = 0; i < text.Length; i++)
            {
                total += CellWidth(text[i]);
            }

            return total;
        }

        // Rectángulo de la celda i del texto actual, en coordenadas locales (AC-APP-05-04).
        public Rect GetCellRect(int index)
        {
            if (index < 0 || index >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var width = contentRect.width - InsideSpan(_suffix);
            var x = width;
            for (var i = _length - 1; i >= index; i--)
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

            if (panel == null)
            {
                // Fuera del panel (QA-V4a-05): termina sin medir; el MotionDriver lo saca al devolver false.
                _rolling = false;
                _appliedWidth = -1;
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

        // Se va del panel: el rodillo en curso termina (fuera del MotionDriver) y la caja se vuelve a medir al volver.
        private void StopOffPanel()
        {
            if (_rolling)
            {
                _rolling = false;
                _motion?.Remove(this);
            }

            _appliedWidth = -1;
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

            var affixesChanged = RefreshAffix(_prefix) | RefreshAffix(_suffix);
            if (ReferenceEquals(font, _font) && Mathf.Approximately(size, _fontSize))
            {
                if (affixesChanged)
                {
                    ApplySize(_rolling ? Mathf.Max(_fromWidth, _toWidth) : _toWidth);
                    MarkDirtyRepaint();
                }

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

            _fallback = new Label(Text);
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
            for (var i = 0; i < _length; i++)
            {
                _cellWidths[i] = CellWidth(_chars[i]);
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

            for (var i = 0; i < _length; i++)
            {
                var c = _chars[i];
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

        // El ancho de la caja: las cifras (o el ancho del rodillo) y los afijos de adentro.
        private void ApplySize(float digitsWidth)
        {
            if (_fallback != null || _font == null || _fontSize <= 0)
            {
                return;
            }

            if (panel == null)
            {
                // Sin panel no hay DPI (QA-V4a-05): se mide al volver a uno (AttachToPanelEvent).
                _appliedWidth = -1;
                return;
            }

            var width = digitsWidth + InsideSpan(_prefix) + InsideSpan(_suffix);
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

        private void Relayout()
        {
            RefreshAffix(_prefix);
            RefreshAffix(_suffix);
            ApplySize(_rolling ? Mathf.Max(_fromWidth, _toWidth) : _toWidth);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var box = contentRect;
            _plate?.Invoke(context, box);
            if (_font == null || _length == 0 || _fontSize <= 0)
            {
                return;
            }

            var color = MeshColor.ForMesh(resolvedStyle.color);
            var right = box.width - InsideSpan(_suffix);
            var baselineY = BaselineFromTop;
            var top = baselineY - _ascent;
            double elapsed = 0;
            if (_rolling && _motion != null)
            {
                elapsed = (_motion.Now - _rollStart) * 1000.0;
            }

            DigitRoll.Evaluate(elapsed, _plan.Direction, _reducedRoll, out var outOffset, out var outAlpha, out var inOffset, out var inAlpha);

            var x = right;
            for (var i = _length - 1; i >= 0; i--)
            {
                var cell = _cellWidths[i];
                x -= cell;
                var incoming = _chars[i];
                if (_rolling && _plan.Length == _length && _plan.IsChanged(i))
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

            DrawAffix(context, _suffix, false);
            DrawAffix(context, _prefix, true);
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

        // ── Afijos ──

        private Affix Configure(Affix? affix, string text, string fontClass, float sizeDp, Color color, float gapDp, AffixPlacement placement,
                                AffixAlign align, float gutterDp, float liftDp)
        {
            if (affix == null || affix.FontClass != fontClass)
            {
                affix?.Holder.RemoveFromHierarchy();
                affix = new Affix(fontClass);
                // El FontAsset del afijo lo resuelve un hijo sin caja y oculto (no se dibuja ni cuenta en el presupuesto).
                Add(affix.Holder);
            }

            affix.Text = text ?? "";
            affix.SizeDp = sizeDp;
            affix.Color = color;
            affix.GapDp = gapDp;
            affix.Placement = placement;
            affix.Align = align;
            affix.GutterDp = gutterDp;
            affix.LiftDp = liftDp;
            affix.MeasuredFont = null;
            return affix;
        }

        // Mide el afijo si cambió su FontAsset, su texto o su tamaño. true = cambió su ancho.
        private bool RefreshAffix(Affix? affix)
        {
            if (affix == null)
            {
                return false;
            }

            var font = FontOf(affix);
            if (font == null)
            {
                var had = affix.Advance > 0;
                affix.Advance = 0;
                affix.MeasuredFont = null;
                return had;
            }

            if (ReferenceEquals(font, affix.MeasuredFont) && affix.MeasuredText == affix.Text && Mathf.Approximately(affix.MeasuredSize, affix.SizeDp))
            {
                return false;
            }

            var before = affix.Advance;
            affix.MeasuredFont = font;
            affix.MeasuredText = affix.Text;
            affix.MeasuredSize = affix.SizeDp;
            var face = font.faceInfo;
            var scale = face.pointSize > 0 ? affix.SizeDp / face.pointSize * (face.scale > 0 ? face.scale : 1f) : 0;
            affix.Scale = scale;
            affix.Ascent = face.ascentLine * scale;
            var advance = 0f;
            var inkTop = 0f;
            var inkBottom = 0f;
            var first = true;
            for (var i = 0; i < affix.Text.Length; i++)
            {
                var c = affix.Text[i];
                if (!font.characterLookupTable.ContainsKey(c))
                {
                    font.TryAddCharacters(CharString(c));
                }

                Character character;
                if (!font.characterLookupTable.TryGetValue(c, out character) || character.glyph == null)
                {
                    advance += 0.5f * affix.SizeDp;
                    continue;
                }

                var m = character.glyph.metrics;
                var k = scale * character.glyph.scale;
                advance += m.horizontalAdvance * k;
                var top = m.horizontalBearingY * k;
                var bottom = (m.horizontalBearingY - m.height) * k;
                if (first)
                {
                    inkTop = top;
                    inkBottom = bottom;
                    first = false;
                }
                else
                {
                    inkTop = Mathf.Max(inkTop, top);
                    inkBottom = Mathf.Min(inkBottom, bottom);
                }
            }

            affix.Advance = advance;
            affix.InkCenterAboveBaseline = (inkTop + inkBottom) / 2f;
            return Math.Abs(before - advance) > 1e-4f;
        }

        private static FontAsset? FontOf(Affix affix)
        {
            return affix.Holder.resolvedStyle.unityFontDefinition.fontAsset;
        }

        private static float InsideSpan(Affix? affix)
        {
            if (affix == null || affix.Placement != AffixPlacement.Inside || affix.Text.Length == 0 || !(affix.Advance > 0))
            {
                return 0;
            }

            return affix.Advance + affix.GapDp;
        }

        // La línea de base del afijo desde el borde de arriba de la caja (sin el desplazamiento de su movimiento).
        private float AffixBaseline(Affix affix)
        {
            if (affix.Align == AffixAlign.CapCenter)
            {
                // La tinta del afijo centrada en la banda de las mayúsculas de las cifras.
                var capCenter = BaselineFromTop - _capHeight / 2f;
                return capCenter + affix.InkCenterAboveBaseline;
            }

            return BaselineFromTop - affix.LiftDp;
        }

        // El borde izquierdo del afijo en coordenadas locales.
        private float AffixLeft(Affix affix, bool prefix)
        {
            var width = contentRect.width;
            if (!prefix)
            {
                return width - affix.Advance;
            }

            if (affix.Placement == AffixPlacement.Gutter)
            {
                return -affix.GutterDp;
            }

            var digitsRight = width - InsideSpan(_suffix);
            return digitsRight - _toWidth - affix.GapDp - affix.Advance;
        }

        private Rect AffixRect(Affix affix, bool prefix)
        {
            var baseline = AffixBaseline(affix) + affix.ShiftY;
            var top = baseline - affix.Ascent;
            return new Rect(AffixLeft(affix, prefix), top, affix.Advance, affix.SizeDp);
        }

        private void DrawAffix(MeshGenerationContext context, Affix? affix, bool prefix)
        {
            if (affix == null || affix.Text.Length == 0 || affix.Alpha <= 0.001f || _fontSize <= 0)
            {
                return;
            }

            var font = FontOf(affix);
            if (font == null)
            {
                return;
            }

            if (!ReferenceEquals(font, affix.MeasuredFont))
            {
                RefreshAffix(affix);
            }

            var color = MeshColor.ForMesh(affix.Color);
            color.a *= affix.Alpha;
            var top = AffixBaseline(affix) + affix.ShiftY - affix.Ascent;
            context.DrawText(affix.Text, new Vector2(AffixLeft(affix, prefix), top), affix.SizeDp * scaledPixelsPerPoint, color, font);
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

        // Un afijo: el texto, su FontAsset (el de la clase de tipo del hijo oculto), su tamaño en dp, color, aire, lugar y
        // alineación; lo medido (avance, ascendente y el centro de la tinta) y cómo se ve (alfa y desplazamiento).
        private sealed class Affix
        {
            public Affix(string fontClass)
            {
                FontClass = fontClass;
                Holder = new VisualElement();
                Holder.AddToClassList(fontClass);
                Holder.pickingMode = PickingMode.Ignore;
                Holder.style.position = Position.Absolute;
                Holder.style.width = 0;
                Holder.style.height = 0;
                Holder.style.visibility = Visibility.Hidden;
            }

            public string FontClass { get; }

            public VisualElement Holder { get; }

            public string Text = "";
            public float SizeDp;
            public Color Color;
            public float GapDp;
            public AffixPlacement Placement;
            public AffixAlign Align;
            public float GutterDp;
            public float LiftDp;
            public float Alpha = 1;
            public float ShiftY;

            public FontAsset? MeasuredFont;
            public string MeasuredText = "";
            public float MeasuredSize;
            public float Scale;
            public float Advance;
            public float Ascent;
            public float InkCenterAboveBaseline;
        }
    }
}
