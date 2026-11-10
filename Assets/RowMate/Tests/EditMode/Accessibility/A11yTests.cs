using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Accessibility;
using RowMate.Core.Localization;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Accessibility
{
    // AO · El orden de recorrido del lector (It. 14, AC-APP-03-12).
    [TestFixture]
    public sealed class A11yOrderTests
    {
        [Test, Category("AC-APP-03-12")]
        public void TopToBottomLeftToRightWithRowTolerance()
        {
            var items = new List<A11yItem>
            {
                Item("tab-history", 300, 850), Item("tab-device", 0, 852), Item("title", 16, 10),
                Item("back", 0, 14), Item("tile-b", 210, 120), Item("tile-a", 10, 126), Item("tile-c", 10, 140),
            };
            A11yOrder.Sort(items);
            Assert.That(items.Select(i => i.Key).ToArray(), Is.EqualTo(new object[]
            {
                "back", "title", "tile-a", "tile-b", "tile-c", "tab-device", "tab-history",
            }), "back y title en la misma fila (4 dp); tile-a 6 dp más abajo que tile-b sigue en su fila; tile-c a 20 dp, fila nueva");
        }

        [Test, Category("AC-APP-03-12")]
        public void StableForEqualPositions()
        {
            var items = new List<A11yItem> { Item("first", 0, 0), Item("second", 0, 0), Item("third", 0, 0) };
            A11yOrder.Sort(items);
            Assert.That(items.Select(i => i.Key).ToArray(), Is.EqualTo(new object[] { "first", "second", "third" }));
        }

        public static A11yItem Item(string key, double x, double y, A11yRole role = A11yRole.Button, string label = "")
        {
            return new A11yItem(key, role, label.Length == 0 ? key : label, x, y, 40, 20);
        }
    }

    // AT · Los textos que solo oye el lector (It. 14, spec §4; AC-APP-03-12, AC-APP-03-14).
    [TestFixture]
    public sealed class A11yTextTests
    {
        [Test, Category("AC-APP-03-12")]
        public void LabelValue()
        {
            Assert.That(A11yText.LabelValue(LocalizationFixture.Es, "Split /500m", "2:13"), Is.EqualTo("Split /500m: 2:13"));
            Assert.That(A11yText.LabelValue(LocalizationFixture.En, "Watts", "120 W"), Is.EqualTo("Watts: 120 W"));
        }

        [Test, Category("AC-APP-03-14")]
        public void StepProgressAndChartSummary()
        {
            Assert.That(A11yText.StepProgress(LocalizationFixture.Es, 2, 5, "Trabajo"), Is.EqualTo("Paso 2 de 5: Trabajo"));
            Assert.That(A11yText.StepProgress(LocalizationFixture.En, 2, 5, "Work"), Is.EqualTo("Step 2 of 5: Work"));
            Assert.That(A11yText.ChartSummary(LocalizationFixture.Es, "Watts", "98", "212"), Is.EqualTo("Watts: de 98 a 212"));
            Assert.That(A11yText.ChartSummary(LocalizationFixture.En, "Watts", "98", "212"), Is.EqualTo("Watts: from 98 to 212"));
        }
    }

    // AP · La política de anuncios (It. 14, AC-APP-03-14; DV-136).
    [TestFixture]
    public sealed class A11yAnnouncePolicyTests
    {
        [Test, Category("AC-APP-03-14")]
        public void TitlesToastsStepsAndCompletionOnce()
        {
            var policy = new A11yAnnouncePolicy();
            Assert.That(policy.Title("Dispositivo"), Is.True);
            Assert.That(policy.Title("Dispositivo"), Is.False, "el mismo título no se repite");
            Assert.That(policy.Title("Historial"), Is.True);
            Assert.That(policy.Title(""), Is.False);
            Assert.That(policy.Toast(1), Is.True);
            Assert.That(policy.Toast(1), Is.False, "un aviso, una vez por Show");
            Assert.That(policy.Toast(2), Is.True);
            Assert.That(policy.Step(7, 0), Is.True);
            Assert.That(policy.Step(7, 0), Is.False);
            Assert.That(policy.Step(7, 1), Is.True);
            Assert.That(policy.Step(8, 1), Is.True, "otra sesión");
            Assert.That(policy.Completion(8), Is.True);
            Assert.That(policy.Completion(8), Is.False);
        }

        [Test, Category("AC-APP-03-14")]
        public void MetricsNeverAnnounceThemselves()
        {
            Assert.That(new A11yAnnouncePolicy().Metric(), Is.False);
        }
    }

    // AD · El volcado del árbol (It. 14, DT14-22; CAP-27-a11y.txt).
    [TestFixture]
    public sealed class A11yTreeDumpTests
    {
        [Test, Category("AC-APP-03-12")]
        public void OneLinePerItemWithRoleNameValueAndState()
        {
            var tab = new A11yItem("t", A11yRole.TabButton, "Historial", 0, 0, 10, 10) { State = A11yState.Selected };
            var metric = new A11yItem("m", A11yRole.StaticText, "Watts: 120 W", 0, 0, 10, 10);
            var button = new A11yItem("b", A11yRole.Button, "Exportar\nTCX", 0, 0, 10, 10) { State = A11yState.Disabled | A11yState.Expanded, Value = "x" };
            Assert.That(A11yTreeDump.Dump(new[] { tab, metric, button }), Is.EqualTo(
                "1. TabButton · Historial · [Selected]\n2. StaticText · Watts: 120 W\n3. Button · Exportar TCX · x · [Disabled, Expanded]\n"));
            Assert.That(A11yTreeDump.Dump(new A11yItem[0]), Is.EqualTo(""));
        }
    }

    // AR · La reconciliación por clave estable (It. 14, DT14-19, QA-G14-06).
    [TestFixture]
    public sealed class A11yReconcilerTests
    {
        [Test, Category("AC-APP-03-12")]
        public void NoChangesIsOnlyKeep()
        {
            var a = new object();
            var b = new object();
            var edits = new List<A11yEdit>();
            A11yReconciler.Diff(new[] { a, b }, new[] { a, b }, edits);
            Assert.That(edits.Select(e => e.Kind).ToArray(), Is.EqualTo(new[] { A11yEditKind.Keep, A11yEditKind.Keep }));
            Assert.That(A11yReconciler.HasChanges(edits), Is.False);
        }

        [Test, Category("AC-APP-03-12")]
        public void InsertMoveRemoveReproduceTheNewList()
        {
            var keys = Enumerable.Range(0, 6).Select(i => (object)("k" + i)).ToArray();
            var previous = new[] { keys[0], keys[1], keys[2], keys[3] };
            var next = new[] { keys[4], keys[2], keys[0], keys[5], keys[3] };
            var edits = new List<A11yEdit>();
            A11yReconciler.Diff(previous, next, edits);
            Assert.That(A11yReconciler.HasChanges(edits), Is.True);
            Assert.That(edits.Count(e => e.Kind == A11yEditKind.Remove), Is.EqualTo(1), "solo sale k1");
            Assert.That(edits.Count(e => e.Kind == A11yEditKind.Insert), Is.EqualTo(2), "entran k4 y k5");
            Assert.That(Apply(previous, edits), Is.EqualTo(next));
        }

        [Test, Category("AC-APP-03-12")]
        public void IdentityNotEquality()
        {
            // Dos claves iguales por Equals pero distintas por identidad son elementos distintos (el VisualElement).
            var a = new string('a', 3);
            var b = new string('a', 3);
            var edits = new List<A11yEdit>();
            A11yReconciler.Diff(new object[] { a }, new object[] { b }, edits);
            Assert.That(edits.Select(e => e.Kind).ToArray(), Is.EqualTo(new[] { A11yEditKind.Remove, A11yEditKind.Insert }));
        }

        [Test, Category("AC-APP-03-12")]
        public void RandomizedListsRoundTrip()
        {
            var state = 14u;
            int Next(int bound)
            {
                state = state * 1664525u + 1013904223u;
                return (int)((state >> 8) % (uint)bound);
            }

            var pool = Enumerable.Range(0, 30).Select(i => new object()).ToArray();
            var edits = new List<A11yEdit>();
            for (var round = 0; round < 200; round++)
            {
                var previous = pool.OrderBy(_ => Next(1 << 20)).Take(Next(20)).ToArray();
                var next = pool.OrderBy(_ => Next(1 << 20)).Take(Next(20)).ToArray();
                A11yReconciler.Diff(previous, next, edits);
                Assert.That(Apply(previous, edits), Is.EqualTo(next), "ronda " + round);
                foreach (var kept in next.Intersect(previous))
                {
                    Assert.That(edits.Any(e => e.Kind == A11yEditKind.Insert && ReferenceEquals(e.Key, kept)), Is.False, "un elemento que sigue conserva su nodo");
                }
            }
        }

        private static object[] Apply(IReadOnlyList<object> previous, List<A11yEdit> edits)
        {
            var list = previous.ToList();
            foreach (var edit in edits)
            {
                switch (edit.Kind)
                {
                    case A11yEditKind.Remove:
                        Assert.That(list[edit.Index], Is.SameAs(edit.Key));
                        list.RemoveAt(edit.Index);
                        break;
                    case A11yEditKind.Insert:
                        list.Insert(edit.Index, edit.Key);
                        break;
                    case A11yEditKind.Move:
                        list.Remove(edit.Key);
                        list.Insert(edit.Index, edit.Key);
                        break;
                    default:
                        Assert.That(list[edit.Index], Is.SameAs(edit.Key));
                        break;
                }
            }

            return list.ToArray();
        }
    }
}
