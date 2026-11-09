using System;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Tarjeta "Pulsómetro" (F-HRM-13; 02-tech-design §3.4; 04-design-notes §4.6; legacy heart_rate_card.dart:13-238). Solo
    // traduce HrmCardViewModel (HrmCardRules.Build): textos, SetEnabled de cada botón (deshabilitado = :disabled, 38 % y sin
    // respuesta, AC-HRM-13-04) y clases de estado. Los nombres hrm-search, hrm-retry, hrm-forget, hrm-disconnect y
    // hrm-device-<id> son los keys del legacy (spec §12.8).
    //   #hrm-card .rm-card .rm-hrm-card .rm-hrm-card--<body> [.rm-hrm-card--breathing]   borde superior metric.hr al 60 % fijo
    //   ├── #hrm-glow .rm-card__glow                             brillo en reposo 0,084
    //   ├── .rm-hrm-card__header > #hrm-heart (favorite relleno 14) + #hrm-title .rm-type-label
    //   ├── #hrm-body .rm-hrm-card__body > .rm-hrm-card__body-content   se rearma al cambiar de cuerpo (HeightTween)
    //   └── #hrm-error .rm-hrm-card__error > #hrm-error-human (state.dangerText 12, hasta 2 líneas con "…")
    //                                       + #hrm-error-detail (text.tertiary 11, 1 línea con "…"; no en incompatible)
    // It. 11 (02-tech-design §4.1; A11-02, A11-03; AC-HRM-05-03, -07-02, -07-03, -13-13, -13-14):
    // - Recordado: .rm-hrm-card__identity > #hrm-indicator (LinkIndicator 16: arco de la espera, spinner del intento, link_off sin
    //   reintentos) + #hrm-remembered-name (1 línea con "…") + #hrm-remembered-suffix (" · reconectando..." / " · no encontrado"):
    //   se lee exactamente "{nombre} · sufijo" y el "…" corta solo el nombre. Debajo, las acciones (paridad).
    // - Conectado: + #hrm-stale .rm-hrm-card__stale ("Sin señal del sensor") solo con la lectura vencida.
    // - Check con retención de 240 ms (A10-05) antes de cambiar el cuerpo a Conectado: el spinner de "Conectando" (#hrm-check) o el
    //   indicador del recordado pasan a check; la app ya está conectada en ese cuadro (el HUD lo ve), solo la vista retiene. Es una
    //   animación finita del MotionDriver (Settle la espera).
    // - HeartBreathing pone .rm-hrm-card--breathing (la respiración la anima WP-D).
    // Marcador de rendimiento RowMate.Device.HrmCard. Una sola instancia: DeviceScreenView la mueve entre #hrm-card-slot-connected y
    // #hrm-card-slot-scan.
    public sealed class HeartRateCardView : VisualElement
    {
        public const double CheckHoldMs = 240;

        public static readonly ProfilerMarker Marker = new ProfilerMarker("RowMate.Device.HrmCard");

        private readonly UiContext _ui;
        private readonly HeartbeatAnimator _heartbeat;
        private readonly Label _title;
        private readonly VisualElement _body;
        private readonly VisualElement _content;
        private readonly HeightTween _heightTween;
        private readonly VisualElement _error;
        private readonly Label _errorHuman;
        private readonly Label _errorDetail;
        private readonly TextClamp _errorClamp;
        private readonly CheckHold _hold;
        private readonly HrmMotion.Breath _breath;
        private readonly HrmMotion.Line _staleLine;
        private VisualElement? _scanColumn;
        private HrmCardViewModel? _model;
        private HeartRateState? _hr;
        private BleStatus _rowerStatus = BleStatus.Disconnected;
        private HrmCardBody? _builtBody;
        private string _builtRows = "";
        private bool _holdDone;

        // Elementos del cuerpo vigente (los que cambian sin rearmar).
        private Label? _connectedName;
        private TabularNumber? _bpm;
        private FitBox? _bpmBox;
        private Label? _bpmUnit;
        private Label? _stale;
        private Label? _connectingText;
        private Spinner? _connectingSpinner;
        private LinkIndicator? _connectingCheck;
        private Label? _searchingText;
        private LinkIndicator? _indicator;
        private Label? _rememberedName;
        private Label? _rememberedSuffix;
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

            _error = new VisualElement { name = "hrm-error" };
            _error.AddToClassList("rm-hrm-card__error");
            _error.pickingMode = PickingMode.Ignore;
            _error.style.display = DisplayStyle.None;
            _errorHuman = Text("hrm-error-human", "rm-type-body-xs");
            _errorHuman.AddToClassList("rm-hrm-card__error-human");
            _errorHuman.enableRichText = false;
            _errorHuman.parseEscapeSequences = false;
            _errorClamp = new TextClamp(_errorHuman, 2);
            _error.Add(_errorHuman);
            _errorDetail = Text("hrm-error-detail", "rm-type-caption");
            _errorDetail.AddToClassList("rm-hrm-card__error-detail");
            _errorDetail.AddToClassList("rm-text--tertiary");
            _errorDetail.enableRichText = false;
            _errorDetail.parseEscapeSequences = false;
            _error.Add(_errorDetail);
            Add(_error);
            _hold = new CheckHold(ui.Motion, OnHoldDone);
            _breath = new HrmMotion.Breath(ui.Motion, Heart);
            _staleLine = new HrmMotion.Line(ui.Motion);

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

        // It. 11: el indicador del recordado (null fuera de ese cuerpo) y el check de "Conectando".
        public LinkIndicator? Indicator => _indicator;

        public LinkIndicator? ConnectingCheck => _connectingCheck;

        // La retención de 240 ms del check está corriendo (el estado ya es Connected, el cuerpo todavía no).
        public bool HoldingCheck => _hold.IsRunning;

        // La frase humana del error (sin cortar), o null.
        public string? ErrorFullText => _error.style.display == DisplayStyle.None ? null : _errorClamp.FullText;

        public bool ErrorIsClamped => _errorClamp.IsClamped;

        public Label ErrorHuman => _errorHuman;

        public Label ErrorDetail => _errorDetail;

        public void FinishAnimations()
        {
            _hold.Finish();
            _heightTween.Finish();
            _staleLine.Finish();
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

            using (Marker.Auto())
            {
                var model = HrmCardRules.Build(_hr, _rowerStatus, _ui.Texts);
                Render(model);
            }
        }

        private void OnHoldDone()
        {
            _holdDone = true;
            try
            {
                // WP-D: el cambio de cuerpo arranca en el instante exacto del fin de la retención (no en el cuadro siguiente): con un
                // cuadro largo (o un salto del reloj de prueba) el corazón y el alto no quedan atrasados respecto de "conectado".
                var overrun = _hold.OverrunMs;
                using (_ui.Motion.StartingAt(_ui.Motion.Now - overrun / 1000.0))
                {
                    Rebuild();
                }

                // Lo que arrancó atrasado se pone al día en este mismo cuadro (MotionDriver lo avanza recién en el próximo Tick).
                if (overrun > 0)
                {
                    var now = _ui.Motion.Now;
                    _heightTween.Tick(now);
                    _heartbeat.Tick(now);
                }
            }
            finally
            {
                _holdDone = false;
            }
        }

        private void Render(HrmCardViewModel model)
        {
            _model = model;
            if (_title.text != model.Title)
            {
                _title.text = model.Title;
            }

            // A10-05: con el cuerpo de "Conectando" o del recordado a la vista, el check se ve 240 ms antes de "Conectado".
            if (model.Body == HrmCardBody.Connected && _builtBody != HrmCardBody.Connected)
            {
                if (_hold.IsRunning)
                {
                    return;
                }

                if (!_holdDone && (_builtBody == HrmCardBody.Connecting || _builtBody == HrmCardBody.Remembered) && _ui.Motion.Armed && panel != null
                    && resolvedStyle.display != DisplayStyle.None)
                {
                    ShowCheck();
                    _hold.Play();
                    return;
                }
            }
            else if (_hold.IsRunning)
            {
                _hold.Cancel();
            }

            _heartbeat.SetState(model.HeartFull, model.HeartBeating, model.Bpm);
            EnableInClassList("rm-hrm-card--breathing", model.HeartBreathing);
            // WP-D (D11-05): respira mientras reconecta; al dejar, vuelve al reposo (o HeartbeatAnimator funde a 1 al conectar).
            _breath.Set(model.HeartBreathing, _heartbeat.TargetOpacity, model.HeartFull);

            var rowsKey = RowsKey(model.Rows);
            // WP-D (§1.3): sensores nuevos al final de la lista → solo entran ellos (clave estable = id); las filas que ya estaban no se
            // reaniman ni cambian de lugar. Cualquier otro cambio de la lista rearma el cuerpo.
            if (_builtBody == HrmCardBody.Scanning && model.Body == HrmCardBody.Scanning && rowsKey != _builtRows && _scanColumn != null
                && rowsKey.StartsWith(_builtRows, StringComparison.Ordinal) && _rows.Count < model.Rows.Count)
            {
                AppendRows(model);
                _builtRows = rowsKey;
            }
            else if (_builtBody != model.Body || (model.Body == HrmCardBody.Scanning && rowsKey != _builtRows))
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

            if (model.ErrorHuman == null)
            {
                _error.style.display = DisplayStyle.None;
            }
            else
            {
                _error.style.display = DisplayStyle.Flex;
                if (_errorClamp.FullText != model.ErrorHuman)
                {
                    _errorClamp.SetText(model.ErrorHuman);
                }

                _errorDetail.style.display = model.ErrorDetail == null ? DisplayStyle.None : DisplayStyle.Flex;
                SetText(_errorDetail, model.ErrorDetail ?? "");
            }
        }

        // El indicador vigente pasa a check (círculo 0-160, tilde 80-240; WP-D).
        private void ShowCheck()
        {
            if (_connectingCheck != null)
            {
                if (_connectingSpinner != null)
                {
                    _connectingSpinner.style.display = DisplayStyle.None;
                }

                _connectingCheck.Show(LinkIndicator.Mode.Check);
            }

            _indicator?.Show(LinkIndicator.Mode.Check);
        }

        private void BuildBody(HrmCardViewModel model)
        {
            _content.Clear();
            _rows.Clear();
            _connectedName = _connectingText = _searchingText = _rememberedName = _rememberedSuffix = _noSensorText = _watchHint = _bpmUnit = _stale = null;
            _bpm = null;
            _bpmBox = null;
            _connectingSpinner = null;
            _connectingCheck = null;
            _indicator = null;
            _scanColumn = null;
            _search = _retry = _forget = _disconnect = null;
            _staleLine.Attach(null, false);
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
                        () => hrm.DisconnectAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag));
                    row.Add(_disconnect);
                    _content.Add(row);
                    // AC-HRM-05-03: "Sin señal del sensor" debajo, solo con la lectura vencida.
                    _stale = Text("hrm-stale", "rm-type-body-xs");
                    _stale.AddToClassList("rm-hrm-card__stale");
                    _stale.AddToClassList("rm-text--tertiary");
                    _content.Add(_stale);
                    // WP-D (§2.2): la línea entra y sale con su coreografía; al armar el cuerpo, en el acto.
                    _stale.text = model.StaleText ?? "";
                    _staleLine.Attach(_stale, model.StaleText != null);
                    break;
                }

                case HrmCardBody.Connecting:
                {
                    var row = Container("rm-hrm-connecting");
                    _connectingSpinner = new Spinner(_ui.Motion);
                    row.Add(_connectingSpinner);
                    _connectingCheck = new LinkIndicator(_ui.Motion, 16) { name = "hrm-check" };
                    _connectingCheck.AddToClassList("rm-hrm-card__check");
                    _connectingCheck.Show(LinkIndicator.Mode.None);
                    row.Add(_connectingCheck);
                    _connectingText = Text("hrm-connecting-text", "rm-type-body-m");
                    row.Add(_connectingText);
                    _content.Add(row);
                    break;
                }

                case HrmCardBody.Scanning:
                {
                    var column = Container("rm-hrm-scanning");
                    _scanColumn = column;
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
                    var identity = Container("rm-hrm-card__identity");
                    identity.name = "hrm-identity";
                    _indicator = new LinkIndicator(_ui.Motion, 16) { name = "hrm-indicator" };
                    _indicator.AddToClassList("rm-hrm-card__indicator");
                    identity.Add(_indicator);
                    _rememberedName = Text("hrm-remembered-name", "rm-type-body-m");
                    _rememberedName.AddToClassList("rm-hrm-card__remembered-name");
                    identity.Add(_rememberedName);
                    _rememberedSuffix = Text("hrm-remembered-suffix", "rm-type-body-m");
                    _rememberedSuffix.AddToClassList("rm-hrm-card__remembered-suffix");
                    identity.Add(_rememberedSuffix);
                    _content.Add(identity);
                    var actions = Container("rm-hrm-actions");
                    _retry = new RmButton("hrm-retry", "tonal", Icons.Refresh, model.ConnectLabel, () => hrm.RetryAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag));
                    actions.Add(_retry);
                    _search = new RmButton("hrm-search", "outline", null, model.SearchOtherLabel, () => hrm.StartScanAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag));
                    actions.Add(_search);
                    _forget = new RmButton("hrm-forget", "text", null, model.ForgetLabel, () => hrm.ForgetAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag));
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
                    _search = new RmButton("hrm-search", "tonal", Icons.BluetoothSearching, model.SearchLabel, () => hrm.StartScanAsync().Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag));
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
            if (_stale != null)
            {
                // El texto se conserva mientras la línea sale (fundido de 120).
                if (model.StaleText != null)
                {
                    SetText(_stale, model.StaleText);
                }

                _staleLine.Set(model.StaleText != null);
            }

            SetText(_connectingText, model.ConnectingText);
            SetText(_searchingText, model.SearchingText);
            SetText(_rememberedName, model.RememberedName);
            SetText(_rememberedSuffix, model.RememberedSuffix);
            UpdateIndicator(model);
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

        // A11-02: arco de la espera (se llena hasta NextAttemptAt), spinner del intento o link_off sin reintentos.
        private void UpdateIndicator(HrmCardViewModel model)
        {
            if (_indicator == null)
            {
                return;
            }

            switch (model.Indicator)
            {
                case HrmIndicator.WaitingArc:
                    var duration = model.ArcDuration.TotalSeconds;
                    _indicator.Show(LinkIndicator.Mode.WaitingArc, (model.ArcStartAt ?? 0) + duration, duration);
                    break;
                case HrmIndicator.Spinner:
                    _indicator.Show(LinkIndicator.Mode.Spinner);
                    break;
                case HrmIndicator.LinkOff:
                    _indicator.Show(LinkIndicator.Mode.LinkOff);
                    break;
                default:
                    _indicator.Show(LinkIndicator.Mode.None);
                    break;
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

        // WP-D (§1.3): las filas nuevas entran con la regla del empuje, escalonadas cada 35 ms dentro del mismo lote (8 como máximo). El
        // alto de referencia es el de una fila ya medida (o 56). Sin la tarjeta a la vista, o sin armar, entran en el acto.
        private void AppendRows(HrmCardViewModel model)
        {
            var column = _scanColumn!;
            var animate = _ui.Motion.Armed && BleMotion.IsDisplayed(this);
            var height = _rows.Count > 0 && _rows[0].layout.height > 0 ? _rows[0].layout.height : (float)BleMotionSpecs.HrmRowHeightDp;
            var order = 0;
            for (var i = _rows.Count; i < model.Rows.Count; i++)
            {
                var button = DeviceRow(model.Rows[i]);
                column.Add(button);
                if (animate)
                {
                    var margin = button.resolvedStyle.marginTop > 0 ? button.resolvedStyle.marginTop : 4f;
                    new HrmMotion.Row(_ui.Motion, button).Play(height, margin, BleMotionSpecs.HrmRowDelayMs(order++));
                }
            }
        }

        private Button DeviceRow(HrmDeviceRow row)
        {
            var hrm = _ui.App.HeartRate;
            var id = row.DeviceId;
            var title = row.Title;
            var button = new Button(() => hrm.ConnectAsync(id, title).Forget(_ui.Logger, RowMate.App.Composition.AppContext.HrmTag)) { name = row.ElementName, text = "" };
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

        // La retención del check (A10-05): finita en el MotionDriver, así Settle y la regla de toma la esperan. Cancel la corta sin avisar.
        private sealed class CheckHold : TimedMotion
        {
            private readonly Action _done;
            private bool _cancelled;

            public CheckHold(MotionDriver driver, Action done)
                : base(driver)
            {
                _done = done;
            }

            protected override double TotalMs => CheckHoldMs;

            public void Play()
            {
                _cancelled = false;
                Begin();
            }

            public void Cancel()
            {
                _cancelled = true;
                Finish();
            }

            protected override void OnUpdate(double elapsedMs)
            {
            }

            protected override void OnFinished()
            {
                if (!_cancelled)
                {
                    _done();
                }
            }
        }
    }
}
