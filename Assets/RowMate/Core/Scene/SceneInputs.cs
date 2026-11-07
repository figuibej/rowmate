using System;
using RowMate.Core.Scene.Stroke;
using RowMate.Core.Workout;

namespace RowMate.Core.Scene
{
    // Las entradas de la escena en cada cuadro (scene_view.dart:65-70; immersive_workout_screen.dart:81-86; AC-SCN-03-07): el
    // split en segundos enteros y el SPM de la ÚLTIMA lectura (la misma que muestra el HUD) y "en marcha" = la fase del motor.
    // Con Simular desconexión la foto conserva la última lectura (como el proveedor del legacy); con Remando apagado el SPM
    // llega en 0 y el bote frena. Desde la It. 7 también los vatios (el esfuerzo visual, AC-SCN-08-12) y el estado de las series
    // (la boya de 500 m, AC-SCN-08-10). La escena solo lee: nunca escribe en el motor (AC-WKT-13-14, AC-SCN-08-11).
    public readonly struct SceneInputs
    {
        public SceneInputs(double pace500m, double spm, bool isActive)
            : this(pace500m, spm, isActive, 0, BuoyInputs.None)
        {
        }

        public SceneInputs(double pace500m, double spm, bool isActive, int powerWatts, in BuoyInputs buoy)
        {
            Pace500m = pace500m;
            Spm = spm;
            IsActive = isActive;
            PowerWatts = powerWatts;
            Buoy = buoy;
        }

        public double Pace500m { get; }

        public double Spm { get; }

        public bool IsActive { get; }

        public int PowerWatts { get; }

        public BuoyInputs Buoy { get; }

        public static SceneInputs From(WorkoutSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var series = snapshot.Series;
            var buoy = series == null
                ? BuoyInputs.None
                : new BuoyInputs(series.IsWorkStep, series.StepIndex ?? -1, series.Laps.Count, series.StepMeters, series.Revision);
            return new SceneInputs(snapshot.Data.Pace500mSeconds, snapshot.Data.StrokeRate, snapshot.Phase == WorkoutPhase.Active,
                snapshot.Data.PowerWatts, buoy);
        }
    }
}
