namespace RowMate.Core.Scene.Water
{
    // El agua de la sesión de la APP (DT6b-01; water_shader.dart:12-22; DV-152): un equipo sin soporte, una falla de carga o de dibujo
    // y el último escalón térmico la dejan degradada hasta reiniciar, sin reintento. Cada Report* devuelve true solo la primera vez
    // que el agua cae (→ una sola línea del registro por sesión de la app, DT6b-13). Lo tiene SceneHost; vive más que las sesiones de
    // escena (salir y volver a entrar no reintenta).
    public sealed class WaterAvailability
    {
        public bool IsDegradedForApp { get; private set; }

        // El motivo de la primera caída (el de la línea del registro); null mientras no cayó.
        public string? Reason { get; private set; }

        public bool Unsupported { get; private set; }

        public bool Failed { get; private set; }

        public bool ThermalDegraded { get; private set; }

        public bool ReportUnsupported(string reason)
        {
            Unsupported = true;
            return Fall(reason ?? WaterModeSelector.UnsupportedReason);
        }

        public bool ReportFailure(string reason)
        {
            Failed = true;
            return Fall(reason ?? WaterModeSelector.FailureReason);
        }

        public bool ReportThermal()
        {
            ThermalDegraded = true;
            return Fall(WaterModeSelector.ThermalReason);
        }

        // Las entradas de la regla con lo pegado de la sesión de la app.
        public WaterSelectorInputs Inputs(QualityTier tier, WaterForce force, bool forceAllowed, bool thumbnail, bool advancedReady)
        {
            return new WaterSelectorInputs
            {
                Tier = tier,
                Supported = !Unsupported,
                Failed = Failed,
                ThermalDegraded = ThermalDegraded,
                Force = force,
                ForceAllowed = forceAllowed,
                Thumbnail = thumbnail,
                AdvancedReady = advancedReady,
                Reason = Reason,
            };
        }

        private bool Fall(string reason)
        {
            if (IsDegradedForApp)
            {
                return false;
            }

            IsDegradedForApp = true;
            Reason = reason;
            return true;
        }
    }
}
