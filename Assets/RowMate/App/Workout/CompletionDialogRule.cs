using System;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // La regla del diálogo de rutina completada (F-WKT-21; 02-tech-design §2.6 de la It. 4b). El legacy lo abre "una vez" con
    // la fase terminada (immersive_workout_screen.dart:48-54), en un cuadro posterior; acá, a los 300 ms del primer estado
    // Finished + Completed (la página, con un Schedule del programador). Pura.
    public static class CompletionDialogRule
    {
        // AC-WKT-21-01, -07, AC-WKT-13-11: a lo sumo una vez por sesión (rotar, cambiar la ventana, minimizarla o pasar a segundo
        // plano no lo duplican; una sesión nueva muestra el suyo), solo si la rutina terminó sola (EndedBy == Completed). Nunca
        // con EndedBy == Manual (Q-53), en libre ni en reposo. shownForSession: la sesión de la última vez que se abrió (0 nunca).
        public static bool ShouldOpen(WorkoutHudState state, long shownForSession)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.Phase == WorkoutPhase.Finished && state.IsRoutine && state.EndedBy == WorkoutEndKind.Completed
                && state.Session > 0 && state.Session != shownForSession;
        }

        // AC-WKT-21-09: sin su entrada si el primer estado que lo pide es Instant (vuelta del segundo plano, despertar, cuadro
        // tarde): el diálogo ya está, con los totales finales.
        public static bool OpensInstantly(WorkoutHudState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.Instant;
        }
    }
}
