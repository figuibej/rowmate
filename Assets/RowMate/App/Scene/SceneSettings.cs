using System;
using RowMate.Core.Common;

namespace RowMate.App.Scene
{
    // Ajustes de la escena. La hora forzada del modo simulador (F-SCN-23): la escribe el panel del simulador ("Hora escena",
    // AC-BLE-16-07) y, desde la It. 6a, la lee la escena en cada cuadro (SceneSession: SceneTickInputs.ForcedHour, con la
    // transición de 1,5 s de AC-SCN-23-02); la escena nunca escribe acá. El escenario elegido (scene.environment) es de la It. 8:
    // en la 6a la escena es siempre el lago y no lo lee ni lo escribe (AC-SCN-21-01).
    // El legacy la guarda como double?, pero siempre con horas enteras (DateTime.now().hour ± 1), así que int? es lo mismo.
    // No se persiste nada (AC-BLE-16-11, AC-SCN-23-03): al reiniciar vuelve a "real".
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
