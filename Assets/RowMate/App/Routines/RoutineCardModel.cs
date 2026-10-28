using System;
using System.Collections.Generic;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.App.Routines
{
    // Lo que dibuja la tarjeta de una rutina (AC-RUT-10-03, -10, -11; legacy: lib/features/routines/routines_screen.dart
    // :41-84), con los textos ya resueltos. La vista (RoutineCardView, WP-B) solo los pone en sus elementos:
    // - Name y Description tal cual se escribieron (sin marcas: la vista apaga el texto enriquecido y las secuencias de
    //   escape, DN3-22); ShowsDescription = descripción no vacía (:56-58, isNotEmpty: sin recortar).
    // - Summary = RoutineSummary.Text (la PLANTILLA: ×4 no lo cambia, AC-RUT-10-10).
    // - Template = los pasos para la mini-barra (RoutineStructure + StructureBarLayout con el ancho resuelto).
    public sealed record RoutineCardModel(long? Id, string Name, string Description, bool ShowsDescription, string Summary,
                                          IReadOnlyList<IntervalStep> Template)
    {
        public static RoutineCardModel For(Routine routine, Localizer l10n)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            return new RoutineCardModel(routine.Id, routine.Name, routine.Description, routine.Description.Length > 0,
                RoutineSummary.Text(routine, l10n), routine.Steps);
        }
    }
}
