using System;
using RowMate.App.Routines;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Pestaña Workout (F-WKT-12, F-BLE-10; legacy WorkoutScreen, lib/features/workout/workout_screen.dart:19-201;
    // 02-tech-design §6.1 de la It. 4a; 04-design-notes §4.1):
    //   #screen-workout .rm-screen .rm-workout
    //   ├── #workout-appbar .rm-appbar                 workoutTitle, sin acciones; SOLO en reposo (fuera, display: none)
    //   ├── #workout-rest .rm-workout__rest            solo con la sesión en reposo
    //   │   └── #workout-scroll (ScrollView) > .rm-workout__content   relleno 16; TODO se desplaza junto (DV-22)
    //   │       ├── .rm-slot > #workout-banner         sin remo: warning 18 + 8 + workoutRowerNotConnected 13 (crece y colapsa)
    //   │       ├── #workout-free                      primario, ancho completo, min-height 56: play_arrow + workoutFree
    //   │       ├── .rm-workout__gap                   24
    //   │       ├── #workout-routines-label            "␣␣O elegí una rutina:" (solo con rutinas), 8 abajo
    //   │       ├── #workout-loading | #workout-error | #workout-empty    los tres estados, centrados en lo que queda
    //   │       └── #workout-routines                  tarjetas de Rutinas con chevron (WorkoutRoutineListView)
    //   └── #workout-in-progress                      fuera de reposo: workoutInProgress centrado en toda la pestaña
    // Lee WorkoutRestState (WorkoutStore) y reenvía gestos. Un toque, una sesión (AC-WKT-01-10, DV-30): StartFree y
    // StartRoutine devuelven false fuera de reposo y la vista de reposo sale del árbol visible en el acto. El inmersivo NO se
    // abre por el toque sino por el estado: cuando la fase deja Idle y no hay un ImmersiveWorkoutPage en la pila que no esté
    // saliendo (Immersive), se apila (PageStack.Push con fundido). Los avisos de Workout (WorkoutStore.Toasts:
    // workoutRoutineCannotStart, workoutSaveError, workoutCompleted) van al ToastLayer.
    public sealed class WorkoutScreenView : VisualElement
    {
        public const string ScreenName = "screen-workout";

        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly int _tabIndex;
        private readonly AppBarView _appBar;
        private readonly VisualElement _rest;
        private readonly VisualElement _content;
        private readonly VisualElement _bannerSlot;
        private readonly VisualElement _banner;
        private readonly Label _bannerText;
        private readonly Label _label;
        private readonly VisualElement _loading;
        private readonly Spinner _spinner;
        private readonly VisualElement _error;
        private readonly Label _errorText;
        private readonly Label _empty;
        private readonly Label _inProgress;
        private readonly GrowMotion _bannerGrow;
        private readonly CollapseMotion _bannerCollapse;
        private readonly EnterMotion _loadingFade;
        private IDisposable? _grace;
        private RoutinesPhase? _phase;
        private bool _shownLoading;
        private bool? _bannerShown;
        private ImmersiveWorkoutPage? _immersive;

        public WorkoutScreenView(UiContext ui, ShellLayout layout, int tabIndex)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _tabIndex = tabIndex;
            name = ScreenName;
            AddToClassList("rm-screen");
            AddToClassList("rm-workout");

            _appBar = new AppBarView("workout-appbar", layout);
            Add(_appBar);

            _rest = new VisualElement { name = "workout-rest" };
            _rest.AddToClassList("rm-workout__rest");
            _rest.pickingMode = PickingMode.Ignore;
            Add(_rest);

            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "workout-scroll" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-workout__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            _content = Scroll.contentContainer;
            _content.AddToClassList("rm-workout__content");
            Scroller = new FocusScroller(ui, Scroll);
            Scroller.SetBottomInset(() => _ui.Layout.TabBarVisible ? _ui.Layout.CurrentTabBarHeight : 0);
            _rest.Add(Scroll);
            // El contenido mide como mínimo el visor: los estados crecen y se centran en lo que queda debajo del botón.
            Scroll.contentViewport.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var height = evt.newRect.height;
                if (height > 0)
                {
                    _content.style.minHeight = height;
                }
            });

            _bannerSlot = new VisualElement { name = "workout-banner-slot" };
            _bannerSlot.AddToClassList("rm-slot");
            _bannerSlot.pickingMode = PickingMode.Ignore;
            _banner = new VisualElement { name = "workout-banner" };
            _banner.AddToClassList("rm-workout__banner");
            _banner.pickingMode = PickingMode.Ignore;
            var warning = new IconView(Icons.Warning, false, 18) { name = "workout-banner-icon" };
            warning.AddToClassList("rm-workout__banner-icon");
            _banner.Add(warning);
            _bannerText = new Label { name = "workout-banner-text" };
            _bannerText.AddToClassList("rm-workout__banner-text");
            _bannerText.AddToClassList("rm-type-body-s");
            _bannerText.pickingMode = PickingMode.Ignore;
            _banner.Add(_bannerText);
            _bannerSlot.Add(_banner);
            _bannerSlot.style.display = DisplayStyle.None;
            _content.Add(_bannerSlot);
            _bannerGrow = new GrowMotion(ui.Motion, _bannerSlot, _banner);
            _bannerCollapse = new CollapseMotion(ui.Motion);

            FreeButton = new RmButton("workout-free", "primary tall", Icons.PlayArrow, "", OnFree);
            FreeButton.AddToClassList("rm-workout__free");
            _content.Add(FreeButton);

            var gap = new VisualElement();
            gap.AddToClassList("rm-workout__gap");
            gap.pickingMode = PickingMode.Ignore;
            _content.Add(gap);

            _label = new Label { name = "workout-routines-label" };
            _label.AddToClassList("rm-workout__label");
            _label.AddToClassList("rm-type-body-s");
            _label.pickingMode = PickingMode.Ignore;
            _label.style.display = DisplayStyle.None;
            _content.Add(_label);

            _loading = new VisualElement { name = "workout-loading" };
            _loading.AddToClassList("rm-workout__state");
            _loading.pickingMode = PickingMode.Ignore;
            _loading.style.display = DisplayStyle.None;
            _spinner = new Spinner(ui.Motion, 32, 3);
            // Gira solo a la vista (StartGrace / StopLoading): oculto no pide 60 fps (hallazgo de la corrección de QA-V4a-02).
            _spinner.SetShown(false);
            _loading.Add(_spinner);
            _content.Add(_loading);
            _loadingFade = new EnterMotion(ui.Motion);

            _error = new VisualElement { name = "workout-error" };
            _error.AddToClassList("rm-workout__state");
            _error.pickingMode = PickingMode.Ignore;
            _error.style.display = DisplayStyle.None;
            var errorIcon = new IconView(Icons.Error, false, 48) { name = "workout-error-icon" };
            errorIcon.AddToClassList("rm-workout__error-icon");
            _error.Add(errorIcon);
            _errorText = new Label { name = "workout-error-text" };
            _errorText.AddToClassList("rm-workout__error-text");
            _errorText.AddToClassList("rm-type-body");
            _errorText.pickingMode = PickingMode.Ignore;
            _error.Add(_errorText);
            RetryButton = new RmButton("workout-retry", "tonal", Icons.Refresh, "", () => _ui.App.Routines.LoadAsync().Forget(_ui.Logger, RoutinesStore.Tag));
            _error.Add(RetryButton);
            _content.Add(_error);

            _empty = new Label { name = "workout-empty" };
            _empty.AddToClassList("rm-workout__empty");
            _empty.AddToClassList("rm-type-body");
            _empty.pickingMode = PickingMode.Ignore;
            _empty.style.display = DisplayStyle.None;
            _content.Add(_empty);

            List = new WorkoutRoutineListView(ui, OnRoutineTapped);
            List.style.display = DisplayStyle.None;
            _content.Add(List);

            _inProgress = new Label { name = "workout-in-progress" };
            _inProgress.AddToClassList("rm-workout__in-progress");
            _inProgress.AddToClassList("rm-type-body");
            _inProgress.pickingMode = PickingMode.Ignore;
            _inProgress.style.display = DisplayStyle.None;
            Add(_inProgress);
            // Centrado entre el borde de arriba y la barra de pestañas: abajo, el alto real de la barra (se conserva oculta).
            layout.RegisterBottomPadding(_inProgress, 0);

            // El relleno de abajo: la barra + el obstáculo más alto que se cruza + 16 (la llave del simulador cuenta: R5).
            layout.RegisterClearance(Scroll, _content, () => _layout.CurrentTabBarHeight + ShellLayout.ClearanceGapDp);

            this.Bind(ui.App.WorkoutRest.State, Render);
            this.Bind(ui.App.WorkoutRest.Toasts, OnToast);
            this.Bind(ui.App.Texts, OnTexts);
        }

        public ScrollView Scroll { get; }

        public FocusScroller Scroller { get; }

        public RmButton FreeButton { get; }

        public RmButton RetryButton { get; }

        public WorkoutRoutineListView List { get; }

        public AppBarView AppBar => _appBar;

        public VisualElement Banner => _banner;

        public Label InProgress => _inProgress;

        public Spinner Spinner => _spinner;

        // El inmersivo que abrió esta pestaña (el de la sesión en curso), si sigue en la pila y NO está saliendo. La página que
        // sale ya no cuenta (QA-V4a-01, QA-V4a-ROB-01): una sesión que empieza durante su fundido de salida (200 ms, 120 con
        // reducir movimiento; la vista de reposo ya responde debajo, ExitLock) apila su propio inmersivo en el mismo toque, como
        // el legacy, que apila una ruta por cada inicio (workout_screen.dart:88-89, :164-169). PageStack.Push acepta una página
        // nueva mientras otra sale; la vieja termina su fundido debajo y se dispone sola (OnPopped).
        public ImmersiveWorkoutPage? Immersive => _immersive != null && _ui.Pages.HasPage(_immersive) && !_ui.Pages.IsExiting(_immersive)
            ? _immersive
            : null;

        // Workout a la vista: es la pestaña elegida y ninguna página la tapa.
        public bool IsOnScreen => _ui.App.Shell.SelectedIndex.Value == _tabIndex && !_ui.Pages.CoversShell;

        // El diálogo de inicio abierto (null si no hay).
        public StartWorkoutDialog? StartDialog { get; private set; }

        private void Render(WorkoutRestState state)
        {
            var idle = state.IsIdle;
            var display = idle ? DisplayStyle.Flex : DisplayStyle.None;
            _appBar.style.display = display;
            _rest.style.display = display;
            _inProgress.style.display = idle ? DisplayStyle.None : DisplayStyle.Flex;
            if (!idle)
            {
                OpenImmersive();
            }

            ShowBanner(!state.RowerConnected);
            FreeButton.SetEnabled(state.CanStart);
            RenderRoutines(state);
        }

        private void RenderRoutines(WorkoutRestState state)
        {
            var previous = _phase;
            _phase = state.RoutinesPhase;
            switch (state.RoutinesPhase)
            {
                case RoutinesPhase.Loading:
                    _label.style.display = DisplayStyle.None;
                    List.style.display = DisplayStyle.None;
                    _error.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    if (previous != RoutinesPhase.Loading)
                    {
                        StartGrace();
                    }

                    return;
                case RoutinesPhase.LoadFailed:
                    StopLoading();
                    _label.style.display = DisplayStyle.None;
                    List.style.display = DisplayStyle.None;
                    _empty.style.display = DisplayStyle.None;
                    _error.style.display = DisplayStyle.Flex;
                    return;
            }

            StopLoading();
            _error.style.display = DisplayStyle.None;
            var hasRoutines = state.Routines.Count > 0;
            List.Render(state.Routines, state.CanStart);
            _label.style.display = hasRoutines ? DisplayStyle.Flex : DisplayStyle.None;
            List.style.display = hasRoutines ? DisplayStyle.Flex : DisplayStyle.None;
            _empty.style.display = hasRoutines ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // El aviso sin remo (AC-BLE-10-03, -05): crece y colapsa en vivo con los verbos de la It. 3 (04-design-notes §5.8); sin
        // la pestaña a la vista o sin armar, en el acto.
        private void ShowBanner(bool shown)
        {
            if (_bannerShown == shown)
            {
                return;
            }

            var first = _bannerShown == null;
            _bannerShown = shown;
            var animate = !first && IsOnScreen && _ui.Motion.Armed;
            if (shown)
            {
                _bannerCollapse.Finish();
                ResetBannerStyles();
                _bannerSlot.style.display = DisplayStyle.Flex;
                if (animate)
                {
                    _bannerGrow.Play();
                }

                return;
            }

            _bannerGrow.Finish();
            if (!animate)
            {
                _bannerSlot.style.display = DisplayStyle.None;
                ResetBannerStyles();
                return;
            }

            _bannerCollapse.Play(_bannerSlot, _banner, () =>
            {
                if (_bannerShown == false)
                {
                    _bannerSlot.style.display = DisplayStyle.None;
                }

                ResetBannerStyles();
            });
        }

        private void ResetBannerStyles()
        {
            _bannerSlot.style.height = StyleKeyword.Null;
            _bannerSlot.style.overflow = StyleKeyword.Null;
            _banner.style.position = StyleKeyword.Null;
            _banner.style.left = StyleKeyword.Null;
            _banner.style.right = StyleKeyword.Null;
            _banner.style.top = StyleKeyword.Null;
            _banner.style.opacity = StyleKeyword.Null;
            _banner.style.flexShrink = StyleKeyword.Null;
        }

        // Cargando (como Rutinas, DN3-15): nada durante 120 ms; después el indicador funde en 160. Nunca junto con el vacío.
        private void StartGrace()
        {
            _grace?.Dispose();
            _shownLoading = false;
            _loading.style.display = DisplayStyle.None;
            _spinner.SetShown(false);
            _grace = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.LoadingGraceMs), () =>
            {
                _grace = null;
                if (_phase != RoutinesPhase.Loading)
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
            _appBar.SetTitle(texts.Get(L10nKeys.WorkoutTitle));
            _bannerText.text = texts.Get(L10nKeys.WorkoutRowerNotConnected);
            FreeButton.SetLabel(texts.Get(L10nKeys.WorkoutFree));
            _label.text = texts.Get(L10nKeys.WorkoutOrPickRoutine);
            _errorText.text = texts.Get(L10nKeys.RoutinesLoadError);
            RetryButton.SetLabel(texts.Get(L10nKeys.Retry));
            _empty.text = texts.Get(L10nKeys.WorkoutNoRoutines);
            _inProgress.text = texts.Get(L10nKeys.WorkoutInProgress);
            List.RenderTexts();
        }

        private void OnToast(string key)
        {
            _ui.Toasts.Show(_ui.Texts.Get(key));
        }

        private void OnFree()
        {
            _ui.App.WorkoutRest.StartFree();
        }

        private void OnRoutineTapped(Routine routine)
        {
            var outcome = _ui.App.WorkoutRest.TapRoutine(routine);
            if (outcome != RoutineTapOutcome.Confirm || StartDialog != null && StartDialog.IsOpen)
            {
                return;
            }

            var dialog = new StartWorkoutDialog(_ui, routine);
            StartDialog = dialog;
            dialog.Closed += closed =>
            {
                if (ReferenceEquals(StartDialog, dialog))
                {
                    StartDialog = null;
                }

                if (closed.WasConfirmed)
                {
                    _ui.App.WorkoutRest.StartRoutine(routine);
                }
            };
            _ui.Dialogs.Open(dialog);
        }

        // La sesión dejó el reposo: el inmersivo, si no hay uno en la pila que no esté saliendo (02-tech-design §6.1; QA-V4a-01).
        private void OpenImmersive()
        {
            if (Immersive != null)
            {
                return;
            }

            var page = new ImmersiveWorkoutPage(_ui, _layout);
            _immersive = page;
            _ui.Pages.Push(page, PageTransitionKind.Fade);
            if (!_ui.Pages.HasPage(page))
            {
                _immersive = null;
                page.OnPopped();
            }
        }
    }
}
