namespace RowMate.Core.Models
{
    // Meta de un paso (AC-RUT-02-02; legacy: interval_step.dart:64-77): por tiempo si tiene duración (aunque también
    // tenga distancia), por distancia si solo tiene distancia, sin meta si no tiene ninguna.
    public enum StepGoal
    {
        Time,
        Distance,
        None,
    }
}
