using System;
using NUnit.Framework;
using RowMate.Core.Localization;

namespace RowMate.Tests.EditMode.Localization
{
    // MessageFormatter: ICU mínimo de los mensajes del catálogo (02-tech-design §3.3). Sin escape con apóstrofos (como
    // gen-l10n sin use-escaping), plurales =N / one / other con '#', anidados y varios por mensaje (D-13).
    [TestFixture]
    public sealed class MessageFormatterTests
    {
        private static string Format(string pattern, params MessageArg[] args)
        {
            return MessageFormatter.Parse(pattern).Format(args);
        }

        [Test, Category("AC-APP-10-05")]
        public void LiteralTextIsKeptAsIs()
        {
            Assert.That(Format(""), Is.EqualTo(""));
            Assert.That(Format("Your sessions won't be uploaded automatically. Continue?"),
                Is.EqualTo("Your sessions won't be uploaded automatically. Continue?"), "el apóstrofo es literal");
            Assert.That(Format("  O elegí una rutina:"), Is.EqualTo("  O elegí una rutina:"));
            Assert.That(Format("a\nb"), Is.EqualTo("a\nb"));
            Assert.That(Format("SPLIT 500m · # fuera de un plural"), Is.EqualTo("SPLIT 500m · # fuera de un plural"));
            Assert.That(Format("📈 ¿Iniciar? ¡Sí!"), Is.EqualTo("📈 ¿Iniciar? ¡Sí!"));
        }

        [Test, Category("AC-APP-10-05")]
        public void SimpleArguments()
        {
            Assert.That(Format("Conectando a {name}...", MessageArg.Text("name", "Polar H10")), Is.EqualTo("Conectando a Polar H10..."));
            Assert.That(Format("{m}m {s}s", MessageArg.Number("m", 62), MessageArg.Number("s", 5)), Is.EqualTo("62m 5s"));
            Assert.That(Format("{min}-{max}W", MessageArg.Number("min", -10), MessageArg.Number("max", 5)), Is.EqualTo("-10-5W"));
            Assert.That(Format("{ x }!", MessageArg.Text("x", "ok")), Is.EqualTo("ok!"), "espacios alrededor del nombre");
            Assert.That(Format("{x}{x}", MessageArg.Text("x", "ab")), Is.EqualTo("abab"));
            Assert.That(Format("{x}", MessageArg.Text("x", "{no es un argumento}")), Is.EqualTo("{no es un argumento}"));
        }

        [Test, Category("AC-APP-10-06")]
        public void PluralSelection()
        {
            const string pattern = "{count, plural, one{# sesión subida} other{# sesiones subidas}}";
            Assert.That(Format(pattern, MessageArg.Number("count", 0)), Is.EqualTo("0 sesiones subidas"));
            Assert.That(Format(pattern, MessageArg.Number("count", 1)), Is.EqualTo("1 sesión subida"));
            Assert.That(Format(pattern, MessageArg.Number("count", 2)), Is.EqualTo("2 sesiones subidas"));
            Assert.That(Format(pattern, MessageArg.Number("count", -1)), Is.EqualTo("-1 sesiones subidas"));
            Assert.That(Format(pattern, MessageArg.Number("count", 1000000)), Is.EqualTo("1000000 sesiones subidas"));

            const string exact = "{n, plural, =0{ninguna} =1{una exacta} one{una} other{# varias}}";
            Assert.That(Format(exact, MessageArg.Number("n", 0)), Is.EqualTo("ninguna"), "=N exacto antes que other");
            Assert.That(Format(exact, MessageArg.Number("n", 1)), Is.EqualTo("una exacta"), "=N exacto antes que one");
            Assert.That(Format(exact, MessageArg.Number("n", 7)), Is.EqualTo("7 varias"));
            Assert.That(Format("{n, plural, =-1{menos uno} other{#}}", MessageArg.Number("n", -1)), Is.EqualTo("menos uno"));
            Assert.That(Format("{n,plural,other{#}}", MessageArg.Number("n", 3)), Is.EqualTo("3"), "sin espacios");
        }

        [Test, Category("AC-APP-10-06")]
        public void TwoPluralsAndNestedArguments()
        {
            const string pattern = "{work, plural, one{# serie} other{# series}} · {rest, plural, one{# descanso} other{# descansos}}";
            Assert.That(Format(pattern, MessageArg.Number("work", 1), MessageArg.Number("rest", 1)), Is.EqualTo("1 serie · 1 descanso"));
            Assert.That(Format(pattern, MessageArg.Number("work", 4), MessageArg.Number("rest", 0)), Is.EqualTo("4 series · 0 descansos"));

            const string nested = "{n, plural, one{{who} subió # sesión} other{{who} subió # sesiones de {m, plural, one{# min} other{# min}}}}";
            Assert.That(Format(nested, MessageArg.Number("n", 1), MessageArg.Text("who", "Ana"), MessageArg.Number("m", 9)), Is.EqualTo("Ana subió 1 sesión"));
            Assert.That(Format(nested, MessageArg.Number("n", 2), MessageArg.Text("who", "Ana"), MessageArg.Number("m", 9)), Is.EqualTo("Ana subió 2 sesiones de 9 min"),
                "'#' es el número del plural más cercano");

            var compiled = MessageFormatter.Parse(nested);
            Assert.That(compiled.ArgumentNames, Is.EqualTo(new[] { "n", "who", "m" }));
            Assert.That(compiled.IsPluralArgument("n"), Is.True);
            Assert.That(compiled.IsPluralArgument("m"), Is.True);
            Assert.That(compiled.IsPluralArgument("who"), Is.False);
        }

        [TestCase("{")]
        [TestCase("{name")]
        [TestCase("hola }")]
        [TestCase("{}")]
        [TestCase("{1x}")]
        [TestCase("{x y}")]
        [TestCase("{x, select, a{b} other{c}}")]
        [TestCase("{x, number}")]
        [TestCase("{x, plural}")]
        [TestCase("{x, plural, one{a}}")]
        [TestCase("{x, plural, few{a} other{b}}")]
        [TestCase("{x, plural, zero{a} other{b}}")]
        [TestCase("{x, plural, one{a} one{b} other{c}}")]
        [TestCase("{x, plural, =1{a} =01{b} other{c}}")]
        [TestCase("{x, plural, other{a}")]
        [TestCase("{x, plural, other{a}} }")]
        [TestCase("{x, plural, other a}")]
        [TestCase("{x, plural, ={a} other{b}}")]
        [Category("AC-APP-10-04")]
        public void InvalidSyntaxFailsAtLoad(string pattern)
        {
            Assert.Throws<FormatException>(() => MessageFormatter.Parse(pattern));
        }

        [Test, Category("AC-APP-10-04")]
        public void ArgumentsMustMatchExactly()
        {
            var compiled = MessageFormatter.Parse("{count, plural, one{# paso} other{# pasos}} de {name}");
            Assert.Throws<ArgumentException>(() => compiled.Format(new[] { MessageArg.Number("count", 1) }), "falta name");
            Assert.Throws<ArgumentException>(() => compiled.Format(new[] { MessageArg.Number("count", 1), MessageArg.Text("name", "a"), MessageArg.Text("x", "b") }), "sobra x");
            Assert.Throws<ArgumentException>(() => compiled.Format(new[] { MessageArg.Text("count", "1"), MessageArg.Text("name", "a") }), "plural con texto");
            Assert.Throws<ArgumentException>(() => compiled.Format(new[] { MessageArg.Number("count", 1), MessageArg.Text("name", "a"), MessageArg.Text("name", "b") }), "repetido");
            Assert.Throws<ArgumentException>(() => compiled.Format(new[] { default(MessageArg) }), "argumento sin nombre");
            Assert.That(compiled.Format(new[] { MessageArg.Text("name", "T1"), MessageArg.Number("count", 8) }), Is.EqualTo("8 pasos de T1"), "en cualquier orden");
            Assert.Throws<ArgumentNullException>(() => MessageFormatter.Parse(null!));
            Assert.Throws<ArgumentNullException>(() => MessageArg.Text("x", null!));
        }
    }
}
