using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Scene.Stroke;

using RowMate.Scene;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // It. 7 · Lo del HUD no tapa al remero (07-remero-palada/02-tech-design §2.11, §3.5, §6.3; AC-WKT-18-23, AC-WKT-17-14): con la
    // cámara real y los huesos medidos del rig (no la cuenta de Core), en las 10 poses, los rectángulos reales de los paneles de series
    // abiertos (TV y P360) y de la llave (L640, solo con el simulador) no se cruzan con la huella del cuerpo (cabeza, tronco, brazos y
    // manos con su grosor: 0,12 m y 0,06 m, como RowerFootprint).
    public sealed class RowerOcclusionTests : SceneTestBase
    {
        private static readonly RowerJoint[] Trunk =
        {
            RowerJoint.Hips, RowerJoint.Spine, RowerJoint.Spine1, RowerJoint.Spine2, RowerJoint.Neck, RowerJoint.Head, RowerJoint.HeadTop,
            RowerJoint.LeftShoulder, RowerJoint.RightShoulder,
        };

        private static readonly RowerJoint[] Arms =
        {
            RowerJoint.LeftArm, RowerJoint.LeftForeArm, RowerJoint.LeftHand, RowerJoint.LeftGrip, RowerJoint.RightArm, RowerJoint.RightForeArm,
            RowerJoint.RightHand, RowerJoint.RightGrip,
        };

        [UnityTest, Category("AC-WKT-18-23")]
        public IEnumerator TheOpenSeriesPanelsDoNotCoverTheRower([Values("TV", "P360")] string code)
        {
            var profile = code == "TV" ? DeviceProfile.TV : DeviceProfile.P360;
            CreateScene(profile, o =>
            {
                o.Routines = HostRoutines.Seeded(RoutineSeeds.F5);
                var preferences = new MemoryPreferences();
                preferences.SetBool(SeriesPanelPrefs.LapsKey, true);
                preferences.SetBool(SeriesPanelPrefs.RepsKey, true);
                o.Preferences = preferences;
            });
            yield return GoToWorkout();
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(250.2);
            var problems = new List<string>();
            foreach (var name in new[] { "hud-laps-panel", "hud-reps-panel" })
            {
                if (!H.IsShown(name))
                {
                    problems.Add(name + " no está a la vista");
                }
            }

            foreach (var c in RowerRigTests.Phases)
            {
                SceneHost.ForceCanonicalForTest(c);
                H.Step();
                yield return null;
                var body = Body(H, SceneHost);
                foreach (var name in new[] { "hud-laps-panel", "hud-reps-panel" })
                {
                    var panel = H.BoundsOf(name);
                    if (panel.Overlaps(body))
                    {
                        problems.Add(code + " c " + F(c) + ": " + name + " " + panel + " tapa el cuerpo " + body);
                    }
                }
            }

            SceneHost.ForceCanonicalForTest(null);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

#if ROWMATE_SIMULATOR
        [UnityTest, Category("AC-WKT-17-14")]
        public IEnumerator TheWrenchDoesNotCoverTheRowerInL640()
        {
            CreateScene(DeviceProfile.L640, o =>
            {
                o.Backend = HostBackends.Simulator(false);
                o.SimulatorLayer = SimulatorLayerMode.Full;
            });
            yield return EnterFree();
            yield return AdvanceTo(20.2);
            Assert.That(H.IsShown("sim-open"), Is.True, "la llave");
            var wrench = H.BoundsOf("sim-open");
            var problems = new List<string>();
            foreach (var c in RowerRigTests.Phases)
            {
                SceneHost.ForceCanonicalForTest(c);
                H.Step();
                yield return null;
                var body = Body(H, SceneHost);
                if (wrench.Overlaps(body))
                {
                    problems.Add("c " + F(c) + ": la llave " + wrench + " tapa el cuerpo " + body);
                }
            }

            SceneHost.ForceCanonicalForTest(null);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
#endif

        // La huella del cuerpo en dp de la ventana (arriba a la izquierda), con la cámara real y los huesos medidos.
        public static Rect Body(UiTestHost h, SceneHost host)
        {
            var joints = host.RowerJointsForTest ?? throw new InvalidOperationException("sin remero");
            var world = host.World!;
            var camera = world.Rig!.Camera;
            var focal = host.Frame.Window.Focal;
            float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
            void Add(RowerJoint joint, double thickness)
            {
                var p = world.Boat!.Pitch.TransformPoint(joints[(int)joint]);
                var v = camera.WorldToViewportPoint(p);
                if (v.z <= 0)
                {
                    return;
                }

                var x = v.x * (float)h.Profile.WidthDp;
                var y = (1 - v.y) * (float)h.Profile.HeightDp;
                var r = (float)(thickness * focal / v.z);
                left = Math.Min(left, x - r);
                right = Math.Max(right, x + r);
                top = Math.Min(top, y - r);
                bottom = Math.Max(bottom, y + r);
            }

            foreach (var joint in Trunk)
            {
                Add(joint, RowerFootprint.TrunkThickness);
            }

            foreach (var joint in Arms)
            {
                Add(joint, RowerFootprint.ArmThickness);
            }

            return Rect.MinMaxRect(left, top, right, bottom);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
