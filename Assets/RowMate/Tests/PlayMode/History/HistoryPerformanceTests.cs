using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace RowMate.Tests.PlayMode.History
{
    // HPF · Presupuesto del historial y del gráfico (AC-HIS-19-05 como indicio en el Editor; 02-tech-design §6; A9-10). Las
    // asignaciones se miden EN CUADROS REALES con FrameAllocations (el C# del cuadro, los generateVisualContent del detalle y el
    // panel), nunca con GC.GetAllocatedBytesForCurrentThread. El [PERF] de verdad es el del build (Device-Win con seed F-9P).
    public sealed class HistoryPerformanceTests : HistoryUiTestBase
    {
        // Cuadros con alguna asignación tolerados por tramo: los del evento (el primer cuadro de la entrada o del cambio, una página
        // nueva de UIR), nunca uno por cuadro.
        public const int MaxFramesWithAllocations = 3;

        // La entrada (700 ms, una vez por apertura): UIR asigna en los primeros cuadros, mientras el marco y las etiquetas funden
        // (0-200 ms) y la malla crece (medido: ≈ 15 de 48). Es un evento, no un gasto continuo: el tope es ese tramo.
        public const int MaxEntryFramesWithAllocations = 16;

        // El cambio de métrica (300 ms): el panel asigna mientras las etiquetas Y funden (0-220; medido: 10 de 24). Evento, para issues.
        public const int MaxSwitchFramesWithAllocations = 12;

        [UnityTest, Category("AC-HIS-19-05")]
        public IEnumerator ChartFramesDoNotAllocate()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return Frames();
            H.Tap("history-card-4");
            yield return Frames(2);
            // El push termina (240) y la entrada arranca en ese instante (OnPushed).
            H.Advance(0.25);
            yield return Frames(3);
            var chart = Detail.Chart;

            // La entrada (≈ 42 cuadros con repintado).
            int entryFrames;
            int entryAllocating;
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                var frames = 0;
                var allocating = 0;
                yield return Measure(measured, 0.8, (f, a) =>
                {
                    frames = f;
                    allocating = a;
                });
                measured.Stop();
                entryFrames = frames;
                entryAllocating = allocating;
                Debug.Log("[HPF] entrada: " + measured + "; " + allocating + " de " + frames + " cuadros con asignaciones");
            }

            Assert.That(chart.EntryDone, Is.True);
            Assert.That(entryAllocating, Is.LessThanOrEqualTo(MaxEntryFramesWithAllocations), "la entrada no asigna por cuadro (" + entryFrames + " cuadros)");

            // El cambio de métrica (las 5 ya están preparadas: elegir otra no arma nada).
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                Detail.Store.SelectMetric(ChartMetric.Split);
                var allocating = 0;
                yield return Measure(measured, 0.4, (f, a) => allocating = a);
                measured.Stop();
                Debug.Log("[HPF] cambio: " + measured);
                Assert.That(allocating, Is.LessThanOrEqualTo(MaxSwitchFramesWithAllocations), "el cambio no asigna por cuadro: " + measured);
            }

            // El cursor recorriendo la curva (el tooltip por carácter, sin cadenas nuevas).
            chart.ShowCursorAt(0);
            yield return Frames(2);
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                var allocating = 0;
                var s = 0.0;
                yield return Measure(measured, 1.0, (f, a) => allocating = a, () =>
                {
                    s += 60;
                    chart.Cursor.ShowAtSeconds(s % 3700);
                });
                measured.Stop();
                Debug.Log("[HPF] cursor: " + measured);
                Assert.That(allocating, Is.LessThanOrEqualTo(MaxFramesWithAllocations), "el cursor no asigna por cuadro: " + measured);
            }
        }

        // El cuadro que escribe Core (WriteFrame) no asigna: lo que se mida en el dibujo es de la copia a UI Toolkit.
        [UnityTest, Category("AC-HIS-19-05")]
        public IEnumerator WriteFrameDoesNotAllocate()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return OpenCard(4);
            var geometry = Detail.Chart.Current!;
            var buffer = new ChartMeshBuffer(geometry.VertexBudget * 2, geometry.IndexBudget * 2);
            ChartGeometry.WriteFrame(buffer, geometry, null, ChartTimeline.Entry(300, geometry.YMax, false));
            var allocations = GcAllocations.During(() =>
            {
                for (var t = 0; t <= 700; t += 16)
                {
                    ChartGeometry.WriteFrame(buffer, geometry, null, ChartTimeline.Entry(t, geometry.YMax, false));
                }
            });
            Assert.That(allocations, Is.EqualTo(0), "WriteFrame en la entrada");
        }

        [UnityTest, Category("AC-HIS-19-05")]
        public IEnumerator PrepareAndGenerateBudget()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(4);
            var chart = Detail.Chart;
            Debug.Log("[HPF] 746 puntos: preparar " + chart.LastPrepareMs.ToString("0.000") + " ms, generar " + chart.LastGenerateMs.ToString("0.000")
                      + " ms, " + chart.LastVertexCount + " vértices");
            Assert.That(chart.LastPrepareMs, Is.LessThanOrEqualTo(10), "las 5 métricas con 746 puntos (≤ 2 ms por métrica en el escritorio; indicio)");
            Assert.That(chart.LastVertexCount, Is.LessThanOrEqualTo(9500), "W1280: ≤ 9 500 vértices");
        }

        [UnityTest, Category("AC-HIS-19-05")]
        public IEnumerator TwoHundredSessions()
        {
            CreateHistory(HistorySeeds.F9P, DeviceProfile.W1280);
            var watch = Stopwatch.StartNew();
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Frames(2);
            watch.Stop();
            Debug.Log("[HPF] F-9P: lista completa en " + watch.Elapsed.TotalMilliseconds.ToString("0") + " ms (Editor, con Settle)");
            Assert.That(Screen.List.Count, Is.EqualTo(200));
            Assert.That(Screen.List.BuiltCount, Is.EqualTo(200));
            var worst = 0.0;
            for (var i = 0; i < 3; i++)
            {
                var frame = Stopwatch.StartNew();
                H.Tap("tab-history");
                H.Step();
                yield return null;
                frame.Stop();
                worst = Math.Max(worst, frame.Elapsed.TotalMilliseconds);
            }

            Debug.Log("[HPF] F-9P: peor cuadro de recarga " + worst.ToString("0.0") + " ms (Editor)");
            Assert.That(H.IsShown("history-loading"), Is.False, "sin indicador al recargar");
        }

        // Cuadros de 1/60 s con el reloj de prueba durante `seconds`: el C# del cuadro (Tick), los dibujos y el panel.
        private IEnumerator Measure(FrameAllocations measured, double seconds, Action<int, int> done, Action? beforeFrame = null)
        {
            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
            var frames = (int)Math.Ceiling(seconds / UiTestHost.FrameSeconds);
            var allocating = 0;
            for (var i = 0; i < frames; i++)
            {
                var before = measured.Total;
                measured.Clock(() => H.Scheduler.Advance(dt));
                measured.Step(() =>
                {
                    beforeFrame?.Invoke();
                    H.Handle.Tick();
                });
                measured.PanelStep();
                yield return null;
                measured.Sample();
                if (measured.Total > before)
                {
                    allocating++;
                }
            }

            done(frames, allocating);
        }
    }
}
