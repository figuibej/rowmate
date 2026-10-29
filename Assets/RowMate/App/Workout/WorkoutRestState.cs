using System.Collections.Generic;
using RowMate.App.Routines;
using RowMate.Core.Models;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // La vista de Workout en reposo (F-WKT-12, F-BLE-10; 02-tech-design §2.11 de la It. 4a). RowerConnected = el remo en
    // Connected (cualquier otro de los cinco estados es "sin remo", también Off: AC-BLE-10-03). RoutinesPhase y Routines son
    // los de la colección compartida de Rutinas (AC-WKT-12-05: Workout no vuelve a leer la base). SessionPhase: fuera de
    // reposo la pestaña muestra solo "en curso" (AC-WKT-01-09).
    public sealed record WorkoutRestState(bool RowerConnected, RoutinesPhase RoutinesPhase, IReadOnlyList<Routine> Routines,
                                          WorkoutPhase SessionPhase)
    {
        public bool IsIdle => SessionPhase == WorkoutPhase.Idle;

        // Entrenamiento libre y las tarjetas responden (AC-WKT-12-03, AC-BLE-10-04).
        public bool CanStart => RowerConnected && IsIdle;
    }
}
