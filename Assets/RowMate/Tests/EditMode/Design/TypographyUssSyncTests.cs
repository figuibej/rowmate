using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // Presentation/Styles/Typography.uss es exactamente lo que genera tools/tokens con TypographyUssWriter desde Tokens.cs
    // (02-tech-design §2.9; como TokensUssSyncTests para Tokens.uss). Además fija a mano lo que midió QA (QA-G2-02, DN-02):
    // .rm-type-label lleva letter-spacing: 8px (= 0,08 em en la unidad de TextCore) y ninguna escala lo repite.
    [TestFixture]
    public sealed class TypographyUssSyncTests
    {
        private static readonly Regex Rule = new Regex(@"(?ms)^([^{}\n]+?)\s*\{(.*?)\}");

        [Test, Category("AC-APP-05-05"), Category("AC-BASE-04")]
        public void UssMatchesTokens()
        {
            var path = RepoPaths.Combine("Assets", "RowMate", "Presentation", "Styles", "Typography.uss");
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path) + ". Generalo con: " + TokensUssSyncTests.RegenerateCommand);
            var current = File.ReadAllText(path).Replace("\r\n", "\n");
            var generated = TypographyUssWriter.Write();
            if (current != generated)
            {
                var currentLines = current.Split('\n');
                var generatedLines = generated.Split('\n');
                var line = 0;
                while (line < currentLines.Length && line < generatedLines.Length && currentLines[line] == generatedLines[line])
                {
                    line++;
                }

                var had = line < currentLines.Length ? currentLines[line] : "(fin del archivo)";
                var expected = line < generatedLines.Length ? generatedLines[line] : "(fin del archivo)";
                Assert.Fail("Typography.uss no coincide con Tokens.cs (línea " + (line + 1) + "): tiene \"" + had + "\", se genera \""
                    + expected + "\". Regenerá con: " + TokensUssSyncTests.RegenerateCommand);
            }
        }

        [Test, Category("AC-APP-05-05")]
        public void LabelTrackingIsEightPx()
        {
            var rules = Rules(TypographyUssWriter.Write());
            Assert.That(rules.ContainsKey(".rm-type-label"), Is.True);
            Assert.That(rules[".rm-type-label"], Does.Contain("letter-spacing: 8px;"), "8px = 0,08 em (unidad de TextCore, QA-G2-02)");
            Assert.That(rules[".rm-type-label"], Does.Not.Contain("0.96px"), "no 0,96 px: casi no se vería");
            Assert.That(TypographyUssWriter.LetterSpacingPx(TypographyUssWriter.LabelTrackingEm), Is.EqualTo("8px"));
            foreach (var rule in rules)
            {
                if (rule.Key.StartsWith(".rm-text-scale-", StringComparison.Ordinal))
                {
                    Assert.That(rule.Value, Does.Not.Contain("letter-spacing"), rule.Key + ": el tracking ya escala con la letra");
                }
                else if (rule.Key.StartsWith(".rm-type-", StringComparison.Ordinal) && rule.Key != ".rm-type-label")
                {
                    Assert.That(rule.Value, Does.Not.Contain("letter-spacing"), rule.Key);
                }
            }
        }

        // QA-V4b-02 = QA-V4b-P01 (AC-WKT-16-03): el tracking por código (style.letterSpacing) va en la misma unidad que el USS, la
        // de TextCore (N = N/100 em por letra): el chip de fase y PAUSA, 0,08 em → 8. Escrito en dp (0,08 × 11 = 0,88) quedaba en
        // 0,0088 em, una novena parte.
        [Test, Category("AC-WKT-16-03"), Category("AC-WKT-13-12")]
        public void CodeTrackingUsesTheTextCoreUnit()
        {
            Assert.That(TypographyUssWriter.LetterSpacingUnits(HudGeometry.PhaseChipTrackingEm), Is.EqualTo(8).Within(1e-9), "el chip de fase");
            Assert.That(TypographyUssWriter.LetterSpacingUnits(HudGeometry.PausedLabelTrackingEm), Is.EqualTo(8).Within(1e-9), "PAUSA");
            Assert.That(TypographyUssWriter.LetterSpacingUnits(TypographyUssWriter.LabelTrackingEm) + "px",
                Is.EqualTo(TypographyUssWriter.LetterSpacingPx(TypographyUssWriter.LabelTrackingEm)), "la misma unidad que el USS");
            Assert.That(TypographyUssWriter.LetterSpacingUnits(0.04), Is.EqualTo(4).Within(1e-9), "el tipo del paso de la 4a (StepInfoBar)");
        }

        [Test, Category("AC-APP-05-05")]
        public void ScaledClassesMatchTokens()
        {
            var rules = Rules(TypographyUssWriter.Write());
            var types = TypeTokens();
            Assert.That(types.Count, Is.GreaterThanOrEqualTo(17));
            foreach (var type in types)
            {
                var name = "." + TypographyUssWriter.ClassName(type);
                Assert.That(rules.ContainsKey(name), Is.True, name);
                Assert.That(rules[name], Does.Contain("font-size: " + Px(type.SizeDp) + ";"), name);
                Assert.That(rules[name], Does.Contain("-unity-font-definition: url(\"project://database/"
                    + FontAssetPaths.For(type.Family, type.Weight) + "\");"), name);
                foreach (var step in TextScaleSteps.Steps)
                {
                    var scaled = "." + TextScaleSteps.ClassName(step) + " " + name;
                    if (step == 1.0)
                    {
                        Assert.That(rules.ContainsKey(scaled), Is.False, "la escala 1 es la regla base");
                        continue;
                    }

                    Assert.That(rules.ContainsKey(scaled), Is.True, scaled);
                    var expected = DartMath.RoundToFixed(type.SizeDp * step, 2);
                    Assert.That(rules[scaled].Trim(), Is.EqualTo("font-size: " + Px(expected) + ";"), scaled);
                }
            }

            // Ejemplos de 04-design-notes §3 (CAP-24): con 1,3×, num.xl pasa a 67,6 y la etiqueta de 12 a 15,6.
            Assert.That(rules[".rm-text-scale-130 .rm-type-num-xl"].Trim(), Is.EqualTo("font-size: 67.6px;"));
            Assert.That(rules[".rm-text-scale-130 .rm-type-label"].Trim(), Is.EqualTo("font-size: 15.6px;"));
            Assert.That(rules[".rm-text-scale-115 .rm-type-body"].Trim(), Is.EqualTo("font-size: 17.25px;"));
        }

        [Test, Category("AC-APP-05-03")]
        public void FontsAndIconsComeFromTheGeneratedAssets()
        {
            var rules = Rules(TypographyUssWriter.Write());
            Assert.That(rules[":root"], Does.Contain(FontAssetPaths.For(FontFamilyRole.Ui, 400)), "Inter 400 por defecto");
            Assert.That(rules[".rm-icon"], Does.Contain(FontAssetPaths.IconsOutline));
            Assert.That(rules[".rm-icon"], Does.Contain("-unity-text-align: middle-center;"));
            Assert.That(rules[".rm-icon"], Does.Contain("white-space: nowrap;"));
            Assert.That(rules[".rm-icon"], Does.Contain("letter-spacing: 0;"));
            Assert.That(rules[".rm-icon--filled"], Does.Contain(FontAssetPaths.IconsFilled));
            Assert.That(rules[".rm-type-num-xl"], Does.Contain("RowMateNumericSC-Bold-SDF.asset"), "números del tablero en RowMate Numeric SC (DD4a-07)");
            Assert.That(rules[".rm-type-title"], Does.Contain("Inter-SemiBold-SDF.asset"));

            // Cada FontAsset sale de un .ttf versionado (ASSET_SLOTS §3-§4) y las rutas no tienen espacios (van en url()).
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var source = FontAssetPaths.SourceFor(face.Family, face.Weight);
                Assert.That(File.Exists(RepoPaths.Combine(source)), Is.True, "falta " + source);
                Assert.That(FontAssetPaths.For(face.Family, face.Weight), Does.Not.Contain(" "));
            }

            Assert.That(File.Exists(RepoPaths.Combine(FontAssetPaths.IconsOutlineSource)), Is.True);
            Assert.That(File.Exists(RepoPaths.Combine(FontAssetPaths.IconsFilledSource)), Is.True);
            Assert.That(File.Exists(RepoPaths.Combine(FontAssetPaths.EmojiFallbackSource)), Is.True, "Noto Emoji (P-09, DN3-21)");
            Assert.That(FontAssetPaths.All.Count, Is.EqualTo(10), "7 de texto y número + 2 de íconos + el respaldo de emojis (It. 3)");
            Assert.Throws<ArgumentOutOfRangeException>(() => FontAssetPaths.For(FontFamilyRole.Mono, 400), "ningún token usa Mono todavía");
        }

        [TestCase("title", "rm-type-title")]
        [TestCase("titleS", "rm-type-title-s")]
        [TestCase("bodyXs", "rm-type-body-xs")]
        [TestCase("num.xl", "rm-type-num-xl")]
        [TestCase("num.hero", "rm-type-num-hero")]
        public void ClassNamesAreKebabCase(string token, string expected)
        {
            var type = TypeScaleToken.Of(token, FontFamilyRole.Ui, 12, 16, 400);
            Assert.That(TypographyUssWriter.ClassName(type), Is.EqualTo(expected));
        }

        [Test]
        public void OnlyTypeTokensProduceClasses()
        {
            var tokens = new IDesignToken[]
            {
                TypeScaleToken.Of("label", FontFamilyRole.Ui, 12, 16, 600),
                ColorToken.Contract("bg.app", "#0D1B2A", ColorRole.Surface),
                DurationToken.Ms("dur.base", 240),
            };
            var text = TypographyUssWriter.Write(tokens);
            Assert.That(text, Does.StartWith(TypographyUssWriter.Header));
            Assert.That(text, Does.Contain(".rm-type-label {"));
            Assert.That(text, Does.Not.Contain("bg-app"));
            Assert.That(text, Does.Not.Contain("dur-base"));
            Assert.That(text, Does.Contain(".rm-text-scale-085 .rm-type-label {"));
            Assert.That(text, Does.EndWith("}\n"));
            Assert.That(text, Does.Not.Contain("\r"));
        }

        private static List<TypeScaleToken> TypeTokens()
        {
            var types = new List<TypeScaleToken>();
            foreach (var token in TokenCatalog.All)
            {
                if (token is TypeScaleToken type)
                {
                    types.Add(type);
                }
            }

            return types;
        }

        // Selector → cuerpo de cada regla (sin comentarios).
        private static Dictionary<string, string> Rules(string uss)
        {
            var text = Regex.Replace(uss, @"/\*.*?\*/", "", RegexOptions.Singleline);
            var rules = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in Rule.Matches(text))
            {
                var selector = match.Groups[1].Value.Trim();
                Assert.That(rules.ContainsKey(selector), Is.False, "regla repetida: " + selector);
                rules[selector] = match.Groups[2].Value;
            }

            return rules;
        }

        private static string Px(double value)
        {
            return (value == Math.Floor(value) ? Fmt.Integer((long)value) : Fmt.DartToString(value)) + "px";
        }
    }
}
