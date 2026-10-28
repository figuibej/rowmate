using System.Collections;
using NUnit.Framework;
using RowMate.Presentation.Controls;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // KAV · El campo con foco sobre el teclado del teléfono, simulado con FakeSoftKeyboard (AC-RUT-11-10, F-APP-15.4;
    // 03-rutinas/02-tech-design §4.10; 04-design-notes §4.14). En el teléfono real: [Device-And], NO VERIFICABLE sin módulo.
    public sealed class KeyboardAvoiderTests : RoutinesUiTestBase
    {
        private void ShowKeyboard(DeviceProfile profile, double dp)
        {
            H.Keyboard.Show(dp * profile.Density);
        }

        [UnityTest, Category("AC-RUT-11-10")]
        public IEnumerator FocusedFieldStaysAboveTheKeyboard()
        {
            const double keyboardDp = 200;
            CreateRoutines(RoutineSeeds.Lista, DeviceProfile.TH, o => o.IsDesktop = false);
            yield return OpenEditorOf(1);
            Editor.DescriptionField.FocusInput(false);
            yield return Frames();
            ShowKeyboard(DeviceProfile.TH, keyboardDp);
            yield return Settle();
            yield return Settle();
            var keyboardTop = H.Root.worldBound.yMax - (float)keyboardDp;
            Assert.That(H.Shell.Keyboard.OccludedDp, Is.EqualTo(keyboardDp).Within(0.5));
            Assert.That(H.BoundsOf("editor-description").yMax, Is.LessThanOrEqualTo(keyboardTop - 16 + 1f), "16 dp por encima del teclado");
            Assert.That(H.BoundsOf("editor-description").yMin, Is.GreaterThanOrEqualTo(H.BoundsOf("editor-appbar").yMax - 1f), "sin meterse bajo la barra");
            H.Keyboard.Hide();
            yield return Settle();
            Assert.That(H.Shell.Keyboard.OccludedDp, Is.EqualTo(0));
        }

        [UnityTest, Category("AC-RUT-11-10")]
        public IEnumerator FocusedFieldStaysAboveTheKeyboardInADialog()
        {
            const double keyboardDp = 300;
            CreateRoutines(RoutineSeeds.Lista, DeviceProfile.TV, o => o.IsDesktop = false);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-1");
            var field = H.Find<RmTextField>("step-prog-split")!;
            field.FocusInput(false);
            yield return Frames();
            ShowKeyboard(DeviceProfile.TV, keyboardDp);
            yield return Settle();
            yield return Settle();
            var keyboardTop = H.Root.worldBound.yMax - (float)keyboardDp;
            var box = H.TopDialog!.Box.worldBound;
            Assert.That(box.yMax, Is.LessThanOrEqualTo(keyboardTop + 1f), "el diálogo se recentra sobre el teclado");
            Assert.That(box.height, Is.LessThanOrEqualTo(H.Root.worldBound.height - (float)keyboardDp - 48 + 1f), "alto máximo: área − 48");
            Assert.That(H.BoundsOf("step-prog-split").yMax, Is.LessThanOrEqualTo(keyboardTop - 16 + 1f), "el campo, 16 dp por encima");
            Assert.That(H.WhyNotTappable("dialog-confirm"), Is.Null, "los botones siguen a mano");
        }
    }
}
