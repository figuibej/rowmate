using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Geometry;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WMS · La ronda del salto (AC-6B-20 b; QA-V6b-R2-03; #91; 06b-agua-avanzada/02-tech-design §18): lo que el vértice del agua avanzada
    // calcula, interpolado sobre la malla en anillos de verdad (WaterRingMesh) en la posición que le da WaterVertexField.MeshOffsetZ, no
    // salta mientras el bote avanza. Es la medición de VID-01 sin GPU: la diferencia media entre cuadros (60 fps, R-4 a 3,8 m/s desde
    // 4090 m, 6 s: cruza 4096 y 4112 m) en el agua de adelante (z de 16 a 60 m, |x| ≤ 6 m) no tiene un pico mayor que 1,5 × la mediana.
    // Con el ancla de 16 m de antes, cada 16 m la malla saltaba y los anillos re-muestreaban la ola: el pico era de 4 × o más.
    [TestFixture]
    public sealed class WaterMeshShiftTests
    {
        private const double Speed = 3.8, Fps = 60, Seconds = 6, StartDistance = 4090, StartTime = 37.0, PeakOverMedian = 1.5;

        private static readonly EnvironmentId[] All = { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta };

        private static readonly Func<WaterVertexSample, double>[] Fields =
        {
            s => s.ColorFactor, s => s.B1Slope, s => s.Crest, s => s.FoamCrest, s => s.Calm,
        };

        private static readonly string[] FieldNames = { "color", "pendiente de B1", "cresta de la columna", "espuma de crestas", "calma" };

        // Las mallas de verdad (Alto, y Bajo, que es también la de Medio: SceneWorldBuilder.WaterMeshTier).
        [TestCase(QualityTier.High), TestCase(QualityTier.Low)]
        [Category("AC-6B-20")]
        public void TheWaterDoesNotJumpWhileRowing(QualityTier tier)
        {
            var mesh = WaterRingMesh.Create(tier);
            var locator = new TriangleLocator(mesh, -12, 12, 0, 90);
            var failures = new List<string>();
            foreach (var id in All)
            {
                var p = WaterProfiles.For(id);
                var frames = (int)(Seconds * Fps);
                var points = Points();
                var prev = new double[Fields.Length][];
                var diffs = new List<double>[Fields.Length];
                for (var f = 0; f < Fields.Length; f++)
                {
                    diffs[f] = new List<double>();
                }

                for (var frame = 0; frame <= frames; frame++)
                {
                    var d = StartDistance + Speed * frame / Fps;
                    var t = StartTime + frame / Fps;
                    var ph = default(WaterPhaseSet);
                    WavePhases.Compute(d, t, p, ref ph);
                    var offset = WaterVertexField.MeshOffsetZ(d);
                    var now = Sample(mesh, locator, points, offset, p, ph);
                    for (var f = 0; f < Fields.Length; f++)
                    {
                        if (prev[f] != null)
                        {
                            var sum = 0.0;
                            for (var i = 0; i < points.Count; i++)
                            {
                                sum += Math.Abs(now[f][i] - prev[f][i]);
                            }

                            diffs[f].Add(sum / points.Count);
                        }

                        prev[f] = now[f];
                    }
                }

                for (var f = 0; f < Fields.Length; f++)
                {
                    var sorted = new List<double>(diffs[f]);
                    sorted.Sort();
                    var median = sorted[sorted.Count / 2];
                    var peak = sorted[sorted.Count - 1];
                    if (peak < 1e-9 || f == 3 && !p.Swell)
                    {
                        continue;   // el campo no se usa en este escenario (la pendiente de B1 en la costa, la espuma fuera de la costa)
                    }

                    var ratio = median > 0 ? peak / median : double.PositiveInfinity;
                    var local = LocalPeak(diffs[f], out var at);
                    TestContext.Progress.WriteLine("[WMS] " + tier + " " + id + " " + FieldNames[f] + ": pico " + peak.ToString("0.000000") + ", mediana "
                        + median.ToString("0.000000") + " (" + ratio.ToString("0.00") + " ×); contra los ±0,25 s: " + local.ToString("0.00") + " × (cuadro "
                        + at + ")");
                    if (local > PeakOverMedian)
                    {
                        failures.Add(tier + " " + id + " " + FieldNames[f] + ": pico " + local.ToString("0.00") + " × la mediana de los ±0,25 s (cuadro " + at + ")");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("; ", failures));
        }

        // El salto se mide contra la mediana de los cuadros vecinos (±0,25 s): un campo umbralizado (la espuma, la cresta) cambia su
        // diferencia media de a poco a lo largo de los 6 s mientras las olas cruzan la zona (eso no es un salto); el re-anclaje la
        // multiplicaba en un solo cuadro.
        private static double LocalPeak(List<double> diffs, out int at)
        {
            const int half = 15;
            var worst = 0.0;
            at = -1;
            var window = new List<double>();
            for (var i = 0; i < diffs.Count; i++)
            {
                window.Clear();
                for (var j = Math.Max(0, i - half); j <= Math.Min(diffs.Count - 1, i + half); j++)
                {
                    if (j != i)
                    {
                        window.Add(diffs[j]);
                    }
                }

                window.Sort();
                var median = window[window.Count / 2];
                var ratio = median > 0 ? diffs[i] / median : diffs[i] > 0 ? double.PositiveInfinity : 0;
                if (ratio > worst)
                {
                    worst = ratio;
                    at = i + 1;
                }
            }

            return worst;
        }

        // El color b de cada vértice (el paso máximo, WaterRingMesh.MaxStep) nunca es menor que el paso de ninguno de sus triángulos, y es
        // el mismo en los vértices que comparten lugar (continuo entre anillos).
        [TestCase(QualityTier.High), TestCase(QualityTier.Low)]
        [Category("AC-6B-20")]
        public void TheVertexMaxStepCoversItsTriangles(QualityTier tier)
        {
            var mesh = WaterRingMesh.Create(tier);
            var c = mesh.Colors;
            for (var t = 0; t < mesh.TriangleCount; t++)
            {
                var a = mesh.Indices[t * 3];
                var b = mesh.Indices[t * 3 + 1];
                var d = mesh.Indices[t * 3 + 2];
                var step = Math.Max(Math.Abs(mesh.X(a) - mesh.X(b)), Math.Max(Math.Abs(mesh.Z(a) - mesh.Z(b)), Math.Abs(mesh.Z(a) - mesh.Z(d))));
                var maxStep = Math.Max(c[a * 4 + 2], Math.Max(c[b * 4 + 2], c[d * 4 + 2])) * WaterRingMesh.MaxRingStep;
                Assert.That(maxStep, Is.GreaterThanOrEqualTo(step - 1e-4), "triángulo " + t);
            }

            Assert.That(WaterVertexField.MeshOffsetZ(4096.5), Is.EqualTo(0), "la malla queda quieta en cinta");
        }

        // Las medias de la cresta y de la espuma (lo que queda donde el paso no alcanza la ola) son las de la ola entera.
        [Test, Category("AC-6B-20")]
        public void TheFadedCrestsKeepTheirMean()
        {
            const int n = 1000;
            double crest = 0, foam = 0;
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    var h = 0.6 * Math.Sin(2 * Math.PI * (i + 0.5) / n) + 0.4 * Math.Sin(2 * Math.PI * (j + 0.5) / n);
                    crest += SceneMath.SmoothStep(0.3, 0.9, h * 0.5 + 0.5);
                    foam += SceneMath.SmoothStep(0.55, 0.85, h);
                }
            }

            Assert.That(crest / n / n, Is.EqualTo(WaterVertexField.CrestMean).Within(1e-5));
            Assert.That(foam / n / n, Is.EqualTo(WaterVertexField.FoamCrestMean).Within(1e-5));
            Assert.That(WaterVertexField.Band(1, 0.5), Is.EqualTo(1), "6 vértices por λ o más: la onda entera");
            Assert.That(WaterVertexField.Band(1, 2.1), Is.EqualTo(0), "3 vértices por λ o menos: nada");
        }

        private static List<(double X, double Z)> Points()
        {
            var points = new List<(double X, double Z)>();
            for (var z = 16.0; z <= 60.0; z += 1.37)
            {
                for (var x = -6.0; x <= 6.0; x += 1.13)
                {
                    points.Add((x, z));
                }
            }

            return points;
        }

        // Cada campo en cada punto: lo que el rasterizador interpola entre los tres vértices del triángulo que lo cubre (el vértice calcula
        // en su z de cinta = la local + el corrimiento de la malla).
        private static double[][] Sample(MeshData mesh, TriangleLocator locator, List<(double X, double Z)> points, double offset, in WaterProfile p,
                                         in WaterPhaseSet ph)
        {
            var result = new double[Fields.Length][];
            for (var f = 0; f < Fields.Length; f++)
            {
                result[f] = new double[points.Count];
            }

            var cache = new Dictionary<int, WaterVertexSample>();
            for (var i = 0; i < points.Count; i++)
            {
                var (x, z) = points[i];
                Assert.That(locator.Find(x, z - offset, out var a, out var b, out var c, out var wa, out var wb, out var wc), Is.True,
                    "sin triángulo en (" + x + "; " + z + ")");
                var sa = Vertex(mesh, a, offset, p, ph, cache);
                var sb = Vertex(mesh, b, offset, p, ph, cache);
                var sc = Vertex(mesh, c, offset, p, ph, cache);
                for (var f = 0; f < Fields.Length; f++)
                {
                    result[f][i] = wa * Fields[f](sa) + wb * Fields[f](sb) + wc * Fields[f](sc);
                }
            }

            return result;
        }

        private static WaterVertexSample Vertex(MeshData mesh, int v, double offset, in WaterProfile p, in WaterPhaseSet ph, Dictionary<int, WaterVertexSample> cache)
        {
            if (cache.TryGetValue(v, out var s))
            {
                return s;
            }

            WaterVertexField.Evaluate(mesh.X(v), mesh.Z(v) + offset, mesh.Colors[v * 4 + 2] * 256.0, p, ph, out s);
            cache[v] = s;
            return s;
        }

        // Los triángulos de una zona de la malla en cubetas de 1 m (en xz local).
        private sealed class TriangleLocator
        {
            private readonly MeshData _mesh;
            private readonly double _x0, _z0;
            private readonly int _nx, _nz;
            private readonly List<int>[] _buckets;

            public TriangleLocator(MeshData mesh, double x0, double x1, double z0, double z1)
            {
                _mesh = mesh;
                _x0 = x0;
                _z0 = z0;
                _nx = (int)Math.Ceiling(x1 - x0);
                _nz = (int)Math.Ceiling(z1 - z0);
                _buckets = new List<int>[_nx * _nz];
                for (var t = 0; t < mesh.TriangleCount; t++)
                {
                    var i = mesh.Indices;
                    double minX = double.MaxValue, maxX = double.MinValue, minZ = double.MaxValue, maxZ = double.MinValue;
                    for (var k = 0; k < 3; k++)
                    {
                        var v = i[t * 3 + k];
                        minX = Math.Min(minX, mesh.X(v));
                        maxX = Math.Max(maxX, mesh.X(v));
                        minZ = Math.Min(minZ, mesh.Z(v));
                        maxZ = Math.Max(maxZ, mesh.Z(v));
                    }

                    for (var bz = Cell(minZ - z0, _nz); bz <= Cell(maxZ - z0, _nz); bz++)
                    {
                        for (var bx = Cell(minX - x0, _nx); bx <= Cell(maxX - x0, _nx); bx++)
                        {
                            if (maxX < x0 || minX > x1 || maxZ < z0 || minZ > z1)
                            {
                                continue;
                            }

                            var bucket = _buckets[bz * _nx + bx] ??= new List<int>();
                            bucket.Add(t);
                        }
                    }
                }
            }

            private static int Cell(double v, int n)
            {
                return Math.Max(0, Math.Min(n - 1, (int)Math.Floor(v)));
            }

            public bool Find(double x, double z, out int a, out int b, out int c, out double wa, out double wb, out double wc)
            {
                a = b = c = 0;
                wa = wb = wc = 0;
                var bucket = _buckets[Cell(z - _z0, _nz) * _nx + Cell(x - _x0, _nx)];
                if (bucket == null)
                {
                    return false;
                }

                foreach (var t in bucket)
                {
                    var ia = _mesh.Indices[t * 3];
                    var ib = _mesh.Indices[t * 3 + 1];
                    var ic = _mesh.Indices[t * 3 + 2];
                    double ax = _mesh.X(ia), az = _mesh.Z(ia), bx = _mesh.X(ib), bz = _mesh.Z(ib), cx = _mesh.X(ic), cz = _mesh.Z(ic);
                    var det = (bz - cz) * (ax - cx) + (cx - bx) * (az - cz);
                    if (Math.Abs(det) < 1e-12)
                    {
                        continue;
                    }

                    var l1 = ((bz - cz) * (x - cx) + (cx - bx) * (z - cz)) / det;
                    var l2 = ((cz - az) * (x - cx) + (ax - cx) * (z - cz)) / det;
                    var l3 = 1 - l1 - l2;
                    if (l1 >= -1e-9 && l2 >= -1e-9 && l3 >= -1e-9)
                    {
                        a = ia;
                        b = ib;
                        c = ic;
                        wa = l1;
                        wb = l2;
                        wc = l3;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
