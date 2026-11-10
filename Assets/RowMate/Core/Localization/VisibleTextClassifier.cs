using System;
using System.Collections.Generic;

namespace RowMate.Core.Localization
{
    // De dónde sale un texto visible (It. 14, AC-APP-11-01: categorías (a) a (e); DT14-25).
    public enum VisibleTextKind
    {
        // (a) Un mensaje del catálogo del idioma activo, con sus parámetros.
        Catalog,

        // (b) Un número, hora, fecha o duración (sin letras, o con solo unidades del catálogo).
        Number,

        // (c) Un dato del usuario (nombre de rutina, de remo o pulsómetro, de archivo o carpeta), cargado por la toma o prueba.
        UserData,

        // (d) Una marca o sigla fija: RowMate, TCX, FTMS.
        Brand,

        // (e) Las excepciones Q-03 (simulador; los textos exactos). Las de Q-04 (diagnóstico BLE) y el panel del simulador se
        // excluyen por subárbol (.rm-audit-exempt) y no llegan acá.
        Exception,

        // Cualquier otro texto: hace fallar la prueba.
        Violation,
    }

    // Lo que cada toma o prueba sabe del estado: el catálogo activo y los datos del usuario de sus semillas.
    public sealed class AuditContext
    {
        private readonly HashSet<string> _userData = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _exceptions = new HashSet<string>(StringComparer.Ordinal);

        public AuditContext(CatalogMatcher catalog, string screen = "", string state = "")
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Screen = screen ?? "";
            State = state ?? "";
        }

        public CatalogMatcher Catalog { get; }

        public string Screen { get; }

        public string State { get; }

        public string Language => Catalog.Locale;

        public IReadOnlyCollection<string> UserData => _userData;

        public AuditContext WithUserData(params string[] values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        _userData.Add(value);
                    }
                }
            }

            return this;
        }

        public bool IsUserData(string text)
        {
            return _userData.Contains(text);
        }

        // (e) Las excepciones Q-03 (los textos exactos del simulador). Las pasa quien audita (Tests/Support/AuditExceptions): Core no
        // puede llevar textos del simulador, porque viaja en el build normal (AC-BLE-13-02).
        public AuditContext WithExceptions(params string[] values)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        _exceptions.Add(value);
                    }
                }
            }

            return this;
        }

        public bool IsException(string text)
        {
            return _exceptions.Contains(text);
        }

        // "{pantalla} · {estado} · {idioma} · "{texto}"" (el mensaje de la falla).
        public string Describe(string text)
        {
            return Screen + " · " + State + " · " + Language + " · \"" + text + "\"";
        }
    }

    // El clasificador puro de la auditoría de textos (It. 14, DT14-25). Orden: vacío o sin letras → Number; Q-03 → Exception;
    // marca → Brand; dato del usuario → UserData; catálogo → Catalog; cifras con unidades del catálogo → Number; si no, Violation.
    // Los espacios de ancho cero de PathBreak y los saltos de línea de más no cuentan.
    public static class VisibleTextClassifier
    {
        public static readonly string[] Brands = { "RowMate", "TCX", "FTMS" };

        public static VisibleTextKind Classify(string text, AuditContext context)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var clean = text.Replace("​", "").Trim();
            if (!HasLetter(clean))
            {
                return VisibleTextKind.Number;
            }

            if (context.IsException(clean))
            {
                return VisibleTextKind.Exception;
            }

            if (Array.IndexOf(Brands, clean) >= 0)
            {
                return VisibleTextKind.Brand;
            }

            if (context.IsUserData(clean) || context.IsUserData(text))
            {
                return VisibleTextKind.UserData;
            }

            if (context.Catalog.Match(clean) != null || context.Catalog.Match(text.Replace("​", "")) != null)
            {
                return VisibleTextKind.Catalog;
            }

            return OnlyUnits(clean, context.Catalog) ? VisibleTextKind.Number : VisibleTextKind.Violation;
        }

        private static bool HasLetter(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsLetter(text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // Todas las palabras (tramos de letras) son unidades del catálogo, y hay al menos una cifra.
        private static bool OnlyUnits(string text, CatalogMatcher catalog)
        {
            var digit = false;
            var i = 0;
            while (i < text.Length)
            {
                if (char.IsDigit(text[i]))
                {
                    digit = true;
                }

                if (!char.IsLetter(text[i]))
                {
                    i++;
                    continue;
                }

                var start = i;
                while (i < text.Length && char.IsLetter(text[i]))
                {
                    i++;
                }

                if (!catalog.IsUnit(text.Substring(start, i - start)))
                {
                    return false;
                }
            }

            return digit;
        }
    }
}
