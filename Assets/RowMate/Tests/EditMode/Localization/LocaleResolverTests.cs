using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;

namespace RowMate.Tests.EditMode.Localization
{
    // F-APP-09: idioma elegido con la lista de idiomas del sistema, con el algoritmo basicLocaleListResolution de
    // Flutter 3.47.6 (widgets/app.dart:146-238) y supportedLocales = [en, es] (legacy: lib/main.dart:103-106). D-06.
    [TestFixture]
    public sealed class LocaleResolverTests
    {
        private static AppLanguage Resolve(string list)
        {
            return LocaleResolver.Resolve(Split(list));
        }

        private static IReadOnlyList<string> Split(string list)
        {
            if (list.Length == 0)
            {
                return new string[0];
            }

            var parts = list.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return parts;
        }

        // AC-APP-09-01: cualquier variante de español usa el español y cualquier variante de inglés, el inglés.
        [TestCase("es", AppLanguage.Es)]
        [TestCase("es-AR", AppLanguage.Es)]
        [TestCase("es-ES", AppLanguage.Es)]
        [TestCase("es-MX", AppLanguage.Es)]
        [TestCase("es-419", AppLanguage.Es)]
        [TestCase("es_AR", AppLanguage.Es)]
        [TestCase("ES-ar", AppLanguage.Es)]
        [TestCase("es-Latn-AR", AppLanguage.Es)]
        [TestCase("es_AR.UTF-8", AppLanguage.Es)]
        [TestCase("en", AppLanguage.En)]
        [TestCase("en-US", AppLanguage.En)]
        [TestCase("en-GB", AppLanguage.En)]
        [TestCase("en-AU", AppLanguage.En)]
        [TestCase("en-IN", AppLanguage.En)]
        [Category("AC-APP-09-01")]
        public void VariantsMapToLanguage(string tag, AppLanguage expected)
        {
            Assert.That(Resolve(tag), Is.EqualTo(expected));
        }

        // AC-APP-09-02: las 17 filas de la spec, incluidas las dos † (la entrada siguiente exacta "es"/"en" gana).
        [TestCase("", AppLanguage.En)]
        [TestCase("es-AR", AppLanguage.Es)]
        [TestCase("es-419", AppLanguage.Es)]
        [TestCase("es", AppLanguage.Es)]
        [TestCase("en-US", AppLanguage.En)]
        [TestCase("pt-BR", AppLanguage.En)]
        [TestCase("fr-FR, es-AR", AppLanguage.Es)]
        [TestCase("ca-ES, es-ES", AppLanguage.Es)]
        [TestCase("pt-BR, en-GB, es-ES", AppLanguage.En)]
        [TestCase("en-US, es-AR", AppLanguage.En)]
        [TestCase("es-AR, en-US", AppLanguage.Es)]
        [TestCase("es-AR, en", AppLanguage.Es)]
        [TestCase("es-AR, es-ES", AppLanguage.Es)]
        [TestCase("fr-FR, es-AR, pt-PT, en", AppLanguage.Es)]
        [TestCase("zh-Hans-CN, ja-JP", AppLanguage.En)]
        [TestCase("fr-FR, es-AR, en", AppLanguage.En)]
        [TestCase("fr-FR, en-GB, es", AppLanguage.Es)]
        [Category("AC-APP-09-02")]
        public void FlutterParityTable(string systemList, AppLanguage expected)
        {
            Assert.That(Resolve(systemList), Is.EqualTo(expected), "lista del sistema: [" + systemList + "]");
        }

        // Más casos del algoritmo de Flutter: la primera entrada difiere si la siguiente tiene el mismo idioma.
        [TestCase("es-AR, es", AppLanguage.Es)]
        [TestCase("en-US, en, es", AppLanguage.En)]
        [TestCase("en-US, en-GB, es", AppLanguage.En)]
        [TestCase("fr-FR, de-DE, es-AR", AppLanguage.Es)]
        [TestCase("fr-FR, en-GB, es-ES", AppLanguage.En)]
        [TestCase("es-AR, fr-FR, en", AppLanguage.Es)]
        [Category("AC-APP-09-02")]
        public void FlutterAlgorithmDetails(string systemList, AppLanguage expected)
        {
            Assert.That(Resolve(systemList), Is.EqualTo(expected), "lista del sistema: [" + systemList + "]");
        }

        // AC-APP-09-03: sin español ni inglés en la lista, o sin lista, inglés (el primer soportado y la plantilla).
        [Test, Category("AC-APP-09-03")]
        public void FallbackIsEnglish()
        {
            Assert.That(LocaleResolver.Resolve(null), Is.EqualTo(AppLanguage.En));
            Assert.That(LocaleResolver.Resolve(new string[0]), Is.EqualTo(AppLanguage.En));
            Assert.That(Resolve("pt-BR"), Is.EqualTo(AppLanguage.En));
            Assert.That(Resolve("fr-FR"), Is.EqualTo(AppLanguage.En));
            Assert.That(Resolve("zh-Hans-CN"), Is.EqualTo(AppLanguage.En));
            Assert.That(Resolve("pt-BR, fr-FR, de-DE, ja-JP"), Is.EqualTo(AppLanguage.En));
            Assert.That(LocaleResolver.Resolve(new[] { "", "x-klingon", "und" }), Is.EqualTo(AppLanguage.En));
            Assert.That(LocaleResolver.Resolve(new string[] { null!, "es-AR" }), Is.EqualTo(AppLanguage.Es), "una entrada nula no coincide con nada");
        }

        // AC-APP-09-06: se usa la LISTA completa del sistema (ISystemLocale), no solo el idioma principal.
        [Test, Category("AC-APP-09-06")]
        public void UsesWholeList()
        {
            ISystemLocale system = new FakeSystemLocale("fr-FR", "es-AR");
            Assert.That(LocaleResolver.Resolve(system.PreferredLanguageTags), Is.EqualTo(AppLanguage.Es));
            Assert.That(LocaleResolver.Resolve(new[] { system.PreferredLanguageTags[0] }), Is.EqualTo(AppLanguage.En),
                "solo con el principal daría inglés: por eso se pasa la lista");
            Assert.That(new LanguageState(system.PreferredLanguageTags, NullAppLogger.Instance).Current.Value, Is.EqualTo(AppLanguage.Es));
        }

        [Test, Category("AC-APP-09-06")]
        [Ignore("NO VERIFICABLE → It. 2: [Device] lista nativa en Android (LocaleList) y Windows (GlobalizationPreferences); [Apple] → It. 10b")]
        public void NativeListOnDevices()
        {
        }

        [TestCase("es", "es", null, null)]
        [TestCase("es-AR", "es", null, "AR")]
        [TestCase("es_419", "es", null, "419")]
        [TestCase("zh-Hans-CN", "zh", "Hans", "CN")]
        [TestCase("zh-hant-tw", "zh", "Hant", "TW")]
        [TestCase("sr-Latn", "sr", "Latn", null)]
        [TestCase("en-US-u-ca-gregory", "en", null, "US")]
        [TestCase("de-DE-x-private", "de", null, "DE")]
        [TestCase("ca_ES@valencia", "ca", null, "ES")]
        [TestCase("en_US.UTF-8", "en", null, "US")]
        [TestCase("", "", null, null)]
        [Category("AC-APP-09-01")]
        public void LanguageTagParts(string tag, string language, string? script, string? region)
        {
            var parsed = LanguageTag.Parse(tag);
            Assert.That(parsed.Language, Is.EqualTo(language));
            Assert.That(parsed.Script, Is.EqualTo(script));
            Assert.That(parsed.Region, Is.EqualTo(region));
        }

        private sealed class FakeSystemLocale : ISystemLocale
        {
            public FakeSystemLocale(params string[] tags)
            {
                PreferredLanguageTags = tags;
            }

            public IReadOnlyList<string> PreferredLanguageTags { get; }
        }
    }
}
