using System;
using System.Globalization;
using RowMate.Core.Common;
using RowMate.Presentation;
using RowMate.Presentation.Workout;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Simulator.Presentation
{
    // Solo con el simulador (AC-WKT-14-19 [Device-Win]; 04b-hud-exr/02-tech-design §3.13; DT4b-14): mientras el inmersivo está
    // arriba, mide el tiempo de CPU del hilo principal de cada cuadro (FrameTimingManager.CaptureFrameTimings y GetLatestTimings:
    // cpuMainThreadFrameTime, en ms) y cada 10 s escribe en el registro "[PERF] cpu del hilo principal: máx X ms, p99 Y ms, N
    // cuadros". "Ningún cuadro tarda más de 33 ms" = el máximo de esa medida ≤ 33 en Serie larga. Las estadísticas necesitan
    // PlayerSettings.enableFrameTimingStats (BuildCli las prende solo en el build de desarrollo con simulador). Sin
    // FrameTimingManager, anota "[PERF] sin FrameTimingManager: NO VERIFICABLE" y el intervalo entre cuadros solo como información.
    // En el Editor no mide (el cuadro del Editor no es la métrica).
    public sealed class FrameTimeProbe
    {
        public const string Tag = "PERF";
        public const double WindowSeconds = 10;
        // Hasta 4096 muestras por ventana para el p99 (10 s a 400 fps); el máximo y la cuenta miran TODOS los cuadros.
        private const int Capacity = 4096;

        private readonly UiContext _ui;
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private readonly double[] _samples = new double[Capacity];
        private readonly double[] _sorted = new double[Capacity];
        private readonly System.Diagnostics.Stopwatch _interval = new System.Diagnostics.Stopwatch();
        private int _count;
        private int _total;
        private double _max;
        private double _windowStart = double.NaN;
        private bool _warned;

        public FrameTimeProbe(UiContext ui, VisualElement host)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (Application.isEditor)
            {
                return;
            }

            host.schedule.Execute(Sample).Every(0);
        }

        private void Sample()
        {
            if (!(_ui.Pages.Top is ImmersiveWorkoutPage))
            {
                _count = 0;
                _total = 0;
                _max = 0;
                _windowStart = double.NaN;
                _interval.Reset();
                return;
            }

            var now = Time.realtimeSinceStartupAsDouble;
            if (double.IsNaN(_windowStart))
            {
                _windowStart = now;
            }

            double sample;
            if (FrameTimingManager.IsFeatureEnabled())
            {
                FrameTimingManager.CaptureFrameTimings();
                sample = FrameTimingManager.GetLatestTimings(1, _timing) > 0 ? _timing[0].cpuMainThreadFrameTime : double.NaN;
            }
            else
            {
                if (!_warned)
                {
                    _warned = true;
                    _ui.Logger.Log(LogLevel.Warning, Tag, "sin FrameTimingManager: NO VERIFICABLE (el intervalo entre cuadros, solo como información)");
                }

                sample = _interval.IsRunning ? _interval.Elapsed.TotalMilliseconds : double.NaN;
                _interval.Restart();
            }

            if (!double.IsNaN(sample))
            {
                _total++;
                _max = Math.Max(_max, sample);
                if (_count < Capacity)
                {
                    _samples[_count++] = sample;
                }
            }

            if (now - _windowStart < WindowSeconds || _count == 0)
            {
                return;
            }

            Array.Copy(_samples, _sorted, _count);
            Array.Sort(_sorted, 0, _count);
            var max = _max;
            var p99 = _sorted[Math.Min(_count - 1, (int)Math.Ceiling(0.99 * _count) - 1)];
            _ui.Logger.Log(LogLevel.Info, Tag, "cpu del hilo principal: máx " + max.ToString("0.0", CultureInfo.InvariantCulture) + " ms, p99 "
                + p99.ToString("0.0", CultureInfo.InvariantCulture) + " ms, " + _total.ToString(CultureInfo.InvariantCulture) + " cuadros");
            _count = 0;
            _total = 0;
            _max = 0;
            _windowStart = now;
        }
    }
}
