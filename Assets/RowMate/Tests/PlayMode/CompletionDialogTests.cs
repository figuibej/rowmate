using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Workout;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // CDT · El diálogo de rutina completada (F-WKT-21; AC-WKT-21-01 a -12; AC-WKT-13-11, AC-WKT-20-07; AC-APP-03-10, -11;
    // AC-APP-05-05, -07; 04b-hud-exr/02-tech-design §2.6, §3.9): a los 300 ms del fin, una vez por sesión, nunca a mano; no
    // descartable; los totales fijos del punto final; su Terminar vuelve a reposo y la página sale.
    public sealed class CompletionDialogTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-21-01"), Category("AC-WKT-21-03"), Category("AC-WKT-21-05"), Category("AC-WKT-21-10"), Category("AC-WKT-03-05")]
        public IEnumerator AppearsThreeHundredMsAfterTheEndWithItsTotals()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(30.2);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished), "la rutina terminó sola y espera");
            Assert.That(H.Completion, Is.Null, "todavía no (300 ms)");
            yield return AdvanceTo(30.35);
            var dialog = H.Completion;
            Assert.That(dialog, Is.Not.Null, "a los 300 ms del fin");
            Assert.That(dialog!.Name, Is.EqualTo(CompletionDialog.DialogName));
            yield return Settle();
            Assert.That(dialog.HeaderTitle.text, Is.EqualTo("¡Entrenamiento completado!"));
            Assert.That(dialog.Message.text, Is.EqualTo("Has completado tu rutina exitosamente."));
            Assert.That(dialog.TimeValue.Text, Is.EqualTo("00:30"), "el conteo termina en el valor exacto");
            Assert.That(dialog.DistanceValue.Text, Is.EqualTo("1120"));
            Assert.That(dialog.DistanceValue.SuffixText, Is.EqualTo("m"), "la m de sufijo");
            Assert.That(dialog.CaloriesValue.Text, Is.EqualTo("43"));
            Assert.That(dialog.Check.Stroke, Is.EqualTo(1).Within(1e-4), "el check dibujado entero");
            Assert.That(H.TextOf("completion-finish"), Is.EqualTo("Terminar"));
            Assert.That(dialog.Actions.childCount, Is.EqualTo(1), "un solo botón, sin Strava (AC-WKT-21-10)");
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sin el aviso workoutCompleted (AC-WKT-03-05)");
        }

        // QA-V4b-MOT-01 (§3.11, §7.4: 0 B por cuadro): el conteo de los totales cambia de valor en cada cuadro y no asigna memoria
        // (antes formateaba tres cadenas por cuadro: 94 B con Corta). Sin lecturas nuevas (el remo desconectado) nada más cambia.
        [UnityTest, Category("AC-WKT-21-05")]
        public IEnumerator CountUpDoesNotAllocatePerFrame()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(30.1);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished));
            H.ScriptedRower!.Disconnect();
            yield return AdvanceTo(30.35);
            var dialog = H.Completion;
            Assert.That(dialog, Is.Not.Null, "a los 300 ms del fin");
            // 10 cuadros de calentamiento (167 ms): los totales empiezan a contar a los 220, 340 y 460 ms.
            for (var i = 0; i < 10; i++)
            {
                H.Step();
            }

            var shown = dialog!.DistanceValue.Value;
            // Con el Recorder de "GC.Alloc" (GcAllocations): en Unity GC.GetAllocatedBytesForCurrentThread da siempre 0.
            var allocations = GcAllocations.During(() =>
            {
                for (var i = 0; i < 30; i++)
                {
                    H.Step();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), "0 B por cuadro durante el conteo: " + allocations + " asignaciones en 30 cuadros");
            Assert.That(dialog.Motion.IsRunning, Is.True, "sigue contando (hasta los 1160 ms)");
            Assert.That(dialog.DistanceValue.Value, Is.GreaterThan(shown), "la distancia contó en esos cuadros");
            yield return Settle();
            Assert.That(dialog.TimeValue.Text, Is.EqualTo("00:30"), "el conteo termina en el valor exacto");
            Assert.That(dialog.DistanceValue.Text, Is.EqualTo("1120"));
            Assert.That(dialog.CaloriesValue.Text, Is.EqualTo("43"));
        }

        [UnityTest, Category("AC-WKT-21-02"), Category("AC-APP-03-10"), Category("AC-WKT-13-03")]
        public IEnumerator ModalScrimEscapeDoNothing()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(30.4);
            yield return Settle();
            var dialog = H.Completion!;
            H.TapScrim();
            yield return Frames();
            Assert.That(dialog.IsOpen, Is.True, "tocar el velo no hace nada");
            H.PressBack(BackSource.Escape);
            yield return Frames();
            Assert.That(dialog.IsOpen, Is.True, "Escape no hace nada");
            H.PressBack(BackSource.SystemBack);
            yield return Frames();
            Assert.That(dialog.IsOpen, Is.True, "Atrás no hace nada");
            Assert.That(H.Immersive!.FinishDialog, Is.Null, "y no llega al HUD (no abre la confirmación de terminar)");
        }

        [UnityTest, Category("AC-WKT-21-04"), Category("AC-WKT-21-08")]
        public IEnumerator TotalsAreFixedAndTheHudStaysLiveBehind()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            var dialog = H.Completion!;
            var distance = dialog.DistanceValue.Text;
            yield return Advance(3);
            Assert.That(dialog.DistanceValue.Text, Is.EqualTo(distance), "fijos aunque lleguen lecturas (DV-43)");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:30"), "el reloj quieto en el fin");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished), "terminada hasta Terminar");
        }

        [UnityTest, Category("AC-WKT-21-06"), Category("AC-WKT-13-04")]
        public IEnumerator TerminarReturnsToRestAndThePageLeaves()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            yield return Tap("completion-finish");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "el cierre ya volvió: a reposo en el acto");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "la página salió");
            Assert.That(H.TopDialog, Is.Null, "ningún diálogo sobre Workout en reposo");
            Assert.That(Screen.IsOnScreen, Is.True);
        }

        [UnityTest, Category("AC-WKT-21-06"), Category("AC-WKT-06-12")]
        public IEnumerator FailedCloseShowsSaveErrorAfterTerminar()
        {
            CreateWorkout(RoutineSeeds.F4A, o => o.Sessions = HostSessions.Scripted(r => r.FailOnFinish = true));
            yield return StartRoutine("Corta");
            yield return AdvanceTo(32);
            yield return Settle();
            Assert.That(H.Completion, Is.Not.Null, "el diálogo aunque el cierre haya fallado");
            yield return Tap("completion-finish");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la sesión"), "el aviso del cierre que falló, en Workout");
        }

        [UnityTest, Category("AC-WKT-21-07"), Category("AC-WKT-13-11")]
        public IEnumerator NeverOnManualFinish()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(12);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Completion, Is.Null, "a mano nunca hay diálogo de completado");
            Assert.That(H.Immersive, Is.Null, "vuelve solo a reposo (AC-WKT-01-06)");
        }

        [UnityTest, Category("AC-WKT-21-11"), Category("AC-APP-05-07")]
        public IEnumerator FirstTabWithoutFocusReachesTheDialog()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            var dialog = H.Completion!;
            Assert.That(H.Focused, Is.Null, "al aparecer, nada enfocado");
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(dialog.FinishButton), "el primer Tab va a Terminar");
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(dialog.FinishButton), "Tab no sale del diálogo");
        }

        [UnityTest, Category("AC-WKT-21-12"), Category("AC-APP-05-05")]
        public IEnumerator SizesAndLetter()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            var box = Bounds("completion-dialog");
            var window = H.Root.worldBound;
            Assert.That(box.xMin - window.xMin, Is.EqualTo(40f).Within(1f), "TV: 40,0-371,0 (min(400; ventana − 80))");
            Assert.That(box.xMax - window.xMin, Is.EqualTo(371f).Within(1f));
            Assert.That(box.yMax, Is.LessThanOrEqualTo(window.yMax), "entero en la ventana");
            // Maratón con los extremos (QA-G4bT-11 (d)): el diálogo con su HudCompletion, sin correr 10 h.
            var maraton = H.OpenCompletion(new HudCompletion("9:59:59", 35999, "16777215 m", 16777215, "65535", 65535));
            yield return Settle();
            Assert.That(maraton.TimeValue.Text, Is.EqualTo("9:59:59"));
            Assert.That(maraton.DistanceValue.Text, Is.EqualTo("16777215"));
            Assert.That(maraton.DistanceValue.worldBound.xMax, Is.LessThanOrEqualTo(maraton.Box.worldBound.xMax + 0.5f), "los extremos entran");
        }
    }
}
