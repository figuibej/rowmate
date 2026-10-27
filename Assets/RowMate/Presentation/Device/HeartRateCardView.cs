using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Tarjeta "Pulsómetro" (F-HRM-13; 02-tech-design §3.4; 04-design-notes §4.6; legacy heart_rate_card.dart:13-238). Solo
    // traduce HrmCardViewModel (HrmCardRules.Build): textos, SetEnabled de cada botón (deshabilitado = :disabled, 38 % y sin
    // respuesta, AC-HRM-13-04) y clases de estado. Los nombres hrm-search, hrm-retry, hrm-forget, hrm-disconnect y
    // hrm-device-<id> son los keys del legacy (spec §12.8).
    //   #hrm-card .rm-card .rm-hrm-card .rm-hrm-card--<body>     borde superior metric.hr al 60 % fijo (sin destello)
    //   ├── #hrm-glow .rm-card__glow                             brillo en reposo 0,084
    //   ├── .rm-hrm-card__header > #hrm-heart (favorite relleno 14) + #hrm-title .rm-type-label
    //   ├── #hrm-body .rm-hrm-card__body > .rm-hrm-card__body-content   se rearma al cambiar de cuerpo (HeightTween)
    //   └── #hrm-error .rm-hrm-card__error .rm-type-body-xs       hasta 3 líneas con "…" (TextClamp)
    // Una sola instancia: DeviceScreenView la mueve entre #hrm-card-slot-connected y #hrm-card-slot-scan.
    public sealed class HeartRateCardView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly HeartbeatAnimator _heartbeat;
        private readonly Label _title;
        private readonly VisualElement _body;
        private readonly VisualElement _content;
        private readonly HeightTween _heightTween;
        private readonly Label _error;
        private readonly TextClamp _errorClamp;
        private HrmCardViewModel? _model;
        private HeartRateState? _hr;
        private BleStatus _rowerStatus = BleStatus.Disconnected;
        private HrmCardBody? _builtBody;
        private string _builtRows = "";

        // Elementos del cuerpo vigente (los que cambian sin rearmar).
        private Label? _connectedName;
        private TabularNumber? _bpm;
        private FitBox? _bpmBox;
        private Label? _bpmUnit;
        private Label? _connectingText;
        private Label? _searchingText;
        private Label? _rememberedText;
        private Label? _noSensorText;
        private Label? _watchHint;
        private RmButton? _search;
        private RmButton? _retry;
        private RmButton? _forget;
        private RmButton? _disconnect;
        private readonly List<Button> _rows = new List<Button>();

        public HeartRateCardView(UiContext ui)
        {
            _ui = ui;
            name = "hrm-card";
            AddToClassList("rm-card");
            AddToClassList("rm-hrm-card");
            style.borderTopColor = TokenColors.WithAlpha(Tokens.MetricHr, MotionSpecs.AccentRestAlpha);

            var glow = new VisualElement { name = "hrm-glow" };
            glow.AddToClassList("rm-card__glow");
            glow.pickingMode = PickingMode.Ignore;
            if (ui.Fade != null)
            {
                glow.style.backgroundImage = new StyleBackground(ui.Fade);
            }

            glow.style.opacity = (float)(MotionSpecs.AccentGlowPeak * MotionSpecs.AccentRestAlpha);
            Add(glow);

            var header = new VisualElement();
            header.AddToClassList("rm-hrm-card__header");
            header.pickingMode = PickingMode.Ignore;
            Heart = new IconView(Icons.Favorite, true, 14) { name = "hrm-heart" };
            Heart.AddToClassList("rm-hrm-card__heart");
            header.Add(Heart);
            _title = new Label { name = "hrm-title" };
            _title.AddToClassList("rm-hrm-card__title");
            _title.AddToClassList("rm-type-label");
            _title.pickingMode = PickingMode.Ignore;
            header.Add(_title);
            Add(header);
            _heartbeat = new HeartbeatAnimator(ui.Motion, Heart);

            _body = new VisualElement { name = "hrm-body" };
            _body.AddToClassList("rm-hrm-card__body");
            _body.pickingMode = PickingMode.Ignore;
            _content = new VisualElement();
            _content.AddToClassList("rm-hrm-card__body-content");
            _content.pickingMode = PickingMode.Ignore;
            _body.Add(_content);
            Add(_body);
            _heightTween = new HeightTween(ui.Motion, _body, _content);

            _error = new Label { name = "hrm-error" };
            _error.AddToClassList("rm-hrm-card__error");
            _error.AddToClassList("rm-type-body-xs");
            _error.pickingMode = PickingMode.Ignore;
            _error.style.display = DisplayStyle.None;
            _errorClamp = new TextClamp(_error, 3);
            Add(_error);

            this.Bind(ui.App.HeartRate.State, OnHeartRate);
            this.Bind(ui.App.Device.State, OnRower);
            this.Bind(ui.App.Texts, _ => Rebuild());
        }

        public IconView Heart { get; }

        public HeartbeatAnimator Heartbeat => _heartbeat;

        public HrmCardViewModel? Model => _model;

        public HrmCardBody? Body => _builtBody;

        public TabularNumber? Bpm => _bpm;

        public FitBox? BpmBox => _bpmBox;

        public Label? BpmUnit => _bpmUnit;

        public string? ErrorFullText => _error.style.display == DisplayStyle.None ? null : _errorClamp.FullText;

        public bool ErrorIsClamped => _errorClamp.IsClamped;

        public void FinishAnimations()
        {
            _heightTween.Finish();
        }

        private void OnHeartRate(HeartRateState state)
        {
            _hr = state;
            Rebuild();
        }

        private void OnRower(DeviceState state)
        {
            if (state.Status == _rowerStatus && _model != null)
            {
                return;
            }

            _rowerStatus = state.Status;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_hr == null)
            {
                return;
            }

            var model = HrmCardRules.Build(_hr, _rowerStatus, _ui.Texts);
            Render(model);
        }

        private void Render(HrmCardViewModel model)
        {
            _model = model;
            if (_title.text != model.Title)
            {
                _title.text = model.Title;
            }

            _heartbeat.SetState(model.HeartFull, model.HeartBeating, model.Bpm);

            var rowsKey = RowsKey(model.Rows);
            if (_builtBody != model.Body || (model.Body == HrmCardBody.Scanning && rowsKey != _builtRows))
            {
                var animate = _builtBody != null && _ui.Motion.Armed && !_ui.Motion.Settings.ReduceMotion;
                if (animate)
                {
                    _heightTween.Start(_body.layout.height);
                }

                BuildBody(model);
                _builtBody = model.Body;
                _builtRows = rowsKey;
                for (var i = 0; i < 5; i++)
                {
                    RemoveFromClassList("rm-hrm-card--" + BodyClass((HrmCardBody)i));
                }

                AddToClassList("rm-hrm-card--" + BodyClass(model.Body));
            }

            UpdateBody(model);

            if (model.ErrorText == null)
            {
                _error.style.display = DisplayStyle.None;
            }
            else
            {
                _error.style.display = DisplayStyle.Flex;
                if (_errorClamp.FullText != model.ErrorText)
                {
                    _errorClamp.SetText(model.ErrorText);
                }
            }
        }

        private void BuildBody(HrmCardViewModel model)
        {
            _content.Clear();
            _rows.Clear();
            _connectedName = _connectingText = _searchingText = _rememberedText = _noSensorText = _watchHint = _bpmUnit = null;
            _bpm = null;
            _bpmBox = null;
            _search = _retry = _forget = _disconnect = null;
            var hrm = _ui.App.HeartRate;
            switch (model.Body)
            {
                case HrmCardBody.Connected:
                {
                    var row = Container("rm-hrm-connected");
                    var texts = Container("rm-hrm-connected__texts");
                    _connectedName = Text("hrm-connected-name", "rm-type-body-m");
                    texts.Add(_connectedName);
                    _bpmBox = new FitBox(true, true, _ui.Motion) { name = "hrm-bpm-box" };
                    _bpmBox.AddToClassList("rm-hrm-bpm-box");
                    var bpmRow = new VisualElement();
                    bpmRow.AddToClassList("rm-hrm-bpm-row");
                    bpmRow.pickingMode = PickingMode.Ignore;
                    _bpm = new TabularNumber(_ui.Motion) { name = "hrm-bpm" };
                    _bpm.AddToClassList("rm-hrm-bpm");
                    _bpm.AddToClassList("rm-type-num-xl");
                    _bpm.SetValue(model.BpmText, model.Bpm);
                    bpmRow.Add(_bpm);
                    _bpmUnit = Text("hrm-bpm-unit", "rm-type-body-m");
                    bpmRow.Add(_bpmUnit);
                    _bpm.RegisterCallback<GeometryChangedEvent>(_ => AlignUnitBaseline());
                    _bpmUnit.RegisterCallback<GeometryChangedEvent>(_ => AlignUnitBaseline());
                    _bpmBox.Add(bpmRow);
                    texts.Add(_bpmBox);
                    row.Add(texts);
                    _disconnect = new RmButton("hrm-disconnect", "text danger", Icons.BluetoothDisabled, model.DisconnectLabel,
                        () => { _ = hrm.DisconnectAsync(); });
                    row.Add(_disconnect);
                    _content.Add(row);
                    break;
                }

                case HrmCardBody.Connecting:
                {
                    var row = Container("rm-hrm-connecting");
                    var spinner = new Spinner(_ui.Motion);
                    row.Add(spinner);
                    _connectingText = Text("hrm-connecting-text", "rm-type-body-m");
                    row.Add(_connectingText);
                    _content.Add(row);
                    break;
                }

                case HrmCardBody.Scanning:
                {
                    var column = Container("rm-hrm-scanning");
                    var status = Container("rm-hrm-scanning__status");
                    status.Add(new Spinner(_ui.Motion));
                    _searchingText = Text("hrm-searching-text", "rm-type-body-s");
                    _searchingText.AddToClassList("rm-text--tertiary");
                    status.Add(_searchingText);
                    column.Add(status);
                    for (var i = 0; i < model.Rows.Count; i++)
                    {
                        column.Add(DeviceRow(model.Rows[i]));
                    }

                    _content.Add(column);
                    break;
                }

                case HrmCardBody.Remembered:
                {
                    _rememberedText = Text("hrm-remembered-text", "rm-type-body-m");
                    _content.Add(_rememberedText);
                    var actions = Container("rm-hrm-actions");
                    _retry = new RmButton("hrm-retry", "tonal", Icons.Refresh, model.ConnectLabel, () => { _ = hrm.RetryAsync(); });
                    actions.Add(_retry);
                    _search = new RmButton("hrm-search", "outline", null, model.SearchOtherLabel, () => { _ = hrm.StartScanAsync(); });
                    actions.Add(_search);
                    _forget = new RmButton("hrm-forget", "text", null, model.ForgetLabel, () => { _ = hrm.ForgetAsync(); });
                    actions.Add(_forget);
                    _content.Add(actions);
                    break;
                }

                default:
                {
                    _noSensorText = Text("hrm-nosensor-text", "rm-type-title-s");
                    _content.Add(_noSensorText);
                    _watchHint = Text("hrm-watch-hint", "rm-type-body-xs");
                    _watchHint.AddToClassList("rm-text--tertiary");
                    _content.Add(_watchHint);
                    _search = new RmButton("hrm-search", "tonal", Icons.BluetoothSearching, model.SearchLabel, () => { _ = hrm.StartScanAsync(); });
                    _content.Add(_search);
                    break;
                }
            }
        }

        private void UpdateBody(HrmCardViewModel model)
        {
            SetText(_connectedName, model.ConnectedName);
            if (_bpm != null && _bpm.Text != model.BpmText)
            {
                if (model.BpmText.Length != _bpm.Text.Length)
                {
                    _bpmBox?.ExpectDataChange();
                }
                _bpm.SetValue(model.BpmText, model.Bpm);
            }

            SetText(_bpmUnit, model.BpmUnit);
            SetText(_connectingText, model.ConnectingText);
            SetText(_searchingText, model.SearchingText);
            SetText(_rememberedText, model.RememberedLine);
            SetText(_noSensorText, model.NoSensorText);
            SetText(_watchHint, model.WatchHint);
            if (_search != null)
            {
                _search.SetLabel(model.Body == HrmCardBody.Remembered ? model.SearchOtherLabel : model.SearchLabel);
                _search.SetEnabled(model.SearchEnabled);
            }

            if (_retry != null)
            {
                _retry.SetLabel(model.ConnectLabel);
                _retry.SetEnabled(model.ConnectEnabled);
            }

            _forget?.SetLabel(model.ForgetLabel);
            _disconnect?.SetLabel(model.DisconnectLabel);
            for (var i = 0; i < _rows.Count && i < model.Rows.Count; i++)
            {
                var title = _rows[i].Q<Label>(className: "rm-hrm-device-row__title");
                SetText(title, model.Rows[i].Title);
            }
        }

        // Unidad "bpm" por la línea de base del número (DN-09): margen superior = base del número − base de la unidad.
        private void AlignUnitBaseline()
        {
            if (_bpm == null || _bpmUnit == null)
            {
                return;
            }

            var font = _bpmUnit.resolvedStyle.unityFontDefinition.fontAsset;
            var size = _bpmUnit.resolvedStyle.fontSize;
            if (font == null || !(size > 0) || !(_bpm.FontSize > 0))
            {
                return;
            }

            var face = font.faceInfo;
            var scale = face.pointSize > 0 ? size / face.pointSize * (face.scale > 0 ? face.scale : 1f) : 0;
            var unitBaseline = _bpmUnit.resolvedStyle.paddingTop + face.ascentLine * scale;
            var margin = Mathf.Max(0, _bpm.BaselineFromTop - unitBaseline);
            if (Mathf.Abs(_bpmUnit.resolvedStyle.marginTop - margin) > 0.05f)
            {
                _bpmUnit.style.marginTop = margin;
            }
        }

        private Button DeviceRow(HrmDeviceRow row)
        {
            var hrm = _ui.App.HeartRate;
            var id = row.DeviceId;
            var title = row.Title;
            var button = new Button(() => { _ = hrm.ConnectAsync(id, title); }) { name = row.ElementName, text = "" };
            RmButton.StripUnityChrome(button);
            button.AddToClassList("rm-hrm-device-row");
            button.focusable = true;
            button.style.flexShrink = 0;
            button.Add(RmButton.StateLayer());
            var icon = new IconView(Icons.Favorite, false, 24);
            icon.AddToClassList("rm-hrm-device-row__icon");
            button.Add(icon);
            var texts = Container("rm-hrm-device-row__texts");
            var titleLabel = new Label(row.Title);
            titleLabel.AddToClassList("rm-hrm-device-row__title");
            titleLabel.AddToClassList("rm-type-body");
            titleLabel.pickingMode = PickingMode.Ignore;
            texts.Add(titleLabel);
            var subtitle = new Label(row.Subtitle);
            subtitle.AddToClassList("rm-hrm-device-row__subtitle");
            subtitle.AddToClassList("rm-type-caption");
            subtitle.AddToClassList("rm-text--tertiary");
            subtitle.pickingMode = PickingMode.Ignore;
            texts.Add(subtitle);
            button.Add(texts);
            var chevron = new IconView(Icons.ChevronRight, false, 18);
            chevron.AddToClassList("rm-hrm-device-row__chevron");
            button.Add(chevron);
            button.Add(RmButton.FocusRing());
            _rows.Add(button);
            return button;
        }

        private static VisualElement Container(string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

        private static Label Text(string name, string typeClass)
        {
            var label = new Label { name = name };
            label.AddToClassList(typeClass);
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private static void SetText(Label? label, string text)
        {
            if (label != null && label.text != text)
            {
                label.text = text;
            }
        }

        private static string RowsKey(IReadOnlyList<HrmDeviceRow> rows)
        {
            if (rows.Count == 0)
            {
                return "";
            }

            var key = "";
            for (var i = 0; i < rows.Count; i++)
            {
                key += rows[i].DeviceId + "\n";
            }

            return key;
        }

        public static string BodyClass(HrmCardBody body)
        {
            switch (body)
            {
                case HrmCardBody.Connected:
                    return "connected";
                case HrmCardBody.Connecting:
                    return "connecting";
                case HrmCardBody.Scanning:
                    return "scanning";
                case HrmCardBody.Remembered:
                    return "remembered";
                default:
                    return "nosensor";
            }
        }
    }
}
