using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Tarjeta del tablero de Dispositivo (AC-BLE-11-03, -06; AC-APP-08-02; 04-design-notes §4.4; legacy _MetricCard,
    // device_screen.dart:288-330).
    //   #metric-tile-<m> .rm-card .rm-metric-tile .rm-metric--<m>
    //   ├── .rm-metric-tile__glow .rm-card__glow          brillo (DN-06): textura de degradé, opacidad 0,14·k por código
    //   ├── .rm-metric-tile__header > .rm-metric-tile__icon (14) + .rm-metric-tile__label .rm-type-label
    //   └── .rm-metric-tile__value-box (FitBox) > .rm-metric-tile__value (TabularNumber) .rm-type-num-xl
    // Solo si cambió el TEXTO del valor: rodillo en las celdas que cambian y destello del acento (DN-07). El borde superior va
    // en el color de la métrica con alfa k por código (0,60 en reposo, DV-02).
    public sealed class MetricTileView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly VisualElement _glow;
        private readonly IconView _icon;
        private readonly Label _label;
        private readonly FitBox _valueBox;
        private readonly TabularNumber _value;
        private readonly AccentFlash _flash;
        private readonly TileEnter _enter;

        public MetricTileView(UiContext ui, MetricTileSpec spec)
        {
            _ui = ui;
            Kind = spec.Kind;
            name = spec.ElementName;
            AddToClassList("rm-card");
            AddToClassList("rm-metric-tile");
            AddToClassList("rm-metric--" + ModifierFor(spec.Kind));
            pickingMode = PickingMode.Ignore;

            _glow = new VisualElement();
            _glow.AddToClassList("rm-metric-tile__glow");
            _glow.AddToClassList("rm-card__glow");
            _glow.pickingMode = PickingMode.Ignore;
            if (ui.Fade != null)
            {
                _glow.style.backgroundImage = new StyleBackground(ui.Fade);
            }

            Add(_glow);

            var header = new VisualElement();
            header.AddToClassList("rm-metric-tile__header");
            header.pickingMode = PickingMode.Ignore;
            _icon = new IconView(spec.IconCodepoint, spec.IconFilled, 14);
            _icon.AddToClassList("rm-metric-tile__icon");
            header.Add(_icon);
            _label = new Label(spec.Label);
            _label.AddToClassList("rm-metric-tile__label");
            _label.AddToClassList("rm-type-label");
            _label.pickingMode = PickingMode.Ignore;
            header.Add(_label);
            Add(header);

            _valueBox = new FitBox(false, false, ui.Motion);
            _valueBox.AddToClassList("rm-metric-tile__value-box");
            _value = new TabularNumber(ui.Motion);
            _value.AddToClassList("rm-metric-tile__value");
            _value.AddToClassList("rm-type-num-xl");
            _value.SetValue(spec.ValueText, spec.NumericValue);
            _valueBox.Add(_value);
            Add(_valueBox);

            _flash = new AccentFlash(ui.Motion, this, _glow, spec.Color);
            _enter = new TileEnter(ui.Motion, this);
        }

        public MetricKind Kind { get; }

        public TabularNumber Value => _value;

        public FitBox ValueBox => _valueBox;

        public AccentFlash Flash => _flash;

        public string LabelText => _label.text;

        public static string ModifierFor(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.Split:
                    return "split";
                case MetricKind.Spm:
                    return "spm";
                case MetricKind.Watts:
                    return "watts";
                case MetricKind.Distance:
                    return "distance";
                case MetricKind.HeartRate:
                    return "hr";
                case MetricKind.Time:
                    return "time";
                default:
                    return "kcal";
            }
        }

        // Una lectura: solo si el texto cambió, rodillo y destello (DN-07, spec §10.2).
        public void Update(MetricTileSpec spec)
        {
            if (_label.text != spec.Label)
            {
                _label.text = spec.Label;
            }

            if (spec.ValueText == _value.Text)
            {
                return;
            }

            // Solo un cambio de largo puede cambiar el ancho (las celdas de dígito son fijas): si no, no hay nada que animar.
            if (spec.ValueText.Length != _value.Text.Length)
            {
                _valueBox.ExpectDataChange();
            }
            _value.SetValue(spec.ValueText, spec.NumericValue);
            _flash.Flash();
        }

        // "Encender" (04-design-notes §5.5, §5.9): entra después de delayMs con opacidad 0 → 1 e y +12 → 0 en 240 ms
        // (ease.enter) y su acento se prende al empezar. Reducir movimiento: solo opacidad, sin escalonado.
        public void PlayEnter(double delayMs)
        {
            _enter.Play(delayMs);
        }

        public void FinishEnter()
        {
            _enter.Finish();
        }

        private sealed class TileEnter : TimedMotion
        {
            private readonly MetricTileView _tile;
            private double _delay;
            private bool _reduced;
            private bool _flashed;

            public TileEnter(MotionDriver driver, MetricTileView tile)
                : base(driver)
            {
                _tile = tile;
            }

            protected override double TotalMs => _delay + (_reduced ? Tokens.DurFast.Milliseconds : MotionSpecs.TileInMs);

            public void Play(double delayMs)
            {
                if (!Driver.Armed)
                {
                    OnFinished();
                    return;
                }

                _reduced = Driver.Settings.ReduceMotion;
                _delay = _reduced ? 0 : delayMs;
                _flashed = false;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var duration = _reduced ? Tokens.DurFast.Milliseconds : MotionSpecs.TileInMs;
                var u = Progress(elapsedMs, _delay, _delay + duration);
                var e = Easing.Evaluate(Tokens.EaseEnter, u);
                _tile.style.opacity = (float)e;
                _tile.style.translate = _reduced ? new Translate(0, 0) : new Translate(0, (float)(Tokens.ShiftM.Value * (1 - e)));
                if (!_flashed && elapsedMs >= _delay)
                {
                    _flashed = true;
                    _tile._flash.Flash();
                }
            }

            protected override void OnFinished()
            {
                _tile.style.opacity = StyleKeyword.Null;
                _tile.style.translate = StyleKeyword.Null;
            }
        }
    }
}
