using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common.Json;

namespace RowMate.Tests.EditMode.Common
{
    // JsonParser: RFC 8259 estricto (catálogo de textos, vectores dorados y lint del proyecto; D-08).
    [TestFixture]
    public sealed class JsonParserTests
    {
        [Test]
        public void ParsesEveryKindOfValue()
        {
            var root = JsonParser.Parse("{\"a\": null, \"b\": true, \"c\": false, \"d\": -12, \"e\": 1.5e3, \"f\": \"x\", \"g\": [1, [2]], \"h\": {}}");
            Assert.That(root.Kind, Is.EqualTo(JsonKind.Object));
            Assert.That(root["a"].IsNull, Is.True);
            Assert.That(root["b"].AsBool(), Is.True);
            Assert.That(root["c"].AsBool(), Is.False);
            Assert.That(root["d"].AsLong(), Is.EqualTo(-12L));
            Assert.That(root["e"].AsDouble(), Is.EqualTo(1500.0));
            Assert.That(root["e"].RawNumber, Is.EqualTo("1.5e3"));
            Assert.That(root["f"].AsString(), Is.EqualTo("x"));
            Assert.That(root["g"].Items.Count, Is.EqualTo(2));
            Assert.That(root["g"].Items[1].Items[0].AsLong(), Is.EqualTo(2L));
            Assert.That(root["h"].Properties, Is.Empty);
        }

        [Test]
        public void KeepsPropertyOrderAndExactIntegers()
        {
            var root = JsonParser.Parse("{\"z\": 9223372036854775807, \"a\": -9223372036854775808, \"m\": 4503599627370496}");
            var keys = new List<string>();
            foreach (var property in root.Properties)
            {
                keys.Add(property.Key);
            }

            Assert.That(keys, Is.EqualTo(new[] { "z", "a", "m" }));
            Assert.That(root["z"].AsLong(), Is.EqualTo(long.MaxValue));
            Assert.That(root["a"].AsLong(), Is.EqualTo(long.MinValue));
            Assert.That(root["m"].AsLong(), Is.EqualTo(4503599627370496L));
        }

        [Test]
        public void DecodesEscapesAndSurrogatePairs()
        {
            var root = JsonParser.Parse("[\"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\", \"\\u00e9\\u00C1\", \"\\ud83d\\udcc8 📈\", \"·¿¡\"]");
            Assert.That(root.Items[0].AsString(), Is.EqualTo("a\"b\\c/d\b\f\n\r\t"));
            Assert.That(root.Items[1].AsString(), Is.EqualTo("éÁ"));
            Assert.That(root.Items[2].AsString(), Is.EqualTo("📈 📈"));
            Assert.That(root.Items[3].AsString(), Is.EqualTo("·¿¡"));
        }

        [Test]
        public void TryGetAndMissingKeys()
        {
            var root = JsonParser.Parse("{\"a\": 1}");
            Assert.That(root.TryGet("a", out var a), Is.True);
            Assert.That(a.AsLong(), Is.EqualTo(1L));
            Assert.That(root.TryGet("b", out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => { var unused = root["b"]; });
            Assert.Throws<InvalidOperationException>(() => root.AsString());
            Assert.Throws<InvalidOperationException>(() => root["a"].AsBool());
        }

        [Test]
        public void AsLongRejectsNonIntegers()
        {
            Assert.Throws<FormatException>(() => JsonParser.Parse("1.5").AsLong());
            Assert.Throws<FormatException>(() => JsonParser.Parse("1e3").AsLong());
            Assert.Throws<FormatException>(() => JsonParser.Parse("9223372036854775808").AsLong());
        }

        [Test]
        public void IgnoresAnInitialBomAndWhitespace()
        {
            Assert.That(JsonParser.Parse("\uFEFF \r\n\t[]\n").Items, Is.Empty);
        }

        [TestCase("")]
        [TestCase("[1,]")]
        [TestCase("{\"a\": 1,}")]
        [TestCase("[1 2]")]
        [TestCase("// c\n[]")]
        [TestCase("/* c */ []")]
        [TestCase("['a']")]
        [TestCase("01")]
        [TestCase("-01")]
        [TestCase("1.")]
        [TestCase(".5")]
        [TestCase("+1")]
        [TestCase("1e")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("tru")]
        [TestCase("nul")]
        [TestCase("\"abc")]
        [TestCase("\"a\tb\"")]
        [TestCase("\"\\x\"")]
        [TestCase("\"\\u12\"")]
        [TestCase("\"\\ud83d\"")]
        [TestCase("\"\\udcc8\"")]
        [TestCase("{\"a\": 1, \"a\": 2}")]
        [TestCase("{a: 1}")]
        [TestCase("[] []")]
        [TestCase("{\"a\" 1}")]
        public void RejectsInvalidDocuments(string text)
        {
            Assert.Throws<FormatException>(() => JsonParser.Parse(text));
        }

        [Test]
        public void ErrorsReportLineAndColumn()
        {
            var error = Assert.Throws<FormatException>(() => JsonParser.Parse("{\n  \"a\": 1,\n  \"a\": 2\n}"));
            Assert.That(error!.Message, Does.Contain("línea 3"));
            Assert.That(error.Message, Does.Contain("columna 3"));
            Assert.That(error.Message, Does.Contain("duplicada"));
        }

        [Test]
        public void LimitsNestingDepth()
        {
            Assert.DoesNotThrow(() => JsonParser.Parse(new string('[', 64) + new string(']', 64)));
            Assert.Throws<FormatException>(() => JsonParser.Parse(new string('[', 65) + new string(']', 65)));
        }
    }
}
