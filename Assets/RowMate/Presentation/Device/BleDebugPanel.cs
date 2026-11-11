using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // El panel DEBUG BLE de la vista conectada (AC-BLE-12-02; device_screen.dart:142-218; 00-arte §5.2), fijo en inglés (Q-04):
    //   #ble-debug-panel .rm-ble-debug (dev.bleDebugBg, radio 12, borde 1)
    //   ├── .rm-ble-debug__header: bug_report 14, "DEBUG BLE" (label 12/600, tracking 0,08 em) y la insignia: #ble-debug-dot (punto de 6
    //   │   que late con cada paquete) y #ble-debug-badge (RECEIVING en state.connected sobre connectedBg / NO DATA en dangerText)
    //   ├── renglones Notifications (#ble-debug-count) y Last received (#ble-debug-last): etiqueta bodyXs en text.tertiary, valor en mono
    //   ├── separador · "Raw bytes (hex):" (caption) y #ble-debug-hex (mono 12/18 en dev.bleDebug; corta en los espacios, 2 líneas mínimo)
    //   └── separador · Parsed SPM / Watts / Distance / Split (valores en mono, D10-13)
    // Sin animación de texto: cambia una vez por paquete (A10-11; el hex lo arma DeviceStore una vez por paquete). El único movimiento es el
    // punto de RECEIVING: opacidad 1 → 0,35 → 1 en 300 (ease.pulse) con cada paquete; con reducir movimiento queda fijo. ≈ 23 elementos.
    public sealed class BleDebugPanel : VisualElement
    {
        private readonly DotPulse? _pulse;
        private string _count = "";

        public BleDebugPanel(MotionDriver? motion = null)
        {
            name = "ble-debug-panel";
            AddToClassList("rm-ble-debug");
            // It. 15 (#79, AC-D15-26, DV-147): diagnóstico Bluetooth (en inglés, Q-04), fuera de la auditoría de textos del catálogo.
            AddToClassList("rm-audit-exempt");
            pickingMode = PickingMode.Ignore;

            var header = Row("rm-ble-debug__header");
            var icon = new IconView(Icons.BugReport, false, 14);
            icon.AddToClassList("rm-ble-debug__icon");
            header.Add(icon);
            var title = Text("rm-type-label", "rm-ble-debug__title");
            title.text = BleDebugSpec.Title;
            header.Add(title);
            var badge = Row("rm-ble-debug__pill");
            Dot = new VisualElement { name = "ble-debug-dot" };
            Dot.AddToClassList("rm-ble-debug__dot");
            Dot.pickingMode = PickingMode.Ignore;
            badge.Add(Dot);
            Badge = Text("rm-type-caption", "rm-ble-debug__badge");
            Badge.name = "ble-debug-badge";
            badge.Add(Badge);
            header.Add(badge);
            Add(header);
            if (motion != null)
            {
                _pulse = new DotPulse(motion, Dot);
            }

            Count = AddPair(BleDebugSpec.NotificationsLabel, "ble-debug-count");
            LastReceived = AddPair(BleDebugSpec.LastReceivedLabel, "ble-debug-last");
            Add(Separator());
            var rawLabel = Text("rm-type-caption", "rm-ble-debug__label");
            rawLabel.AddToClassList("rm-ble-debug__raw-label");
            rawLabel.text = BleDebugSpec.RawBytesLabel;
            Add(rawLabel);
            Hex = Text("rm-type-mono", "rm-ble-debug__hex");
            Hex.name = "ble-debug-hex";
            Add(Hex);
            Add(Separator());
            Spm = AddPair(BleDebugSpec.ParsedSpmLabel, "ble-debug-spm");
            Watts = AddPair(BleDebugSpec.ParsedWattsLabel, "ble-debug-watts");
            Distance = AddPair(BleDebugSpec.ParsedDistanceLabel, "ble-debug-distance");
            Split = AddPair(BleDebugSpec.ParsedSplitLabel, "ble-debug-split");
        }

        public Label Badge { get; }

        public VisualElement Dot { get; }

        public Label Count { get; }

        public Label LastReceived { get; }

        public Label Hex { get; }

        public Label Spm { get; }

        public Label Watts { get; }

        public Label Distance { get; }

        public Label Split { get; }

        public void Render(BleDebugSpec spec)
        {
            Set(Badge, spec.Badge);
            EnableInClassList("rm-ble-debug--receiving", spec.Receiving);
            Badge.EnableInClassList("rm-ble-debug__badge--receiving", spec.Receiving);
            Dot.style.display = spec.Receiving ? DisplayStyle.Flex : DisplayStyle.None;
            // Un paquete nuevo = el contador cambió: el punto late una vez.
            if (spec.Receiving && _count.Length > 0 && spec.Notifications != _count)
            {
                _pulse?.Beat();
            }

            _count = spec.Notifications;
            Set(Count, spec.Notifications);
            Set(LastReceived, spec.LastReceived);
            Set(Hex, spec.RawHex);
            Set(Spm, spec.ParsedSpm);
            Set(Watts, spec.ParsedWatts);
            Set(Distance, spec.ParsedDistance);
            Set(Split, spec.ParsedSplit);
        }

        private Label AddPair(string label, string valueName)
        {
            var row = Row("rm-ble-debug__row");
            var name = Text("rm-type-body-xs", "rm-ble-debug__label");
            name.text = label;
            row.Add(name);
            var value = Text("rm-type-mono", "rm-ble-debug__value");
            value.name = valueName;
            row.Add(value);
            Add(row);
            return value;
        }

        private static VisualElement Row(string className)
        {
            var row = new VisualElement();
            row.AddToClassList(className);
            row.pickingMode = PickingMode.Ignore;
            return row;
        }

        private static VisualElement Separator()
        {
            var line = new VisualElement();
            line.AddToClassList("rm-ble-debug__separator");
            line.pickingMode = PickingMode.Ignore;
            return line;
        }

        private static Label Text(string typeClass, string className)
        {
            var label = new Label();
            label.AddToClassList(typeClass);
            label.AddToClassList(className);
            label.pickingMode = PickingMode.Ignore;
            label.enableRichText = false;
            return label;
        }

        private static void Set(Label label, string text)
        {
            if (label.text != text)
            {
                label.text = text;
            }
        }

        // El latido del punto de RECEIVING (00-arte §5.2): finito, 300 ms por paquete.
        private sealed class DotPulse : TimedMotion
        {
            private readonly VisualElement _dot;

            public DotPulse(MotionDriver driver, VisualElement dot)
                : base(driver)
            {
                _dot = dot;
            }

            protected override double TotalMs => BleMotionSpecs.DebugDotPulseMs;

            public void Beat()
            {
                if (!Driver.Armed || Driver.Settings.ReduceMotion)
                {
                    return;
                }

                Finish();
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                _dot.style.opacity = (float)BleMotionSpecs.DebugDotOpacity(elapsedMs);
            }

            protected override void OnFinished()
            {
                _dot.style.opacity = StyleKeyword.Null;
            }
        }
    }
}
