using System;
using NUnit.Framework;
using RowMate.Core.Common;

namespace RowMate.Tests.EditMode.Common
{
    // CHS · La cadena de un carácter para los textos dibujados (O-4 de la lente de motion de la It. 5; 06a-escena-lago/00-deuda-5
    // §3): el texto es el del carácter; el mismo carácter da siempre la misma cadena (lo que se dibuja en cada cuadro no asigna);
    // Latin-1, la puntuación general y el signo menos están armados de entrada, y lo demás se arma una sola vez.
    [TestFixture]
    public sealed class CharStringsTests
    {
        private static readonly char[] Samples =
        {
            '0', 'a', 'Z', ' ', '/', '·', '×', 'é', 'ñ', '\u00FF', '\u2009', '–', '—', '…', '\u206F', CharStrings.Minus,
        };

        [Test]
        public void EachCharacterGivesItsOwnText()
        {
            foreach (var c in Samples)
            {
                Assert.That(CharStrings.Of(c), Is.EqualTo(c.ToString()), "U+" + ((int)c).ToString("X4"));
            }

            Assert.That(CharStrings.Of('\u0000'), Is.EqualTo("\u0000"));
        }

        [Test]
        public void TheSameCharacterAlwaysGivesTheSameString()
        {
            foreach (var c in Samples)
            {
                Assert.That(CharStrings.Of(c), Is.SameAs(CharStrings.Of(c)), "U+" + ((int)c).ToString("X4"));
            }
        }

        [Test]
        public void LatinPunctuationAndMinusArePrebuiltAndTheRestIsBuiltOnce()
        {
            Assert.That(CharStrings.Prebuilt, Is.EqualTo(369), "256 de Latin-1, 112 de la puntuación general y el signo menos");
            foreach (var c in Samples)
            {
                CharStrings.Of(c);
            }

            var before = CharStrings.OnDemand;
            Assert.That(before, Is.GreaterThanOrEqualTo(0));
            // Un carácter fuera de lo armado (un ideograma): se arma la primera vez y queda guardado.
            var rare = '\u4E2D';
            var first = CharStrings.Of(rare);
            var after = CharStrings.OnDemand;
            Assert.That(after, Is.LessThanOrEqualTo(before + 1), "a lo sumo uno más");
            Assert.That(CharStrings.Of(rare), Is.SameAs(first), "la segunda vez, la misma cadena");
            Assert.That(CharStrings.OnDemand, Is.EqualTo(after), "y no se arma otra");
        }

#if NET5_0_OR_GREATER
        // Solo en .NET 8: en Mono el contador del hilo da 0 (la lección de la 4b); en Unity lo cubren las medidas de 0 bytes por
        // cuadro con el registro de asignaciones (HBT).
        [Test]
        public void AskingForCharactersDoesNotAllocate()
        {
            // Calentamiento: JIT (con sus niveles), estáticos y los caracteres a pedido; después, la menor de tres medidas (lo que
            // asigne el cambio de nivel del JIT en el medio no es del código medido).
            for (var i = 0; i < 3; i++)
            {
                AskMany(out _);
                System.Threading.Thread.Sleep(20);
            }

            var best = long.MaxValue;
            for (var i = 0; i < 3; i++)
            {
                best = Math.Min(best, AskMany(out var length));
                Assert.That(length, Is.EqualTo(100 * (Samples.Length + 1)));
            }

            Assert.That(best, Is.EqualTo(0L));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long AskMany(out int length)
        {
            var samples = Samples;
            var before = GC.GetAllocatedBytesForCurrentThread();
            var total = 0;
            for (var i = 0; i < 100; i++)
            {
                for (var j = 0; j < samples.Length; j++)
                {
                    total += CharStrings.Of(samples[j]).Length;
                }

                total += CharStrings.Of('\u4E2D').Length;
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            length = total;
            return allocated;
        }
#endif
    }
}
