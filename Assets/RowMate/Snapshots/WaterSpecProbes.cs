#if ROWMATE_SIMULATOR
using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // Las sondas de la spec del set it6b (01-po-spec §3; QA-V6b-03, -04, -05 y -07 de la verificación): se leen sobre la RT de la escena del
    // mismo cuadro (sin la UI) o sobre el cuadro entero (con la UI: el vidrio y el diálogo) y proyectan con la cámara real. La luminancia es la
    // de WaterProbes (Rec. 709 sobre los valores de 8 bits, como mide QA) y los contrastes son cocientes de luminancias medias (≥ 1).
    //   AC-6B-04 (#3): (a) la cordillera lejana contra el cielo de atrás ≥ 1,3:1 por tercio de la pantalla (CAP-02, CAP-17); (b) en TH la ladera
    //                  cercana tapa ≤ 30 % del ancho de la lejana (CAP-17, con el modelo de Core); (c) a las 18 h el cielo a 2 diámetros del
    //                  disco supera en ≥ 15 % al de 6 diámetros, arriba del disco (CAP-03).
    //   AC-6B-11: el desvío de luminancia del agua cercana (2 a 20 m) a las 12 h; el orden regata ≤ lago < río < costa se mide en CAP-14 con
    //             los de CAP-02, -06 y -10 de la misma corrida.
    //   AC-6B-12: a las 12 h, el agua al pie de la orilla oscura (los pinos del lago, las fachadas del río) ≥ 10 % más oscura que el agua bajo
    //             cielo abierto en la misma fila (mediana: sin los destellos).
    //   AC-6B-13: la columna más brillante de la franja de 20 a 80 m a ± 40 dp de la x del astro (6, 12 y 18 h); a las 22 h, la luna más tenue
    //             que el sol de las 12 h del mismo escenario.
    //   AC-6B-17 (#12): el charco (adentro) y su borde contra el agua de alrededor ≥ 1,15:1; el hervidero ≥ 1,15:1; el par de remolinos a
    //                   40° ± 5° de x (los dos ojos, los mínimos de luminancia del charco, en el plano del agua).
    //   AC-6B-18: de noche, el agua de cada columna de luz más clara que la de los costados; de día, ninguna columna.
    //   AC-6B-19: la espuma de las crestas de la costa: la fracción de agua blanquecina de la franja de 15 a 60 m (informativa ≤ 35 %).
    //   AC-6B-23: la variación de luminancia dentro de cada pieza de vidrio sobre la orilla ≤ 25 % de la de la misma zona sin la pieza.
    //   AC-6B-27: la popa debajo del diálogo de completado ≥ 10 % más clara que el agua a la misma altura.
    public static class WaterSpecProbes
    {
        public const double MountainContrastMin = 1.3;
        public const double NearCoverMax = 0.30;
        public const double HaloGainMin = 0.15;
        public const double ShoreDarkMin = 0.10;
        public const double ColumnToleranceDp = 40;
        public const double EffectContrastMin = 1.15;
        public const double SwirlAngleDeg = 40, SwirlToleranceDeg = 5;
        public const double GlassVariationMax = 0.25;
        public const double SternGainMin = 0.10;
        public const double CrestFoamMax = 0.35;

        // Lo que se mide entre tomas de la misma corrida (AC-6B-11 y -13): por escenario.
        private static readonly Dictionary<EnvironmentId, double> NearDeviation = new Dictionary<EnvironmentId, double>();
        private static readonly Dictionary<EnvironmentId, double> NoonPeak = new Dictionary<EnvironmentId, double>();

        public static Func<UiTestHost, List<string>, List<ProbeRecord>> ForMatrix(int n, EnvironmentId env, int hour, DeviceProfile profile)
        {
            return (host, values) =>
            {
                var records = new List<ProbeRecord>();
                With(host, values, (scene, camera, px, w, h) =>
                {
                    var tv = profile.Code == DeviceProfile.TV.Code;
                    if (env == EnvironmentId.Lake && hour == 12)
                    {
                        records.Add(Mountains(host, camera, px, w, h, values));
                        if (!tv)
                        {
                            records.Add(NearCover(host, camera, px, w, h, values));
                        }
                    }

                    if (env == EnvironmentId.Lake && hour == 18)
                    {
                        records.Add(Halo(camera, px, w, h, values));
                    }

                    if (n > 16)
                    {
                        return;
                    }

                    if (hour == 12)
                    {
                        records.AddRange(NearVariation(env, camera, px, w, h, values));
                        if (env == EnvironmentId.Lake || env == EnvironmentId.River)
                        {
                            records.Add(ShoreDarkening(env, camera, px, w, h, values));
                        }
                    }

                    records.AddRange(Column(host, env, hour, camera, px, w, h, values));
                    records.Add(LightColumns(host, hour, camera, px, w, h, values));
                    if (env == EnvironmentId.Coast && hour == 12)
                    {
                        records.Add(CrestFoam(camera, px, w, h, values));
                    }
                });
                return records;
            };
        }

        public static Func<UiTestHost, List<string>, List<ProbeRecord>> PuddlesAndBoil()
        {
            return (host, values) =>
            {
                var records = new List<ProbeRecord>();
                With(host, values, (scene, camera, px, w, h) =>
                {
                    records.AddRange(Puddles(host, camera, px, w, h, values));
                    records.Add(Boil(host, camera, px, w, h, values));
                });
                return records;
            };
        }

        // La CAP-04 de la 7 (AC-6B-17): el charco real de la palada, proyectado.
        public static Func<UiTestHost, List<string>, List<ProbeRecord>> PuddlesOnly()
        {
            return (host, values) =>
            {
                var records = new List<ProbeRecord>();
                With(host, values, (scene, camera, px, w, h) => records.AddRange(Puddles(host, camera, px, w, h, values)));
                return records;
            };
        }

        public static Func<UiTestHost, List<string>, List<ProbeRecord>> GlassVariation()
        {
            return (host, values) =>
            {
                var records = new List<ProbeRecord>();
                With(host, values, (scene, camera, px, w, h) => records.Add(Glass(host, px, w, h, values)));
                return records;
            };
        }

        public static Func<UiTestHost, List<string>, List<ProbeRecord>> SternUnderDialog()
        {
            return (host, values) =>
            {
                var records = new List<ProbeRecord>();
                With(host, values, (scene, camera, px, w, h) => records.Add(Stern(host, camera, px, w, h, values)));
                return records;
            };
        }

        private delegate void Body(RowMate.Scene.SceneHost scene, Camera camera, Color32[] px, int w, int h);

        private static void With(UiTestHost host, List<string> values, Body body)
        {
            var scene = host.Scene;
            var output = scene?.Target.Texture;
            var camera = scene?.World?.Rig?.Camera;
            if (scene == null || output == null || camera == null)
            {
                values.Add("sondas de la spec: sin escena");
                return;
            }

            var image = RtReader.Read(output);
            try
            {
                // Solo para revisar a mano (nunca se versiona): la RT de la escena en ROWMATE_SCENE_DUMP.
                var dump = Environment.GetEnvironmentVariable("ROWMATE_SCENE_DUMP");
                if (!string.IsNullOrEmpty(dump))
                {
                    System.IO.Directory.CreateDirectory(dump);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dump, "scene-" + Time.frameCount + ".png"), image.EncodeToPNG());
                }

                body(scene, camera, image.GetPixels32(), image.width, image.height);
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        // ── AC-6B-04 ──

        private static ProbeRecord Mountains(UiTestHost host, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var f = host.Scene!.Frame;
            var thirds = new List<double>[] { new List<double>(), new List<double>(), new List<double>() };
            var lm = new List<double>[] { new List<double>(), new List<double>(), new List<double>() };
            var ls = new List<double>[] { new List<double>(), new List<double>(), new List<double>() };
            for (var az = -64.0; az <= 64.0; az += 1.0)
            {
                var far = Crest(camera, w, h, az, f.HorizonPeakTan * f.HorizonFarScale * Math.Max(0, HorizonModel.Profile(az, true)));
                var near = Crest(camera, w, h, az, f.HorizonPeakTan * f.HorizonNearScale * LakeArt.NearHeightScale * Math.Max(0, HorizonModel.Profile(az, false)));
                if (far.x < 4 || far.x >= w - 4 || far.y < 12 || far.y >= h - 12 || near.y < far.y + 14)
                {
                    continue;
                }

                var mountain = Mean(px, w, h, far.x, far.y + 5, 2, 2);
                var sky = Mean(px, w, h, far.x, far.y - 5, 2, 2);
                var c = Mid(px, w, h, far.x, far.y + 5);
                // Sin los pinos (verdes) delante de la cordillera.
                if (c.g > c.r + 20 && c.g > c.b)
                {
                    continue;
                }

                var third = Math.Min(2, far.x * 3 / w);
                thirds[third].Add(Ratio(mountain, sky));
                lm[third].Add(mountain);
                ls[third].Add(sky);
            }

            var names = new[] { "izquierda", "centro", "derecha" };
            var parts = new List<string>();
            var pass = true;
            var worst = double.MaxValue;
            var measured = 0;
            for (var i = 0; i < 3; i++)
            {
                if (thirds[i].Count < 3)
                {
                    parts.Add(names[i] + " sin columnas medibles");
                    continue;
                }

                var median = Median(thirds[i]);
                measured++;
                worst = Math.Min(worst, median);
                pass &= median >= MountainContrastMin;
                parts.Add(names[i] + " " + F(median) + ":1 (" + thirds[i].Count + " columnas; L cordillera " + F(Median(lm[i])) + ", cielo " + F(Median(ls[i])) + ")");
            }

            pass &= measured > 0;
            values.Add("cordillera lejana contra el cielo (AC-6B-04 a): " + string.Join("; ", parts));
            return Record("cordillera lejana contra el cielo, peor tercio (AC-6B-04 a)", "≥ " + F(MountainContrastMin) + ":1",
                measured > 0 ? F(worst) + ":1" : "sin columnas", pass, false);
        }

        // (b) La ladera cercana (el terreno de la orilla con sus pinos) sobre la cresta lejana: de los acimuts con la cresta lejana en el cuadro y
        // sin la cordillera cercana delante, cuántos tienen verde (pasto o pinos) en la cresta.
        private static ProbeRecord NearCover(UiTestHost host, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var f = host.Scene!.Frame;
            var visible = 0;
            var covered = 0;
            for (var az = -64.0; az <= 64.0; az += 0.5)
            {
                var pFar = Math.Max(0, HorizonModel.Profile(az, true));
                var far = Crest(camera, w, h, az, f.HorizonPeakTan * f.HorizonFarScale * pFar);
                var near = Crest(camera, w, h, az, f.HorizonPeakTan * f.HorizonNearScale * LakeArt.NearHeightScale * Math.Max(0, HorizonModel.Profile(az, false)));
                if (pFar <= 0 || far.x < 0 || far.x >= w || far.y < 0 || far.y >= h || near.y <= far.y + 2)
                {
                    continue;
                }

                visible++;
                var c = Mid(px, w, h, far.x, far.y + 2);
                if (c.g > c.r + 20 && c.g > c.b)
                {
                    covered++;
                }
            }

            var fraction = visible > 0 ? covered / (double)visible : 1;
            values.Add("ladera cercana sobre la cordillera lejana (AC-6B-04 b): " + covered + " de " + visible + " acimuts con la cresta lejana a la vista tapados por la orilla ("
                       + F(fraction * 100) + " %)");
            return Record("ladera cercana tapa la lejana (AC-6B-04 b)", "≤ " + F(NearCoverMax * 100) + " %", F(fraction * 100) + " %", visible > 0 && fraction <= NearCoverMax, false);
        }

        private static ProbeRecord Halo(Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var disk = (Vector3)Shader.GetGlobalVector("_RmDiskDir");
            var elevation = Math.Asin(Math.Max(-1, Math.Min(1, disk.y)));
            var azimuth = Math.Atan2(disk.x, disk.z);
            var diameter = 2 * SkyArt.SunDiskRadiusDeg * Math.PI / 180;
            var center = ProjectDir(camera, w, h, Direction(azimuth, elevation));
            var two = ProjectDir(camera, w, h, Direction(azimuth, elevation + 2 * diameter));
            var six = ProjectDir(camera, w, h, Direction(azimuth, elevation + 6 * diameter));
            var onScreen = Inside(center, w, h) && Inside(two, w, h) && Inside(six, w, h);
            var diskL = onScreen ? Mean(px, w, h, center.x, center.y, 2, 2) : 0;
            var l2 = onScreen ? Mean(px, w, h, two.x, two.y, 3, 3) : 0;
            var l6 = onScreen ? Mean(px, w, h, six.x, six.y, 3, 3) : 0;
            var c2 = onScreen ? Mid(px, w, h, two.x, two.y) : default;
            var sky = onScreen && !(c2.g > c2.r + 20 && c2.g > c2.b);
            var gain = l6 > 0 ? l2 / l6 - 1 : 0;
            values.Add("halo de las 18 h (AC-6B-04 c): disco " + (onScreen ? "(" + center.x + "; " + center.y + ") L " + F(diskL) + (diskL > 0.9 ? ", a la vista" : ", tapado") : "fuera de cuadro")
                       + "; a 2 diámetros L " + F(l2) + (sky ? "" : " (no es cielo)") + ", a 6 diámetros L " + F(l6) + " → " + F(gain * 100) + " %");
            return Record("halo de las 18 h: 2 contra 6 diámetros (AC-6B-04 c)", "≥ " + F(HaloGainMin * 100) + " %", F(gain * 100) + " %", onScreen && sky && gain >= HaloGainMin, false);
        }

        // ── AC-6B-11, -12, -13 ──

        private static IEnumerable<ProbeRecord> NearVariation(EnvironmentId env, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            // El agua de 2 a 20 m entre |x| 1,5 y 6 m (sin el bote, los remos ni la estela del centro): el desvío estándar de la luminancia.
            var samples = new List<double>();
            var cam = camera.transform.position;
            for (var z = 2.0; z <= 20.0; z += 0.25)
            {
                for (var x = -6.0; x <= 6.0; x += 0.25)
                {
                    if (Math.Abs(x) < 1.5 || (z > 3 && z < 15 && Math.Abs(x) < 3.5))
                    {
                        continue;
                    }

                    var p = Project(camera, w, h, new Vector3((float)x, 0, (float)(cam.z + z)));
                    if (Inside(p, w, h))
                    {
                        samples.Add(Lum(Mid(px, w, h, p.x, p.y)));
                    }
                }
            }

            var deviation = StdDev(samples);
            NearDeviation[env] = deviation;
            values.Add("agua cercana de 2 a 20 m (AC-6B-11): desvío de luminancia " + F(deviation) + " (" + samples.Count + " puntos)");
            if (env != EnvironmentId.Regatta)
            {
                yield break;
            }

            var ok = NearDeviation.TryGetValue(EnvironmentId.Lake, out var lake) & NearDeviation.TryGetValue(EnvironmentId.River, out var river)
                     & NearDeviation.TryGetValue(EnvironmentId.Coast, out var coast);
            var order = ok && deviation <= lake && lake < river && river < coast;
            var text = ok ? "regata " + F(deviation) + " ≤ lago " + F(lake) + " < río " + F(river) + " < costa " + F(coast) : "faltan CAP-02, -06 o -10 en la corrida";
            values.Add("orden del agua cercana (AC-6B-11): " + text);
            yield return Record("orden del agua cercana (AC-6B-11)", "regata ≤ lago < río < costa", text, order, false);
        }

        private static ProbeRecord ShoreDarkening(EnvironmentId env, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            // El pie de la orilla oscura (lago: el bosque de la izquierda; río: las fachadas de la izquierda) a 1 m del agua, a 40 m; el cielo
            // abierto, en el centro de la misma fila. Mediana de una ventana de 9 × 5 px.
            var cam = camera.transform.position;
            var shore = env == EnvironmentId.Lake ? -8.0 : -(RiverArt.WallX - 1.0);
            var dark = Project(camera, w, h, new Vector3((float)shore, 0, cam.z + 40));
            var open = Project(camera, w, h, new Vector3(0, 0, cam.z + 40));
            var lDark = MedianWindow(px, w, h, dark.x, dark.y, 4, 2);
            var lOpen = MedianWindow(px, w, h, open.x, dark.y, 4, 2);
            var darker = lOpen > 0 ? 1 - lDark / lOpen : 0;
            values.Add("reflejo de la orilla (AC-6B-12): al pie de la orilla (x " + F(shore) + " m, 40 m) L " + F(lDark) + ", cielo abierto L " + F(lOpen) + " → "
                       + F(darker * 100) + " % más oscura");
            return Record("agua al pie de la orilla oscura (AC-6B-12)", "≥ " + F(ShoreDarkMin * 100) + " % más oscura", F(darker * 100) + " %", darker >= ShoreDarkMin, false);
        }

        private static IEnumerable<ProbeRecord> Column(UiTestHost host, EnvironmentId env, int hour, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            // La franja de 20 a 80 m del agua: por columna de píxeles, la luminancia media de las filas de agua (|x| < la mitad del agua).
            var cam = camera.transform.position;
            var top = Project(camera, w, h, new Vector3(0, 0, cam.z + 80)).y;
            var bottom = Project(camera, w, h, new Vector3(0, 0, cam.z + 20)).y;
            var half = WaterHalfWidth(env);
            var columns = new List<double>[w];
            for (var y = Math.Max(0, top); y <= Math.Min(h - 1, bottom); y++)
            {
                // z de la fila (el plano del agua): de la proyección del centro.
                var z = 20 + (80 - 20) * (bottom - y) / Math.Max(1.0, bottom - top);
                var left = Project(camera, w, h, new Vector3((float)-half, 0, (float)(cam.z + z))).x;
                var right = env == EnvironmentId.Coast ? w - 1 : Project(camera, w, h, new Vector3((float)half, 0, (float)(cam.z + z))).x;
                for (var x = Math.Max(0, left); x <= Math.Min(w - 1, right); x++)
                {
                    (columns[x] ??= new List<double>()).Add(Lum(Mid(px, w, h, x, y)));
                }
            }

            // Por columna, el percentil 75 (el camino son muchos destellos; una boya o un destello suelto no lo mueven); promedio de 9.
            var p75 = new double[w];
            for (var x = 0; x < w; x++)
            {
                if (columns[x] != null && columns[x].Count > 4)
                {
                    columns[x].Sort();
                    p75[x] = columns[x][(int)(columns[x].Count * 0.75)];
                }
                else
                {
                    p75[x] = double.NaN;
                }
            }

            // El centro del camino: el centroide de lo que cada columna pasa de la mediana de las columnas (un camino ancho como el de la costa
            // satura muchas columnas y su máximo no dice dónde está; una boya o una línea de carriles pesa poco). El pico, para la luna.
            var valid = new List<double>();
            for (var x = 0; x < w; x++)
            {
                if (!double.IsNaN(p75[x]))
                {
                    valid.Add(p75[x]);
                }
            }

            var median = Median(valid);
            double weight = 0, moment = 0, peak = 0;
            for (var x = 0; x < w; x++)
            {
                if (double.IsNaN(p75[x]))
                {
                    continue;
                }

                peak = Math.Max(peak, p75[x]);
                var excess = Math.Max(0, p75[x] - median);
                weight += excess;
                moment += excess * x;
            }

            var best = weight > 0 ? (int)Math.Round(moment / weight) : -1;

            var disk = (Vector3)Shader.GetGlobalVector("_RmDiskDir");
            var flat = new Vector3(disk.x, 0, disk.z);
            var astroX = flat.sqrMagnitude > 1e-6 ? camera.WorldToViewportPoint(cam + flat.normalized * 5000f).x * w : double.NaN;
            var dpPerPx = host.Profile.WidthDp / w;
            var offsetDp = best >= 0 && !double.IsNaN(astroX) ? (best - astroX) * dpPerPx : double.NaN;
            if (hour == 12)
            {
                NoonPeak[env] = peak;
            }

            values.Add("camino de brillo (AC-6B-13): centro del camino de 20 a 80 m en x " + best + " px (L " + F(peak) + "), astro en x " + F(astroX) + " px → " + F(offsetDp) + " dp");
            if (hour != 22)
            {
                // "Bajo el sol o la luna que se ve": si el astro está bajo el horizonte o su x cae fuera del agua de la franja (el sol bajo
                // de las 6 y las 18 h sobre la orilla del canal), no hay camino que medir.
                var mid = Project(camera, w, h, new Vector3(0, 0, cam.z + 50));
                var waterLeft = Project(camera, w, h, new Vector3((float)-half, 0, cam.z + 50)).x;
                var waterRight = env == EnvironmentId.Coast ? w - 1 : Project(camera, w, h, new Vector3((float)half, 0, cam.z + 50)).x;
                var applies = host.Scene!.Frame.Celestial.DiskVisibility > 0.5 && !double.IsNaN(astroX) && astroX >= waterLeft && astroX <= waterRight && mid.y < h;
                if (!applies)
                {
                    values.Add("camino de brillo (AC-6B-13): no aplica (el astro " + (host.Scene.Frame.Celestial.DiskVisibility > 0.5 ? "cae fuera del agua, x " + F(astroX) + " px, agua de " + waterLeft + " a " + waterRight : "no se ve") + ")");
                    yield return Record("camino de brillo bajo el astro (AC-6B-13)", "± " + F(ColumnToleranceDp) + " dp", "no aplica (el astro fuera del agua)", true, true);
                    yield break;
                }

                yield return Record("camino de brillo bajo el astro (AC-6B-13)", "± " + F(ColumnToleranceDp) + " dp", F(offsetDp) + " dp",
                    !double.IsNaN(offsetDp) && Math.Abs(offsetDp) <= ColumnToleranceDp, false);
                yield break;
            }

            var has = NoonPeak.TryGetValue(env, out var noon);
            values.Add("la luna contra el sol de las 12 h (AC-6B-13): " + (has ? F(peak) + " contra " + F(noon) : "falta la toma de las 12 h en la corrida"));
            yield return Record("camino de la luna más tenue que el del sol (AC-6B-13)", "< L de las 12 h", has ? F(peak) + " contra " + F(noon) : "sin la de las 12 h",
                has && peak < noon, false);
        }

        // ── AC-6B-18, -19 ──

        private static ProbeRecord LightColumns(UiTestHost host, int hour, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var columns = host.Scene!.Frame.Water.Columns;
            var count = 0;
            var lit = 0;
            var measured = 0;
            if (columns != null)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    ref readonly var c = ref columns[i];
                    if (c.Alpha <= 0)
                    {
                        continue;
                    }

                    count++;
                    // La parte de la columna sobre el agua (la recta cámara-pie, x = X·z/zF): la mediana de su centro contra la de los
                    // costados (± 3 anchos en pantalla, al menos 4 px), del 40 % del camino al pie (cerca de la cámara se apaga).
                    var cam = camera.transform.position;
                    var zFoot = c.RelZ;
                    var center = new List<double>();
                    var sides = new List<double>();
                    var half = 8.5;
                    for (var z = Math.Max(4.0, 0.4 * zFoot); z < zFoot; z += 0.5)
                    {
                        var k = z / zFoot;
                        var x = cam.x + (c.X - cam.x) * k;
                        if (Math.Abs(x) > half - 0.5)
                        {
                            continue;
                        }

                        var q = Project(camera, w, h, new Vector3((float)x, 0, (float)(cam.z + z)));
                        var edge = Project(camera, w, h, new Vector3((float)(x + c.Width * k), 0, (float)(cam.z + z)));
                        var side = Math.Max(4, 3 * Math.Abs(edge.x - q.x));
                        if (!Inside(q, w, h) || q.x - side < 0 || q.x + side >= w)
                        {
                            continue;
                        }

                        center.Add(Lum(Mid(px, w, h, q.x, q.y)));
                        sides.Add(Lum(Mid(px, w, h, q.x - side, q.y)));
                        sides.Add(Lum(Mid(px, w, h, q.x + side, q.y)));
                    }

                    if (center.Count < 4)
                    {
                        continue;
                    }

                    measured++;
                    if (Median(center) > Median(sides) * 1.03)
                    {
                        lit++;
                    }
                }
            }

            values.Add("columnas de luz (AC-6B-18): " + count + " en el cuadro, " + measured + " medibles, " + lit + " más claras que sus costados");
            if (hour == 12)
            {
                return Record("de día, ninguna columna de luz (AC-6B-18)", "0", count.ToString(CultureInfo.InvariantCulture), count == 0, false);
            }

            if (hour != 22)
            {
                return Record("columnas de luz a las " + hour + " h (informativa)", "—", count.ToString(CultureInfo.InvariantCulture), true, true);
            }

            return Record("de noche, cada columna medible más clara que sus costados (AC-6B-18)", measured + " de " + measured, lit + " de " + measured,
                measured == 0 || lit == measured, measured == 0);
        }

        private static ProbeRecord CrestFoam(Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var cam = camera.transform.position;
            var total = 0;
            var white = 0;
            for (var z = 15.0; z <= 60.0; z += 0.5)
            {
                for (var x = -10.0; x <= 10.0; x += 0.25)
                {
                    if (Math.Abs(x) < 4 && z < 16)
                    {
                        continue;
                    }

                    var p = Project(camera, w, h, new Vector3((float)x, 0, (float)(cam.z + z)));
                    if (!Inside(p, w, h))
                    {
                        continue;
                    }

                    var c = Mid(px, w, h, p.x, p.y);
                    total++;
                    if (Lum(c) > 0.75 && Math.Max(c.r, Math.Max(c.g, c.b)) - Math.Min(c.r, Math.Min(c.g, c.b)) < 40)
                    {
                        white++;
                    }
                }
            }

            var fraction = total > 0 ? white / (double)total : 0;
            values.Add("espuma de las crestas (AC-6B-19): " + F(fraction * 100) + " % de agua blanquecina de 15 a 60 m (" + total + " puntos)");
            return Record("espuma de las crestas de la costa (AC-6B-19, informativa)", "≤ " + F(CrestFoamMax * 100) + " %", F(fraction * 100) + " %", fraction <= CrestFoamMax, true);
        }

        // ── AC-6B-17 ──

        private static IEnumerable<ProbeRecord> Puddles(UiTestHost host, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var f = host.Scene!.Frame;
            var puddles = f.Rower.Puddles;
            var found = 0;
            if (puddles != null)
            {
                for (var i = 0; i < puddles.Count; i++)
                {
                    var p = puddles[i];
                    var relZ = p.WorldZ - f.Distance;
                    if (relZ <= 1 || p.Alpha <= 0)
                    {
                        continue;
                    }

                    var center = Project(camera, w, h, new Vector3((float)p.X, 0, (float)relZ));
                    if (!Inside(center, w, h))
                    {
                        continue;
                    }

                    found++;
                    var r = PuddleField.Radius(p.Age);
                    var quad = r * WaterLookArt.PuddleQuadScale;
                    var inside = Disk(camera, px, w, h, p.X, relZ, 0, 0.55 * quad);
                    var edge = Disk(camera, px, w, h, p.X, relZ, 0.78 * quad, 0.92 * quad);
                    var outside = Disk(camera, px, w, h, p.X, relZ, 1.25 * quad, 1.6 * quad);
                    var cIn = Ratio(inside, outside);
                    var cEdge = Ratio(edge, outside);
                    var angle = SwirlAngle(camera, px, w, h, p.X, relZ, quad);
                    var name = (p.X < 0 ? "izquierdo" : "derecho");
                    values.Add("charco " + name + " (AC-6B-17): edad " + F(p.Age) + " s, radio " + F(r) + " m, en (" + center.x + "; " + center.y + ") px; adentro L " + F(inside)
                               + ", borde " + F(edge) + ", afuera " + F(outside) + " → " + F(cIn) + ":1 y " + F(cEdge) + ":1; remolinos a " + F(angle) + "° de x");
                    yield return Record("charco " + name + ": adentro contra el agua (AC-6B-17)", "≥ " + F(EffectContrastMin) + ":1", F(cIn) + ":1", cIn >= EffectContrastMin, false);
                    yield return Record("charco " + name + ": borde de espuma contra el agua (AC-6B-17)", "≥ " + F(EffectContrastMin) + ":1", F(cEdge) + ":1", cEdge >= EffectContrastMin, false);
                    // El ángulo solo con el charco entero en el cuadro (cortado por el borde, el eje de los ojos sale corrido): si no, no concluyente.
                    var west = Project(camera, w, h, new Vector3((float)(p.X - 0.55 * quad), 0, (float)relZ));
                    var east = Project(camera, w, h, new Vector3((float)(p.X + 0.55 * quad), 0, (float)relZ));
                    var whole = Inside(west, w, h) && Inside(east, w, h);
                    yield return Record("charco " + name + ": par de remolinos (AC-6B-17)", F(SwirlAngleDeg) + "° ± " + F(SwirlToleranceDeg) + "°",
                        F(angle) + "°" + (whole ? "" : " (no concluyente: el charco cortado por el borde)"),
                        !whole || (!double.IsNaN(angle) && Math.Abs(angle - SwirlAngleDeg) <= SwirlToleranceDeg), !whole);
                }
            }

            if (found == 0)
            {
                values.Add("charcos (AC-6B-17): ninguno a la vista");
                yield return Record("charcos a la vista (AC-6B-17)", "≥ 1", "0", false, false);
            }
        }

        private static ProbeRecord Boil(UiTestHost host, Camera camera, Color32[] px, int w, int h, List<string> values)
        {
            var f = host.Scene!.Frame;
            var rower = f.Rower;
            var boils = host.Scene.World?.Director?.WaterMarks?.Boils ?? 0;
            if (boils == 0)
            {
                values.Add("hervidero (AC-6B-17): no se dibuja en este cuadro");
                return Record("hervidero contra el agua (AC-6B-17)", "≥ " + F(EffectContrastMin) + ":1", "sin hervidero", false, false);
            }

            var boatZ = rower.BoatWorldZ - f.Distance;
            var worst = double.MaxValue;
            var parts = new List<string>();
            foreach (var oar in new[] { rower.Pose.Left, rower.Pose.Right })
            {
                var cx = oar.BladeCenter.X;
                var cz = boatZ + oar.BladeCenter.Z - WaterLookArt.BoilAftOffset;
                // El tercio de popa del hervidero (lo que no tapa la pala desde la cámara de atrás) contra el agua de afuera, del lado de afuera.
                var boil = Disk(camera, px, w, h, cx, cz - 0.08, 0, 0.1);
                var water = Disk(camera, px, w, h, cx + Math.Sign(cx) * 0.6, cz - 0.08, 0, 0.12);
                var c = Ratio(boil, water);
                worst = Math.Min(worst, c);
                parts.Add((cx < 0 ? "izquierdo" : "derecho") + " L " + F(boil) + " contra " + F(water) + " → " + F(c) + ":1");
            }

            values.Add("hervidero (AC-6B-17): " + string.Join("; ", parts));
            return Record("hervidero contra el agua, el peor (AC-6B-17)", "≥ " + F(EffectContrastMin) + ":1", F(worst) + ":1", worst >= EffectContrastMin, false);
        }

        // El par de remolinos: el eje principal (PCA en el plano del agua) del 4 % más oscuro del charco (los dos ojos, más oscuros que el
        // aplanado), de 0 a 90° respecto de x.
        private static double SwirlAngle(Camera camera, Color32[] px, int w, int h, double x0, double z0, double quad)
        {
            var points = new List<(double x, double z, double l)>();
            var r = 0.5 * quad;
            for (var dz = -r; dz <= r; dz += 0.015)
            {
                for (var dx = -r; dx <= r; dx += 0.015)
                {
                    if (dx * dx + dz * dz > r * r)
                    {
                        continue;
                    }

                    var p = Project(camera, w, h, new Vector3((float)(x0 + dx), 0, (float)(z0 + dz)));
                    if (Inside(p, w, h))
                    {
                        points.Add((dx, dz, Lum(Mid(px, w, h, p.x, p.y))));
                    }
                }
            }

            if (points.Count < 20)
            {
                return double.NaN;
            }

            points.Sort((a, b) => a.l.CompareTo(b.l));
            var n = Math.Max(10, points.Count * 4 / 100);
            double mx = 0, mz = 0;
            for (var i = 0; i < n; i++)
            {
                mx += points[i].x;
                mz += points[i].z;
            }

            mx /= n;
            mz /= n;
            double sxx = 0, szz = 0, sxz = 0;
            for (var i = 0; i < n; i++)
            {
                var ex = points[i].x - mx;
                var ez = points[i].z - mz;
                sxx += ex * ex;
                szz += ez * ez;
                sxz += ex * ez;
            }

            var angle = 0.5 * Math.Atan2(2 * sxz, sxx - szz) * 180 / Math.PI;
            return Math.Abs(angle) > 90 ? 180 - Math.Abs(angle) : Math.Abs(angle);
        }

        // ── AC-6B-23, -27 (con la UI) ──

        private static ProbeRecord Glass(UiTestHost host, Color32[] scenePx, int sw, int sh, List<string> values)
        {
            var glass = host.Immersive?.View.Glass;
            var frame = RtReader.Read(host.Target);
            try
            {
                var fw = frame.width;
                var fh = frame.height;
                var framePx = frame.GetPixels32();
                var density = host.Profile.Density;
                var worst = 0.0;
                var measured = 0;
                var parts = new List<string>();
                if (glass != null)
                {
                    foreach (var piece in glass.Pieces)
                    {
                        var b = piece.worldBound;
                        if (b.width < 8 || b.height < 8 || piece.resolvedStyle.display == DisplayStyle.None)
                        {
                            continue;
                        }

                        // Sin los 8 dp del borde y el reflejo interior: el interior de la pieza.
                        var x0 = (int)((b.xMin + 8) * density);
                        var x1 = (int)((b.xMax - 8) * density);
                        var y0 = (int)((b.yMin + 10) * density);
                        var y1 = (int)((b.yMax - 8) * density);
                        if (x1 - x0 < 8 || y1 - y0 < 8)
                        {
                            continue;
                        }

                        var with = new List<double>();
                        var without = new List<double>();
                        for (var y = y0; y < y1; y += 2)
                        {
                            for (var x = x0; x < x1; x += 2)
                            {
                                with.Add(Lum(WaterProbes.At(framePx, fw, fh, x, y)));
                                without.Add(Lum(WaterProbes.At(scenePx, sw, sh, x * sw / fw, y * sh / fh)));
                            }
                        }

                        // La variación sin el texto: el p10-p50 del interior (el texto es blanco y queda arriba del p50).
                        var vWith = Spread(with);
                        var vWithout = Spread(without);
                        if (vWithout < 0.02)
                        {
                            continue;
                        }

                        measured++;
                        var ratio = vWith / vWithout;
                        worst = Math.Max(worst, ratio);
                        parts.Add((string.IsNullOrEmpty(piece.name) ? "pieza" : piece.name) + " " + F(ratio * 100) + " % (" + F(vWith) + " / " + F(vWithout) + ")");
                    }
                }

                values.Add("vidrio sobre la escena (AC-6B-23): variación adentro / sin la pieza: " + (parts.Count > 0 ? string.Join("; ", parts) : "ninguna pieza sobre una zona con variación"));
                return Record("vidrio: variación dentro de la pieza, la peor (AC-6B-23)", "≤ " + F(GlassVariationMax * 100) + " %", measured > 0 ? F(worst * 100) + " %" : "sin piezas medibles",
                    measured > 0 && worst <= GlassVariationMax, false);
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }

        private static ProbeRecord Stern(UiTestHost host, Camera camera, Color32[] scenePx, int sw, int sh, List<string> values)
        {
            var scene = host.Scene!;
            var f = scene.Frame;
            var frame = RtReader.Read(host.Target);
            try
            {
                var fw = frame.width;
                var fh = frame.height;
                var px = frame.GetPixels32();
                // La cubierta de la popa (1 m delante de la punta, después del afinado de 0,6 m, sobre el eje) y el agua a la misma fila, 2,5 m a cada lado del casco.
                var z = (float)(f.Rower.BoatWorldZ - f.Distance - (SceneCamera.BoatZ - LakeArt.HullStern) + 1.0);
                var deck = Project(camera, fw, fh, new Vector3(0, (float)LakeArt.HullFreeboard, z));
                var left = Project(camera, fw, fh, new Vector3(-2.5f, 0, z));
                var right = Project(camera, fw, fh, new Vector3(2.5f, 0, z));
                var dialog = host.Root.Q(className: "rm-dialog--glass");
                var density = host.Profile.Density;
                var under = dialog != null && dialog.worldBound.Contains(new Vector2((float)(deck.x / density), (float)(deck.y / density)));
                var lDeck = Mean(px, fw, fh, deck.x, deck.y, 2, 2);
                var lWater = (Mean(px, fw, fh, left.x, deck.y, 3, 2) + Mean(px, fw, fh, right.x, deck.y, 3, 2)) / 2;
                var gain = lWater > 0 ? lDeck / lWater - 1 : 0;
                // La escena sola (sin la UI) en los mismos puntos: lo que el vidrio tiene detrás.
                var sDeck = Project(camera, sw, sh, new Vector3(0, (float)LakeArt.HullFreeboard, z));
                var sLeft = Project(camera, sw, sh, new Vector3(-2.5f, 0, z));
                var sceneDeck = Mean(scenePx, sw, sh, sDeck.x, sDeck.y, 1, 1);
                var sceneWater = Mean(scenePx, sw, sh, sLeft.x, sDeck.y, 1, 1);
                values.Add("popa en la escena sola: L " + F(sceneDeck) + " contra el agua " + F(sceneWater) + " (RT " + sw + " × " + sh + ")");
                values.Add("popa bajo el diálogo (AC-6B-27): cámara " + f.CameraMode + "; popa en (" + deck.x + "; " + deck.y + ") px, " + (under ? "debajo del diálogo" : "FUERA del diálogo")
                           + "; L popa " + F(lDeck) + ", agua " + F(lWater) + " → +" + F(gain * 100) + " %");
                return Record("popa bajo el diálogo contra el agua (AC-6B-27)", "≥ +" + F(SternGainMin * 100) + " %", "+" + F(gain * 100) + " %" + (under ? "" : " (fuera del diálogo)"),
                    under && gain >= SternGainMin, false);
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }

        // ── Ayudas ──

        private static Vector2Int Crest(Camera camera, int w, int h, double azimuthDeg, double tanElevation)
        {
            var a = azimuthDeg * Math.PI / 180;
            var d = new Vector3((float)Math.Sin(a), (float)tanElevation, (float)Math.Cos(a));
            return WaterProbes.Project(camera, w, h, camera.transform.position + d * 1000f);
        }

        private static Vector3 Direction(double azimuth, double elevation)
        {
            return new Vector3((float)(Math.Sin(azimuth) * Math.Cos(elevation)), (float)Math.Sin(elevation), (float)(Math.Cos(azimuth) * Math.Cos(elevation)));
        }

        private static Vector2Int ProjectDir(Camera camera, int w, int h, Vector3 direction)
        {
            return WaterProbes.Project(camera, w, h, camera.transform.position + direction * 1000f);
        }

        private static Vector2Int Project(Camera camera, int w, int h, Vector3 world)
        {
            return WaterProbes.Project(camera, w, h, world);
        }

        private static double WaterHalfWidth(EnvironmentId env)
        {
            switch (env)
            {
                case EnvironmentId.River:
                    return RiverArt.WallX;
                case EnvironmentId.Coast:
                    return 12;
                case EnvironmentId.Regatta:
                    return 9;
                default:
                    return 8.4;
            }
        }

        private static double Disk(Camera camera, Color32[] px, int w, int h, double x0, double z0, double r0, double r1)
        {
            var sum = 0.0;
            var n = 0;
            var step = Math.Max(0.01, r1 / 12);
            for (var dz = -r1; dz <= r1; dz += step)
            {
                for (var dx = -r1; dx <= r1; dx += step)
                {
                    var d2 = dx * dx + dz * dz;
                    if (d2 > r1 * r1 || d2 < r0 * r0)
                    {
                        continue;
                    }

                    var p = WaterProbes.Project(camera, w, h, new Vector3((float)(x0 + dx), 0, (float)(z0 + dz)));
                    if (Inside(p, w, h))
                    {
                        sum += Lum(Mid(px, w, h, p.x, p.y));
                        n++;
                    }
                }
            }

            return n > 0 ? sum / n : double.NaN;
        }

        private static double Mean(Color32[] px, int w, int h, int x, int y, int rx, int ry)
        {
            var sum = 0.0;
            var n = 0;
            for (var dy = -ry; dy <= ry; dy++)
            {
                for (var dx = -rx; dx <= rx; dx++)
                {
                    sum += Lum(WaterProbes.At(px, w, h, x + dx, y + dy));
                    n++;
                }
            }

            return sum / n;
        }

        private static double MedianWindow(Color32[] px, int w, int h, int x, int y, int rx, int ry)
        {
            var list = new List<double>();
            for (var dy = -ry; dy <= ry; dy++)
            {
                for (var dx = -rx; dx <= rx; dx++)
                {
                    list.Add(Lum(WaterProbes.At(px, w, h, x + dx, y + dy)));
                }
            }

            return Median(list);
        }

        private static Color32 Mid(Color32[] px, int w, int h, int x, int y)
        {
            return WaterProbes.At(px, w, h, x, y);
        }

        private static bool Inside(Vector2Int p, int w, int h)
        {
            return p.x >= 0 && p.x < w && p.y >= 0 && p.y < h;
        }

        private static double Lum(Color32 c)
        {
            return WaterProbes.Lum(c);
        }

        private static double Ratio(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b) || a <= 0 || b <= 0)
            {
                return double.NaN;
            }

            return a > b ? a / b : b / a;
        }

        private static double Median(List<double> list)
        {
            if (list.Count == 0)
            {
                return double.NaN;
            }

            var sorted = new List<double>(list);
            sorted.Sort();
            return sorted[sorted.Count / 2];
        }

        private static double StdDev(List<double> list)
        {
            if (list.Count < 2)
            {
                return double.NaN;
            }

            var mean = 0.0;
            foreach (var v in list)
            {
                mean += v;
            }

            mean /= list.Count;
            var sum = 0.0;
            foreach (var v in list)
            {
                sum += (v - mean) * (v - mean);
            }

            return Math.Sqrt(sum / (list.Count - 1));
        }

        // La variación de la luminancia sin el texto ni los íconos (que son claros): p50 − p5 del interior.
        private static double Spread(List<double> list)
        {
            var sorted = new List<double>(list);
            sorted.Sort();
            return sorted[sorted.Count / 2] - sorted[(int)(sorted.Count * 0.05)];
        }

        private static ProbeRecord Record(string name, string expected, string actual, bool pass, bool aspect)
        {
            return new ProbeRecord
            {
                Name = name,
                Element = "scene-surface",
                Expected = expected,
                Actual = actual,
                Difference = pass ? 0 : 1,
                Tolerance = 0,
                Aspect = aspect,
                Pass = pass,
            };
        }

        private static string F(double value)
        {
            return double.IsNaN(value) ? "—" : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
#endif
