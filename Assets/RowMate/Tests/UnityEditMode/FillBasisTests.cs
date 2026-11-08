using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // FBA · La base del relleno guardada por objeto (FillStreamer, cierre de WP-B de la It. 8): Compose(Of(...)) da la MISMA matriz,
    // bit a bit, que el Matrix4x4.TRS por cuadro de antes, para los giros enteros de juncos, rocas, bosque y matas, los de las matas de
    // pasto (Rank × 360) y escalas y posiciones de toda la orilla (así las tomas del lago no cambian). Con el control: una base de otro
    // giro no coincide.
    public sealed class FillBasisTests
    {
        private static int Mismatches(in Matrix4x4 expected, in Matrix4x4 actual)
        {
            var n = 0;
            for (var i = 0; i < 16; i++)
            {
                if (BitConverter.ToInt32(BitConverter.GetBytes(expected[i]), 0) != BitConverter.ToInt32(BitConverter.GetBytes(actual[i]), 0))
                {
                    n++;
                }
            }

            return n;
        }

        [Test, Category("AC-SCN-01-19")]
        public void ComposeIsBitIdenticalToTrs()
        {
            var rnd = new DartRandom(8);
            var checkedCount = 0;
            for (var a = 0; a < 720; a++)
            {
                // Los enteros de Variant % 360 y Variant * 71 % 360, y los de Rank × 360 (Rank en [0, 1)).
                var angle = a < 360 ? a : (float)(rnd.NextDouble() * 360);
                foreach (var scale in new[] { (1f, (float)(0.4 + rnd.NextDouble() * 1.2)), (1.6f, 0.7f), (3.2f * 0.83f, 0.83f), (2.5f, 2.5f), (0.11f, 0.11f) })
                {
                    var position = new Vector3((float)(rnd.NextDouble() * 120 - 60), (float)(rnd.NextDouble() * 3 - 0.5), (float)(rnd.NextDouble() * 425 - 25));
                    var expected = Matrix4x4.TRS(position, Quaternion.Euler(0, angle, 0), new Vector3(scale.Item1, scale.Item2, scale.Item1));
                    var actual = FillBasis.Compose(FillBasis.Of(angle, scale.Item1, scale.Item2), scale.Item2, position.x, position.y, position.z);
                    Assert.That(Mismatches(expected, actual), Is.EqualTo(0), "giro " + angle + ", escala " + scale + ", posición " + position + ":\n" + expected + "\n" + actual);
                    checkedCount++;
                }
            }

            Assert.That(checkedCount, Is.EqualTo(3600));
            // El control: otro giro da otra matriz.
            var other = FillBasis.Compose(FillBasis.Of(31, 1, 1), 1, 0, 0, 0);
            Assert.That(Mismatches(Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 30, 0), Vector3.one), other), Is.GreaterThan(0));
        }

        [Test, Category("AC-SCN-01-19")]
        public void TheItemBasisFollowsEachKind()
        {
            // Los mismos giros y escalas que FillStreamer.Add y AddGrass.
            var reed = new FillItem(FillKind.Reed, 1, 2, 0, 1.3, 400, 0.2, 0);
            Assert.That(FillBasis.Of(reed), Is.EqualTo(FillBasis.Of(40, 1, 1.3f)));
            var rock = new FillItem(FillKind.Rock, 1, 2, 0, 0.9, 7, 0.2, 0);
            Assert.That(FillBasis.Of(rock), Is.EqualTo(FillBasis.Of(7 * 71 % 360, 0.9f, 0.9f)));
            var fern = new FillItem(FillKind.Fern, 1, 2, 0, 0.5, 0, 0.25, 0);
            Assert.That(FillBasis.Of(fern), Is.EqualTo(FillBasis.Of(90, 1.6f, 0.5f)));
            var bush = new FillItem(FillKind.MeadowBush, 1, 2, 0, 3, 10, 0.2, 0);
            var s = (float)(3 / RowMate.Core.Scene.Art.LakeArt.PineHeight);
            Assert.That(FillBasis.Of(bush), Is.EqualTo(FillBasis.Of(10, s * 3.2f, s)));
            Assert.That(FillBasis.Of(new FillItem(FillKind.CityBlock, 1, 2, 0, 3, 10, 0.2, 0)), Is.EqualTo(Vector4.zero));
        }
    }
}
