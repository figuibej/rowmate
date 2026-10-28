using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Models;
using RowMate.Presentation.Routines;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace RowMate.Tests.PlayMode
{
    // PRF · Rendimiento con la base en su hilo (AC-HIS-17-06; 03-rutinas/02-tech-design §6.5, QA-G3-21, R3-09): el
    // SerialExecutor real (HostRoutines.Serial), F-LISTA + 50 rutinas de 20 pasos. No mide cuadros (en batchmode dependen del
    // equipo): mide con Stopwatch el tiempo de NUESTRO código en el hilo principal (armar la lista, abrir el editor, el Tick
    // del shell mientras la base guarda), descarta 3 repeticiones de calentamiento y lo compara con 16 ms (60 fps). El
    // máximo va al registro ([PRF] … ms) para el informe.
    public sealed class RoutinesPerformanceTests : RoutinesUiTestBase
    {
        private const double FrameBudgetMs = 16;
        private const int Warmup = 3;
        private const int Runs = 5;

        // Antes de cada medición: la basura que dejaron las pruebas anteriores no se cobra adentro de la nuestra (una
        // recolección a mitad no es tiempo de este código; con el recolector limpio, lo que asigna la lista sí cuenta).
        private static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private IEnumerator WaitReady(double timeoutSeconds)
        {
            var clock = Stopwatch.StartNew();
            while (H.App.Routines.State.Value.Phase != RoutinesPhase.Ready && clock.Elapsed.TotalSeconds < timeoutSeconds)
            {
                yield return null;
            }
        }

        [UnityTest, Category("AC-HIS-17-06")]
        public IEnumerator FiftyFourRoutinesUnderOneSecond()
        {
            var clock = Stopwatch.StartNew();
            Create(new UiHostOptions { Routines = HostRoutines.Serial(RoutineSeeds.Performance) });
            yield return WaitReady(5);
            yield return GoToRoutines();
            clock.Stop();
            Assert.That(H.App.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready));
            Assert.That(Screen.List.Count, Is.EqualTo(54));
            Debug.Log("[PRF] 54 rutinas a la vista en " + clock.Elapsed.TotalMilliseconds.ToString("0") + " ms (con el arranque del host)");
            Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(1.0), "la lista a la vista en menos de 1 s");
        }

        [UnityTest, Category("AC-HIS-17-06")]
        public IEnumerator MainThreadWorkStaysUnderTheFrameBudget()
        {
            Create(new UiHostOptions { Routines = HostRoutines.Serial(RoutineSeeds.Performance) });
            yield return WaitReady(5);
            yield return GoToRoutines();
            var ui = H.Shell.Ui;
            var routines = H.App.Routines.State.Value.Routines;
            Assert.That(routines.Count, Is.EqualTo(54));

            // 1. Armar la lista entera (las 54 tarjetas) como al cargar.
            var listMs = 0.0;
            for (var i = 0; i < Warmup + Runs; i++)
            {
                var list = new RoutineListView(ui, _ => { }, (_, __) => { });
                CollectGarbage();
                var sw = Stopwatch.StartNew();
                list.Render(routines, false);
                sw.Stop();
                if (i >= Warmup)
                {
                    listMs = Math.Max(listMs, sw.Elapsed.TotalMilliseconds);
                }
            }

            // 2. Abrir el editor de una rutina de 20 pasos (página, campos y filas).
            Routine big = routines[0];
            var editorMs = 0.0;
            for (var i = 0; i < Warmup + Runs; i++)
            {
                CollectGarbage();
                var sw = Stopwatch.StartNew();
                var page = new RoutineEditorPage(ui, H.Shell.Layout, big);
                sw.Stop();
                page.OnPopped();
                if (i >= Warmup)
                {
                    editorMs = Math.Max(editorMs, sw.Elapsed.TotalMilliseconds);
                }
            }

            // 3. El Tick del shell en cada cuadro mientras la base guarda en su hilo.
            CollectGarbage();
            var save = H.App.Routines.UpdateAsync(big with { Name = big.Name + " (2)" });
            var tickMs = 0.0;
            var frames = 0;
            while (!save.IsCompleted && frames < 300)
            {
                var sw = Stopwatch.StartNew();
                H.Handle.Tick();
                sw.Stop();
                if (frames >= Warmup)
                {
                    tickMs = Math.Max(tickMs, sw.Elapsed.TotalMilliseconds);
                }

                frames++;
                yield return null;
            }

            yield return Frames(3);
            Assert.That(save.IsCompleted, Is.True, "el guardado terminó");
            Debug.Log("[PRF] hilo principal: lista de 54 " + listMs.ToString("0.0") + " ms, editor de 20 pasos " + editorMs.ToString("0.0") + " ms, Tick durante el guardado "
                      + tickMs.ToString("0.00") + " ms (máximos, sin las 3 de calentamiento)");
            Assert.That(listMs, Is.LessThan(FrameBudgetMs), "armar la lista entra en un cuadro (si no: de a 8 tarjetas por cuadro, R3-09)");
            Assert.That(editorMs, Is.LessThan(FrameBudgetMs), "abrir el editor entra en un cuadro");
            Assert.That(tickMs, Is.LessThan(FrameBudgetMs), "el Tick no espera a la base");
        }
    }
}
