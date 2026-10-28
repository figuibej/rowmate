using System;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Pestaña Rutinas (F-RUT-10; legacy RoutinesScreen, lib/features/routines/routines_screen.dart; 03-rutinas/02-tech-design
    // §4.3; 04-design-notes §4.1):
    //   #screen-routines .rm-screen .rm-routines
    //   ├── #routines-appbar .rm-appbar               routinesTitle, sin acciones (AC-RUT-10-01)
    //   ├── #routines-body .rm-routines__body        los estados, centrados entre las dos barras
    //   │   ├── #routines-loading > .rm-spinner      solo el indicador de 32 / 3, tras 120 ms de gracia y fundido de 160 (DN3-15)
    //   │   ├── #routines-error                       error 48 + routinesLoadError + #routines-retry (tonal): Q-13, AC-RUT-09-04
    //   │   ├── #routines-empty .rm-empty             fitness_center 56 @12 %, routinesEmpty, routinesEmptyHint (AC-RUT-10-02)
    //   │   └── RoutineListView (#routines-list)      Ready con rutinas
    //   └── #routines-add .rm-fab                     add; right 16, bottom = alto real de la barra + 16 (AC-RUT-10-04)
    // Los tres estados se alternan con display (nunca un indicador eterno: Q-13); el + se ve en los tres. Mientras Rutinas es
    // la pestaña elegida y ninguna página la tapa, el + es un obstáculo inferior (la llave del simulador sube sobre él y el
    // aviso lo esquiva: DV-13, DN3-13, DN3-14).
    // Tocar una tarjeta (o ⋮ → Editar) abre el editor de esa rutina; + abre uno nuevo (AC-RUT-10-04, -05, -06). Eliminar abre
    // DeleteRoutineDialog; el borrado no espera dentro del diálogo (routines_screen.dart:115) y si falla, aviso
    // routineDeleteError (AC-RUT-09-06).
    public sealed class RoutinesScreenView : VisualElement
    {
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
        private readonly Label _emptyHint;
        private readonly EnterMotion _loadingFade;
        private readonly EnterMotion _stateEnter;
        private IDisposable? _grace;
        private RoutinesPhase? _phase;
        private bool _shownLoading;

        public RoutinesScreenView(UiContext ui, ShellLayout layout, int tabIndex)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _tabIndex = tabIndex;
            name = "screen-routines";
            AddToClassList("rm-screen");
            AddToClassList("rm-routines");

            _appBar = new AppBarView("routines-appbar", layout);
            Add(_appBar);

            _body = new VisualElement { name = "routines-body" };
            _body.AddToClassList("rm-routines__body");
            _body.pickingMode = PickingMode.Ignore;
            Add(_body);

            _loading = State("routines-loading", "rm-routines__center");
            _spinner = new Spinner(ui.Motion, 32, 3);
            _loading.Add(_spinner);
            _loadingFade = new EnterMotion(ui.Motion);

            _error = State("routines-error", "rm-routines__center");
            var errorIcon = new IconView(Icons.Error, false, 48) { name = "routines-error-icon" };
            errorIcon.AddToClassList("rm-routines__error-icon");
            _error.Add(errorIcon);
            _errorText = new Label { name = "routines-error-text" };
            _errorText.AddToClassList("rm-routines__error-text");
            _errorText.AddToClassList("rm-type-body");
            _errorText.pickingMode = PickingMode.Ignore;
            _error.Add(_errorText);
            _retry = new RmButton("routines-retry", "tonal", Icons.Refresh, "", () => _ = _ui.App.Routines.LoadAsync());
            _error.Add(_retry);

            _empty = State("routines-empty", "rm-routines__center");
            _empty.AddToClassList("rm-empty");
            var emptyIcon = new IconView(Icons.FitnessCenter, false, 56);
            emptyIcon.AddToClassList("rm-empty__icon");
            emptyIcon.AddToClassList("rm-routines__empty-icon");
            _empty.Add(emptyIcon);
            _emptyTitle = new Label();
            _emptyTitle.AddToClassList("rm-empty__title");
            _emptyTitle.AddToClassList("rm-type-body");
            _emptyTitle.pickingMode = PickingMode.Ignore;
            _empty.Add(_emptyTitle);
            _emptyHint = new Label();
            _emptyHint.AddToClassList("rm-empty__hint");
            _emptyHint.AddToClassList("rm-type-body-s");
            _emptyHint.AddToClassList("rm-text--tertiary");
            _emptyHint.pickingMode = PickingMode.Ignore;
            _empty.Add(_emptyHint);
            _stateEnter = new EnterMotion(ui.Motion);

            List = new RoutineListView(ui, OpenEditor, OpenMenu);
            List.style.display = DisplayStyle.None;
            _body.Add(List);
            layout.RegisterClearance(List.Scroll, List.Content, ListClearanceMinimum);

            AddButton = new Fab("routines-add", Icons.Add, () => OpenEditor(null));
            Add(AddButton);

            layout.Changed += PlaceFab;
            _ui.Pages.CoversShellChanged += _ => UpdateObstacle();
            RegisterCallback<GeometryChangedEvent>(_ => PlaceFab());
            this.Bind(ui.App.Shell.SelectedIndex, _ => UpdateObstacle());
            this.Bind(ui.App.Routines.State, Render);
            this.Bind(ui.App.Texts, OnTexts);
            PlaceFab();
        }

        public RoutineListView List { get; }

        public Fab AddButton { get; }

        public AppBarView AppBar => _appBar;

        public Spinner Spinner => _spinner;

        public RoutinesPhase? Phase => _phase;

        // Rutinas a la vista: es la pestaña elegida y ninguna página la tapa.
        public bool IsOnScreen => _ui.App.Shell.SelectedIndex.Value == _tabIndex && !_ui.Pages.CoversShell;

        private VisualElement State(string id, string className)
        {
            var state = new VisualElement { name = id };
            state.AddToClassList(className);
            state.pickingMode = PickingMode.Ignore;
            state.style.display = DisplayStyle.None;
            _body.Add(state);
            // Centrado entre las dos barras: el relleno inferior es el alto real de la barra (DN3-15).
            _layout.RegisterBottomPadding(state, 0);
            return state;
        }

        private void Render(RoutinesState state)
        {
            var previous = _phase;
            _phase = state.Phase;
            var visible = IsOnScreen && _ui.Motion.Armed;
            switch (state.Phase)
            {
                case RoutinesPhase.Loading:
                    _error.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    List.style.display = DisplayStyle.None;
                    if (previous != RoutinesPhase.Loading)
                    {
                        StartGrace();
                    }

                    return;
                case RoutinesPhase.LoadFailed:
                    StopLoading();
                    List.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    _error.style.display = DisplayStyle.Flex;
                    if (previous != RoutinesPhase.LoadFailed && visible)
                    {
                        _stateEnter.Play(_error, 0, MotionSpecs.ListItemInMs, MotionSpecs.EmptyInShiftDp);
                    }

                    return;
            }

            StopLoading();
            _error.style.display = DisplayStyle.None;
            var hasRoutines = state.Routines.Count > 0;
            List.Render(state.Routines, visible && previous == RoutinesPhase.Loading);
            List.style.display = hasRoutines ? DisplayStyle.Flex : DisplayStyle.None;
            var showEmpty = !hasRoutines;
            var wasEmpty = _empty.resolvedStyle.display != DisplayStyle.None && _empty.style.display != DisplayStyle.None;
            _empty.style.display = showEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            if (showEmpty && !wasEmpty && visible)
            {
                _stateEnter.Play(_empty, 0, MotionSpecs.ListItemInMs, MotionSpecs.EmptyInShiftDp);
            }
        }

        // Cargando (DN3-15): nada durante 120 ms (una carga corta no deja un parpadeo); después el indicador funde en 160.
        private void StartGrace()
        {
            _grace?.Dispose();
            _shownLoading = false;
            _loading.style.display = DisplayStyle.None;
            _grace = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.LoadingGraceMs), () =>
            {
                _grace = null;
                if (_phase != RoutinesPhase.Loading)
                {
                    return;
                }

                _shownLoading = true;
                _loading.style.display = DisplayStyle.Flex;
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
        }

        private void OnTexts(Localizer texts)
        {
            _appBar.SetTitle(texts.Get(L10nKeys.RoutinesTitle));
            _errorText.text = texts.Get(L10nKeys.RoutinesLoadError);
            _retry.SetLabel(texts.Get(L10nKeys.Retry));
            _emptyTitle.text = texts.Get(L10nKeys.RoutinesEmpty);
            _emptyHint.text = texts.Get(L10nKeys.RoutinesEmptyHint);
            List.RenderTexts();
        }

        private void OpenEditor(Routine? routine)
        {
            if (_ui.Pages.HasPages)
            {
                return;
            }

            var page = new RoutineEditorPage(_ui, _layout, routine);
            _ui.Pages.Push(page);
            if (!_ui.Pages.HasPage(page))
            {
                page.OnPopped();
            }
        }

        private void OpenMenu(Routine routine, VisualElement anchor)
        {
            var texts = _ui.Texts;
            var menu = new PopupMenu(false);
            menu.AddItem("routine-menu-edit", texts.Get(L10nKeys.RoutineEdit), false, () => OpenEditor(routine));
            menu.AddItem("routine-menu-delete", texts.Get(L10nKeys.RoutineDelete), true, () => ConfirmDelete(routine));
            _ui.Popups.ShowMenu(menu, anchor);
        }

        private void ConfirmDelete(Routine routine)
        {
            var dialog = new DeleteRoutineDialog(_ui, routine);
            dialog.Closed += closed =>
            {
                if (!closed.WasConfirmed || !routine.Id.HasValue)
                {
                    return;
                }

                _ = Delete(routine.Id.Value);
            };
            _ui.Dialogs.Open(dialog);
        }

        // El borrado no espera dentro del diálogo; si falla, la tarjeta sigue y aparece el aviso (AC-RUT-09-06).
        private async System.Threading.Tasks.Task Delete(long id)
        {
            var ok = await _ui.App.Routines.DeleteAsync(id);
            if (!ok)
            {
                _ui.Toasts.Show(RoutineTexts.DeleteFailedToast(_ui.Texts));
            }
        }

        private void PlaceFab()
        {
            AddButton.style.bottom = _layout.CurrentTabBarHeight + Fab.GapAboveTabBarDp;
            UpdateObstacle();
        }

        // Relleno inferior mínimo de la lista (AC-RUT-10-09): nunca menos que el del + (barra + 16 + 56 + 16 = barra + 88,
        // 04-design-notes §4.12), esté o no registrado como obstáculo. Fuera de la pestaña no se achica: mientras sale, la
        // lista todavía se ve y un contenido más corto movería el desplazamiento (al volver, la lista donde estaba).
        private double ListClearanceMinimum()
        {
            var normal = _layout.CurrentTabBarHeight + Fab.GapAboveTabBarDp + Fab.SizeDp + ShellLayout.ClearanceGapDp;
            if (_ui.App.Shell.SelectedIndex.Value == _tabIndex)
            {
                return normal;
            }

            var current = List.Content.style.paddingBottom;
            return current.keyword == StyleKeyword.Undefined ? Math.Max(normal, current.value.value) : normal;
        }

        // El + es obstáculo mientras Rutinas es la pestaña elegida y ninguna página la tapa (DN3-14).
        private void UpdateObstacle()
        {
            var width = _layout.WindowSize.x;
            if (_ui.App.Shell.SelectedIndex.Value == _tabIndex && !_ui.Pages.CoversShell && width > 0)
            {
                _layout.SetBottomObstacle(this, Fab.ObstacleFor(width, _layout.CurrentTabBarHeight + Fab.GapAboveTabBarDp));
            }
            else
            {
                _layout.SetBottomObstacle(this, null);
            }
        }
    }
}
