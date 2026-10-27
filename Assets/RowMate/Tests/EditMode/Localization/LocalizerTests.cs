using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Localization;

namespace RowMate.Tests.EditMode.Localization
{
    // F-APP-10: textos con parámetros y plurales de los catálogos reales; clave inexistente = error visible en los tests.
    [TestFixture]
    public sealed class LocalizerTests
    {
        private static Localizer Es => LocalizationFixture.Es;

        private static Localizer En => LocalizationFixture.En;

        // AC-APP-10-05.
        [Test, Category("AC-APP-10-05")]
        public void TextsWithParameters()
        {
            Assert.That(Es.Format(L10nKeys.HrmConnectingTo, MessageArg.Text("name", "Polar H10")), Is.EqualTo("Conectando a Polar H10..."));
            Assert.That(En.Format(L10nKeys.HrmConnectingTo, MessageArg.Text("name", "Polar H10")), Is.EqualTo("Connecting to Polar H10..."));
            Assert.That(Es.Format(L10nKeys.HrmConnectError, MessageArg.Text("error", "timeout")), Is.EqualTo("Error al conectar: timeout"));
            Assert.That(En.Format(L10nKeys.HrmConnectError, MessageArg.Text("error", "timeout")), Is.EqualTo("Connection error: timeout"));
            Assert.That(Es.Format(L10nKeys.RoutineDeleteContent, MessageArg.Text("name", "Escalera SPM")), Is.EqualTo("¿Eliminar \"Escalera SPM\"?"));
            Assert.That(En.Format(L10nKeys.RoutineDeleteContent, MessageArg.Text("name", "Escalera SPM")), Is.EqualTo("Delete \"Escalera SPM\"?"));
            // Sin plural, como el legacy.
            Assert.That(Es.Format(L10nKeys.EditorStepsCount, MessageArg.Number("count", 1)), Is.EqualTo("Pasos (1)"));
            Assert.That(En.Format(L10nKeys.EditorStepsCount, MessageArg.Number("count", 1)), Is.EqualTo("Steps (1)"));
            Assert.That(Es.Format(L10nKeys.EditorStepsCount, MessageArg.Number("count", 3)), Is.EqualTo("Pasos (3)"));
            Assert.That(En.Format(L10nKeys.EditorStepsCount, MessageArg.Number("count", 3)), Is.EqualTo("Steps (3)"));
            Assert.That(Es.Get(L10nKeys.WorkoutOrPickRoutine), Is.EqualTo("  O elegí una rutina:"));
            Assert.That(En.Get(L10nKeys.ProfileDisconnectContent), Is.EqualTo("Your sessions won't be uploaded automatically. Continue?"));
        }

        // AC-APP-10-06: one = exactamente 1; other = todo lo demás, incluido 0.
        [TestCase(0, "0 sesiones sincronizadas", "0 sessions synced")]
        [TestCase(1, "1 sesión sincronizada", "1 session synced")]
        [TestCase(2, "2 sesiones sincronizadas", "2 sessions synced")]
        [Category("AC-APP-10-06")]
        public void Plurals(int count, string es, string en)
        {
            Assert.That(Es.Format(L10nKeys.ProfileSyncResult, MessageArg.Number("count", count)), Is.EqualTo(es));
            Assert.That(En.Format(L10nKeys.ProfileSyncResult, MessageArg.Number("count", count)), Is.EqualTo(en));
        }

        [TestCase(1, "1 sesión subida", "1 session uploaded")]
        [TestCase(3, "3 sesiones subidas", "3 sessions uploaded")]
        [TestCase(0, "0 sesiones subidas", "0 sessions uploaded")]
        [Category("AC-APP-10-06")]
        public void UploadPlurals(int count, string es, string en)
        {
            Assert.That(Es.Format(L10nKeys.ProfileUploadResult, MessageArg.Number("count", count)), Is.EqualTo(es));
            Assert.That(En.Format(L10nKeys.ProfileUploadResult, MessageArg.Number("count", count)), Is.EqualTo(en));
        }

        // AC-APP-10-09: una clave que no está en el catálogo hace fallar el test (nunca un texto vacío ni el nombre de la clave).
        [Test, Category("AC-APP-10-09")]
        public void MissingKeyThrows()
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(l10n.Contains("claveQueNoExiste"), Is.False);
                var get = Assert.Throws<MissingTranslationException>(() => l10n.Get("claveQueNoExiste"))!;
                Assert.That(get.Key, Is.EqualTo("claveQueNoExiste"));
                Assert.That(get.LanguageCode, Is.EqualTo(l10n.LanguageCode));
                Assert.That(get, Is.InstanceOf<KeyNotFoundException>());
                Assert.Throws<MissingTranslationException>(() => l10n.Format("claveQueNoExiste", MessageArg.Number("count", 1)));
                Assert.Throws<ArgumentNullException>(() => l10n.Get(null!));
            }
        }

        // Tipos y nombres exactos (AC-APP-10-04): un parámetro de otro tipo o con otro nombre es un error de programación.
        [Test, Category("AC-APP-10-04")]
        public void ArgumentsMustMatchMetadata()
        {
            Assert.Throws<ArgumentException>(() => Es.Get(L10nKeys.HrmConnectingTo), "Get de una clave con parámetros");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.HrmConnectingTo), "sin argumentos");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.HrmConnectingTo, MessageArg.Number("name", 1)), "int en un String");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.EditorStepsCount, MessageArg.Text("count", "1")), "String en un int");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.HrmConnectingTo, MessageArg.Text("nombre", "x")), "otro nombre");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.StepDurationMinSec, MessageArg.Number("m", 1), MessageArg.Number("m", 2)), "repetido");
            Assert.Throws<ArgumentException>(() => Es.Format(L10nKeys.NavDevice, MessageArg.Text("x", "y")), "argumentos de más");
            Assert.That(Es.Format(L10nKeys.NavDevice), Is.EqualTo("Dispositivo"), "Format sin argumentos de una clave sin parámetros");
            Assert.That(Es.LanguageCode, Is.EqualTo("es"));
            Assert.That(En.LanguageCode, Is.EqualTo("en"));
            Assert.That(LocalizationFixture.Set.For(AppLanguage.Es), Is.SameAs(Es));
            Assert.That(LocalizationFixture.Set.For(AppLanguage.En), Is.SameAs(En));
        }

        // El catálogo valida al cargar (02-tech-design §3.3): metadatos y mensajes tienen que coincidir.
        [TestCase("{\"@@locale\": \"es\", \"a\": \"{x}\"}", TestName = "Catalog argumento sin metadato")]
        [TestCase("{\"@@locale\": \"es\", \"a\": \"hola\", \"@a\": {\"placeholders\": {\"x\": {\"type\": \"String\"}}}}", TestName = "Catalog metadato sin argumento")]
        [TestCase("{\"@@locale\": \"es\", \"a\": \"{n, plural, other{#}}\", \"@a\": {\"placeholders\": {\"n\": {\"type\": \"String\"}}}}", TestName = "Catalog plural que no es int")]
        [TestCase("{\"@@locale\": \"es\", \"a\": \"{x}\", \"@a\": {\"placeholders\": {\"x\": {\"type\": \"double\"}}}}", TestName = "Catalog tipo no admitido")]
        [TestCase("{\"@@locale\": \"es\", \"a\": \"x\", \"a\": \"y\"}", TestName = "Catalog clave repetida")]
        [TestCase("{\"@@locale\": \"es\", \"@b\": {}}", TestName = "Catalog metadatos de una clave inexistente")]
        [TestCase("{\"a\": \"x\"}", TestName = "Catalog sin locale")]
        [TestCase("{\"@@locale\": \"es\", \"a\": \"hola }\"}", TestName = "Catalog mensaje inválido")]
        [TestCase("{\"@@locale\": \"es\", \"a\": 3}", TestName = "Catalog mensaje que no es texto")]
        [TestCase("{\"@@locale\": \"es\", \"a-b\": \"x\"}", TestName = "Catalog clave inválida")]
        [Category("AC-APP-10-04")]
        public void InvalidCatalogFailsAtLoad(string json)
        {
            Assert.Throws<FormatException>(() => LocalizationCatalog.Parse(json));
        }

        [Test, Category("AC-APP-10-01")]
        public void SetRejectsCatalogsWithDifferentKeysOrParameters()
        {
            const string es = "{\"@@locale\": \"es\", \"a\": \"x\", \"b\": \"{n}\", \"@b\": {\"placeholders\": {\"n\": {\"type\": \"int\"}}}}";
            const string enMissing = "{\"@@locale\": \"en\", \"a\": \"x\"}";
            const string enOtherType = "{\"@@locale\": \"en\", \"a\": \"x\", \"b\": \"{n}\", \"@b\": {\"placeholders\": {\"n\": {\"type\": \"String\"}}}}";
            const string enOk = "{\"@@locale\": \"en\", \"a\": \"y\", \"b\": \"n={n}\", \"@b\": {\"placeholders\": {\"n\": {\"type\": \"int\"}}}}";
            Assert.Throws<FormatException>(() => LocalizationSet.Load(es, enMissing));
            Assert.Throws<FormatException>(() => LocalizationSet.Load(enMissing.Replace("\"en\"", "\"es\""), enOk));
            Assert.Throws<FormatException>(() => LocalizationSet.Load(es, enOtherType));
            Assert.Throws<FormatException>(() => LocalizationSet.Load(enOk, es), "los @@locale cruzados");
            var set = LocalizationSet.Load(es, enOk);
            Assert.That(set.En.Format("b", MessageArg.Number("n", 3)), Is.EqualTo("n=3"));
        }
    }
}
