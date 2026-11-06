using System;
using RowMate.Core.Workout;

namespace RowMate.Core.Scene
{
    // Las entradas de la escena en cada cuadro (scene_view.dart:65-70; immersive_workout_screen.dart:81-86; AC-SCN-03-07): el
    // split en segundos enteros y el SPM de la ÚLTIMA lectura (la misma que muestra el HUD) y "en marcha" = la fase del motor.
    // Con Simular desconexión la foto conserva la última lectura (como el proveedor del legacy); con Remando apagado el SPM
    // llega en 0 y el bote frena. La escena solo lee: nunca escribe en el motor (AC-WKT-13-14).
    public readonly struct SceneInputs
    {
        public SceneInputs(double pace500m, double spm, bool isActive)
        {
            Pace500m = pace500m;
            Spm = spm;
            IsActive = isActive;
        }

        public double Pace500m { get; }

        public double Spm { get; }

        public bool IsActive { get; }

        public static SceneInputs From(WorkoutSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new SceneInputs(snapshot.Data.Pace500mSeconds, snapshot.Data.StrokeRate, snapshot.Phase == WorkoutPhase.Active);
        }
    }
}
