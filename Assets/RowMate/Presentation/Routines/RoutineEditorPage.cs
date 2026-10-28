using System;
using System.Threading.Tasks;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
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
    // el editor ya cerrado no toca la página; si falló, el aviso igual sale (la lista quedó a la vista, QA-G3-09).
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
            Save = new RmButton("editor-save", "text", null, "", () => _ = SaveAsync());
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
            Content.Add(_empty);

            Steps = new EditorStepsView(ui, EditStep, DeleteStep, ChangeRepeat, DeleteSeries, AddStepToSeries);
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

        public bool IsClosed => _closed;

        public void OnPushed()
        {
        }

        public void OnPopped()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
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
            _empty.style.display = state.StepCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Steps.style.display = state.StepCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            Steps.Render(state, texts);
            var added = Steps.LastAdded;
            if (added != null)
            {
                ScrollToWhenLaidOut(added);
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
