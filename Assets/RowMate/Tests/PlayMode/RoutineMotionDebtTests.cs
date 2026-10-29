using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Deuda de la It. 3 saldada al empezar la It. 4a (docs/migration/iterations/04a-motor-hud-funcional/00-deuda-it3.md):
    // - QA-V3-02 (04-design-notes §5.2, §5.5-§5.7 y §16.6, puntos 1 a 4): lo último borrado se desvanece y colapsa antes de que
    //   entre el vacío, el primer paso funde el vacío en su lugar, las hojas de la serie crecen y funden, los colores del tipo
    //   cruzan y la línea de error se abre y se cierra. Cuadro a cuadro con el reloj de prueba: nada salta.
    // - QA-V3-04: UiTestHost.Type deja el cursor al final y Tap mueve el foco como un clic (el arnés, no la app).
    public sealed class RoutineMotionDebtTests : RoutinesUiTestBase
    {
        // VID-01 a los 8 s: el tacho de la única serie. Antes, el bloque desaparecía en el cuadro 0 y el vacío aparecía entero.
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-14-10")]
        public IEnumerator LastBlockCollapsesBeforeTheEmptyText()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            var slot = Block(0).parent;
            var full = slot.layout.height;
            Assert.That(full, Is.GreaterThan(100), "la serie con sus dos pasos");
            H.Tap("series-delete-0");
            yield return Frames();
            Assert.That(Editor.ShowsEmpty, Is.True, "sin pasos, el vacío vale en el acto");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (0)"));
            yield return CheckCollapseThenEmpty(slot, full, Editor.Steps, Editor.EmptyLabel);
        }

        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-14-10")]
        public IEnumerator LastLooseStepCollapsesBeforeTheEmptyText()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return AddLooseStep(("step-min", "1"));
            var slot = Row(0).parent;
            var full = slot.layout.height;
            Assert.That(full, Is.GreaterThan(50));
            H.Tap("step-delete-0");
            yield return Frames();
            yield return CheckCollapseThenEmpty(slot, full, Editor.Steps, Editor.EmptyLabel);
        }

        // 04-design-notes §5.5: "si era la última, el vacío entra al terminar" (antes la lista se ocultaba en el acto).
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-10-02")]
        public IEnumerator LastRoutineCollapsesBeforeTheEmptyState()
        {
            CreateWith(new[] { RoutineSeeds.EscaleraSpm() });
            yield return GoToRoutines();
            var slot = Screen.List.CardAt(0)!.parent;
            var full = slot.layout.height;
            yield return Tap("routine-menu-0");
            yield return Tap("routine-menu-delete");
            H.Tap("dialog-confirm");
            yield return Frames();
            Assert.That(Screen.List.Count, Is.EqualTo(0), "la rutina ya no está");
            yield return CheckCollapseThenEmpty(slot, full, Screen.List, H.Find("routines-empty")!);
        }

        // Lo borrado: el alto nunca sube ni salta más que la curva, y mientras se ve, el vacío no; después el vacío entra
        // fundiendo y subiendo 8 dp (240) y queda en reposo sin nada en línea.
        private IEnumerator CheckCollapseThenEmpty(VisualElement slot, float full, VisualElement list, VisualElement empty)
        {
            var previous = slot.layout.height;
            var biggestStep = 0f;
            var frames = 0;
            while (slot.panel != null && frames++ < 40)
            {
                Assert.That(list.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "lo borrado se ve mientras colapsa (cuadro " + frames + ")");
                Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "el vacío espera al colapso (cuadro " + frames + ")");
                H.Step();
                yield return null;
                if (slot.panel == null)
                {
                    break;
                }

                var height = slot.layout.height;
                Assert.That(height, Is.LessThanOrEqualTo(previous + 0.5f), "el alto solo baja (cuadro " + frames + ")");
                biggestStep = Mathf.Max(biggestStep, previous - height);
                previous = height;
            }

            Assert.That(slot.panel, Is.Null, "terminó el colapso y el slot salió del árbol");
            Assert.That(frames, Is.GreaterThan(10), "el colapso dura 280 ms (" + frames + " cuadros)");
            Assert.That(biggestStep, Is.LessThan(full * 0.35f), "nada salta: el paso más grande fue " + biggestStep + " de " + full);
            yield return Frames();
            Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "el vacío entra al terminar");
            H.Step();
            yield return Frames();
            Assert.That(empty.resolvedStyle.opacity, Is.GreaterThan(0f).And.LessThan(1f), "entra fundiendo");
            yield return Settle();
            Assert.That(empty.resolvedStyle.opacity, Is.EqualTo(1f), "en reposo");
            Assert.That(empty.style.opacity.keyword, Is.EqualTo(StyleKeyword.Null), "sin nada en línea");
            Assert.That(list.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        // §5.6: con el primer paso, el vacío funde → 0 en 120 en su lugar (fuera del flujo) mientras la fila nueva crece.
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-11-04")]
        public IEnumerator FirstStepFadesTheEmptyTextOutInPlace()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            var empty = Editor.EmptyLabel;
            var before = empty.worldBound;
            H.Tap("editor-add-series");
            H.Step();
            yield return Frames();
            Assert.That(empty.resolvedStyle.position, Is.EqualTo(Position.Absolute), "sale del flujo: la serie toma su lugar");
            Assert.That(empty.resolvedStyle.opacity, Is.GreaterThan(0f).And.LessThan(1f), "funde");
            Assert.That(empty.worldBound.yMin, Is.EqualTo(before.yMin).Within(0.5f), "en su mismo lugar");
            Assert.That(Editor.Steps.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            H.Advance(MotionSpecs.EmptyOutMs / 1000.0);
            yield return Frames();
            Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "a los 120 ms ya no está");
            Assert.That(empty.style.position.keyword, Is.EqualTo(StyleKeyword.Null), "sin nada en línea");
            yield return Settle();
        }

        // DD4a-05 a (04a/00-deuda-diseno §2.3): si se agrega algo mientras el vacío entra, sale desde la opacidad que tiene, sin
        // destello a 1.
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-11-04")]
        public IEnumerator EmptyTextLeavesFromWhereItWas()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            H.Tap("series-delete-0");
            var empty = Editor.EmptyLabel;
            for (var i = 0; i < 40 && !(empty.resolvedStyle.display == DisplayStyle.Flex && empty.resolvedStyle.opacity > 0.25f); i++)
            {
                H.Step();
                yield return null;
            }

            var seen = empty.resolvedStyle.opacity;
            Assert.That(seen, Is.GreaterThan(0.25f).And.LessThan(0.95f), "el vacío está entrando");
            H.Tap("editor-add-series");
            for (var i = 0; i < 12 && empty.resolvedStyle.display == DisplayStyle.Flex; i++)
            {
                Assert.That(empty.resolvedStyle.opacity, Is.LessThanOrEqualTo(seen + 0.01f), "nunca vuelve a 1 (cuadro " + i + ")");
                H.Step();
                yield return null;
            }

            Assert.That(empty.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "se fue en 120");
            yield return Settle();
            Assert.That(Block(0).VisibleLayers, Is.EqualTo(2), "y la serie nueva está");
        }

        // §5.6: ×N → OK, las hojas aparecen o se van con alto 0 ↔ 3 y opacidad en 160 (antes, en el acto).
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-14-02")]
        public IEnumerator SeriesSheetsGrowAndFade()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            Assert.That(Block(0).VisibleLayers, Is.EqualTo(2), "×3: dos hojas");
            yield return SetRepeat("1");
            var block = Block(0);
            Assert.That(block.VisibleLayers, Is.EqualTo(0), "las hojas que quedan, en el acto");
            H.Advance(0.06);
            yield return Frames();
            Assert.That(block.LayersAnimating, Is.True, "se van con su tiempo");
            Assert.That(block.LayerOne.resolvedStyle.opacity, Is.GreaterThan(0f).And.LessThan(1f), "hoja 1 fundiendo");
            Assert.That(block.LayerOne.layout.height, Is.GreaterThan(0f).And.LessThan(3f), "hoja 1 bajando su alto");
            yield return Settle();
            Assert.That(block.LayerOne.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(block.LayerTwo.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            yield return SetRepeat("2");
            Assert.That(block.VisibleLayers, Is.EqualTo(1));
            H.Advance(0.06);
            yield return Frames();
            Assert.That(block.LayerOne.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(block.LayerOne.layout.height, Is.GreaterThan(0f).And.LessThan(3f), "hoja 1 creciendo");
            Assert.That(block.LayerTwo.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "×2: la segunda no");
            yield return Settle();
            Assert.That(block.LayerOne.layout.height, Is.EqualTo(3f).Within(0.1f), "el alto del USS (3 dp, al píxel)");
            Assert.That(block.LayerOne.style.height.keyword, Is.EqualTo(StyleKeyword.Null), "sin nada en línea en reposo");
            Assert.That(block.LayerOne.resolvedStyle.opacity, Is.EqualTo(1f));
        }

        private IEnumerator SetRepeat(string amount)
        {
            yield return ScrollAndTap("series-repeat-0");
            yield return TypeInDialog("repeat-amount", amount);
            H.Tap("dialog-confirm");
            yield return Frames();
        }

        // §5.7: el mosaico elegido cruza fondo, borde y etiqueta, el anterior al revés y la vista previa cambia sus colores en el
        // mismo tiempo (160 ease.move); al terminar, lo del USS queda suelto y lo del código en su color final.
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-13-01")]
        public IEnumerator TypeColorsCrossFade()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return ScrollAndTap("editor-add-step");
            var dialog = StepDialog;
            var work = IndexOf(StepType.Work);
            var rest = IndexOf(StepType.Rest);
            Assert.That(dialog.Types.Selected, Is.EqualTo(work), "un paso nuevo empieza en Trabajo");
            H.Tap("step-type-rest");
            yield return Frames();
            H.Advance(0.08);
            yield return Frames();
            Assert.That(dialog.Types.FadeOf(rest)!.IsRunning, Is.True);
            Assert.That(dialog.Preview.TypeFade.IsRunning, Is.True);
            var border = dialog.Preview.resolvedStyle.borderLeftColor;
            AssertBetween(border, TokenColors.ToColor(StepTypeInfo.Color(StepType.Work)), TokenColors.ToColor(StepTypeInfo.Color(StepType.Rest)), "borde de la vista previa a mitad");
            var tile = dialog.Types.OptionAt(rest);
            Assert.That(tile.resolvedStyle.backgroundColor.a, Is.GreaterThan(0.01f).And.LessThan(0.19f), "fondo del mosaico a mitad");
            yield return Settle();
            AssertColor(dialog.Preview.resolvedStyle.borderLeftColor, StepTypeInfo.Color(StepType.Rest), "borde final de la vista previa");
            Assert.That(dialog.Preview.style.borderLeftColor.keyword, Is.EqualTo(StyleKeyword.Null), "el borde vuelve al USS");
            AssertColor(dialog.Preview.Title.resolvedStyle.color, StepRowModelHud(StepType.Rest), "título en step.rest.hud");
            Assert.That(dialog.Preview.Title.style.color.keyword, Is.EqualTo(StyleKeyword.Null));
            var restBg = TokenColors.WithAlpha(StepTypeInfo.Color(StepType.Rest), 0.2);
            Assert.That(tile.resolvedStyle.backgroundColor.a, Is.EqualTo(restBg.a).Within(0.01f), "fondo final del mosaico (tipo @20 %)");
            AssertColor(tile.resolvedStyle.borderLeftColor, StepTypeInfo.Color(StepType.Rest), "borde final del mosaico");
            var previous = dialog.Types.OptionAt(work);
            Assert.That(previous.resolvedStyle.backgroundColor.a, Is.EqualTo(0f).Within(0.01f), "el anterior, sin fondo");
            AssertColor(previous.resolvedStyle.borderLeftColor, Tokens.BorderDefault, "el anterior, borde de reposo");
            Assert.That(tile.Q<Label>(className: "rm-segmented__label").style.color.keyword, Is.EqualTo(StyleKeyword.Null), "la etiqueta vuelve al USS");
        }

        // §5.10: reducir movimiento no cambia los fundidos de color.
        [UnityTest, Category("QA-V3-02"), Category("AC-APP-05-08")]
        public IEnumerator TypeColorsCrossFadeAlsoWithReducedMotion()
        {
            CreateRoutines(RoutineSeeds.Empty, null, o => o.ReduceMotion = true);
            yield return OpenNewEditor();
            yield return ScrollAndTap("editor-add-step");
            H.Tap("step-type-cooldown");
            yield return Frames();
            Assert.That(StepDialog.Preview.TypeFade.IsRunning, Is.True, "el cruce corre igual");
            yield return Settle();
            AssertColor(StepDialog.Preview.resolvedStyle.borderLeftColor, StepTypeInfo.Color(StepType.Cooldown), "borde final");
        }

        // §5.2: la caja cambia en el acto (clase) y la línea de error abre su alto de 0 al de su texto y funde en 160; al
        // corregir el campo, al revés, con el texto viejo a la vista hasta que se cierra.
        [UnityTest, Category("QA-V3-02"), Category("AC-RUT-13-11")]
        public IEnumerator ErrorLineOpensAndClosesInPlace()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return ScrollAndTap("editor-add-step");
            yield return TypeInDialog("step-wmin", "10000");
            var field = H.Find<RmTextField>("step-wmin")!;
            H.Tap("dialog-confirm");
            Assert.That(field.ClassListContains("rm-field--error"), Is.True, "la caja, en el acto");
            Assert.That(field.ErrorText, Does.StartWith("Fuera de rango"));
            var line = field.ErrorLine;
            yield return Frames();
            H.Step();
            H.Step();
            H.Step();
            yield return Frames();
            Assert.That(line.IsAnimating, Is.True, "la línea se está abriendo");
            Assert.That(line.Slot.layout.height, Is.GreaterThan(0f).And.LessThan(line.Label.layout.height + 4f), "a mitad de su alto");
            Assert.That(line.Label.resolvedStyle.opacity, Is.GreaterThan(0f).And.LessThan(1f), "fundiendo");
            yield return Settle();
            Assert.That(line.IsAnimating, Is.False);
            Assert.That(line.Slot.layout.height, Is.EqualTo(line.Label.layout.height + 4f).Within(0.5f), "abierta: el texto y su margen de 4");
            Assert.That(line.Slot.style.height.keyword, Is.EqualTo(StyleKeyword.Null), "en reposo, nada en línea");
            Assert.That(line.Label.style.position.keyword, Is.EqualTo(StyleKeyword.Null));

            H.Type("step-wmin", "100");
            Assert.That(field.ErrorText, Is.Empty, "el error lógico se va en el acto");
            Assert.That(field.ClassListContains("rm-field--error"), Is.False, "y la caja también");
            yield return Frames();
            H.Step();
            H.Step();
            H.Step();
            yield return Frames();
            Assert.That(line.IsAnimating, Is.True, "la línea se está cerrando");
            Assert.That(line.Label.text, Does.StartWith("Fuera de rango"), "con el texto viejo a la vista");
            yield return Settle();
            Assert.That(line.Label.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "cerrada");
            Assert.That(line.Label.text, Is.Empty);
            Assert.That(line.Slot.layout.height, Is.EqualTo(0f).Within(0.01f));
        }

        // §5.0: la línea de error con reducir movimiento, en el acto.
        [UnityTest, Category("QA-V3-02"), Category("AC-APP-05-08")]
        public IEnumerator ErrorLineIsInstantWithReducedMotion()
        {
            CreateRoutines(RoutineSeeds.Empty, null, o => o.ReduceMotion = true);
            yield return OpenNewEditor();
            yield return ScrollAndTap("editor-add-step");
            yield return TypeInDialog("step-wmin", "10000");
            var field = H.Find<RmTextField>("step-wmin")!;
            H.Tap("dialog-confirm");
            Assert.That(field.ErrorLine.IsAnimating, Is.False, "sin animación");
            yield return Frames();
            Assert.That(field.ErrorLine.Label.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "entera desde el primer cuadro");
            Assert.That(field.ErrorLine.Label.resolvedStyle.opacity, Is.EqualTo(1f));
            yield return Settle();
        }

        // QA-V3-04: Type deja el cursor al final, como al teclear ("|500" en CAP-16 antes).
        [UnityTest, Category("QA-V3-04")]
        public IEnumerator TypeLeavesTheCaretAtTheEnd()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            H.Type("editor-name", "Escalera SPM");
            yield return Frames();
            Assert.That(Editor.NameField.Selection, Is.EqualTo((12, 12)));
            H.Type("editor-name", "A\nB");
            yield return Frames();
            Assert.That(Editor.NameField.Value, Is.EqualTo("AB"), "el campo de una línea filtra");
            Assert.That(Editor.NameField.Selection, Is.EqualTo((2, 2)), "al final de lo que quedó");
        }

        // QA-V3-04: un Tap le da el foco a lo que toca si es enfocable, como un clic. Antes el foco quedaba en el nombre, al cerrar
        // el diálogo ExitLock se lo devolvía y selectAllOnFocus lo seleccionaba entero (DEMO-04 y DEMO-06).
        [UnityTest, Category("QA-V3-04")]
        public IEnumerator TapMovesTheFocusLikeAClick()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            H.Type("editor-name", "Escalera SPM");
            yield return Frames();
            Assert.That(Editor.NameField.IsFocused, Is.True);
            yield return ScrollAndTap("editor-add-step");
            Assert.That(Editor.NameField.IsFocused, Is.False, "el botón se llevó el foco");
            yield return TypeInDialog("step-min", "5");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Editor.NameField.IsFocused, Is.False, "al cerrar el diálogo el foco no vuelve al nombre");
            Assert.That(H.FocusedField, Is.Null);
            Assert.That(H.Shell.KeyboardFocus.IsKeyboardNavigation, Is.False, "sin teclado, sin anillo");
        }

        // QA-V3-06: TabularNumber mide cada carácter que dibuja con su avance real aunque el glifo todavía no esté en el atlas. Antes
        // caía a 0,5 em y el ancho del chip ×N dependía de lo que se había dibujado antes (CAP-10 contra CAP-11 en la It. 3). La
        // barra vertical no la dibuja ningún otro texto de estas pruebas: llega sin glifo.
        [UnityTest, Category("QA-V3-06"), Category("AC-APP-05-04")]
        public IEnumerator TabularNumberMeasuresEveryCharacterItDraws()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            var count = Block(0).Count;
            var font = count.Font;
            Assert.That(font, Is.Not.Null, "el chip tiene su FontAsset numérico");
            count.SetValue("|", 0);
            yield return Frames();
            Assert.That(font!.characterLookupTable.TryGetValue('|', out var character) && character.glyph != null, Is.True, "el glifo quedó en el atlas");
            var size = count.resolvedStyle.fontSize;
            var face = font.faceInfo;
            var expected = character.glyph.metrics.horizontalAdvance * (size / face.pointSize * (face.scale > 0 ? face.scale : 1f)) * character.glyph.scale;
            Assert.That(count.GetCellRect(0).width, Is.EqualTo(expected).Within(0.01f), "su avance real, no 0,5 em");
            Assert.That(expected, Is.LessThan(0.45f * size), "más angosto que la celda de respaldo");
        }

        // El fundamento de TapMovesTheFocusLikeAClick: en UI Toolkit, un PointerDown de verdad sobre un botón enfocable le da el
        // foco (lo que hace el FocusController al presionar). Si esto dejara de pasar, Tap tendría que dejar de mover el foco.
        [UnityTest, Category("QA-V3-04")]
        public IEnumerator PointerDownFocusesAButton()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            H.Type("editor-name", "Escalera SPM");
            yield return Frames();
            H.PointerDown("editor-add-step");
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(H.Find("editor-add-step")), "el botón presionado tiene el foco");
            Assert.That(Editor.NameField.IsFocused, Is.False);
            H.PointerUp("editor-add-step");
            yield return Settle();
        }

        private static int IndexOf(StepType type)
        {
            var order = StepTypeInfo.SelectorOrder;
            for (var i = 0; i < order.Count; i++)
            {
                if (order[i] == type)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(type));
        }

        private static ColorToken StepRowModelHud(StepType type)
        {
            return RowMate.App.Routines.StepRowModel.HudColor(type);
        }

        private static void AssertBetween(Color actual, Color from, Color to, string what)
        {
            var total = Distance(from, to);
            Assert.That(total, Is.GreaterThan(0.05f), what + ": los dos extremos son distintos");
            Assert.That(Distance(actual, from), Is.GreaterThan(0.01f).And.LessThan(total), what + ": ya salió del color de antes");
            Assert.That(Distance(actual, to), Is.GreaterThan(0.01f).And.LessThan(total), what + ": todavía no llegó al nuevo");
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }
    }
}
