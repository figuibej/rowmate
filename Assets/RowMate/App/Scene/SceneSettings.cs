using System;
using RowMate.Core.Common;

namespace RowMate.App.Scene
{
    // Ajustes de la escena. En la It. 2 solo la hora forzada del modo simulador (F-SCN-23): la escribe el panel del
    // simulador ("Hora escena", AC-BLE-16-07) y nadie la lee hasta la It. 6a. El escenario elegido (persistido) llega en la 4a.
    // El legacy la guarda como double?, pero siempre con horas enteras (DateTime.now().hour ± 1), así que int? es lo mismo.
    // No se persiste nada (AC-BLE-16-11): al reiniciar vuelve a "real".
    // legacy: lib/features/workout/scene/scene_settings.dart:14-24
    public sealed class SceneSettings
    {
        public SceneSettings(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            HourOverride = new ObservableValue<int?>(null, logger);
        }

        // null = la hora real; 0-23 = la hora forzada.
        public ObservableValue<int?> HourOverride { get; }
    }
}
