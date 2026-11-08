using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Las marcas y las sondas del conjunto it8 (08-entornos-selector/02-tech-design §6; solo el arnés):
    // - Marks (CAP-44 a -46): las marcas del legacy de SceneMarks (la base de cada objeto de la lista del cuadro, con la cámara del
    //   contrato) y la x del ancla 3D con la cámara real (± 2 dp, AC-SCN-14-10); además el desnivel de la base (PropPlacement.FootY:
    //   ≤ 2,5 m; en el río, el muro de 1,2 m).
    // - NightProbes (issue #9, QA-V6a-08): en la pasada SIN post, la caja de cada objeto encendido más cercano (cabaña, edificio, club,
    //   farol, faro, torre) proyectada con la cámara real; dentro de la caja, el píxel más parecido al color de la ventana encendida
    //   (#FFE082 ± 3) y la mediana del frente (luminancia, como dato).
    public static class EnvironmentMarks
    {
        public const double MaxFootRise = 2.5;
        public const double RiverWallRise = 1.2;
        public const int WindowTolerance = 3;

        // El color de la ventana encendida (LakeArt.WindowNight = RiverArt.WindowLit); el globo del farol (#FFF8E1) y el halo del faro
        // son del Glow de WP-D1 (B8-05): hoy el farol y el faro se encienden con el material de ventana, así que la sonda es la misma.
        private const string WindowHex = "FFE082";

        // Las ventanas del skyline del río (RiverArt.SkylineWindow, al 85 % sobre la silueta).
        private const string SkylineHex = "FFE08A";
        private const int SkylineTolerance = 40;

        public static IEnumerable<string> Marks(UiTestHost host, Texture2D frame, List<string> values, EnvironmentId env)
        {
            var problems = new List<string>();
            var scene = host.Scene;
            if (scene == null)
            {
                problems.Add("marcas: sin escena");
                return problems;
            }

            if (scene.KitEnvironment != env)
            {
                problems.Add("el kit armado es " + scene.KitEnvironment + ", no " + env);
            }

            var worst = SceneMarks.Marks(host, frame, values);
            if (double.IsNaN(worst) || worst > SceneMarks.MarkToleranceDp)
            {
                problems.Add("marcas: |Δx| " + F(worst) + " dp > " + F(SceneMarks.MarkToleranceDp));
            }

            var props = scene.Frame.VisibleProps.Items;
            var rise = 0.0;
            var kind = "";
            for (var i = 0; i < props.Length; i++)
            {
                var y = PropPlacement.FootY(env, props[i]);
                if (y > rise)
                {
                    rise = y;
                    kind = props[i].Kind.ToString();
                }
            }

            var cap = MaxFootRise;
            values.Add("bases: " + props.Length + " objetos; desnivel máximo " + F(rise) + " m (" + (kind.Length == 0 ? "—" : kind) + "; tope " + F(cap)
                       + (env == EnvironmentId.River ? ", muro " + F(RiverWallRise) : "") + ")");
            if (rise > cap + 1e-6)
            {
                problems.Add("la base de " + kind + " sube " + F(rise) + " m (tope " + F(cap) + ")");
            }

            return problems;
        }

        // Las sondas de las luces de la noche: una por tipo encendido a la vista (el más cercano entre 8 y 160 m).
        public static List<ProbeRecord> NightProbes(UiTestHost host, List<string> values)
        {
            var records = new List<ProbeRecord>();
            var scene = host.Scene;
            var output = scene?.Target.Texture;
            var camera = scene?.World?.Rig?.Camera;
            if (scene == null || output == null || camera == null)
            {
                values.Add("luces de noche: sin escena");
                return records;
            }

            var f = scene.Frame;
            var props = f.VisibleProps.Items;
            var chosen = new Dictionary<PropKind, int>();
            for (var i = 0; i < props.Length; i++)
            {
                var relZ = props[i].Z - f.Distance;
                if (f.PropLight[i] < 0.99 || relZ < 8 || relZ > 160 || !NightLights.TryKindOf(props[i], out _, out _))
                {
                    continue;
                }

                if (!chosen.TryGetValue(props[i].Kind, out var best) || relZ < props[best].Z - f.Distance)
                {
                    chosen[props[i].Kind] = i;
                }
            }

            if (chosen.Count == 0)
            {
                values.Add("luces de noche: ningún objeto encendido entre 8 y 160 m");
                return records;
            }

            var rt = new RenderTexture(output.descriptor) { name = "RowMate.NightProbes", hideFlags = HideFlags.HideAndDontSave };
            rt.Create();
            Texture2D? image = null;
            try
            {
                // Sin el halo aditivo del Glow: la sonda mide el color emisivo de la ventana, del farol y de la linterna del faro.
                var glow = scene.World?.Director?.Glow;
                if (glow != null)
                {
                    glow.SuppressDrawForTest = true;
                }

                try
                {
                    scene.RenderWithoutPostForTest(rt);
                }
                finally
                {
                    if (glow != null)
                    {
                        glow.SuppressDrawForTest = false;
                    }
                }

                image = RtReader.Read(rt);
                var kinds = new List<PropKind>(chosen.Keys);
                kinds.Sort();
                foreach (var kind in kinds)
                {
                    var p = props[chosen[kind]];
                    var relZ = p.Z - f.Distance;
                    if (!TryBox(camera, image, p, relZ, out var box))
                    {
                        continue;
                    }

                    ColorUtility.TryParseHtmlString("#" + WindowHex, out var expected);
                    Color32 e = expected;
                    var match = BestMatch(image, box, e, out var diff);
                    var front = Median(image, box);
                    var name = "luz " + kind + " (x " + F(p.X) + "; relZ " + F(relZ) + ")";
                    records.Add(new ProbeRecord
                    {
                        Name = name + ": ventana",
                        Element = "scene-surface",
                        Expected = "#" + WindowHex,
                        Actual = "#" + ColorUtility.ToHtmlStringRGB(match),
                        Difference = diff,
                        Tolerance = WindowTolerance,
                        // El farol, el faro y la torre: de aspecto (no bloquean) hasta el Glow de WP-D1 (B8-05); su linterna es chica o
                        // queda cortada arriba en TH. Las ventanas (cabaña, edificio, club) son de contrato.
                        Aspect = kind == PropKind.Lamp || kind == PropKind.Lighthouse || kind == PropKind.FinishTower,
                        Pass = diff <= WindowTolerance,
                    });
                    values.Add(name + ": ventana #" + ColorUtility.ToHtmlStringRGB(match) + " contra #" + WindowHex + " ± " + WindowTolerance + " (diferencia " + diff
                               + "); frente #" + ColorUtility.ToHtmlStringRGB(front) + " (luminancia " + F(Luminance(front)) + "); caja " + box.width + " × " + box.height
                               + " px; luz " + F(f.PropLight[chosen[kind]]));
                }

                // Issue #9: las ventanas del skyline del río (#FFE08A al 85 % sobre el tono de la silueta, de noche): la franja de arriba del
                // horizonte (el 15 % del alto) a todo el ancho; el píxel más parecido. De aspecto: el 15 % del tono la corre del color puro.
                if (scene.KitEnvironment == EnvironmentId.River && f.Palette.Ambient < 0.6)
                {
                    var forward = camera.transform.forward;
                    forward.y = 0;
                    var horizon = camera.WorldToViewportPoint(camera.transform.position + forward.normalized * 5000f);
                    var row = (int)Math.Round((1 - horizon.y) * image.height);
                    var band = Math.Max(4, image.height * 15 / 100);
                    var top = Math.Max(0, row - band);
                    var height = Math.Min(image.height, row) - top;
                    if (horizon.z > 0 && height > 2)
                    {
                        var box = new RectInt(0, top, image.width, height);
                        ColorUtility.TryParseHtmlString("#" + SkylineHex, out var skyline);
                        var match = BestMatch(image, box, skyline, out var diff);
                        records.Add(new ProbeRecord
                        {
                            Name = "luz skyline: ventana",
                            Element = "scene-surface",
                            Expected = "#" + SkylineHex,
                            Actual = "#" + ColorUtility.ToHtmlStringRGB(match),
                            Difference = diff,
                            Tolerance = SkylineTolerance,
                            Aspect = true,
                            Pass = diff <= SkylineTolerance,
                        });
                        values.Add("luz skyline: ventana #" + ColorUtility.ToHtmlStringRGB(match) + " contra #" + SkylineHex + " ± " + SkylineTolerance + " (diferencia " + diff
                                   + "); franja " + box.width + " × " + box.height + " px");
                    }
                }
            }
            finally
            {
                if (image != null)
                {
                    Object.DestroyImmediate(image);
                }

                rt.Release();
                Object.DestroyImmediate(rt);
            }

            return records;
        }

        // La caja del objeto (ancho, alto y fondo aproximados por tipo, con su escala; el volumen crece hacia +z desde el ancla, DT8-19
        // R1) proyectada con la cámara real: px desde arriba.
        private static bool TryBox(Camera camera, Texture2D image, in ShoreProp p, double relZ, out RectInt box)
        {
            box = default;
            Size(p, out var half, out var height, out var depth);
            // De −1 m (el agua) a la altura del tipo más 3 m (el muro del río, la meseta de la costa, el promontorio del faro).
            var y0 = -1.0;
            height += 3;
            var xMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMin = double.PositiveInfinity;
            var yMax = double.NegativeInfinity;
            for (var c = 0; c < 8; c++)
            {
                var wx = p.X + ((c & 1) == 0 ? -half : half);
                var wy = y0 + ((c & 2) == 0 ? 0 : height);
                var wz = relZ + ((c & 4) == 0 ? -1 : depth);
                var v = camera.WorldToViewportPoint(new Vector3((float)wx, (float)wy, (float)wz));
                if (v.z <= 0)
                {
                    return false;
                }

                xMin = Math.Min(xMin, v.x);
                xMax = Math.Max(xMax, v.x);
                yMin = Math.Min(yMin, 1 - v.y);
                yMax = Math.Max(yMax, 1 - v.y);
            }

            var x0 = (int)Math.Floor(Math.Max(0, xMin) * image.width);
            var x1 = (int)Math.Ceiling(Math.Min(1, xMax) * image.width);
            var top = (int)Math.Floor(Math.Max(0, yMin) * image.height);
            var bottom = (int)Math.Ceiling(Math.Min(1, yMax) * image.height);
            if (x1 - x0 < 2 || bottom - top < 2)
            {
                return false;
            }

            box = new RectInt(x0, top, x1 - x0, bottom - top);
            return true;
        }

        private static void Size(in ShoreProp p, out double half, out double height, out double depth)
        {
            var s = p.Scale > 0 ? p.Scale : 1;
            switch (p.Kind)
            {
                case PropKind.Cabin:
                    half = 3.5 * s;
                    height = 5 * s;
                    depth = 6 * s;
                    break;
                case PropKind.Building:
                    half = 6 * s;
                    height = 22 * s;
                    depth = 10 * s;
                    break;
                case PropKind.Boathouse:
                    half = 8 * s;
                    height = 9 * s;
                    depth = 12 * s;
                    break;
                case PropKind.Lamp:
                    half = 0.8;
                    height = 5;
                    depth = 0.8;
                    break;
                case PropKind.Lighthouse:
                    half = 3;
                    height = 14;
                    depth = 3;
                    break;
                default:
                    half = 3 * s;
                    height = 14 * s;
                    depth = 3 * s;
                    break;
            }
        }

        private static Color32 BestMatch(Texture2D image, RectInt box, Color32 e, out int best)
        {
            best = int.MaxValue;
            var found = default(Color32);
            for (var y = box.y; y < box.y + box.height; y++)
            {
                for (var x = box.x; x < box.x + box.width; x++)
                {
                    var c = RtReader.PixelTopLeft(image, x, y);
                    var d = Math.Max(Math.Abs(c.r - e.r), Math.Max(Math.Abs(c.g - e.g), Math.Abs(c.b - e.b)));
                    if (d < best)
                    {
                        best = d;
                        found = c;
                    }
                }
            }

            return found;
        }

        // La mediana por canal de la mitad de abajo del centro de la caja (el frente: pared y puerta).
        private static Color32 Median(Texture2D image, RectInt box)
        {
            var r = new List<byte>();
            var g = new List<byte>();
            var b = new List<byte>();
            var x0 = box.x + box.width / 4;
            var x1 = box.x + box.width * 3 / 4;
            var y0 = box.y + box.height / 2;
            var y1 = box.y + box.height * 7 / 8;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var c = RtReader.PixelTopLeft(image, x, y);
                    r.Add(c.r);
                    g.Add(c.g);
                    b.Add(c.b);
                }
            }

            if (r.Count == 0)
            {
                return default(Color32);
            }

            r.Sort();
            g.Sort();
            b.Sort();
            return new Color32(r[r.Count / 2], g[g.Count / 2], b[b.Count / 2], 255);
        }

        private static double Luminance(Color32 c)
        {
            return (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
        }

        private static string F(double value)
        {
            return double.IsNaN(value) ? "—" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
