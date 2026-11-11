using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
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
    //       ├── #device-header (DeviceHeaderView; presión ≥ 0,5 s abre el diagnóstico, DV-104)
    //       ├── #metric-grid (MetricGridView)
    //       ├── #hrm-card-slot-connected
    //       ├── #ble-debug-toggle   "Show BLE debug" (3 arrastres de > 20 dp hacia abajo, ds:52-74, :124-134)
    //       └── #ble-debug-panel    (BleDebugPanel, AC-BLE-12-02)
    // It. 10a: el acceso al diagnóstico es DebugRevealGesture (App); se reinicia al salir de la vista conectada (ResetDebug).
    public sealed class ConnectedView : VisualElement
    {
        public const float ContentPaddingBottom = 16;

        private readonly UiContext _ui;
        private System.IDisposable? _pressTimer;
        private readonly VisualElement _pressLayer;
        private readonly Motion.BleMotion.Fade _pressFade;

        public ConnectedView(UiContext ui, ShellLayout layout)
        {
            _ui = ui;
            name = "device-connected-view";
            AddToClassList("rm-device-connected");
            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "device-connected-scroll" };
            Scroll.AddToClassList("rm-scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            Scroll.contentContainer.AddToClassList("rm-device-connected__content");
            layout.RegisterBottomPadding(Scroll.contentContainer, ContentPaddingBottom);
            // It. 15 (#82, AC-D15-33): el primer elemento visible queda en su lugar al cambiar el ancho (como en Rutinas, DD14-10).
            Anchor = ScrollAnchor.Attach(Scroll);
            Add(Scroll);
            Header = new DeviceHeaderView();
            // WP-D (00-arte §5.1): la capa de la cabecera es el progreso de "mantener presionado" (0 → 8 % lineal en 0-500 ms; soltar antes la
            // devuelve en 160).
            _pressLayer = new VisualElement { name = "device-header-press" };
            _pressLayer.AddToClassList("rm-device-header__press");
            _pressLayer.pickingMode = PickingMode.Ignore;
            _pressLayer.style.opacity = 0;
            Header.Insert(0, _pressLayer);
            _pressFade = new Motion.BleMotion.Fade(ui.Motion);
            Scroll.Add(Header);
            Grid = new MetricGridView(ui);
            Scroll.Add(Grid);
            CardSlot = new VisualElement { name = "hrm-card-slot-connected" };
            CardSlot.AddToClassList("rm-hrm-slot");
            CardSlot.pickingMode = PickingMode.Ignore;
            Scroll.Add(CardSlot);

            // El botón y el panel se crean recién cuando hacen falta (el acceso es escondido: sin gesto no hay ningún texto de depuración
            // en el árbol, ShellTests.NoDebugMarksBesidesTheSimulatorRibbon).

            Scroll.verticalScroller.valueChanged += value =>
            {
                Gesture.OnScroll(value);
                RefreshDebug();
            };
            Header.pickingMode = PickingMode.Position;
            Header.RegisterCallback<PointerDownEvent>(_ => PressStart());
            Header.RegisterCallback<PointerUpEvent>(_ => PressEnd());
            Header.RegisterCallback<PointerLeaveEvent>(_ => PressEnd());
            Header.RegisterCallback<PointerCancelEvent>(_ => PressEnd());
        }

        public ScrollView Scroll { get; }

        public ScrollAnchor Anchor { get; }

        public DeviceHeaderView Header { get; }

        public MetricGridView Grid { get; }

        public VisualElement CardSlot { get; }

        public RmButton? DebugToggle { get; private set; }

        public BleDebugPanel? DebugPanel { get; private set; }

        public DebugRevealGesture Gesture { get; } = new DebugRevealGesture();

        // Al volver a la vista conectada (tras una caída o al reconectar) el desplazamiento vuelve a 0, como el legacy, que
        // crea un _ConnectedView nuevo. El cambio de pestaña no lo toca.
        public void ResetScroll()
        {
            Scroll.scrollOffset = Vector2.zero;
        }

        // Al salir de la vista conectada: hay que repetir el gesto (AC-BLE-12-01).
        public void ResetDebug()
        {
            CancelPress();
            Gesture.Reset();
            RefreshDebug();
        }

        public void RenderDebug(BleDebugSpec spec)
        {
            if (Gesture.PanelOpen && DebugPanel != null)
            {
                DebugPanel.Render(spec);
            }
        }

        public void OpenDebug()
        {
            if (_pressLayer.resolvedStyle.opacity > 0 || _pressFade.IsRunning)
            {
                _pressFade.Play(_pressLayer, _pressLayer.resolvedStyle.opacity, 0, 0, Tokens.DurFast.Milliseconds, Tokens.EaseExit, false);
            }

            Gesture.OpenPanel();
            RefreshDebug();
            DebugOpened?.Invoke();
        }

        // El panel se abrió (la vista le pasa el estado actual).
        public event System.Action? DebugOpened;

        private void RefreshDebug()
        {
            if (Gesture.ShowButton && DebugToggle == null)
            {
                DebugToggle = new RmButton("ble-debug-toggle", "text", Icons.BugReport, BleDebugSpec.ShowButton, OpenDebug, 14, "rm-type-body-xs");
                DebugToggle.AddToClassList("rm-ble-debug-toggle");
                // It. 15 (AC-D15-26c, DV-147): "Show BLE debug" sigue en inglés y exento como diagnóstico (Q-04).
                DebugToggle.AddToClassList("rm-audit-exempt");
                Scroll.Add(DebugToggle);
            }

            if (Gesture.PanelOpen && DebugPanel == null)
            {
                DebugPanel = new BleDebugPanel(_ui.Motion);
                Scroll.Add(DebugPanel);
            }

            if (DebugToggle != null)
            {
                DebugToggle.style.display = Gesture.ShowButton ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (DebugPanel != null)
            {
                DebugPanel.style.display = Gesture.PanelOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // Mantener presionada la cabecera ≥ 0,5 s (DV-104): abre el panel directo.
        private void PressStart()
        {
            CancelPress();
            var scheduler = _ui.App.Scheduler;
            Gesture.PressStart(scheduler.MonotonicSeconds);
            _pressFade.Play(_pressLayer, 0, (float)BleMotionSpecs.HeaderPressLayerOpacity, 0, BleMotionSpecs.HeaderPressMs, Tokens.EaseLinear, false);
            _pressTimer = scheduler.Schedule(System.TimeSpan.FromSeconds(DebugRevealGesture.LongPressSeconds), () =>
            {
                _pressTimer = null;
                if (Gesture.PressUpdate(scheduler.MonotonicSeconds))
                {
                    OpenDebug();
                }
            });
        }

        private void PressEnd()
        {
            if (!Gesture.Pressing)
            {
                return;
            }

            CancelPress();
            if (Gesture.PressEnd(_ui.App.Scheduler.MonotonicSeconds))
            {
                OpenDebug();
            }
        }

        private void CancelPress()
        {
            if (_pressLayer.resolvedStyle.opacity > 0 || _pressFade.IsRunning)
            {
                _pressFade.Play(_pressLayer, _pressLayer.resolvedStyle.opacity, 0, 0, Tokens.DurFast.Milliseconds, Tokens.EaseExit, false);
            }

            _pressTimer?.Dispose();
            _pressTimer = null;
        }
    }
}
