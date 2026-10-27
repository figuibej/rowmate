namespace RowMate.Simulator.Panel
{
    // Preset de intensidad del panel del simulador: nombre, vatios y ritmo objetivo (AC-BLE-16-05).
    // legacy: lib/core/dev/simulator_overlay.dart:10-14 (_presets)
    public sealed record SimulatorPreset(string Name, int Watts, int Spm)
    {
        // Para el nombre de contrato del chip ("sim-preset-suave").
        public string ElementName => "sim-preset-" + Name.ToLowerInvariant();
    }
}
