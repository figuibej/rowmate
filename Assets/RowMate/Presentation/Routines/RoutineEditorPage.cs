using System;
using System.Threading.Tasks;
using RowMate.App.Routines;
using RowMate.Core.Common;
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
    // Editor de rutina, una página apilada (F-RUT-11, -12, -14; legacy RoutineEditorScreen,
    // lib/features/routines/routine_editor_screen.dart; 03-rutinas/02-tech-design §4.4; 04-design-notes §4.5):
    //   #routine-editor .rm-page .rm-editor
    //   ├── #editor-appbar .rm-appbar: #editor-back (arrow_back, ayuda navBack), título editorNewRoutine | editorEditRoutine,
    //   │                              #editor-save (Guardar, de texto)
    //   └── #editor-scroll (UN solo ScrollView, DV-18) > .rm-editor__content
    //       ├── .rm-editor__fields: #editor-name (una línea) → 12 → #editor-description (dos líneas)
    //       ├── .rm-divider
    //       ├── .rm-editor__steps-header: #editor-steps-count, #editor-add-step (+ Paso), #editor-add-series (Serie)
    //       ├── #editor-empty .rm-editor__empty            editorEmptySteps, centrado en lo que queda
    //       └── EditorStepsView (#editor-steps)
    // Guardar = RoutineEditorStore.SaveAsync: Saved → Pop una sola vez (el store queda cerrado, QA-G3-01); NameEmpty,
    // NoSteps, StepOutOfRange y Failed → aviso (RoutineTexts.SaveToast); Ignored → nada. La flecha y "Atrás" de Android
    // cierran sin preguntar (AC-RUT-11-08); Escape NO (P-08: lo resuelve BackNavigation). Un guardado lento que vuelve con
    // el editor ya cerrado no toca la página; si falló, el aviso igual sale (la lista quedó a la vista, QA-G3-09). Una falla que
    // no sea de la base no queda sin observar: se registra con el tag DB (Forget; QA-V3-10).
    // El vacío (04-design-notes §5.6; QA-V3-02): con el último paso borrado, la lista de pasos queda a la vista hasta que lo
    // borrado terminó de desvanecerse y colapsar (EditorStepsView.Collapsed) y recién ahí entra el vacío (240, y +8); con el
    // primer paso, el vacío funde → 0 en 120 en su lugar (ExitMotion) mientras la fila nueva crece. Nada salta.
    public sealed class RoutineEditorPage : IPage
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly RoutineEditorStore _store;
        private readonly AppBarView _appBar;
        private readonly Label _count;
        private readonly RmButton _addStep;
        private readonly RmButton _addSeries;
        private readonly Label _empty;
        private readonly EnterMotion _emptyIn;
        private readonly ExitMotion _emptyOut;
        private bool _emptyShown;
        private bool _emptyPending;
        private bool _rendered;
        private bool _closed;
        private bool _disposed;

        public RoutineEditorPage(UiContext ui, ShellLayout layout, Routine? existing)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _store = ui.App.CreateEditor(existing);

            Root = new VisualElement { name = "routine-editor" };
            Root.AddToClassList("rm-editor");

            _appBar = new AppBarView("editor-appbar", layout);
            _appBar.AddToClassList("rm-appbar--with-back");
            Back = new IconButton("editor-back", Icons.ArrowBack, Close);
            Back.AddToClassList("rm-appbar__back");
            Back.SetTooltip(ui, () => _ui.Texts.Get(L10nKeys.NavBack));
            _appBar.Insert(0, Back);
            Save = new RmButton("editor-save", "text", null, "", () => SaveAsync().Forget(_ui.Logger, RoutinesStore.Tag));
            _appBar.Actions.Add(Save);
            Root.Add(_appBar);

            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "editor-scroll" };
            Scroll.AddToClassList("rm-scroll");
            Scroll.AddToClassList("rm-editor__scroll");
            ScrollPolicy.Apply(Scroll, ui.IsDesktop);
            Content = Scroll.contentContainer;
            Content.AddToClassList("rm-editor__content");
            Scroller = new FocusScroller(ui, Scroll);
            Root.Add(Scroll);

            var fields = new VisualElement();
            fields.AddToClassList("rm-editor__fields");
            fields.pickingMode = PickingMode.Ignore;
            NameField = new RmTextField(ui, "editor-name");
            NameField.Value = _store.Name;
            NameField.Changed += text => _store.Name = text;
            fields.Add(NameField);
            DescriptionField = new RmTextField(ui, "editor-description", true);
            DescriptionField.Value = _store.Description;
            DescriptionField.Changed += text => _store.Description = text;
            fields.Add(DescriptionField);
            Content.Add(fields);

            var divider = new VisualElement();
            divider.AddToClassList("rm-divider");
            divider.pickingMode = PickingMode.Ignore;
            Content.Add(divider);

            var header = new VisualElement();
            header.AddToClassList("rm-editor__steps-header");
            header.pickingMode = PickingMode.Ignore;
            _count = new Label { name = "editor-steps-count" };
            _count.AddToClassList("rm-editor__steps-count");
            _count.AddToClassList("rm-type-title-xs");
            _count.pickingMode = PickingMode.Ignore;
            header.Add(_count);
            var spacer = new VisualElement();
            spacer.AddToClassList("rm-editor__spacer");
            spacer.pickingMode = PickingMode.Ignore;
            header.Add(spacer);
            _addStep = new RmButton("editor-add-step", "text", Icons.Add, "", AddStep);
            header.Add(_addStep);
            _addSeries = new RmButton("editor-add-series", "text", Icons.Repeat, "", AddSeries);
            header.Add(_addSeries);
            Content.Add(header);

            _empty = new Label { name = "editor-empty" };
            _empty.AddToClassList("rm-editor__empty");
            _empty.AddToClassList("rm-type-body-m");
            _empty.pickingMode = PickingMode.Ignore;
            _empty.style.display = DisplayStyle.None;
            Content.Add(_empty);
            _emptyIn = new EnterMotion(ui.Motion);
            _emptyOut = new ExitMotion(ui.Motion);

            Steps = new EditorStepsView(ui, EditStep, DeleteStep, ChangeRepeat, DeleteSeries, AddStepToSeries);
            Steps.Collapsed += OnStepsCollapsed;
            Content.Add(Steps);

            layout.RegisterClearance(Scroll, Content, () => 24 + _layout.Insets.Bottom, () => _ui.Keyboard.IsVisible ? KeyboardDp() : 0);
            Scroller.SetBottomInset(KeyboardDp);
            Root.Bind(_store.State, Render);
            Root.Bind(ui.App.Texts, OnTexts);
        }

        public VisualElement Root { get; }

        public RoutineEditorStore Store => _store;

        public IconButton Back { get; }

        public RmButton Save { get; }

        public ScrollView Scroll { get; }

        public VisualElement Content { get; }

        public FocusScroller Scroller { get; }

        public RmTextField NameField { get; }

        public RmTextField DescriptionField { get; }

        public EditorStepsView Steps { get; }

        public Label CountLabel => _count;

        public Label EmptyLabel => _empty;

        // El vacío vale (se ve, entra o espera a que termine el colapso de lo último borrado).
        public bool ShowsEmpty => _emptyShown;

        public bool IsClosed => _closed;

        // Terminó la entrada (la barra de pestañas ya está tapada): la superficie de una pantalla apilada para la llave del
        // simulador (D-ART-10: 16 + margen inferior, en el carril izquierdo; baja con el resorte cuando termina la entrada).
        public void OnPushed()
        {
            if (!_closed && !_disposed)
            {
                _layout.SetSurface(this, OverlaySurface.Stacked());
            }
        }

        public void OnPopped()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _layout.SetSurface(this, null);
            _layout.UnregisterAppBar(_appBar);
            _layout.UnregisterClearance(Scroll);
            _store.Dispose();
        }

        // "Atrás" de Android con el editor arriba: cierra sin preguntar (AC-RUT-11-08).
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
            // La llave sube cuando EMPIEZA la salida (la barra vuelve en el cuadro 0, D-ART-10).
            _layout.SetSurface(this, null);
            _ui.Pages.Pop(this);
        }

        // Guardar (AC-RUT-11-05 a -07, AC-RUT-09-03, -05). Público para las pruebas de Enter y doble toque.
        public async Task<SaveOutcome> SaveAsync()
        {
            if (_closed)
            {
                return SaveOutcome.Ignored;
            }

            var outcome = await _store.SaveAsync();
            if (outcome == SaveOutcome.Saved)
            {
                if (!_disposed)
                {
                    Close();
                }

                return outcome;
            }

            var toast = RoutineTexts.SaveToast(outcome, _ui.Texts);
            if (toast != null)
            {
                // Con el editor ya cerrado (Atrás durante un guardado lento) el aviso igual sale: la lista quedó a la vista.
                _ui.Toasts.Show(toast);
            }

            return outcome;
        }

        private double KeyboardDp()
        {
            var keyboard = _ui.Keyboard;
            var density = _ui.Display.DpScale > 0 ? _ui.Display.DpScale : 1;
            return keyboard.IsVisible ? keyboard.OccludedHeightPx / density : 0;
        }

        private void Render(RoutineEditorState state)
        {
            if (_disposed)
            {
                return;
            }

            var texts = _ui.Texts;
            _count.text = RoutineTexts.StepsCount(state.StepCount, texts);
            // La primera vez (al abrir) sin animar: el estado llega con la página.
            var animate = _rendered && _ui.Motion.Armed;
            _rendered = true;
            if (state.StepCount > 0)
            {
                HideEmpty(animate);
            }

            Steps.Render(state, texts);
            if (state.StepCount == 0)
            {
                ShowEmptyAfterCollapse(animate);
            }

            var added = Steps.LastAdded;
            if (added != null)
            {
                ScrollToWhenLaidOut(added);
            }
        }

        // Hay pasos: la lista a la vista y el vacío se va fundiéndose en su lugar (con el primer paso) o en el acto.
        private void HideEmpty(bool animate)
        {
            _emptyPending = false;
            Steps.style.display = DisplayStyle.Flex;
            if (!_emptyShown)
            {
                return;
            }

            _emptyShown = false;
            // Si estaba entrando, sale desde la opacidad y la y que tiene (DD4a-05 a: sin destello a 1).
            var opacity = 1.0;
            var shift = 0f;
            if (_emptyIn.IsRunning)
            {
                opacity = _empty.style.opacity.value;
                shift = _empty.style.translate.value.y.value;
            }

            _emptyIn.Finish();
            if (animate)
            {
                _emptyOut.Play(_empty, MotionSpecs.EmptyOutMs, opacity, shift);
            }
            else
            {
                _empty.style.display = DisplayStyle.None;
            }
        }

        // Sin pasos: si lo último borrado todavía colapsa, la lista sigue a la vista y el vacío espera a que termine.
        private void ShowEmptyAfterCollapse(bool animate)
        {
            if (_emptyShown)
            {
                return;
            }

            _emptyShown = true;
            if (animate && Steps.IsCollapsing)
            {
                _emptyPending = true;
                return;
            }

            ShowEmpty(animate, 0);
        }

        // overrunMs: cuánto se pasó el colapso de su final en este cuadro; el vacío arranca en ese instante (00-deuda-diseno de la
        // It. 4a §2.1: a los 300 ms ya va por 0,231).
        private void ShowEmpty(bool animate, double overrunMs)
        {
            _emptyPending = false;
            _emptyOut.Finish();
            Steps.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            if (animate)
            {
                _emptyIn.Play(_empty, -overrunMs, MotionSpecs.ListItemInMs, MotionSpecs.EmptyInShiftDp);
            }
        }

        private void OnStepsCollapsed(double overrunMs)
        {
            if (_emptyPending && _emptyShown && !_disposed)
            {
                ShowEmpty(_ui.Motion.Armed, overrunMs);
            }
        }

        private void OnTexts(Localizer texts)
        {
            _appBar.SetTitle(texts.Get(_store.IsNew ? L10nKeys.EditorNewRoutine : L10nKeys.EditorEditRoutine));
            Save.SetLabel(texts.Get(L10nKeys.Save));
            NameField.SetLabel(texts.Get(L10nKeys.EditorRoutineName));
            DescriptionField.SetLabel(texts.Get(L10nKeys.EditorDescription));
            _addStep.SetLabel(texts.Get(L10nKeys.EditorStep));
            _addSeries.SetLabel(texts.Get(L10nKeys.EditorSeries));
            _empty.text = texts.Get(L10nKeys.EditorEmptySteps);
            _count.text = RoutineTexts.StepsCount(_store.State.Value.StepCount, texts);
            Steps.RenderTexts(texts);
        }

        // Lo nuevo se lleva a la vista con 16 dp de aire, con el resorte ω 12, cuando ya tiene su alto (DN3-18).
        private void ScrollToWhenLaidOut(VisualElement element)
        {
            EventCallback<GeometryChangedEvent>? callback = null;
            callback = _ =>
            {
                element.UnregisterCallback(callback!);
                Scroller.ScrollIntoView(element, MotionSpecs.ScrollToNewOmega);
            };
            element.RegisterCallback(callback);
        }

        private void AddStep()
        {
            if (_closed)
            {
                return;
            }

            OpenStepDialog(_store.NewStepForm(), step => _store.AddLooseStep(step));
        }

        private void AddSeries()
        {
            if (!_closed)
            {
                _store.AddSeries();
            }
        }

        private void AddStepToSeries(string groupId)
        {
            if (_closed)
            {
                return;
            }

            OpenStepDialog(_store.NewSeriesStepForm(groupId), step => _store.AddStepToSeries(groupId, step));
        }

        private void EditStep(int index)
        {
            if (_closed || index < 0 || index >= _store.Template.Count)
            {
                return;
            }

            var key = _store.State.Value.Steps[index].Key;
            OpenStepDialog(_store.EditStepForm(index), step =>
            {
                Steps.MarkEdited(key);
                _store.ReplaceStep(index, step);
            });
        }

        private void DeleteStep(int index)
        {
            if (!_closed && index >= 0 && index < _store.Template.Count)
            {
                _store.DeleteStep(index);
            }
        }

        private void DeleteSeries(string groupId)
        {
            if (!_closed)
            {
                _store.DeleteSeries(groupId);
            }
        }

        private void ChangeRepeat(string groupId)
        {
            if (_closed)
            {
                return;
            }

            _ui.Dialogs.Open(new RepeatDialog(_ui, _store, groupId));
        }

        private void OpenStepDialog(App.Routines.StepDialogForm form, Action<IntervalStep> apply)
        {
            var dialog = new StepDialog(_ui, form);
            dialog.Closed += closed =>
            {
                var step = ((StepDialog)closed).Result;
                if (closed.WasConfirmed && step != null && !_closed)
                {
                    apply(step);
                }
            };
            _ui.Dialogs.Open(dialog);
        }
    }
}
