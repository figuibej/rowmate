using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // DRN · El generador del legacy (06a-escena-lago/02-tech-design §2.10; AC-SCN-14-02; Q-61, ADR 012): los cuatro vectores
    // dorados de la spec y, del oráculo (scene_random.json), 64 tiradas de NextDouble, NextInt(1000), NextInt(2²⁶), NextInt(7) y
    // NextBool para nueve semillas. Comparador: bit a bit (aritmética entera y una división exacta).
    [TestFixture]
    public sealed class DartRandomTests
    {
        [Test, Category("AC-SCN-14-02")]
        public void SpecGoldenVectors()
        {
            var r = new DartRandom(42);
            Assert.That(Bits(r.NextDouble()), Is.EqualTo(Bits(0.15092545597797424)));
            Assert.That(Bits(r.NextDouble()), Is.EqualTo(Bits(0.6041479725361217)));
            Assert.That(r.NextInt(1000), Is.EqualTo(77));
            // La semilla del segmento 7 del lago: 0·1 000 003 + 7·7 919 + 17 = 55 450.
            Assert.That(ShoreEnvironment.SeedFor(EnvironmentId.Lake, 7), Is.EqualTo(55450L));
            Assert.That(Bits(new DartRandom(55450).NextDouble()), Is.EqualTo(Bits(0.854835531354132)));
        }

        [Test, Category("AC-SCN-14-02")]
        public void OracleSequencesMatchBitForBit()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var rows = GoldenVectors.CheckRows("scene_random.json", "sequences", row =>
            {
                var seed = row.Int("seed");
                var op = row.Text("op");
                seen.Add(op);
                var expected = row.Text("values").Split(' ');
                var r = new DartRandom(seed);
                for (var i = 0; i < expected.Length; i++)
                {
                    string got;
                    switch (op)
                    {
                        case "double":
                            got = GoldenJsonExtensions.ToF64Bits(r.NextDouble());
                            break;
                        case "int1000":
                            got = r.NextInt(1000).ToString(CultureInfo.InvariantCulture);
                            break;
                        case "int2p26":
                            got = r.NextInt(1 << 26).ToString(CultureInfo.InvariantCulture);
                            break;
                        case "int7":
                            got = r.NextInt(7).ToString(CultureInfo.InvariantCulture);
                            break;
                        case "bool":
                            got = r.NextBool() ? "1" : "0";
                            break;
                        default:
                            return "operación desconocida " + op;
                    }

                    if (got != expected[i])
                    {
                        return "tirada " + i + ": esperado " + expected[i] + ", obtenido " + got;
                    }
                }

                return expected.Length == 64 ? null : "se esperaban 64 tiradas";
            });
            Assert.That(rows, Is.EqualTo(9 * 5), "9 semillas × 5 operaciones");
            Assert.That(seen, Is.EquivalentTo(new[] { "double", "int1000", "int2p26", "int7", "bool" }));
        }

        [Test, Category("AC-SCN-14-02")]
        public void ReseedRestartsTheSequenceWithoutANewInstance()
        {
            var r = new DartRandom(1);
            var first = r.NextDouble();
            r.NextInt(1000);
            r.Reseed(1);
            Assert.That(Bits(r.NextDouble()), Is.EqualTo(Bits(first)));
            r.Reseed(42);
            Assert.That(Bits(r.NextDouble()), Is.EqualTo(Bits(0.15092545597797424)));
        }

        [Test, Category("AC-SCN-14-02")]
        public void NextIntRejectsWhatDartRejects()
        {
            var r = new DartRandom(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(-5));
            Assert.That(r.NextInt(1), Is.EqualTo(0));
            Assert.That(r.NextInt(int.MaxValue), Is.InRange(0, int.MaxValue - 1));
        }

        [Test, Category("AC-SCN-14-02")]
        public void DoublesAreInTheUnitInterval()
        {
            var r = new DartRandom(7919);
            for (var i = 0; i < 10000; i++)
            {
                var d = r.NextDouble();
                Assert.That(d >= 0 && d < 1, Is.True, d.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        private static long Bits(double d)
        {
            return BitConverter.DoubleToInt64Bits(d);
        }
    }
}
