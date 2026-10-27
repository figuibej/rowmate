using RowMate.Core.Text;

namespace RowMate.Simulator.Panel
{
    // Estado del panel del simulador (AC-BLE-16-04 a -08): abierto o cerrado, objetivos, hora forzada y Remando. Los textos
    // de los valores son los del legacy, con Fmt (sin la cultura del equipo).
    // legacy: lib/core/dev/simulator_overlay.dart:29, :115-156
    public sealed record SimulatorPanelState(bool IsOpen, int TargetWatts, int TargetSpm, int? HourOverride, bool Rowing)
    {
        // :127: '${sim.targetWatts} W'
        public string WattsText => Fmt.Integer(TargetWatts) + SimulatorTexts.WattsUnit;

        // :134: '${sim.targetSpm} spm'
        public string SpmText => Fmt.Integer(TargetSpm) + SimulatorTexts.SpmUnit;

        // :141: hour == null ? 'real' : '${hour.round() % 24}:00' (sin cero a la izquierda: "0:00", "14:00").
        public string HourText => HourOverride == null
            ? SimulatorTexts.RealHourValue
            : Fmt.Integer(DartMath.Mod(HourOverride.Value, 24)) + SimulatorTexts.HourSuffix;

        // :146: el botón "Hora real" solo con la hora forzada.
        public bool ShowRealHourButton => HourOverride != null;
    }
}
