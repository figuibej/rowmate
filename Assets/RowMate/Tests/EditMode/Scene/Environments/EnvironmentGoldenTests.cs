using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // EGO · Río, costa y regata contra el oráculo (scene_{river,coast,regatta}.json; AC-SCN-14-08, -09, -05 cambia): los segmentos
    // 0 a 2 000 por su FNV-1a y 0 a 99 enteros, bit a bit; las listas visibles a 8 distancias (a igual z, el orden no importa:
    // tramos de z iguales ordenados por (tipo, x, semilla) en los dos lados); los topes 60 y 200 con el escenario de prueba.
    [TestFixture]
    public sealed class EnvironmentGoldenTests
    {
        private static readonly double[] VisibleDistances = { 0, 1, 49.5, 50, 920, 1234, 5000, 42195 };

        private static IEnumerable<TestCaseData> Files()
        {
            yield return new TestCaseData(EnvironmentId.River, "scene_river.json").SetName("river");
            yield return new TestCaseData(EnvironmentId.Coast, "scene_coast.json").SetName("coast");
            yield return new TestCaseData(EnvironmentId.Regatta, "scene_regatta.json").SetName("regatta");
        }

        [TestCaseSource(nameof(Files)), Category("AC-SCN-14-08")]
        public void Segments0To2000MatchTheOracleDigest(EnvironmentId id, string file)
        {
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            var rows = GoldenVectors.CheckRows(file, "digests", row =>
            {
                var props = segments.PropsForSegment(row.Int("k"));
                if (props.Length != row.Int("count"))
                {
                    return "esperados " + row.Int("count") + " objetos, obtenidos " + props.Length;
                }

                var fnv = SceneGolden.Fnv1a64(props);
                return fnv == row.Text("fnv") ? null : "FNV esperado " + row.Text("fnv") + ", obtenido " + fnv;
            });
            Assert.That(rows, Is.EqualTo(2001));
        }

        [TestCaseSource(nameof(Files)), Category("AC-SCN-14-08")]
        public void Segments0To99MatchTheOracleField(EnvironmentId id, string file)
        {
            var expected = SceneGolden.PropsBySegment(file, "segments");
            Assert.That(expected.Count, Is.EqualTo(100));
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            for (var k = 0; k < 100; k++)
            {
                var diff = SceneGolden.Diff(expected[k], new List<ShoreProp>(segments.PropsForSegment(k).ToArray()));
                Assert.That(diff, Is.Null, "segmento " + k + ": " + diff);
            }
        }

        [TestCaseSource(nameof(Files)), Category("AC-SCN-14-08"), Category("AC-SCN-14-09")]
        public void VisibleListsMatchTheOracle(EnvironmentId id, string file)
        {
            var expected = SceneGolden.PropsByDistance(file, "visible");
            Assert.That(expected.Count, Is.EqualTo(VisibleDistances.Length));
            var env = EnvironmentCatalog.Create(id);
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            foreach (var d in VisibleDistances)
            {
                segments.VisibleProps(d, into);
                var actual = SceneGolden.CanonicalTies(into.Items.ToArray());
                var oracle = SceneGolden.CanonicalTies(expected[BitConverter.DoubleToInt64Bits(d)]);
                var diff = SceneGolden.Diff(oracle, actual);
                Assert.That(diff, Is.Null, "a " + SceneGolden.F(d) + " m: " + diff);
            }
        }

        [TestCaseSource(nameof(Files)), Category("AC-SCN-14-08")]
        public void ConstantsMatchTheLegacy(EnvironmentId id, string file)
        {
            var env = EnvironmentCatalog.Create(id);
            var row = GoldenVectors.Rows(file, "constants").First();
            Assert.That(env.MaxVisibleProps, Is.EqualTo(row.Int("maxVisibleProps")));
            Assert.That(env.WaveAmplitude, Is.EqualTo(row.F64("waveAmplitude")));
            Assert.That(env.WaveScale, Is.EqualTo(row.F64("waveScale")));
            Assert.That(env.Id, Is.EqualTo(id));
        }

        // AC-SCN-14-05 (cambia): la regla del tope con los topes reales de la costa (60) y la regata (200), contra visibleProps del
        // legacy sobre el mismo escenario de prueba, con boyas de sobra y con hitos de sobra.
        [TestCase("scene_coast.json", 60, Category = "AC-SCN-14-05,AC-SCN-14-09")]
        [TestCase("scene_regatta.json", 200, Category = "AC-SCN-14-05,AC-SCN-14-09")]
        public void TheRealCapsMatchTheLegacyFunction(string file, int cap)
        {
            var env = cap == 60 ? ToppedEnvironment.Coast60 : ToppedEnvironment.Regatta200;
            var segments = new ShoreSegments(env);
            var raw = SceneGolden.PropsBySegment(file, "capSegments");
            Assert.That(raw.Count, Is.EqualTo(42));
            foreach (var pair in raw)
            {
                var diff = SceneGolden.Diff(pair.Value, new List<ShoreProp>(segments.PropsForSegment(pair.Key).ToArray()));
                Assert.That(diff, Is.Null, "segmento de prueba " + pair.Key + ": " + diff);
            }

            var expected = SceneGolden.PropsByDistance(file, "capVisible");
            Assert.That(expected.Count, Is.EqualTo(6));
            var into = new ShorePropBuffer(cap);
            var sawBuoyTrim = false;
            var sawLandmarkTrim = false;
            foreach (var pair in expected)
            {
                var d = BitConverter.Int64BitsToDouble(pair.Key);
                segments.VisibleProps(d, into);
                Assert.That(into.Count, Is.EqualTo(cap), "supera el tope a " + SceneGolden.F(d) + " m");
                var diff = SceneGolden.Diff(SceneGolden.CanonicalTies(pair.Value), SceneGolden.CanonicalTies(into.Items.ToArray()));
                Assert.That(diff, Is.Null, "a " + SceneGolden.F(d) + " m: " + diff);
                var buoys = 0;
                foreach (var p in into.Items)
                {
                    buoys += p.Kind == PropKind.LaneBuoy ? 1 : 0;
                }

                sawBuoyTrim |= buoys > 0;
                sawLandmarkTrim |= buoys == 0;
            }

            Assert.That(sawBuoyTrim && sawLandmarkTrim, Is.True, "el escenario ejercita los dos caminos del tope");
        }

        [Test, Category("AC-SCN-14-09")]
        public void RealListsNeverExceedTheirCapAnywhere()
        {
            // Los escenarios reales no llegan a su tope (la regla queda para la densidad de prueba); lo mismo vale a cualquier distancia.
            foreach (var id in new[] { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var env = EnvironmentCatalog.Create(id);
                var segments = new ShoreSegments(env);
                var into = new ShorePropBuffer(env.MaxVisibleProps);
                for (var d = 0.0; d < 5000; d += 37.5)
                {
                    segments.VisibleProps(d, into);
                    Assert.That(into.Count, Is.LessThanOrEqualTo(env.MaxVisibleProps), id + " a " + d);
                }
            }
        }
    }
}
