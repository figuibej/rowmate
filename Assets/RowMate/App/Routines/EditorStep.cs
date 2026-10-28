using System;
using RowMate.Core.Models;

namespace RowMate.App.Routines
{
    // Un paso de la plantilla del editor con una clave estable mientras dura el editor (no se guarda): la vista reusa la fila
    // por Key para animar altas, bajas y ediciones en el lugar (legacy: la ValueKey de cada fila,
    // lib/features/routines/routine_editor_screen.dart:320). Editar un paso conserva su clave; uno nuevo recibe otra.
    public readonly struct EditorStep : IEquatable<EditorStep>
    {
        public EditorStep(int key, IntervalStep step)
        {
            Key = key;
            Step = step ?? throw new ArgumentNullException(nameof(step));
        }

        public int Key { get; }

        public IntervalStep Step { get; }

        public bool Equals(EditorStep other)
        {
            return Key == other.Key && Equals(Step, other.Step);
        }

        public override bool Equals(object? obj)
        {
            return obj is EditorStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Step);
        }
    }
}
