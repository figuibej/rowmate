using System;
using System.Text;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Campo de texto de la app (03-rutinas/02-tech-design §4.10; 04-design-notes §4.8, DN3-09): envuelve el TextField de
    // UI Toolkit con etiqueta adentro de la caja, ayuda, error, foco y selección.
    //   #<nombre> .rm-field [--focused] [--filled] [--error] [--multiline] [--floating]
    //   ├── .rm-field__box          borde por USS (foco y error por clase); min-height 52 (72 la descripción), nunca alto fijo
    //   │   ├── TextField .rm-field__input .rm-type-body   sin etiqueta propia; relleno superior calculado con la etiqueta
    //   │   └── .rm-field__label    .rm-type-body; flotando: arriba y escala 0,8 por código (160 ms, ease.move)
    //   └── .rm-field__error-slot > #<error> .rm-field__error .rm-type-body-xs   display none sin error; se abre y se cierra
    //                                en 160 ease.move (ErrorLine; QA-V3-02), la clase --error cambia en el acto
    // --floating = con foco, con texto o con ayuda (los tres de progresión tienen la etiqueta siempre arriba: DV-19).
    // Lo que fija del TextField de runtime (QA-G3-10; la respuesta del diseño ya decidida, §4.10):
    // - Tab (también en la descripción) pasa al control siguiente y nunca escribe \t: el KeyDown se frena en TrickleDown y
    //   el foco lo mueve este control, una sola vez. S-B3a: el TextField multilínea escribe \t con Tab y el
    //   NavigationMoveEvent (Next/Previous) no saca el foco del campo; por eso el NavigationMove de Tab también se frena aquí
    //   (si llega solo, sin KeyDown, mueve el foco por el mismo camino) y nunca hay dos movimientos.
    // - Escape: no revierte el texto ni suelta el foco (se frena); cerrar un menú o un diálogo llega por HandleBack.
    // - Enter en una línea: quita el foco y no confirma ni guarda (el NavigationSubmit no sale del campo). En la
    //   descripción agrega un salto de línea y no sigue.
    // - Una línea: los saltos (\n, \r) y las tabulaciones se quitan al cambiar el valor. El legacy solo quita \n
    //   (FilteringTextInputFormatter.singleLineFormatter = deny('\n'), <flutter-sdk>/packages/flutter/lib/src/services/
    //   text_formatter.dart:425); quitar también \r y \t es DV-20 (03-rutinas/01-po-spec §11.1, QA-V3-07: sin caracteres
    //   invisibles en el nombre). En la descripción (multilínea) se quita solo \t: Tab nunca lo escribe y tampoco entra pegado
    //   (QA-G3-10, 03-rutinas/02-tech-design §4.10).
    //   Pegar (Ctrl+V, Mayús+Insert y el comando Paste) lo hace este control con IClipboard: S-B3a midió que el Paste de UI
    //   Toolkit cambia cada salto por un espacio antes de cambiar el valor (TextEditingUtilities.ReplaceNewlinesWithSpaces),
    //   y la spec pide que el salto pegado se quite (AC-RUT-11-02).
    // - selectAllOnFocus solo en escritorio y en una línea; selectAllOnMouseUp siempre en false (un clic deja el cursor).
    // - hideMobileInput, sin autocorrección; teclado con signo en los numéricos (DV-15).
    // - El texto se ve tal cual: enableRichText y parseEscapeSequences en false (DN3-22).
    // - Con foco pide 60 fps (DT-17).
    public sealed class RmTextField : VisualElement
    {
        public const string UssClass = "rm-field";
        public const float LabelTopFloatingDp = 6;
        public const float LabelGapDp = 1;
        public const float SingleLineMinDp = 52;
        public const float BottomRoomDp = 11.4f;

        private readonly UiContext _ui;
        private readonly bool _multiline;
        private readonly VisualElement _box;
        private readonly Label _label;
        private readonly Label _error;
        private readonly ErrorLine _errorLine;
        private readonly TextField _input;
        private readonly VisualElement? _textInput;
        private readonly TextElement? _textElement;
        private readonly LabelFloat _float;
        private bool _focused;
        private bool _boxError;
        private bool _pacing;
        private bool _filtering;
        private int _pendingTab;
        private float _labelHeight;
        private float _restTop = 16.7f;
        private float _padTop = 22f;

        public RmTextField(UiContext ui, string name, bool multiline = false, bool numeric = false, string? errorName = null)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _multiline = multiline;
            this.name = name;
            AddToClassList(UssClass);
            EnableInClassList(UssClass + "--multiline", multiline);
            pickingMode = PickingMode.Ignore;

            _box = new VisualElement();
            _box.AddToClassList(UssClass + "__box");
            Add(_box);

            _input = new TextField { multiline = multiline, label = "" };
            _input.AddToClassList(UssClass + "__input");
            _input.AddToClassList("rm-type-body");
            _input.labelElement.style.display = DisplayStyle.None;
            _input.textSelection.selectAllOnFocus = ui.IsDesktop && !multiline;
            _input.textSelection.selectAllOnMouseUp = false;
            _input.textEdition.hideMobileInput = true;
            _input.textEdition.autoCorrection = false;
            _input.textEdition.hidePlaceholderOnFocus = false;
            _input.keyboardType = numeric ? TouchScreenKeyboardType.NumbersAndPunctuation : TouchScreenKeyboardType.Default;
            if (multiline)
            {
                _input.verticalScrollerVisibility = ScrollPolicy.VerticalFor(ui.IsDesktop);
            }

            _textInput = _input.Q(TextField.textInputUssName);
            _textElement = _textInput?.Q<TextElement>();
            if (_textElement != null)
            {
                _textElement.enableRichText = false;
                _textElement.parseEscapeSequences = false;
            }

            _input.RegisterValueChangedCallback(OnValueChanged);
            _input.RegisterCallback<FocusInEvent>(_ => SetFocused(true));
            _input.RegisterCallback<FocusOutEvent>(_ => SetFocused(false));
            _input.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _input.RegisterCallback<KeyDownEvent>(OnKeyDownBubble);
            _input.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation());
            _input.RegisterCallback<NavigationCancelEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            _input.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            _input.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand, TrickleDown.TrickleDown);
            _box.Add(_input);

            _label = new Label();
            _label.AddToClassList(UssClass + "__label");
            _label.AddToClassList("rm-type-body");
            _label.pickingMode = PickingMode.Ignore;
            _label.enableRichText = false;
            _label.style.transformOrigin = new TransformOrigin(new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel));
            _label.RegisterCallback<GeometryChangedEvent>(_ => Measure());
            _box.Add(_label);

            _error = new Label { name = errorName ?? name + "-error" };
            _error.AddToClassList(UssClass + "__error");
            _error.AddToClassList("rm-type-body-xs");
            _error.pickingMode = PickingMode.Ignore;
            _error.enableRichText = false;
            _errorLine = new ErrorLine(ui.Motion, _error);
            Add(_errorLine.Slot);

            _float = new LabelFloat(ui.Motion, this);
            RegisterCallback<DetachFromPanelEvent>(_ => ReleasePacing());
            ContextMenu = new TextContextMenu(ui, this);
            Refresh(false);
        }

        public TextField Input => _input;

        public Label LabelElement => _label;

        public Label ErrorLabel => _error;

        public ErrorLine ErrorLine => _errorLine;

        public VisualElement Box => _box;

        public TextContextMenu ContextMenu { get; }

        public bool IsMultiline => _multiline;

        public bool IsFocused => _focused;

        public bool HasError => _boxError || _errorLine.HasText;

        // El error que vale ahora (la línea puede estar todavía abriéndose o yéndose).
        public string ErrorText => _errorLine.Text;

        public string Value
        {
            get => _input.value ?? "";
            set
            {
                var text = Clean(value ?? "");
                if (!string.Equals(_input.value, text, StringComparison.Ordinal))
                {
                    _input.SetValueWithoutNotify(text);
                }

                Refresh(true);
            }
        }

        // La ayuda (Ej: 2…): visible con el campo vacío, con foco o sin él (DV-19).
        public string Placeholder
        {
            get => _input.textEdition.placeholder ?? "";
            set
            {
                _input.textEdition.placeholder = value ?? "";
                Refresh(true);
            }
        }

        public bool IsFloating => _focused || Value.Length > 0 || Placeholder.Length > 0;

        // Cambió por el usuario (ya filtrado).
        public event Action<string>? Changed;

        public void SetLabel(string text)
        {
            if (_label.text != text)
            {
                _label.text = text;
            }
        }

        // Texto de error debajo del campo (null o vacío = sin error): la caja cambia en el acto y la línea se mueve (ErrorLine).
        public void SetError(string? text)
        {
            _errorLine.Set(text);
            Refresh(true);
        }

        // Caja en error sin texto propio (los pares Min/Seg y Split min/Split seg: un solo texto debajo del par).
        public void SetBoxError(bool error)
        {
            _boxError = error;
            Refresh(true);
        }

        public void ClearError()
        {
            _boxError = false;
            SetError(null);
        }

        // Foco por código; selectAll (escritorio) o el cursor al final (teléfono, AC-RUT-14-04).
        public void FocusInput(bool selectAll)
        {
            var previous = _input.textSelection.selectAllOnFocus;
            _input.textSelection.selectAllOnFocus = false;
            _input.Focus();
            _input.textSelection.selectAllOnFocus = previous;
            var length = Value.Length;
            if (selectAll)
            {
                _input.textSelection.SelectRange(0, length);
            }
            else
            {
                _input.textSelection.SelectRange(length, length);
            }
        }

        // Selección actual [inicio, fin) en índices del texto.
        public (int Start, int End) Selection
        {
            get
            {
                var a = Mathf.Clamp(_input.textSelection.cursorIndex, 0, Value.Length);
                var b = Mathf.Clamp(_input.textSelection.selectIndex, 0, Value.Length);
                return a <= b ? (a, b) : (b, a);
            }
        }

        public void Select(int start, int end)
        {
            _input.textSelection.SelectRange(start, end);
        }

        // Reemplaza [start, end) por text (cortar, pegar), deja el cursor al final de lo insertado y avisa Changed.
        public void ReplaceRange(int start, int end, string text)
        {
            var value = Value;
            start = Mathf.Clamp(start, 0, value.Length);
            end = Mathf.Clamp(end, start, value.Length);
            var insert = Clean(text ?? "");
            var result = value.Substring(0, start) + insert + value.Substring(end);
            _input.SetValueWithoutNotify(result);
            var cursor = start + insert.Length;
            _input.textSelection.SelectRange(cursor, cursor);
            Refresh(true);
            Changed?.Invoke(result);
        }

        // Sin \n ni \r en una línea; sin \t nunca (lo que el teclado no deja escribir tampoco entra pegado).
        public string Clean(string text)
        {
            if (text.IndexOf('\t') < 0 && (_multiline || (text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\t' || (!_multiline && (c == '\n' || c == '\r')))
                {
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            evt.StopPropagation();
            if (_filtering)
            {
                return;
            }

            var raw = evt.newValue ?? "";
            var clean = Clean(raw);
            if (!string.Equals(raw, clean, StringComparison.Ordinal))
            {
                _filtering = true;
                var cursor = Mathf.Clamp(_input.textSelection.cursorIndex - (raw.Length - clean.Length), 0, clean.Length);
                _input.SetValueWithoutNotify(clean);
                _input.textSelection.SelectRange(cursor, cursor);
                _filtering = false;
            }

            Refresh(true);
            Changed?.Invoke(clean);
        }

        // Pega el texto del portapapeles en la selección (sin \n ni \r en una línea; sin \t nunca).
        public void PasteFromClipboard()
        {
            var clipboard = _ui.Clipboard;
            if (!clipboard.HasText)
            {
                return;
            }

            var (start, end) = Selection;
            ReplaceRange(start, end, clipboard.Text);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (evt.commandName != "Paste")
            {
                return;
            }

            evt.StopImmediatePropagation();
            _input.focusController?.IgnoreEvent(evt);
            PasteFromClipboard();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if ((evt.actionKey && evt.keyCode == KeyCode.V) || (evt.shiftKey && evt.keyCode == KeyCode.Insert))
            {
                evt.StopImmediatePropagation();
                _input.focusController?.IgnoreEvent(evt);
                PasteFromClipboard();
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.Tab:
                    // Nunca escribe \t; el foco pasa al siguiente (o al anterior con Mayús).
                    evt.StopImmediatePropagation();
                    _input.focusController?.IgnoreEvent(evt);
                    QueueTab(evt.shiftKey ? -1 : 1);
                    return;
                case KeyCode.Escape:
                    evt.StopImmediatePropagation();
                    _input.focusController?.IgnoreEvent(evt);
                    return;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (!_multiline)
                    {
                        evt.StopImmediatePropagation();
                        _input.Blur();
                    }

                    return;
            }

            if (evt.character == '\t')
            {
                evt.StopImmediatePropagation();
                _input.focusController?.IgnoreEvent(evt);
                return;
            }

            if (!_multiline && (evt.character == '\n' || evt.character == '\r'))
            {
                evt.StopImmediatePropagation();
            }
        }

        // Lo que el TextField multilínea ya procesó (Enter) no sigue hacia el diálogo ni la página.
        private void OnKeyDownBubble(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n')
            {
                evt.StopPropagation();
            }
        }

        // El NavigationMove de Tab (Next/Previous) no sale del campo: lo mueve este control (si el KeyDown de Tab ya pidió el
        // movimiento, no se pide otro: una sola vez por pulsación).
        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            evt.StopImmediatePropagation();
            _input.focusController?.IgnoreEvent(evt);
            if (_pendingTab == 0)
            {
                QueueTab(evt.direction == NavigationMoveEvent.Direction.Next ? 1 : -1);
            }
        }

        private void QueueTab(int direction)
        {
            _pendingTab = direction;
            schedule.Execute(() =>
            {
                var pending = _pendingTab;
                _pendingTab = 0;
                if (pending == 0 || !_focused || panel == null)
                {
                    return;
                }

                var scope = FocusScope();
                var next = FocusTrap.Next(scope, _input, pending > 0);
                if (next != null && !ReferenceEquals(next, _input))
                {
                    next.Focus();
                }
            });
        }

        // El diálogo de arriba, si el campo está en uno; si no, la página o la pantalla que lo contiene.
        private VisualElement FocusScope()
        {
            VisualElement scope = this;
            for (var e = hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                scope = e;
                if (e.ClassListContains("rm-dialog") || e.ClassListContains("rm-page") || e.ClassListContains("rm-screen"))
                {
                    return e;
                }
            }

            return scope;
        }

        private void SetFocused(bool focused)
        {
            if (_focused == focused)
            {
                return;
            }

            _focused = focused;
            if (focused && !_pacing)
            {
                _pacing = true;
                _ui.Motion.Pacing.Acquire();
            }
            else if (!focused)
            {
                ReleasePacing();
            }

            Refresh(true);
        }

        private void ReleasePacing()
        {
            if (!_pacing)
            {
                return;
            }

            _pacing = false;
            _ui.Motion.Pacing.Release();
        }

        private void Refresh(bool animate)
        {
            var filled = Value.Length > 0;
            EnableInClassList(UssClass + "--focused", _focused);
            EnableInClassList(UssClass + "--filled", filled);
            EnableInClassList(UssClass + "--error", _boxError || _errorLine.HasText);
            var floating = IsFloating;
            EnableInClassList(UssClass + "--floating", floating);
            _float.To(floating, animate);
        }

        // Relleno superior del texto y lugares de la etiqueta con las cajas resueltas (letra 1,3×, DN3-09).
        private void Measure()
        {
            var height = _label.layout.height;
            if (!(height > 0) || Math.Abs(height - _labelHeight) < 0.01f)
            {
                return;
            }

            _labelHeight = height;
            _padTop = LabelTopFloatingDp + height * (float)MotionSpecs.FieldLabelScale + LabelGapDp;
            var single = Mathf.Max(SingleLineMinDp, _padTop + height + BottomRoomDp);
            _restTop = (single - height) / 2;
            if (_textInput != null)
            {
                _textInput.style.paddingTop = _padTop;
                if (_multiline)
                {
                    // Dos líneas visibles; lo que no entra se desplaza dentro del campo (AC-RUT-11-02).
                    _textInput.style.height = _padTop + height * 2 + BottomRoomDp - 3;
                }
            }

            _float.To(IsFloating, false);
        }

        // La etiqueta entre el reposo (centrada en la primera línea) y arriba con escala 0,8.
        private sealed class LabelFloat : TimedMotion
        {
            private readonly RmTextField _field;
            private double _from;
            private double _to;
            private double _current;

            public LabelFloat(MotionDriver driver, RmTextField field)
                : base(driver)
            {
                _field = field;
            }

            protected override double TotalMs => Tokens.DurFast.Milliseconds;

            public void To(bool floating, bool animate)
            {
                var target = floating ? 1.0 : 0.0;
                if (Math.Abs(target - _to) < 1e-6 && (IsRunning || Math.Abs(_current - target) < 1e-6))
                {
                    Apply(_current);
                    return;
                }

                _from = _current;
                _to = target;
                if (!animate || !Driver.Armed || Driver.Settings.ReduceMotion || _field.panel == null)
                {
                    if (IsRunning)
                    {
                        Finish();
                    }

                    _current = target;
                    Apply(target);
                    return;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var u = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, TotalMs));
                _current = _from + (_to - _from) * u;
                Apply(_current);
            }

            private void Apply(double t)
            {
                var label = _field._label;
                var top = _field._restTop + (LabelTopFloatingDp - _field._restTop) * t;
                var scale = 1 + (MotionSpecs.FieldLabelScale - 1) * t;
                label.style.top = (float)top;
                label.style.scale = new Scale(new Vector2((float)scale, (float)scale));
            }
        }
    }
}
