using System.Linq;
using NUnit.Framework;
using RowMate.Core.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Localization
{
    // CM · El emparejador del catálogo de la auditoría de textos (It. 14, AC-APP-11-01 (a); DT14-25).
    [TestFixture]
    public sealed class CatalogMatcherTests
    {
        private static readonly CatalogMatcher Es = new CatalogMatcher(LocalizationFixture.Catalog("es"));
        private static readonly CatalogMatcher En = new CatalogMatcher(LocalizationFixture.Catalog("en"));

        [Test, Category("AC-APP-11-01")]
        public void LiteralsAndParameters()
        {
            Assert.That(Es.Match("Dispositivo"), Is.EqualTo("navDevice"));
            Assert.That(Es.Match("Reconectando con AMS-670B RowMaster 2024…"), Is.EqualTo("deviceReconnecting"));
            Assert.That(Es.Match("Guardado en Documentos\\RowMate\\TCX"), Is.EqualTo("exportTcxSaved"));
            Assert.That(Es.Match("Asegurate que el rower esté encendido\ny el Bluetooth activado"), Is.EqualTo("deviceNoResultsHint"), "con salto de línea");
            Assert.That(Es.Match("Paso 2 de 5: Trabajo"), Is.Null, "a11y*: solo lo oye el lector, no es un texto visible");
            Assert.That(Es.Match("Cualquier cosa: con dos puntos"), Is.Null, "a11yLabelValue no describe todo lo que tiene dos puntos");
            Assert.That(Es.Match("45s"), Is.EqualTo("stepDurationSec"), "{s} es entero");
            Assert.That(Es.Match("Watios"), Is.Null);
            Assert.That(Es.Match("Dispositivos"), Is.Null, "el texto entero, no un prefijo");
        }

        [Test, Category("AC-APP-11-01")]
        public void PluralVariantsExpand()
        {
            Assert.That(Es.Match("1 paso"), Is.EqualTo("routineSummarySteps"));
            Assert.That(Es.Match("12 pasos"), Is.EqualTo("routineSummarySteps"));
            Assert.That(Es.Match("3 series · 1 descanso"), Is.EqualTo("routineSummaryWorkRest"));
            Assert.That(CatalogMatcher.ToPattern("{count, plural, one{# paso} other{# pasos}}"), Is.EqualTo(@"(?:-?\d+\ paso|-?\d+\ pasos)"));
            Assert.That(CatalogMatcher.ToPattern("{a}: {b}"), Is.EqualTo("(?:.+?):\\ (?:.+?)"));
        }

        [Test, Category("AC-APP-11-01")]
        public void OtherLanguageDoesNotMatch()
        {
            Assert.That(En.Match("Dispositivo"), Is.Null, "en inglés, un texto en español es una violación");
            Assert.That(En.Match("Device"), Is.EqualTo("navDevice"));
            Assert.That(Es.Match("Up next:"), Is.Null);
        }

        [Test, Category("AC-APP-11-01")]
        public void UnitTokensComeFromTheCatalog()
        {
            Assert.That(Es.IsUnit("W"), Is.True, "{avg} / {target} W");
            Assert.That(Es.IsUnit("de"), Is.False, "Paso {current} de {total}: une dos parámetros");
            Assert.That(Es.IsUnit("a"), Is.False);
            Assert.That(Es.UnitTokens.All(u => u.Length <= 4), Is.True, string.Join(", ", Es.UnitTokens));
        }
    }

    // VC · El clasificador de textos visibles (It. 14, AC-APP-11-01: categorías (a) a (e)).
    [TestFixture]
    public sealed class VisibleTextClassifierTests
    {
        private static AuditContext Context(string language)
        {
            return new AuditContext(new CatalogMatcher(LocalizationFixture.Catalog(language)), "Dispositivo", "conectado").WithExceptions(AuditExceptions.Simulator)
                .WithUserData("Escalera", "AMS-670B RowMaster 2024", "rowmate-2026-11-09.tcx");
        }

        [Test, Category("AC-APP-11-01")]
        public void EachCategory()
        {
            var es = Context("es");
            Assert.That(VisibleTextClassifier.Classify("Historial", es), Is.EqualTo(VisibleTextKind.Catalog));
            Assert.That(VisibleTextClassifier.Classify("02:13", es), Is.EqualTo(VisibleTextKind.Number));
            Assert.That(VisibleTextClassifier.Classify("1.234", es), Is.EqualTo(VisibleTextKind.Number));
            Assert.That(VisibleTextClassifier.Classify("120 W", es), Is.EqualTo(VisibleTextKind.Number));
            Assert.That(VisibleTextClassifier.Classify("", es), Is.EqualTo(VisibleTextKind.Number));
            Assert.That(VisibleTextClassifier.Classify("Escalera", es), Is.EqualTo(VisibleTextKind.UserData));
            Assert.That(VisibleTextClassifier.Classify("RowMate", es), Is.EqualTo(VisibleTextKind.Brand));
            Assert.That(VisibleTextClassifier.Classify("FTMS", es), Is.EqualTo(VisibleTextKind.Brand));
            Assert.That(VisibleTextClassifier.Classify("SIMULADOR", es), Is.EqualTo(VisibleTextKind.Exception));
            Assert.That(VisibleTextClassifier.Classify("Hora escena", es), Is.EqualTo(VisibleTextKind.Exception));
            Assert.That(VisibleTextClassifier.Classify("Lorem ipsum", es), Is.EqualTo(VisibleTextKind.Violation));
            Assert.That(VisibleTextClassifier.Classify("120 Watios", es), Is.EqualTo(VisibleTextKind.Violation), "palabras que no son unidades");
        }

        [Test, Category("AC-APP-11-01")]
        public void SpanishInEnglishIsAViolation()
        {
            var en = Context("en");
            Assert.That(VisibleTextClassifier.Classify("Terminar entrenamiento", en), Is.EqualTo(VisibleTextKind.Violation));
            Assert.That(VisibleTextClassifier.Classify("SIMULADOR", en), Is.EqualTo(VisibleTextKind.Exception), "Q-03: queda en español");
            Assert.That(VisibleTextClassifier.Classify("Device", en), Is.EqualTo(VisibleTextKind.Catalog));
        }

        [Test, Category("AC-APP-11-01")]
        public void PathBreaksDoNotCount()
        {
            var es = Context("es");
            Assert.That(VisibleTextClassifier.Classify("Guardado en Documentos\\​RowMate\\​TCX", es), Is.EqualTo(VisibleTextKind.Catalog));
            Assert.That(es.Describe("x"), Is.EqualTo("Dispositivo · conectado · es · \"x\""));
        }
    }
}
