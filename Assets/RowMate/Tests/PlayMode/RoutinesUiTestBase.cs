using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Base de las pruebas de Rutinas y del editor (03-rutinas/02-tech-design §6.2): un UiTestHost con las rutinas de la
    // semilla pedida sobre SQLite real (TempDatabase + InlineDbExecutor: determinista cuadro a cuadro), en memoria (CreateWith)
    // o de guion (CreateScripted), con el reloj de creación de prueba (SequenceClock) y el portapapeles de prueba. Por
    // defecto, el teléfono (TV) en escritorio; cada prueba pide lo suyo.
    public abstract class RoutinesUiTestBase : UiTestBase
    {
        protected UiTestHost CreateRoutines(RoutineSeed seed, DeviceProfile? profile = null, Action<UiHostOptions>? configure = null)
        {
            var options = new UiHostOptions { Profile = profile ?? DeviceProfile.TV, Routines = HostRoutines.Seeded(seed) };
            configure?.Invoke(options);
            return Create(options);
        }

        // Rutinas armadas por la prueba, en memoria (las mismas reglas de orden que SQLite).
        protected UiTestHost CreateWith(IEnumerable<Routine> routines, Action<UiHostOptions>? configure = null)
        {
            var options = new UiHostOptions { Routines = HostRoutines.InMemory(routines) };
            configure?.Invoke(options);
            return Create(options);
        }

        protected UiTestHost CreateScripted(Action<ScriptedRoutineRepository> configure, IEnumerable<Routine>? initial = null, Action<UiHostOptions>? options = null)
        {
            var host = new UiHostOptions { Routines = HostRoutines.Scripted(configure, initial) };
            options?.Invoke(host);
            return Create(host);
        }

        protected RoutinesScreenView Screen => H.RoutinesScreen ?? throw new InvalidOperationException("sin la pantalla Rutinas");

        protected RoutineEditorPage Editor => H.Editor ?? throw new InvalidOperationException("no hay un editor abierto");

        // Lo que está en la lista de la app, en orden.
        protected IReadOnlyList<Routine> Listed => H.App.Routines.State.Value.Routines;

        protected IEnumerator GoToRoutines()
        {
            yield return Tap("tab-routines");
        }

        protected IEnumerator OpenNewEditor()
        {
            yield return GoToRoutines();
            yield return Tap("routines-add");
            Assert.That(H.Editor, Is.Not.Null, "se abrió el editor");
        }

        protected IEnumerator OpenEditorOf(int card)
        {
            yield return GoToRoutines();
            yield return Tap("routine-card-" + card);
            Assert.That(H.Editor, Is.Not.Null, "se abrió el editor");
        }

        protected IEnumerator Type(string field, string text)
        {
            yield return Frames();
            H.Type(field, text);
            yield return Settle();
        }

        protected IEnumerator Back(BackSource source)
        {
            yield return Frames();
            H.PressBack(source);
            yield return Settle();
        }

        // Toca hasta que se pueda (lo lleva a la vista si está en un ScrollView).
        protected IEnumerator ScrollAndTap(string elementName)
        {
            yield return Frames();
            if (H.WhyNotTappable(elementName) != null && UiQuery.ScrollViewOf(H.Find(elementName)!) != null)
            {
                H.ScrollTo(elementName);
                yield return Frames();
            }

            H.Tap(elementName);
            yield return Settle();
        }

        // Escribe en un campo del diálogo de arriba (lo lleva a la vista primero).
        protected IEnumerator TypeInDialog(string field, string text)
        {
            yield return Frames();
            if (H.WhyNotTappable(field) != null && UiQuery.ScrollViewOf(H.Find(field)!) != null)
            {
                H.ScrollTo(field);
                yield return Frames();
            }

            H.Type(field, text);
            yield return Settle();
        }

        // + Paso → (campos) → Confirmar.
        protected IEnumerator AddLooseStep(params (string Field, string Text)[] values)
        {
            yield return ScrollAndTap("editor-add-step");
            foreach (var value in values)
            {
                yield return TypeInDialog(value.Field, value.Text);
            }

            yield return ScrollAndTap("dialog-confirm");
        }

        protected StepDialog StepDialog => H.TopDialog as StepDialog ?? throw new InvalidOperationException("no hay un diálogo de paso abierto");

        protected RepeatDialog RepeatDialog => H.TopDialog as RepeatDialog ?? throw new InvalidOperationException("no hay un diálogo de repeticiones abierto");

        protected StepRowView Row(int index)
        {
            return H.Find("step-row-" + index) as StepRowView ?? throw new InvalidOperationException("no existe #step-row-" + index);
        }

        protected SeriesBlockView Block(int ordinal)
        {
            return H.Find("series-" + ordinal) as SeriesBlockView ?? throw new InvalidOperationException("no existe #series-" + ordinal);
        }

        protected int RowCount()
        {
            var count = 0;
            while (H.Find("step-row-" + count) != null)
            {
                count++;
            }

            return count;
        }

        protected static int ChildIndex(VisualElement element)
        {
            return element.parent == null ? -1 : element.parent.IndexOf(element);
        }

        protected static void AssertColor(Color actual, ColorToken token, string what, float tolerance = 0.01f)
        {
            var expected = TokenColors.ToColor(token);
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), what + " (r)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), what + " (g)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), what + " (b)");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), what + " (a)");
        }

        protected static float LineHeightOf(Label label, string sample = "Ag")
        {
            return label.MeasureTextSize(sample, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
        }
    }
}
