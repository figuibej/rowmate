using System;
using System.Collections;
using RowMate.App.Workout;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;

namespace RowMate.Tests.PlayMode
{
    // Base de las pruebas de Workout y del inmersivo (04a-motor-hud-funcional/02-tech-design §8.2): el remo de prueba con R-4
    // (150 W, 24 spm, split 125 s; L(k) en el instante k), el pulsómetro de prueba sin conectar, las rutinas de la semilla en
    // SQLite (las sesiones en el mismo archivo) o en memoria, y el reloj de prueba desde 2026-10-28T13:00:00Z. "A los N s" =
    // N s desde el toque que empezó la sesión.
    public abstract class WorkoutUiTestBase : UiTestBase
    {
        protected double SessionStart { get; private set; } = double.NaN;

        protected WorkoutScreenView Screen => H.WorkoutScreen ?? throw new InvalidOperationException("no hay pantalla Workout");

        protected ImmersiveWorkoutPage Immersive => H.Immersive ?? throw new InvalidOperationException("no hay inmersivo");

        protected HudView Hud => Immersive.View;

        protected WorkoutPhase Phase => H.App.Workout.State.Value.Phase;

        protected UiTestHost CreateWorkout(RoutineSeed? seed = null, Action<UiHostOptions>? configure = null, Func<int, RowingData>? reading = null)
        {
            var options = new UiHostOptions
            {
                Backend = HostBackends.TestRower(reading ?? RowerScripts.R4),
                Routines = seed != null ? HostRoutines.Seeded(seed) : HostRoutines.Empty,
                StartUtc = UiHostOptions.WorkoutStartUtc,
            };
            configure?.Invoke(options);
            return Create(options);
        }

        protected IEnumerator GoToWorkout()
        {
            if (H.App.Shell.SelectedIndex.Value != 1)
            {
                yield return Tap("tab-workout");
            }

            yield return Settle();
        }

        protected IEnumerator StartFree()
        {
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            SessionStart = H.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        protected IEnumerator OpenStartDialog(string routine)
        {
            yield return GoToWorkout();
            yield return Frames();
            H.TapRoutineNamed(routine);
            yield return Settle();
        }

        protected IEnumerator StartRoutine(string routine)
        {
            yield return OpenStartDialog(routine);
            yield return Frames();
            H.Tap("dialog-confirm");
            SessionStart = H.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        // Hasta N s desde el toque que empezó la sesión (nunca para atrás).
        protected IEnumerator AdvanceTo(double secondsSinceStart)
        {
            if (double.IsNaN(SessionStart))
            {
                throw new InvalidOperationException("AdvanceTo sin una sesión empezada");
            }

            var target = SessionStart + secondsSinceStart;
            var now = H.Scheduler.MonotonicSeconds;
            if (target > now + 1e-9)
            {
                H.Advance(target - now);
            }

            yield return Frames();
        }

        protected Rect Bounds(string name)
        {
            return H.BoundsOf(name);
        }

        // Responde a un toque: existe, está habilitado (SetEnabled) y el hit-test llega a él.
        protected bool Responds(string name)
        {
            var element = H.Find(name);
            return element != null && element.enabledInHierarchy && H.WhyNotTappable(name) == null;
        }

        protected static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        protected bool Inside(string name)
        {
            var window = H.Root.worldBound;
            var b = H.BoundsOf(name);
            return H.IsShown(name) && b.xMin >= window.xMin - 0.5f && b.yMin >= window.yMin - 0.5f && b.xMax <= window.xMax + 0.5f && b.yMax <= window.yMax + 0.5f;
        }

        protected static bool SameColor(Color color, RowMate.Core.Design.ColorToken token, int tolerance = 1)
        {
            var c = (Color32)color;
            var t = token.Rgb;
            return Math.Abs(c.r - t.R) <= tolerance && Math.Abs(c.g - t.G) <= tolerance && Math.Abs(c.b - t.B) <= tolerance;
        }

        protected int SessionCount()
        {
            var task = H.Sessions.ListAsync();
            Require(task.IsCompleted, "la lista de sesiones no volvió en el acto (ejecutor en línea)");
            return task.Result.Count;
        }

        protected SessionListing? LastSession()
        {
            var task = H.Sessions.ListAsync();
            Require(task.IsCompleted, "la lista de sesiones no volvió en el acto (ejecutor en línea)");
            return task.Result.Count > 0 ? task.Result[0] : null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
