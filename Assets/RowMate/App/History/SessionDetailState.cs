using System.Collections.Generic;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.App.History
{
    // Estado del detalle de una sesión (F-HIS-09 a -15; 02-tech-design §3.2): la fase, la sesión (la releída o la de la tarjeta),
    // las estadísticas de los puntos (el Resumen, AC-HIS-10-01), las filas del Rendimiento por paso y los datos del gráfico (con
    // Ready). Los pasos expandidos son los de la rutina ACTUAL (Q-16), vacíos si es libre o se borró.
    public sealed record SessionDetailState(SessionDetailPhase Phase, WorkoutSession Session, SessionStats Stats,
                                            IReadOnlyList<StepBreakdownRow> Steps, IReadOnlyList<IntervalStep> FlatSteps,
                                            SessionChartData? Chart)
    {
        // El Rendimiento por paso solo si algún punto tiene índice (_hasStepData, sd:145).
        public bool HasSteps => Steps.Count > 0;

        // La fila del Resumen: la misma de la tarjeta (AC-HIS-10-01).
        public IReadOnlyList<StatColumn> Columns => StatColumns.Build(Stats);

        // El título: el nombre de la rutina guardado en la sesión o workoutFree (no el "Libre" de la tarjeta, Q-09; sd:85-87).
        public string Title(Localizer l10n)
        {
            return SessionDetailTitle.For(Session, l10n);
        }
    }

    public static class SessionDetailTitle
    {
        public static string For(WorkoutSession session, Localizer l10n)
        {
            if (session == null)
            {
                throw new System.ArgumentNullException(nameof(session));
            }

            if (l10n == null)
            {
                throw new System.ArgumentNullException(nameof(l10n));
            }

            return session.RoutineName ?? l10n.Get(L10nKeys.WorkoutFree);
        }
    }
}
