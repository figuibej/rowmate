using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.App.Routines
{
    // Resultado de Confirmar en el diálogo de paso: el paso (y el diálogo se cierra) o los errores de tope, uno por campo como
    // mucho, en el orden de StepField (y el diálogo sigue abierto con todo lo escrito: AC-RUT-13-11).
    public sealed class StepDialogResult
    {
        private static readonly IReadOnlyList<FieldError> NoErrors = new FieldError[0];

        private StepDialogResult(IntervalStep? step, IReadOnlyList<FieldError> errors)
        {
            Step = step;
            Errors = errors;
        }

        public IntervalStep? Step { get; }

        public IReadOnlyList<FieldError> Errors { get; }

        public bool IsValid => Step != null;

        internal static StepDialogResult Valid(IntervalStep step)
        {
            return new StepDialogResult(step ?? throw new ArgumentNullException(nameof(step)), NoErrors);
        }

        internal static StepDialogResult Invalid(IReadOnlyList<FieldError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                throw new ArgumentException("StepDialogResult: un resultado inválido tiene al menos un error.", nameof(errors));
            }

            return new StepDialogResult(null, errors);
        }
    }
}
