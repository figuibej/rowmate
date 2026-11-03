using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Tests.PlayMode
{
    // 4BD · La deuda de la 4b que se salda al empezar la It. 5 (05-series-parciales/00-deuda-4b):
    // - QA-V4b-06 (§2): HudWarmup paga en el arranque la primera vez del HUD (JIT, estilos, texto) sin que se vea nada, sin
    //   dejar nada en el árbol ni en el MotionDriver y sin tocar el motor; el primer inmersivo después se abre igual.
    // - O-5 de la lente de motion (§4): si el diálogo de completado no se pudiera armar, la sesión no queda terminada para siempre.
    // - O-1 de la lente de motion (§4): los finitos de la 4a de la barra de pasos no siguen después de que la barra se va.
    public sealed class FourBDebtTests : WorkoutUiTestBase
    {
        // It. 5 (QA-G5T-05; 05-series-parciales/02-tech-design §2.7): con los paneles de series abiertos y con contenido, con su estado
        // propio en memoria: 0 escrituras en las preferencias de la app y el mismo SubscriberCount de su State antes y después.
        [UnityTest, Category("QA-V4b-06"), Category("AC-WKT-13-01"), Category("AC-WKT-13-16"), Category("AC-WKT-18-11")]
        public IEnumerator HudWarmupIsNotSeenAndLeavesNothing()
        {
            var preferences = new RecordingPreferences();
            CreateWorkout(RoutineSeeds.F4A, o => o.Preferences = preferences);
            var subscribers = H.App.SeriesPanels.SubscriberCount;
            yield return GoToWorkout();
            yield return Frames(4);
            var children = H.Root.childCount;
            var motions = new List<IMotion>();
            H.Handle.Motion.CopyMotions(motions);
            var demands = H.Handle.Pacing.Demands;
            var sessions = SessionCount();
            var before = Pixels();
            var warm = new HudWarmup(H.Shell.Ui, H.Root);
            warm.Start();
            Assert.That(warm.IsRunning, Is.True);
            Assert.That(H.Root[0].name, Is.EqualTo(HudWarmup.HolderName), "detrás de toda la app");
            Assert.That(H.Root.childCount, Is.EqualTo(children + 1));

            // El cuadro del panel: estilo, layout, texto y dibujo del HUD precalentado. No se ve nada.
            yield return Frames(2);
            var during = Pixels();
            Assert.That(Differences(before, during), Is.EqualTo(0), "lo que ve el usuario no cambia");

            Assert.That(warm.Tick(), Is.True, "con layout, termina en ese cuadro");
            Assert.That(warm.IsDone, Is.True);
            Assert.That(H.Root.Q(HudWarmup.HolderName), Is.Null, "sale del árbol");
            Assert.That(H.Root.childCount, Is.EqualTo(children));
            yield return Frames(2);
            var now = new List<IMotion>();
            H.Handle.Motion.CopyMotions(now);
            Assert.That(now, Is.EquivalentTo(motions), "nada queda animando");
            Assert.That(H.Handle.Pacing.Demands, Is.EqualTo(demands), "ni pidiendo 60 fps");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "el motor no se movió");
            Assert.That(SessionCount(), Is.EqualTo(sessions), "ninguna sesión");
            Assert.That(H.Logger.Contains("HUD precalentado (QA-V4b-06)"), Is.True, string.Join("\n", H.Logger.Lines));
            foreach (var line in H.Logger.Lines)
            {
                Assert.That(line, Does.Not.Contain("Warning:"), "ningún aviso (ni de DPI, ni del precalentado)");
            }

            Assert.That(preferences.WriteCount, Is.EqualTo(0), "el precalentado no escribe las preferencias (QA-G5T-05)");
            Assert.That(H.App.SeriesPanels.SubscriberCount, Is.EqualTo(subscribers), "ni queda suscripto a su State");
            Assert.That(Differences(before, Pixels()), Is.EqualTo(0), "y después tampoco");
            warm.Dispose();
        }

        [UnityTest, Category("QA-V4b-06"), Category("AC-WKT-13-01"), Category("AC-WKT-16-03")]
        public IEnumerator TheFirstImmersiveAfterTheWarmupOpensTheSame()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return GoToWorkout();
            yield return Frames(2);
            var warm = new HudWarmup(H.Shell.Ui, H.Root);
            warm.Start();
            yield return Frames(2);
            Assert.That(warm.Tick(), Is.True);
            yield return Frames(2);
            H.Tap("workout-free");
            Assert.That(H.Immersive, Is.Not.Null, "se abre en el acto (AC-WKT-13-01)");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
            yield return Settle();
            Assert.That(Hud.Free, Is.Not.Null);
            Assert.That(Hud.Free!.Chip.Label.text, Is.EqualTo(H.App.Texts.Value.Get(RowMate.Core.Localization.L10nKeys.HudPhaseActive)), "el chip de verdad");
            Assert.That(Hud.Band.Time.Text, Is.EqualTo(H.HudState!.Clock), "los valores son los del motor, no los de ejemplo del precalentado");
            AssertClean(H);
        }

        // O-5: el diálogo no se puede armar (una excepción): se anota y la sesión vuelve a reposo como con su Terminar; la página
        // sale como siempre y la sesión queda guardada.
        [UnityTest, Category("AC-WKT-21-06"), Category("AC-WKT-13-04"), Category("AC-WKT-13-16"), Category("QA-V4b-O-5")]
        public IEnumerator ACompletionDialogThatCannotOpenStillReturnsToRest()
        {
            CreateWorkout(RoutineSeeds.F4A);
            var sessions = SessionCount();
            yield return StartRoutine("Corta");
            var page = Immersive;
            page.CompletionFactoryForTest = _ => throw new InvalidOperationException("prueba O-5");
            LogAssert.Expect(LogType.Error, new Regex("no se pudo abrir el diálogo de completado: vuelve a reposo sin él \\(O-5\\)"));
            yield return AdvanceTo(30.5);
            yield return Settle();
            yield return Frames(2);
            yield return Settle();
            Assert.That(H.Completion, Is.Null, "sin diálogo");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "la sesión no queda terminada para siempre");
            Assert.That(H.Shell.Pages.HasPage(page), Is.False, "la página salió");
            Assert.That(SessionCount(), Is.EqualTo(sessions + 1), "la sesión quedó guardada");
        }

        // O-1: la barra de pasos que se va con el ancho de Próximo creciendo (y su entrada) no deja ningún finito en el driver.
        [UnityTest, Category("AC-WKT-13-09"), Category("QA-V4b-O-1")]
        public IEnumerator TheStepBarLeavesWithoutItsFiniteMotions()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return GoToWorkout();
            yield return Frames(2);
            var baseline = new List<IMotion>();
            H.Handle.Motion.CopyMotions(baseline);
            var store = H.App.CreateHud();
            var view = new HudView(H.Shell.Ui, true, store.WallClock, () => { }, () => { }, () => { });
            view.style.position = UnityEngine.UIElements.Position.Absolute;
            view.style.left = 0;
            view.style.top = 0;
            view.style.right = 0;
            view.style.bottom = 0;
            H.Root.Add(view);
            var running = HudWarmup.Running(store.State.Value, true, H.App.Texts.Value);
            view.Apply(running with { Step = running.Step! with { UpNextType = null, UpNextTypeText = null } }, 1);
            yield return Frames(2);
            Assert.That(H.Handle.Motion.Armed, Is.True);
            view.Apply(running with { Instant = false, Revision = running.Revision + 1 }, 1);
            var during = new List<IMotion>();
            H.Handle.Motion.CopyMotions(during);
            var spring = during.Find(m => m is SpringMotion && !baseline.Contains(m));
            Assert.That(spring, Is.Not.Null, "Próximo entra creciendo (el resorte del ancho corre)");

            view.Dispose();
            var after = new List<IMotion>();
            H.Handle.Motion.CopyMotions(after);
            foreach (var motion in after)
            {
                Assert.That(baseline.Contains(motion), Is.True, "queda " + motion.GetType().Name + " después de irse");
            }

            view.RemoveFromHierarchy();
            store.Dispose();
            yield return Frames(2);
        }

        private Color32[] Pixels()
        {
            var frame = RtReader.Read(H.Target);
            try
            {
                return frame.GetPixels32();
            }
            finally
            {
                Object.Destroy(frame);
            }
        }

        private static int Differences(Color32[] a, Color32[] b)
        {
            if (a.Length != b.Length)
            {
                return int.MaxValue;
            }

            var count = 0;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
