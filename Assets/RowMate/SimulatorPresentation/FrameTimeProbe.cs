using System;
using System.Diagnostics;
using System.Globalization;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Presentation;
using RowMate.Presentation.Workout;
using Unity.Profiling;
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
    // It. 5 (QA-V4b-06; 05-series-parciales/00-deuda-4b §2): además, la ENTRADA de cada inmersivo, una línea con el máximo de los
    // primeros 30 cuadros medidos (las medidas llegan con unos cuadros de atraso, así que incluyen el cuadro del toque):
    // "[PERF] entrada del inmersivo: máx X ms en los primeros 30 cuadros (n.º k)".
    // It. 6a, ronda 1 de la verificación (06a-escena-lago/02-tech-design, "Cambios por verificación (ronda 1)"):
    // - QA-V6a-REN-01: al ver la escena lista escribe la marca "[PERF] marca: escena Ready" (ShaderWorkLog.ReadyMark) y desde ahí cuenta
    //   las subidas de variantes de la escena y de URP ("Uploaded shader variant to the GPU driver: …", ShaderWorkLog) si le llegan por
    //   Application.logMessageReceived; tools/build/perf-log-check.ps1 cuenta lo mismo sobre el registro del player.
    // - QA-V6a-REN-02: del peor cuadro de la entrada dice cuándo fue (ms desde el toque), si la pantalla estaba entera negra y quieta
    //   (desde los 220 ms hasta el cuadro en que se arma el HUD, inclusive: lo único que AC-WKT-13-15 admite de más de 16,7 ms) y
    //   cuántas recolecciones del GC hubo en ese cuadro; y aparte el peor cuadro CON MOVIMIENTO (el que cuenta para AC-WKT-13-15). El
    //   momento sale del "Main Thread" del ProfilerRecorder, con un cuadro de atraso exacto (QA-V6a-R2-04: la correspondencia por el
    //   frameStartTimestamp de FrameTimingManager no servía en este player y se sacó).
    // - QA-V6a-REN-03: por ventana de 10 s, la cuenta de asignaciones del hilo principal por cuadro ("GC Allocation In Frame Count",
    //   solo en los builds de desarrollo): el máximo y los cuadros con alguna (sin el cuadro que sigue a una línea de la sonda).
    public sealed class FrameTimeProbe
    {
        public const string Tag = "PERF";
        public const double WindowSeconds = 10;
        public const int EntryFrames = 30;

        // It. 6a (06a-escena-lago/02-tech-design §4.5; AC-WKT-13-15 corregido; QA-G6aT-17): la entrada POR TIEMPO (los primeros 2,5 s
        // desde el toque: el vuelo entero, con la escena) y la salida (de 0 a 1 s desde la confirmación de Terminar), con la CPU del hilo
        // principal y la GPU si FrameTimingManager la da.
        public const double EntrySeconds = 2.5;
        public const double ExitSeconds = 1.0;

        // El negro de la entrada está entero desde los 220 ms (PageTransitionKind.Immerse).
        public const double BlackFromMs = 220;

        private double _entryStart = double.NaN;
        private double _entryCpu;
        private double _entryGpu;
        private double _exitStart = double.NaN;
        private double _exitCpu;
        private double _exitGpu;
        private bool _exitLogged;
        private double _windowGpu;
        private int _compilesAfterReady;
        private bool _markWritten;

        // Hasta 4096 muestras por ventana para el p99 (10 s a 400 fps); el máximo y la cuenta miran TODOS los cuadros.
        private const int Capacity = 4096;

        private readonly UiContext _ui;
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private readonly double[] _samples = new double[Capacity];
        private readonly double[] _sorted = new double[Capacity];
        private readonly Stopwatch _interval = new Stopwatch();
        private int _count;
        private int _total;
        private double _max;
        private double _windowStart = double.NaN;
        private bool _warned;
        private int _entrySamples;
        private double _entryMax;
        private int _entries;
        private bool _panelsInWindow;

        // QA-V6a-REN-02: el estado de los últimos cuadros (el momento de cada Sample, ms desde el toque, negro y quieto, recolecciones).
        private const int Ring = 64;
        private readonly double[] _ringSince = new double[Ring];
        private readonly bool[] _ringBlack = new bool[Ring];
        private readonly int[] _ringGc = new int[Ring];
        private int _ringCount;
        private bool _viewSeen;
        private int _entryGc;
        private int _entryGcStart;
        private double _entryGcFirst;
        private double _entryGcSecond;

        // QA-V6a-REN-03: las asignaciones por cuadro del hilo principal (en desarrollo).
        private ProfilerRecorder _allocations;

        // QA-V6a-REN-14: los dibujos y los SetPass del cuadro (la escena y la UI), el máximo de la ventana: si crecen de una sesión a otra.
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private long _windowBatches;
        private long _windowSetPass;
        private long _windowAllocMax;
        private int _windowAllocFrames;
        private bool _skipAllocSample;

        // QA-V6a-REN-02: el tiempo del hilo principal del cuadro ANTERIOR ("Main Thread", en desarrollo): con un cuadro de atraso exacto,
        // así el estado de ese cuadro (ms desde el toque, negro y quieto, GC) es el del Sample anterior. Para atribuir los picos.
        private ProfilerRecorder _mainThread;
        private double _entryMain;
        private double _entryMainSince = double.NaN;
        private bool _entryMainBlack;
        private int _entryMainGc;
        private double _entryMainMoving;
        private double _windowMain;
        private double _windowMainSince = double.NaN;
        private int _windowMainGc;
        private int _windowGcStart;

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
            UnityEngine.Rendering.GraphicsSettings.logWhenShaderIsCompiled = true;
            Application.logMessageReceived += OnLog;
            _allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        // Las subidas o compilaciones de shaders de la escena y de URP después de Ready que llegaron por logMessageReceived.
        public int SceneCompilesAfterReady => _compilesAfterReady;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_markWritten && ShaderWorkLog.IsSceneShaderWork(condition))
            {
                _compilesAfterReady++;
                // Sin repetir el texto de la línea (si no, esta misma línea volvería a contar).
                _ui.Logger.Log(LogLevel.Warning, Tag, "un shader de la escena o de URP subió después de Ready (n.º "
                    + _compilesAfterReady.ToString(CultureInfo.InvariantCulture) + ")");
            }
        }

        private ImmersiveWorkoutPage? _page;
        private bool _entryLogged;

        private void Sample()
        {
            if (!_markWritten && _ui.Scene != null && _ui.Scene.Readiness.Value == ImmersiveSceneReadiness.Ready)
            {
                _markWritten = true;
                Write(ShaderWorkLog.ReadyMark.Substring(Tag.Length + 3));
            }

            var top = _ui.Pages.Top as ImmersiveWorkoutPage;
            if (top != null && !top.IsExiting)
            {
                _page = top;
            }

            var now = Time.realtimeSinceStartupAsDouble;
            Remember(top);
            var sample = Capture(out var gpu, out var frameStart);

            // La salida: de 0 a 1 s desde la confirmación de Terminar (la página empieza a salir).
            if (_page != null && _page.IsExiting)
            {
                if (double.IsNaN(_exitStart))
                {
                    _exitStart = now;
                    _exitCpu = 0;
                    _exitGpu = 0;
                    _exitLogged = false;
                }

                if (now - _exitStart <= ExitSeconds)
                {
                    _exitCpu = Max(_exitCpu, sample);
                    _exitGpu = Max(_exitGpu, gpu);
                }
                else if (!_exitLogged)
                {
                    _exitLogged = true;
                    Write("salida del inmersivo: máx " + Ms(_exitCpu) + " ms de CPU, " + Ms(_exitGpu) + " ms de GPU en 0-1 s (n.º "
                        + _entries.ToString(CultureInfo.InvariantCulture) + ")");
                    _page = null;
                    _exitStart = double.NaN;
                }
            }

            if (top == null || top.IsExiting)
            {
                _count = 0;
                _total = 0;
                _max = 0;
                _windowGpu = 0;
                _windowStart = double.NaN;
                _interval.Reset();
                _entrySamples = -1;
                _panelsInWindow = false;
                _windowAllocMax = 0;
                _windowAllocFrames = 0;
                _windowBatches = 0;
                _windowSetPass = 0;
                return;
            }

            if (double.IsNaN(_windowStart))
            {
                _windowStart = now;
                _entrySamples = 0;
                _entryMax = 0;
                _entryStart = now;
                _entryCpu = 0;
                _entryGpu = 0;
                _entryLogged = false;
                _entries++;
                _entryGcStart = GC.CollectionCount(0);
                _entryGc = 0;
                _entryGcFirst = double.NaN;
                _entryGcSecond = double.NaN;
                _entryMain = 0;
                _entryMainSince = double.NaN;
                _entryMainBlack = false;
                _entryMainGc = 0;
                _entryMainMoving = 0;
                _windowMain = 0;
                _windowMainSince = double.NaN;
                _windowMainGc = 0;
                _windowGcStart = GC.CollectionCount(0);
            }

            // It. 5 (05-series-parciales/02-tech-design §3.7): la ventana en la que un panel de series estuvo abierto o se movió se
            // anota como "paneles" (la medida de AC-WKT-18-21 en el build, -rowmate-perf-series).
            var series = top.HasView ? top.View.Series : null;
            if (series != null && (series.Expanded.Laps || series.Expanded.Reps || series.IsAnimating))
            {
                _panelsInWindow = true;
            }

            SampleAllocations();
            SampleMainThread(now - _entryStart <= EntrySeconds);
            if (_batches.Valid)
            {
                _windowBatches = Math.Max(_windowBatches, _batches.LastValue);
                _windowSetPass = Math.Max(_windowSetPass, _setPass.LastValue);
            }

            if (!double.IsNaN(sample))
            {
                _total++;
                _max = Math.Max(_max, sample);
                _windowGpu = Max(_windowGpu, gpu);
                if (_count < Capacity)
                {
                    _samples[_count++] = sample;
                }

                if (_entrySamples >= 0 && _entrySamples < EntryFrames)
                {
                    _entryMax = Math.Max(_entryMax, sample);
                    if (++_entrySamples == EntryFrames)
                    {
                        Write("entrada del inmersivo: máx " + _entryMax.ToString("0.0", CultureInfo.InvariantCulture)
                            + " ms en los primeros " + EntryFrames.ToString(CultureInfo.InvariantCulture) + " cuadros (n.º "
                            + _entries.ToString(CultureInfo.InvariantCulture) + ")");
                    }
                }

                // It. 6a: la entrada por tiempo (2,5 s: el vuelo entero, con la escena). QA-V6a-R2-04: el cuadro de cada medida de
                // FrameTimingManager no se podía ubicar (su frameStartTimestamp tiene otro origen que el Stopwatch en este player: la sonda
                // escribía "sin el cuadro" en todas las entradas); el momento, si estaba negro y quieto y el GC salen del "Main Thread" del
                // ProfilerRecorder (un cuadro de atraso exacto), que es lo que se escribe.
                if (now - _entryStart <= EntrySeconds)
                {
                    _entryCpu = Math.Max(_entryCpu, sample);
                    _entryGpu = Max(_entryGpu, gpu);
                }
                else if (!_entryLogged)
                {
                    _entryLogged = true;
                    Write("entrada (2,5 s): máx " + Ms(_entryCpu) + " ms de CPU, " + Ms(_entryGpu) + " ms de GPU (n.º "
                        + _entries.ToString(CultureInfo.InvariantCulture) + "; escena " + (_ui.Scene == null ? "no" : _ui.Scene.Readiness.Value.ToString())
                        + "; shaders de la escena o de URP después de Ready: " + _compilesAfterReady.ToString(CultureInfo.InvariantCulture) + "); GC en la entrada: "
                        + _entryGc.ToString(CultureInfo.InvariantCulture) + Times() + "; hilo principal (Main Thread): máx " + Ms(_entryMain) + " ms ("
                        + Where(!double.IsNaN(_entryMainSince), _entryMainSince, _entryMainBlack, _entryMainGc) + "), con movimiento " + Ms(_entryMainMoving) + " ms");
                }
            }

            if (now - _entryStart <= EntrySeconds)
            {
                var gcNow = GC.CollectionCount(0) - _entryGcStart;
                if (gcNow > _entryGc)
                {
                    var at = (_ui.Motion.Now - top.OpenedAt) * 1000;
                    if (double.IsNaN(_entryGcFirst))
                    {
                        _entryGcFirst = at;
                    }
                    else if (double.IsNaN(_entryGcSecond))
                    {
                        _entryGcSecond = at;
                    }

                    _entryGc = gcNow;
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
            Write((_panelsInWindow ? "paneles: máx " : "cpu del hilo principal: máx ") + max.ToString("0.0", CultureInfo.InvariantCulture) + " ms, p99 "
                + p99.ToString("0.0", CultureInfo.InvariantCulture) + " ms, " + _total.ToString(CultureInfo.InvariantCulture) + " cuadros; GPU máx "
                + Ms(_windowGpu) + " ms; memoria " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)).ToString(CultureInfo.InvariantCulture)
                + " MB; asignaciones por cuadro: " + (_allocations.Valid
                    ? "máx " + _windowAllocMax.ToString(CultureInfo.InvariantCulture) + ", " + _windowAllocFrames.ToString(CultureInfo.InvariantCulture)
                      + " cuadros con alguna"
                    : "sin contador") + "; GC en la ventana " + (GC.CollectionCount(0) - _windowGcStart).ToString(CultureInfo.InvariantCulture)
                + "; peor del hilo principal " + Ms(_windowMain) + " ms (" + Where(!double.IsNaN(_windowMainSince), _windowMainSince, false, _windowMainGc) + "); dibujos máx " + _windowBatches.ToString(CultureInfo.InvariantCulture) + ", SetPass máx "
                + _windowSetPass.ToString(CultureInfo.InvariantCulture));
            _count = 0;
            _total = 0;
            _max = 0;
            _windowGpu = 0;
            _windowStart = now;
            _panelsInWindow = false;
            _windowAllocMax = 0;
            _windowAllocFrames = 0;
            _windowBatches = 0;
            _windowSetPass = 0;
            _windowMain = 0;
            _windowMainSince = double.NaN;
            _windowMainGc = 0;
            _windowGcStart = GC.CollectionCount(0);
        }

        // Lo que la sonda escribe asigna (el texto): el cuadro siguiente no se cuenta en las asignaciones.
        private void Write(string message)
        {
            _skipAllocSample = true;
            _ui.Logger.Log(LogLevel.Info, Tag, message);
        }

        private void SampleAllocations()
        {
            if (!_allocations.Valid)
            {
                return;
            }

            var value = _allocations.LastValue;
            if (_skipAllocSample)
            {
                _skipAllocSample = false;
                return;
            }

            if (value > 0)
            {
                _windowAllocFrames++;
                _windowAllocMax = Math.Max(_windowAllocMax, value);
            }
        }

        // El estado de este cuadro (QA-V6a-REN-02): el momento del Sample, ms desde el toque (NaN sin inmersivo), negro y quieto (desde los
        // 220 ms hasta el cuadro en que se arma el HUD, inclusive) y las recolecciones del GC hasta acá.
        private void Remember(ImmersiveWorkoutPage? page)
        {
            var i = _ringCount % Ring;
            _ringCount++;
            _ringGc[i] = GC.CollectionCount(0);
            if (page == null || page.IsExiting)
            {
                _ringSince[i] = double.NaN;
                _ringBlack[i] = false;
                _viewSeen = false;
                return;
            }

            var since = (_ui.Motion.Now - page.OpenedAt) * 1000;
            _ringSince[i] = since;
            var built = page.HasView && !_viewSeen;
            _ringBlack[i] = since >= BlackFromMs && (!page.HasView || built);
            _viewSeen |= page.HasView;
        }

        // El cuadro anterior (el del valor de "Main Thread"): su estado está en el Sample anterior del anillo.
        private void SampleMainThread(bool inEntry)
        {
            if (!_mainThread.Valid || _ringCount < 2)
            {
                return;
            }

            var ms = _mainThread.LastValue / 1e6;
            var i = (_ringCount - 2) % Ring;
            var since = _ringSince[i];
            var black = _ringBlack[i];
            var gc = _ringGc[i] - _ringGc[(_ringCount - 3 + Ring) % Ring];
            if (double.IsNaN(since))
            {
                return;
            }

            if (inEntry)
            {
                if (ms > _entryMain)
                {
                    _entryMain = ms;
                    _entryMainSince = since;
                    _entryMainBlack = black;
                    _entryMainGc = gc;
                }

                if (!black && ms > _entryMainMoving)
                {
                    _entryMainMoving = ms;
                }
            }

            if (ms > _windowMain)
            {
                _windowMain = ms;
                _windowMainSince = since;
                _windowMainGc = gc;
            }
        }

        private static string Where(bool mapped, double since, bool black, int gc)
        {
            if (!mapped || double.IsNaN(since))
            {
                return "sin el cuadro (otro reloj o fuera de la memoria)";
            }

            return "a los " + since.ToString("0", CultureInfo.InvariantCulture) + " ms del toque, " + (black ? "negro y quieto" : "con movimiento")
                   + ", GC +" + gc.ToString(CultureInfo.InvariantCulture);
        }

        private string Times()
        {
            if (double.IsNaN(_entryGcFirst))
            {
                return "";
            }

            return " (a los " + _entryGcFirst.ToString("0", CultureInfo.InvariantCulture)
                   + (double.IsNaN(_entryGcSecond) ? "" : " y " + _entryGcSecond.ToString("0", CultureInfo.InvariantCulture)) + " ms)";
        }

        // La CPU del hilo principal (ms), la GPU (ms, NaN si FrameTimingManager no la da) y el comienzo del cuadro medido (0 sin él).
        private double Capture(out double gpu, out ulong frameStart)
        {
            gpu = double.NaN;
            frameStart = 0;
            if (FrameTimingManager.IsFeatureEnabled())
            {
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
                {
                    gpu = _timing[0].gpuFrameTime > 0 ? _timing[0].gpuFrameTime : double.NaN;
                    frameStart = _timing[0].frameStartTimestamp;
                    return _timing[0].cpuMainThreadFrameTime;
                }

                return double.NaN;
            }

            if (!_warned)
            {
                _warned = true;
                Write("sin FrameTimingManager: NO VERIFICABLE (el intervalo entre cuadros, solo como información)");
            }

            var sample = _interval.IsRunning ? _interval.Elapsed.TotalMilliseconds : double.NaN;
            _interval.Restart();
            return sample;
        }

        private static double Max(double a, double b)
        {
            return double.IsNaN(b) ? a : Math.Max(a, b);
        }

        private static string Ms(double value)
        {
            return value > 0 ? value.ToString("0.0", CultureInfo.InvariantCulture) : "—";
        }
    }
}
