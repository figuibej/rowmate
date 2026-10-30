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

        // It. 4b (§3.11; DT4b-17; R4a-03): el peor caso de la 4b, en los cuatro perfiles: los tres objetivos fuera (tres flechas), BPM,
        // Próximo, la cuenta grande y el sello a la vista a la vez. Con las juntas M1, M2, M4 y M6 (y el sello en dos etiquetas, el
        // plan B de S-B4b) queda en 59 o menos; cuando falla, lista cada elemento visible.
        [UnityTest, Category("AC-WKT-17-01"), Category("AC-WKT-14-19")]
        public IEnumerator AtMostSixtyVisibleElementsInTheFourBWorstCase()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 })
            {
                CreateWorkout(RoutineSeeds.Budget, o =>
                {
                    o.Profile = profile;
                    o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                }, k => RowerScripts.R4(k).CopyWith(heartRate: 140));
                yield return StartRoutine("Presupuesto 4b");
                yield return AdvanceTo(5.5);
                yield return Settle();
                Assert.That(Hud.Spm.ArrowShown && Hud.Split.ArrowShown && Hud.Watts.ArrowShown, Is.True, profile.Code + ": las tres flechas");
                Assert.That(H.IsShown("hud-bpm"), Is.True, profile.Code + ": BPM");
                Assert.That(H.IsShown("hud-up-next"), Is.True, profile.Code + ": Próximo");
                Assert.That(H.IsShown("hud-big-countdown"), Is.True, profile.Code + ": la cuenta grande");
                Assert.That(Hud.Steps!.Seal.IsShowing, Is.True, profile.Code + ": el sello");
                var count = CountVisible(Immersive.Root);
                Assert.That(count, Is.LessThanOrEqualTo(MaxVisibleElements), profile.Code + ": " + count + " elementos: " + DescribeVisible(Immersive.Root));
            }
        }

        // §3.13 (QA-G4bT-11 (a), (c); QA-V4b-05): el trabajo del HUD (aplicar el estado y el generateVisualContent de la fila) con 200
        // pasos, a lo sumo 16 ms por cuadro, de los 26,2 a los 31,5 s de Serie larga: el halo de los últimos 5 s, la cuenta grande
        // (3, 2, 1), el cambio de paso a los 30 s con el pulgar que pasa a la 1 y el halo que se apaga. Serie larga no tiene objetivos
        // (no hay sello): el sello con todo lo demás es el peor caso de AtMostSixtyVisibleElementsInTheFourBWorstCase. Los 0 B por
        // cuadro, en NoAllocationsPerFrameWithTwoHundredPills.
        [UnityTest, Category("AC-WKT-14-19")]
        public IEnumerator LongSeriesStaysSmooth()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(26.2);
            var row = Hud.Steps!.PillRow;
            var worstApply = 0.0;
            var worstDraw = 0.0;
            var sawHalo = false;
            var sawCount = false;
            var sawChange = false;
            for (var i = 0; i < 318; i++)
            {
                H.Step();
                yield return null;
                worstApply = Math.Max(worstApply, Hud.LastApplyMs);
                worstDraw = Math.Max(worstDraw, row.LastDrawMs);
                sawHalo |= row.HaloAlpha > 0.2;
                sawCount |= H.IsShown("hud-big-countdown");
                sawChange |= row.LookAt(0) != RowMate.Core.Workout.StepPillLook.Current && row.LookAt(1) == RowMate.Core.Workout.StepPillLook.Current;
            }

            Assert.That(row.DrawCount, Is.GreaterThan(0), "la fila se dibujó");
            Assert.That(sawHalo && sawCount && sawChange, Is.True, "pasó por el halo (" + sawHalo + "), la cuenta grande (" + sawCount + ") y el cambio de paso (" + sawChange + ")");
            Assert.That(row.LookAt(1), Is.EqualTo(RowMate.Core.Workout.StepPillLook.Current), "el pulgar en la 1");
            Assert.That(worstApply + worstDraw, Is.LessThanOrEqualTo(16), "el trabajo del HUD: aplicar " + worstApply.ToString("0.00") + " ms, la fila " + worstDraw.ToString("0.00") + " ms");
        }

        // §3.13 (QA-G4bT-11 (c); QA-V4b-05): 0 B por cuadro (el Recorder de GC.Alloc) con 200 píldoras y el halo que late, y con la
        // fila sola de 1998 pasos (el tope del editor, una HudPillRow armada a mano con el pulgar en el medio y el halo, sin correr el
        // motor 1998 pasos): la tira por columnas de píxel cuesta lo mismo que con 200 (DT4b-21). En pausa y sin lecturas nada cambia
        // de valor; el halo sigue latiendo en pausa (AC-WKT-14-11).
        [UnityTest, Category("AC-WKT-14-19"), Category("AC-WKT-17-06")]
        public IEnumerator NoAllocationsPerFrameWithTwoHundredPills()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(27.1);
            yield return Tap("hud-pause");
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            var row = Hud.Steps!.PillRow;
            Assert.That(row.Count, Is.EqualTo(200));
            Assert.That(row.HaloAlpha, Is.GreaterThan(0.2), "el halo de la en curso");
            yield return MeasureAllocations("200 pasos");

            var types = new RowMate.Core.Models.StepType[1998];
            var looks = new RowMate.Core.Workout.StepPillLook[1998];
            for (var i = 0; i < types.Length; i++)
            {
                types[i] = i % 2 == 0 ? RowMate.Core.Models.StepType.Work : RowMate.Core.Models.StepType.Rest;
                looks[i] = i < 999 ? (i % 3 == 0 ? RowMate.Core.Workout.StepPillLook.Hollow : RowMate.Core.Workout.StepPillLook.Solid)
                    : i == 999 ? RowMate.Core.Workout.StepPillLook.Current : RowMate.Core.Workout.StepPillLook.Pending;
            }

            row.Apply(new RowMate.App.Workout.HudPillRow(types, looks, 999, 27, 30, false, true), false);
            var worstDraw = 0.0;
            var draws = row.DrawCount;
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
                worstDraw = Math.Max(worstDraw, row.LastDrawMs);
            }

            Assert.That(row.Count, Is.EqualTo(1998), "la fila de 1998");
            Assert.That(row.CurrentLayout.Strip && row.CurrentLayout.Narrow, Is.True, "la tira con el pulgar");
            Assert.That(row.DrawCount, Is.GreaterThan(draws), "se dibujó con el halo");
            Assert.That(worstDraw, Is.LessThanOrEqualTo(16), "la fila de 1998: " + worstDraw.ToString("0.00") + " ms");
            yield return MeasureAllocations("1998 pasos");
            Assert.That(row.Count, Is.EqualTo(1998), "siguió la fila de 1998 mientras se medía");
        }

        private IEnumerator MeasureAllocations(string label)
        {
            // Calentamiento de los caminos: unos cuadros antes de medir.
            for (var i = 0; i < 10; i++)
            {
                H.Step();
                yield return null;
            }

            var allocations = GcAllocations.During(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    H.Step();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), label + ": " + allocations + " asignaciones en 20 cuadros");
        }

        private static string DescribeVisible(VisualElement root)
        {
            var names = new System.Text.StringBuilder();
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (UiQuery.IsShown(element))
                {
                    names.Append(string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name).Append(", ");
                }
            }

            return names.ToString();
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
            // Con el Recorder de "GC.Alloc" (GcAllocations): en Unity GC.GetAllocatedBytesForCurrentThread da siempre 0 y esta
            // prueba no medía nada (04b-hud-exr, "Cambios por verificación (ronda 1)").
            var allocations = GcAllocations.During(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    H.Step();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), "0 B por cuadro del HUD (ARCHITECTURE §7.8): " + allocations + " asignaciones en 20 cuadros");
        }
    }
}
