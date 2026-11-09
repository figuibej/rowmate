using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RowMate.Core.Export
{
    // El nombre del archivo (AC-STR-09-13, -14, -15; 02-tech-design §2.4, DT12-15):
    // RowMate_{AAAA-MM-DD}_{HHmm}[_{nombre}][_SIM]{_n}.tcx, con el inicio en la hora local de la zona de exportación.
    // - Sanitize: NFC; se conservan letras y dígitos Unicode (char.IsLetterOrDigit(string, i), que acepta pares sustitutos), '-'
    //   y '_'; todo lo demás pasa a '-'; los '-' seguidos se juntan; se quitan los '-' de los bordes; se recorta a 40 unidades
    //   UTF-16 sin cortar un par y DESPUÉS se vuelven a quitar los '-' del borde (m-03). Nunca quedan \ / : * ? " < > | ni
    //   controles. Sin nombres reservados de Windows: el archivo siempre empieza con "RowMate_".
    // - Una libre (sin nombre de rutina) usa historyFree del idioma vigente; si el saneado queda vacío, no hay "_{nombre}".
    // - Resolve agrega _2, _3, … antes de la extensión si el nombre ya existe (sin distinguir mayúsculas), hasta _999.
    public static class TcxFileName
    {
        public const string Prefix = "RowMate_";
        public const string SimulatorSuffix = "_SIM";
        public const int MaxNameLength = 40;
        public const int MaxCollisionSuffix = 999;

        public static string Sanitize(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "";
            }

            // Normalize no admite sustitutos sueltos ni U+FFFE/U+FFFF: antes pasan a '-' (igual se iban a cambiar). Si el runtime
            // rechaza otro carácter, se sigue sin normalizar (el saneado igual deja solo letras, dígitos, '-' y '_').
            var text = WithoutInvalidCodePoints(name!);
            try
            {
                text = text.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
            }

            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var pair = char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
                if (char.IsLetterOrDigit(text, i))
                {
                    builder.Append(c);
                    if (pair)
                    {
                        builder.Append(text[i + 1]);
                        i++;
                    }

                    continue;
                }

                if (pair)
                {
                    i++; // el par entero es un solo carácter que no se conserva
                }

                var kept = c == '_' ? '_' : '-';
                if (kept == '-' && builder.Length > 0 && builder[builder.Length - 1] == '-')
                {
                    continue; // los '-' seguidos se juntan
                }

                builder.Append(kept);
            }

            var trimmed = TrimDashes(builder.ToString());
            if (trimmed.Length > MaxNameLength)
            {
                var cut = MaxNameLength;
                if (char.IsHighSurrogate(trimmed[cut - 1]))
                {
                    cut--; // sin cortar un par
                }

                trimmed = TrimDashes(trimmed.Substring(0, cut));
            }

            return trimmed;
        }

        public static string Base(DateTimeOffset start, TimeZoneInfo zone, string? routineName, string freeLabel, bool simulator)
        {
            if (freeLabel == null)
            {
                throw new ArgumentNullException(nameof(freeLabel));
            }

            var local = TcxTime.ToLocal(start, zone);
            var builder = new StringBuilder(Prefix.Length + 16 + MaxNameLength + 8);
            builder.Append(Prefix);
            TcxTime.Pad(builder, local.Year, 4);
            builder.Append('-');
            TcxTime.Pad(builder, local.Month, 2);
            builder.Append('-');
            TcxTime.Pad(builder, local.Day, 2);
            builder.Append('_');
            TcxTime.Pad(builder, local.Hour, 2);
            TcxTime.Pad(builder, local.Minute, 2);
            var name = Sanitize(routineName ?? freeLabel);
            if (name.Length > 0)
            {
                builder.Append('_').Append(name);
            }

            if (simulator)
            {
                builder.Append(SimulatorSuffix);
            }

            return builder.ToString();
        }

        // El primer nombre libre: base + extensión, o base_2 + extensión, … (sin distinguir mayúsculas, también en Android).
        public static string Resolve(string baseName, string extension, IEnumerable<string> taken)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            var used = new HashSet<string>(taken ?? throw new ArgumentNullException(nameof(taken)), StringComparer.OrdinalIgnoreCase);
            var candidate = baseName + extension;
            if (!used.Contains(candidate))
            {
                return candidate;
            }

            for (var n = 2; n <= MaxCollisionSuffix; n++)
            {
                candidate = baseName + "_" + n.ToString(CultureInfo.InvariantCulture) + extension;
                if (!used.Contains(candidate))
                {
                    return candidate;
                }
            }

            throw new IOException("TcxFileName: no queda ningún nombre libre para " + baseName + extension + ".");
        }

        private static string TrimDashes(string text)
        {
            return text.Trim('-');
        }

        private static string WithoutInvalidCodePoints(string text)
        {
            StringBuilder? builder = null;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    builder?.Append(c).Append(text[i + 1]);
                    i++;
                    continue;
                }

                if (char.IsSurrogate(c) || c >= (char)0xFFFE)
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(text.Length);
                        builder.Append(text, 0, i);
                    }

                    builder.Append('-');
                    continue;
                }

                builder?.Append(c);
            }

            return builder == null ? text : builder.ToString();
        }
    }
}
