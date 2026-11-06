using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Cifras tabulares con el rodillo de 180 DIBUJADAS ADENTRO DE OTRO ELEMENTO (05-series-parciales/02-tech-design §3.1; DT5-13): la
    // lógica de celdas y del rodillo de TabularNumber (ARCHITECTURE §7.8; DN-08) sin ser un elemento, para los paneles de series,
    // que dibujan todo en un solo generateVisualContent (DD5-04; el presupuesto R4a-03). TabularNumber no se toca en esta iteración
    // (verificado en la 4a y la 4b); unificarlos queda como deuda (DT5-13).
    // - Celda de un dígito = el avance máximo de 0-9 en la fuente y el tamaño; ':' y '.' en 0,32 em; el resto con su avance. Cada
    //   glifo centrado en su celda; las celdas, pegadas a la derecha (el borde derecho es left + ancho, o un ancla a la derecha).
    // - Set(texto, valor, animar): las celdas que cambian ruedan (DigitRoll, 180 ms ease.enter; reducir movimiento: el fundido de
    //   80); si llega otro valor, el rodillo en curso salta al final. Si cambia el largo, el ancho interpola 180 ms.
    // - DrawFromLeft (lo anclado a la izquierda con un rótulo, la línea Rep; QA-V5-02): rueda por tramos (DigitRuns), así lo que no
    //   cambia ("Rep 2/3 · ", " m") no rueda.
    // - Sin asignar en el cuadro a cuadro: el texto es la cadena de la lectura (una por segundo) y las cadenas de un carácter están
    //   precalculadas (DrawnFont.CharString).
    // - Es un IMotion (finito) solo mientras rueda: avisa repaint en cada cuadro del rodillo.
    public sealed class TabularCells : IMotion, IRecurringMotion
    {
        private readonly MotionDriver? _motion;
        private readonly Action _repaint;
        private string _text = "";
        private string _previous = "";
        private double _value;
        private DigitRollPlan _plan;
        private bool _rolling;
        private bool _reduced;
        private double _rollStart;

        // QA-V5-02: los tramos del texto anterior y del nuevo (DigitRuns) cuando tienen la misma forma; _runCount < 0 si no.
        private readonly int[] _previousStarts = new int[DigitRuns.MaxRuns];
        private readonly int[] _previousLengths = new int[DigitRuns.MaxRuns];
        private readonly bool[] _previousNumeric = new bool[DigitRuns.MaxRuns];
        private readonly int[] _nextStarts = new int[DigitRuns.MaxRuns];
        private readonly int[] _nextLengths = new int[DigitRuns.MaxRuns];
        private readonly bool[] _nextNumeric = new bool[DigitRuns.MaxRuns];
        private int _runCount = -1;

        public TabularCells(MotionDriver? motion, Action repaint)
        {
            _motion = motion;
            _repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
        }

        public string Text => _text;

        public double Value => _value;

        public bool IsFinite => true;

        public bool IsRolling => _rolling;

        // El rodillo en curso va por tramos (DrawFromLeft; QA-V5-02).
        public bool RollsByRuns => _rolling && _runCount >= 0;

        // La celda cell del texto nuevo rueda en el rodillo en curso (por tramos si los hay; si no, el plan de toda la cadena).
        public bool RollsCell(int cell)
        {
            if (!_rolling || cell < 0 || cell >= _text.Length)
            {
                return false;
            }

            if (_runCount < 0)
            {
                return _plan.Length == _text.Length && _plan.IsChanged(cell);
            }

            for (var r = 0; r < _runCount; r++)
            {
                var start = _nextStarts[r];
                var length = _nextLengths[r];
                if (cell < start || cell >= start + length)
                {
                    continue;
                }

                if (!_nextNumeric[r])
                {
                    return false;
                }

                var p = DigitRuns.PreviousCell(_previousStarts[r], _previousLengths[r], length, cell - start);
                return p < 0 || _previous[p] != _text[cell];
            }

            return false;
        }

        // 0 → 1 del rodillo en curso (1 sin rodillo): la regla de toma (un rodillo antes de su mitad bloquea).
        public double RollProgress
        {
            get
            {
                if (!_rolling || _motion == null)
                {
                    return 1;
                }

                var u = (_motion.Now - _rollStart) * 1000.0 / (_reduced ? MotionSpecs.ReducedFadeMs : DigitRoll.DurationMs);
                return u < 0 ? 0 : u > 1 ? 1 : u;
            }
        }

        // El texto nuevo, en el cuadro del dato. animate = false: sin rodillo (Instant, el primer estado, un panel cerrado).
        public void Set(string text, double value, bool animate)
        {
            text ??= "";
            if (text == _text)
            {
                _value = value;
                return;
            }

            if (_rolling)
            {
                Finish();
            }

            animate = animate && _motion != null && _motion.Armed && _text.Length > 0;
            var previousValue = _value;
            _previous = _text;
            _text = text;
            _value = value;
            _runCount = -1;
            if (animate)
            {
                _plan = DigitRoll.Plan(_previous, _text, previousValue, value);
                _rolling = _plan.HasChanges;
                _reduced = _motion!.Settings.ReduceMotion;
                _rollStart = _motion.Now;
                if (_rolling)
                {
                    // Por tramos (DrawFromLeft), si los dos textos tienen la misma forma (una vez por dato, nunca por cuadro).
                    var previousRuns = DigitRuns.Split(_previous, _previousStarts, _previousLengths, _previousNumeric);
                    var nextRuns = DigitRuns.Split(_text, _nextStarts, _nextLengths, _nextNumeric);
                    if (DigitRuns.SameShape(_previous, previousRuns, _previousStarts, _previousLengths, _previousNumeric, _text, nextRuns, _nextStarts,
                            _nextLengths, _nextNumeric))
                    {
                        _runCount = nextRuns;
                    }

                    _motion.Add(this);
                }
            }

            _repaint();
        }

        // Termina el rodillo en el acto (la salida, el panel que se va, Stop).
        public void Finish()
        {
            if (!_rolling)
            {
                return;
            }

            _rolling = false;
            _motion?.Remove(this);
            _repaint();
        }

        public bool Tick(double nowSeconds)
        {
            if (!_rolling)
            {
                return false;
            }

            _repaint();
            var elapsed = (nowSeconds - _rollStart) * 1000.0;
            if (DigitRoll.IsFinished(elapsed, _reduced))
            {
                _rolling = false;
                return false;
            }

            return true;
        }

        // El ancho del texto que queda (base).
        public float Width(DrawnFont font, float sizeDp)
        {
            return Measure(font, _text, sizeDp);
        }

        // El ancho de las celdas en este cuadro (interpola si el rodillo cambia el largo).
        public float WidthNow(DrawnFont font, float sizeDp)
        {
            var to = Measure(font, _text, sizeDp);
            if (!_rolling || _motion == null || _previous.Length == _text.Length)
            {
                return to;
            }

            var from = Measure(font, _previous, sizeDp);
            return (float)DigitRoll.WidthAt((_motion.Now - _rollStart) * 1000.0, from, to);
        }

        // Dibuja las celdas con su borde derecho en right (base), la línea de base en baseline. color: el de la ficha.
        public void Draw(MeshGenerationContext context, DrawnFont font, float sizeDp, float right, float baseline, Color color, float alpha, float pixelsPerPoint)
        {
            if (alpha <= 0.001f || _text.Length == 0 || font.Font == null || sizeDp <= 0)
            {
                return;
            }

            if (!_rolling || _motion == null || _plan.Length != _text.Length)
            {
                DrawPlain(context, font, _text, sizeDp, right, baseline, color, alpha, pixelsPerPoint);
                return;
            }

            var mesh = MeshColor.ForMesh(color);
            var baseAlpha = color.a * alpha;
            var top = baseline - font.AscentAt(sizeDp);
            var size = sizeDp * pixelsPerPoint;
            var elapsed = (_motion.Now - _rollStart) * 1000.0;
            DigitRoll.Evaluate(elapsed, _plan.Direction, _reduced, out var outOffset, out var outAlpha, out var inOffset, out var inAlpha);
            var x = right;
            for (var i = _text.Length - 1; i >= 0; i--)
            {
                var c = _text[i];
                var cell = CellWidth(font, c, sizeDp);
                x -= cell;
                if (_plan.IsChanged(i))
                {
                    var outgoing = _plan.OutgoingAt(i);
                    if (outgoing != '\0' && outAlpha > 0.001)
                    {
                        DrawChar(context, font, outgoing, x, CellWidth(font, outgoing, sizeDp), top + (float)(outOffset * sizeDp), size, sizeDp, mesh,
                            baseAlpha * (float)outAlpha);
                    }

                    if (inAlpha > 0.001)
                    {
                        DrawChar(context, font, c, x, cell, top + (float)(inOffset * sizeDp), size, sizeDp, mesh, baseAlpha * (float)inAlpha);
                    }
                }
                else
                {
                    DrawChar(context, font, c, x, cell, top, size, sizeDp, mesh, baseAlpha);
                }
            }

            // Las celdas que se van (el texto se acortó): solo salen, a la izquierda.
            var removed = _plan.RemovedCells;
            if (removed > 0 && outAlpha > 0.001)
            {
                for (var k = removed - 1; k >= 0; k--)
                {
                    var c = _previous[k];
                    var w = CellWidth(font, c, sizeDp);
                    x -= w;
                    DrawChar(context, font, c, x, w, top + (float)(outOffset * sizeDp), size, sizeDp, mesh, baseAlpha * (float)outAlpha);
                }
            }
        }

        // Dibuja con el borde IZQUIERDO fijo en left (la línea Rep, anclada a la izquierda con su rótulo; QA-V5-02, H5-D2-01). Si el
        // texto anterior y el nuevo tienen la misma forma (DigitRuns: los mismos tramos de texto, las cifras en el mismo lugar), rueda
        // tramo por tramo: el texto no rueda, cada tramo de cifras rueda alineado a su derecha con su ancho interpolado y lo que
        // sigue se corre con él ("Rep 2/3 · 8 m" → "Rep 2/3 · 12 m": solo el 1 entra y el 8 → 2 rueda; " m" acompaña el ancho). Si
        // no, como Draw con el borde derecho en left + el ancho de este cuadro. Sin asignar.
        public void DrawFromLeft(MeshGenerationContext context, DrawnFont font, float sizeDp, float left, float baseline, Color color, float alpha,
                                 float pixelsPerPoint)
        {
            if (alpha <= 0.001f || _text.Length == 0 || font.Font == null || sizeDp <= 0)
            {
                return;
            }

            if (!_rolling || _motion == null || _runCount < 0 || _plan.Length != _text.Length)
            {
                Draw(context, font, sizeDp, left + WidthNow(font, sizeDp), baseline, color, alpha, pixelsPerPoint);
                return;
            }

            var mesh = MeshColor.ForMesh(color);
            var baseAlpha = color.a * alpha;
            var top = baseline - font.AscentAt(sizeDp);
            var size = sizeDp * pixelsPerPoint;
            var elapsed = (_motion.Now - _rollStart) * 1000.0;
            DigitRoll.Evaluate(elapsed, _plan.Direction, _reduced, out var outOffset, out var outAlpha, out var inOffset, out var inAlpha);
            var outTop = top + (float)(outOffset * sizeDp);
            var inTop = top + (float)(inOffset * sizeDp);
            var x = left;
            for (var r = 0; r < _runCount; r++)
            {
                var nextStart = _nextStarts[r];
                var nextLength = _nextLengths[r];
                if (!_nextNumeric[r])
                {
                    // El texto (igual en los dos): quieto, corrido por lo que crece o se achica antes.
                    for (var j = 0; j < nextLength; j++)
                    {
                        var c = _text[nextStart + j];
                        var cell = CellWidth(font, c, sizeDp);
                        DrawChar(context, font, c, x, cell, top, size, sizeDp, mesh, baseAlpha);
                        x += cell;
                    }

                    continue;
                }

                var previousStart = _previousStarts[r];
                var previousLength = _previousLengths[r];
                var from = MeasureRange(font, _previous, previousStart, previousLength, sizeDp);
                var to = MeasureRange(font, _text, nextStart, nextLength, sizeDp);
                var right = x + (float)DigitRoll.WidthAt(elapsed, from, to);
                var cx = right;
                for (var j = nextLength - 1; j >= 0; j--)
                {
                    var c = _text[nextStart + j];
                    var cell = CellWidth(font, c, sizeDp);
                    cx -= cell;
                    var p = DigitRuns.PreviousCell(previousStart, previousLength, nextLength, j);
                    if (p >= 0 && _previous[p] == c)
                    {
                        DrawChar(context, font, c, cx, cell, top, size, sizeDp, mesh, baseAlpha);
                        continue;
                    }

                    if (p >= 0 && outAlpha > 0.001)
                    {
                        var outgoing = _previous[p];
                        DrawChar(context, font, outgoing, cx, CellWidth(font, outgoing, sizeDp), outTop, size, sizeDp, mesh, baseAlpha * (float)outAlpha);
                    }

                    if (inAlpha > 0.001)
                    {
                        DrawChar(context, font, c, cx, cell, inTop, size, sizeDp, mesh, baseAlpha * (float)inAlpha);
                    }
                }

                // Las celdas que se van (el tramo se acortó): solo salen, a la izquierda del tramo.
                if (previousLength > nextLength && outAlpha > 0.001)
                {
                    for (var k = previousLength - nextLength - 1; k >= 0; k--)
                    {
                        var c = _previous[previousStart + k];
                        var w = CellWidth(font, c, sizeDp);
                        cx -= w;
                        DrawChar(context, font, c, cx, w, outTop, size, sizeDp, mesh, baseAlpha * (float)outAlpha);
                    }
                }

                x = right;
            }
        }

        private static float MeasureRange(DrawnFont font, string text, int start, int length, float sizeDp)
        {
            var total = 0f;
            for (var i = start; i < start + length; i++)
            {
                total += CellWidth(font, text[i], sizeDp);
            }

            return total;
        }

        // Un texto en celdas tabulares sin rodillo (las filas, lo que sale en un cambio de pieza): sin asignar.
        public static void DrawPlain(MeshGenerationContext context, DrawnFont font, string text, float sizeDp, float right, float baseline, Color color,
                                     float alpha, float pixelsPerPoint)
        {
            if (alpha <= 0.001f || string.IsNullOrEmpty(text) || font.Font == null || sizeDp <= 0)
            {
                return;
            }

            var mesh = MeshColor.ForMesh(color);
            var a = color.a * alpha;
            var top = baseline - font.AscentAt(sizeDp);
            var size = sizeDp * pixelsPerPoint;
            var x = right;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var c = text[i];
                var cell = CellWidth(font, c, sizeDp);
                x -= cell;
                DrawChar(context, font, c, x, cell, top, size, sizeDp, mesh, a);
            }
        }

        // El ancho de un texto en celdas tabulares (base).
        public static float Measure(DrawnFont font, string? text, float sizeDp)
        {
            if (string.IsNullOrEmpty(text) || font.Font == null)
            {
                return 0;
            }

            var total = 0f;
            for (var i = 0; i < text!.Length; i++)
            {
                total += CellWidth(font, text[i], sizeDp);
            }

            return total;
        }

        public static float CellWidth(DrawnFont font, char c, float sizeDp)
        {
            if (c >= '0' && c <= '9')
            {
                return font.DigitAdvance(sizeDp);
            }

            if (c == ':' || c == '.')
            {
                return (float)(MotionSpecs.ColonCellEm * sizeDp);
            }

            return font.Advance(c, sizeDp);
        }

        private static void DrawChar(MeshGenerationContext context, DrawnFont font, char c, float cellX, float cellWidth, float top, float pixelSize,
                                     float sizeDp, Color mesh, float alpha)
        {
            if (alpha <= 0.001f || c == ' ')
            {
                return;
            }

            var advance = font.Advance(c, sizeDp);
            mesh.a = alpha;
            context.DrawText(DrawnFont.CharString(c), new Vector2(cellX + (cellWidth - advance) / 2f, top), pixelSize, mesh, font.Font);
        }
    }
}
