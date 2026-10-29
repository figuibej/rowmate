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
    // Pestaña Dispositivo (F-BLE-07, F-BLE-11, AC-BLE-05; 02-tech-design §3.3; legacy device_screen.dart).
    //   #screen-device .rm-screen
    //   ├── #device-appbar .rm-appbar: deviceTitle y, solo conectado, #device-disconnect (texto peligro, bluetooth_disabled)
    //   └── #device-views .rm-device-views: #device-connected-view y #device-scan-view superpuestas (DN-12)
    // Qué vista: DeviceScreenRules.ViewFor (AC-BLE-11-01); el cambio usa ViewSwap. Una sola HeartRateCardView que se mueve
    // de slot al cambiar de vista. Las lecturas llegan por DeviceStore (una por segundo con el simulador).
    public sealed class DeviceScreenView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly AppBarView _appBar;
        private readonly RmButton _disconnect;
        private readonly VisualElement _views;
        private readonly ViewSwap _swap;
        private DeviceViewKind? _view;
        private DeviceState? _device;
        private bool _hrmScanning;

        public DeviceScreenView(UiContext ui, ShellLayout layout)
        {
            _ui = ui;
            name = "screen-device";
            AddToClassList("rm-screen");
            _appBar = new AppBarView("device-appbar", layout);
            _disconnect = new RmButton("device-disconnect", "text danger", Icons.BluetoothDisabled, "",
                () => _ui.App.Device.DisconnectAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag));
            _disconnect.style.display = DisplayStyle.None;
            _appBar.Actions.Add(_disconnect);
            Add(_appBar);

            _views = new VisualElement { name = "device-views" };
            _views.AddToClassList("rm-device-views");
            _views.pickingMode = PickingMode.Ignore;
            Connected = new ConnectedView(ui, layout);
            Scan = new ScanView(ui, layout, () => _ui.App.Device.StartScanAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.BleTag));
            Connected.style.display = DisplayStyle.None;
            Scan.style.display = DisplayStyle.None;
            _views.Add(Connected);
            _views.Add(Scan);
            Add(_views);
            Card = new HeartRateCardView(ui);
            _swap = new ViewSwap(ui.Motion);
            Blocker(Connected);
            Blocker(Scan);

            this.Bind(ui.App.Device.State, OnDevice);
            this.Bind(ui.App.HeartRate.State, OnHeartRate);
            this.Bind(ui.App.Texts, _ => Render());
        }

        public ConnectedView Connected { get; }

        public ScanView Scan { get; }

        public HeartRateCardView Card { get; }

        public DeviceViewKind? View => _view;

        public RmButton Disconnect => _disconnect;

        private void Blocker(VisualElement view)
        {
            view.RegisterCallback<PointerDownEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<PointerUpEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<ClickEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
            view.RegisterCallback<NavigationSubmitEvent>(evt => Block(view, evt), TrickleDown.TrickleDown);
        }

        private void Block(VisualElement view, EventBase evt)
        {
            if (_swap.IsBlocking(view))
            {
                evt.StopImmediatePropagation();
            }
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

            // La vista conectada solo se dibuja con el remo conectado (QA-V2-01, AC-BLE-11-02). Al desconectar queda congelada
            // con lo último que mostró mientras sale (120 ms): el legacy la desmonta en el acto (device_screen.dart:33-38) y
            // nunca dibuja la cabecera sin remo; si se la dibujara acá, ConnectedDeviceName ya es null y diría "Rower" (:98).
            var view = DeviceScreenRules.ViewFor(state.Status);
            if (view == DeviceViewKind.Connected)
            {
                Connected.Header.Render(DeviceScreenRules.DeviceName(state.ConnectedDeviceName), texts.Get(L10nKeys.DeviceConnected));
                Connected.Grid.Render(DeviceScreenRules.Tiles(state.Data, texts));
            }

            Scan.Render(DeviceScreenRules.Scan(state.Status, _hrmScanning, state.ScanResults.Count), texts);

            if (_view == view)
            {
                return;
            }

            var previous = _view;
            _view = view;
            if (previous == null)
            {
                // Estado inicial: sin animación (nada se anima antes del primer cuadro presentado).
                ShowNow(view);
                return;
            }

            if (view == DeviceViewKind.Scan)
            {
                _swap.Play(Connected, Scan, true, true, () => PlaceCard(DeviceViewKind.Scan));
            }
            else
            {
                Connected.Grid.FinishAnimations();
                _swap.Play(Scan, Connected, false, false, OnConnectedEnters);
            }
        }

        private void ShowNow(DeviceViewKind view)
        {
            Connected.style.display = view == DeviceViewKind.Connected ? DisplayStyle.Flex : DisplayStyle.None;
            Scan.style.display = view == DeviceViewKind.Scan ? DisplayStyle.Flex : DisplayStyle.None;
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
