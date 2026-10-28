using System;
using RowMate.Core.Text;

namespace RowMate.App.Routines
{
    // Un error de tope de Q-92 debajo de un campo del diálogo de paso. Repetition null: editorErrorRange (el valor del campo
    // se pasa: AC-RUT-13-11); desde 1: editorErrorRangeRep (la primera repetición de la serie en la que el objetivo
    // progresado se pasa, debajo de la PROGRESIÓN que lo causa: AC-RUT-13-12). MaxText es el {max}: "9999", "99:59:59"…
    public readonly struct FieldError : IEquatable<FieldError>
    {
        public FieldError(StepField field, int? repetition, string maxText)
        {
            Field = field;
            Repetition = repetition;
            MaxText = maxText ?? throw new ArgumentNullException(nameof(maxText));
        }

        public StepField Field { get; }

        public int? Repetition { get; }

        public string MaxText { get; }

        public bool Equals(FieldError other)
        {
            return Field == other.Field && Repetition == other.Repetition && string.Equals(MaxText, other.MaxText, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is FieldError other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Field, Repetition, MaxText);
        }

        public override string ToString()
        {
            return Field + (Repetition.HasValue ? " rep " + Fmt.Integer(Repetition.Value) : "") + " ±" + MaxText;
        }
    }
}
