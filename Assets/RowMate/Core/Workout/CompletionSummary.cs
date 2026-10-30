using RowMate.Core.Text;

namespace RowMate.Core.Workout
{
    // Los totales del diálogo de rutina completada (AC-WKT-21-04; DV-43; 02-tech-design §2.6 de la It. 4b): el reloj activo al
    // terminar y la distancia y las kcal del punto final (workout_provider.dart:259-273), fijos aunque lleguen lecturas o se
    // rote la pantalla. El legacy los lee un cuadro después del fin (immersive_workout_screen.dart:965-991).
    public readonly struct CompletionSummary
    {
        public CompletionSummary(int activeSeconds, int distanceMeters, int calories)
        {
            ActiveSeconds = activeSeconds;
            DistanceMeters = distanceMeters;
            Calories = calories;
        }

        public int ActiveSeconds { get; }

        public int DistanceMeters { get; }

        public int Calories { get; }

        public override string ToString()
        {
            return Fmt.Integer(ActiveSeconds) + " s / " + Fmt.Integer(DistanceMeters) + " m / " + Fmt.Integer(Calories) + " kcal";
        }
    }
}
