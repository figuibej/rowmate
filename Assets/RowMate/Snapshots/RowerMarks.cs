using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Las marcas y sondas del conjunto it7 (07-remero-palada/02-tech-design §5): los valores del remero por cuadro (el manifest de los
    // videos), la huella del cuerpo con la cámara real y los huesos medidos (contra los paneles, la llave y la zona del bote), la regla de
    // 1 m en el perno para la cámara lateral, los ángulos medidos en el rig contra los de Core y la hoja de 5 × 2 poses. Lo que se dibuja
    // encima de la toma (contornos, regla, hoja) es UI del arnés en la capa de arriba: nunca entra a la app.
    public static class RowerMarks
    {
        public const string RulerName = "snapshot-ruler";
        public const string SheetName = "snapshot-sheet";

        // p, φ, c, r, eventos, esfuerzo, surge, cabeceo, charcos y gotas vivos, relZ de la boya, vueltas del panel y desvío del horizonte.
        public static string Values(UiTestHost h, double t)
        {
            var scene = h.Scene;
            if (scene?.Session == null)
            {
                return "t " + F(t) + ": sin escena";
            }

            var f = scene.Frame;
            var r = f.Rower;
            var laps = SceneInputs.From(h.App.Workout.State.Value).Buoy.LapCount;
            var buoy = r.Buoy != null && r.Buoy.Visible ? F(r.Buoy.WorldZ - f.Distance) : "-";
            return "t " + F(t) + " s; p " + F(r.ContractPhase) + "; φ " + F(r.Phase) + "; c " + F(r.Canonical) + "; r " + F(r.Ratio) + "; ω " + F(r.PhaseSpeed)
                   + "; eventos " + r.Events + "; esfuerzo " + F(r.Effort) + "; surge " + F(r.Boat.SurgeM) + " m; cabeceo " + F(r.Boat.PitchRad * 180 / Math.PI)
                   + "°; respiración del remero " + F(r.IdleWeight) + "; charcos " + (r.Puddles?.Count ?? 0) + "; gotas " + (r.Spray?.Count ?? 0) + "; boya relZ " + buoy
                   + "; vueltas " + laps + "; sombra " + r.ShadowMode + " lado " + F(r.ShadowSide) + "; desvío del horizonte " + F(f.HorizonDeviationDp) + " dp; distancia "
                   + F(f.Distance) + " m";
        }

        // Las articulaciones medidas en el rig (marco del bote) como SceneVec3.
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

        // Tronco, rodilla, cadera, hombro y codo (izquierda).
        public static double[] Angles(SceneVec3[] joints)
        {
            return new[]
            {
                PoseAngles.TrunkDeg(joints), PoseAngles.KneeDeg(joints, true), PoseAngles.HipDeg(joints, true), PoseAngles.ShoulderDeg(joints, true),
                PoseAngles.ElbowDeg(joints, true),
            };
        }

        private static readonly string[] AngleNames = { "tronco", "rodilla", "cadera", "hombro", "codo" };

        // Los ángulos medidos en el rig contra los de Core (la hoja de A7-01) y los contactos: anota en values y devuelve los problemas
        // (± 8°, manos ≤ 2 cm de su puño).
        public static List<string> RigProbe(UiTestHost h, List<string> values)
        {
            var host = h.Scene!;
            var measured = Measured(host);
            var pose = host.Frame.Rower.Pose;
            var a = Angles(measured);
            var e = Angles(pose.Joints);
            var problems = new List<string>();
            var sb = new StringBuilder("rig (" + host.World!.Rower!.Source + ") c " + F(host.Frame.Rower.Canonical) + ":");
            for (var i = 0; i < a.Length; i++)
            {
                sb.Append(' ').Append(AngleNames[i]).Append(' ').Append(F(a[i])).Append("° (Core ").Append(F(e[i])).Append("°)");
                if (Math.Abs(a[i] - e[i]) > 8)
                {
                    problems.Add(AngleNames[i] + " medido " + F(a[i]) + "° contra " + F(e[i]) + "° de Core");
                }
            }

            var left = SceneVec3.Distance(measured[(int)RowerJoint.LeftGrip], pose.Left.Grip);
            var right = SceneVec3.Distance(measured[(int)RowerJoint.RightGrip], pose.Right.Grip);
            sb.Append("; manos a ").Append(F(left * 100)).Append(" / ").Append(F(right * 100)).Append(" cm de su puño; puño z ").Append(F(pose.Left.Grip.Z))
                .Append("; cadera z ").Append(F(measured[(int)RowerJoint.LeftUpLeg].Z)).Append("; talones z ").Append(F(measured[(int)RowerJoint.LeftFoot].Z))
                .Append("; centro de pala ").Append(F(pose.Left.CurveBladeY)).Append(" m; plumeo ").Append(F(pose.Left.FeatherDeg)).Append("°; barrido ")
                .Append(F(pose.Left.SweepDeg)).Append('°');
            if (left > 0.02 || right > 0.02)
            {
                problems.Add("una mano a más de 2 cm de su puño");
            }

            values.Add(sb.ToString());
            return problems;
        }

        // La huella del cuerpo en dp de la ventana (cabeza, tronco, brazos y manos con su grosor: 0,12 / 0,06 m; RowerFootprint).
        public static Rect Body(UiTestHost h)
        {
            var host = h.Scene!;
            return Project(h, host, true);
        }

        // El bote hasta los pernos y el remero con las piernas (la huella de la zona del bote).
        public static Rect BoatToPins(UiTestHost h)
        {
            return Project(h, h.Scene!, false);
        }

        private static Rect Project(UiTestHost h, SceneHost host, bool bodyOnly)
        {
            var joints = host.RowerJointsForTest ?? throw new InvalidOperationException("sin remero");
            var world = host.World!;
            var camera = world.Rig!.Camera;
            var focal = host.Frame.Window.Focal;
            var pitch = world.Boat!.Pitch;
            float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
            void Add(Vector3 local, double thickness)
            {
                var v = camera.WorldToViewportPoint(pitch.TransformPoint(local));
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

            for (var j = 0; j < joints.Length; j++)
            {
                var joint = (RowerJoint)j;
                var leg = joint >= RowerJoint.LeftUpLeg;
                if (bodyOnly && leg)
                {
                    continue;
                }

                var arm = joint >= RowerJoint.LeftArm && joint <= RowerJoint.RightGrip && joint != RowerJoint.RightShoulder;
                Add(joints[j], arm ? RowerFootprint.ArmThickness : RowerFootprint.TrunkThickness);
            }

            if (!bodyOnly)
            {
                var boat = BoatSpec.Real;
                Add(new Vector3((float)boat.PinX, (float)boat.PinY, 0), 0.02);
                Add(new Vector3((float)-boat.PinX, (float)boat.PinY, 0), 0.02);
                Add(new Vector3(0, 0.1f, (float)RowerFootprint.HullNearZ), 0.15);
            }

            return Rect.MinMaxRect(left, top, right, bottom);
        }

        // Un contorno de 1 dp (las tomas "con el contorno").
        public static void Outline(UiTestHost h, string name, Rect rect, ColorToken color)
        {
            var box = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.left = rect.xMin;
            box.style.top = rect.yMin;
            box.style.width = rect.width;
            box.style.height = rect.height;
            var c = RowMate.Presentation.Controls.TokenColors.ToColor(color);
            box.style.borderLeftColor = c;
            box.style.borderRightColor = c;
            box.style.borderTopColor = c;
            box.style.borderBottomColor = c;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            h.Root.Add(box);
        }

        // La regla de 1 m en el perno de la izquierda de la cámara lateral (x −0,80): de la altura del perno a 1 m más arriba, con la
        // cámara del cuadro (SceneHost.ShowRulerForTest).
        public static void Ruler(UiTestHost h)
        {
            var host = h.Scene!;
            var old = h.Root.Q(RulerName);
            old?.RemoveFromHierarchy();
            if (!host.ShowRulerForTest)
            {
                return;
            }

            var world = host.World!;
            var camera = world.Rig!.Camera;
            var boat = BoatSpec.Real;
            var baseWorld = world.Boat!.Root.TransformPoint(new Vector3((float)-boat.PinX, 0, 0));
            var a = camera.WorldToViewportPoint(baseWorld);
            var b = camera.WorldToViewportPoint(baseWorld + Vector3.up);
            var x = a.x * (float)h.Profile.WidthDp;
            var y0 = (1 - a.y) * (float)h.Profile.HeightDp;
            var y1 = (1 - b.y) * (float)h.Profile.HeightDp;
            var bar = new VisualElement { name = RulerName, pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = x - 1;
            bar.style.width = 2;
            bar.style.top = Math.Min(y0, y1);
            bar.style.height = Math.Abs(y0 - y1);
            bar.style.backgroundColor = RowMate.Presentation.Controls.TokenColors.ToColor(Tokens.DevSimulator);
            for (var i = 0; i <= 10; i++)
            {
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.style.position = Position.Absolute;
                tick.style.left = 0;
                tick.style.width = i % 5 == 0 ? 10 : 6;
                tick.style.height = 1;
                tick.style.top = Math.Abs(y0 - y1) * i / 10f;
                tick.style.backgroundColor = RowMate.Presentation.Controls.TokenColors.ToColor(Tokens.DevSimulator);
                bar.Add(tick);
            }

            h.Root.Add(bar);
        }

        // La hoja de poses: las tomas en una grilla de `columns` × `rows` (escaladas), centrada, sobre negro, como un elemento de UI que tapa
        // la ventana (la toma de la hoja es lo que se ve). Devuelve la textura de la hoja (la destruye quien llama con RemoveSheet).
        public static Texture2D Sheet(UiTestHost h, IReadOnlyList<Texture2D> shots, int columns, int rows)
        {
            var width = h.Profile.WidthPx;
            var height = h.Profile.HeightPx;
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = SheetName, hideFlags = HideFlags.HideAndDontSave };
            var black = new Color32[width * height];
            for (var i = 0; i < black.Length; i++)
            {
                black[i] = Color.black;
            }

            sheet.SetPixels32(black);
            var cellW = width / columns;
            var scale = Math.Min((float)cellW / Math.Max(1, shots[0].width), (float)(height / rows) / Math.Max(1, shots[0].height));
            var cellH = (int)(shots[0].height * scale);
            cellW = (int)(shots[0].width * scale);
            var offsetY = (height - rows * cellH) / 2;
            var offsetX = (width - columns * cellW) / 2;
            for (var k = 0; k < shots.Count && k < columns * rows; k++)
            {
                var col = k % columns;
                var row = k / columns;
                var shot = shots[k];
                for (var y = 0; y < cellH; y++)
                {
                    for (var x = 0; x < cellW; x++)
                    {
                        var c = shot.GetPixel((int)(x / scale), (int)(y / scale));
                        // Las filas de la textura van de abajo arriba: la fila 0 de la hoja queda arriba.
                        sheet.SetPixel(offsetX + col * cellW + x, height - offsetY - (row + 1) * cellH + y, c);
                    }
                }
            }

            sheet.Apply(false, false);
            var element = new VisualElement { name = SheetName, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            element.style.backgroundImage = new StyleBackground(sheet);
            h.Root.Add(element);
            return sheet;
        }

        public static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
