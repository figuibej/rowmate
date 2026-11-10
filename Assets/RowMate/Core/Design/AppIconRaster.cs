using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Las formas de salida del ícono (It. 14, 02-tech-design §2.1; 00-arte §1.5).
    public enum AppIconVariant
    {
        // Placa cuadrada redondeada (Windows, Android legacy): margen round(lado/16), radio round(lado·0,19), bg.app adentro y
        // transparente afuera. La marca se recorta a la placa.
        Plate,

        // A sangre y opaca, sin esquinas (iOS: las pone el sistema).
        Bleed,

        // Android adaptativo, capa de fondo: bg.app liso a sangre, sin marca (AC-APP-16-09).
        AdaptiveBackground,

        // Android adaptativo, capa de frente: la marca de 60 dp centrada en 108, transparente alrededor, sin resplandor.
        AdaptiveForeground,

        // Android redondo: disco de 0,92 del lado.
        Round,

        // macOS: placa de 824 × 824 en (100; 100) de un lienzo de 1024, radio 185, sombra 0 10 28 negra al 30 % y la marca a
        // 0,70 (o el nivel) de la placa.
        MacPlate,

        // La marca sin fondo, a 0,94 del lienzo (el radio máximo queda dentro): arranque de la app y de Android 8-11, y la
        // imagen del launch screen de iOS.
        BootMark,
    }

    // Una imagen RGBA de 8 bits con alfa recto, fila por fila desde arriba.
    public sealed class AppIconImage
    {
        public AppIconImage(int width, int height, byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }

            if (width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            {
                throw new ArgumentException("AppIconImage: el tamaño no coincide con los píxeles.", nameof(rgba));
            }

            Width = width;
            Height = height;
            Rgba = rgba;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] Rgba { get; }

        public bool IsOpaque
        {
            get
            {
                for (var i = 3; i < Rgba.Length; i += 4)
                {
                    if (Rgba[i] != 255)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Pixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            var i = (y * Width + x) * 4;
            r = Rgba[i];
            g = Rgba[i + 1];
            b = Rgba[i + 2];
            a = Rgba[i + 3];
        }

        // PNG determinista (PngEncoder); sin alfa solo si es opaca.
        public byte[] ToPng(bool withAlpha = true)
        {
            return PngEncoder.Encode(Width, Height, Rgba, withAlpha);
        }
    }

    // Rasterizador puro del ícono (It. 14, DT14-01; 00-arte §1.4): 8 × 8 muestras por píxel en los píxeles con un borde y una
    // en el centro en los demás (un píxel sin borde tiene la misma cobertura en todas sus muestras; los degradés y el
    // resplandor son continuos), composición "over" sobre sRGB con alfa recto y redondeo DartMath.Round a 8 bits. La marca
    // se dibuja en su propia capa (los recortes la vacían) y después va sobre el fondo de la forma. Coordenadas en double y
    // solo + − × ÷ y √ (QA-G14-05: los bytes no dependen de la plataforma).
    public static class AppIconRaster
    {
        public const int SamplesPerAxis = 8;

        // Medio diagonal de un píxel: una muestra nunca está más lejos del centro.
        private const double HalfDiagonal = 0.7071067811865476;

        public static AppIconImage Render(AppIconVariant variant, int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "AppIconRaster: el tamaño tiene que ser positivo.");
            }

            var frame = Frame.For(variant, size);
            var rgba = new byte[size * size * 4];
            var n = SamplesPerAxis;
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    double sumA = 0, sumR = 0, sumG = 0, sumB = 0;
                    var cx = px + 0.5;
                    var cy = py + 0.5;
                    if (frame.IsEdge(cx, cy))
                    {
                        for (var sy = 0; sy < n; sy++)
                        {
                            for (var sx = 0; sx < n; sx++)
                            {
                                frame.Sample(px + (sx + 0.5) / n, py + (sy + 0.5) / n, out var r, out var g, out var b, out var a);
                                sumA += a;
                                sumR += r * a;
                                sumG += g * a;
                                sumB += b * a;
                            }
                        }

                        var count = n * n;
                        sumA /= count;
                        sumR /= count;
                        sumG /= count;
                        sumB /= count;
                    }
                    else
                    {
                        frame.Sample(cx, cy, out var r, out var g, out var b, out var a);
                        sumA = a;
                        sumR = r * a;
                        sumG = g * a;
                        sumB = b * a;
                    }

                    var i = (py * size + px) * 4;
                    var alpha = ToByte(sumA * 255);
                    if (alpha == 0)
                    {
                        continue;
                    }

                    rgba[i] = ToByte(sumR / sumA);
                    rgba[i + 1] = ToByte(sumG / sumA);
                    rgba[i + 2] = ToByte(sumB / sumA);
                    rgba[i + 3] = alpha;
                }
            }

            return new AppIconImage(size, size, rgba);
        }

        // Lo que se dibuja en un lienzo de `size` px, con el nivel que le toca (AppIconLevels.For).
        public static AppIconLevel LevelFor(AppIconVariant variant, int size)
        {
            return AppIconLevels.For(size);
        }

        // Lado de la caja de 100 de la marca en px.
        public static double MarkPixels(AppIconVariant variant, int size)
        {
            var level = LevelFor(variant, size);
            switch (variant)
            {
                case AppIconVariant.AdaptiveForeground:
                    return size * 60.0 / 108.0;
                case AppIconVariant.BootMark:
                    return size * 0.94;
                case AppIconVariant.MacPlate:
                    return AppIconLevels.MarkScale(level) * 824.0 * size / 1024.0;
                default:
                    return AppIconLevels.MarkScale(level) * size;
            }
        }

        // Placa de Windows y Android legacy: margen y radio en px enteros (00-arte §1.5; en 16: margen 1 y radio 3).
        public static int PlateMargin(int size)
        {
            return (int)DartMath.Round(size / 16.0);
        }

        public static int PlateRadius(int size)
        {
            return (int)DartMath.Round(size * 0.19);
        }

        // Distancia con signo a un rectángulo redondeado (px).
        internal static double RoundedRect(double x, double y, double left, double top, double right, double bottom, double radius)
        {
            var hx = (right - left) / 2;
            var hy = (bottom - top) / 2;
            var qx = Math.Abs(x - (left + hx)) - (hx - radius);
            var qy = Math.Abs(y - (top + hy)) - (hy - radius);
            var ox = Math.Max(qx, 0);
            var oy = Math.Max(qy, 0);
            return Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - radius;
        }

        private static byte ToByte(double value)
        {
            var rounded = DartMath.Round(value);
            return (byte)(rounded < 0 ? 0 : (rounded > 255 ? 255 : rounded));
        }

        // Fondo, marca y escala de un lienzo.
        private sealed class Frame
        {
            private readonly AppIconVariant _variant;
            private readonly IReadOnlyList<IconPiece> _pieces;
            private readonly bool _hasMark;
            private readonly double _ox;
            private readonly double _oy;
            private readonly double _k;      // unidades de la caja por px
            private readonly double _hBox;   // medio diagonal del píxel en unidades de la caja
            private readonly double _left;
            private readonly double _top;
            private readonly double _right;
            private readonly double _bottom;
            private readonly double _radius;
            private readonly double _shadowOffset;
            private readonly double _shadowBlur;
            private readonly Rgb _background;

            private Frame(AppIconVariant variant, int size)
            {
                _variant = variant;
                _background = Tokens.BgApp.Rgb;
                var level = LevelFor(variant, size);
                _hasMark = variant != AppIconVariant.AdaptiveBackground;
                _pieces = _hasMark ? AppIconGeometry.Mark(level, variant != AppIconVariant.AdaptiveForeground) : new IconPiece[0];
                var markPx = MarkPixels(variant, size);
                _k = 100.0 / markPx;
                _ox = (size - markPx) / 2;
                _oy = (size - markPx) / 2;
                _hBox = HalfDiagonal * _k;
                switch (variant)
                {
                    case AppIconVariant.Plate:
                        var margin = PlateMargin(size);
                        _left = margin;
                        _top = margin;
                        _right = size - margin;
                        _bottom = size - margin;
                        _radius = PlateRadius(size);
                        break;
                    case AppIconVariant.Round:
                        _radius = 0.46 * size;
                        break;
                    case AppIconVariant.MacPlate:
                        var unit = size / 1024.0;
                        _left = 100 * unit;
                        _top = 100 * unit;
                        _right = 924 * unit;
                        _bottom = 924 * unit;
                        _radius = 185 * unit;
                        _shadowOffset = 10 * unit;
                        _shadowBlur = 28 * unit;
                        break;
                }

                Size = size;
            }

            public int Size { get; }

            public static Frame For(AppIconVariant variant, int size)
            {
                return new Frame(variant, size);
            }

            // ¿Pasa el borde de alguna pieza (o del fondo) por el píxel de centro (x, y)?
            public bool IsEdge(double x, double y)
            {
                var background = BackgroundDistance(x, y);
                if (Math.Abs(background) <= HalfDiagonal)
                {
                    return true;
                }

                if (!_hasMark)
                {
                    return false;
                }

                var bx = (x - _ox) * _k;
                var by = (y - _oy) * _k;
                for (var i = 0; i < _pieces.Count; i++)
                {
                    var piece = _pieces[i];
                    if (piece.Shape == IconShape.RadialGlow)
                    {
                        continue;
                    }

                    if (Math.Abs(piece.SignedDistance(bx, by, _hBox)) <= _hBox)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Una muestra en (x, y) px: color 0..255 y alfa 0..1, rectos.
            public void Sample(double x, double y, out double r, out double g, out double b, out double a)
            {
                double ma = 0, mr = 0, mg = 0, mb = 0;
                if (_hasMark)
                {
                    var bx = (x - _ox) * _k;
                    var by = (y - _oy) * _k;
                    for (var i = 0; i < _pieces.Count; i++)
                    {
                        var piece = _pieces[i];
                        if (piece.Shape == IconShape.RadialGlow)
                        {
                            var glow = piece.GlowAlpha(bx, by);
                            if (glow > 0)
                            {
                                Over(piece.Color.R, piece.Color.G, piece.Color.B, glow, ref mr, ref mg, ref mb, ref ma);
                            }

                            continue;
                        }

                        if (piece.SignedDistance(bx, by, 0) > 0)
                        {
                            continue;
                        }

                        if (piece.KnockOut)
                        {
                            ma = 0;
                            continue;
                        }

                        piece.ColorAt(by, out var pr, out var pg, out var pb);
                        mr = pr;
                        mg = pg;
                        mb = pb;
                        ma = 1;
                    }
                }

                switch (_variant)
                {
                    case AppIconVariant.AdaptiveForeground:
                    case AppIconVariant.BootMark:
                        r = mr;
                        g = mg;
                        b = mb;
                        a = ma;
                        return;
                }

                if (BackgroundDistance(x, y) > 0)
                {
                    r = 0;
                    g = 0;
                    b = 0;
                    a = 0;
                    if (_variant == AppIconVariant.MacPlate)
                    {
                        // Sombra 0 10 28 negra al 30 %: la placa corrida 10 hacia abajo, desvanecida en ±28.
                        var shadow = RoundedRect(x, y - _shadowOffset, _left, _top, _right, _bottom, _radius);
                        a = 0.30 * (1 - IconMath.SmoothStep((shadow + _shadowBlur) / (2 * _shadowBlur)));
                    }

                    return;
                }

                // La marca sobre el fondo opaco.
                r = mr * ma + _background.R * (1 - ma);
                g = mg * ma + _background.G * (1 - ma);
                b = mb * ma + _background.B * (1 - ma);
                a = 1;
            }

            // Distancia con signo al borde del fondo en px (negativa adentro); a sangre, siempre adentro.
            private double BackgroundDistance(double x, double y)
            {
                switch (_variant)
                {
                    case AppIconVariant.Plate:
                    case AppIconVariant.MacPlate:
                        return RoundedRect(x, y, _left, _top, _right, _bottom, _radius);
                    case AppIconVariant.Round:
                        return IconPiece.Length(x - Size / 2.0, y - Size / 2.0) - _radius;
                    case AppIconVariant.AdaptiveForeground:
                    case AppIconVariant.BootMark:
                        return double.MaxValue;
                    default:
                        return double.MinValue;
                }
            }

            // "over" con alfa recto: el de arriba (c, alpha) sobre lo acumulado.
            private static void Over(double cr, double cg, double cb, double alpha, ref double r, ref double g, ref double b, ref double a)
            {
                var outA = alpha + a * (1 - alpha);
                if (outA <= 0)
                {
                    return;
                }

                r = (cr * alpha + r * a * (1 - alpha)) / outA;
                g = (cg * alpha + g * a * (1 - alpha)) / outA;
                b = (cb * alpha + b * a * (1 - alpha)) / outA;
                a = outA;
            }
        }
    }
}
