using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // AC-BASE-04: reglas del archivo Tokens.cs (02-tech-design §4.3) y del mecanismo (§4.2).
    [TestFixture]
    public sealed class TokenCatalogTests
    {
        private static readonly Regex NamePattern = new Regex(@"^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9]*)*$", RegexOptions.CultureInvariant);

        [Test, Category("AC-BASE-04")]
        public void NamesAreValidAndUnique()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in TokenCatalog.All)
            {
                Assert.That(NamePattern.IsMatch(token.Name), Is.True, "nombre inválido: " + token.Name);
                Assert.That(seen.Add(token.Name), Is.True, "nombre repetido: " + token.Name);
            }

            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(38));
        }

        // It. 5 (05-series-parciales/02-tech-design §2.10; DD5-08, excepción de dueño de §7.6): num.lap 34/36 → 28/28, num.row
        // 24/28 → 20/22 y num.xs 13/16/700 nuevo: 136 → 137 tokens.
        [Test, Category("AC-BASE-04"), Category("AC-WKT-18-04"), Category("AC-WKT-18-07")]
        public void ItFiveTypeTokens()
        {
            Assert.That(TokenCatalog.All.Count, Is.EqualTo(138), "It. 10a: + mono (A10-10)");
            AssertType(Tokens.NumLap, "num.lap", 28, 28);
            AssertType(Tokens.NumRow, "num.row", 20, 22);
            AssertType(Tokens.NumXs, "num.xs", 13, 16);
            AssertType(Tokens.NumStat, "num.stat", 16, 20);
        }

        // It. 10a (00-arte-bluetooth §5.3, A10-10): el único token nuevo, JetBrains Mono 400 12/18.
        [Test, Category("A10-10"), Category("AC-BLE-12-02")]
        public void ItTenAMonoToken()
        {
            Assert.That(Tokens.Mono.Name, Is.EqualTo("mono"));
            Assert.That(Tokens.Mono.Family, Is.EqualTo(FontFamilyRole.Mono));
            Assert.That(Tokens.Mono.SizeDp, Is.EqualTo(12));
            Assert.That(Tokens.Mono.LineHeightDp, Is.EqualTo(18));
            Assert.That(Tokens.Mono.Weight, Is.EqualTo(400));
            Assert.That(FontAssetPaths.For(FontFamilyRole.Mono, 400), Does.EndWith("/JetBrainsMono-Regular-SDF.asset"));
            Assert.That(FontAssetPaths.SourceFor(FontFamilyRole.Mono, 400), Is.EqualTo("Assets/RowMate/UI/Fonts/JetBrainsMono/JetBrainsMono-Regular.ttf"));
            Assert.That(FontAssetPaths.KerningFor(FontFamilyRole.Mono, 400), Is.Null, "monoespaciada: sin tabla");
        }

        private static void AssertType(TypeScaleToken token, string name, double size, double lineHeight)
        {
            Assert.That(token.Name, Is.EqualTo(name));
            Assert.That(token.Family, Is.EqualTo(FontFamilyRole.Numeric), name);
            Assert.That(token.SizeDp, Is.EqualTo(size), name);
            Assert.That(token.LineHeightDp, Is.EqualTo(lineHeight), name);
            Assert.That(token.Weight, Is.EqualTo(700), name);
        }

        [Test, Category("AC-BASE-04")]
        public void FieldNameIsPascalCaseOfTheTokenName()
        {
            var fields = TokenCatalog.Fields();
            Assert.That(fields.Count, Is.EqualTo(TokenCatalog.All.Count));
            foreach (var field in fields)
            {
                Assert.That(field.Key, Is.EqualTo(TokenCatalog.FieldNameFor(field.Value.Name)), "campo de " + field.Value.Name);
            }
        }

        [TestCase("metric.split.hud", "MetricSplitHud", Category = "AC-BASE-04")]
        [TestCase("text.onAccent", "TextOnAccent", Category = "AC-BASE-04")]
        [TestCase("r.glass", "RGlass", Category = "AC-BASE-04")]
        [TestCase("step.unknown.hud", "StepUnknownHud", Category = "AC-BASE-04")]
        [TestCase("space.dp2", "SpaceDp2", Category = "AC-BASE-04")]
        public void FieldNameForExamples(string tokenName, string expected)
        {
            Assert.That(TokenCatalog.FieldNameFor(tokenName), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-04")]
        public void AllIsInOrdinalOrderOfName()
        {
            var all = TokenCatalog.All;
            for (var i = 1; i < all.Count; i++)
            {
                Assert.That(string.CompareOrdinal(all[i - 1].Name, all[i].Name), Is.LessThan(0), all[i - 1].Name + " antes de " + all[i].Name);
            }
        }

        [Test, Category("AC-BASE-04")]
        public void AlphasAreWholePercentagesWithoutRounding()
        {
            foreach (var token in TokenCatalog.All)
            {
                if (token is ColorToken color)
                {
                    Assert.That(color.AlphaPercent, Is.InRange(0, 100), color.Name);
                    Assert.That(color.Alpha, Is.EqualTo(color.AlphaPercent / 100.0), color.Name);
                    if (color.IsContract)
                    {
                        Assert.That(color.IsOpaque, Is.True, color.Name);
                    }
                }
            }

            Assert.That(Tokens.GlassFallback.Alpha, Is.EqualTo(0.86));
        }

        [Test, Category("AC-BASE-04")]
        public void UssVariableNamesAreUnique()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in TokenCatalog.All)
            {
                var variable = UssTokenWriter.VariableName(token.Name) + (token.Kind == TokenKind.TypeScale ? "-size" : "");
                Assert.That(seen.Add(variable), Is.True, "variable USS repetida: " + variable);
            }
        }

        [Test, Category("AC-BASE-04")]
        public void EasingsHaveXInsideTheUnitInterval()
        {
            foreach (var token in TokenCatalog.All)
            {
                if (token is EasingToken easing)
                {
                    Assert.That(easing.X1, Is.InRange(0.0, 1.0), easing.Name);
                    Assert.That(easing.X2, Is.InRange(0.0, 1.0), easing.Name);
                }
            }

            // y puede salir de [0; 1]: ease.emphasis tiene y1 = 1,56 (rebote, 04-design-notes §2.6).
            Assert.That(Tokens.EaseEmphasis.Y1, Is.EqualTo(1.56));
        }

        [Test, Category("AC-BASE-04")]
        public void LookupsByName()
        {
            Assert.That(TokenCatalog.Get("bg.app"), Is.SameAs(Tokens.BgApp));
            Assert.That(TokenCatalog.Color("step.work"), Is.SameAs(Tokens.StepWork));
            Assert.Throws<KeyNotFoundException>(() => TokenCatalog.Get("no.existe"));
            Assert.Throws<InvalidCastException>(() => TokenCatalog.Color("r.glass"));
        }

        [Test, Category("AC-BASE-04")]
        public void FactoriesValidateTheirInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ColorToken.HexAlpha("x.y", "#000000", 101));
            Assert.Throws<ArgumentOutOfRangeException>(() => ColorToken.HexAlpha("x.y", "#000000", -1));
            Assert.Throws<FormatException>(() => ColorToken.Hex("x.y", "#12345"));
            Assert.Throws<FormatException>(() => ColorToken.Hex("x.y", "123456"));
            Assert.Throws<FormatException>(() => ColorToken.Hex("x.y", "#12345G"));
            Assert.Throws<ArgumentOutOfRangeException>(() => EasingToken.CubicBezier("x.y", 1.2, 0, 0, 1));
            Assert.DoesNotThrow(() => EasingToken.CubicBezier("x.y", 0.34, 1.56, 0.64, 1.0));
            Assert.That(Rgb.Parse("#0d1b2a").ToHex(), Is.EqualTo("#0D1B2A"));
            Assert.That(Rgb.Parse("#0D1B2A"), Is.EqualTo(new Rgb(0x0D, 0x1B, 0x2A)));
        }
    }
}
