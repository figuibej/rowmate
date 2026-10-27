using UnityEngine.UIElements;

namespace RowMate.Simulator.Presentation
{
    // Cinta "SIMULADOR" (AC-BLE-16-01, DV-01; legacy Banner topStart, main.dart:108-110, simulator_overlay.dart:33-36):
    // 80 × 12 dp rotada −45° con centro en (24, 24) desde la esquina superior izquierda de la PANTALLA, sin insets propios.
    // Es el último hijo de la capa (por encima de la llave y del panel), siempre visible y sin eventos. Texto fijo en
    // español en los dos idiomas (Q-03), sin clase .rm-type-* (no escala con la letra del sistema).
    public sealed class SimulatorRibbon : VisualElement
    {
        public SimulatorRibbon()
        {
            name = "sim-ribbon";
            AddToClassList("rm-sim-ribbon");
            pickingMode = PickingMode.Ignore;
            Label = new Label(SimulatorTexts.Ribbon);
            Label.AddToClassList("rm-sim-ribbon__label");
            Label.pickingMode = PickingMode.Ignore;
            Add(Label);
        }

        public Label Label { get; }
    }
}
