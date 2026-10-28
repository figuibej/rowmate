using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Routines;
using RowMate.Tests.Support;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // RPT · Diálogo de repeticiones (AC-RUT-14-04 a -06; 03-rutinas/02-tech-design §4.5).
    public sealed class RepeatDialogTests : RoutinesUiTestBase
    {
        private IEnumerator OpenRepeat(bool desktop = true)
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.IsDesktop = desktop);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("series-repeat-0");
            Assert.That(H.TopDialog, Is.InstanceOf<RepeatDialog>(), "se abrió el diálogo de repeticiones");
        }

        [UnityTest, Category("AC-RUT-14-04")]
        public IEnumerator PrefillAndFocusOnDesktop()
        {
            yield return OpenRepeat();
            Assert.That(RepeatDialog.Title.text, Is.EqualTo("Repeticiones"));
            Assert.That(RepeatDialog.Field.LabelElement.text, Is.EqualTo("Cantidad"));
            Assert.That(RepeatDialog.Field.Value, Is.EqualTo("3"), "precargado con la cantidad actual");
            Assert.That(RepeatDialog.Field.IsFocused, Is.True, "con el foco al abrir");
            Assert.That(RepeatDialog.Field.Selection, Is.EqualTo((0, 1)), "escritorio: todo seleccionado");
        }

        [UnityTest, Category("AC-RUT-14-04")]
        public IEnumerator PrefillAndFocusOnPhone()
        {
            yield return OpenRepeat(false);
            Assert.That(RepeatDialog.Field.Value, Is.EqualTo("3"));
            Assert.That(RepeatDialog.Field.IsFocused, Is.True);
            Assert.That(RepeatDialog.Field.Selection, Is.EqualTo((1, 1)), "teléfono: el cursor al final");
        }

        [UnityTest, Category("AC-RUT-14-05")]
        public IEnumerator Rules()
        {
            yield return OpenRepeat();
            yield return TypeInDialog("repeat-amount", "5");
            yield return Tap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Block(0).Count.Text, Is.EqualTo("×5"));
            yield return ScrollAndTap("series-repeat-0");
            yield return TypeInDialog("repeat-amount", "0");
            yield return Tap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null, "0 cierra sin cambiar");
            Assert.That(Block(0).Count.Text, Is.EqualTo("×5"));
            yield return ScrollAndTap("series-repeat-0");
            yield return TypeInDialog("repeat-amount", "abc");
            yield return Tap("dialog-confirm");
            Assert.That(Block(0).Count.Text, Is.EqualTo("×1"), "lo que no es entero cuenta como 1 (como el legacy)");
        }

        [UnityTest, Category("AC-RUT-14-06")]
        public IEnumerator OutOfRangeKeepsTheDialog()
        {
            yield return OpenRepeat();
            yield return TypeInDialog("repeat-amount", "1000");
            yield return Tap("dialog-confirm");
            Assert.That(H.TopDialog, Is.InstanceOf<RepeatDialog>(), "sigue abierto");
            Assert.That(RepeatDialog.Field.ErrorText, Is.EqualTo("Máximo 999"));
            yield return TypeInDialog("repeat-amount", "492");
            Assert.That(RepeatDialog.Field.ErrorText, Is.Empty, "al escribir, el error se va");
            yield return Tap("dialog-confirm");
            Assert.That(RepeatDialog.Field.ErrorText, Is.EqualTo("Con ×492, un objetivo queda fuera de rango"), "SPM 18 + 2 por repetición");
            yield return TypeInDialog("repeat-amount", "491");
            yield return Tap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Block(0).Count.Text, Is.EqualTo("×491"));
        }
    }
}
