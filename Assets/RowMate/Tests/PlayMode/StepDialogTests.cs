using System.Collections;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // SDT · Diálogo de paso en pantalla (F-RUT-13; 03-rutinas/02-tech-design §4.5, §6.3).
    public sealed class StepDialogTests : RoutinesUiTestBase
    {
        private IEnumerator OpenNewStep(RoutineSeed? seed = null, DeviceProfile? profile = null, System.Action<UiHostOptions>? configure = null)
        {
            CreateRoutines(seed ?? RoutineSeeds.Empty, profile, configure);
            yield return OpenNewEditor();
            yield return ScrollAndTap("editor-add-step");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>(), "se abrió el diálogo de paso");
        }

        private IEnumerator OpenEditStep(int index)
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-" + index);
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>());
        }

        private RmTextField Field(string name)
        {
            return H.Find<RmTextField>(name) ?? throw new System.InvalidOperationException("no existe #" + name);
        }

        [UnityTest, Category("AC-RUT-13-01")]
        public IEnumerator TitlesAndDefaults()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("editor-add-step");
            Assert.That(StepDialog.Title.text, Is.EqualTo("Nuevo paso"));
            Assert.That(StepDialog.Types.Selected, Is.EqualTo(1), "Trabajo");
            Assert.That(StepDialog.Mode.Selected, Is.EqualTo(0), "Por tiempo");
            foreach (var name in new[] { "step-min", "step-sec", "step-wmin", "step-wmax", "step-spm", "step-split-min", "step-split-sec" })
            {
                Assert.That(Field(name).Value, Is.Empty, name + " vacío");
            }

            yield return ScrollAndTap("dialog-cancel");
            yield return ScrollAndTap("step-edit-0");
            Assert.That(StepDialog.Title.text, Is.EqualTo("Editar paso"));
        }

        [UnityTest, Category("AC-RUT-13-02")]
        public IEnumerator FieldsInOrder()
        {
            yield return OpenNewStep();
            var order = new[] { "step-type-label", "step-type", "step-mode", "step-min", "step-sec", "step-optional-label", "step-wmin", "step-wmax", "step-spm", "step-split-min", "step-split-sec" };
            var y = float.MinValue;
            var x = float.MinValue;
            for (var i = 0; i < order.Length; i++)
            {
                var bounds = H.BoundsOf(order[i]);
                var sameRow = Mathf.Abs(bounds.yMin - y) < 1f;
                Assert.That(sameRow ? bounds.xMin > x : bounds.yMin > y, Is.True, order[i] + " va después de " + (i > 0 ? order[i - 1] : "el título"));
                y = bounds.yMin;
                x = bounds.xMin;
            }

            Assert.That(H.IsShown("step-meters"), Is.False, "en modo tiempo no se ve Metros");
            Assert.That(Field("step-min").LabelElement.text, Is.EqualTo("Min"));
            Assert.That(Field("step-wmin").LabelElement.text, Is.EqualTo("W mín"));
            Assert.That(Field("step-spm").LabelElement.text, Is.EqualTo("SPM objetivo"));
        }

        [UnityTest, Category("AC-RUT-13-02")]
        public IEnumerator ProgressionsOnlyInASeries()
        {
            yield return OpenNewStep();
            Assert.That(H.Find("step-prog-spm"), Is.Null, "un paso suelto no tiene progresiones");
            yield return ScrollAndTap("dialog-cancel");
            yield return Tap("editor-add-series");
            yield return ScrollAndTap("series-add-step-0");
            Assert.That(H.Find("step-prog-spm"), Is.Not.Null, "en una serie sí");
            Assert.That(H.Find("step-prog-watts"), Is.Not.Null);
            Assert.That(H.Find("step-prog-split"), Is.Not.Null);
            Assert.That(H.TextOf("step-progressions-label"), Is.EqualTo("Progresiones (solo aplica en series)"));
        }

        [UnityTest, Category("AC-RUT-13-02")]
        public IEnumerator HintsWhenEmpty()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            yield return ScrollAndTap("series-add-step-0");
            var spm = Field("step-prog-spm");
            Assert.That(spm.Placeholder, Is.EqualTo("Ej: 2"), "la ayuda");
            Assert.That(spm.IsFocused, Is.False);
            Assert.That(spm.IsFloating, Is.True, "con ayuda, la etiqueta va arriba aunque no tenga foco (DV-19)");
            Assert.That(Field("step-prog-watts").Placeholder, Is.EqualTo("Ej: 5"));
            Assert.That(Field("step-prog-split").Placeholder, Is.EqualTo("Ej: -2 (más rápido)"));
            Assert.That(Field("step-min").IsFloating, Is.False, "sin ayuda ni texto, en reposo");
        }

        [UnityTest, Category("AC-RUT-13-03")]
        public IEnumerator FourTypesOneSelected()
        {
            yield return OpenNewStep();
            var types = StepDialog.Types;
            Assert.That(types.Count, Is.EqualTo(4));
            var names = new[] { "Calentamiento", "Trabajo", "Descanso", "Enfriamiento" };
            var selected = 0;
            for (var i = 0; i < 4; i++)
            {
                Assert.That(types.OptionAt(i).Q<Label>(className: "rm-segmented__label").text, Is.EqualTo(names[i]));
                selected += types.OptionAt(i).ClassListContains(SegmentedControl.SelectedClass) ? 1 : 0;
            }

            Assert.That(selected, Is.EqualTo(1), "uno solo elegido");
            yield return ScrollAndTap("step-type-rest");
            Assert.That(types.Selected, Is.EqualTo(2));
            Assert.That(StepDialog.Preview.Title.text, Is.EqualTo("Descanso"), "la vista previa cambia");
        }

        [UnityTest, Category("AC-RUT-13-04")]
        public IEnumerator ModeSwitchKeepsTheText()
        {
            yield return OpenNewStep();
            yield return TypeInDialog("step-min", "5");
            yield return ScrollAndTap("step-mode-distance");
            Assert.That(H.IsShown("step-meters"), Is.True);
            Assert.That(H.IsShown("step-min"), Is.False);
            yield return TypeInDialog("step-meters", "500");
            yield return ScrollAndTap("step-mode-time");
            Assert.That(Field("step-min").Value, Is.EqualTo("5"), "Min sigue diciendo 5");
            yield return ScrollAndTap("step-mode-distance");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(Editor.Store.Template[0].DistanceMeters, Is.EqualTo(500));
            Assert.That(Editor.Store.Template[0].DurationSeconds, Is.Null, "sin duración");
        }

        [UnityTest, Category("AC-RUT-13-06")]
        public IEnumerator EmptyTimeConfirmsZero()
        {
            yield return OpenNewStep();
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Editor.Store.Template[0].DurationSeconds, Is.EqualTo(0));
            Assert.That(Row(0).Subtitle.Text, Is.EqualTo("0s"));
        }

        [UnityTest, Category("AC-RUT-13-07")]
        public IEnumerator EmptyDistanceIsNoGoal()
        {
            yield return OpenNewStep();
            yield return ScrollAndTap("step-mode-distance");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(Editor.Store.Template[0].DistanceMeters, Is.Null);
            Assert.That(Editor.Store.Template[0].DurationSeconds, Is.Null);
            Assert.That(Row(0).Subtitle.Text, Is.EqualTo("?"));
        }

        [UnityTest, Category("AC-RUT-13-10")]
        public IEnumerator PrefillOnScreen()
        {
            yield return OpenEditStep(1);
            Assert.That(StepDialog.Types.Selected, Is.EqualTo(1), "Trabajo");
            Assert.That(Field("step-min").Value, Is.EqualTo("5"));
            Assert.That(Field("step-sec").Value, Is.EqualTo("0"));
            Assert.That(Field("step-spm").Value, Is.EqualTo("18"));
            Assert.That(Field("step-prog-spm").Value, Is.EqualTo("2"));
            Assert.That(Field("step-wmin").Value, Is.Empty);
        }

        [UnityTest, Category("AC-RUT-13-11")]
        public IEnumerator RangeErrorKeepsTheDialogAndClearsOnChange()
        {
            yield return OpenNewStep();
            yield return TypeInDialog("step-wmin", "99999");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>(), "el diálogo sigue abierto");
            var field = Field("step-wmin");
            Assert.That(field.ErrorText, Does.StartWith("Fuera de rango"));
            Assert.That(field.ClassListContains("rm-field--error"), Is.True);
            yield return TypeInDialog("step-wmin", "9999");
            Assert.That(field.ErrorText, Is.Empty, "al cambiar el valor, el error se va");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Editor.Store.Template[0].TargetWattsMin, Is.EqualTo(9999));
        }

        [UnityTest, Category("AC-RUT-13-11")]
        public IEnumerator FocusGoesToTheFirstError()
        {
            yield return OpenNewStep(null, DeviceProfile.L640);
            yield return TypeInDialog("step-split-sec", "999999");
            yield return TypeInDialog("step-spm", "5000");
            StepDialog.ContentScroll.scrollOffset = Vector2.zero;
            yield return Frames();
            yield return ScrollAndTap("dialog-confirm");
            yield return Frames(3);
            Assert.That(H.FocusedField?.name, Is.EqualTo("step-spm"), "el foco va al primer error (en orden)");
            var viewport = StepDialog.ContentScroll.contentViewport.worldBound;
            var focused = H.BoundsOf("step-spm");
            Assert.That(focused.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.5f), "y queda a la vista");
            Assert.That(focused.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.5f));
        }

        [UnityTest, Category("AC-RUT-13-12")]
        public IEnumerator RepetitionErrorUnderTheProgression()
        {
            CreateRoutines(RoutineSeeds.SeriesTimesTen);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-0");
            yield return TypeInDialog("step-prog-spm", "200");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>());
            var error = Field("step-prog-spm").ErrorText;
            Assert.That(error, Does.Contain("repetición"), "el error dice en qué repetición se pasa");
        }

        [UnityTest, Category("AC-RUT-13-13"), Category("AC-APP-05-07")]
        public IEnumerator TabStaysInsideTheDialog()
        {
            yield return OpenNewStep();
            var dialog = StepDialog;
            StepDialog.ConfirmButton.Focus();
            yield return Frames();
            for (var i = 0; i < 25; i++)
            {
                H.PressTab();
                yield return Frames();
                var focused = H.Focused as VisualElement;
                Assert.That(focused, Is.Not.Null, "siempre hay algo con foco");
                Assert.That(dialog.Box.Contains(focused!) || ReferenceEquals(focused, dialog.Box), Is.True, "Tab no sale del diálogo (" + UiQuery.Describe(focused) + ")");
            }
        }

        [UnityTest, Category("AC-RUT-13-13")]
        public IEnumerator NumericFieldsUseSignedKeyboard()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            yield return ScrollAndTap("series-add-step-0");
            foreach (var name in new[] { "step-min", "step-sec", "step-wmin", "step-wmax", "step-spm", "step-split-min", "step-split-sec", "step-prog-spm", "step-prog-watts", "step-prog-split" })
            {
                Assert.That(Field(name).Input.keyboardType, Is.EqualTo(TouchScreenKeyboardType.NumbersAndPunctuation), name + ": teclado con signo (DV-15)");
            }
        }

        [UnityTest, Category("AC-RUT-13-14")]
        public IEnumerator CancelOutsideEscapeAndBackChangeNothing()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            var before = Editor.Store.Template[0];
            var ways = new[] { "cancelar", "velo", "Escape", "Atrás" };
            foreach (var way in ways)
            {
                yield return ScrollAndTap("step-edit-0");
                yield return TypeInDialog("step-min", "42");
                switch (way)
                {
                    case "cancelar":
                        yield return ScrollAndTap("dialog-cancel");
                        break;
                    case "velo":
                        H.TapScrim();
                        yield return Settle();
                        break;
                    case "Escape":
                        yield return Back(BackSource.Escape);
                        break;
                    default:
                        yield return Back(BackSource.SystemBack);
                        break;
                }

                Assert.That(H.TopDialog, Is.Null, way + " cierra el diálogo");
                Assert.That(H.Editor, Is.Not.Null, way + " no cierra el editor");
                Assert.That(Editor.Store.Template[0], Is.EqualTo(before), way + " no cambia nada");
            }
        }

        [UnityTest, Category("AC-RUT-13-15")]
        public IEnumerator LowWindowScrollsAndKeepsTheButtons()
        {
            CreateRoutines(RoutineSeeds.Lista, DeviceProfile.L640);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-1");
            yield return Frames(2);
            var dialog = StepDialog;
            var window = H.Root.worldBound;
            Assert.That(dialog.Box.worldBound.yMin, Is.GreaterThanOrEqualTo(window.yMin - 0.5f), "el diálogo entra en la ventana");
            Assert.That(dialog.Box.worldBound.yMax, Is.LessThanOrEqualTo(window.yMax + 0.5f));
            Assert.That(dialog.ContentScroll.contentContainer.layout.height, Is.GreaterThan(dialog.ContentScroll.contentViewport.layout.height), "el contenido se desplaza");
            Assert.That(dialog.PreviewIsPinned, Is.False, "alto máximo < 560: la vista previa va dentro del desplazamiento");
            Assert.That(dialog.Preview.parent, Is.SameAs(dialog.Content));
            Assert.That(ChildIndex(dialog.Preview), Is.EqualTo(0), "primera");
            Assert.That(H.IsShown("step-preview"), Is.True, "y se ve");
            Assert.That(dialog.Box.ClassListContains("rm-dialog--compact"), Is.True);
            Assert.That(H.WhyNotTappable("dialog-confirm"), Is.Null, "Confirmar siempre a la vista");
            Assert.That(H.WhyNotTappable("dialog-cancel"), Is.Null);
            H.ScrollDialogToEnd();
            yield return Frames(2);
            Assert.That(H.WhyNotTappable("dialog-confirm"), Is.Null);
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-13-16")]
        public IEnumerator PreviewMatchesTheEditorRow()
        {
            yield return OpenNewStep();
            yield return TypeInDialog("step-min", "2");
            yield return TypeInDialog("step-spm", "22");
            var preview = StepDialog.Preview;
            Assert.That(preview.Title.text, Is.EqualTo("Trabajo"));
            Assert.That(preview.Subtitle.Text, Is.EqualTo("2m  ·  22 spm"));
            Assert.That(preview.Edit, Is.Null, "la vista previa no tiene botones");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(Row(0).Subtitle.Text, Is.EqualTo("2m  ·  22 spm"), "la fila confirmada es la de la vista previa");
            Assert.That(Row(0).Title.text, Is.EqualTo("Trabajo"));
        }

        [UnityTest, Category("AC-APP-05-11")]
        public IEnumerator NoScrollbarOnPhone()
        {
            yield return OpenNewStep(null, DeviceProfile.TV, o => o.IsDesktop = false);
            Assert.That(StepDialog.ContentScroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator TextScale130()
        {
            yield return OpenNewStep(null, DeviceProfile.TV, o => o.TextScale = 1.3);
            yield return Frames(2);
            var window = H.Root.worldBound;
            Assert.That(StepDialog.Box.worldBound.xMax, Is.LessThanOrEqualTo(window.xMax + 0.5f));
            Assert.That(H.WhyNotTappable("dialog-confirm"), Is.Null);
            var audit = H.Audit();
            if (!audit.IsClean)
            {
                var detail = "";
                foreach (var text in UiQuery.TextElements(H.Root))
                {
                    if (text is TextElement element && element.text == "Trabajo")
                    {
                        detail += " | " + string.Join(".", element.GetClasses()) + " en " + UiQuery.Describe(element.parent) + " " + element.layout + " (padre "
                                  + element.parent.layout + ")";
                    }
                }

                Assert.Fail(audit + detail);
            }
        }
    }
}
