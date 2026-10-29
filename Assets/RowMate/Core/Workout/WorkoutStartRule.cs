using System;
using RowMate.Core.Models;
using RowMate.Core.Routines;

namespace RowMate.Core.Workout
{
    // R3-08 (03-rutinas/02-tech-design §8.2; 02-tech-design §5 de la It. 4a): una rutina que no se puede iniciar (AC-WKT-12-08,
    // AC-RUT-06-08, DV-28). Sin expandir y sin bucles por repetición: con ×2 000 000 000 decide en microsegundos y no reserva
    // memoria. Lo miran WorkoutStore al tocar la tarjeta (aviso, sin diálogo ni sesión) y WorkoutEngine al iniciar: Flatten
    // nunca recibe una rutina que no pasó la regla (spec §12 punto 5).
    public static class WorkoutStartRule
    {
        // La validación 3 del editor (AC-RUT-11-05: los topes de Q-92 por campo y por repetición, ×1000 o más fuera de rango,
        // ×0 y negativos sin controlar; un split ≤ 0 es "sin split") y que la rutina expandida tenga al menos un paso (Total
        // con saturación). Las dos en forma cerrada.
        public static bool CanStart(Routine template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return StepLimitCheck.TemplateIsWithinLimits(template.Steps) && RoutineStructure.Segments(template.Steps, 0).Total > 0;
        }
    }
}
