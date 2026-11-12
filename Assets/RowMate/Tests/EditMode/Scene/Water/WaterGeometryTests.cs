using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WRM · La malla del agua avanzada (PA6b-02; 02-tech-design §3.4, §6; AC-6B-29): ≤ 20 k triángulos, el interior de 40 × 40 m con paso
    // de 0,5 m (1 m en Bajo), los anillos que duplican el paso y el borde a 1,5 km; determinista.
    [TestFixture]
    public sealed class WaterRingMeshTests
    {
        [TestCase(QualityTier.High, TestName = "AC-6B-29 · la malla del agua en Alto")]
        [TestCase(QualityTier.Medium, TestName = "AC-6B-29 · la malla del agua en Medio")]
        [TestCase(QualityTier.Low, TestName = "AC-6B-29 · la malla del agua en Bajo")]
        [Category("AC-6B-29")]
        public void UnderTwentyThousandTrianglesToTheEdge(QualityTier tier)
        {
            var mesh = WaterRingMesh.Create(tier);
            Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(WaterRingMesh.MaxTriangles));
            WaterRingMesh.Counts(tier, out var v, out var i);
            Assert.That(mesh.VertexCount, Is.EqualTo(v));
            Assert.That(mesh.IndexCount, Is.EqualTo(i));
            mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            Assert.That(maxZ, Is.EqualTo(WaterRingMesh.EdgeHalf).Within(1e-3), "el borde a 1,5 km");
            Assert.That(minX, Is.EqualTo(-WaterRingMesh.EdgeHalf).Within(1e-3));
            Assert.That(minY, Is.EqualTo(0));
            Assert.That(maxY, Is.EqualTo(0), "plana: B0 la mueve en el vértice");

            // El paso del interior (color de vértice g = paso/256) y el de cada anillo, el doble del anterior.
            var inner = WaterRingMesh.InnerStep(tier);
            Assert.That(inner, Is.EqualTo(tier == QualityTier.Low ? 1.0 : 0.5));
            Assert.That(mesh.Colors[1] * 256, Is.EqualTo(inner).Within(1e-4));
            Assert.That(WaterRingMesh.Rings(tier), Is.GreaterThan(4));

            // Determinista: dos armados dan los mismos bytes.
            var again = WaterRingMesh.Create(tier);
            Assert.That(again.Positions, Is.EqualTo(mesh.Positions));
            Assert.That(again.Indices, Is.EqualTo(mesh.Indices));
        }

        [Test, Category("AC-6B-29")]
        public void TheInnerSquareIsFineAndFacesUp()
        {
            var mesh = WaterRingMesh.Create(QualityTier.Medium);
            // El interior: (80 + 1)² vértices a 0,5 m.
            var innerVertices = 81 * 81;
            for (var k = 0; k < innerVertices; k += 97)
            {
                Assert.That(Math.Abs(mesh.X(k)), Is.LessThanOrEqualTo(WaterRingMesh.InnerHalf + 1e-6));
                Assert.That(mesh.X(k) / 0.5, Is.EqualTo(Math.Floor(mesh.X(k) / 0.5 + 0.5)).Within(1e-4), "en la grilla de 0,5 m");
            }

            for (var t = 0; t < mesh.TriangleCount; t += 101)
            {
                int a = mesh.Indices[3 * t], b = mesh.Indices[3 * t + 1], c = mesh.Indices[3 * t + 2];
                var ux = mesh.X(b) - mesh.X(a);
                var uz = mesh.Z(b) - mesh.Z(a);
                var vx = mesh.X(c) - mesh.X(a);
                var vz = mesh.Z(c) - mesh.Z(a);
                // Normal de la cara con la convención de Unity (horario visto de frente): (b − a) × (c − a) apunta a +y.
                var ny = uz * vx - ux * vz;
                Assert.That(ny, Is.GreaterThan(0), "triángulo " + t);
            }
        }
    }

    // WNM · El mapa de normales procedural (PA6b-04; DT6b-17, DT6b-18; AC-6B-37): determinista, sin costura y en las dos bandas.
    [TestFixture]
    public sealed class WaterNormalMapTests
    {
        [Test, Category("AC-6B-37")]
        public void SameBytesTwiceAndSeamless()
        {
            const int size = 128;
            var a = new byte[WaterNormalMap.ByteCount(size)];
            var b = new byte[WaterNormalMap.ByteCount(size)];
            WaterNormalMap.Fill(size, WaterNormalMap.DefaultSeed, a);
            WaterNormalMap.Fill(size, WaterNormalMap.DefaultSeed, b);
            Assert.That(b, Is.EqualTo(a), "la misma semilla, los mismos bytes");
            var c = new byte[WaterNormalMap.ByteCount(size)];
            WaterNormalMap.Fill(size, WaterNormalMap.DefaultSeed + 1, c);
            Assert.That(c, Is.Not.EqualTo(a), "otra semilla, otro mapa");

            // Sin costura: el salto entre el último píxel y el primero (de la fila y de la columna) es como el de dos píxeles vecinos.
            for (var ch = 0; ch < 4; ch++)
            {
                double inside = 0, across = 0;
                for (var y = 0; y < size; y++)
                {
                    inside += Math.Abs(a[(y * size + 1) * 4 + ch] - a[(y * size) * 4 + ch]);
                    across += Math.Abs(a[(y * size) * 4 + ch] - a[(y * size + size - 1) * 4 + ch]);
                }

                Assert.That(across, Is.LessThan(inside * 1.6 + size), "canal " + ch + " sin costura");
            }
        }

        [Test, Category("AC-6B-37")]
        public void BothBandsUseTheRangeAroundTheFlatNormal()
        {
            const int size = 256;
            var map = new byte[WaterNormalMap.ByteCount(size)];
            WaterNormalMap.Fill(size, WaterNormalMap.DefaultSeed, map);
            for (var ch = 0; ch < 4; ch++)
            {
                long sum = 0;
                int min = 255, max = 0;
                for (var i = 0; i < size * size; i++)
                {
                    int v = map[i * 4 + ch];
                    sum += v;
                    min = Math.Min(min, v);
                    max = Math.Max(max, v);
                }

                var mean = (double)sum / (size * size);
                Assert.That(mean, Is.EqualTo(128).Within(3), "canal " + ch + ": centrado en la normal plana (pendiente media 0)");
                Assert.That(max - min, Is.GreaterThan(60), "canal " + ch + ": con relieve");
                Assert.That(min, Is.GreaterThan(0));
                Assert.That(max, Is.LessThan(255), "sin recortar");
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => WaterNormalMap.Fill(100, 1, new byte[100 * 100 * 4]));
            Assert.Throws<ArgumentException>(() => WaterNormalMap.Fill(64, 1, new byte[10]));
        }
    }

    // HPF · El perfil del horizonte de 256 × 1 para el reflejo de Medio (PA6b-05; AC-6B-12): la silueta lejana por acimut, que se acerca
    // con la distancia, y 0 donde no hay silueta.
    [TestFixture]
    public sealed class HorizonProfileTests
    {
        [Test, Category("AC-6B-12")]
        public void TheSilhouetteByAzimuth()
        {
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var near = new byte[HorizonProfile.Width];
                var far = new byte[HorizonProfile.Width];
                var profile = new HorizonProfile(id);
                profile.Fill(0, near);
                profile.Fill(5000, far);
                var any = 0;
                for (var c = 0; c < HorizonProfile.Width; c++)
                {
                    any += near[c] > 0 ? 1 : 0;
                    Assert.That(far[c], Is.GreaterThanOrEqualTo(near[c]), id + ": la silueta se acerca con la distancia (k(d) ≥ 1)");
                    Assert.That(HorizonProfile.Decode(near[c]), Is.EqualTo(Math.Min(profile.Value(c, 0), HorizonProfile.Encoding)).Within(1.0 / 255 + 1e-9));
                }

                Assert.That(any, Is.GreaterThan(HorizonProfile.Width / 4), id + ": hay silueta");
                var again = new byte[HorizonProfile.Width];
                HorizonProfile.Fill(id, 0, again);
                Assert.That(again, Is.EqualTo(near), "la firma estática da lo mismo");
            }

            // La costa: el acantilado lejano de un lado y el mar abierto del otro (0: ahí se mide la costura).
            var coast = new byte[HorizonProfile.Width];
            HorizonProfile.Fill(EnvironmentId.Coast, 0, coast);
            Assert.That(coast[HorizonProfile.Width - 1], Is.EqualTo(0), "a la derecha, mar abierto");
            Assert.That(HorizonProfile.AzimuthDeg(0), Is.EqualTo(-65 + 130.0 / 512).Within(1e-12));
            Assert.That(HorizonProfile.Encode(5), Is.EqualTo(255));
            Assert.That(HorizonProfile.Encode(0), Is.EqualTo(0));
        }
    }

    // HUL · La popa del casco (#85; AC-6B-21; 00-arte-agua §9): el degradé de la cubierta y el afinado de los últimos 0,6 m; el contrato
    // del casco (8,2 m, manga, el punto del remero, la borda y el cockpit) sin cambios.
    [TestFixture]
    public sealed class HullMeshTests
    {
        [Test, Category("AC-6B-21")]
        public void SternTaperAndGradient()
        {
            Assert.That(HullMesh.DeckColor(LakeArt.HullStern), Is.EqualTo(SceneColor.FromArgb(0xFFC9D1D6)), "#C9D1D6 en la punta");
            Assert.That(HullMesh.DeckColor(LakeArt.HullStern + 2.5), Is.EqualTo(SceneColor.FromArgb(0xFFF5F5F5)), "#F5F5F5 desde 2,5 m");
            Assert.That(HullMesh.DeckColor(9.0), Is.EqualTo(SceneColor.FromArgb(LakeArt.HullWhite)));
            var mid = HullMesh.DeckColor(LakeArt.HullStern + 1.25);
            Assert.That(mid.R, Is.EqualTo((0xF5 + 0xC9) / 2.0 / 255).Within(1e-9), "lineal");

            var mesh = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
            HullMesh.Build(mesh);
            // La manga en la punta: 0,06 m; a 0,6 m de la punta, la natural.
            double tipHalf = 0, deckTip = double.NaN, deckMid = double.NaN;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                if (Math.Abs(mesh.Z(v) - LakeArt.HullStern) < 1e-6)
                {
                    tipHalf = Math.Max(tipHalf, Math.Abs(mesh.X(v)));
                    if (Math.Abs(mesh.X(v)) < 1e-6 && mesh.Normals[3 * v + 1] > 0.99f)
                    {
                        deckTip = mesh.Y(v);
                    }
                }

                if (Math.Abs(mesh.Z(v) - HullMesh.SectionZ(6)) < 1e-6 && Math.Abs(mesh.X(v)) < 1e-6 && mesh.Normals[3 * v + 1] > 0.99f)
                {
                    deckMid = mesh.Y(v);
                }
            }

            Assert.That(tipHalf * 2, Is.EqualTo(LakeArt.HullSternTipBeam).Within(1e-6), "0,06 m de manga en la punta");
            Assert.That(deckMid - deckTip, Is.EqualTo(LakeArt.HullSternDeckDrop).Within(1e-6), "la cubierta 0,03 m más baja en la punta");
            Assert.That(HullMesh.SternBeamFactor(LakeArt.HullStern + 0.6), Is.EqualTo(1));
            Assert.That(HullMesh.SternBeamFactor(LakeArt.HullStern + 0.3), Is.LessThan(1));
            Assert.That(HullMesh.SternDrop(LakeArt.HullStern + 0.6), Is.EqualTo(0).Within(1e-12));

            // La sonda de CAP-31 lee ≥ 8 % de variación de punta a punta: el degradé solo ya da ~ 13 % de luminancia.
            var tip = SceneColor.FromArgb(0xFFC9D1D6).ToLinear().LinearLuminance();
            var white = SceneColor.FromArgb(0xFFF5F5F5).ToLinear().LinearLuminance();
            Assert.That(1 - tip / white, Is.GreaterThan(0.08));
        }

        [Test, Category("AC-6B-21"), Category("AC-SCN-01-08")]
        public void ContractUnchanged()
        {
            var mesh = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
            HullMesh.Build(mesh);
            mesh.Bounds(out var minX, out _, out var minZ, out var maxX, out var maxY, out var maxZ);
            Assert.That(minZ, Is.EqualTo(5.3).Within(1e-6));
            Assert.That(maxZ, Is.EqualTo(13.5).Within(1e-6));
            Assert.That(maxX * 2, Is.EqualTo(LakeArt.HullBeamGunwale).Within(0.01));
            Assert.That(maxY, Is.EqualTo(LakeArt.HullFreeboard + 0.02).Within(1e-6), "la cubierta a la altura de siempre fuera de la popa");
            Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.HullTriangleBudget));
            var stripe = SceneColor.FromArgb(LakeArt.HullStripe);
            var cockpit = SceneColor.FromArgb(LakeArt.HullCockpit);
            int stripes = 0, cockpits = 0;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                var isStripe = Math.Abs(mesh.Colors[4 * v] - stripe.R) < 1e-6 && Math.Abs(mesh.Colors[4 * v + 2] - stripe.B) < 1e-6;
                var isCockpit = Math.Abs(mesh.Colors[4 * v] - cockpit.R) < 1e-6 && Math.Abs(mesh.Colors[4 * v + 2] - cockpit.B) < 1e-6;
                stripes += isStripe ? 1 : 0;
                cockpits += isCockpit ? 1 : 0;
                if (isCockpit)
                {
                    Assert.That(LakeArt.IsCockpit(mesh.Z(v)), Is.True);
                }
            }

            Assert.That(stripes, Is.GreaterThan(0), "la borda #00B4D8");
            Assert.That(cockpits, Is.GreaterThan(0), "el cockpit");
        }
    }
}
