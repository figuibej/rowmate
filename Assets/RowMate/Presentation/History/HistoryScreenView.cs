using System;
using RowMate.App.History;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // Pestaña Historial (F-HIS-06, F-HIS-07; legacy HistoryScreen, lib/features/history/history_screen.dart; 02-tech-design §5.1
    // de la It. 9). Reemplaza el contenido provisorio de AC-APP-02-08:
    //   #screen-history .rm-screen .rm-history
    //   ├── #history-appbar .rm-appbar                 historyTitle, sin acciones (sin Strava: It. 12)
    //   └── #history-body .rm-history__body            los estados, centrados entre las dos barras
    //       ├── #history-loading > Spinner 32/3       solo en la primera carga sin datos, tras 120 ms de gracia y fundido de 160
    //       ├── #history-empty .rm-empty               history 56 @12 %, 12, historyEmpty en text.tertiary (AC-HIS-07-01)
    //       ├── #history-error                         history 56 @12 % (It. 14, #37) + historyLoadError + #history-retry (tonal; Q-13, DV-95, AC-HIS-06-04)
    //       └── HistoryListView (#history-list)
    // Con una lista a la vista el indicador nunca vuelve (HistoryStore no vuelve a Loading) y una recarga fallida deja la lista
    // igual y muestra el aviso historyLoadError de 4 s (HistoryStore.ReloadFailed → ToastLayer). Tocar una tarjeta apila el
    // detalle (SessionDetailPage), que al salir pide otra recarga (AC-HIS-06-02).
    public sealed class HistoryScreenView : VisualElement
    {
        public const string ScreenName = "screen-history";

        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly int _tabIndex;
        private readonly AppBarView _appBar;
        private readonly VisualElement _body;
        private readonly VisualElement _loading;
        private readonly Spinner _spinner;
        private readonly VisualElement _error;
        private readonly Label _errorText;
        private readonly RmButton _retry;
        private readonly VisualElement _empty;
        private readonly Label _emptyTitle;
        private readonly EnterMotion _loadingFade;
        private readonly EnterMotion _stateEnter;
        private IDisposable? _grace;
        private HistoryPhase? _phase;
        private bool _shownLoading;

        public HistoryScreenView(UiContext ui, ShellLayout layout, int tabIndex)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _tabIndex = tabIndex;
            name = ScreenName;
            AddToClassList("rm-screen");
            AddToClassList("rm-history");

            _appBar = new AppBarView("history-appbar", layout);
            Add(_appBar);

            _body = new VisualElement { name = "history-body" };
            _body.AddToClassList("rm-history__body");
            _body.pickingMode = PickingMode.Ignore;
            Add(_body);

            _loading = State("history-loading");
            _spinner = new Spinner(ui.Motion, 32, 3);
            _spinner.SetShown(false);
            _loading.Add(_spinner);
            _loadingFade = new EnterMotion(ui.Motion);

            _error = State("history-error");
            // It. 14 (#37, AC-DEBT-08): el error lleva el mismo ícono de 56 al 12 % que el vacío, 12 encima del texto (CAP-25).
            var errorIcon = new IconView(Icons.History, false, 56) { name = "history-error-icon" };
            errorIcon.AddToClassList("rm-empty__icon");
            errorIcon.AddToClassList("rm-history__error-icon");
            _error.Add(errorIcon);
            _errorText = new Label { name = "history-error-text" };
            _errorText.AddToClassList("rm-history__error-text");
            _errorText.AddToClassList("rm-type-body");
            _errorText.pickingMode = PickingMode.Ignore;
            _error.Add(_errorText);
            _retry = new RmButton("history-retry", "tonal", Icons.Refresh, "", () => _ui.App.History.LoadAsync().Forget(_ui.Logger, HistoryStore.Tag));
            _error.Add(_retry);

            _empty = State("history-empty");
            _empty.AddToClassList("rm-empty");
            var emptyIcon = new IconView(Icons.History, false, 56) { name = "history-empty-icon" };
            emptyIcon.AddToClassList("rm-empty__icon");
            emptyIcon.AddToClassList("rm-history__empty-icon");
            _empty.Add(emptyIcon);
            _emptyTitle = new Label { name = "history-empty-text" };
            _emptyTitle.AddToClassList("rm-empty__title");
            _emptyTitle.AddToClassList("rm-history__empty-text");
            _emptyTitle.AddToClassList("rm-type-body");
            _emptyTitle.pickingMode = PickingMode.Ignore;
            _empty.Add(_emptyTitle);
            _stateEnter = new EnterMotion(ui.Motion);

            List = new HistoryListView(ui, Open);
            List.style.display = DisplayStyle.None;
            _body.Add(List);
            layout.RegisterClearance(List.Scroll, List.Content, () => _layout.CurrentTabBarHeight + ShellLayout.ClearanceGapDp);

            layout.SetSurface(this, OverlaySurface.Tab(layout.CurrentTabBarHeight));
            this.Bind(ui.App.History.State, Render);
            this.Bind(ui.App.Texts, OnTexts);
            ui.App.History.ReloadFailed += OnReloadFailed;
        }

        public HistoryListView List { get; }

        public AppBarView AppBar => _appBar;

        public Spinner Spinner => _spinner;

        public HistoryPhase? Phase => _phase;

        // El detalle abierto desde esta pantalla (el último; null después de salir).
        public SessionDetailPage? Detail { get; private set; }

        // Historial a la vista: es la pestaña elegida y ninguna página la tapa.
        public bool IsOnScreen => IsSelected && !_ui.Pages.CoversShell;

        private bool IsSelected => _ui.App.Shell.SelectedIndex.Value == _tabIndex;

        private VisualElement State(string id)
        {
            var state = new VisualElement { name = id };
            state.AddToClassList("rm-history__center");
            state.pickingMode = PickingMode.Ignore;
            state.style.display = DisplayStyle.None;
            _body.Add(state);
            _layout.RegisterBottomPadding(state, 0);
            return state;
        }

        private void Render(HistoryState state)
        {
            var previous = _phase;
            _phase = state.Phase;
            var visible = IsOnScreen && _ui.Motion.Armed;
            switch (state.Phase)
            {
                case HistoryPhase.Loading:
                    _error.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    List.style.display = DisplayStyle.None;
                    if (previous != HistoryPhase.Loading)
                    {
                        StartGrace();
                    }

                    return;
                case HistoryPhase.LoadFailed:
                    StopLoading();
                    List.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    _error.style.display = DisplayStyle.Flex;
                    if (previous != HistoryPhase.LoadFailed && visible)
                    {
                        _stateEnter.Play(_error, 0, MotionSpecs.ListItemInMs, MotionSpecs.EmptyInShiftDp);
                    }

                    return;
            }

            StopLoading();
            _error.style.display = DisplayStyle.None;
            List.Render(state.Entries, IsSelected, visible && previous == HistoryPhase.Loading);
            if (state.Entries.Count > 0)
            {
                List.style.display = DisplayStyle.Flex;
                _empty.style.display = DisplayStyle.None;
                return;
            }

            List.style.display = DisplayStyle.None;
            var wasEmpty = _empty.style.display == DisplayStyle.Flex;
            _empty.style.display = DisplayStyle.Flex;
            if (!wasEmpty && visible)
            {
                _stateEnter.Play(_empty, 0, MotionSpecs.ListItemInMs, MotionSpecs.EmptyInShiftDp);
            }
        }

        // Cargando (como Rutinas, DN3-15): nada durante 120 ms; después el indicador funde en 160.
        private void StartGrace()
        {
            _grace?.Dispose();
            _shownLoading = false;
            _loading.style.display = DisplayStyle.None;
            _spinner.SetShown(false);
            _grace = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.LoadingGraceMs), () =>
            {
                _grace = null;
                if (_phase != HistoryPhase.Loading)
                {
                    return;
                }

                _shownLoading = true;
                _loading.style.display = DisplayStyle.Flex;
                _spinner.SetShown(true);
                _loadingFade.Play(_loading, 0, Tokens.DurFast.Milliseconds, 0);
            });
        }

        private void StopLoading()
        {
            _grace?.Dispose();
            _grace = null;
            if (_shownLoading)
            {
                _loadingFade.Finish();
                _shownLoading = false;
            }

            _loading.style.display = DisplayStyle.None;
            _spinner.SetShown(false);
        }

        private void OnTexts(Localizer texts)
        {
            _appBar.SetTitle(texts.Get(L10nKeys.HistoryTitle));
            _errorText.text = texts.Get(L10nKeys.HistoryLoadError);
            _retry.SetLabel(texts.Get(L10nKeys.Retry));
            _emptyTitle.text = texts.Get(L10nKeys.HistoryEmpty);
            List.RenderTexts();
        }

        // Una recarga falló con la lista a la vista: la lista queda igual y el aviso de 4 s (DT9-06).
        private void OnReloadFailed()
        {
            _ui.Toasts.Show(_ui.Texts.Get(L10nKeys.HistoryLoadError));
        }

        private void Open(HistoryCardView card)
        {
            if (_ui.Pages.HasPages || card.Entry == null)
            {
                return;
            }

            var page = new SessionDetailPage(_ui, _layout, card.Entry);
            _ui.Pages.Push(page);
            if (!_ui.Pages.HasPage(page))
            {
                page.OnPopped();
                return;
            }

            Detail = page;
            page.Closed += () =>
            {
                if (ReferenceEquals(Detail, page))
                {
                    Detail = null;
                }
            };
        }
    }
}
