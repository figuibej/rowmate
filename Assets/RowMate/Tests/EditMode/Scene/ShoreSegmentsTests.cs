using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;

using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SHS · La orilla determinista (06a-escena-lago/02-tech-design §2.11; F-SCN-14; LT-75, LT-76, LT-77): los segmentos 0 a 2 000
    // del lago por su resumen FNV-1a y 0 a 99 enteros, las listas visibles a 8 distancias, el tope con el escenario de prueba,
    // el olvido y que no asigna después de calentar. Comparador: bit a bit (aritmética pura y el generador).
    [TestFixture]
    public sealed class ShoreSegmentsTests
    {
        private static readonly double[] VisibleDistances = { 0, 1, 49.5, 50, 920, 1234, 5000, 42195 };

        // legacy: test/scene/environment_test.dart:10 'EnvironmentId.lake: propsForSegment es determinista'
        [TestCase(TestName = "EnvironmentId.lake: propsForSegment es determinista", Category = "LT-75,AC-SCN-14-01")]
        public void LakePropsForSegmentIsDeterministic()
        {
            var a = new ShoreSegments(new LakeEnvironment());
            var b = new ShoreSegments(new LakeEnvironment());
            var first = a.PropsForSegment(7).ToArray();
            var again = a.PropsForSegment(7).ToArray();
            var other = b.PropsForSegment(7).ToArray();
            Assert.That(again, Is.EqualTo(first), "dos veces el segmento 7");
            Assert.That(other, Is.EqualTo(first), "otra sesión, el mismo segmento 7");
            Assert.That(a.PropsForSegment(8).ToArray(), Is.Not.EqualTo(first), "el 8 es distinto");
        }

        // legacy: test/scene/environment_test.dart:17 'EnvironmentId.lake: cada segmento tiene objetos y quedan dentro del segmento'
        [TestCase(TestName = "EnvironmentId.lake: cada segmento tiene objetos y quedan dentro del segmento", Category = "LT-76,AC-SCN-14-03")]
        public void LakeSegmentsHavePropsInsideTheirSegment()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            for (var k = 0; k < 30; k++)
            {
                var props = segments.PropsForSegment(k);
                Assert.That(props.Length, Is.GreaterThan(0), "segmento " + k + " vacío");
                foreach (var p in props)
                {
                    Assert.That(p.Z, Is.InRange(k * ShoreEnvironment.SegmentLength, (k + 1) * ShoreEnvironment.SegmentLength));
                    if (p.Kind != PropKind.Bridge && p.Kind != PropKind.LaneBuoy)
                    {
                        Assert.That(Math.Abs(p.X), Is.GreaterThanOrEqualTo(ShoreEnvironment.BankX), p.Kind + " en el agua");
                    }
                }
            }

            Assert.That(segments.PropsForSegment(-1).Length, Is.EqualTo(0), "no hay segmentos negativos");
        }

        // legacy: test/scene/environment_test.dart:30 'EnvironmentId.lake: visibleProps ordena de lejos a cerca y respeta el máximo'
        [TestCase(TestName = "EnvironmentId.lake: visibleProps ordena de lejos a cerca y respeta el máximo",
            Category = "LT-77,AC-SCN-14-04,AC-SCN-15-04")]
        public void LakeVisiblePropsSortedAndCapped()
        {
            var env = new LakeEnvironment();
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            segments.VisibleProps(1234, into);
            var props = into.Items;
            Assert.That(props.Length, Is.LessThanOrEqualTo(env.MaxVisibleProps));
            for (var i = 1; i < props.Length; i++)
            {
                Assert.That(props[i].Z, Is.LessThanOrEqualTo(props[i - 1].Z));
            }

            foreach (var p in props)
            {
                Assert.That(p.Z, Is.InRange(1235, 1234 + ShoreEnvironment.VisibleRange));
            }
        }

        [Test, Category("AC-SCN-14-02"), Category("AC-SCN-14-01")]
        public void Segments0To2000MatchTheOracleDigest()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            var rows = GoldenVectors.CheckRows("scene_lake.json", "digests", row =>
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

        [Test, Category("AC-SCN-14-02"), Category("AC-SCN-15-02"), Category("AC-SCN-15-03")]
        public void Segments0To99MatchTheOracleField()
        {
            var expected = SceneGolden.PropsBySegment("scene_lake.json", "segments");
            Assert.That(expected.Count, Is.EqualTo(100));
            var segments = new ShoreSegments(new LakeEnvironment());
            for (var k = 0; k < 100; k++)
            {
                var actual = new List<ShoreProp>(segments.PropsForSegment(k).ToArray());
                var diff = SceneGolden.Diff(expected[k], actual);
                Assert.That(diff, Is.Null, "segmento " + k + ": " + diff);
            }
        }

        [Test, Category("AC-SCN-14-04"), Category("AC-SCN-15-05")]
        public void VisibleListsMatchTheOracle()
        {
            var expected = SceneGolden.PropsByDistance("scene_lake.json", "visible");
            Assert.That(expected.Count, Is.EqualTo(VisibleDistances.Length));
            var env = new LakeEnvironment();
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

        [Test, Category("AC-SCN-14-04")]
        public void TheLakeHasNoTiesInZ()
        {
            // La regla de empates (QA-G6aT-15 a) vale igual; en el lago no hace falta: z distintas en 100 km.
            var segments = new ShoreSegments(new LakeEnvironment());
            var seen = new HashSet<long>();
            for (var k = 0; k <= 2000; k++)
            {
                foreach (var p in segments.PropsForSegment(k))
                {
                    Assert.That(seen.Add(BitConverter.DoubleToInt64Bits(p.Z)), Is.True, "z repetida en el segmento " + k + ": " + p);
                }
            }
        }

        [Test, Category("AC-SCN-14-05")]
        public void TheCapScenarioMatchesTheLegacyFunction()
        {
            var env = new CapEnvironment();
            var segments = new ShoreSegments(env);

            // La entrada: los segmentos del escenario son los del oráculo, bit a bit.
            var raw = SceneGolden.PropsBySegment("scene_lake.json", "capSegments");
            Assert.That(raw.Count, Is.EqualTo(42));
            foreach (var pair in raw)
            {
                var diff = SceneGolden.Diff(pair.Value, new List<ShoreProp>(segments.PropsForSegment(pair.Key).ToArray()));
                Assert.That(diff, Is.Null, "segmento de prueba " + pair.Key + ": " + diff);
            }

            // El corte no tiene empates de z (la prueba lo afirma antes de comparar).
            foreach (var pair in raw)
            {
                var zs = new HashSet<long>();
                foreach (var p in pair.Value)
                {
                    Assert.That(zs.Add(BitConverter.DoubleToInt64Bits(p.Z)), Is.True, "empate de z en el segmento de prueba " + pair.Key);
                }
            }

            // La salida: la función del legacy (visibleProps sobre el mismo escenario) a 6 distancias, con boyas de sobra, con hitos
            // de sobra y mezclado.
            var expected = SceneGolden.PropsByDistance("scene_lake.json", "capVisible");
            Assert.That(expected.Count, Is.EqualTo(6));
            var into = new ShorePropBuffer(CapEnvironment.Cap);
            var sawBuoyTrim = false;
            var sawLandmarkTrim = false;
            foreach (var pair in expected)
            {
                var d = BitConverter.Int64BitsToDouble(pair.Key);
                segments.VisibleProps(d, into);
                Assert.That(into.Count, Is.EqualTo(CapEnvironment.Cap), "el escenario supera el tope a " + SceneGolden.F(d) + " m");
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

        [Test, Category("AC-SCN-14-05")]
        public void TheCapKeepsLandmarksThenTheNearestBuoysThenDropsTheFarthestLandmarks()
        {
            // Las reglas del tope, una por una, sobre el escenario de prueba (además de la comparación con el oráculo).
            var env = new CapEnvironment();
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(CapEnvironment.Cap);
            segments.VisibleProps(0, into);
            var landmarks = 0;
            var nearestKeptBuoy = double.MaxValue;
            var farthestKeptBuoy = double.MinValue;
            foreach (var p in into.Items)
            {
                if (p.Kind == PropKind.LaneBuoy)
                {
                    nearestKeptBuoy = Math.Min(nearestKeptBuoy, p.Z);
                    farthestKeptBuoy = Math.Max(farthestKeptBuoy, p.Z);
                }
                else
                {
                    landmarks++;
                }
            }

            // Todos los hitos de [1; 400] siguen (con boyas de sobra hay 2 por segmento).
            var allLandmarks = 0;
            var maxBuoyZ = double.MinValue;
            for (var k = 0; k <= 8; k++)
            {
                foreach (var p in segments.PropsForSegment(k))
                {
                    if (p.Z >= 1 && p.Z <= 400)
                    {
                        allLandmarks += p.Kind != PropKind.LaneBuoy ? 1 : 0;
                        if (p.Kind == PropKind.LaneBuoy)
                        {
                            maxBuoyZ = Math.Max(maxBuoyZ, p.Z);
                        }
                    }
                }
            }

            Assert.That(landmarks, Is.EqualTo(allLandmarks), "se conservan todos los hitos");
            Assert.That(farthestKeptBuoy, Is.LessThan(maxBuoyZ), "las boyas que quedan son las más cercanas");

            // Con hitos de sobra (desde el segmento 20): ninguna boya y los hitos más cercanos.
            segments.VisibleProps(1200, into);
            foreach (var p in into.Items)
            {
                Assert.That(p.Kind, Is.Not.EqualTo(PropKind.LaneBuoy));
            }

            for (var i = 1; i < into.Count; i++)
            {
                Assert.That(into[i].Z, Is.LessThanOrEqualTo(into[i - 1].Z), "se reordena de lejos a cerca");
            }
        }

        [Test, Category("AC-SCN-14-06")]
        public void BehindIsForgottenAndComesBackIdentical()
        {
            var env = new LakeEnvironment();
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            var first = segments.PropsForSegment(0).ToArray();
            var max = 0;
            for (var d = 0.0; d <= 42195; d += 25)
            {
                segments.VisibleProps(d, into);
                max = Math.Max(max, segments.CachedSegments);
            }

            Assert.That(max, Is.LessThanOrEqualTo(11), "la memoria del paisaje no crece con la distancia (9 a la vista y 1 atrás)");
            Assert.That(segments.CachedSegments, Is.LessThanOrEqualTo(11));
            Assert.That(segments.PropsForSegment(0).ToArray(), Is.EqualTo(first), "lo olvidado vuelve idéntico");
        }

        [Test, Category("AC-SCN-01-12"), Category("AC-SCN-14-06")]
        public void VisiblePropsDoesNotAllocateAfterWarmup()
        {
#if NET5_0_OR_GREATER
            var env = new LakeEnvironment();
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            Walk(segments, into, 0, 2000, 7.3); // calentamiento: JIT y estáticos fuera de la medición
            var best = long.MaxValue;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                best = Math.Min(best, Walk(segments, into, 2000 + attempt * 10000, 12000 + attempt * 10000, 0.71));
            }

            Assert.That(best, Is.EqualTo(0L), "VisibleProps en 10 km");
#else
            Assert.Ignore("solo dotnet: en Mono el contador del hilo da 0 (la medida de Unity es la de cuadros reales, SBT)");
#endif
        }

#if NET5_0_OR_GREATER
        // Optimizado de entrada (sin tiering ni OSR): la medición ve solo lo que reserva ShoreSegments.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long Walk(ShoreSegments segments, ShorePropBuffer into, double from, double to, double step)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var d = from; d < to; d += step)
            {
                segments.VisibleProps(d, into);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif
    }
}
