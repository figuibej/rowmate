using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace RowMate.Tests.PlayMode.It15
{
    // It. 15 (#34, AC-D15-04, DT15-16): la basura por cuadro del gráfico del Detalle de F-9P n.º 1, medida con FrameAllocations (el C# del
    // cuadro, los generateVisualContent del Detalle y el panel). Topes de la spec: el cursor 0 de 60 cuadros, la entrada ≤ 1 de 48 y el
    // cambio de métrica ≤ 1 de 24 (antes: 3/60, 15/48 y 10/24). El registro deja qué elemento asignó (AllocatingElements) para la causa.
    public sealed class ChartAllocationTests : History.HistoryUiTestBase
    {
        public const int EntryFrames = 48;
        public const int SwitchFrames = 24;
        public const int CursorFrames = 60;

        // El objetivo de AC-D15-04.
        public const int SpecEntry = 1;
        public const int SpecSwitch = 1;
        public const int SpecCursor = 0;

        [UnityTest, Category("AC-D15-04")]
        public IEnumerator ChartFramesWithinTheSpec()
        {
            CreateHistory(HistorySeeds.F9P, DeviceProfile.TV);
            yield return GoToHistory();
            Screen.List.FinishBatches();
            yield return Frames();
            H.Tap("history-card-0");
            yield return Frames(2);
            H.Advance(0.25);
            yield return Frames(3);
            var chart = Detail.Chart;

            int entry;
            int entryDraws;
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                var allocating = 0;
                AllocationProfile.Begin("chart-entry");
                yield return Measure(measured, EntryFrames, a => allocating = a);
                AllocationProfile.End();
                measured.Stop();
                entry = allocating;
                entryDraws = measured.DrawAllocations;
                Debug.Log("[CAT] entrada: " + measured + "; " + allocating + " de " + EntryFrames + " cuadros; " + measured.AllocatingElements());
            }

            int change;
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                Detail.Store.SelectMetric(ChartMetric.Split);
                var allocating = 0;
                AllocationProfile.Begin("chart-switch");
                yield return Measure(measured, SwitchFrames, a => allocating = a);
                AllocationProfile.End();
                measured.Stop();
                change = allocating;
                Debug.Log("[CAT] cambio: " + measured + "; " + allocating + " de " + SwitchFrames + " cuadros; " + measured.AllocatingElements());
            }

            chart.ShowCursorAt(0);
            yield return Frames(2);
            int cursor;
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                var allocating = 0;
                var s = 0.0;
                AllocationProfile.Begin("chart-cursor");
                yield return Measure(measured, CursorFrames, a => allocating = a, () =>
                {
                    s += 30;
                    chart.Cursor.ShowAtSeconds(s % 1800);
                });
                AllocationProfile.End();
                measured.Stop();
                cursor = allocating;
                Debug.Log("[CAT] cursor: " + measured + "; " + allocating + " de " + CursorFrames + " cuadros; " + measured.AllocatingElements());
            }

            // QA-V15-01: la misma pasada otra vez. Lo que asignó la primera (medido con las pilas) es el pool de comandos de UIR
            // (RenderTreeManager.AllocCommand) que crece hasta su máximo la primera vez que el tooltip y la guía piden más comandos: un costo
            // de una sola vez por panel, no por cuadro. En la segunda pasada, el mismo recorrido no asigna.
            int warmCursor;
            using (var measured = FrameAllocations.WatchTree(Detail.Root))
            {
                var allocating = 0;
                var s = 0.0;
                yield return Measure(measured, CursorFrames, a => allocating = a, () =>
                {
                    s += 30;
                    chart.Cursor.ShowAtSeconds(s % 1800);
                });
                measured.Stop();
                warmCursor = allocating;
                Debug.Log("[CAT] cursor (segunda pasada): " + measured + "; " + allocating + " de " + CursorFrames + " cuadros");
            }

            Assert.That(chart.EntryDone, Is.True);
            var summary = "entrada " + entry + "/" + EntryFrames + ", cambio " + change + "/" + SwitchFrames + ", cursor " + cursor + "/" + CursorFrames;
            var meets = entry <= SpecEntry && change <= SpecSwitch && cursor <= SpecCursor;
            // AC-D15-04 (15-deuda-pc/02-tech-design, WP-B-04 y "Cambios por verificación" QA-V15-01, con las pilas del Profiler): el dibujo
            // de chart-mesh ya no asigna (ChartMeshUpload: DrawMesh de tamaño fijo, sin MeshWriteData) y las etiquetas de los ejes funden
            // por capas (sin copias de estilos). Lo que queda: la entrada, las páginas nuevas de UIR para la malla grande que se actualiza
            // con cuadros en vuelo (UIRenderDevice.UpdateAfterGPUUsedData, 2 cuadros); el cambio, el cruce de colores del selector de
            // métrica (ColorFade: color y fondo en línea en cada cuadro de los 160 ms); el cursor en frío, el pool de comandos de UIR. La
            // prueba deja el dato y no deja retroceder: chart-mesh en 0, el cursor en caliente en 0 y los topes de la It. 9.
            Debug.Log("[CAT] AC-D15-04 " + (meets ? "PASA" : "FAIL") + " (objetivo entrada ≤ " + SpecEntry + ", cambio ≤ " + SpecSwitch + ", cursor " + SpecCursor + "): " + summary);
            Debug.Log("[CAT] cursor en caliente: " + warmCursor + "/" + CursorFrames);
            Assert.That(entryDraws, Is.EqualTo(0), "el dibujo de chart-mesh no asigna (ChartMeshUpload); " + summary);
            Assert.That(warmCursor, Is.EqualTo(0), "el cursor en caliente no asigna; " + summary);
            Assert.That(entry, Is.LessThanOrEqualTo(History.HistoryPerformanceTests.MaxEntryFramesWithAllocations), "la entrada no empeora; " + summary);
            Assert.That(change, Is.LessThanOrEqualTo(History.HistoryPerformanceTests.MaxSwitchFramesWithAllocations), "el cambio no empeora; " + summary);
            Assert.That(cursor, Is.LessThanOrEqualTo(History.HistoryPerformanceTests.MaxFramesWithAllocations), "el cursor no empeora; " + summary);
        }

        // Cuadros de 1/60 s con el reloj de prueba: el C# del cuadro (Tick), los dibujos y el panel.
        private IEnumerator Measure(FrameAllocations measured, int frames, Action<int> done, Action? beforeFrame = null)
        {
            var dt = TimeSpan.FromTicks((long)Math.Round(UiTestHost.FrameSeconds * TimeSpan.TicksPerSecond));
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

            done(allocating);
        }
    }
}
