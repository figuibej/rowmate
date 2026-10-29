namespace RowMate.Core.Workout
{
    // Lo que pasó al pedir un inicio (02-tech-design §2.10 de la It. 4a): empezó; el motor no estaba en reposo (un segundo
    // toque, AC-WKT-01-08, -10: ni sesión nueva ni cambio); o la rutina no se puede iniciar (R3-08, AC-WKT-12-08).
    public enum StartOutcome
    {
        Started,
        Busy,
        CannotStart,
    }
}
