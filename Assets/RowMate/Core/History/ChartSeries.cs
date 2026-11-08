using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.History
{
    // Lo que el gráfico necesita de una métrica, armado una vez y guardado (SessionChartData.View): sus ejes, sus líneas
    // objetivo, el mayor valor dibujado (curva y objetivos) y los textos de las etiquetas (sin etiquetas Y en el split sin
    // ningún ritmo, Q-24).
    public sealed class ChartMetricView
    {
        internal ChartMetricView(ChartMetric metric, YAxis y, XAxis x, IReadOnlyList<TargetLine> targets, double maxPlotted,
                                 IReadOnlyList<string> yLabels, IReadOnlyList<string> xLabels)
        {
            Metric = metric;
            Y = y;
            X = x;
            Targets = targets;
            MaxPlotted = maxPlotted;
            YLabels = yLabels;
            XLabels = xLabels;
        }

        public ChartMetric Metric { get; }

        public YAxis Y { get; }

        public XAxis X { get; }

        public IReadOnlyList<TargetLine> Targets { get; }

        public double MaxPlotted { get; }

        // Una por marca de Y.Ticks, o ninguna.
        public IReadOnlyList<string> YLabels { get; }

        // Una por marca de X.Ticks.
        public IReadOnlyList<string> XLabels { get; }
    }

    // Los datos del gráfico de una sesión (F-HIS-13; session_detail_screen.dart:291-311), armados una vez por apertura:
    // - X: el tiempo DEL MONITOR de cada punto (no arranca en 0, AC-HIS-13-01).
    // - Raw: los valores tal cual, con los ceros (AC-HIS-13-02), en double (el tooltip los formatea con toStringAsFixed).
    // - Plotted: lo que se dibuja (float): igual que Raw salvo el split, invertido como lento − ritmo, con el ritmo 0 al piso
    //   (DT9-08, Q-24: el legacy pone lento − 0 = arriba). Lento = el máximo de los ritmos > 0 (0 si no hay ninguno).
    // - Las franjas (StepBands) y, perezosas por métrica, sus vistas (ejes, objetivos y etiquetas).
    public sealed class SessionChartData
    {
        private const int MetricCount = 5;

        private readonly double[][] _raw;
        private readonly float[][] _plotted;
        private readonly double[] _maxPlotted;
        private readonly ChartMetricView?[] _views = new ChartMetricView?[MetricCount];
        private readonly IReadOnlyList<IntervalStep> _flatSteps;

        private SessionChartData(double[] x, double[][] raw, float[][] plotted, double[] maxPlotted, double slowestPace,
                                 IReadOnlyList<StepBand> bands, IReadOnlyList<IntervalStep> flatSteps)
        {
            X = x;
            _raw = raw;
            _plotted = plotted;
            _maxPlotted = maxPlotted;
            SlowestPace = slowestPace;
            Bands = bands;
            _flatSteps = flatSteps;
        }

        public int Count => X.Length;

        public double[] X { get; }

        public double SlowestPace { get; }

        public bool HasPace => SlowestPace > 0;

        public IReadOnlyList<StepBand> Bands { get; }

        public static SessionChartData Build(IReadOnlyList<DataPoint> points, IReadOnlyList<IntervalStep> flatSteps)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (flatSteps == null)
            {
                throw new ArgumentNullException(nameof(flatSteps));
            }

            var n = points.Count;
            var x = new double[n];
            var raw = new double[MetricCount][];
            var plotted = new float[MetricCount][];
            for (var m = 0; m < MetricCount; m++)
            {
                raw[m] = new double[n];
                plotted[m] = new float[n];
            }

            double slowest = 0;
            for (var i = 0; i < n; i++)
            {
                var p = points[i] ?? throw new ArgumentException("SessionChartData: un punto nulo.", nameof(points));
                x[i] = p.ElapsedSeconds;
                raw[(int)ChartMetric.Watts][i] = p.PowerWatts;
                raw[(int)ChartMetric.Spm][i] = p.StrokeRate;
                raw[(int)ChartMetric.Split][i] = p.Pace500mSeconds;
                raw[(int)ChartMetric.Distance][i] = p.DistanceMeters;
                raw[(int)ChartMetric.HeartRate][i] = p.HeartRate;
                if (p.Pace500mSeconds > 0 && p.Pace500mSeconds > slowest)
                {
                    slowest = p.Pace500mSeconds;
                }
            }

            var maxPlotted = new double[MetricCount];
            for (var m = 0; m < MetricCount; m++)
            {
                var values = raw[m];
                var target = plotted[m];
                var max = double.NegativeInfinity;
                for (var i = 0; i < n; i++)
                {
                    var y = m == (int)ChartMetric.Split ? PlottedPace(values[i], slowest) : values[i];
                    target[i] = (float)y;
                    if (y > max)
                    {
                        max = y;
                    }
                }

                maxPlotted[m] = n == 0 ? 0 : max;
            }

            return new SessionChartData(x, raw, plotted, maxPlotted, slowest, StepBands.Build(points), flatSteps);
        }

        // DT9-08: ritmo > 0 → lento − ritmo; si no, el piso.
        public static double PlottedPace(double pace, double slowestPace)
        {
            return pace > 0 ? slowestPace - pace : 0;
        }

        public double[] Raw(ChartMetric metric)
        {
            return _raw[Index(metric)];
        }

        public float[] Plotted(ChartMetric metric)
        {
            return _plotted[Index(metric)];
        }

        // Perezosa y en caché: una vez por métrica (nunca por cuadro).
        public ChartMetricView View(ChartMetric metric)
        {
            var index = Index(metric);
            var view = _views[index];
            if (view != null)
            {
                return view;
            }

            var targets = TargetLines.Build(Bands, _flatSteps, metric, SlowestPace);
            // sd:390-397: el máximo de la curva y de los puntos de las líneas objetivo.
            var max = _maxPlotted[index];
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i].Y > max)
                {
                    max = targets[i].Y;
                }
            }

            var y = YAxis.For(max);
            var x = Count == 0 ? XAxis.For(0, 0) : XAxis.For(X[0], X[Count - 1]);
            var yLabels = new List<string>(y.Ticks.Length);
            if (metric != ChartMetric.Split || HasPace)
            {
                for (var i = 0; i < y.Ticks.Length; i++)
                {
                    yLabels.Add(metric == ChartMetric.Split ? HistoryFormats.AxisPace(SlowestPace, y.Ticks[i]) : HistoryFormats.AxisValue(y.Ticks[i]));
                }
            }

            var xLabels = new string[x.Ticks.Length];
            for (var i = 0; i < xLabels.Length; i++)
            {
                xLabels[i] = HistoryFormats.AxisTime(x.Ticks[i]);
            }

            view = new ChartMetricView(metric, y, x, targets, max, yLabels, xLabels);
            _views[index] = view;
            return view;
        }

        private static int Index(ChartMetric metric)
        {
            var index = (int)metric;
            if (index < 0 || index >= MetricCount)
            {
                throw new ArgumentOutOfRangeException(nameof(metric), metric, "ChartMetric desconocida.");
            }

            return index;
        }
    }
}
