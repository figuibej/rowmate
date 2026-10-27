using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Vista conectada de Dispositivo (AC-BLE-11-01 a -07; legacy _ConnectedView, device_screen.dart:80-136): un solo bloque
    // desplazable con márgenes 16 / 20 / 16 / 20 (+ el alto real de la barra abajo, DN-23), la cabecera a 8 + 16 del borde,
    // la grilla a 24, la tarjeta del pulsómetro a 12 y 64 dp bajo ella hasta el final del contenido (margin-bottom 48 del slot
    // + padding 16; los SizedBox de 32 y 16 del legacy, AC-BLE-11-07 corregida, QA-G2-12).
    //   #device-connected-view .rm-device-connected        absoluta, cuatro lados en 0
    //   └── #device-connected-scroll (ScrollView)
    //       ├── #device-header (DeviceHeaderView)
    //       ├── #metric-grid (MetricGridView)
    //       └── #hrm-card-slot-connected
    public sealed class ConnectedView : VisualElement
    {
        public const float ContentPaddingBottom = 16;

        public ConnectedView(UiContext ui, ShellLayout layout)
        {
            name = "device-connected-view";
            AddToClassList("rm-device-connected");
            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "device-connected-scroll" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Scroll.contentContainer.AddToClassList("rm-device-connected__content");
            layout.RegisterBottomPadding(Scroll.contentContainer, ContentPaddingBottom);
            Add(Scroll);
            Header = new DeviceHeaderView();
            Scroll.Add(Header);
            Grid = new MetricGridView(ui);
            Scroll.Add(Grid);
            CardSlot = new VisualElement { name = "hrm-card-slot-connected" };
            CardSlot.AddToClassList("rm-hrm-slot");
            CardSlot.pickingMode = PickingMode.Ignore;
            Scroll.Add(CardSlot);
        }

        public ScrollView Scroll { get; }

        public DeviceHeaderView Header { get; }

        public MetricGridView Grid { get; }

        public VisualElement CardSlot { get; }

        // Al volver a la vista conectada (tras una caída o al reconectar) el desplazamiento vuelve a 0, como el legacy, que
        // crea un _ConnectedView nuevo. El cambio de pestaña no lo toca.
        public void ResetScroll()
        {
            Scroll.scrollOffset = Vector2.zero;
        }
    }
}
