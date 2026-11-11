using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.History;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // Las etiquetas de los ejes del gráfico (AC-HIS-13-04, -05, -06; arte §4; 02-tech-design DT9-12): Label reusados en una capa
    // sin eventos, ubicados con translate (sin layout). Fuente numérica tabular de 11 en text.tertiary (.rm-chart__axis), sin
    // escala de letra (el gráfico mide 220 siempre).
    // - Y: alineadas a la derecha a 6 del área y centradas en su marca (#chart-y-<i>). Dos juegos (el que entra y el que sale en
    //   el cambio de métrica: viejas 0-120, nuevas 100-220); en el split, m:ss del ritmo; sin ritmo, ninguna (Q-24).
    // - X: centradas en su marca, debajo del área (#chart-x-<i>); las de los bordes se empujan adentro del marco (como fl_chart).
    // Los textos y las posiciones cambian solo al cambiar de métrica o de ancho; en cada cuadro de una animación, solo la opacidad.
    // - It. 14 (#32, AC-DEBT-07; DT14-28): una marca INTERMEDIA cuya etiqueta quedaría a menos de 4 dp de la de un borde no se dibuja
    //   (AxisTicks.DropCrowded sobre las posiciones finales, ya empujadas adentro del marco), en X y en Y. Sigue en XTexts/YTexts (son
    //   las marcas del eje); XDropped/YDropped dicen cuáles quedaron ocultas. Sin asignar: arreglos que crecen solo al preparar.
    public sealed class ChartAxisLabels : VisualElement
    {
        public const string AxisClass = "rm-chart__axis";

        // It. 15 (#37/#76, AC-D15-25, DT15-22): las cifras en RowMate Numeric SC 600 a 11 dp en text.tertiary (la USS de WP-D).
        public const string NumClass = "rm-chart-axis__num";
        public const float LabelHeight = 14;
        public const float XLabelGap = 4;

        private readonly List<Label> _x = new List<Label>();
        private readonly List<Label>[] _y = { new List<Label>(), new List<Label>() };
        private readonly int[] _yCount = new int[2];
        private int _xCount;
        private int _currentY;
        private double[] _centers = new double[8];
        private double[] _extents = new double[8];
        private bool[] _visible = new bool[8];
        private bool[] _xDropped = new bool[8];
        private bool[] _yDropped = new bool[8];

        // It. 15 (#34, AC-D15-04; QA-V15-01): la opacidad de cada animación va en TRES capas (las X, el juego Y 0 y el juego Y 1), no en
        // cada etiqueta. Medido con las pilas del Profiler: una opacidad en línea que cambia rehace el estilo del elemento y UI Toolkit
        // vuelve a aplicar (y copia) los grupos de TODOS sus estilos en línea (la posición, el alto, el ancho, el traslado, el display) en
        // cada cuadro del cambio de métrica. Las capas no tienen otro estilo en línea que la opacidad (por una opacidad sola no se copia
        // nada, 06a V2-01): sin alto (las etiquetas son absolutas) y arriba a la izquierda de #chart-axis, así las etiquetas quedan donde
        // estaban.
        private readonly VisualElement _xLayer;
        private readonly VisualElement[] _yLayers = new VisualElement[2];

        public ChartAxisLabels()
        {
            name = "chart-axis";
            AddToClassList("rm-chart__labels");
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            _xLayer = Layer("chart-axis-x");
            _yLayers[0] = Layer("chart-axis-y0");
            _yLayers[1] = Layer("chart-axis-y1");
        }

        private VisualElement Layer(string layerName)
        {
            var layer = new VisualElement { name = layerName, pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicColor };
            Add(layer);
            return layer;
        }

        // La capa de las X (su opacidad es la de las etiquetas X; pruebas).
        public VisualElement XLayer => _xLayer;

        public int XCount => _xCount;

        public int YCount => _yCount[_currentY];

        public Label XAt(int index)
        {
            return _x[index];
        }

        public Label YAt(int index)
        {
            return _y[_currentY][index];
        }

        // La etiqueta X i no se dibuja porque chocaba con la de un borde (#32).
        public bool XDropped(int index)
        {
            return index < _xDropped.Length && _xDropped[index];
        }

        public bool YDropped(int index)
        {
            return index < _yDropped.Length && _yDropped[index];
        }

        // Los textos de las marcas X de la vista actual, en orden (pruebas y sondas).
        public string[] XTexts()
        {
            var texts = new string[_xCount];
            for (var i = 0; i < _xCount; i++)
            {
                texts[i] = _x[i].text;
            }

            return texts;
        }

        public string[] YTexts()
        {
            var labels = _y[_currentY];
            var texts = new string[_yCount[_currentY]];
            for (var i = 0; i < texts.Length; i++)
            {
                texts[i] = labels[i].text;
            }

            return texts;
        }

        // Arma las etiquetas de la geometría actual. keepPrevious: el juego Y que estaba pasa a ser el que sale (cambio de métrica).
        public void Layout(ChartGeometry geometry, bool keepPrevious)
        {
            if (geometry == null || !geometry.IsPrepared || geometry.View == null)
            {
                return;
            }

            if (keepPrevious)
            {
                _currentY = 1 - _currentY;
            }
            else
            {
                Hide(_y[1 - _currentY], 0);
                _yCount[1 - _currentY] = 0;
            }

            var view = geometry.View;
            var frame = geometry.Frame;
            var area = frame.Area;

            // Y: una por marca (o ninguna).
            var labels = _y[_currentY];
            var yLabels = view.YLabels;
            var ticks = view.Y.Ticks;
            var count = Math.Min(yLabels.Count, ticks.Length);
            Ensure(labels, count, "chart-y-", _yLayers[_currentY]);
            var right = (float)(area.Left - ChartSpecs.YLabelGap);
            var width = (float)(right - ChartSpecs.Padding + ChartSpecs.Padding / 2);
            for (var i = 0; i < count; i++)
            {
                var label = labels[i];
                label.name = "chart-y-" + i;
                SetText(label, yLabels[i]);
                label.style.width = width;
                label.EnableInClassList(AxisClass + "--y", true);
                var y = (float)geometry.PixelY(ticks[i], view.Y.Max) - LabelHeight / 2;
                y = Mathf.Clamp(y, 0, (float)frame.Height - LabelHeight);
                label.style.translate = new Translate(right - width, y);
                label.style.display = DisplayStyle.Flex;
                Grow(count);
                _centers[i] = y + LabelHeight / 2;
                _extents[i] = LabelHeight;
            }

            Hide(labels, count);
            _yCount[_currentY] = count;
            DropCrowded(labels, count, _yDropped, true);
            if (keepPrevious)
            {
                var old = _y[1 - _currentY];
                for (var i = 0; i < _yCount[1 - _currentY]; i++)
                {
                    old[i].name = "chart-y-old-" + i;
                }
            }

            // X: una por marca, centradas; las de los bordes, adentro del marco.
            var xLabels = view.XLabels;
            var xTicks = view.X.Ticks;
            var xCount = Math.Min(xLabels.Count, xTicks.Length);
            Ensure(_x, xCount, "chart-x-", _xLayer);
            var top = (float)(area.Bottom + XLabelGap);
            for (var i = 0; i < xCount; i++)
            {
                var label = _x[i];
                label.name = "chart-x-" + i;
                SetText(label, xLabels[i]);
                var textWidth = Measure(label);
                label.style.width = textWidth;
                var x0 = (float)geometry.PixelX(xTicks[i]) - textWidth / 2;
                x0 = Mathf.Clamp(x0, (float)ChartSpecs.Padding, Math.Max((float)ChartSpecs.Padding, (float)(frame.Width - ChartSpecs.RightMargin) - textWidth));
                label.style.translate = new Translate(x0, top);
                label.style.display = DisplayStyle.Flex;
                Grow(xCount);
                _centers[i] = x0 + textWidth / 2.0;
                _extents[i] = textWidth;
            }

            Hide(_x, xCount);
            _xCount = xCount;
            DropCrowded(_x, xCount, _xDropped, false);
        }

        // Cada cuadro de una animación: la opacidad de las X (el marco), de las Y nuevas y de las que salen.
        public void SetAlphas(double xAlpha, double newYAlpha, double oldYAlpha)
        {
            SetOpacity(_xLayer, (float)xAlpha);
            SetOpacity(_yLayers[_currentY], (float)newYAlpha);
            var old = _y[1 - _currentY];
            var o = (float)oldYAlpha;
            SetOpacity(_yLayers[1 - _currentY], o);

            if (o <= 0 && _yCount[1 - _currentY] > 0)
            {
                Hide(old, 0);
                _yCount[1 - _currentY] = 0;
            }
        }

        // Las Y van de arriba hacia abajo en pantalla con los valores al revés; DropCrowded solo mira distancias, así que el orden da igual
        // mientras los bordes sean la primera y la última marca.
        private void DropCrowded(List<Label> labels, int count, bool[] dropped, bool vertical)
        {
            Array.Clear(dropped, 0, dropped.Length);
            if (count < 3)
            {
                return;
            }

            AxisTicks.DropCrowded(_centers, _extents, _visible, count);
            for (var i = 0; i < count; i++)
            {
                if (_visible[i])
                {
                    continue;
                }

                dropped[i] = true;
                labels[i].style.display = DisplayStyle.None;
            }
        }

        // Los arreglos de trabajo crecen solo al preparar (una métrica con más marcas), nunca por cuadro.
        private void Grow(int count)
        {
            if (_centers.Length >= count)
            {
                return;
            }

            var size = Math.Max(count, _centers.Length * 2);
            _centers = new double[size];
            _extents = new double[size];
            _visible = new bool[size];
            Array.Resize(ref _xDropped, size);
            Array.Resize(ref _yDropped, size);
        }

        private static void Ensure(List<Label> labels, int count, string prefix, VisualElement layer)
        {
            while (labels.Count < count)
            {
                var label = new Label { name = prefix + labels.Count };
                label.AddToClassList(AxisClass);
                label.AddToClassList(NumClass);
                label.pickingMode = PickingMode.Ignore;
                label.enableRichText = false;
                label.style.position = Position.Absolute;
                label.style.left = 0;
                label.style.top = 0;
                label.style.height = LabelHeight;
                // La opacidad (de su capa) cambia en cada cuadro de la entrada y del cambio: sin el hint, UIR vuelve a teselar el texto.
                label.usageHints = UsageHints.DynamicColor;
                layer.Add(label);
                labels.Add(label);
            }
        }

        private static void Hide(List<Label> labels, int from)
        {
            for (var i = from; i < labels.Count; i++)
            {
                if (labels[i].style.display != DisplayStyle.None)
                {
                    labels[i].style.display = DisplayStyle.None;
                }
            }
        }

        private static void SetText(Label label, string text)
        {
            if (label.text != text)
            {
                label.text = text;
            }
        }

        private static void SetOpacity(VisualElement element, float opacity)
        {
            if (Math.Abs(element.style.opacity.value - opacity) > 1e-4f || element.style.opacity.keyword != StyleKeyword.Undefined)
            {
                element.style.opacity = opacity;
            }
        }

        // El ancho del texto con los avances de la cara SC (AxisCrowding.LabelWidthDp, medidos en el hmtx; It. 15, DT15-22): el mismo
        // con o sin panel, y sin medir el Label (no asigna).
        private static float Measure(Label label)
        {
            return Mathf.Ceil((float)AxisCrowding.LabelWidthDp(label.text)) + 1;
        }
    }
}
