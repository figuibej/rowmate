using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // RGM · Las mallas de la It. 7 (02-tech-design §2.13; AC-SCN-08-01, AC-SCN-01-08; arte §2, §5, §9): el esqueleto y la malla del
    // maniquí (≤ 6 000 triángulos, pesos que suman 1, largos de hueso ± 1 mm, regiones), el remo, los herrajes, el carro, la boya y
    // el cockpit de 9,05 a 10,45.
    [TestFixture]
    public sealed class RowerGeometryTests
    {
        [Test, Category("AC-SCN-08-01")]
        public void TheMannequinSkeletonHasTheMixamoBodyBonesWithTheRigLengths()
        {
            var r = RowerSpec.Mannequin;
            var bones = MannequinSkeleton.Build(r);
            Assert.That(bones.Length, Is.EqualTo(MannequinSkeleton.BoneCount));
            var names = new HashSet<string>();
            for (var i = 0; i < bones.Length; i++)
            {
                Assert.That(names.Add(bones[i].Name), Is.True, bones[i].Name);
                Assert.That(bones[i].Name, Does.StartWith("mixamorig:"));
                Assert.That(bones[i].Parent, Is.LessThan(i), "los padres antes que los hijos");
            }

            Assert.That(bones[0].Parent, Is.EqualTo(-1));
            Assert.That(MannequinSkeleton.IndexOf(bones, "mixamorig:LeftForeArm"), Is.EqualTo(MannequinSkeleton.LeftForeArm));
            Assert.That(bones[MannequinSkeleton.LeftArm].X, Is.LessThan(0), "la izquierda en −x (FBX de Mixamo en Unity)");
            Length(bones, MannequinSkeleton.LeftUpLeg, MannequinSkeleton.LeftLeg, r.Thigh);
            Length(bones, MannequinSkeleton.RightLeg, MannequinSkeleton.RightFoot, r.Shin);
            Length(bones, MannequinSkeleton.LeftArm, MannequinSkeleton.LeftForeArm, r.UpperArm);
            Length(bones, MannequinSkeleton.RightForeArm, MannequinSkeleton.RightHand, r.Forearm);
            Length(bones, MannequinSkeleton.Head, MannequinSkeleton.HeadTop, r.HeadTopAbove);
            Assert.That(bones[MannequinSkeleton.HeadTop].Y, Is.EqualTo(1.85).Within(0.001), "1,85 m");
            Assert.That(bones[MannequinSkeleton.LeftArm].X, Is.EqualTo(-r.ShoulderHalfWidth).Within(0.001));
        }

        [Test, Category("AC-SCN-08-01")]
        public void TheMannequinMeshIsUnderBudgetWithValidSkinAndRegions()
        {
            var mesh = new SkinnedMeshData(MannequinMesh.VertexCapacity, MannequinMesh.IndexCapacity);
            MannequinMesh.Build(RowerSpec.Mannequin, mesh);
            Assert.That(mesh.Mesh.TriangleCount, Is.LessThanOrEqualTo(RowerArt.MannequinTriangleBudget));
            Assert.That(mesh.Mesh.TriangleCount, Is.GreaterThan(1500), "un cuerpo, no cápsulas sueltas");
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                Assert.That(mesh.Weight0[i] + mesh.Weight1[i], Is.EqualTo(1).Within(1e-6), "pesos que suman 1");
                Assert.That(mesh.Bone0[i], Is.InRange(0, MannequinSkeleton.BoneCount - 1));
                Assert.That(mesh.Bone1[i], Is.InRange(0, MannequinSkeleton.BoneCount - 1));
            }

            for (var i = 0; i < mesh.Mesh.IndexCount; i++)
            {
                Assert.That(mesh.Mesh.Indices[i], Is.LessThan(mesh.VertexCount));
            }

            mesh.Mesh.Bounds(out var minX, out var minY, out _, out var maxX, out var maxY, out _);
            Assert.That(maxY, Is.EqualTo(1.85).Within(0.03), "el alto");
            Assert.That(minY, Is.GreaterThanOrEqualTo(-0.01), "los pies en el piso");
            Assert.That(maxX - minX, Is.GreaterThan(1.7), "los brazos en T");
            var regions = MannequinMesh.Regions(mesh);
            foreach (var region in new[] { RowerArt.RegionSuit, RowerArt.RegionStripe, RowerArt.RegionSkin, RowerArt.RegionSocks, RowerArt.RegionGlasses, RowerArt.RegionHair })
            {
                Assert.That(regions, Has.Member(region), "región " + region);
            }

            Assert.That(regions, Has.No.Member(RowerArt.RegionCap), "la gorra apagada (DT7-09)");
            Assert.That(regions, Has.No.Member(RowerArt.RegionJoints), "las articulaciones son de Y Bot");
            Assert.That(RowerArt.RegionColors[RowerArt.RegionSuit], Is.EqualTo(0xFF1565C0u), "el mono del contrato");
            Assert.That(RowerArt.RegionColors.Length, Is.EqualTo(RowerArt.RegionCount));
            Assert.That(RowerArt.RegionSmoothness.Length, Is.EqualTo(RowerArt.RegionCount));
        }

        [Test, Category("AC-SCN-08-01"), Category("AC-SCN-08-03")]
        public void TheOarFittingsSeatAndBuoyAreUnderBudget()
        {
            var boat = BoatSpec.Real;
            var oar = new MeshData(OarMesh.VertexCapacity, OarMesh.IndexCapacity);
            OarMesh.Build(boat, oar);
            Assert.That(oar.TriangleCount, Is.LessThanOrEqualTo(RowerArt.OarTriangleBudget));
            Assert.That(oar.SubmeshCount, Is.EqualTo(2), "la caña y la pala (la pala proyecta sombra, DA7-06)");
            oar.Bounds(out var minX, out _, out _, out var maxX, out _, out _);
            Assert.That(minX, Is.EqualTo(-boat.Inboard).Within(1e-6));
            Assert.That(maxX, Is.EqualTo(boat.OarLength - boat.Inboard).Within(1e-6), "2,88 m");
            oar.Submesh(1, out _, out var bladeIndices);
            Assert.That(bladeIndices, Is.GreaterThan(0));
            Assert.That(OarMesh.BladeTop(0.5) - OarMesh.BladeBottom(0.5), Is.EqualTo(boat.BladeWidth).Within(0.01), "0,22 de ancho");

            var fit = RiggingFit.Solve(boat, RowerSpec.Mannequin);
            var fittings = new MeshData(BoatFittingsMesh.VertexCapacity, BoatFittingsMesh.IndexCapacity);
            BoatFittingsMesh.Build(boat, RowerSpec.Mannequin, fit, fittings);
            Assert.That(fittings.TriangleCount, Is.LessThanOrEqualTo(RowerArt.FittingsTriangleBudget));
            fittings.Bounds(out var fx0, out _, out _, out var fx1, out _, out _);
            Assert.That(fx1, Is.EqualTo(boat.PinX + 0.03).Within(1e-6), "las chumaceras en los pernos");
            Assert.That(fx0, Is.EqualTo(-boat.PinX - 0.03).Within(1e-6));

            var seat = new MeshData(SeatMesh.VertexCapacity, SeatMesh.IndexCapacity);
            SeatMesh.Build(seat);
            seat.Bounds(out var sx0, out _, out var sz0, out var sx1, out var sy1, out var sz1);
            Assert.That(sx1 - sx0, Is.EqualTo(0.24).Within(1e-6));
            Assert.That(sz1 - sz0, Is.EqualTo(0.20).Within(1e-6));
            Assert.That(sy1, Is.EqualTo(0).Within(1e-6), "la cara de arriba en 0 (se pone en SeatTopY)");

            var buoy = new MeshData(BuoyMesh.VertexCapacity, BuoyMesh.IndexCapacity);
            BuoyMesh.Build(buoy);
            Assert.That(buoy.TriangleCount, Is.LessThanOrEqualTo(RowerArt.BuoyTriangleBudget));
            buoy.Bounds(out var bx0, out var by0, out _, out var bx1, out var by1, out _);
            Assert.That(bx1 - bx0, Is.EqualTo(0.5).Within(1e-3), "Ø 0,50");
            Assert.That(by1, Is.EqualTo(0.35).Within(1e-3), "asoma 0,35");
            var stripe = 0;
            for (var i = 0; i < buoy.VertexCount; i++)
            {
                stripe += buoy.Colors[i * 4] > 0.5f ? 1 : 0;
            }

            Assert.That(stripe, Is.GreaterThan(0), "la franja blanca");
        }

        [Test, Category("AC-SCN-08-01"), Category("AC-SCN-01-08")]
        public void TheCockpitGoesFrom905To1045()
        {
            Assert.That(LakeArt.CockpitStartZ, Is.EqualTo(9.05));
            Assert.That(LakeArt.CockpitEndZ, Is.EqualTo(10.45));
            var mesh = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
            HullMesh.Build(mesh);
            var cockpit = SceneColor.FromArgb(LakeArt.HullCockpit);
            var minZ = double.MaxValue;
            var maxZ = double.MinValue;
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                if (Math.Abs(mesh.Colors[i * 4] - cockpit.R) < 1e-6 && Math.Abs(mesh.Colors[i * 4 + 2] - cockpit.B) < 1e-6)
                {
                    minZ = Math.Min(minZ, mesh.Z(i));
                    maxZ = Math.Max(maxZ, mesh.Z(i));
                }
            }

            Assert.That(minZ, Is.GreaterThanOrEqualTo(9.05 - 1e-6));
            Assert.That(maxZ, Is.LessThanOrEqualTo(10.45 + 1e-6));
            Assert.That(maxZ, Is.GreaterThan(10.0), "se corre hacia proa (DA7-02)");
            Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.HullTriangleBudget));
        }

        private static void Length(SkeletonBone[] bones, int a, int b, double length)
        {
            Assert.That(SceneVecLength(bones[a], bones[b]), Is.EqualTo(length).Within(0.001), bones[a].Name + " → " + bones[b].Name);
        }

        private static double SceneVecLength(in SkeletonBone a, in SkeletonBone b)
        {
            return (a.Position - b.Position).Length;
        }
    }
}
