namespace RowMate.Simulator
{
    // Textos fijos de la capa del simulador, en español en los dos idiomas (Q-03, AC-BLE-16-04): no entran al catálogo
    // (Q-01, §12.9 de la spec). Los nombres "Remo simulado" y "Pulsómetro simulado" son constantes de los servicios.
    // legacy: lib/core/dev/simulator_overlay.dart:10-14, :34, :103-164
    public static class SimulatorTexts
    {
        public const string Ribbon = "SIMULADOR";                         // :34 (Banner)
        public const string Title = "Simulador";                          // :103
        public const string Watts = "Watts";                              // :126
        public const string Spm = "Ritmo";                                // :133
        public const string SceneHour = "Hora escena";                    // :140
        public const string RealHourValue = "real";                       // :141
        public const string RealHour = "Hora real";                       // :149
        public const string Rowing = "Remando";                           // :153
        public const string SimulateDisconnect = "Simular desconexión";   // :159
        public const string SimulateHeartRateDrop = "Simular caída del pulsómetro"; // :164
        public const string PresetSoft = "Suave";                         // :11
        public const string PresetMedium = "Medio";                       // :12
        public const string PresetHard = "Fuerte";                        // :13
        public const string WattsUnit = " W";                             // :127
        public const string SpmUnit = " spm";                             // :134
        public const string HourSuffix = ":00";                           // :141
    }
}
