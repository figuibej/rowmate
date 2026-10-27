using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Design;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // AC-BASE-02 (Q-06): los textos no dependen de la región del equipo (coma decimal, punto de miles, signo menos U+2212).
    [TestFixture]
    public sealed class CultureIndependenceTests
    {
        [TestCase("es-AR", Category = "AC-BASE-02")]
        [TestCase("de-DE", Category = "AC-BASE-02")]
        [TestCase("fr-FR", Category = "AC-BASE-02")]
        [TestCase("sv-SE", Category = "AC-BASE-02")]
        [TestCase("en-US", Category = "AC-BASE-02")]
        public void SameTextsInEveryCulture(string culture)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                CultureInfo.CurrentUICulture = new CultureInfo(culture);
                AssertInvariantTexts();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        // Antes usaba [SetCulture("de-DE")]. El Unity Test Framework no lo aplica (UnityTestExecutionContext.CurrentCulture
        // no toca el hilo; primer open real, It. 2, 00-primer-open.md §4) y en Unity el test fallaba por la precondición.
        // Hace a mano lo mismo que el atributo de NUnit: fija la cultura del HILO (Thread.CurrentThread), no la de
        // CultureInfo.CurrentCulture como SameTextsInEveryCulture. La precondición prueba que la cultura quedó puesta.
        [Test, Category("AC-BASE-02")]
        public void SameTextsWithTheThreadCulture()
        {
            var thread = Thread.CurrentThread;
            var previous = thread.CurrentCulture;
            var previousUi = thread.CurrentUICulture;
            try
            {
                thread.CurrentCulture = new CultureInfo("de-DE");
                thread.CurrentUICulture = new CultureInfo("de-DE");
                Assert.That(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, Is.EqualTo(","));
                AssertInvariantTexts();
            }
            finally
            {
                thread.CurrentCulture = previous;
                thread.CurrentUICulture = previousUi;
            }
        }

        private static void AssertInvariantTexts()
        {
            // 24,25 spm con 1 decimal y 10000 m (texto de AC-BASE-02).
            Assert.That(Fmt.Fixed(24.25, 1), Is.EqualTo("24.3"));
            Assert.That(Fmt.Integer(10000) + "m", Is.EqualTo("10000m"));
            Assert.That(Fmt.Integer(-10), Is.EqualTo("-10"));
            Assert.That(Fmt.DartToString(1.5), Is.EqualTo("1.5"));
            Assert.That(Fmt.DartToString(-1234567.25), Is.EqualTo("-1234567.25"));
            Assert.That(Fmt.DartToString(1e-7), Is.EqualTo("1e-7"));
            Assert.That(DartMath.RoundToFixed(24.25, 1), Is.EqualTo(24.3));
            Assert.That(TimeFormats.MinSec(-65), Is.EqualTo("-1:55"));
            Assert.That(TimeFormats.Elapsed(3661), Is.EqualTo("1:01:01"));
            Assert.That(IsoDate.FormatUtc(new DateTimeOffset(2026, 10, 27, 9, 37, 38, 628, TimeSpan.Zero)), Is.EqualTo("2026-10-27T09:37:38.628Z"));
            Assert.That(JsonParser.Parse("[1.5, -2]").Items[0].AsDouble(), Is.EqualTo(1.5));
            Assert.That(JsonParser.Parse("[1.5, -2]").Items[1].AsLong(), Is.EqualTo(-2L));

            var buffer = new char[16];
            Assert.That(Fmt.TryFormatFixed(-24.25, 1, buffer, out var written), Is.True);
            Assert.That(new string(buffer, 0, written), Is.EqualTo("-24.3"));

            var uss = UssTokenWriter.Write(new IDesignToken[]
            {
                ColorToken.HexAlpha("glass.test", "#070D16", 86),
                LengthToken.Dp("stroke.test", 1.5),
                LengthToken.Dp("r.test", 14),
            });
            Assert.That(uss, Does.Contain("rgba(7, 13, 22, 0.86)"));
            Assert.That(uss, Does.Contain("1.5px"));
            Assert.That(uss, Does.Contain("14px"));
        }
    }
}
