using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Core.Export
{
    // El registro de la exportación (DT12-19, AC-STR-09-21) con el tag EXPORT y SIN rutas del equipo:
    //   Info    "TCX guardado: {archivo} en {etiqueta}"
    //   Warning "no se pudo exportar: {Tipo}: {mensaje}"
    // Scrub cambia cada ruta conocida por su etiqueta (la carpeta real → la etiqueta de la carpeta; la carpeta del usuario →
    // %USERPROFILE%) antes de registrar: sin distinguir mayúsculas y de la más larga a la más corta.
    public static class ExportLog
    {
        public const string Tag = "EXPORT";
        public const string UserProfileLabel = "%USERPROFILE%";

        public static string Saved(string fileName, string folderLabel)
        {
            return "TCX guardado: " + fileName + " en " + folderLabel;
        }

        public static string Failed(Exception error, IReadOnlyList<KeyValuePair<string, string>> replacements)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return "no se pudo exportar: " + error.GetType().Name + ": " + Scrub(error.Message ?? "", replacements);
        }

        public static string Scrub(string message, IReadOnlyList<KeyValuePair<string, string>> replacements)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (replacements == null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }

            var ordered = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < replacements.Count; i++)
            {
                var pair = replacements[i];
                if (!string.IsNullOrEmpty(pair.Key))
                {
                    ordered.Add(pair);
                }
            }

            // Del más largo al más corto: una carpeta de adentro se cambia antes que la del usuario que la contiene.
            ordered.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            var text = message;
            for (var i = 0; i < ordered.Count; i++)
            {
                text = ReplaceIgnoreCase(text, ordered[i].Key, ordered[i].Value ?? "");
                // La misma ruta con la otra barra (un mensaje de .NET puede traer / en Windows o \ en otra plataforma).
                var other = ordered[i].Key.IndexOf('\\') >= 0 ? ordered[i].Key.Replace('\\', '/') : ordered[i].Key.Replace('/', '\\');
                if (other != ordered[i].Key)
                {
                    text = ReplaceIgnoreCase(text, other, ordered[i].Value ?? "");
                }
            }

            return text;
        }

        private static string ReplaceIgnoreCase(string text, string find, string replacement)
        {
            var index = text.IndexOf(find, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            var from = 0;
            while (index >= 0)
            {
                builder.Append(text, from, index - from).Append(replacement);
                from = index + find.Length;
                index = text.IndexOf(find, from, StringComparison.OrdinalIgnoreCase);
            }

            builder.Append(text, from, text.Length - from);
            return builder.ToString();
        }
    }
}
