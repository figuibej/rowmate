using System;
using System.Collections.Generic;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Core.Scene.Geometry
{
    // Un hueso del esqueleto del maniquí: el nombre de Mixamo, el padre (−1 la raíz), la posición de reposo (T-pose, marco local del
    // personaje: mira a +z, la izquierda en −x, los pies en y = 0, como un FBX de Mixamo importado en Unity) y la articulación de
    // la pose que lo mueve (RowerJoint).
    public readonly struct SkeletonBone
    {
        public SkeletonBone(string name, int parent, double x, double y, double z, RowerJoint joint)
        {
            Name = name;
            Parent = parent;
            X = x;
            Y = y;
            Z = z;
            Joint = joint;
        }

        public string Name { get; }
        public int Parent { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public RowerJoint Joint { get; }

        public SceneVec3 Position => new SceneVec3(X, Y, Z);
    }

    // El esqueleto del maniquí procedural (DT7-01, DA7-03): los huesos del cuerpo de Mixamo (sin dedos: el puño es rígido) con las
    // proporciones de RowerSpec, en T-pose. ProceduralRower (WP-B1) arma los Transform con estos nombres; el mismo RowerRigDriver mueve
    // el maniquí e Y Bot.
    public static class MannequinSkeleton
    {
        public const int Hips = 0, Spine = 1, Spine1 = 2, Spine2 = 3, Neck = 4, Head = 5, HeadTop = 6;
        public const int LeftShoulder = 7, LeftArm = 8, LeftForeArm = 9, LeftHand = 10;
        public const int RightShoulder = 11, RightArm = 12, RightForeArm = 13, RightHand = 14;
        public const int LeftUpLeg = 15, LeftLeg = 16, LeftFoot = 17, LeftToeBase = 18, LeftToeEnd = 19;
        public const int RightUpLeg = 20, RightLeg = 21, RightFoot = 22, RightToeBase = 23, RightToeEnd = 24;
        public const int BoneCount = 25;

        public static SkeletonBone[] Build(in RowerSpec r)
        {
            var hip = r.AnkleHeight + r.Shin + r.Thigh;
            var spine = hip + r.PelvisLength;
            var spine1 = spine + r.SpineLength;
            var spine2 = spine1 + r.Spine1Length;
            var neck = spine2 + r.Spine2Length;
            var head = neck + r.NeckLength;
            var shoulderY = neck - r.ShoulderDrop;
            var bones = new SkeletonBone[BoneCount];
            bones[Hips] = new SkeletonBone("mixamorig:Hips", -1, 0, hip + r.HipsAboveJoints, 0, RowerJoint.Hips);
            bones[Spine] = new SkeletonBone("mixamorig:Spine", Hips, 0, spine, 0, RowerJoint.Spine);
            bones[Spine1] = new SkeletonBone("mixamorig:Spine1", Spine, 0, spine1, 0, RowerJoint.Spine1);
            bones[Spine2] = new SkeletonBone("mixamorig:Spine2", Spine1, 0, spine2, 0, RowerJoint.Spine2);
            bones[Neck] = new SkeletonBone("mixamorig:Neck", Spine2, 0, neck, 0, RowerJoint.Neck);
            bones[Head] = new SkeletonBone("mixamorig:Head", Neck, 0, head, 0, RowerJoint.Head);
            bones[HeadTop] = new SkeletonBone("mixamorig:HeadTop_End", Head, 0, head + r.HeadTopAbove, 0, RowerJoint.HeadTop);
            for (var k = 0; k < 2; k++)
            {
                var left = k == 0;
                var s = left ? -1.0 : 1.0;
                var side = left ? "Left" : "Right";
                var shoulder = left ? LeftShoulder : RightShoulder;
                bones[shoulder] = new SkeletonBone("mixamorig:" + side + "Shoulder", Spine2, s * r.ClavicleHalfWidth, shoulderY, 0,
                    left ? RowerJoint.LeftShoulder : RowerJoint.RightShoulder);
                bones[shoulder + 1] = new SkeletonBone("mixamorig:" + side + "Arm", shoulder, s * r.ShoulderHalfWidth, shoulderY, 0,
                    left ? RowerJoint.LeftArm : RowerJoint.RightArm);
                bones[shoulder + 2] = new SkeletonBone("mixamorig:" + side + "ForeArm", shoulder + 1, s * (r.ShoulderHalfWidth + r.UpperArm), shoulderY, 0,
                    left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm);
                bones[shoulder + 3] = new SkeletonBone("mixamorig:" + side + "Hand", shoulder + 2, s * (r.ShoulderHalfWidth + r.UpperArm + r.Forearm),
                    shoulderY, 0, left ? RowerJoint.LeftHand : RowerJoint.RightHand);
                var upLeg = left ? LeftUpLeg : RightUpLeg;
                var x = s * r.HipHalfWidth;
                bones[upLeg] = new SkeletonBone("mixamorig:" + side + "UpLeg", Hips, x, hip, 0, left ? RowerJoint.LeftUpLeg : RowerJoint.RightUpLeg);
                bones[upLeg + 1] = new SkeletonBone("mixamorig:" + side + "Leg", upLeg, x, r.AnkleHeight + r.Shin, 0, left ? RowerJoint.LeftLeg : RowerJoint.RightLeg);
                bones[upLeg + 2] = new SkeletonBone("mixamorig:" + side + "Foot", upLeg + 1, x, r.AnkleHeight, 0, left ? RowerJoint.LeftFoot : RowerJoint.RightFoot);
                bones[upLeg + 3] = new SkeletonBone("mixamorig:" + side + "ToeBase", upLeg + 2, x, 0.02, 0.72 * r.FootLength - 0.2 * r.FootLength,
                    left ? RowerJoint.LeftToeBase : RowerJoint.RightToeBase);
                bones[upLeg + 4] = new SkeletonBone("mixamorig:" + side + "Toe_End", upLeg + 3, x, 0.02, 0.8 * r.FootLength,
                    left ? RowerJoint.LeftToeEnd : RowerJoint.RightToeEnd);
            }

            return bones;
        }

        // El índice de un hueso por nombre (−1 si no está).
        public static int IndexOf(SkeletonBone[] bones, string name)
        {
            for (var i = 0; i < bones.Length; i++)
            {
                if (bones[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    // La malla del maniquí (arte §2.2, §2.3; DT7-09): un loft de superelipse (exponente 2,5, 12 lados, un anillo cada 3,5 cm) por
    // segmento, con los pesos repartidos entre dos huesos en ± 4 cm de cada articulación (± 5 en rodilla y codo) y la región del
    // shader Rower en el rojo del color de vértice (RowerArt.RegionToRed): mono con franjas a los costados del tronco y del muslo
    // (hasta 6 cm sobre la rodilla), sin mangas; piel, medias y zapatillas, anteojos y pelo. ≤ 6 000 triángulos. En el marco local del
    // esqueleto (T-pose).
    public static class MannequinMesh
    {
        public const int VertexCapacity = 2400;
        public const int IndexCapacity = 3 * 6000;

        private delegate void BoneRule(in SceneVec3 p, out int bone0, out int bone1, out double weight0);

        private delegate int RegionRule(in SceneVec3 p, in SceneVec3 normal);

        public static void Build(in RowerSpec r, SkinnedMeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var bones = MannequinSkeleton.Build(r);
            var hip = bones[MannequinSkeleton.LeftUpLeg].Y;
            var spineY = bones[MannequinSkeleton.Spine].Y;
            var spine1Y = bones[MannequinSkeleton.Spine1].Y;
            var spine2Y = bones[MannequinSkeleton.Spine2].Y;
            var neckY = bones[MannequinSkeleton.Neck].Y;
            var headY = bones[MannequinSkeleton.Head].Y;
            var topY = bones[MannequinSkeleton.HeadTop].Y;
            var kneeY = bones[MannequinSkeleton.LeftLeg].Y;
            var ankleY = bones[MannequinSkeleton.LeftFoot].Y;
            var shoulderY = bones[MannequinSkeleton.LeftArm].Y;
            var k = r.Height / 1.85;
            var blend = RowerArt.BlendHalfWidth * k;
            var blendJoint = RowerArt.BlendHalfWidthKneeElbow * k;

            // El tronco: de la pelvis a la base del cuello, con los huesos por altura.
            var torsoT = new[] { 0.0, 0.10, 0.20, 0.37, 0.53, 0.70, 0.87, 0.95, 1.0 };
            var torsoA = new[] { 0.15, 0.175, 0.17, 0.145, 0.15, 0.17, 0.17, 0.15, 0.09 };
            var torsoB = new[] { 0.10, 0.11, 0.11, 0.10, 0.105, 0.12, 0.115, 0.10, 0.07 };
            Scale(torsoA, k);
            Scale(torsoB, k);
            BoneRule torsoBones = (in SceneVec3 p, out int b0, out int b1, out double w0) =>
            {
                Chain(p.Y, new[] { spineY, spine1Y, spine2Y },
                    new[] { MannequinSkeleton.Hips, MannequinSkeleton.Spine, MannequinSkeleton.Spine1, MannequinSkeleton.Spine2 }, blend,
                    out b0, out b1, out w0);
            };
            RegionRule suit = (in SceneVec3 p, in SceneVec3 n) => Math.Abs(n.X) > 0.85 ? RowerArt.RegionStripe : RowerArt.RegionSuit;
            Tube(mesh, new SceneVec3(0, hip - 0.06 * k, 0), new SceneVec3(0, shoulderY + 0.01 * k, 0), SceneVec3.UnitX, torsoT, torsoA, torsoB,
                RowerArt.LoftExponent, torsoBones, suit, true, true);

            // El cuello.
            Tube(mesh, new SceneVec3(0, neckY - 0.03 * k, 0), new SceneVec3(0, headY + 0.03 * k, 0), SceneVec3.UnitX, new[] { 0.0, 1.0 },
                new[] { 0.056 * k, 0.05 * k }, new[] { 0.06 * k, 0.052 * k }, 2.0,
                Span(MannequinSkeleton.Neck, MannequinSkeleton.Spine2, MannequinSkeleton.Head, neckY, headY, blend, true), Fixed(RowerArt.RegionSkin),
                false, false);

            // La cabeza: un ovoide de 0,16 × 0,20 × 0,24 con la nariz insinuada, pelo arriba y atrás y la franja de los anteojos.
            var headBottom = headY - 0.03 * k;
            var eyeY = headY + 0.095 * k;
            var headT = new[] { 0.0, 0.08, 0.2, 0.35, 0.5, 0.65, 0.8, 0.92, 1.0 };
            var headA = new double[headT.Length];
            var headB = new double[headT.Length];
            for (var i = 0; i < headT.Length; i++)
            {
                var e = 2 * headT[i] - 1;
                var s = Math.Sqrt(Math.Max(0.02, 1 - e * e));
                headA[i] = 0.08 * k * s;
                headB[i] = 0.10 * k * s;
            }

            RegionRule headRegion = (in SceneVec3 p, in SceneVec3 n) =>
            {
                if (p.Z > 0.03 * k && p.Y > eyeY - 0.018 * k && p.Y < eyeY + 0.016 * k)
                {
                    return RowerArt.RegionGlasses;
                }

                return p.Y > eyeY + 0.035 * k && n.Z < 0.55 || p.Y > eyeY - 0.03 * k && n.Z < -0.35 ? RowerArt.RegionHair : RowerArt.RegionSkin;
            };
            Tube(mesh, new SceneVec3(0, headBottom, 0.01 * k), new SceneVec3(0, topY, 0.01 * k), SceneVec3.UnitX, headT, headA, headB, 2.0,
                Single(MannequinSkeleton.Head), headRegion, true, true, nose: new SceneVec3(0, eyeY - 0.035 * k, 0.02 * k));

            for (var side = 0; side < 2; side++)
            {
                var left = side == 0;
                var s = left ? -1.0 : 1.0;
                var axis = new SceneVec3(s, 0, 0);
                var shoulder = left ? MannequinSkeleton.LeftShoulder : MannequinSkeleton.RightShoulder;
                var arm = shoulder + 1;
                var fore = shoulder + 2;
                var hand = shoulder + 3;
                var armX = bones[arm].X;
                var elbowX = bones[fore].X;
                var wristX = bones[hand].X;

                // El brazo con el deltoides, el antebrazo y el puño cerrado (rígido, atado a Hand).
                Tube(mesh, new SceneVec3(armX - s * 0.04 * k, shoulderY, 0), new SceneVec3(elbowX, shoulderY, 0), SceneVec3.UnitY,
                    new[] { 0.0, 0.2, 0.6, 1.0 }, new[] { 0.06 * k, 0.055 * k, 0.045 * k, 0.0375 * k }, new[] { 0.06 * k, 0.052 * k, 0.043 * k, 0.036 * k },
                    RowerArt.LoftExponent, Span(arm, shoulder, fore, armX, elbowX, blendJoint, false), Fixed(RowerArt.RegionSkin), true, false);
                Tube(mesh, new SceneVec3(elbowX, shoulderY, 0), new SceneVec3(wristX, shoulderY, 0), SceneVec3.UnitY,
                    new[] { 0.0, 0.33, 1.0 }, new[] { 0.040 * k, 0.0425 * k, 0.0275 * k }, new[] { 0.036 * k, 0.038 * k, 0.022 * k },
                    RowerArt.LoftExponent, Span(fore, arm, hand, elbowX, wristX, blendJoint, false), Fixed(RowerArt.RegionSkin), false, false);
                Tube(mesh, new SceneVec3(wristX, shoulderY, 0), new SceneVec3(wristX + s * 0.10 * k, shoulderY, 0), SceneVec3.UnitY,
                    new[] { 0.0, 0.3, 0.8, 1.0 }, new[] { 0.032 * k, 0.045 * k, 0.045 * k, 0.03 * k }, new[] { 0.024 * k, 0.028 * k, 0.026 * k, 0.02 * k },
                    4.0, Single(hand), Fixed(RowerArt.RegionSkin), false, true);

                // El muslo (mono hasta 6 cm sobre la rodilla, con la franja), la pierna (medias abajo) y la zapatilla.
                var upLeg = left ? MannequinSkeleton.LeftUpLeg : MannequinSkeleton.RightUpLeg;
                var x = bones[upLeg].X;
                var kneeLimit = kneeY + 0.06 * k;
                RegionRule thigh = (in SceneVec3 p, in SceneVec3 n) =>
                    p.Y > kneeLimit ? (Math.Abs(n.X) > 0.85 ? RowerArt.RegionStripe : RowerArt.RegionSuit) : RowerArt.RegionSkin;
                Tube(mesh, new SceneVec3(x, hip + 0.02 * k, 0), new SceneVec3(x, kneeY, 0), SceneVec3.UnitX,
                    new[] { 0.0, 0.5, 0.88, 1.0 }, new[] { 0.085 * k, 0.07 * k, 0.056 * k, 0.052 * k }, new[] { 0.085 * k, 0.072 * k, 0.058 * k, 0.054 * k },
                    RowerArt.LoftExponent, Span(upLeg, MannequinSkeleton.Hips, upLeg + 1, hip, kneeY, blendJoint, true), thigh, false, false);
                var sockLimit = ankleY + 0.10 * k;
                RegionRule shin = (in SceneVec3 p, in SceneVec3 n) => p.Y < sockLimit ? RowerArt.RegionSocks : RowerArt.RegionSkin;
                Tube(mesh, new SceneVec3(x, kneeY, 0), new SceneVec3(x, ankleY - 0.02 * k, 0), SceneVec3.UnitX,
                    new[] { 0.0, 0.33, 0.8, 1.0 }, new[] { 0.05 * k, 0.0575 * k, 0.04 * k, 0.035 * k }, new[] { 0.052 * k, 0.06 * k, 0.04 * k, 0.036 * k },
                    RowerArt.LoftExponent, Span(upLeg + 1, upLeg, upLeg + 2, kneeY, ankleY, blendJoint, true), shin, false, false);
                var toeZ = bones[upLeg + 3].Z;
                BoneRule foot = (in SceneVec3 p, out int b0, out int b1, out double w0) =>
                {
                    Chain(p.Z, new[] { toeZ }, new[] { upLeg + 2, upLeg + 3 }, blend, out b0, out b1, out w0);
                };
                Tube(mesh, new SceneVec3(x, 0.045 * k, -0.06 * k), new SceneVec3(x, 0.035 * k, 0.8 * r.FootLength), SceneVec3.UnitX,
                    new[] { 0.0, 0.2, 0.7, 1.0 }, new[] { 0.035 * k, 0.045 * k, 0.05 * k, 0.03 * k }, new[] { 0.045 * k, 0.045 * k, 0.035 * k, 0.025 * k },
                    4.0, foot, Fixed(RowerArt.RegionSocks), true, true);
            }
        }

        private static void Scale(double[] values, double k)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] *= k;
            }
        }

        private static BoneRule Single(int bone)
        {
            return (in SceneVec3 p, out int b0, out int b1, out double w0) =>
            {
                b0 = bone;
                b1 = bone;
                w0 = 1;
            };
        }

        private static RegionRule Fixed(int region)
        {
            return (in SceneVec3 p, in SceneVec3 n) => region;
        }

        // Un segmento de un hueso entre dos coordenadas (y si vertical; x si no), mezclado con el anterior y el siguiente en ± blend.
        private static BoneRule Span(int bone, int before, int after, double start, double end, double blend, bool vertical)
        {
            return (in SceneVec3 p, out int b0, out int b1, out double w0) =>
            {
                var c = vertical ? p.Y : p.X;
                var fromStart = Math.Abs(c - start);
                var toEnd = Math.Abs(end - c);
                b0 = bone;
                if (fromStart < blend && fromStart <= toEnd)
                {
                    b1 = before;
                    w0 = 0.5 + 0.5 * fromStart / blend;
                }
                else if (toEnd < blend)
                {
                    b1 = after;
                    w0 = 0.5 + 0.5 * toEnd / blend;
                }
                else
                {
                    b1 = bone;
                    w0 = 1;
                }

            };
        }

        // Una cadena de huesos por una coordenada creciente: bones[i] entre cuts[i − 1] y cuts[i], mezclando en ± blend de cada corte.
        private static void Chain(double c, double[] cuts, int[] bones, double blend, out int b0, out int b1, out double w0)
        {
            var i = 0;
            while (i < cuts.Length && c >= cuts[i])
            {
                i++;
            }

            b0 = bones[i];
            b1 = b0;
            w0 = 1;
            if (i < cuts.Length && cuts[i] - c < blend)
            {
                b1 = bones[i + 1];
                w0 = 0.5 + 0.5 * (cuts[i] - c) / blend;
            }
            else if (i > 0 && c - cuts[i - 1] < blend)
            {
                b1 = bones[i - 1];
                w0 = 0.5 + 0.5 * (c - cuts[i - 1]) / blend;
            }
        }

        // Un tubo de superelipse de p0 a p1: anillos cada RingSpacing con los semiejes interpolados entre las claves (u = el eje dado,
        // v = u × eje), tapas en abanico y una nariz opcional (empuja hacia +z los vértices del frente cerca de ese punto).
        private static void Tube(SkinnedMeshData mesh, in SceneVec3 p0, in SceneVec3 p1, in SceneVec3 uAxis, double[] keyT, double[] keyA,
                                 double[] keyB, double exponent, BoneRule bones, RegionRule region, bool capStart, bool capEnd,
                                 SceneVec3? nose = null)
        {
            var axis = (p1 - p0).Normalized(SceneVec3.UnitY);
            var u = (uAxis - axis * SceneVec3.Dot(uAxis, axis)).Normalized(SceneVec3.UnitX);
            var v = SceneVec3.Cross(u, axis);
            var length = (p1 - p0).Length;
            var rings = Math.Max(2, (int)Math.Ceiling(length / RowerArt.RingSpacing) + 1);
            var sides = RowerArt.LoftSides;
            var first = mesh.VertexCount;
            var power = 2.0 / exponent;
            for (var ring = 0; ring < rings; ring++)
            {
                var t = ring / (double)(rings - 1);
                Interpolate(keyT, keyA, keyB, t, out var a, out var b);
                var center = SceneVec3.Lerp(p0, p1, t);
                for (var j = 0; j <= sides; j++)
                {
                    var th = 2 * Math.PI * j / sides;
                    var c = Math.Cos(th);
                    var s = Math.Sin(th);
                    var px = Math.Sign(c) * Math.Pow(Math.Abs(c), power) * a;
                    var py = Math.Sign(s) * Math.Pow(Math.Abs(s), power) * b;
                    var pos = center + u * px + v * py;
                    var normal = (u * (px / (a * a + 1e-12)) + v * (py / (b * b + 1e-12))).Normalized(u);
                    if (nose.HasValue && normal.Z > 0.7)
                    {
                        var d = Math.Abs(pos.Y - nose.Value.Y);
                        if (d < 0.025)
                        {
                            pos = pos + new SceneVec3(0, 0, nose.Value.Z * (1 - d / 0.025) * Math.Max(0, 1 - Math.Abs(pos.X) / 0.02));
                        }
                    }

                    bones(pos, out var b0, out var b1, out var w0);
                    mesh.AddVertex(pos.X, pos.Y, pos.Z, normal.X, normal.Y, normal.Z, RowerArt.RegionToRed(region(pos, normal)), j / (double)sides, t,
                        b0, b1, w0);
                }
            }

            for (var ring = 0; ring < rings - 1; ring++)
            {
                for (var j = 0; j < sides; j++)
                {
                    var a = first + ring * (sides + 1) + j;
                    var b = a + 1;
                    var d = a + sides + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }

            if (capStart)
            {
                Cap(mesh, first, p0, -axis, sides, bones, region, false);
            }

            if (capEnd)
            {
                Cap(mesh, first + (rings - 1) * (sides + 1), p1, axis, sides, bones, region, true);
            }
        }

        private static void Cap(SkinnedMeshData mesh, int ringFirst, in SceneVec3 center, in SceneVec3 normal, int sides, BoneRule bones,
                                RegionRule region, bool end)
        {
            bones(center, out var b0, out var b1, out var w0);
            var c = mesh.AddVertex(center.X, center.Y, center.Z, normal.X, normal.Y, normal.Z, RowerArt.RegionToRed(region(center, normal)), 0.5, 0.5,
                b0, b1, w0);
            for (var j = 0; j < sides; j++)
            {
                if (end)
                {
                    mesh.AddTriangle(ringFirst + j, c, ringFirst + j + 1);
                }
                else
                {
                    mesh.AddTriangle(ringFirst + j + 1, c, ringFirst + j);
                }
            }
        }

        private static void Interpolate(double[] keyT, double[] keyA, double[] keyB, double t, out double a, out double b)
        {
            for (var i = 1; i < keyT.Length; i++)
            {
                if (t <= keyT[i])
                {
                    var f = (t - keyT[i - 1]) / (keyT[i] - keyT[i - 1]);
                    a = keyA[i - 1] + (keyA[i] - keyA[i - 1]) * f;
                    b = keyB[i - 1] + (keyB[i] - keyB[i - 1]) * f;
                    return;
                }
            }

            a = keyA[keyA.Length - 1];
            b = keyB[keyB.Length - 1];
        }

        // Las regiones de un vértice, para las pruebas.
        public static IReadOnlyList<int> Regions(SkinnedMeshData mesh)
        {
            var list = new List<int>();
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                var region = SkinnedMeshData.RegionOf(mesh.Mesh.Colors[i * 4]);
                if (!list.Contains(region))
                {
                    list.Add(region);
                }
            }

            list.Sort();
            return list;
        }
    }
}
