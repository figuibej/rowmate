using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Water;

namespace RowMate.Core.Scene
{
    // La escena de una miniatura del selector (thumbnail_painter.dart:10-43; AC-SCN-22-04; DT8-02): 12:00, distancia de escena
    // 20 m, tiempo 0, respiración 0, sin bote, remero, efectos, boya ni degradés; de la lista visible del legacy solo los objetos
    // con z < d + 150; cielo, terreno, relleno y horizonte sí. La cámara del contrato con la tarjeta de 152 × 96 dp (apaisada:
    // horizonte al 42 %, focal = lado corto). Se dibuja a 1 824 × 1 152 y se reduce 4 × 4 → 456 × 288 (3× la tarjeta).
    public static class ThumbnailRule
    {
        public const int Version = 1;
        public const double Hour = 12, Distance = 20, SceneTime = 0, NearRange = 150;
        public const double CardWidthDp = 152, CardHeightDp = 96;
        public const int RenderWidth = 1824, RenderHeight = 1152, Downsample = 4;
        public const int Width = RenderWidth / Downsample, Height = RenderHeight / Downsample;

        // It. 6b (DT6b-19; F-SCN-22, paridad): las miniaturas siguen con el agua degradada (WaterModeSelector con Thumbnail).
        public const WaterForce Water = WaterForce.Degraded;

        public static SceneCamera Camera => new SceneCamera(CardWidthDp, CardHeightDp);

        // thumbnail_painter.dart:32-35: visibleProps(20).where(z < 20 + 150), en el mismo orden. Vacía `into`.
        public static int Filter(ReadOnlySpan<ShoreProp> visible, ShorePropBuffer into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();
            for (var i = 0; i < visible.Length; i++)
            {
                if (visible[i].Z < Distance + NearRange)
                {
                    into.Add(visible[i]);
                }
            }

            return into.Count;
        }

        public static bool Keeps(in ShoreProp prop)
        {
            return prop.Z < Distance + NearRange;
        }
    }

    // La reducción por promedio de cajas de f × f (RGBA8, filas contiguas), con redondeo al entero más cercano (mitades hacia
    // arriba): determinista, la misma en el Editor y en dotnet.
    public static class BoxDownsample
    {
        public static void Apply(byte[] source, int width, int height, int factor, byte[] destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (factor <= 0 || width % factor != 0 || height % factor != 0)
            {
                throw new ArgumentException("BoxDownsample: el ancho y el alto tienen que ser múltiplos del factor.", nameof(factor));
            }

            if (source.Length < width * height * 4)
            {
                throw new ArgumentException("BoxDownsample: la fuente es más chica que el tamaño.", nameof(source));
            }

            var w = width / factor;
            var h = height / factor;
            if (destination.Length < w * h * 4)
            {
                throw new ArgumentException("BoxDownsample: el destino es más chico que el resultado.", nameof(destination));
            }

            var n = factor * factor;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    for (var c = 0; c < 4; c++)
                    {
                        var sum = 0;
                        for (var j = 0; j < factor; j++)
                        {
                            var row = ((y * factor + j) * width + x * factor) * 4 + c;
                            for (var i = 0; i < factor; i++)
                            {
                                sum += source[row + i * 4];
                            }
                        }

                        destination[(y * w + x) * 4 + c] = (byte)((sum * 2 + n) / (2 * n));
                    }
                }
            }
        }
    }

    // La huella de las miniaturas horneadas (DT8-16): FNV-1a de 64 bits de las constantes de arte de Core (LakeArt, RiverArt,
    // CoastArt, RegattaArt, SkyArt, WaterArt, ShaderArt, MaterialArt y la tabla de la hora, en cultura invariante), del texto de los
    // shaders de la escena (con el fin de línea normalizado a LF, en el orden de su ruta) y de la versión de ThumbnailRule. El
    // horneador la escribe en thumbnails.json; un test de dotnet la recalcula y falla con "miniatura vieja: volver a hornear".
    public static class ThumbnailFingerprint
    {
        private static readonly Type[] ArtTypes =
        {
            typeof(LakeArt), typeof(RiverArt), typeof(CoastArt), typeof(RegattaArt), typeof(SkyArt), typeof(WaterArt), typeof(ShaderArt),
            typeof(MaterialArt),
        };

        // shaders: (ruta relativa con '/', texto). Se ordenan por ruta (ordinal) adentro.
        public static string Compute(IEnumerable<KeyValuePair<string, string>> shaders)
        {
            if (shaders == null)
            {
                throw new ArgumentNullException(nameof(shaders));
            }

            var sb = new StringBuilder();
            sb.Append("thumbnail-rule:").Append(ThumbnailRule.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append(ArtConstants());
            var list = new List<KeyValuePair<string, string>>(shaders);
            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            foreach (var pair in list)
            {
                sb.Append("shader:").Append(pair.Key).Append('\n');
                sb.Append(pair.Value.Replace("\r\n", "\n").Replace('\r', '\n')).Append('\n');
            }

            return Fnv1a64(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        // Las constantes de arte serializadas (nombre=valor, una por línea, en el orden del nombre).
        public static string ArtConstants()
        {
            var sb = new StringBuilder();
            foreach (var type in ArtTypes)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
                foreach (var f in fields)
                {
                    sb.Append(type.Name).Append('.').Append(f.Name).Append('=');
                    Write(sb, f.GetValue(null), 0);
                    sb.Append('\n');
                }
            }

            sb.Append("HourLookTable.Default=");
            Write(sb, HourLookTable.Default, 0);
            sb.Append('\n');
            return sb.ToString();
        }

        public static string Fnv1a64(byte[] bytes)
        {
            ulong h = 0xcbf29ce484222325UL;
            const ulong Prime = 0x100000001b3UL;
            for (var i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                unchecked
                {
                    h *= Prime;
                }
            }

            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static void Write(StringBuilder sb, object? value, int depth)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    return;
                case double d:
                    sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case float f:
                    sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case string s:
                    sb.Append('"').Append(s).Append('"');
                    return;
                case IFormattable formattable when value.GetType().IsPrimitive || value is Enum:
                    sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    return;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;
                case IEnumerable list:
                    sb.Append('[');
                    var first = true;
                    foreach (var item in list)
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        first = false;
                        Write(sb, item, depth + 1);
                    }

                    sb.Append(']');
                    return;
            }

            if (depth > 6)
            {
                sb.Append('…');
                return;
            }

            // Un struct o una clase: sus propiedades públicas de instancia, por nombre.
            var props = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(props, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            sb.Append('{');
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                sb.Append(p.Name).Append(':');
                Write(sb, p.GetValue(value), depth + 1);
                sb.Append(';');
            }

            sb.Append('}');
        }
    }
}
