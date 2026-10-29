using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HBT · El presupuesto del HUD (ARCHITECTURE §7.8; 04a-motor-hud-funcional/02-tech-design §6.13): en el peor caso (rutina
    // con objetivos en SPM y Watts, BPM, Próximo y el spinner del guardado) a lo sumo 60 elementos a la vista bajo #immersive,
    // y 0 B por cuadro mientras solo late (entre dos lecturas).
    public sealed class HudBudgetTests : WorkoutUiTestBase
    {
        public const int MaxVisibleElements = 60;

        private static int CountVisible(VisualElement root)
        {
            var count = 0;
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (UiQuery.IsShown(element))
                {
                    count++;
                }
            }

            return count;
        }

        [UnityTest, Category("AC-WKT-17-01")]
        public IEnumerator AtMostSixtyVisibleElementsInTheWorstCase()
        {
            CreateWorkout(RoutineSeeds.F4A, o => o.Sessions = HostSessions.Scripted(r => r.FinishDelay = TimeSpan.FromSeconds(5)),
                k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(80.05);
            yield return Settle();
            Assert.That(Hud.SpmHasTarget && Hud.WattsHasTarget, Is.True, "objetivos en SPM y Watts");
            Assert.That(H.IsShown("hud-bpm"), Is.True, "con BPM");
            Assert.That(H.IsShown("hud-up-next"), Is.True, "con Próximo");
            var routine = CountVisible(Immersive.Root);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Advance(0.4);
            yield return Frames(3);
            Assert.That(Hud.Controls.ShowsSpinner, Is.True, "con el spinner del guardado");
            var withSpinner = CountVisible(Immersive.Root);
            Assert.That(Math.Max(routine, withSpinner), Is.LessThanOrEqualTo(MaxVisibleElements),
                "elementos a la vista: " + routine + " con la rutina, " + withSpinner + " con el spinner");
        }

        [UnityTest, Category("AC-WKT-17-06")]
        public IEnumerator NoAllocationsPerFrameWhileItOnlyBeats()
        {
            CreateWorkout(RoutineSeeds.F4A, null, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(80.1);
            // En pausa y sin lecturas nada cambia de valor: solo laten el corazón y Próximo (que sigue en pausa, AC-WKT-14-06).
            yield return Tap("hud-pause");
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            Assert.That(Hud.Steps!.Pulse.IsPulsing, Is.True);
            // Calentamiento de los caminos: unos cuadros antes de medir.
            for (var i = 0; i < 10; i++)
            {
                H.Step();
            }

            yield return Frames();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 20; i++)
            {
                H.Step();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), "0 B por cuadro del HUD (ARCHITECTURE §7.8): " + allocated + " B en 20 cuadros");
        }
    }
}
