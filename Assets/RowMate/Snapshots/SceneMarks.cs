using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Scene;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Las capas de depuración y las sondas del conjunto it6a (06a-escena-lago/02-tech-design §5.1, §5.2; solo el arnés):
    // - Marks: una marca de 4 dp en la base de cada objeto del legacy (la lista de ShoreSegments del cuadro, la misma que el oráculo,
    //   proyectada con SceneCamera.TryProject(x; 0; relZ), como el legacy dibuja la marca), pintada encima de la imagen leída; y en el
    //   manifest la diferencia en x entre cada marca y el ancla 3D del objeto proyectada por la CÁMARA REAL (WorldToViewportPoint × el
    //   tamaño de la ventana): la sonda de ± 2 dp (AC-SCN-15-05).
    // - Disk: el disco del astro: dónde lo pone la cámara real para su dirección y el centro de lo brillante a su alrededor.
    // - Clouds y Stars: las 7 nubes contra su punto del legacy y las estrellas a la vista.
    // - Still: dos dibujos seguidos de la escena con el bote quieto (sin temblor: 0 píxeles distintos).
    // - Values: los valores de la escena del cuadro (para el manifest de las fotos y de cada cuadro de los videos).
    public static class SceneMarks
    {
        public const double MarkToleranceDp = 2;
        // El naranja de la cinta del simulador (dev.simulator): un color de depuración, como el contorno de la zona del bote.
        private static readonly Color32 MarkColor = RowMate.Presentation.Controls.TokenColors.ToColor(RowMate.Core.Design.Tokens.DevSimulator);

        // Las marcas del legacy y la sonda de ± 2 dp. Devuelve el peor |Δx| (dp) y escribe la línea del manifest.
        public static double Marks(UiTestHost host, Texture2D frame, List<string> values)
        {
            var scene = host.Scene;
            if (scene?.World?.Rig == null || scene.World.Director == null)
            {
                values.Add("marcas: sin escena");
                return double.NaN;
            }

            var camera = scene.World.Rig.Camera;
            var w = host.Profile.WidthDp;
            var h = host.Profile.HeightDp;
            var cam = new SceneCamera(w, h);
            var d = scene.Frame.Distance;
            var worst = 0.0;
            var count = 0;
            var density = host.Profile.Density;
            var size = (int)Math.Max(2, Math.Round(4 * density));
            var props = scene.Frame.VisibleProps;
            for (var i = 0; i < props.Count; i++)
            {
                var p = props[i];
                var relZ = p.Z - d;
                if (!cam.TryProject(p.X, 0, relZ, out var lx, out var ly))
                {
                    continue;
                }

                if (!scene.World.Director.Shore.TryAnchor(p, out var ax, out var ay, out _))
                {
                    continue;
                }

                var v = camera.WorldToViewportPoint(new Vector3((float)ax, (float)ay, (float)relZ));
                if (v.z <= 0)
                {
                    continue;
                }

                var dx = Math.Abs(v.x * w - lx);
                worst = Math.Max(worst, dx);
                count++;
                Paint(frame, lx * density, ly * density, size);
            }

            frame.Apply(false);
            values.Add("marcas: " + I(count) + " objetos del legacy; máx |Δx| entre la marca y el ancla 3D = " + F(worst) + " dp (tope "
                       + F(MarkToleranceDp) + ")");
            return worst;
        }

        // El astro: dónde lo pone la cámara real (dp, arriba a la izquierda) y el centro de lo brillante a su alrededor.
        public static (double X, double Y, double Error) Disk(UiTestHost host, Texture2D frame, List<string> values)
        {
            var scene = host.Scene;
            if (scene?.World?.Rig == null)
            {
                values.Add("astro: sin escena");
                return (double.NaN, double.NaN, double.NaN);
            }

            var sky = scene.Frame.Celestial;
            SceneMath.Direction(sky.DiskAzimuthDeg, sky.DiskElevationDeg, out var x, out var y, out var z);
            var (ex, ey) = Project(host, x, y, z);
            var density = host.Profile.Density;
            var radiusDp = scene.Frame.DiskRadiusRad * scene.Frame.Window.Focal;
            double sx = 0, sy = 0, n = 0;
            var box = Math.Max(6, radiusDp * 1.6);
            for (var py = (int)((ey - box) * density); py <= (int)((ey + box) * density); py++)
            {
                for (var px = (int)((ex - box) * density); px <= (int)((ex + box) * density); px++)
                {
                    if (px < 0 || py < 0 || px >= frame.width || py >= frame.height)
                    {
                        continue;
                    }

                    var c = RtReader.PixelTopLeft(frame, px, py);
                    if (Luminance(c) > 0.8)
                    {
                        sx += px;
                        sy += py;
                        n++;
                    }
                }
            }

            var visible = n > 0;
            var cx = visible ? sx / n / density : double.NaN;
            var cy = visible ? sy / n / density : double.NaN;
            var error = visible ? Math.Sqrt((cx - ex) * (cx - ex) + (cy - ey) * (cy - ey)) : double.NaN;
            values.Add("astro: " + (sky.DiskIsMoon ? "luna" : "sol") + " acimut " + F(sky.DiskAzimuthDeg) + "°, elevación " + F(sky.DiskElevationDeg)
                       + "°, visibilidad " + F(sky.DiskVisibility) + "; la cámara real lo pone en (" + F(ex) + "; " + F(ey) + ") dp, radio " + F(radiusDp)
                       + " dp" + (sky.DiskIsMoon ? ", iluminada " + F(scene.Frame.Moon.Illumination) : "") + "; "
                       + (visible ? "lo brillante centrado en (" + F(cx) + "; " + F(cy) + ") dp, a " + F(error) + " dp" : "sin píxeles brillantes (tapado o de noche)"));
            return (ex, ey, error);
        }

        // El brillo del sol (DN6a-21, AC-SCN-11-05): el ancho (dp) de lo que pasa de 0,9 de luminancia en la fila del centro del disco.
        public static double Glow(UiTestHost host, Texture2D frame, List<string> values)
        {
            var scene = host.Scene;
            if (scene?.World?.Rig == null)
            {
                return double.NaN;
            }

            var sky = scene.Frame.Celestial;
            SceneMath.Direction(sky.DiskAzimuthDeg, sky.DiskElevationDeg, out var x, out var y, out var z);
            var (ex, ey) = Project(host, x, y, z);
            var density = host.Profile.Density;
            var row = (int)Math.Round(ey * density);
            var center = (int)Math.Round(ex * density);
            if (row < 0 || row >= frame.height || center < 0 || center >= frame.width)
            {
                values.Add("brillo: el sol fuera de la imagen");
                return double.NaN;
            }

            var left = center;
            while (left > 0 && Luminance(RtReader.PixelTopLeft(frame, left - 1, row)) > 0.9)
            {
                left--;
            }

            var right = center;
            while (right < frame.width - 1 && Luminance(RtReader.PixelTopLeft(frame, right + 1, row)) > 0.9)
            {
                right++;
            }

            var diameter = Luminance(RtReader.PixelTopLeft(frame, center, row)) > 0.9 ? (right - left + 1) / density : 0;
            values.Add("brillo: diámetro con luminancia > 0,9 en la fila del centro del sol = " + F(diameter) + " dp (mínimo 30)");
            return diameter;
        }

        // Las 7 nubes: la cámara real contra el punto de la pantalla del legacy (± 1 dp). Devuelve el peor error.
        public static double Clouds(UiTestHost host, List<string> values)
        {
            var scene = host.Scene;
            var clouds = scene?.Frame.Clouds;
            if (scene?.World?.Rig == null || clouds == null)
            {
                return double.NaN;
            }

            var worst = 0.0;
            var shown = 0;
            for (var i = 0; i < clouds.Length; i++)
            {
                var c = clouds[i];
                if (c.BodyAlpha <= 0)
                {
                    continue;
                }

                var (px, py) = Project(host, c.DirX, c.DirY, c.DirZ);
                worst = Math.Max(worst, Math.Max(Math.Abs(px - c.ScreenX), Math.Abs(py - c.ScreenY)));
                shown++;
            }

            values.Add("nubes: " + I(shown) + " a la vista; máx error contra el punto del legacy = " + F(worst) + " dp");
            return worst;
        }

        // Las estrellas con alfa > 0 (y cuántas caen adentro del disco del astro).
        public static int Stars(UiTestHost host, List<string> values)
        {
            var scene = host.Scene;
            var stars = scene?.Frame.Stars;
            if (scene == null || stars == null)
            {
                return -1;
            }

            var sky = scene.Frame.Celestial;
            SceneMath.Direction(sky.DiskAzimuthDeg, sky.DiskElevationDeg, out var dx, out var dy, out var dz);
            var radius = scene.Frame.DiskRadiusRad;
            var visible = 0;
            var inside = 0;
            for (var i = 0; i < stars.Length; i++)
            {
                if (stars[i].Alpha <= 0)
                {
                    continue;
                }

                visible++;
                var dot = stars[i].DirX * dx + stars[i].DirY * dy + stars[i].DirZ * dz;
                if (Math.Acos(Math.Max(-1, Math.Min(1, dot))) < radius)
                {
                    inside++;
                }
            }

            values.Add("estrellas: " + I(visible) + " con alfa > 0 (fundido " + F(scene.Frame.StarFade) + "), " + I(inside) + " adentro del disco");
            return visible;
        }

        // Sin temblor: la escena se dibuja otra vez sin que pase el tiempo (el bote quieto) y se cuentan los píxeles distintos.
        public static int Still(UiTestHost host, List<string> values)
        {
            var rt = host.Scene?.Target.Texture;
            if (rt == null)
            {
                return -1;
            }

            var a = RtReader.Read(rt);
            host.SceneTick();
            var b = RtReader.Read(rt);
            try
            {
                var pa = a.GetPixels32();
                var pb = b.GetPixels32();
                var differing = 0;
                for (var i = 0; i < pa.Length; i++)
                {
                    if (pa[i].r != pb[i].r || pa[i].g != pb[i].g || pa[i].b != pb[i].b)
                    {
                        differing++;
                    }
                }

                values.Add("sin temblor: dos dibujos seguidos con el bote quieto, " + I(differing) + " píxeles distintos de " + I(pa.Length));
                return differing;
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        // Las sondas de contrato (04-design-notes §10.4; QA-G6aT-13): el mismo cuadro dibujado sin el post de la cámara y con lo emisivo en 1
        // (RenderWithoutPostForTest, la "pasada de contrato"), la mediana de un parche de 5 × 5 dp en cada punto, contra su hex ± tol por
        // canal. No se publica como imagen: los valores van al manifest. Devuelve los problemas.
        public static List<string> Contract(UiTestHost host, List<string> values, params (string Name, double X, double Y, string Hex, int Tolerance)[] probes)
        {
            var problems = new List<string>();
            var scene = host.Scene;
            var output = scene?.Target.Texture;
            if (scene == null || output == null)
            {
                problems.Add("contrato: sin escena");
                return problems;
            }

            var rt = new RenderTexture(output.descriptor) { name = "RowMate.SceneContract", hideFlags = HideFlags.HideAndDontSave };
            rt.Create();
            Texture2D? frame = null;
            try
            {
                scene.RenderWithoutPostForTest(rt);
                frame = RtReader.Read(rt);
                var scale = rt.width / host.Profile.WidthDp;
                foreach (var probe in probes)
                {
                    var c = Median(frame, probe.X * scale, probe.Y * scale, Math.Max(1, (int)Math.Round(5 * scale)));
                    ColorUtility.TryParseHtmlString("#" + probe.Hex, out var expected);
                    Color32 e = expected;
                    var diff = Math.Max(Math.Abs(c.r - e.r), Math.Max(Math.Abs(c.g - e.g), Math.Abs(c.b - e.b)));
                    var line = "contrato " + probe.Name + " (" + F(probe.X) + "; " + F(probe.Y) + "): #" + ColorUtility.ToHtmlStringRGB(c) + " contra #"
                               + probe.Hex + " ± " + I(probe.Tolerance) + " (diferencia " + I(diff) + ")";
                    values.Add(line);
                    if (diff > probe.Tolerance)
                    {
                        problems.Add(line);
                    }
                }
            }
            finally
            {
                if (frame != null)
                {
                    Object.DestroyImmediate(frame);
                }

                rt.Release();
                Object.DestroyImmediate(rt);
            }

            return problems;
        }

        // La mediana por canal de un parche cuadrado de lado `size` px centrado en (x; y) (px desde arriba a la izquierda).
        private static Color32 Median(Texture2D frame, double x, double y, int size)
        {
            var r = new List<byte>();
            var g = new List<byte>();
            var b = new List<byte>();
            var x0 = (int)Math.Round(x) - size / 2;
            var y0 = (int)Math.Round(y) - size / 2;
            for (var py = y0; py < y0 + size; py++)
            {
                for (var px = x0; px < x0 + size; px++)
                {
                    var c = RtReader.PixelTopLeft(frame, px, py);
                    r.Add(c.r);
                    g.Add(c.g);
                    b.Add(c.b);
                }
            }

            r.Sort();
            g.Sort();
            b.Sort();
            return new Color32(r[r.Count / 2], g[g.Count / 2], b[b.Count / 2], 255);
        }

        // Los valores de la escena de este cuadro (las fotos y cada cuadro de los videos, §5.1).
        public static string Values(UiTestHost host, double t)
        {
            var scene = host.Scene;
            if (scene == null)
            {
                return "t " + F(t) + ": sin escena";
            }

            if (scene.Session == null)
            {
                return "t " + F(t) + " s; escena cerrada; dibujos " + scene.Draws.ToString(CultureInfo.InvariantCulture);
            }

            var f = scene.Frame;
            var p = f.Pose;
            return "t " + F(t) + " s; escena " + F(f.SceneTime) + " s; distancia " + F(f.Distance) + " m; velocidad " + F(f.Speed) + " m/s; hora "
                   + F(f.VisibleHour) + "; cámara " + f.CameraMode + " (" + F(p.X) + "; " + F(p.Y) + "; " + F(p.Z) + ") guiñada " + F(p.YawDeg) + "° cabeceo "
                   + F(p.PitchDeg) + "° rolido " + F(p.RollDeg) + "° focal " + F(p.FocalDp) + " horizonte " + F(p.HorizonDp) + "; respiración "
                   + F(f.BreathingMultiplier) + "; apartamiento del horizonte " + F(f.HorizonDeviationDp) + " dp; velo " + F(f.Post.VeilWeight)
                   + "; astro " + (f.Celestial.DiskIsMoon ? "luna" : "sol") + " " + F(f.Celestial.DiskAzimuthDeg) + "/" + F(f.Celestial.DiskElevationDeg) + "° vis. "
                   + F(f.Celestial.DiskVisibility) + "; estrellas " + F(f.StarFade) + "; líneas del agua " + F(f.ShimmerVisibility)
                   + "; opacidad de scene-surface " + F(SurfaceOpacity(host)) + "; " + scene.Readiness.Value + "; dibujos " + scene.Draws.ToString(CultureInfo.InvariantCulture);
        }

        // La opacidad con la que se ve la escena (0 si scene-surface no está a la vista).
        public static double SurfaceOpacity(UiTestHost host)
        {
            var page = host.Immersive;
            if (page == null || !page.HasView)
            {
                return 0;
            }

            double opacity = 1;
            for (var e = (UnityEngine.UIElements.VisualElement?)page.View.SceneSurface; e != null; e = e.hierarchy.parent)
            {
                if (e.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None)
                {
                    return 0;
                }

                opacity *= e.resolvedStyle.opacity;
            }

            return opacity;
        }

        // Una dirección del mundo (unitaria) a la pantalla con la cámara real: dp desde arriba a la izquierda.
        public static (double X, double Y) Project(UiTestHost host, double x, double y, double z)
        {
            var camera = host.Scene!.World!.Rig!.Camera;
            var v = camera.WorldToViewportPoint(camera.transform.position + new Vector3((float)x, (float)y, (float)z) * 1000f);
            return (v.x * host.Profile.WidthDp, (1 - v.y) * host.Profile.HeightDp);
        }

        private static double Luminance(Color32 c)
        {
            return (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
        }

        private static void Paint(Texture2D frame, double xPx, double yPx, int size)
        {
            var x0 = (int)Math.Round(xPx) - size / 2;
            var y0 = (int)Math.Round(yPx) - size / 2;
            for (var y = y0; y < y0 + size; y++)
            {
                for (var x = x0; x < x0 + size; x++)
                {
                    if (x >= 0 && y >= 0 && x < frame.width && y < frame.height)
                    {
                        frame.SetPixel(x, frame.height - 1 - y, MarkColor);
                    }
                }
            }
        }

        private static string I(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
