using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RowMate.Core.Localization;

namespace RowMate.Tests.EditMode.Localization
{
    // L10nKeys se mantiene a mano (D-05): este test es su sincronía con los catálogos (AC-APP-10-09: el código no puede
    // pedir una clave que no existe; si se la saca del catálogo, falla acá antes de que el usuario vea nada).
    [TestFixture]
    public sealed class L10nKeysTests
    {
        private static IReadOnlyList<KeyValuePair<string, string>> Constants()
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (var field in typeof(L10nKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    result.Add(new KeyValuePair<string, string>(field.Name, (string)field.GetValue(null)!));
                }
            }

            return result;
        }

        [Test, Category("AC-APP-10-09")]
        public void EveryConstantExistsInBothCatalogs()
        {
            var missing = new List<string>();
            foreach (var constant in Constants())
            {
                foreach (var l10n in LocalizationFixture.Both)
                {
                    if (!l10n.Contains(constant.Value))
                    {
                        missing.Add(l10n.LanguageCode + ": " + constant.Key + " = \"" + constant.Value + "\"");
                    }
                }
            }

            Assert.That(missing, Is.Empty, "constantes de L10nKeys sin clave en el catálogo:\n" + string.Join("\n", missing));
        }

        [Test, Category("AC-APP-10-09")]
        public void EveryCatalogKeyHasAConstantNamedInPascalCase()
        {
            var byValue = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var constant in Constants())
            {
                Assert.That(byValue.ContainsKey(constant.Value), Is.False, "valor repetido: " + constant.Value);
                byValue[constant.Value] = constant.Key;
                Assert.That(constant.Key, Is.EqualTo(char.ToUpperInvariant(constant.Value[0]) + constant.Value.Substring(1)),
                    "nombre de la constante = clave en PascalCase");
            }

            var catalog = LocalizationFixture.Catalog("es");
            var missing = new List<string>();
            foreach (var key in catalog.Keys)
            {
                if (!byValue.ContainsKey(key))
                {
                    missing.Add(key);
                }
            }

            Assert.That(missing, Is.Empty, "claves del catálogo sin constante en L10nKeys: " + string.Join(", ", missing));
            Assert.That(byValue.Count, Is.EqualTo(190));
        }

        // Ningún texto del catálogo queda vacío (el usuario nunca ve un texto vacío en lugar de uno real).
        [Test, Category("AC-APP-10-09")]
        public void NoEmptyTexts()
        {
            foreach (var language in new[] { "es", "en" })
            {
                var catalog = LocalizationFixture.Catalog(language);
                foreach (var key in catalog.Keys)
                {
                    Assert.That(catalog.Message(key), Is.Not.Empty, language + "/" + key);
                }
            }
        }
    }
}
