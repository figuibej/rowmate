#if ROWMATE_SIMULATOR
using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Tests.UiSupport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Snapshots
{
    // VID-01 (AC-6B-20 b): la diferencia media de luminancia entre cuadros consecutivos del agua de adelante (z de 16 a 60 m, |x| ≤ 6 m: sin el
    // bote, los remos, los charcos ni la estela), leída en la RT de la escena. Al último cuadro, el pico contra la mediana (≤ 1,5 ×).
    internal sealed class WaterFrameDiff
    {
        public const double PeakOverMedian = 1.5;

        private readonly List<double> _diffs = new List<double>();
        private readonly List<double> _times = new List<double>();
        private double[]? _last;
        private double _crossedAt = double.NaN;

        public string? Check(UiTestHost h, double t, double seconds)
        {
            var scene = h.Scene;
            var camera = scene?.World?.Rig?.Camera;
            var output = scene?.Target.Texture;
            if (scene?.Session == null || camera == null || output == null)
            {
                return null;
            }

            if (double.IsNaN(_crossedAt) && scene.Frame.Distance >= 4096)
            {
                _crossedAt = t;
            }

            var image = RtReader.Read(output);
            try
            {
                var px = image.GetPixels32();
                var w = image.width;
                var hh = image.height;
                var cam = camera.transform.position;
                var samples = new List<double>();
                for (var z = 16.0; z <= 60.0; z += 2.0)
                {
                    for (var x = -6.0; x <= 6.0; x += 0.5)
                    {
                        // La media de un bloque de 9 × 9 px: la fase de las olas, sin el titileo de los destellos sueltos.
                        var p = WaterProbes.Project(camera, w, hh, new Vector3((float)x, 0, (float)(cam.z + z)));
                        var sum = 0.0;
                        for (var dy = -4; dy <= 4; dy++)
                        {
                            for (var dx = -4; dx <= 4; dx++)
                            {
                                sum += WaterProbes.Lum(WaterProbes.At(px, w, hh, p.x + dx, p.y + dy));
                            }
                        }

                        samples.Add(sum / 81);
                    }
                }

                var current = samples.ToArray();
                if (_last != null && _last.Length == current.Length)
                {
                    var sum = 0.0;
                    for (var i = 0; i < current.Length; i++)
                    {
                        sum += Math.Abs(current[i] - _last[i]);
                    }

                    _diffs.Add(sum / current.Length);
                    _times.Add(t);
                }

                _last = current;
            }
            finally
            {
                Object.DestroyImmediate(image);
            }

            if (t < seconds - 1e-6 || _diffs.Count < 3)
            {
                return null;
            }

            var sorted = new List<double>(_diffs);
            sorted.Sort();
            var median = sorted[sorted.Count / 2];
            var peak = sorted[sorted.Count - 1];
            var peakAt = _times[_diffs.IndexOf(peak)];
            var ratio = median > 0 ? peak / median : double.PositiveInfinity;
            var summary = "diferencia entre cuadros del agua: mediana " + F(median) + ", pico " + F(peak) + " a los " + F(peakAt) + " s (" + F(ratio) + " ×); cruce de 4096 m a los " + F(_crossedAt) + " s";
            Debug.Log("[SNAPSHOT] VID-01: " + summary);
            if (double.IsNaN(_crossedAt))
            {
                return "el bote no cruzó 4096 m";
            }

            return ratio > PeakOverMedian ? summary + ": pico > " + F(PeakOverMedian) + " × la mediana (AC-6B-20 b)" : null;
        }

        private static string F(double v)
        {
            return v.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    // VID-02 (AC-6B-07, -08): la avanzada antes de la falla (1,0 s), el fundido de 0,6 s y la degradada después; la sesión sigue (la distancia
    // no se detiene) y una sola línea del agua.
    internal sealed class FallbackTracker
    {
        private double _lastBlend;
        private double _lastDistance = double.NaN;

        public string? Check(UiTestHost h, double t)
        {
            var scene = h.Scene;
            if (scene?.Session == null)
            {
                return "la sesión de escena se cortó";
            }

            var blend = scene.DegradedBlend;
            var distance = scene.Frame.Distance;
            string? problem = null;
            if (t < 1.0 - 1e-6 && (!scene.ShowingAdvancedWater || blend > 1e-6))
            {
                problem = "antes de la falla no se ve la avanzada";
            }
            else if (t > 1.0 + 0.6 + 0.1 && blend < 1 - 1e-6)
            {
                problem = "el fundido no terminó a los 0,7 s de la falla (" + blend.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            }
            else if (blend < _lastBlend - 1e-9)
            {
                problem = "el fundido volvió para atrás";
            }
            else if (!double.IsNaN(_lastDistance) && distance <= _lastDistance)
            {
                problem = "la distancia no avanza (la sesión se cortó)";
            }
            else if (scene.WaterLogLines > 1)
            {
                problem = "más de una línea del agua (" + scene.WaterLogLines + ")";
            }

            _lastBlend = blend;
            _lastDistance = distance;
            return problem;
        }
    }

    // VID-03 (AC-6B-14; QA-V6b-02): la cinta arranca en la popa que se dibuja en todos los cuadros con estela, con alfa 0 en la cabeza (sin
    // borde recto); 4,5 s después de la pausa (3 × la inercia de 1,5 s) la popa ya casi no deja estela.
    internal sealed class WakeTracker
    {
        public string? Check(UiTestHost h, double t)
        {
            var scene = h.Scene;
            if (scene?.Session == null || !scene.ShowingAdvancedWater)
            {
                return null;
            }

            var f = scene.Frame;
            var wake = Shader.GetGlobalVectorArray(RowMate.Scene.SceneShaderIds.Wake);
            if (wake == null || wake.Length == 0)
            {
                return null;
            }

            if (Math.Abs(wake[0].x - f.Water.HullStartRelZ) > 1e-3)
            {
                return "la cinta arranca en " + wake[0].x.ToString("0.###", CultureInfo.InvariantCulture) + " m y la popa está en "
                       + f.Water.HullStartRelZ.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            }

            if (wake[0].w > 1e-6)
            {
                return "la cabeza de la cinta no nace con alfa 0 (" + wake[0].w.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            }

            if (t >= 13.5 && f.Water.WakeIntensity > 0.15)
            {
                return "4,5 s después de la pausa la popa sigue con intensidad " + f.Water.WakeIntensity.ToString("0.###", CultureInfo.InvariantCulture);
            }

            return null;
        }
    }

    // VID-04 (AC-6B-18, -34): a las 18 h (antes del cambio) y de día ninguna columna; a las 22 h (desde los 2,5 s) las columnas encendidas; la
    // hora no salta (≤ 0,3 h por cuadro).
    internal sealed class ColumnTracker
    {
        private double _lastHour = double.NaN;
        private int _maxColumns;

        public string? Check(UiTestHost h, double t)
        {
            var scene = h.Scene;
            var marks = scene?.World?.Director?.WaterMarks;
            if (scene?.Session == null || marks == null)
            {
                return null;
            }

            var hour = scene.Frame.VisibleHour;
            string? problem = null;
            // Un salto sería el cambio entero en un cuadro: con 1,5 s por el camino más corto (4 h) y su curva, ≤ 0,3 h por cuadro a 30 fps.
            if (!double.IsNaN(_lastHour) && Math.Abs(hour - _lastHour) > 0.3)
            {
                problem = "la hora saltó de " + _lastHour.ToString("0.##", CultureInfo.InvariantCulture) + " a " + hour.ToString("0.##", CultureInfo.InvariantCulture);
            }

            _lastHour = hour;
            _maxColumns = Math.Max(_maxColumns, marks.Columns);
            if (t > 5.9 && _maxColumns == 0)
            {
                problem = "a las 22 h no se encendió ninguna columna de luz";
            }

            return problem;
        }
    }

    // VID-05 (AC-6B-19): la boya de la costa sube y baja con la ola (|arfada| ≤ h·0,15 = 0,12 m) y se ve en algún cuadro.
    internal sealed class SwellTracker
    {
        private bool _seen;
        private double _min = double.MaxValue, _max = double.MinValue;

        public string? Check(UiTestHost h, double t)
        {
            var scene = h.Scene;
            if (scene?.Session == null)
            {
                return null;
            }

            var f = scene.Frame;
            var buoy = f.Rower.Buoy;
            string? problem = null;
            if (buoy != null && buoy.Visible)
            {
                _seen = true;
                _min = Math.Min(_min, f.Water.BuoyHeave);
                _max = Math.Max(_max, f.Water.BuoyHeave);
                if (Math.Abs(f.Water.BuoyHeave) > 0.12 + 1e-6)
                {
                    problem = "la boya arfa " + f.Water.BuoyHeave.ToString("0.###", CultureInfo.InvariantCulture) + " m (> 0,12)";
                }
            }

            if (t > 7.9)
            {
                if (!_seen)
                {
                    return "la boya no se vio";
                }

                Debug.Log("[SNAPSHOT] VID-05: arfada de la boya de " + _min.ToString("0.###", CultureInfo.InvariantCulture) + " a " + _max.ToString("0.###", CultureInfo.InvariantCulture) + " m");
            }

            return problem;
        }
    }
}
#endif
