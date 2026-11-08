using System;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Pestaña Dispositivo (F-BLE-07, F-BLE-11, AC-BLE-05; 02-tech-design §3.3; legacy device_screen.dart). It. 10a (02-tech-design §6.1):
    //   #screen-device .rm-screen
    //   ├── #device-appbar .rm-appbar: deviceTitle y, solo conectado, #device-disconnect (texto peligro, bluetooth_disabled)
    //   ├── #device-error-banner (ErrorBanner: deviceScanError / deviceConnectError + el detalle; arriba en las tres vistas)
    //   └── #device-views .rm-device-views: superpuestas (DN-12)
    //       ├── #device-state-view      BluetoothStateView (apagado, sin permiso, sin Bluetooth LE)
    //       ├── #device-scan-view       ScanView (caja del enlace, búsqueda, lista, vacío y la tarjeta del pulsómetro)
    //       └── #device-connected-view  ConnectedView (cabecera, grilla, pulsómetro y diagnóstico)
    // Qué vista: DeviceScreenRules.ViewFor(status, adapter) (AC-BLE-04-01, -02, -06, AC-BLE-11-01); el cambio usa ViewSwap. Una sola
    // HeartRateCardView que se mueve de slot al cambiar de vista (no se ve en la de estado, spec §7). Pasos (A10-05): al quedar
    // conectado por una conexión elegida, la tarjeta tocada muestra "Listo" y la vista retiene 240 ms el cambio antes del "encender";
    // el estado de la app ya es Connected (Workout lo ve en el cuadro 0). El primer estado (los primeros 500 ms) se aplica sin animar.
    public sealed class DeviceScreenView : VisualElement
    {
        // A10-05: lo que se ve el check de "Listo" antes del "encender" (D10-04: 240 + 360 ≤ 600 ms).
        public const double ReadyHoldMs = 240;

        // 00-arte §3.2: el primer estado conocido en los primeros 500 ms desde que se abre la app se aplica en el acto.
        public const double FirstStateWindowMs = 500;

        private readonly UiContext _ui;
        private readonly AppBarView _appBar;
        private readonly RmButton _disconnect;
        private readonly VisualElement _views;
        private readonly ViewSwap _swap;
        private readonly BleMotion.Wake _wake;
        private readonly BleMotion.DiscIn _discIn;
        private readonly double _createdAt;
        private DeviceViewKind? _view;
        private DeviceState? _device;
        private bool _hrmScanning;
        private IDisposable? _readyHold;
        private bool _holding;

        public DeviceScreenView(UiContext ui, ShellLayout layout)
        {
            _ui = ui;
            _createdAt = ui.App.Scheduler.MonotonicSeconds;
            name = "screen-device";
            AddToClassList("rm-screen");
            _appBar = new AppBarView("device-appbar", layout);
            _disconnect = new RmButton("device-disconnect", "text danger", Icons.BluetoothDisabled, "",
                () => _ui.App.Device.DisconnectAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag));
            _disconnect.style.display = DisplayStyle.None;
            _appBar.Actions.Add(_disconnect);
            Add(_appBar);

            ErrorBanner = new ErrorBanner();
            Add(ErrorBanner);

            _views = new VisualElement { name = "device-views" };
            _views.AddToClassList("rm-device-views");
            _views.pickingMode = PickingMode.Ignore;
            State = new BluetoothStateView(ui, layout, () => _ui.App.Device.RequestEnableAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag));
            Connected = new ConnectedView(ui, layout);
            Scan = new ScanView(ui, layout, () => _ui.App.Device.StartScanAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag),
                id => _ui.App.Device.ConnectAsync(id).Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag), OnLinkAction);
            State.style.display = DisplayStyle.None;
            Connected.style.display = DisplayStyle.None;
            Scan.style.display = DisplayStyle.None;
            _views.Add(State);
            _views.Add(Connected);
            _views.Add(Scan);
            Add(_views);
            Card = new HeartRateCardView(ui);
            _swap = new ViewSwap(ui.Motion);
            _wake = new BleMotion.Wake(ui.Motion);
            _discIn = new BleMotion.DiscIn(ui.Motion);
            Blocker(State);
            Blocker(Connected);
            Blocker(Scan);
            Connected.DebugOpened += () =>
            {
                if (_device != null)
                {
                    Connected.RenderDebug(DeviceScreenRules.BleDebug(_device));
                }
            };

            this.Bind(ui.App.Device.State, OnDevice);
            this.Bind(ui.App.HeartRate.State, OnHeartRate);
            this.Bind(ui.App.Texts, _ => Render());
            RegisterCallback<DetachFromPanelEvent>(_ => CancelHold());
        }

        public ConnectedView Connected { get; }

        public ScanView Scan { get; }

        public BluetoothStateView State { get; }

        public ErrorBanner ErrorBanner { get; }

        public HeartRateCardView Card { get; }

        // La vista a la vista (la que se muestra; durante la retención de "Listo", todavía la búsqueda).
        public DeviceViewKind? View => _view;

        public RmButton Disconnect => _disconnect;

        // La retención de 240 ms de "Listo" está corriendo (A10-05).
        public bool HoldingReady => _holding;

        private void Blocker(VisualElement view)
        {
            view.RegisterCallback<PointerDownEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<PointerUpEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<ClickEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<NavigationSubmitEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
        }

        private void Block(VisualElement view, EventBase evt)
        {
            if (_swap.IsBlocking(view) || _wake.IsBlocking(view) || _holding && ReferenceEquals(view, Scan))
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnLinkAction(LinkBannerKind kind)
        {
            var device = _ui.App.Device;
            var task = kind == LinkBannerKind.MonitorStopped ? device.RetryAsync() : device.DisconnectAsync();
            task.Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag);
        }

        private void OnDevice(DeviceState state)
        {
            _device = state;
            Render();
        }

        private void OnHeartRate(HeartRateState state)
        {
            var scanning = state.IsScanning;
            if (scanning == _hrmScanning && _device != null)
            {
                return;
            }

            _hrmScanning = scanning;
            Render();
        }

        private void Render()
        {
            var state = _device;
            if (state == null)
            {
                return;
            }

            var texts = _ui.Texts;
            _appBar.SetTitle(texts.Get(L10nKeys.DeviceTitle));
            _disconnect.SetLabel(texts.Get(L10nKeys.DeviceDisconnect));
            _disconnect.style.display = DeviceScreenRules.ShowDisconnectAction(state.Status) ? DisplayStyle.Flex : DisplayStyle.None;
            ErrorBanner.Render(DeviceScreenRules.ErrorText(state, texts));

            // La vista conectada solo se dibuja con el remo conectado (QA-V2-01, AC-BLE-11-02). Al desconectar queda congelada
            // con lo último que mostró mientras sale (120 ms): el legacy la desmonta en el acto (device_screen.dart:33-38).
            var view = DeviceScreenRules.ViewFor(state.Status, state.AdapterState);
            if (view == DeviceViewKind.Connected)
            {
                Connected.Header.Render(DeviceScreenRules.DeviceName(state, texts), texts.Get(L10nKeys.DeviceConnected));
                Connected.Grid.Render(DeviceScreenRules.Tiles(state.Data, texts));
                Connected.RenderDebug(DeviceScreenRules.BleDebug(state));
            }

            var bluetooth = DeviceScreenRules.BluetoothState(state.AdapterState, texts);
            if (bluetooth != null)
            {
                State.Render(bluetooth);
            }

            var items = DeviceScreenRules.ScanItems(state, texts);
            Scan.Render(DeviceScreenRules.Scan(state.Status, _hrmScanning, state.ScanResults.Count), items, DeviceScreenRules.LinkBanner(state, texts), texts);

            if (view != DeviceViewKind.Connected)
            {
                CancelHold();
            }

            if (_view == view || _holding && view == DeviceViewKind.Connected)
            {
                return;
            }

            var previous = _view;
            if (previous == null || _ui.App.Scheduler.MonotonicSeconds - _createdAt < FirstStateWindowMs / 1000.0 && previous != DeviceViewKind.Connected
                && view != DeviceViewKind.Connected)
            {
                // Estado inicial (o el primero conocido en los primeros 500 ms): sin animación.
                _view = view;
                ShowNow(view);
                return;
            }

            // A10-05: una conexión elegida por el usuario muestra "Listo" en su tarjeta 240 ms antes del "encender".
            if (view == DeviceViewKind.Connected && previous == DeviceViewKind.Scan && HasActiveItem(items) && _ui.Motion.Armed)
            {
                _holding = true;
                _readyHold = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(ReadyHoldMs), () =>
                {
                    _readyHold = null;
                    _holding = false;
                    if (_device != null && DeviceScreenRules.ViewFor(_device.Status, _device.AdapterState) == DeviceViewKind.Connected
                        && _view != DeviceViewKind.Connected)
                    {
                        SwapTo(DeviceViewKind.Connected);
                    }
                });
                return;
            }

            SwapTo(view);
        }

        // La tarjeta de la conexión que eligió el usuario (no la reconexión) está en la lista (ScanItemSpec.IsActive).
        private static bool HasActiveItem(System.Collections.Generic.IReadOnlyList<ScanItemSpec> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private void SwapTo(DeviceViewKind view)
        {
            var previous = _view;
            _view = view;
            if (previous == null)
            {
                ShowNow(view);
                return;
            }

            if (previous == DeviceViewKind.Connected)
            {
                Connected.ResetDebug();
            }

            var outgoing = ElementFor(previous.Value);
            var incoming = ElementFor(view);
            // WP-D (00-arte §3.2, A10-07): la transición en curso salta a su final y arranca la nueva (regla de la It. 2).
            _wake.Finish();
            if (previous == DeviceViewKind.BluetoothState && view == DeviceViewKind.Scan)
            {
                // "Se despierta": la ilustración en 0-360, la vista de estado sale en 360-480 y la búsqueda entra en 480-720.
                _swap.Finish();
                _wake.Play(State, Scan, State.Art, () => PlaceCard(view));
                return;
            }

            if (view == DeviceViewKind.BluetoothState)
            {
                // La vista de estado entra con el disco 0,94 → 1 y el ícono ya en rojo (120-360).
                _swap.Play(outgoing, incoming, previous == DeviceViewKind.Connected, true, () =>
                {
                    PlaceCard(view);
                    _discIn.Play(State.Art);
                });
                return;
            }

            if (view == DeviceViewKind.Connected)
            {
                Connected.Grid.FinishAnimations();
                _swap.Play(outgoing, incoming, false, false, OnConnectedEnters);
            }
            else
            {
                _swap.Play(outgoing, incoming, previous == DeviceViewKind.Connected, true, () => PlaceCard(view));
            }
        }

        private void CancelHold()
        {
            _readyHold?.Dispose();
            _readyHold = null;
            _holding = false;
        }

        private VisualElement ElementFor(DeviceViewKind view)
        {
            switch (view)
            {
                case DeviceViewKind.Connected:
                    return Connected;
                case DeviceViewKind.BluetoothState:
                    return State;
                default:
                    return Scan;
            }
        }

        private void ShowNow(DeviceViewKind view)
        {
            Connected.style.display = view == DeviceViewKind.Connected ? DisplayStyle.Flex : DisplayStyle.None;
            Scan.style.display = view == DeviceViewKind.Scan ? DisplayStyle.Flex : DisplayStyle.None;
            State.style.display = view == DeviceViewKind.BluetoothState ? DisplayStyle.Flex : DisplayStyle.None;
            if (view != DeviceViewKind.Connected)
            {
                Connected.ResetDebug();
            }

            PlaceCard(view);
        }

        // "Encender" (04-design-notes §5.5): desplazamiento en 0, la tarjeta al slot de la conectada, la cabecera entra con
        // opacidad y cada tarjeta de la grilla escalonada 35 ms con su destello; la del pulsómetro después, sin destello.
        private void OnConnectedEnters()
        {
            PlaceCard(DeviceViewKind.Connected);
            Connected.ResetScroll();
            if (!_ui.Motion.Armed)
            {
                return;
            }

            Connected.Grid.PlayEnter(0);
            _headerFade ??= new FadeIn(_ui.Motion);
            _headerFade.Play(Connected.Header, 0, MotionSpecs.ViewInEndMs - MotionSpecs.ViewOutMs, false);
            _cardFade ??= new FadeIn(_ui.Motion);
            var delay = _ui.Motion.Settings.ReduceMotion ? 0 : MotionSpecs.TileStaggerMs * Connected.Grid.Tiles.Count;
            _cardFade.Play(Card, delay, MotionSpecs.TileInMs, true);
        }

        private FadeIn? _headerFade;
        private FadeIn? _cardFade;

        // La tarjeta del pulsómetro: en el slot de la vista conectada o en el de la búsqueda; la vista de estado no la muestra (el slot
        // de la búsqueda queda oculto con ella).
        private void PlaceCard(DeviceViewKind view)
        {
            var slot = view == DeviceViewKind.Connected ? Connected.CardSlot : Scan.CardSlot.contentContainer;
            if (Card.parent != slot)
            {
                slot.Add(Card);
            }
        }

        // Entrada con opacidad (y desplazamiento shift.m si no hay reducir movimiento) después de una espera.
        private sealed class FadeIn : TimedMotion
        {
            private VisualElement? _target;
            private double _delay;
            private double _duration;
            private bool _reduced;
            private bool _slide;

            public FadeIn(MotionDriver driver)
                : base(driver)
            {
            }

            protected override double TotalMs => _delay + _duration;

            public void Play(VisualElement target, double delayMs, double durationMs, bool slide)
            {
                Finish();
                _target = target;
                _reduced = Driver.Settings.ReduceMotion;
                _slide = slide;
                _delay = delayMs;
                _duration = _reduced ? Tokens.DurFast.Milliseconds : durationMs;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_target == null)
                {
                    return;
                }

                var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, _delay, _delay + _duration));
                _target.style.opacity = (float)e;
                _target.style.translate = _reduced || !_slide ? new Translate(0, 0) : new Translate(0, (float)(Tokens.ShiftM.Value * (1 - e)));
            }

            protected override void OnFinished()
            {
                if (_target != null)
                {
                    _target.style.opacity = StyleKeyword.Null;
                    _target.style.translate = StyleKeyword.Null;
                }

                _target = null;
            }
        }
    }
}
