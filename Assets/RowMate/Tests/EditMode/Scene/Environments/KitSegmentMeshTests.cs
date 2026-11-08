using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // KSM · La malla por segmento de río, costa y regata (WP-B1, respaldo sin los shaders de WP-D1) y la silueta lejana de los tres:
    // todos los objetos del legacy del segmento quedan dibujados con su colocación (las casas del río en su recorte, el puente, las
    // boyas en el agua), entra en la capacidad, es determinista y la submalla de la noche lleva solo lo que se enciende.
    [TestFixture]
    public sealed class KitSegmentMeshTests
    {
        private static readonly EnvironmentId[] Kits = { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private static void BuildSegment(EnvironmentId id, long k, int lod, MeshData into, KitSegmentMesh kit, ShoreSegments segments,
                                         RiverPlacement? placement)
        {
            var props = segments.PropsForSegment(k).ToArray();
            var foot = new double[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                foot[i] = PropPlacement.FootY(id, props[i]);
            }

            var houses = new HouseLayout[RiverPlacement.MaxHousesPerSegment];
            var n = placement != null ? placement.Houses(k, houses) : 0;
            var fill = new FillBuffer(EnvironmentFill.SegmentCapacity);
            EnvironmentFill.Segment(id, k, props, new DartRandom(1), fill);
            kit.Build(k, props, foot, new ReadOnlySpan<HouseLayout>(houses, 0, n), fill.Items, lod, into);
        }

        [Test]
        public void EverySegmentFitsAndIsDeterministic([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            var kit = new KitSegmentMesh(id);
            kit.WarmUp();
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            var placement = id == EnvironmentId.River ? new RiverPlacement(segments) : null;
            var a = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            var b = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            var maxVertices = 0;
            for (long k = 0; k < 200; k++)
            {
                for (var lod = 0; lod < KitSegmentMesh.Lods; lod++)
                {
                    BuildSegment(id, k, lod, a, kit, segments, placement);
                    BuildSegment(id, k, lod, b, kit, segments, placement);
                    Assert.That(a.VertexCount, Is.EqualTo(b.VertexCount), "segmento " + k);
                    Assert.That(a.SubmeshCount, Is.EqualTo(2));
                    Assert.That(a.TriangleCount, Is.GreaterThan(0), id + " segmento " + k + " sin nada que dibujar");
                    maxVertices = Math.Max(maxVertices, a.VertexCount);
                    for (var i = 0; i < a.VertexCount * 3; i++)
                    {
                        Assert.That(a.Positions[i], Is.EqualTo(b.Positions[i]));
                        Assert.That(float.IsNaN(a.Positions[i]), Is.False);
                    }
                }
            }

            TestContext.WriteLine(id + ": hasta " + maxVertices + " vértices por segmento");
        }

        // Cada objeto del legacy tiene geometría en su ancla (x, z de cinta del segmento) y nada queda bajo el agua salvo el muro y el
        // pie de las boyas y del puente.
        [Test]
        public void EveryLegacyPropHasGeometryAtItsAnchor([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            var kit = new KitSegmentMesh(id);
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            var placement = id == EnvironmentId.River ? new RiverPlacement(segments) : null;
            var mesh = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            for (long k = 0; k < 60; k++)
            {
                BuildSegment(id, k, 0, mesh, kit, segments, placement);
                var props = segments.PropsForSegment(k);
                var z0 = k * ShoreEnvironment.SegmentLength;
                foreach (var p in props.ToArray())
                {
                    var anchorX = p.Kind == PropKind.Bridge ? 0 : p.X;
                    var found = false;
                    for (var v = 0; v < mesh.VertexCount && !found; v++)
                    {
                        found = Math.Abs(mesh.X(v) - anchorX) < 6 && Math.Abs(mesh.Z(v) - (p.Z - z0)) < 6;
                    }

                    Assert.That(found, Is.True, id + ": " + p.Kind + " en (" + p.X + "; " + p.Z + ") sin geometría");
                }
            }
        }

        [Test]
        public void TheRiverHousesStayOnTheirSideOfTheWall()
        {
            var id = EnvironmentId.River;
            var kit = new KitSegmentMesh(id);
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            var placement = new RiverPlacement(segments);
            var mesh = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            var houses = new HouseLayout[RiverPlacement.MaxHousesPerSegment];
            for (long k = 0; k < 100; k++)
            {
                var n = placement.Houses(k, houses);
                for (var h = 0; h < n; h++)
                {
                    // La caja de la casa recortada (R2): todo en |x| ≥ 10,1 salvo el alero, que ya está en InnerAbsX.
                    Assert.That(houses[h].InnerAbsX, Is.GreaterThanOrEqualTo(RiverArt.FaceMinX - 1e-9));
                }

                BuildSegment(id, k, 1, mesh, kit, segments, placement);
                for (var v = 0; v < mesh.VertexCount; v++)
                {
                    // Lo que está arriba del malecón (las casas) nunca pasa al canal (|x| < 9) salvo el puente (arco, pilares y tablero, en
                    // z₀ + 20 a z₀ + 30 de los segmentos con k mod 16 = 5) y el globo de los faroles (en ±9,5, de radio 0,3).
                    var y = mesh.Y(v);
                    var ax = Math.Abs(mesh.X(v));
                    var bridge = k % 16 == 5 && mesh.Z(v) > 19.9 && mesh.Z(v) < 30.1;
                    if (!bridge && y > RiverArt.QuayHeight + 0.5 && ax < RiverArt.WallX - 0.1)
                    {
                        Assert.That(ax, Is.GreaterThan(ShoreEnvironment.BankX + 0.5 - RiverArt.LampGlobeRadius - 0.1), "algo en el canal a " + y + " m en x "
                            + mesh.X(v) + ", z " + mesh.Z(v) + " del segmento " + k);
                    }
                }
            }
        }

        [Test]
        public void TheNightSubmeshOnlyHasWhatLightsUp()
        {
            var kit = new KitSegmentMesh(EnvironmentId.Regatta);
            var segments = new ShoreSegments(EnvironmentCatalog.Create(EnvironmentId.Regatta));
            var mesh = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            // Segmento 12: la torre de llegada (k mod 20 = 12): su sala de control va a la submalla 1.
            BuildSegment(EnvironmentId.Regatta, 12, 0, mesh, kit, segments, null);
            mesh.Submesh(1, out _, out var glow);
            Assert.That(glow, Is.GreaterThan(0));
            BuildSegment(EnvironmentId.Regatta, 13, 0, mesh, kit, segments, null);
            mesh.Submesh(1, out _, out glow);
            Assert.That(glow, Is.EqualTo(0), "sin torre no hay nada que se encienda");
        }

        [Test]
        public void TheMarkerHasItsDigits()
        {
            var kit = new KitSegmentMesh(EnvironmentId.Regatta);
            var segments = new ShoreSegments(EnvironmentCatalog.Create(EnvironmentId.Regatta));
            var withMarker = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            // k = 5: cartel de 250 m (k mod 5 = 0) a −10,5.
            BuildSegment(EnvironmentId.Regatta, 5, 0, withMarker, kit, segments, null);
            var dark = 0;
            for (var v = 0; v < withMarker.VertexCount; v++)
            {
                if (Math.Abs(withMarker.Colors[v * 4] - 0x1A / 255.0) < 1e-3 && Math.Abs(withMarker.X(v) + 10.5) < 1 && withMarker.Y(v) > 1.5)
                {
                    dark++;
                }
            }

            // "250 m": 2 + 5 + 6 segmentos y los 4 trazos de la m, 4 vértices cada uno.
            Assert.That(dark, Is.EqualTo(4 * (5 + 5 + 6 + 4)));
        }

        [Test]
        public void TheFarSilhouettesCoverTheStripAndStayLow([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            var mesh = new MeshData(EnvironmentHorizonStrip.VertexCapacity, EnvironmentHorizonStrip.IndexCapacity);
            EnvironmentHorizonStrip.Build(id, mesh);
            Assert.That(mesh.TriangleCount, Is.GreaterThan(0));
            mesh.Bounds(out var minX, out var minY, out _, out var maxX, out var maxY, out _);
            Assert.That(minX, Is.EqualTo(SceneMath.DegToRad(LakeArt.StripMinAzimuthDeg)).Within(1e-5));
            Assert.That(maxX, Is.EqualTo(SceneMath.DegToRad(LakeArt.StripMaxAzimuthDeg)).Within(1e-5));
            Assert.That(minY, Is.GreaterThanOrEqualTo(0));
            Assert.That(maxY, Is.LessThanOrEqualTo(1.0 + 1e-9));
            for (var i = 1; i < mesh.VertexCount && mesh.Z(i) == 0; i++)
            {
                Assert.That(mesh.X(i), Is.GreaterThanOrEqualTo(mesh.X(i - 1) - 1e-6), "la tira va de izquierda a derecha");
            }
        }

        // QA-V8-01 (AC-SCN-19-05; regatta_environment.dart:56-68): la tira de la regata trae los grupos de 4 banderas (z = 1 a 4) con su
        // mástil (z = 5) en el acimut del legacy; el paño va de p 0,6 a 0,5 con la punta (r = 1, la que flamea) en 0,55 a 6 px de TV.
        // Río y costa no tienen.
        [Test, Category("AC-SCN-19-05")]
        public void TheRegattaStripCarriesTheFarFlags([ValueSource(nameof(Kits))] EnvironmentId id)
        {
            var mesh = new MeshData(EnvironmentHorizonStrip.VertexCapacity, EnvironmentHorizonStrip.IndexCapacity);
            EnvironmentHorizonStrip.Build(id, mesh);
            var perCode = new int[6];
            var tips = 0;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                var code = (int)Math.Round(mesh.Z(v));
                perCode[code]++;
                if (code >= 1 && code <= 4 && mesh.Colors[v * 4] > 0.5)
                {
                    tips++;
                    Assert.That(mesh.Y(v), Is.EqualTo(RegattaBackdrop.FlagTipP).Within(1e-6));
                }
            }

            if (id != EnvironmentId.Regatta)
            {
                Assert.That(perCode[1] + perCode[2] + perCode[3] + perCode[4] + perCode[5], Is.EqualTo(0), id + " sin banderas lejanas");
                return;
            }

            for (var code = 1; code <= 4; code++)
            {
                Assert.That(perCode[code], Is.GreaterThanOrEqualTo(3 * 4), "al menos 4 grupos con la bandera " + code);
            }

            Assert.That(perCode[5], Is.EqualTo(4 * (perCode[1] + perCode[2] + perCode[3] + perCode[4]) / 3), "un mástil por bandera");
            Assert.That(tips, Is.EqualTo((perCode[1] + perCode[2] + perCode[3] + perCode[4]) / 3), "una punta por bandera");
            // La bandera 0 del bloque 0 está en el acimut del legacy (x + 0,42·w desde el borde izquierdo de TV).
            var found = false;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                if (Math.Round(mesh.Z(v)) == 1 && Math.Abs(mesh.X(v) - RegattaBackdrop.FlagAzimuthRad(0, 0)) < 1e-6)
                {
                    found = true;
                }
            }

            Assert.That(found, Is.True);
            Assert.That(RegattaBackdrop.FlagWave(1, 0.5), Is.EqualTo(Math.Sin(3 * 0.5 + 1) * 2 / (914 * 0.06)).Within(1e-12));
            Assert.That(ShaderArt.Constants()["kRmFarFlagWave"], Is.EqualTo(RegattaBackdrop.FlagWaveP));
        }

        [Test]
        public void TheLakeIsNotAKit()
        {
            Assert.Throws<ArgumentException>(() => new KitSegmentMesh(EnvironmentId.Lake));
        }
    }
}
