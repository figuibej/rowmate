using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // It. 7 · El rig sigue a Core (07-remero-palada/02-tech-design §3.2, §6.3; DT7-01; AC-SCN-05-06, -08, -09): en las 10 poses de la
    // tabla (fase forzada), las articulaciones medidas en los huesos después de Apply quedan a ≤ 2 cm de las de Core, las manos a ≤ 2 cm
    // de su puño, y los ángulos medidos en el rig (tronco, rodilla, cadera, hombro, codo) a ± 8° de los de Core. Con el maniquí siempre;
    // con Y Bot si la máquina lo tiene (si no, NO VERIFICABLE). La hoja medida queda en el registro ("[RIG] …").
    public sealed class RowerRigTests : SceneTestBase
    {
        public static readonly double[] Phases = { 0, 0.09, 0.18, 0.24, 0.36, 0.395, 0.6, 0.75, 0.85, 0.96 };

        private static readonly RowerJoint[] Contacts =
        {
            RowerJoint.Hips, RowerJoint.Spine2, RowerJoint.Neck, RowerJoint.Head, RowerJoint.LeftArm, RowerJoint.RightArm, RowerJoint.LeftForeArm,
            RowerJoint.RightForeArm, RowerJoint.LeftHand, RowerJoint.RightHand, RowerJoint.LeftUpLeg, RowerJoint.RightUpLeg, RowerJoint.LeftLeg,
            RowerJoint.RightLeg, RowerJoint.LeftFoot, RowerJoint.RightFoot,
        };

        [UnityTest, Category("AC-SCN-05-08"), Category("AC-SCN-05-09"), Category("AC-SCN-05-06")]
        public IEnumerator TheRigFollowsCoreInTheTenPoses([Values(RowerCharacterKind.Mannequin, RowerCharacterKind.Slot)] RowerCharacterKind kind)
        {
            CreateScene(DeviceProfile.TV, o => o.SceneCharacter = kind);
            yield return EnterFree();
            var host = SceneHost;
            if (kind == RowerCharacterKind.Slot && !host.World!.Rower!.FromSlot)
            {
                Assert.Ignore("NO VERIFICABLE: esta máquina no tiene el FBX de Mixamo (sin binding)");
            }

            var problems = new List<string>();
            var sheet = new StringBuilder("[RIG] " + host.World!.Rower!.Source + " (c; tronco, rodilla, cadera, hombro, codo medidos; Δ máx. de articulación cm; manos cm)\n");
            foreach (var c in Phases)
            {
                host.ForceCanonicalForTest(c);
                H.Step();
                H.Step();
                yield return null;
                var core = host.Frame.Rower.Pose.Joints;
                var measured = Measured(host);
                var worst = 0.0;
                var worstJoint = "";
                foreach (var joint in Contacts)
                {
                    var d = SceneVec3.Distance(measured[(int)joint], core[(int)joint]);
                    if (d > worst)
                    {
                        worst = d;
                        worstJoint = joint.ToString();
                    }
                }

                var hands = Math.Max(SceneVec3.Distance(measured[(int)RowerJoint.LeftGrip], host.Frame.Rower.Pose.Left.Grip),
                    SceneVec3.Distance(measured[(int)RowerJoint.RightGrip], host.Frame.Rower.Pose.Right.Grip));
                var angles = Angles(measured);
                var expected = Angles(core);
                sheet.Append(F(c)).Append(": ");
                for (var i = 0; i < angles.Length; i++)
                {
                    sheet.Append(F(angles[i])).Append(i < angles.Length - 1 ? ", " : "");
                    if (Math.Abs(angles[i] - expected[i]) > 8)
                    {
                        problems.Add("c " + F(c) + ": ángulo " + i + " medido " + F(angles[i]) + "° contra Core " + F(expected[i]) + "°");
                    }
                }

                sheet.Append("; Δ ").Append(F(worst * 100)).Append(" (").Append(worstJoint).Append("); manos ").Append(F(hands * 100)).Append('\n');
                // El maniquí tiene los largos de Core: 2 cm. Y Bot: las articulaciones de su rig no son las de la tabla (el hueso Hips, la
                // clavícula), los extremos sí: 3 cm.
                var tolerance = kind == RowerCharacterKind.Mannequin ? 0.02 : 0.03;
                if (worst > tolerance)
                {
                    problems.Add("c " + F(c) + ": " + worstJoint + " a " + F(worst * 100) + " cm de Core");
                }

                if (hands > 0.02)
                {
                    problems.Add("c " + F(c) + ": la mano a " + F(hands * 100) + " cm de su puño");
                }
            }

            host.ForceCanonicalForTest(null);
            Debug.Log(sheet.ToString());
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // Las articulaciones medidas (marco del bote) como SceneVec3.
        public static SceneVec3[] Measured(SceneHost host)
        {
            var joints = host.RowerJointsForTest ?? throw new InvalidOperationException("sin remero");
            var result = new SceneVec3[joints.Length];
            for (var i = 0; i < joints.Length; i++)
            {
                result[i] = new SceneVec3(joints[i].x, joints[i].y, joints[i].z);
            }

            return result;
        }

        // Tronco, rodilla, cadera, hombro y codo (los de la izquierda): PoseAngles de Core.
        public static double[] Angles(SceneVec3[] joints)
        {
            return new[]
            {
                PoseAngles.TrunkDeg(joints), PoseAngles.KneeDeg(joints, true), PoseAngles.HipDeg(joints, true), PoseAngles.ShoulderDeg(joints, true),
                PoseAngles.ElbowDeg(joints, true),
            };
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
