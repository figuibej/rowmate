using System;
using System.Globalization;

namespace RowMate.Tests.UiSupport
{
    // Tamaños de pantalla de las pruebas y las capturas (01-po-spec §7.2): px y densidad; los dp = px / densidad. Cada
    // captura representa el tamaño en dp con su densidad, sin depender del monitor de la PC (spec §12.2).
    public readonly struct DeviceProfile
    {
        public static readonly DeviceProfile TV = new DeviceProfile("TV", 1080, 2400, 2.625, true);     // 411 × 914 dp, teléfono
        public static readonly DeviceProfile TH = new DeviceProfile("TH", 2400, 1080, 2.625, true);     // 914 × 411, teléfono
        public static readonly DeviceProfile L640 = new DeviceProfile("L640", 640, 360, 1);
        public static readonly DeviceProfile P800 = new DeviceProfile("P800", 800, 360, 1);
        public static readonly DeviceProfile W1280 = new DeviceProfile("W1280", 1280, 720, 1);

        public DeviceProfile(string code, int widthPx, int heightPx, double density, bool phone = false)
        {
            if (widthPx <= 0 || heightPx <= 0 || !(density > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(widthPx), "DeviceProfile: tamaño o densidad inválidos.");
            }

            Code = code;
            WidthPx = widthPx;
            HeightPx = heightPx;
            Density = density;
            IsPhone = phone;
        }

        public string Code { get; }

        // TV y TH son el teléfono de la spec (§7.2); L640, P800 y W1280, ventanas de escritorio. Las capturas arman la UI de
        // teléfono (UiHostOptions.IsDesktop = false: sin barra de desplazamiento ni :hover, QA-V2-13) con los perfiles de teléfono.
        public bool IsPhone { get; }

        public int WidthPx { get; }

        public int HeightPx { get; }

        public double Density { get; }

        public double WidthDp => WidthPx / Density;

        public double HeightDp => HeightPx / Density;

        // 800 × 600 dp de LT-28, etc. (px redondeados; con densidad 1, exactos).
        public static DeviceProfile Dp(double widthDp, double heightDp, double density = 1)
        {
            return new DeviceProfile(widthDp.ToString("0.##", CultureInfo.InvariantCulture) + "x" + heightDp.ToString("0.##", CultureInfo.InvariantCulture),
                (int)Math.Round(widthDp * density), (int)Math.Round(heightDp * density), density);
        }

        public override string ToString()
        {
            return Code + " " + WidthPx + "×" + HeightPx + " px @" + Density.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
