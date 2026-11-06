using System;
using System.Globalization;

namespace RowMate.Core.Scene
{
    // Un color sRGB con canales double en [0; 1], como el Color de Flutter del legacy (06a-escena-lago/02-tech-design §2.4): un
    // hex de 8 bits se lee como byte/255.0 y no se redondea después (painting.dart: Color._fromRGBOC). Lerp es Color.lerp del
    // SDK que corre el legacy: clampDouble(a·(1 − t) + b·t; 0; 1) por canal (painting.dart:424-455, lerp.dart:28-31;
    // QA-6a-PAR-06). Opaco siempre: el alfa que dibuja cada pieza va aparte.
    public readonly struct SceneColor : IEquatable<SceneColor>
    {
        public static readonly SceneColor White = new SceneColor(1, 1, 1);
        public static readonly SceneColor Black = new SceneColor(0, 0, 0);

        public SceneColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        public double R { get; }

        public double G { get; }

        public double B { get; }

        // Color(0xAARRGGBB) del legacy: el alfa se ignora (todos los de la escena son opacos).
        public static SceneColor FromArgb(uint argb)
        {
            return new SceneColor(((argb >> 16) & 0xFF) / 255.0, ((argb >> 8) & 0xFF) / 255.0, (argb & 0xFF) / 255.0);
        }

        // "#RRGGBB" (pruebas y herramientas; el código de producción usa FromArgb, ADR 016).
        public static SceneColor FromHex(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length != 7 || hex[0] != '#'
                || !uint.TryParse(hex.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
            {
                throw new FormatException("SceneColor: se esperaba #RRGGBB y llegó \"" + hex + "\".");
            }

            return FromArgb(0xFF000000u | rgb);
        }

        // Color.lerp del Flutter del legacy, canal por canal, recortado a [0; 1].
        public static SceneColor Lerp(in SceneColor a, in SceneColor b, double t)
        {
            return new SceneColor(Clamp01(a.R * (1.0 - t) + b.R * t), Clamp01(a.G * (1.0 - t) + b.G * t), Clamp01(a.B * (1.0 - t) + b.B * t));
        }

        // EOTF sRGB (lo que recibe el shader en un proyecto lineal).
        public SceneColor ToLinear()
        {
            return new SceneColor(ToLinear(R), ToLinear(G), ToLinear(B));
        }

        public static double ToLinear(double c)
        {
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        // Luminancia relativa de un color ya lineal (Rec. 709).
        public double LinearLuminance()
        {
            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }

        // "#RRGGBB" redondeado al byte más cercano (solo para mensajes y comparaciones con las tablas del Designer).
        public string ToHex()
        {
            return "#" + Byte(R).ToString("X2", CultureInfo.InvariantCulture) + Byte(G).ToString("X2", CultureInfo.InvariantCulture)
                + Byte(B).ToString("X2", CultureInfo.InvariantCulture);
        }

        public bool Equals(SceneColor other)
        {
            return BitConverter.DoubleToInt64Bits(R) == BitConverter.DoubleToInt64Bits(other.R)
                && BitConverter.DoubleToInt64Bits(G) == BitConverter.DoubleToInt64Bits(other.G)
                && BitConverter.DoubleToInt64Bits(B) == BitConverter.DoubleToInt64Bits(other.B);
        }

        public override bool Equals(object? obj)
        {
            return obj is SceneColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (BitConverter.DoubleToInt64Bits(R).GetHashCode() * 397 ^ BitConverter.DoubleToInt64Bits(G).GetHashCode()) * 397
                    ^ BitConverter.DoubleToInt64Bits(B).GetHashCode();
            }
        }

        public override string ToString()
        {
            return ToHex() + " (" + R.ToString("R", CultureInfo.InvariantCulture) + "; " + G.ToString("R", CultureInfo.InvariantCulture)
                + "; " + B.ToString("R", CultureInfo.InvariantCulture) + ")";
        }

        // clampDouble de Flutter: los extremos, y el valor tal cual adentro.
        internal static double Clamp01(double x)
        {
            if (x < 0.0)
            {
                return 0.0;
            }

            return x > 1.0 ? 1.0 : x;
        }

        private static int Byte(double c)
        {
            var v = (int)Math.Floor(Clamp01(c) * 255.0 + 0.5);
            return v > 255 ? 255 : v;
        }
    }
}
