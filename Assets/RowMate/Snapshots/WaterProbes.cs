#if ROWMATE_SIMULATOR
using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Tests.UiSupport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Las sondas de color del set it6b (06b-agua-avanzada/02-tech-design §8; 00-arte-agua §12; 04-design-notes §6; la ronda de ajuste de
    // WP-D). Se leen sobre la RT de la escena del mismo cuadro, con el post y SIN la UI (lo que el panel muestra debajo del HUD), con la
    // cámara real para proyectar el mundo:
    // - Seam (CAP-01 a -16): la costura del horizonte. La primera fila entera de agua contra la primera entera de cielo, solo en las
    //   columnas sin silueta (el cielo de esa columna, parejo en 3 filas y no más oscuro que la mediana de la fila: las siluetas son más
    //   oscuras y con textura). p95 de la diferencia por canal, tope 6/255. De aspecto: la versión del diseño (las columnas de
    //   HorizonProfile y la pasada sin siluetas) queda para issues.
    // - Lines (la degradada): los máximos locales de la luminancia en la columna de las líneas (corrida 10 % del ancho a la derecha del
    //   astro para no caer sobre el bote; las líneas tienen ≥ 15 % de semiancho), del horizonte al borde de abajo: 10 (#2, AC-6B-03).
    // - Glitter (12 h, Alto y Medio): los píxeles con luminancia > 0,85 a ± 3 m del bote, sin el casco (≥ 30, 00-arte-agua §12). De aspecto.
    // - Stern (CAP-31): la cubierta de la popa de punta a cockpit (el píxel más claro de cada fila, el gelcoat): la variación de punta a
    //   punta ≥ 8 % (AC-6B-21) y la media ≤ 0,82 (informativa).
    public static class WaterProbes
    {
        public const int SeamTolerance = 6;
        public const int Lines = 10;
        public const int GlitterMin = 30;
        public const double SternVariationMin = 0.08;
        public const double SternMeanMax = 0.82;

        public static Func<UiTestHost, List<string>, List<ProbeRecord>> For(bool seam, bool lines, bool glitter, bool stern, bool openRight = false)
        {
            return (host, values) => Run(host, values, seam, lines, glitter, stern, openRight);
        }

        private static List<ProbeRecord> Run(UiTestHost host, List<string> values, bool seam, bool lines, bool glitter, bool stern, bool openRight)
        {
            var records = new List<ProbeRecord>();
            var scene = host.Scene;
            var output = scene?.Target.Texture;
            var camera = scene?.World?.Rig?.Camera;
            if (scene == null || output == null || camera == null)
            {
                values.Add("sondas del agua: sin escena");
                return records;
            }

            var image = RtReader.Read(output);
            try
            {
                var w = image.width;
                var h = image.height;
                var pixels = image.GetPixels32();
                var forward = camera.transform.forward;
                forward.y = 0;
                var horizon = camera.WorldToViewportPoint(camera.transform.position + forward.normalized * 5000f);
                var horizonRow = (1 - horizon.y) * h;
                if (seam)
                {
                    // Las columnas donde el agua toca el horizonte: el canal (|x| < 8,4 m) a 1 400 m (el borde de la malla en anillos); en la
                    // costa, además, todo el mar abierto de la derecha.
                    var cam = camera.transform.position;
                    var left = Project(camera, w, h, new Vector3(-8.4f, 0, cam.z + 1400)).x;
                    var right = openRight ? w : Project(camera, w, h, new Vector3(8.4f, 0, cam.z + 1400)).x;
                    records.Add(Seam(pixels, w, h, horizonRow, Math.Max(0, left), Math.Min(w, right + 1), values));
                }

                if (lines)
                {
                    records.AddRange(LineCount(pixels, w, h, horizonRow, camera, scene.Frame.WaterPhase, values));
                }

                if (glitter)
                {
                    records.Add(Glitter(pixels, w, h, camera, scene.Frame.Distance, values));
                }

                if (stern)
                {
                    records.AddRange(Stern(pixels, w, h, camera, values));
                }
            }
            finally
            {
                Object.DestroyImmediate(image);
            }

            return records;
        }

        private static ProbeRecord Seam(Color32[] px, int w, int h, double horizonRow, int from, int to, List<string> values)
        {
            var water = Math.Min(h - 1, (int)Math.Floor(horizonRow) + 1);
            var sky = Math.Max(3, (int)Math.Ceiling(horizonRow) - 1);
            var skyLum = new double[w];
            for (var x = 0; x < w; x++)
            {
                skyLum[x] = Lum(At(px, w, h, x, sky));
            }

            var sorted = (double[])skyLum.Clone();
            Array.Sort(sorted);
            var median = sorted[w / 2];
            var diffs = new List<int>();
            for (var x = from; x < to; x++)
            {
                var s0 = At(px, w, h, x, sky);
                var s3 = At(px, w, h, x, sky - 3);
                if (skyLum[x] < median - 1.0 / 255 || MaxChannel(s0, s3) > 4)
                {
                    continue;
                }

                diffs.Add(MaxChannel(s0, At(px, w, h, x, water)));
            }

            var p95 = 255;
            if (diffs.Count > 0)
            {
                diffs.Sort();
                p95 = diffs[Math.Min(diffs.Count - 1, (int)Math.Floor(0.95 * diffs.Count))];
            }

            values.Add("costura: filas " + water + " (agua) y " + sky + " (cielo); " + diffs.Count + " columnas con agua y sin silueta (de " + from + " a " + to + "); p95 "
                       + p95 + "/255 (tope " + SeamTolerance + ")" + (diffs.Count < 32 ? "; menos de 32 columnas: no concluyente sin la pasada sin siluetas" : ""));
            return new ProbeRecord
            {
                Name = "costura del horizonte (p95 por canal, " + diffs.Count + " columnas)",
                Element = "scene-surface",
                Expected = "≤ " + SeamTolerance + "/255",
                Actual = p95 + "/255",
                Difference = p95,
                Tolerance = SeamTolerance,
                Aspect = true,
                Pass = diffs.Count >= 32 && p95 <= SeamTolerance,
            };
        }

        private static IEnumerable<ProbeRecord> LineCount(Color32[] px, int w, int h, double horizonRow, Camera camera, double phase, List<string> values)
        {
            var disk = Shader.GetGlobalVector("_RmDiskDir");
            var flat = new Vector3(disk.x, 0, disk.z);
            var column = w / 2;
            if (flat.sqrMagnitude > 1e-6)
            {
                var vp = camera.WorldToViewportPoint(camera.transform.position + flat.normalized * 5000f);
                column = (int)Math.Round(vp.x * w);
            }

            column = Math.Max(1, Math.Min(w - 2, column + w / 10));
            var top = (int)Math.Ceiling(horizonRow) + 1;
            var n = Math.Max(0, h - top);
            var lum = new double[n];
            for (var i = 0; i < n; i++)
            {
                var y = top + i;
                lum[i] = (Lum(At(px, w, h, column - 1, y)) + Lum(At(px, w, h, column, y)) + Lum(At(px, w, h, column + 1, y))) / 3;
            }

            // Un máximo: más alto que los vecinos y 3/255 sobre el mínimo de ± 8 filas (las líneas son de 1,5 dp, separadas t²·alto).
            var count = 0;
            var rows = new List<string>();
            for (var i = 1; i < n - 1; i++)
            {
                if (lum[i] <= lum[i - 1] || lum[i] < lum[i + 1])
                {
                    continue;
                }

                var low = lum[i];
                for (var j = Math.Max(0, i - 8); j <= Math.Min(n - 1, i + 8); j++)
                {
                    low = Math.Min(low, lum[j]);
                }

                if (lum[i] - low >= 3.0 / 255)
                {
                    count++;
                    rows.Add((top + i) + ":" + F((lum[i] - low) * 255));
                }
            }

            // El modelo de la degradada (water_painter.dart): la línea n en t = 0,1·n + fase, a t² del alto del agua bajo el horizonte. Cada
            // una de las 10 tiene un máximo a ± 6 filas (vista) o queda bajo algo más fuerte (> 30/255: el remo o el bote; tapada) o en el 2 %
            // de arriba del agua (t < 0,14: a 1,5 dp de alto se funden con la orilla lejana y la bruma; no se puede medir).
            // La 11.ª (#2) sería un máximo en las últimas 4 filas: tiene que faltar.
            var waterRows = h - horizonRow;
            var seen = 0;
            var hidden = 0;
            for (var k = 0; k < Lines; k++)
            {
                var t = 0.1 * k + phase;
                var row = horizonRow + t * t * waterRows;
                var found = false;
                var covered = false;
                foreach (var m in Maxima(lum, top))
                {
                    if (Math.Abs(m.row - row) <= 6)
                    {
                        found |= m.prominence <= 30.0 / 255;
                        covered |= m.prominence > 30.0 / 255;
                    }
                }

                if (found)
                {
                    seen++;
                }
                else if (covered || row - horizonRow < 0.02 * waterRows)
                {
                    hidden++;
                }
            }

            var eleventh = false;
            foreach (var m in Maxima(lum, top))
            {
                eleventh |= m.row >= h - 5;
            }

            values.Add("líneas de la degradada: " + seen + " vistas y " + hidden + " tapadas o sobre el horizonte de " + Lines + "; 11.ª " + (eleventh ? "PRESENTE" : "ausente")
                       + " (columna x " + column + " px; máximos: " + string.Join(", ", rows) + ")");
            // De contrato: que no haya una 11.ª en el borde de abajo (#2). De aspecto: la cuenta (con la fase cerca de 0,1, la 10.ª cae en
            // las últimas filas y se pierde contra el borde; la del índice la prueba DegradedClampsToTenLines en dotnet).
            return new[]
            {
                new ProbeRecord
                {
                    Name = "líneas de la degradada (#2): sin la 11.ª en el borde de abajo",
                    Element = "scene-surface",
                    Expected = "sin la 11.ª",
                    Actual = eleventh ? "la 11.ª" : "sin la 11.ª",
                    Difference = eleventh ? 1 : 0,
                    Tolerance = 0,
                    Aspect = false,
                    Pass = !eleventh,
                },
                new ProbeRecord
                {
                    Name = "líneas de la degradada (#2): vistas y tapadas",
                    Element = "scene-surface",
                    Expected = Lines.ToString(CultureInfo.InvariantCulture),
                    Actual = (seen + hidden).ToString(CultureInfo.InvariantCulture),
                    Difference = Math.Abs(seen + hidden - Lines),
                    Tolerance = 0,
                    Aspect = true,
                    Pass = seen + hidden == Lines,
                },
            };
        }

        private static IEnumerable<(int row, double prominence)> Maxima(double[] lum, int top)
        {
            var n = lum.Length;
            for (var i = 1; i < n - 1; i++)
            {
                if (lum[i] <= lum[i - 1] || lum[i] < lum[i + 1])
                {
                    continue;
                }

                var low = lum[i];
                for (var j = Math.Max(0, i - 8); j <= Math.Min(n - 1, i + 8); j++)
                {
                    low = Math.Min(low, lum[j]);
                }

                if (lum[i] - low >= 3.0 / 255)
                {
                    yield return (top + i, lum[i] - low);
                }
            }
        }

        private static ProbeRecord Glitter(Color32[] px, int w, int h, Camera camera, double distance, List<string> values)
        {
            // ± 3 m del bote (popa 5,3 y proa 13,7 en cinta) sobre el agua; sin la franja del casco (± 0,4 m).
            var cam = camera.transform.position;
            var box = Box(camera, w, h, new Vector3(-3, 0, 5.3f - 3 + cam.z), new Vector3(3, 0, 13.7f + 3 + cam.z));
            var hull = Box(camera, w, h, new Vector3(-0.4f, 0, 5.3f + cam.z), new Vector3(0.4f, 0.6f, 13.7f + cam.z));
            var count = 0;
            for (var y = box.yMin; y < box.yMax; y++)
            {
                for (var x = box.xMin; x < box.xMax; x++)
                {
                    if (hull.Contains(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    if (Lum(At(px, w, h, x, y)) > 0.85)
                    {
                        count++;
                    }
                }
            }

            values.Add("camino de brillo: " + count + " píxeles con L > 0,85 a ± 3 m del bote (caja " + box.width + " × " + box.height + " px)");
            return new ProbeRecord
            {
                Name = "camino de brillo a ± 3 m del bote",
                Element = "scene-surface",
                Expected = "≥ " + GlitterMin + " px",
                Actual = count + " px",
                Difference = Math.Max(0, GlitterMin - count),
                Tolerance = 0,
                Aspect = true,
                Pass = count >= GlitterMin,
            };
        }

        private static IEnumerable<ProbeRecord> Stern(Color32[] px, int w, int h, Camera camera, List<string> values)
        {
            // La cubierta entre la punta (5,3 m) y el cockpit (7,8 m): en cada fila, el píxel más claro a ± 0,15 m del eje (el gelcoat).
            var cam = camera.transform.position;
            var tip = Project(camera, w, h, new Vector3(0, 0.25f, 5.35f + cam.z));
            var cockpit = Project(camera, w, h, new Vector3(0, 0.25f, 7.8f + cam.z));
            var span = Math.Abs(Project(camera, w, h, new Vector3(0.15f, 0.25f, 6.5f + cam.z)).x - Project(camera, w, h, new Vector3(0, 0.25f, 6.5f + cam.z)).x);
            var y0 = Math.Min(tip.y, cockpit.y);
            var y1 = Math.Max(tip.y, cockpit.y);
            var rows = new List<double>();
            for (var y = y0; y <= y1; y++)
            {
                var t = y1 == y0 ? 0.0 : (double)(y - tip.y) / (cockpit.y - tip.y);
                var cx = (int)Math.Round(tip.x + t * (cockpit.x - tip.x));
                var best = 0.0;
                for (var x = cx - Math.Max(1, span); x <= cx + Math.Max(1, span); x++)
                {
                    best = Math.Max(best, Lum(At(px, w, h, x, y)));
                }

                rows.Add(best);
            }

            var mean = 0.0;
            foreach (var r in rows)
            {
                mean += r;
            }

            mean /= Math.Max(1, rows.Count);
            var k = Math.Max(1, rows.Count / 6);
            double End(bool atTip)
            {
                var sum = 0.0;
                for (var i = 0; i < k; i++)
                {
                    sum += rows[atTip ? (tip.y > cockpit.y ? rows.Count - 1 - i : i) : (tip.y > cockpit.y ? i : rows.Count - 1 - i)];
                }

                return sum / k;
            }

            var tipL = rows.Count > 0 ? End(true) : 0;
            var cockpitL = rows.Count > 0 ? End(false) : 0;
            var variation = cockpitL > 0 ? (cockpitL - tipL) / cockpitL : 0;
            values.Add("popa: " + rows.Count + " filas; luminancia media " + F(mean) + " (informativa ≤ " + F(SternMeanMax) + "); cockpit " + F(cockpitL) + ", punta "
                       + F(tipL) + ", variación " + F(variation * 100) + " % (≥ " + F(SternVariationMin * 100) + " %)");
            yield return new ProbeRecord
            {
                Name = "popa: variación de punta a punta (#85)",
                Element = "scene-surface",
                Expected = "≥ " + F(SternVariationMin * 100) + " %",
                Actual = F(variation * 100) + " %",
                Difference = (int)Math.Round(Math.Max(0, SternVariationMin - variation) * 100),
                Tolerance = 0,
                Aspect = false,
                Pass = rows.Count > 2 && variation >= SternVariationMin,
            };
            yield return new ProbeRecord
            {
                Name = "popa: luminancia media (informativa)",
                Element = "scene-surface",
                Expected = "≤ " + F(SternMeanMax),
                Actual = F(mean),
                Difference = (int)Math.Round(Math.Max(0, mean - SternMeanMax) * 255),
                Tolerance = 0,
                Aspect = true,
                Pass = mean <= SternMeanMax,
            };
        }

        internal static Vector2Int Project(Camera camera, int w, int h, Vector3 world)
        {
            var vp = camera.WorldToViewportPoint(world);
            return new Vector2Int((int)Math.Round(vp.x * w), (int)Math.Round((1 - vp.y) * h));
        }

        private static RectInt Box(Camera camera, int w, int h, Vector3 min, Vector3 max)
        {
            var xMin = int.MaxValue;
            var yMin = int.MaxValue;
            var xMax = int.MinValue;
            var yMax = int.MinValue;
            for (var i = 0; i < 8; i++)
            {
                var p = Project(camera, w, h, new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
                xMin = Math.Min(xMin, p.x);
                yMin = Math.Min(yMin, p.y);
                xMax = Math.Max(xMax, p.x);
                yMax = Math.Max(yMax, p.y);
            }

            xMin = Math.Max(0, xMin);
            yMin = Math.Max(0, yMin);
            xMax = Math.Min(w, xMax);
            yMax = Math.Min(h, yMax);
            return new RectInt(xMin, yMin, Math.Max(0, xMax - xMin), Math.Max(0, yMax - yMin));
        }

        // El píxel (x; fila desde arriba): la textura de RtReader tiene la fila 0 abajo.
        internal static Color32 At(Color32[] px, int w, int h, int x, int rowFromTop)
        {
            x = Math.Max(0, Math.Min(w - 1, x));
            rowFromTop = Math.Max(0, Math.Min(h - 1, rowFromTop));
            return px[(h - 1 - rowFromTop) * w + x];
        }

        internal static double Lum(Color32 c)
        {
            return (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
        }

        private static int MaxChannel(Color32 a, Color32 b)
        {
            return Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b)));
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
#endif
