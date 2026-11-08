using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Text;

namespace RowMate.Core.History
{
    // Los intervalos de fl_chart 1.1.1 al pie de la letra (lib/src/utils/utils.dart:117-187): getEfficientInterval y
    // roundInterval (debajo de 1 con el toString de Dart: Fmt.DartToString).
    public static class FlChartInterval
    {
        // getEfficientInterval (:117-131): allowedCount = max(viewSize ~/ pixelPerInterval, 1); diff 0 → 1; con 2 o menos
        // divisiones, diff / allowedCount sin redondear; si no, roundInterval(diff / allowedCount).
        public static double Efficient(double axisViewSize, double diffInAxis, double pixelPerInterval = ChartSpecs.LegacyPixelsPerInterval)
        {
            var allowedCount = Math.Max(DartMath.TruncDiv(axisViewSize, pixelPerInterval), 1);
            if (diffInAxis == 0)
            {
                return 1;
            }

            var accurate = diffInAxis / allowedCount;
            if (allowedCount <= 2)
            {
                return accurate;
            }

            return Round(accurate);
        }

        // roundInterval (:134-140).
        public static double Round(double input)
        {
            return input < 1 ? RoundBelowOne(input) : RoundAboveOne(input);
        }

        // _roundIntervalBelowOne (:142-168): cuenta las cifras del toString de Dart, deja a lo sumo 2 significativas después de
        // los ceros, escala a ≥ 1, redondea y vuelve.
        private static double RoundBelowOne(double input)
        {
            if (input < 0.000001)
            {
                return input;
            }

            var text = Fmt.DartToString(input);
            var precisionCount = text.Length - 2;
            var zeroCount = 0;
            for (var i = 2; i < text.Length; i++)
            {
                if (text[i] != '0')
                {
                    break;
                }

                zeroCount++;
            }

            var afterZeros = precisionCount - zeroCount;
            if (afterZeros > 2)
            {
                precisionCount -= afterZeros - 2;
            }

            var pow = Math.Pow(10, precisionCount);
            return RoundAboveOne(input * pow) / pow;
        }

        // _roundIntervalAboveOne (:170-187): la mantisa s en [1; 10) → 10 si s ≥ 7,6, 5 si ≥ 2,6, 2 si ≥ 1,6 y 1 si no, por su
        // potencia de 10.
        private static double RoundAboveOne(double input)
        {
            var decimalCount = Fmt.Integer((long)Math.Truncate(input)).Length - 1;
            var pow = Math.Pow(10, decimalCount);
            input /= pow;
            var scaled = input >= 10 ? DartMath.Round(input) / 10.0 : input;
            if (scaled >= 7.6)
            {
                return 10 * pow;
            }

            if (scaled >= 2.6)
            {
                return 5 * pow;
            }

            if (scaled >= 1.6)
            {
                return 2 * pow;
            }

            return pow;
        }
    }

    // iterateThroughAxis (lib/src/chart/base/axis_chart/axis_chart_helper.dart:23-58) con getBestInitialIntervalValue
    // (utils.dart:295-310): el módulo es el de Dart (nunca negativo, DartMath.Mod), así una marca cae en los múltiplos del paso
    // contados desde baseline. Las etiquetas usan minIncluded y maxIncluded (el 0 y el máximo, AC-HIS-13-04); la grilla, no.
    public static class AxisTicks
    {
        // Escribe los valores en outValues y devuelve cuántos son. Si no entran, ArgumentException.
        public static int Iterate(double min, double max, double baseline, double interval, bool minIncluded, bool maxIncluded, double[] outValues)
        {
            if (outValues == null)
            {
                throw new ArgumentNullException(nameof(outValues));
            }

            if (!(interval > 0) || double.IsInfinity(interval))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "AxisTicks: el paso tiene que ser positivo.");
            }

            var count = 0;
            var initialValue = BestInitialValue(min, max, interval, baseline);
            var axisSeek = initialValue;
            var firstOverlapsWithMin = axisSeek == min;
            if (!minIncluded && firstOverlapsWithMin)
            {
                axisSeek += interval;
            }

            var diff = max - min;
            var steps = DartMath.TruncDiv(diff, interval);
            var lastPosition = initialValue + steps * interval;
            var lastOverlapsWithMax = lastPosition == max;
            var end = !maxIncluded && lastOverlapsWithMax ? max - interval : max;
            var epsilon = interval / 100000;
            if (minIncluded && !firstOverlapsWithMin)
            {
                count = Put(outValues, count, min);
            }

            while (axisSeek <= end + epsilon)
            {
                count = Put(outValues, count, axisSeek);
                axisSeek += interval;
            }

            if (maxIncluded && !lastOverlapsWithMax)
            {
                count = Put(outValues, count, max);
            }

            return count;
        }

        // Lo mismo en un arreglo nuevo (una vez por métrica; nunca por cuadro).
        public static double[] Iterate(double min, double max, double baseline, double interval, bool minIncluded, bool maxIncluded)
        {
            var guess = (int)Math.Min(4096, Math.Max(0, Math.Floor(Math.Abs(max - min) / interval)) + 4);
            var buffer = new double[guess];
            var count = Iterate(min, max, baseline, interval, minIncluded, maxIncluded, buffer);
            var result = new double[count];
            Array.Copy(buffer, result, count);
            return result;
        }

        // getBestInitialIntervalValue (utils.dart:299-310).
        public static double BestInitialValue(double min, double max, double interval, double baseline = 0)
        {
            var diff = baseline - min;
            var mod = DartMath.Mod(diff, interval);
            if (Math.Abs(max - min) <= mod)
            {
                return min;
            }

            if (mod == 0)
            {
                return min;
            }

            return min + mod;
        }

        private static int Put(double[] values, int count, double value)
        {
            if (count >= values.Length)
            {
                throw new ArgumentException("AxisTicks: las marcas no entran en el arreglo.", nameof(values));
            }

            values[count] = value;
            return count + 1;
        }
    }

    // El eje Y de una métrica (AC-HIS-13-04; sd:390-402): de 0 a 1,1 × el mayor valor dibujado (curva y objetivos) o 100 si
    // ese máximo no es positivo; el paso es el de fl_chart con el área del legacy (196 / 40 = 4 divisiones, DT9-09). Ticks:
    // las etiquetas (con el 0 y el máximo); Grid: las marcas interiores, donde va la grilla horizontal (sin 0 ni máximo).
    public readonly struct YAxis
    {
        private YAxis(double max, double interval, double[] ticks, double[] grid)
        {
            Max = max;
            Interval = interval;
            Ticks = ticks;
            Grid = grid;
        }

        public double Max { get; }

        public double Interval { get; }

        public double[] Ticks { get; }

        public double[] Grid { get; }

        public static double MaxFor(double maxPlotted)
        {
            var max = maxPlotted * ChartSpecs.YHeadroom;
            return max > 0 ? max : ChartSpecs.EmptyYMax;
        }

        public static YAxis For(double maxPlotted)
        {
            var max = MaxFor(maxPlotted);
            var interval = FlChartInterval.Efficient(ChartSpecs.LegacyYAxisViewSize, max);
            var ticks = AxisTicks.Iterate(0, max, 0, interval, true, true);
            var grid = AxisTicks.Iterate(0, max, 0, interval, false, false);
            return new YAxis(max, interval, ticks, grid);
        }
    }

    // El eje X (AC-HIS-13-01, -05; sd:427-439, :525-532): del primer punto al último (el tiempo del monitor, no arranca en 0),
    // con el paso de HistoryRules.XIntervalFor(último) y las marcas en sus múltiplos más el inicio y el fin.
    public readonly struct XAxis
    {
        private XAxis(double min, double max, double interval, double[] ticks)
        {
            Min = min;
            Max = max;
            Interval = interval;
            Ticks = ticks;
        }

        public double Min { get; }

        public double Max { get; }

        public double Interval { get; }

        public double[] Ticks { get; }

        // Un solo punto: Min = Max (gate m-02: la vista lo centra).
        public bool IsDegenerate => Max <= Min;

        public static XAxis For(double first, double last)
        {
            var interval = HistoryRules.XIntervalFor((int)Math.Truncate(last));
            var ticks = AxisTicks.Iterate(first, last, 0, interval, true, true);
            return new XAxis(first, last, interval, ticks);
        }
    }
}
