using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Las hojas de verificación del ícono (It. 14, spec §7: CAP-01 y CAP-02), armadas sin Unity a partir de los mismos
    // rasterizados. Los fondos claro y negro de CAP-01 y el de CAP-02 son de la hoja (verificación), no colores de la app.
    public static class AppIconSheets
    {
        private const int Pad = 32;
        private const int Magnify = 4;

        // CAP-01: el maestro 1024 y 256, 64, 48, 32, 24 y 16 a 1:1, más 32, 24 y 16 ampliados ×4 (píxel a píxel), sobre fondo
        // claro (#F2F2F2) arriba y negro abajo. renders: (tamaño → placa de Windows) de AppIconOutputs.
        public static AppIconImage Cap01(Func<int, AppIconImage> plate)
        {
            if (plate == null)
            {
                throw new ArgumentNullException(nameof(plate));
            }

            var sizes = new[] { 1024, 256, 64, 48, 32, 24, 16 };
            var magnified = new[] { 32, 24, 16 };
            var width = Pad;
            foreach (var size in sizes)
            {
                width += size + Pad;
            }

            foreach (var size in magnified)
            {
                width += size * Magnify + Pad;
            }

            var rowHeight = 1024 + 2 * Pad;
            var sheet = new Canvas(width, rowHeight * 2);
            var backgrounds = new[] { new Rgb(0xF2, 0xF2, 0xF2), new Rgb(0, 0, 0) };
            for (var row = 0; row < 2; row++)
            {
                var top = row * rowHeight;
                sheet.Fill(0, top, width, rowHeight, backgrounds[row]);
                var x = Pad;
                foreach (var size in sizes)
                {
                    sheet.Draw(plate(size), x, top + Pad, 1);
                    x += size + Pad;
                }

                foreach (var size in magnified)
                {
                    sheet.Draw(plate(size), x, top + Pad, Magnify);
                    x += size * Magnify + Pad;
                }
            }

            return sheet.ToImage();
        }

        // CAP-02: el adaptativo de Android a 432 px (108 dp × 4): las dos capas sin máscara con el visor de 72 dp, y con las
        // máscaras círculo, squircle y cuadrado redondeado sobre el visor; en todas, la zona segura de 66 dp marcada en ámbar.
        public static AppIconImage Cap02(AppIconImage background, AppIconImage foreground)
        {
            if (background == null || foreground == null)
            {
                throw new ArgumentNullException(background == null ? nameof(background) : nameof(foreground));
            }

            var size = background.Width;
            var dp = size / 108.0;
            var tiles = 4;
            var width = Pad + tiles * (size + Pad);
            var height = size + 2 * Pad;
            var sheet = new Canvas(width, height);
            var paper = new Rgb(0xF2, 0xF2, 0xF2);
            sheet.Fill(0, 0, width, height, paper);
            var ring = Tokens.MetricSplit.Rgb;
            var viewport = 72 * dp / 2;
            var safe = 66 * dp / 2;
            var center = size / 2.0;
            for (var tile = 0; tile < tiles; tile++)
            {
                var left = Pad + tile * (size + Pad);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var px = x + 0.5 - center;
                        var py = y + 0.5 - center;
                        var coverage = tile == 0 ? 1.0 : MaskCoverage(tile, px, py, viewport, dp);
                        if (coverage > 0)
                        {
                            background.Pixel(x, y, out var br, out var bg, out var bb, out var ba);
                            foreground.Pixel(x, y, out var fr, out var fg, out var fb, out var fa);
                            sheet.Blend(left + x, Pad + y, br, bg, bb, ba / 255.0 * coverage);
                            sheet.Blend(left + x, Pad + y, fr, fg, fb, fa / 255.0 * coverage);
                        }

                        var d = Math.Sqrt(px * px + py * py);
                        var line = IconMath.Clamp01(1.5 - Math.Abs(d - safe));
                        if (tile == 0)
                        {
                            var edge = Math.Abs(AppIconRaster.RoundedRect(px, py, -viewport, -viewport, viewport, viewport, 0));
                            line = Math.Max(line, IconMath.Clamp01(1.5 - edge));
                        }

                        if (line > 0)
                        {
                            sheet.Blend(left + x, Pad + y, ring.R, ring.G, ring.B, line);
                        }
                    }
                }
            }

            return sheet.ToImage();
        }

        // 1 = círculo de 72 dp, 2 = squircle (|x|⁴ + |y|⁴ = a⁴), 3 = cuadrado redondeado (radio 16 dp); borde suavizado en 1 px.
        private static double MaskCoverage(int mask, double x, double y, double half, double dp)
        {
            double sd;
            switch (mask)
            {
                case 1:
                    sd = Math.Sqrt(x * x + y * y) - half;
                    break;
                case 2:
                    var nx = x / half;
                    var ny = y / half;
                    var f = nx * nx * nx * nx + ny * ny * ny * ny;
                    sd = (Math.Sqrt(Math.Sqrt(f)) - 1) * half;
                    break;
                default:
                    sd = AppIconRaster.RoundedRect(x, y, -half, -half, half, half, 16 * dp);
                    break;
            }

            return IconMath.Clamp01(0.5 - sd);
        }

        // Un lienzo opaco donde se pegan imágenes con "over".
        private sealed class Canvas
        {
            private readonly double[] _rgb;

            public Canvas(int width, int height)
            {
                Width = width;
                Height = height;
                _rgb = new double[width * height * 3];
            }

            public int Width { get; }

            public int Height { get; }

            public void Fill(int left, int top, int width, int height, Rgb color)
            {
                for (var y = top; y < top + height; y++)
                {
                    for (var x = left; x < left + width; x++)
                    {
                        var i = (y * Width + x) * 3;
                        _rgb[i] = color.R;
                        _rgb[i + 1] = color.G;
                        _rgb[i + 2] = color.B;
                    }
                }
            }

            // La imagen en (left, top), ampliada `scale` veces (vecino más cercano).
            public void Draw(AppIconImage image, int left, int top, int scale)
            {
                for (var y = 0; y < image.Height * scale; y++)
                {
                    for (var x = 0; x < image.Width * scale; x++)
                    {
                        image.Pixel(x / scale, y / scale, out var r, out var g, out var b, out var a);
                        Blend(left + x, top + y, r, g, b, a / 255.0);
                    }
                }
            }

            public void Blend(int x, int y, double r, double g, double b, double alpha)
            {
                if (alpha <= 0)
                {
                    return;
                }

                var i = (y * Width + x) * 3;
                _rgb[i] = r * alpha + _rgb[i] * (1 - alpha);
                _rgb[i + 1] = g * alpha + _rgb[i + 1] * (1 - alpha);
                _rgb[i + 2] = b * alpha + _rgb[i + 2] * (1 - alpha);
            }

            public AppIconImage ToImage()
            {
                var rgba = new byte[Width * Height * 4];
                for (int i = 0, j = 0; i < _rgb.Length; i += 3, j += 4)
                {
                    rgba[j] = ToByte(_rgb[i]);
                    rgba[j + 1] = ToByte(_rgb[i + 1]);
                    rgba[j + 2] = ToByte(_rgb[i + 2]);
                    rgba[j + 3] = 255;
                }

                return new AppIconImage(Width, Height, rgba);
            }

            private static byte ToByte(double value)
            {
                var rounded = DartMath.Round(value);
                return (byte)(rounded < 0 ? 0 : (rounded > 255 ? 255 : rounded));
            }
        }
    }

    // Un archivo que escribe tools/icon: su ruta relativa a la raíz del repo y sus bytes.
    public sealed class AppIconOutput
    {
        public AppIconOutput(string relativePath, byte[] bytes)
        {
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public string RelativePath { get; }

        public byte[] Bytes { get; }
    }

    // Todo lo que genera tools/icon (02-tech-design §2.2), en un orden fijo. AppIconAssetsAreCurrent lo vuelve a generar en
    // memoria y lo compara byte a byte con lo versionado.
    public static class AppIconOutputs
    {
        public const string IdentityFolder = "Assets/RowMate/Art/Identity";
        public const string SnapshotsFolder = "docs/migration/iterations/14-pulido-plataformas/snapshots";

        public static IReadOnlyList<AppIconOutput> All()
        {
            var cache = new Dictionary<long, AppIconImage>();
            AppIconImage Render(AppIconVariant variant, int size)
            {
                var key = ((long)variant << 32) | (uint)size;
                if (!cache.TryGetValue(key, out var image))
                {
                    image = AppIconRaster.Render(variant, size);
                    cache[key] = image;
                }

                return image;
            }

            var outputs = new List<AppIconOutput>();
            void Add(string relative, AppIconImage image, bool withAlpha = true)
            {
                outputs.Add(new AppIconOutput(relative, image.ToPng(withAlpha)));
            }

            foreach (var size in AppIconSizes.Standalone)
            {
                Add(IdentityFolder + "/Standalone/icon-" + size + ".png", Render(AppIconVariant.Plate, size));
            }

            foreach (var size in AppIconSizes.AndroidAdaptive)
            {
                Add(IdentityFolder + "/Android/adaptive-bg-" + size + ".png", Render(AppIconVariant.AdaptiveBackground, size));
                Add(IdentityFolder + "/Android/adaptive-fg-" + size + ".png", Render(AppIconVariant.AdaptiveForeground, size));
            }

            foreach (var size in AppIconSizes.AndroidLegacy)
            {
                Add(IdentityFolder + "/Android/round-" + size + ".png", Render(AppIconVariant.Round, size));
                Add(IdentityFolder + "/Android/legacy-" + size + ".png", Render(AppIconVariant.Plate, size));
            }

            Add(IdentityFolder + "/Android/splash-mark.png", Render(AppIconVariant.BootMark, AppIconSizes.BootMark));
            Add(IdentityFolder + "/boot-mark.png", Render(AppIconVariant.BootMark, AppIconSizes.BootMark));
            Add(IdentityFolder + "/Apple/AppIcon.appiconset/icon-1024.png", Render(AppIconVariant.Bleed, AppIconSizes.Ios), false);
            for (var i = 0; i < AppIconSizes.LaunchMark.Length; i++)
            {
                Add(IdentityFolder + "/Apple/RowMateLaunchMark.imageset/mark@" + (i + 1) + "x.png", Render(AppIconVariant.BootMark, AppIconSizes.LaunchMark[i]));
            }

            foreach (var size in AppIconSizes.Mac)
            {
                Add(IdentityFolder + "/Apple/macOS/icon-" + size + ".png", Render(AppIconVariant.MacPlate, size));
            }

            Add(SnapshotsFolder + "/CAP-01.png", AppIconSheets.Cap01(size => Render(AppIconVariant.Plate, size)), false);
            Add(SnapshotsFolder + "/CAP-02.png", AppIconSheets.Cap02(Render(AppIconVariant.AdaptiveBackground, 432), Render(AppIconVariant.AdaptiveForeground, 432)), false);
            return outputs;
        }
    }
}
