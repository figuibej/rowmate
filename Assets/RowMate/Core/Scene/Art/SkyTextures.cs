using System;

namespace RowMate.Core.Scene.Art
{
    // Las recetas de las texturas procedurales de la escena (06a-escena-lago/04-design-notes §4.3, §5.2, §5.4, §7.5; DN6a-18; WP-D1,
    // dueño el Designer). Puras y deterministas: WP-B1 (ProceduralTextures) las hornea por filas al precargar (02-tech-design §3.4,
    // paso 3) y los shaders leen sus canales:
    // - el atlas de nubes (sky.clouds, 512 × 256, RG8): cuatro celdas de 256 × 128, la variante k en la celda (k mod 2; k / 2) contada
    //   desde ARRIBA; R = el cuerpo (las cuatro manchas del legacy con el borde comido por fBm y 2-3 sub-copos), G = la sombra (la
    //   elipse de abajo del legacy). La celda cubre el quad de la nube: x de −1,4·s a +1,4·s y y de −0,85·s a +0,70·s alrededor del
    //   punto del legacy (sky_painter.dart:96-101), con y hacia abajo. Unity cuenta las filas desde abajo: el texel (x; y desde
    //   arriba) va en la fila Alto − 1 − y;
    // - la luna (sky.moon, 256², R8 o RG8 con el albedo en R): el albedo del disco en (u; v) de −1 a 1 (v hacia arriba);
    // - el ruido del terreno (256², RGBA8, sin costura): R valor fino, G fBm grueso, B los cantos rodados (un valor por canto, oscuro
    //   en las juntas), A fBm de detalle;
    // - la capa lejana de nubes (solo Alto, 1 024 × 512, R8, sin costura en x): cirros estirados con cobertura ≈ 0,35.
    public static class SkyTextures
    {
        // ── El atlas de nubes ──
        public const int CloudAtlasWidth = 512, CloudAtlasHeight = 256, CloudCellWidth = 256, CloudCellHeight = 128, CloudVariants = 4;
        public const double CloudQuadHalfWidth = 1.4, CloudQuadTop = -0.85, CloudQuadBottom = 0.70;

        // Las manchas del legacy en unidades de s (y hacia abajo): la base (elipse 2,6 × 0,8 en y + 0,25), tres copos y la sombra
        // (elipse 2,0 × 0,3 en y + 0,5). Opacas hasta el 55 % del radio y transparentes en el borde (el RadialGradient del legacy).
        private static readonly double[] BodyBlobs =
        {
            // centro x, centro y, radio x, radio y
            0, 0.25, 1.3, 0.4,
            -0.6, 0, 0.5, 0.5,
            0, -0.15, 0.65, 0.65,
            0.7, 0.05, 0.45, 0.45,
        };

        private const double ShadeX = 0, ShadeY = 0.5, ShadeRx = 1.0, ShadeRy = 0.15;
        private const double BlobCore = 0.55;
        private const double LegacyLeft = -1.3, LegacyRight = 1.3, LegacyTop = -0.8;

        public static void CloudAtlasCell(int variant, out int x0, out int y0)
        {
            var k = ((variant % CloudVariants) + CloudVariants) % CloudVariants;
            x0 = (k % 2) * CloudCellWidth;
            y0 = (k / 2) * CloudCellHeight;
        }

        // El cuerpo y la sombra de la variante en (u; v) de su celda (v = 0 arriba), en [0; 1]. La variante 0 es la nube del legacy
        // tal cual (con el borde comido); la 1 es su espejo; la 2 y la 3, la del legacy y su espejo con los copos corridos.
        public static void CloudTexel(int variant, double u, double v, out double body, out double shade)
        {
            var k = ((variant % CloudVariants) + CloudVariants) % CloudVariants;
            var x = (u - 0.5) * 2 * CloudQuadHalfWidth;
            var y = CloudQuadTop + v * (CloudQuadBottom - CloudQuadTop);
            var mirror = k % 2 == 1;
            var xs = mirror ? -x : x;
            long salt = 0x434C4F55L + k;   // "CLOU" + variante
            var edge = Fbm(salt, xs * 2.5 + 11, y * 2.5 + 7, 3);   // [0; 1]
            var clear = 1.0;
            for (var b = 0; b < BodyBlobs.Length; b += 4)
            {
                double cx = BodyBlobs[b], cy = BodyBlobs[b + 1], rx = BodyBlobs[b + 2], ry = BodyBlobs[b + 3];
                if (k >= 2 && b > 0)
                {
                    // Los copos de las variantes 2 y 3: corridos hasta 0,08·s y con el radio × (0,9 a 1,1), dentro de la nube del legacy.
                    var jx = SceneHash.Unit(salt, b, 1) - 0.5;
                    var jy = SceneHash.Unit(salt, b, 2) - 0.5;
                    var jr = 0.9 + 0.2 * SceneHash.Unit(salt, b, 3);
                    cx += 0.16 * jx;
                    cy += 0.16 * jy;
                    rx *= jr;
                    ry *= jr;
                    Contain(ref cx, ref cy, rx, ry);
                }

                clear *= 1 - Blob(xs, y, cx, cy, rx, ry, edge);
            }

            // Los sub-copos: 2 o 3 por variante sobre el borde de arriba de los copos, así no son círculos.
            var subs = 2 + (int)(SceneHash.Unit(salt, 100, 0) * 2);
            for (var s = 0; s < subs; s++)
            {
                var host = 4 + 4 * (s % 3);
                double hx = BodyBlobs[host], hy = BodyBlobs[host + 1], hr = BodyBlobs[host + 2];
                var angle = Math.PI * (0.15 + 0.7 * SceneHash.Unit(salt, 200 + s, 0));   // en la mitad de arriba
                var r = 0.25 + 0.10 * SceneHash.Unit(salt, 200 + s, 1);
                var cx = hx + Math.Cos(angle) * hr * 0.55;
                var cy = hy - Math.Sin(angle) * hr * 0.55;
                Contain(ref cx, ref cy, r, r);
                clear *= 1 - Blob(xs, y, cx, cy, r, r, edge);
            }

            body = 1 - clear;
            shade = Blob(xs, y, ShadeX, ShadeY, ShadeRx, ShadeRy, edge * 0.5);
        }

        // Una mancha del legacy: 1 hasta el 55 % del radio y lineal a 0 en el borde; el fBm la come desde afuera (nunca la agranda,
        // así ninguna pasa del quad).
        private static double Blob(double x, double y, double cx, double cy, double rx, double ry, double edge)
        {
            var dx = (x - cx) / rx;
            var dy = (y - cy) / ry;
            var d = Math.Sqrt(dx * dx + dy * dy) * (1 + SkyArt.CloudFbm * edge);
            return SceneMath.Clamp01((1 - d) / (1 - BlobCore));
        }

        private static void Contain(ref double cx, ref double cy, double rx, double ry)
        {
            cx = Math.Max(LegacyLeft + rx, Math.Min(LegacyRight - rx, cx));
            cy = Math.Max(LegacyTop + ry, cy);
        }

        // ── La luna ──
        public const int MoonSize = 256;

        // Cuatro mares (Imbrio, Serenidad y Tranquilidad, Procelas, Nubes) de 0,70 a 0,80 de luminancia sobre las tierras altas (0,94), con
        // cráteres por ruido. (u; v) en el disco, de −1 a 1, v hacia arriba.
        private static readonly double[] Maria =
        {
            // centro u, centro v, radio, luminancia
            -0.32, 0.36, 0.30, 0.70,
            0.30, 0.22, 0.30, 0.76,
            -0.58, -0.08, 0.28, 0.80,
            0.06, -0.42, 0.24, 0.72,
        };

        public static double MoonAlbedo(double u, double v)
        {
            const long Salt = 0x4D4F4F4EL;   // "MOON"
            var albedo = SkyArt.MoonHighlands;
            var wobble = Value(Salt, u * 4 + 9, v * 4 + 3) - 0.5;
            for (var m = 0; m < Maria.Length; m += 4)
            {
                var dx = u - Maria[m];
                var dy = v - Maria[m + 1];
                var d = Math.Sqrt(dx * dx + dy * dy) / (Maria[m + 2] * (1 + 0.35 * wobble));
                var mask = 1 - SceneMath.SmoothStep(0.6, 1.0, d);
                albedo = SceneMath.Lerp(albedo, Maria[m + 3], mask);
            }

            var craters = Value(Salt + 1, u * 22 + 5, v * 22 + 1) - 0.5;
            return SceneMath.Clamp01(albedo + 0.10 * craters);
        }

        // ── El ruido del terreno (sin costura) ──
        public const int NoiseSize = 256;

        public static double Noise(int channel, int x, int y)
        {
            var px = Mod(x, NoiseSize);
            var py = Mod(y, NoiseSize);
            var fx = (px + 0.5) / NoiseSize;
            var fy = (py + 0.5) / NoiseSize;
            const long Salt = 0x4E4F4953L;   // "NOIS"
            switch (channel)
            {
                case 0:
                    return TileValue(Salt, fx * 16, fy * 16, 16, 16);
                case 1:
                    return TileFbm(Salt + 1, fx, fy, 2, 3);
                case 2:
                    return Pebbles(Salt + 2, fx * 16, fy * 16, 16);
                default:
                    return TileFbm(Salt + 3, fx, fy, 4, 3);
            }
        }

        // ── La capa lejana (sin costura en x) ──
        public const int CloudLayerWidth = 1024, CloudLayerHeight = 512;
        private const double CloudLayerThreshold = 0.55, CloudLayerSoftness = 0.08;

        public static double CloudLayer(int x, int y)
        {
            var px = Mod(x, CloudLayerWidth);
            var fx = (px + 0.5) / CloudLayerWidth;
            var fy = (Mod(y, CloudLayerHeight) + 0.5) / CloudLayerHeight;
            const long Salt = 0x4C415952L;   // "LAYR"
            // Cirros estirados: 32 celdas a lo ancho y 4 a lo alto en la primera octava.
            double sum = 0, weight = 0, amplitude = 1;
            for (var o = 0; o < 4; o++)
            {
                var cellsX = 32 << o;
                var cellsY = 4 << o;
                sum += amplitude * TileValue(Salt + o, fx * cellsX, fy * cellsY, cellsX, cellsY);
                weight += amplitude;
                amplitude *= 0.5;
            }

            var n = sum / weight;
            return SceneMath.SmoothStep(CloudLayerThreshold - CloudLayerSoftness, CloudLayerThreshold + CloudLayerSoftness, n);
        }

        // ── Ruido de valor ──
        private static double Value(long salt, double x, double y)
        {
            return SceneHash.Value2(salt, x, y);
        }

        private static double Fbm(long salt, double x, double y, int octaves)
        {
            double sum = 0, weight = 0, amplitude = 1, frequency = 1;
            for (var o = 0; o < octaves; o++)
            {
                sum += amplitude * SceneHash.Value2(salt + o, x * frequency, y * frequency);
                weight += amplitude;
                amplitude *= 0.5;
                frequency *= 2;
            }

            return sum / weight;
        }

        // Ruido de valor que se repite cada period celdas (las coordenadas del retículo, módulo el período).
        private static double TileValue(long salt, double x, double y, int periodX, int periodY)
        {
            var ix = Math.Floor(x);
            var iy = Math.Floor(y);
            var fx = SceneMath.Smooth(x - ix);
            var fy = SceneMath.Smooth(y - iy);
            var kx = (long)ix;
            var ky = (long)iy;
            var a = Lattice(salt, kx, ky, periodX, periodY);
            var b = Lattice(salt, kx + 1, ky, periodX, periodY);
            var c = Lattice(salt, kx, ky + 1, periodX, periodY);
            var d = Lattice(salt, kx + 1, ky + 1, periodX, periodY);
            var top = a + (b - a) * fx;
            var bottom = c + (d - c) * fx;
            return top + (bottom - top) * fy;
        }

        private static double TileFbm(long salt, double fx, double fy, int cells, int octaves)
        {
            double sum = 0, weight = 0, amplitude = 1;
            for (var o = 0; o < octaves; o++)
            {
                var n = cells << o;
                sum += amplitude * TileValue(salt + o, fx * n, fy * n, n, n);
                weight += amplitude;
                amplitude *= 0.5;
            }

            return sum / weight;
        }

        private static double Lattice(long salt, long kx, long ky, int periodX, int periodY)
        {
            return SceneHash.Unit(salt, Mod(kx, periodX), Mod(ky, periodY));
        }

        // Cantos rodados: un punto por celda corrido al azar; el valor del canto más cercano, oscuro en la junta (F2 − F1 chico).
        private static double Pebbles(long salt, double x, double y, int period)
        {
            var ix = (long)Math.Floor(x);
            var iy = (long)Math.Floor(y);
            double f1 = double.MaxValue, f2 = double.MaxValue, value = 0;
            for (var j = -1; j <= 1; j++)
            {
                for (var i = -1; i <= 1; i++)
                {
                    var cx = ix + i;
                    var cy = iy + j;
                    var wx = Mod(cx, period);
                    var wy = Mod(cy, period);
                    var px = cx + 0.15 + 0.7 * SceneHash.Unit(salt, wx, wy * 3 + 1);
                    var py = cy + 0.15 + 0.7 * SceneHash.Unit(salt, wx, wy * 3 + 2);
                    var d = Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                        value = SceneHash.Unit(salt, wx, wy * 3);
                    }
                    else if (d < f2)
                    {
                        f2 = d;
                    }
                }
            }

            return value * SceneMath.SmoothStep(0, 0.12, f2 - f1);
        }

        private static long Mod(long a, long n)
        {
            var r = a % n;
            return r < 0 ? r + n : r;
        }

        private static int Mod(int a, int n)
        {
            var r = a % n;
            return r < 0 ? r + n : r;
        }
    }
}
