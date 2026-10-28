using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // TXT · Campos de texto y menú del clic derecho (AC-RUT-11-02, -09, -10, AC-APP-09-07, AC-APP-05-07; 03-rutinas/02-tech-design
    // §4.10 y lo que midió S-B3a, Anexo A.3). Siempre con el FakeClipboard (R3-10).
    public sealed class TextEditingTests : RoutinesUiTestBase
    {
        private RmTextField NameField => Editor.NameField;

        private RmTextField DescriptionField => Editor.DescriptionField;

        private IEnumerator OpenEditor(System.Action<UiHostOptions>? configure = null)
        {
            CreateRoutines(RoutineSeeds.Lista, null, configure);
            yield return OpenEditorOf(0);
        }

        [UnityTest, Category("AC-RUT-11-02")]
        public IEnumerator SingleLineDropsPastedNewlines()
        {
            yield return OpenEditor();
            H.TestClipboard!.Text = "a\nb\r\nc\td";
            NameField.FocusInput(true);
            yield return Frames();
            H.PressKey(KeyCode.V, 'v', EventModifiers.Control);
            yield return Frames();
            Assert.That(NameField.Value, Is.EqualTo("abcd"), "Ctrl+V: el salto pegado se quita (no se cambia por un espacio)");
            Assert.That(Editor.Store.Name, Is.EqualTo("abcd"));
            NameField.Select(0, 4);
            H.PressKey(KeyCode.Insert, '\0', EventModifiers.Shift);
            yield return Frames();
            Assert.That(NameField.Value, Is.EqualTo("abcd"), "Mayús+Insert, igual");
            DescriptionField.FocusInput(false);
            DescriptionField.Value = "";
            yield return Frames();
            H.PressKey(KeyCode.V, 'v', EventModifiers.Control);
            yield return Frames();
            Assert.That(DescriptionField.Value, Is.EqualTo("a\nb\r\ncd"), "en la descripción los saltos quedan; la tabulación nunca");
        }

        [UnityTest, Category("AC-RUT-11-02")]
        public IEnumerator DescriptionTwoLinesEnterAndScroll()
        {
            yield return OpenEditor();
            DescriptionField.FocusInput(false);
            DescriptionField.Value = "uno";
            DescriptionField.Select(3, 3);
            yield return Frames();
            var height = DescriptionField.Box.layout.height;
            H.PressKey(KeyCode.Return, '\n');
            yield return Frames();
            Assert.That(DescriptionField.Value, Does.Contain("\n"), "Enter agrega un salto de línea");
            Assert.That(DescriptionField.IsFocused, Is.True, "y no suelta el foco");
            yield return Type("editor-description", "1\n2\n3\n4\n5\n6");
            yield return Frames(2);
            Assert.That(DescriptionField.Box.layout.height, Is.EqualTo(height).Within(1f), "dos líneas visibles: lo demás se desplaza adentro");
            Assert.That(H.Editor, Is.Not.Null);
        }

        [UnityTest, Category("AC-RUT-11-02"), Category("AC-APP-05-07")]
        public IEnumerator EnterInASingleLineFieldBlurs()
        {
            yield return OpenEditor();
            NameField.FocusInput(true);
            yield return Frames();
            H.PressEnter();
            yield return Settle();
            Assert.That(NameField.IsFocused, Is.False, "Enter le quita el foco");
            Assert.That(H.Editor, Is.Not.Null, "no guarda");
            Assert.That(H.Shell.Toasts.IsVisible, Is.False);
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator EnterInADialogFieldDoesNotConfirm()
        {
            yield return OpenEditor();
            yield return ScrollAndTap("step-edit-0");
            var field = H.Find<RmTextField>("step-min")!;
            field.FocusInput(true);
            yield return Frames();
            H.PressEnter();
            yield return Settle();
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>(), "Enter en un campo no confirma el diálogo");
            Assert.That(field.IsFocused, Is.False);
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator TabFromTheDescriptionMovesFocus()
        {
            yield return OpenEditor();
            DescriptionField.FocusInput(false);
            DescriptionField.Value = "x";
            yield return Frames();
            H.PressTab();
            yield return Frames(2);
            Assert.That(DescriptionField.Value, Is.EqualTo("x"), "Tab no escribe una tabulación");
            Assert.That(DescriptionField.IsFocused, Is.False, "el foco pasa al control siguiente");
            Assert.That(H.Focused, Is.Not.Null);
            DescriptionField.FocusInput(false);
            yield return Frames();
            H.PressTab(true);
            yield return Frames(2);
            Assert.That(NameField.IsFocused, Is.True, "Mayús+Tab, al anterior");
        }

        [UnityTest, Category("AC-APP-03-02")]
        public IEnumerator EscapeInAFieldKeepsTextAndFocus()
        {
            yield return OpenEditor();
            NameField.FocusInput(false);
            yield return Frames();
            H.Type("editor-name", "Cambiado");
            H.PressKey(KeyCode.Escape);
            yield return Frames(2);
            Assert.That(NameField.Value, Is.EqualTo("Cambiado"), "el texto no se revierte (el TextField de UI Toolkit sí lo haría)");
            Assert.That(NameField.IsFocused, Is.True, "y el foco sigue");
        }

        [UnityTest, Category("AC-RUT-11-09")]
        public IEnumerator ContextMenuItemsByState()
        {
            yield return OpenEditor();
            NameField.FocusInput(false);
            yield return Frames();
            Assert.That(NameField.ContextMenu.ItemsNow(), Is.EqualTo(new[] { TextContextMenu.SelectAllName }), "sin selección ni portapapeles: Seleccionar todo");
            NameField.Select(0, 4);
            Assert.That(NameField.ContextMenu.ItemsNow(), Is.EqualTo(new[] { TextContextMenu.CutName, TextContextMenu.CopyName, TextContextMenu.SelectAllName }));
            H.TestClipboard!.Text = "algo";
            NameField.Select(0, NameField.Value.Length);
            Assert.That(NameField.ContextMenu.ItemsNow(), Is.EqualTo(new[] { TextContextMenu.CutName, TextContextMenu.CopyName, TextContextMenu.PasteName }),
                "todo seleccionado: sin Seleccionar todo");
            H.RightClick("editor-name");
            yield return Settle();
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.True);
            Assert.That(H.TextOf(TextContextMenu.CutName), Is.EqualTo("Cortar"));
            Assert.That(H.TextOf(TextContextMenu.CopyName), Is.EqualTo("Copiar"));
            Assert.That(H.TextOf(TextContextMenu.PasteName), Is.EqualTo("Pegar"));
            Assert.That(NameField.Selection, Is.EqualTo((0, NameField.Value.Length)), "el clic derecho no borra la selección");
        }

        [UnityTest, Category("AC-RUT-11-09")]
        public IEnumerator CutCopyPasteWithTheTestClipboard()
        {
            yield return OpenEditor();
            NameField.FocusInput(false);
            NameField.Select(0, 8);
            H.RightClick("editor-name");
            yield return Settle();
            yield return Tap(TextContextMenu.CopyName);
            Assert.That(H.TestClipboard!.Text, Is.EqualTo("Escalera"));
            Assert.That(NameField.Value, Is.EqualTo("Escalera SPM"));
            NameField.Select(8, 12);
            H.RightClick("editor-name");
            yield return Settle();
            yield return Tap(TextContextMenu.CutName);
            Assert.That(H.TestClipboard.Text, Is.EqualTo(" SPM"));
            Assert.That(NameField.Value, Is.EqualTo("Escalera"));
            Assert.That(Editor.Store.Name, Is.EqualTo("Escalera"), "el store se entera");
            NameField.Select(0, 0);
            H.RightClick("editor-name");
            yield return Settle();
            yield return Tap(TextContextMenu.PasteName);
            Assert.That(NameField.Value, Is.EqualTo(" SPMEscalera"));
            Assert.That(NameField.Selection, Is.EqualTo((4, 4)), "el cursor queda al final de lo pegado");
            H.RightClick("editor-name");
            yield return Settle();
            yield return Tap(TextContextMenu.SelectAllName);
            Assert.That(NameField.Selection, Is.EqualTo((0, NameField.Value.Length)));
            Assert.That(H.TestClipboard.Writes, Is.EqualTo(2), "solo Copiar y Cortar escriben el portapapeles");
        }

        [UnityTest, Category("AC-RUT-11-09")]
        public IEnumerator TabSelectsAllClickPlacesTheCaret()
        {
            yield return OpenEditor();
            NameField.Input.Focus();
            yield return Frames();
            Assert.That(NameField.Selection, Is.EqualTo((0, 12)), "foco por teclado o por código: todo seleccionado (escritorio)");
            NameField.Input.Blur();
            yield return Frames();
            Assert.That(NameField.Input.textSelection.selectAllOnMouseUp, Is.False, "un clic deja el cursor donde se hizo clic (S-B3a, punto 2)");
            Assert.That(NameField.Input.textSelection.selectAllOnFocus, Is.True);
        }

        [UnityTest, Category("AC-RUT-11-09")]
        public IEnumerator DescriptionDoesNotSelectAll()
        {
            yield return OpenEditorAt(1);
            DescriptionField.Input.Focus();
            yield return Frames();
            var selection = DescriptionField.Selection;
            Assert.That(selection.End - selection.Start, Is.EqualTo(0), "la descripción no selecciona todo al enfocar");
            Assert.That(DescriptionField.Input.textSelection.selectAllOnFocus, Is.False);
        }

        private IEnumerator OpenEditorAt(int card)
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(card);
        }

        [UnityTest, Category("AC-RUT-11-09")]
        public IEnumerator NoMenuWithoutActions()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            H.RightClick("editor-name");
            yield return Settle();
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "campo vacío y portapapeles vacío: no se abre nada (P-06)");
            Assert.That(NameField.IsFocused, Is.True, "el clic derecho igual le da el foco");
        }

        [UnityTest, Category("AC-APP-09-07")]
        public IEnumerator ContextMenuInTheAppLanguage()
        {
            yield return OpenEditor(o => o.LanguageTags = new[] { "en-US" });
            H.TestClipboard!.Text = "x";
            NameField.FocusInput(false);
            NameField.Select(0, 3);
            H.RightClick("editor-name");
            yield return Settle();
            Assert.That(H.TextOf(TextContextMenu.CutName), Is.EqualTo("Cut"));
            Assert.That(H.TextOf(TextContextMenu.CopyName), Is.EqualTo("Copy"));
            Assert.That(H.TextOf(TextContextMenu.PasteName), Is.EqualTo("Paste"));
            Assert.That(H.TextOf(TextContextMenu.SelectAllName), Is.EqualTo("Select all"));
            yield return Back(RowMate.Presentation.Shell.BackSource.Escape);
            H.SetLanguage("es-AR");
            yield return Settle();
            NameField.Select(0, 3);
            H.RightClick("editor-name");
            yield return Settle();
            Assert.That(H.TextOf(TextContextMenu.SelectAllName), Is.EqualTo("Seleccionar todo"), "sigue el idioma de la app");
        }

        [UnityTest, Category("AC-RUT-11-10")]
        public IEnumerator MobileInputIsHidden()
        {
            yield return OpenEditor(o => o.IsDesktop = false);
            Assert.That(NameField.Input.textEdition.hideMobileInput, Is.True, "sin la caja extra del sistema");
            Assert.That(DescriptionField.Input.textEdition.hideMobileInput, Is.True);
            Assert.That(NameField.Input.textSelection.selectAllOnFocus, Is.False, "en el teléfono no selecciona todo");
        }

        [UnityTest, Category("AC-RUT-11-10")]
        public IEnumerator LongPressMenuInAppMenuMode()
        {
            yield return OpenEditor(o =>
            {
                o.IsDesktop = false;
                o.TextMenuTouchMode = TextMenuTouchMode.AppMenu;
            });
            NameField.FocusInput(false);
            yield return Frames();
            H.PointerDown("editor-name");
            H.Advance(0.3);
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "antes de 0,5 s, nada");
            H.Advance(0.3);
            yield return Settle();
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.True, "mantener presionado 0,5 s abre el mismo menú de la PC (R3-13)");
            Assert.That(H.TextOf(TextContextMenu.SelectAllName), Is.EqualTo("Seleccionar todo"), "en el idioma de la app");
            H.PointerUp("editor-name");
            yield return Settle();
        }
    }
}
