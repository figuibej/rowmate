namespace RowMate.App.Workout
{
    // Lo que pasa al tocar una tarjeta de rutina en Workout (02-tech-design §2.11 de la It. 4a): nada (sin remo o con una
    // sesión: AC-BLE-10-04, AC-WKT-01-10); el aviso de que no se puede iniciar, sin diálogo ni sesión (R3-08, AC-WKT-12-08;
    // P-09); o abrir el diálogo de inicio (AC-WKT-12-07). legacy: lib/features/workout/workout_screen.dart:118-120.
    public enum RoutineTapOutcome
    {
        Ignored,
        CannotStart,
        Confirm,
    }
}
