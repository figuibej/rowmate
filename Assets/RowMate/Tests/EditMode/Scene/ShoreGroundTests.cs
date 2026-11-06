using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene
{
    // TRN · El terreno (06a-escena-lago/02-tech-design §2.13; 04-design-notes §7.1; AC-SCN-15-07): continuo, la orilla solo hacia
    // afuera, el corte con sus tramos y las envolventes.
    [TestFixture]
    public sealed class TerrainProfileTests
    {
        private const EnvironmentId Lake = EnvironmentId.Lake;

        [Test, Category("AC-SCN-15-07")]
        public void TheShoreMeandersOnlyOutward()
        {
            for (var z = -100.0; z < 5000; z += 0.37)
            {
                foreach (var side in new[] { -1, 1 })
                {
                    var shore = TerrainProfile.ShoreX(side, z);
                    Assert.That(shore, Is.InRange(9.0, 9.5), "la línea de costa en |x| = 9, nunca adentro del canal");
                    Assert.That(TerrainProfile.Height(Lake, side * shore, z), Is.EqualTo(0).Within(1e-12), "el agua toca la orilla");
                }
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void TheCrossSection()
        {
            const double Z = 1234;
            var shore = TerrainProfile.ShoreX(1, Z);
            Assert.That(TerrainProfile.Height(Lake, shore - 2, Z), Is.EqualTo(LakeArt.BottomDepth).Within(1e-12), "el fondo bajo el agua");
            Assert.That(TerrainProfile.Height(Lake, shore - 3, Z), Is.EqualTo(LakeArt.BottomDepth));
            Assert.That(TerrainProfile.Height(Lake, shore + 1, Z), Is.EqualTo(LakeArt.BeachTop).Within(1e-12), "la playa hasta 0,27");
            Assert.That(TerrainProfile.Height(Lake, shore + 7, Z), Is.EqualTo(LakeArt.SlopeTop).Within(1e-12), "el talud hasta 2,5");
            Assert.That(TerrainProfile.Height(Lake, -(TerrainProfile.ShoreX(-1, Z) + 7 + 24), Z), Is.EqualTo(2.5 + 0.12 * 24).Within(1e-9), "la ladera");
            Assert.That(TerrainProfile.Height(Lake, shore + 7 + 84, Z), Is.EqualTo(2.5 + 0.02 * 84).Within(1e-9), "la pradera");
            for (var d = 1.0; d <= 7; d += 0.01)
            {
                var h = TerrainProfile.Height(Lake, shore + d, Z);
                Assert.That(h, Is.InRange(LakeArt.BeachTop - 1e-12, LakeArt.SlopeTop + 1e-12), "el talud con su ruido, siempre entre 0,27 y 2,5");
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void ContinuousWithoutSeamsBetweenSegments()
        {
            for (var k = 1; k < 200; k++)
            {
                var z = k * ShoreEnvironment.SegmentLength;
                for (var x = -60.0; x <= 60; x += 0.25)
                {
                    var below = TerrainProfile.Height(Lake, x, z - 1e-6);
                    var above = TerrainProfile.Height(Lake, x, z + 1e-6);
                    Assert.That(Math.Abs(above - below), Is.LessThan(0.001), "costura < 1 mm en z " + z + ", x " + x);
                }
            }

            for (var x = -60.0; x < 60; x += 0.01)
            {
                var a = TerrainProfile.Height(Lake, x, 777);
                var b = TerrainProfile.Height(Lake, x + 0.01, 777);
                Assert.That(Math.Abs(b - a), Is.LessThan(0.05), "continuo en x");
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void Envelopes()
        {
            Assert.That(TerrainProfile.Envelope(-18), Is.EqualTo(2.2 + 0.20 * 18).Within(1e-12));
            Assert.That(TerrainProfile.Envelope(-100), Is.EqualTo(22.2).Within(1e-12));
            Assert.That(double.IsNaN(TerrainProfile.Envelope(-17.9)), Is.True, "más adentro, solo lo bajo");
            Assert.That(TerrainProfile.Envelope(30), Is.EqualTo(2.2 + 0.06 * 30).Within(1e-12));
            Assert.That(double.IsNaN(TerrainProfile.Envelope(29)), Is.True);
            Assert.That(LakeArt.LeftEnvelopeSlope, Is.EqualTo(0.20), "DN6a-14: 0,20, no 0,25");
            Assert.That(TerrainProfile.CanopyHeight(-60, 100), Is.InRange(TerrainProfile.Envelope(-60) - 3, TerrainProfile.Envelope(-60) - 1));
            Assert.That(double.IsNaN(TerrainProfile.CanopyHeight(60, 100)), Is.True);
        }

        [Test, Category("AC-SCN-15-07")]
        public void OnlyTheLakeExistsInSixA()
        {
            Assert.Throws<NotSupportedException>(() => TerrainProfile.Height(EnvironmentId.River, 10, 0));
        }
    }

    // PPL · La colocación (DT6a-08; AC-SCN-15-05, -07): nada flota, el ancla de la cabaña igual al legacy y ningún tronco adentro.
    [TestFixture]
    public sealed class PropPlacementTests
    {
        private const EnvironmentId Lake = EnvironmentId.Lake;

        [Test, Category("AC-SCN-15-05"), Category("AC-SCN-15-07")]
        public void NothingFloatsInOneHundredKilometers()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            for (var k = 0; k <= 2000; k++)
            {
                foreach (var p in segments.PropsForSegment(k))
                {
                    if (p.Kind != PropKind.Pine)
                    {
                        continue;
                    }

                    var foot = PropPlacement.PineFootY(Lake, p);
                    var ground = TerrainProfile.Height(Lake, p.X, p.Z);
                    Assert.That(ground, Is.GreaterThan(0), "el pino no está en el agua");
                    Assert.That(foot, Is.LessThanOrEqualTo(ground - LakeArt.PineSink + 1e-12), "el tronco entra en el suelo: no flota");
                    Assert.That(foot + LakeArt.PineSink, Is.LessThanOrEqualTo(2.5), "a lo sumo 2,5 m sobre el agua");
                }
            }
        }

        [Test, Category("AC-SCN-15-05"), Category("AC-SCN-15-07")]
        public void TheCabinAnchorNeverMovesAndTrunksInsideArePruned()
        {
            // DT6a-08: el ancla no se mueve nunca; la profundidad es 4, 3 o 2,5 m hacia +z, la primera sin troncos en la huella
            // + 0,3 m; si choca igual, el pino se poda. Medido en 100 km: con el ancla fija y los pinos del legacy en su lugar, en
            // un tercio de las cabañas queda un tronco adentro aun con 2,5 m (el desvío está informado en "Cambios por
            // implementación"): la prueba fija la regla del gate y que todo tronco adentro sea de un pino podado.
            var segments = new ShoreSegments(new LakeEnvironment());
            var depths = new Dictionary<double, int>();
            var pruned = 0;
            var collided = 0;
            var cabins = 0;
            for (var k = 7; k <= 2000; k += 12)
            {
                var props = segments.PropsForSegment(k).ToArray();
                var cabin = Array.Find(props, p => p.Kind == PropKind.Cabin);
                var layout = PropPlacement.Cabin(Lake, cabin, props);
                cabins++;
                Assert.That(layout.AnchorX, Is.EqualTo(13.0), "el ancla del legacy");
                Assert.That(layout.AnchorZ, Is.EqualTo(k * 50.0 + 10), "el centro del frente");
                Assert.That(layout.MinZ, Is.EqualTo(layout.AnchorZ), "la profundidad crece hacia +z");
                depths[layout.Depth] = depths.TryGetValue(layout.Depth, out var n) ? n + 1 : 1;
                collided += layout.Collided ? 1 : 0;
                foreach (var p in props)
                {
                    if (PropPlacement.TrunkInside(layout, p))
                    {
                        Assert.That(layout.Collided, Is.True, "solo si ninguna profundidad se libró");
                        Assert.That(layout.Depth, Is.EqualTo(2.5));
                        Assert.That(PropPlacement.IsPrunedBy(layout, p), Is.True, "ese pino se poda");
                    }

                    pruned += PropPlacement.IsPrunedBy(layout, p) ? 1 : 0;
                }

                Assert.That(layout.FloorY, Is.GreaterThan(layout.PlinthBottomY), "el piso sobre el zócalo");
                Assert.That(layout.FloorY, Is.LessThan(3.0));
            }

            TestContext.Out.WriteLine("cabañas: " + cabins + "; profundidades: " + string.Join(", ", Describe(depths)) + "; con tronco adentro: "
                + collided + "; pinos podados: " + pruned);
            Assert.That(depths.Keys, Is.SubsetOf(LakeArt.CabinDepths));
            Assert.That(depths.ContainsKey(4.0), Is.True);
        }

        [Test, Category("AC-SCN-15-05")]
        public void ThePierAnchorIsTheLegacyOne()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            for (var k = 3; k <= 2000; k += 8)
            {
                var pier = Array.Find(segments.PropsForSegment(k).ToArray(), p => p.Kind == PropKind.Pier);
                var layout = PropPlacement.Pier(Lake, pier);
                Assert.That(layout.AnchorX, Is.EqualTo(-9.0));
                Assert.That(layout.AnchorZ, Is.EqualTo(k * 50.0 + 25));
                Assert.That(layout.DeckY, Is.EqualTo(0.40));
                Assert.That(layout.DeckToX, Is.EqualTo(-6.0), "la punta del muelle, 3 m hacia el centro del lago");
                Assert.That(layout.DeckFromX, Is.InRange(-10.7, -10.0), "hasta donde la playa llega a 0,30 m (≈ −10,1)");
                Assert.That(TerrainProfile.Height(Lake, layout.DeckFromX, layout.AnchorZ), Is.EqualTo(0.30).Within(1e-6));
                for (var i = 0; i < 3; i++)
                {
                    Assert.That(layout.PileBottom(i), Is.LessThan(0), "los pilotes hasta el fondo");
                }
            }
        }

        private static IEnumerable<string> Describe(Dictionary<double, int> d)
        {
            foreach (var pair in d)
            {
                yield return pair.Key + " m: " + pair.Value;
            }
        }
    }

    // SFL · Lo que se suma al legacy (DD6a-11; AC-SCN-15-07; QA-G6aT-15 b): determinista por distancia, sin tocar el generador del
    // legacy, cada segmento solo, lo bajo en |x| < 18 y nada fuera de la envolvente.
    [TestFixture]
    public sealed class ShoreFillTests
    {
        private const EnvironmentId Lake = EnvironmentId.Lake;

        [Test, Category("AC-SCN-15-07")]
        public void TheLegacyIsTheSameWithAndWithoutFill()
        {
            var plain = new ShoreSegments(new LakeEnvironment());
            var filled = new ShoreSegments(new LakeEnvironment());
            var rnd = new DartRandom(0);
            var buffer = new FillBuffer(ShoreFill.SegmentCapacity);
            for (var k = 0; k < 200; k++)
            {
                var props = filled.PropsForSegment(k);
                buffer.Clear();
                ShoreFill.Segment(Lake, k, props, rnd, buffer);
                Assert.That(filled.PropsForSegment(k).ToArray(), Is.EqualTo(plain.PropsForSegment(k).ToArray()), "segmento " + k);
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void RequestOrderDoesNotMatter()
        {
            // Pedir k + 1 antes que k, y los segmentos 0 a 200 en orden aleatorio con semilla fija, da el mismo relleno, bit a bit.
            var inOrder = new List<FillItem[]>();
            for (var k = 0; k <= 200; k++)
            {
                inOrder.Add(Fill(k));
            }

            var order = new List<int>();
            for (var k = 0; k <= 200; k++)
            {
                order.Add(k);
            }

            var shuffle = new DartRandom(2026);
            for (var i = order.Count - 1; i > 0; i--)
            {
                var j = shuffle.NextInt(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            foreach (var k in order)
            {
                Assert.That(Fill(k), Is.EqualTo(inOrder[k]), "segmento " + k);
            }

            Assert.That(Fill(8), Is.EqualTo(inOrder[8]));
            Assert.That(Fill(7), Is.EqualTo(inOrder[7]));
        }

        [Test, Category("AC-SCN-15-07")]
        public void LowOnlyInsideTheLegacyStripAndNothingAboveTheEnvelope()
        {
            for (var k = 0; k <= 400; k++)
            {
                foreach (var item in Fill(k))
                {
                    CheckEnvelope(item, "segmento " + k);
                }
            }

            var buffer = new FillBuffer(60000);
            ShoreFill.Grass(Lake, 900, 960, buffer);
            ShoreFill.Ferns(Lake, 900, 960, buffer);
            Assert.That(buffer.Count, Is.GreaterThan(1000));
            foreach (var item in buffer.Items)
            {
                CheckEnvelope(item, "pasto");
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void TheReedsOfTheGate()
        {
            var counts = 0;
            for (var k = 0; k <= 400; k++)
            {
                var props = new ShoreSegments(new LakeEnvironment()).PropsForSegment(k).ToArray();
                foreach (var item in Fill(k))
                {
                    if (item.Kind != FillKind.Reed)
                    {
                        continue;
                    }

                    counts++;
                    Assert.That(Math.Abs(item.X), Is.InRange(LakeArt.ReedInnerX, LakeArt.ReedOuterX));
                    Assert.That(item.BaseY, Is.LessThanOrEqualTo(LakeArt.ReedRootMaxY), "la raíz en agua somera o playa húmeda");
                    Assert.That(item.Height, Is.InRange(0.6, 1.2), "DT6a-09");
                    foreach (var p in props)
                    {
                        if (p.Kind == PropKind.Pier && item.X < 0)
                        {
                            Assert.That(Math.Abs(item.Z - p.Z), Is.GreaterThanOrEqualTo(3.0), "nunca a menos de 3 m de un muelle");
                        }

                        if (p.Kind == PropKind.Cabin && item.X > 0)
                        {
                            Assert.That(item.Z < p.Z - 4 || item.Z > p.Z + 4, Is.True, "ni frente a una cabaña");
                        }
                    }
                }
            }

            Assert.That(counts / 401.0, Is.InRange(60.0, 130.0), "≈ 1 cada 0,8 m por orilla, con huecos");
        }

        [Test, Category("AC-SCN-15-07")]
        public void TheNearForestIsAPoissonDiskPerSegment()
        {
            for (var k = 0; k <= 200; k++)
            {
                var trees = new List<FillItem>();
                foreach (var item in Fill(k))
                {
                    if (item.Kind == FillKind.ForestPine)
                    {
                        trees.Add(item);
                        Assert.That(-item.X, Is.InRange(18.0, 45.0));
                        Assert.That(item.Z - k * 50.0, Is.InRange(3.0, 47.0), "un margen interno de la mitad del radio");
                        Assert.That(item.Height, Is.LessThanOrEqualTo(6 + 0.12 * (-item.X - 18) + 1e-9));
                    }
                }

                Assert.That(trees.Count, Is.GreaterThan(5), "un bosque de verdad");
                for (var i = 0; i < trees.Count; i++)
                {
                    for (var j = i + 1; j < trees.Count; j++)
                    {
                        var d = Math.Sqrt(Math.Pow(trees[i].X - trees[j].X, 2) + Math.Pow(trees[i].Z - trees[j].Z, 2));
                        Assert.That(d, Is.GreaterThanOrEqualTo(6.0), "Poisson de 6 m");
                    }
                }
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void RocksSinkAndMeadowGroupsAreSpaced()
        {
            var groups = new List<double>();
            for (var k = 0; k <= 400; k++)
            {
                var items = Fill(k);
                var firstMeadow = Array.FindIndex(items, i => i.Kind == FillKind.MeadowBush || i.Kind == FillKind.MeadowConifer);
                if (firstMeadow >= 0)
                {
                    groups.Add(items[firstMeadow].Z);
                }

                foreach (var item in items)
                {
                    if (item.Kind == FillKind.Rock)
                    {
                        Assert.That(item.Height, Is.InRange(0.2, 0.7));
                        Assert.That(item.BaseY, Is.LessThan(TerrainProfile.Height(Lake, item.X, item.Z)), "hundida el 30 % de su alto");
                    }

                    if (item.Kind == FillKind.MeadowBush || item.Kind == FillKind.MeadowConifer)
                    {
                        Assert.That(item.X, Is.InRange(30.0, 120.0));
                    }
                }
            }

            Assert.That(groups.Count, Is.InRange(120, 200), "un grupo cada 90 a 150 m");
        }

        [Test, Category("AC-SCN-15-07")]
        public void GrassIsDeterministicAndThinsWithDistance()
        {
            var a = new FillBuffer(30000);
            var b = new FillBuffer(30000);
            ShoreFill.Grass(Lake, 100, 130, a);
            ShoreFill.Grass(Lake, 115, 130, b);
            ShoreFill.Grass(Lake, 100, 115, b);
            Assert.That(b.Count, Is.EqualTo(a.Count), "la franja partida en dos da lo mismo");
            var set = new HashSet<FillItem>(a.Items.ToArray());
            foreach (var item in b.Items)
            {
                Assert.That(set.Contains(item), Is.True);
            }

            Assert.That(ShoreFill.KeepGrass(0.99, 0, 40), Is.True, "4/m² junto a la cámara");
            Assert.That(ShoreFill.KeepGrass(0.5, 20, 40), Is.False);
            Assert.That(ShoreFill.KeepGrass(0.49, 20, 40), Is.True, "la mitad a la mitad del fin");
            Assert.That(ShoreFill.KeepGrass(0.0, 40, 40), Is.False, "0 en el fin");
        }

        private static FillItem[] Fill(long k)
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            var buffer = new FillBuffer(ShoreFill.SegmentCapacity);
            ShoreFill.Segment(Lake, k, segments.PropsForSegment(k), new DartRandom(0), buffer);
            return buffer.Items.ToArray();
        }

        private static void CheckEnvelope(in FillItem item, string where)
        {
            var env = TerrainProfile.Envelope(item.X);
            if (Math.Abs(item.X) < LakeArt.LegacyStripHalfWidth || double.IsNaN(env))
            {
                Assert.That(item.Height, Is.LessThanOrEqualTo(LakeArt.LowVegetationMax), where + ": en |x| < 18 solo lo bajo (" + item.Kind + ")");
            }
            else
            {
                Assert.That(item.TopY, Is.LessThanOrEqualTo(env + 1e-9), where + ": " + item.Kind + " pasa la envolvente");
            }
        }
    }
}
