using System;
using System.Collections.Generic;
using RowMate.App.History;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.History;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // El detalle de una sesión, una página apilada por encima de la barra de pestañas (F-HIS-09 a -15; AC-HIS-09-01 a -04,
    // AC-HIS-10-01; legacy SessionDetailScreen, lib/features/history/session_detail_screen.dart; 02-tech-design §5.3):
    //   #session-detail .rm-page .rm-session
    //   ├── #session-appbar .rm-appbar: #session-back (arrow_back, ayuda navBack) + título (RoutineName ?? workoutFree, Q-09)
    //   │   + .rm-appbar__actions > #session-export (It. 12: ExportTcxButton, solo con la sesión exportable)
    //   └── #session-body
    //       ├── #session-loading > Spinner 32/3 | #session-no-telemetry (sessionNoTelemetry) | #session-error (sessionLoadError
    //       │   + #session-retry)
    //       └── #session-scroll (ScrollView, relleno 16; UNA columna en todos los tamaños, §10.2)
    //           ├── #session-summary .rm-session-card: sessionSummary 16/700 → 12 → StatRowView (la fila de la tarjeta)
    //           ├── (20) #session-steps .rm-session-card: sessionStepBreakdown → 12 → StepBreakdownView (si hay índices)
    //           ├── (20) #session-metric SegmentedControl --chart (Vatios, SPM, Split 500m, Distancia, Pulso; Vatios siempre)
    //           └── (8)  SessionChartView #session-chart (220)
    // La flecha, "Atrás" de Android y Escape vuelven (IClosesOnEscape); al salir se dispone el store y la lista recarga
    // (AC-HIS-06-02). Las secciones entran en cascada de 35 ms si los datos llegan después del push; si llegan durante el push,
    // entran con la página. Strava no aparece (descartada; la It. 12 es "Exportar TCX").
    // It. 15: una fila de "Rendimiento por paso" se marca y resalta sus franjas (#33, DT15-02; Escape primero desmarca, DT15-03) y la
    // página conserva el primer elemento visible al cambiar el ancho (ScrollAnchor, #82, AC-D15-33; el cursor se oculta en ese cuadro).
    public sealed class SessionDetailPage : IPage, IClosesOnEscape, IHandlesEscape
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly SessionDetailStore _store;
        private readonly AppBarView _appBar;
        private readonly VisualElement _loading;
        private readonly Spinner _spinner;
        private readonly VisualElement _noTelemetry;
        private readonly Label _noTelemetryText;
        private readonly VisualElement _error;
        private readonly Label _errorText;
        private readonly RmButton _retry;
        private readonly Label _summaryTitle;
        private readonly Label _stepsTitle;
        private readonly List<EnterMotion> _cascade = new List<EnterMotion>();
        private IDisposable? _grace;
        private SessionDetailPhase? _phase;
        private bool _pushed;
        private bool _closed;
        private bool _disposed;

        public SessionDetailPage(UiContext ui, ShellLayout layout, HistoryEntry entry)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            _store = ui.App.CreateSessionDetail(entry);
            Root = new VisualElement { name = "session-detail" };
            Root.AddToClassList("rm-session");

            _appBar = new AppBarView("session-appbar", layout);
            _appBar.AddToClassList("rm-appbar--with-back");
            Back = new IconButton("session-back", Icons.ArrowBack, Close);
            Back.AddToClassList("rm-appbar__back");
            Back.SetTooltip(ui, () => _ui.Texts.Get(L10nKeys.NavBack));
            _appBar.Insert(0, Back);
            // It. 12 (12-exportar-tcx/02-tech-design §5.1, DT12-17): "Exportar TCX" a la derecha del título, si la plataforma exporta.
            if (ui.App.Export.Delivery != RowMate.Core.Ports.ExportDelivery.None)
            {
                Export = new ExportTcxButton(ui, ui.App.Export);
                _appBar.Actions.Add(Export.Button);
            }

            Root.Add(_appBar);

            var body = new VisualElement { name = "session-body" };
            body.AddToClassList("rm-session__body");
            body.pickingMode = PickingMode.Ignore;
            Root.Add(body);

            _loading = Center(body, "session-loading");
            _spinner = new Spinner(ui.Motion, 32, 3);
            _spinner.SetShown(false);
            _loading.Add(_spinner);

            _noTelemetry = Center(body, "session-no-telemetry");
            _noTelemetryText = new Label { name = "session-no-telemetry-text" };
            _noTelemetryText.AddToClassList("rm-type-body");
            _noTelemetryText.AddToClassList("rm-session__center-text");
            _noTelemetryText.pickingMode = PickingMode.Ignore;
            _noTelemetry.Add(_noTelemetryText);

            _error = Center(body, "session-error");
            _errorText = new Label { name = "session-error-text" };
            _errorText.AddToClassList("rm-type-body");
            _errorText.AddToClassList("rm-session__error-text");
            _errorText.pickingMode = PickingMode.Ignore;
            _error.Add(_errorText);
            _retry = new RmButton("session-retry", "tonal", Icons.Refresh, "", () => _store.LoadAsync().Forget(_ui.Logger, SessionDetailStore.Tag));
            _error.Add(_retry);

            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "session-scroll" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-session__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            Scroll.style.display = DisplayStyle.None;
            Content = Scroll.contentContainer;
            Content.AddToClassList("rm-session__content");
            body.Add(Scroll);

            Summary = Section("session-summary");
            _summaryTitle = SectionTitle(Summary);
            SummaryStats = new StatRowView("session-summary-stat");
            SummaryStats.AddToClassList("rm-session__stats");
            Summary.Add(SummaryStats);

            Steps = Section("session-steps");
            _stepsTitle = SectionTitle(Steps);
            StepRows = new StepBreakdownView();
            StepRows.RowActivated += OnStepRow;
            Steps.Add(StepRows);

            Selector = new SegmentedControl(false, ui.Motion) { name = "session-metric" };
            Selector.EnableChartVariant();
            Selector.AddToClassList("rm-session__metric");
            var metrics = ChartMetrics.SelectorOrder;
            for (var i = 0; i < metrics.Count; i++)
            {
                Selector.AddOption("session-metric-" + MetricId(metrics[i]), null, "");
            }

            Selector.Select(0, false);
            Selector.Changed += index => _store.SelectMetric(ChartMetrics.SelectorOrder[index]);
            Content.Add(Selector);

            Chart = new SessionChartView(ui.Motion);
            Chart.AddToClassList("rm-session__chart");
            Content.Add(Chart);

            Anchor = ScrollAnchor.Attach(Scroll);
            Content.RegisterCallback<GeometryChangedEvent>(OnContentGeometry);
            layout.RegisterClearance(Scroll, Content, () => ChartSpecs.Padding + 4 + _layout.Insets.Bottom);
            Root.Bind(_store.State, Render);
            Root.Bind(_store.Metric, OnMetric);
            Root.Bind(ui.App.Texts, OnTexts);
            _store.LoadAsync().Forget(_ui.Logger, SessionDetailStore.Tag);
        }

        public VisualElement Root { get; }

        public SessionDetailStore Store => _store;

        public IconButton Back { get; }

        public ScrollView Scroll { get; }

        public VisualElement Content { get; }

        public VisualElement Summary { get; }

        public StatRowView SummaryStats { get; }

        public VisualElement Steps { get; }

        public StepBreakdownView StepRows { get; }

        public SegmentedControl Selector { get; }

        public SessionChartView Chart { get; }

        public ScrollAnchor Anchor { get; }

        // El paso marcado en "Rendimiento por paso" (índice del paso; −1 ninguno). Estado de la vista: se pierde al salir (DT15-02).
        public int SelectedStep { get; private set; } = -1;

        // Marca el paso de esa fila; la misma fila desmarca y otra la reemplaza (AC-D15-02).
        public void ToggleStepRow(int row)
        {
            var step = StepRows.StepIndexAt(row);
            SelectStep(step == SelectedStep ? -1 : step);
        }

        public void SelectStep(int stepIndex)
        {
            SelectedStep = stepIndex;
            StepRows.SetSelected(stepIndex >= 0 ? StepRows.RowOfStep(stepIndex) : -1);
            Chart.SelectStep(stepIndex);
        }

        // Escape: con una fila marcada solo la desmarca (DT15-03); si no, cierra como siempre (IClosesOnEscape).
        public bool TryHandleEscape()
        {
            if (SelectedStep < 0)
            {
                return false;
            }

            SelectStep(-1);
            return true;
        }

        public AppBarView AppBar => _appBar;

        // It. 12: el botón "Exportar TCX" (null si la plataforma no exporta).
        public ExportTcxButton? Export { get; }

        public SessionDetailPhase? Phase => _phase;

        public bool IsClosed => _closed;

        // Salió del árbol (la pantalla del historial suelta su referencia).
        public event Action? Closed;

        public void OnPushed()
        {
            if (_closed || _disposed)
            {
                return;
            }

            _pushed = true;
            _layout.SetSurface(this, OverlaySurface.Stacked());
            if (_phase == SessionDetailPhase.Ready)
            {
                Chart.Begin();
            }
        }

        public void OnPopped()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _grace?.Dispose();
            _spinner.SetShown(false);
            _layout.SetSurface(this, null);
            _layout.UnregisterAppBar(_appBar);
            _layout.UnregisterClearance(Scroll);
            _store.Dispose();
            // Volver del detalle recarga la lista (hs:53-56; AC-HIS-06-02).
            _ui.App.History.LoadAsync().Forget(_ui.Logger, HistoryStore.Tag);
            Closed?.Invoke();
        }

        // "Atrás" de Android y Escape (IClosesOnEscape): vuelven a la lista (AC-HIS-09-01).
        public void OnBackRequested()
        {
            Close();
        }

        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _layout.SetSurface(this, null);
            _ui.Pages.Pop(this);
        }

        private void OnStepRow(int row)
        {
            ToggleStepRow(row);
        }

        // Cambió el ancho del contenido (redimensionar, rotar): el cursor se oculta en ese cuadro (AC-D15-33).
        private float _contentWidth = -1;

        private void OnContentGeometry(GeometryChangedEvent evt)
        {
            var width = evt.newRect.width;
            if (_contentWidth >= 0 && Math.Abs(width - _contentWidth) > 0.5f)
            {
                Chart.Cursor.Hide();
            }

            _contentWidth = width;
        }

        private VisualElement Center(VisualElement body, string id)
        {
            var state = new VisualElement { name = id };
            state.AddToClassList("rm-session__center");
            state.pickingMode = PickingMode.Ignore;
            state.style.display = DisplayStyle.None;
            body.Add(state);
            return state;
        }

        private VisualElement Section(string id)
        {
            var section = new VisualElement { name = id };
            section.AddToClassList("rm-session-card");
            section.pickingMode = PickingMode.Ignore;
            Content.Add(section);
            return section;
        }

        private static Label SectionTitle(VisualElement section)
        {
            var title = new Label { name = section.name + "-title" };
            title.AddToClassList("rm-session-card__title");
            title.pickingMode = PickingMode.Ignore;
            section.Add(title);
            return title;
        }

        private void Render(SessionDetailState state)
        {
            if (_disposed)
            {
                return;
            }

            var previous = _phase;
            _phase = state.Phase;
            SetTitle(state);
            Export?.SetSession(state.Session, state.CanExport);
            _loading.style.display = DisplayStyle.None;
            _noTelemetry.style.display = DisplayStyle.None;
            _error.style.display = DisplayStyle.None;
            switch (state.Phase)
            {
                case SessionDetailPhase.Loading:
                    Scroll.style.display = DisplayStyle.None;
                    StartGrace();
                    return;
                case SessionDetailPhase.NoTelemetry:
                    StopGrace();
                    Scroll.style.display = DisplayStyle.None;
                    _noTelemetry.style.display = DisplayStyle.Flex;
                    return;
                case SessionDetailPhase.LoadFailed:
                    StopGrace();
                    Scroll.style.display = DisplayStyle.None;
                    _error.style.display = DisplayStyle.Flex;
                    return;
            }

            StopGrace();
            Scroll.style.display = DisplayStyle.Flex;
            SummaryStats.Bind(HistoryCardColumn.Row(state.Stats, _ui.Texts));
            StepRows.Bind(state.Steps);
            Steps.style.display = state.HasSteps ? DisplayStyle.Flex : DisplayStyle.None;
            if (state.Chart != null)
            {
                Chart.SetData(state.Chart, _store.Metric.Value);
            }

            if (_pushed)
            {
                Chart.Begin();
                if (previous != SessionDetailPhase.Ready && _ui.Motion.Armed)
                {
                    PlayCascade();
                }
            }
        }

        // Los datos llegaron después del push: las secciones entran escalonadas cada 35 ms (opacidad 0 → 1 e y +12 → 0 en 240).
        private void PlayCascade()
        {
            var sections = new List<VisualElement> { Summary };
            if (Steps.style.display != DisplayStyle.None)
            {
                sections.Add(Steps);
            }

            sections.Add(Selector);
            sections.Add(Chart);
            HistoryMotion.PlayDetailCascade(_ui.Motion, sections, _cascade, false);
        }

        private void StartGrace()
        {
            if (_grace != null)
            {
                return;
            }

            _grace = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.LoadingGraceMs), () =>
            {
                _grace = null;
                if (_phase == SessionDetailPhase.Loading && !_disposed)
                {
                    _loading.style.display = DisplayStyle.Flex;
                    _spinner.SetShown(true);
                }
            });
        }

        private void StopGrace()
        {
            _grace?.Dispose();
            _grace = null;
            _spinner.SetShown(false);
        }

        private void OnMetric(ChartMetric metric)
        {
            var order = ChartMetrics.SelectorOrder;
            for (var i = 0; i < order.Count; i++)
            {
                if (order[i] == metric)
                {
                    Selector.Select(i, false);
                }
            }

            Chart.SetMetric(metric);
        }

        private void SetTitle(SessionDetailState state)
        {
            _appBar.SetTitle(state.Title(_ui.Texts));
        }

        private void OnTexts(Localizer texts)
        {
            SetTitle(_store.State.Value);
            _noTelemetryText.text = texts.Get(L10nKeys.SessionNoTelemetry);
            _errorText.text = texts.Get(L10nKeys.SessionLoadError);
            _retry.SetLabel(texts.Get(L10nKeys.Retry));
            _summaryTitle.text = texts.Get(L10nKeys.SessionSummary);
            _stepsTitle.text = texts.Get(L10nKeys.SessionStepBreakdown);
            var order = ChartMetrics.SelectorOrder;
            for (var i = 0; i < order.Count; i++)
            {
                Selector.SetLabel(i, texts.Get(ChartMetrics.LabelKey(order[i])));
            }

            var state = _store.State.Value;
            if (state.Phase == SessionDetailPhase.Ready)
            {
                SummaryStats.Bind(HistoryCardColumn.Row(state.Stats, texts));
            }
        }

        private static string MetricId(ChartMetric metric)
        {
            switch (metric)
            {
                case ChartMetric.Watts:
                    return "watts";
                case ChartMetric.Spm:
                    return "spm";
                case ChartMetric.Split:
                    return "split";
                case ChartMetric.Distance:
                    return "distance";
                default:
                    return "hr";
            }
        }
    }
}
