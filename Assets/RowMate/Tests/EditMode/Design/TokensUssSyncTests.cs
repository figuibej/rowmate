using System.IO;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // AC-BASE-04 y AC-APP-19-03: Presentation/Styles/Tokens.uss es exactamente lo que genera tools/tokens desde Tokens.cs.
    [TestFixture]
    public sealed class TokensUssSyncTests
    {
        public const string RegenerateCommand =
            "\"C:/Program Files/dotnet/dotnet.exe\" build tools/dotnet/RowMate.sln --configuration Release --artifacts-path tools/dotnet/.artifacts"
            + " y después \"C:/Program Files/dotnet/dotnet.exe\" tools/dotnet/.artifacts/bin/RowMate.Tokens/release/RowMate.Tokens.dll";

        [Test, Category("AC-BASE-04"), Category("AC-APP-19-03")]
        public void UssMatchesTokens()
        {
            var path = RepoPaths.Combine("Assets", "RowMate", "Presentation", "Styles", "Tokens.uss");
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path) + ". Generalo con: " + RegenerateCommand);
            var current = File.ReadAllText(path).Replace("\r\n", "\n");
            var generated = UssTokenWriter.Write(TokenCatalog.All);
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
                Assert.Fail("Tokens.uss no coincide con Tokens.cs (línea " + (line + 1) + "): tiene \"" + had + "\", se genera \""
                    + expected + "\". Regenerá con: " + RegenerateCommand);
            }
        }

        [Test, Category("AC-BASE-04")]
        public void WriterFormat()
        {
            var tokens = new IDesignToken[]
            {
                TypeScaleToken.Of("num.hero", FontFamilyRole.Numeric, 64, 64, 800),
                ColorToken.Contract("bg.app", "#0D1B2A", ColorRole.Surface),
                ColorToken.HexAlpha("glass.fallback", "#070D16", 86, ColorRole.Surface),
                LengthToken.Dp("r.glass", 14),
                LengthToken.Dp("stroke.target", 1.5),
                DurationToken.Ms("dur.base", 240),
                EasingToken.CubicBezier("ease.standard", 0.2, 0.0, 0.0, 1.0, ussKeyword: "ease-out-cubic"),
                EasingToken.CubicBezier("ease.noKeyword", 0.2, 0.0, 0.0, 1.0),
                ColorToken.Hex("text.onAccent", "#04131c"),
            };
            var expected =
                "/* GENERADO por tools/tokens desde Assets/RowMate/Core/Design/Tokens.cs. NO EDITAR A MANO (ADR 016). */\n" +
                ":root {\n" +
                "    --rm-bg-app: #0D1B2A;\n" +
                "    --rm-dur-base: 240ms;\n" +
                "    --rm-ease-standard: ease-out-cubic;\n" +
                "    --rm-glass-fallback: rgba(7, 13, 22, 0.86);\n" +
                "    --rm-num-hero-size: 64px;\n" +
                "    --rm-r-glass: 14px;\n" +
                "    --rm-stroke-target: 1.5px;\n" +
                "    --rm-text-on-accent: #04131C;\n" +
                "}\n";
            Assert.That(UssTokenWriter.Write(tokens), Is.EqualTo(expected));
        }

        [TestCase("text.onAccent", "--rm-text-on-accent", Category = "AC-BASE-04")]
        [TestCase("metric.split.hud", "--rm-metric-split-hud", Category = "AC-BASE-04")]
        [TestCase("space.dp2", "--rm-space-dp2", Category = "AC-BASE-04")]
        [TestCase("focus.ringHalo", "--rm-focus-ring-halo", Category = "AC-BASE-04")]
        [TestCase("state.offTarget.hud", "--rm-state-off-target-hud", Category = "AC-BASE-04")]
        public void VariableNames(string tokenName, string expected)
        {
            Assert.That(UssTokenWriter.VariableName(tokenName), Is.EqualTo(expected));
        }

        [Test, Category("AC-BASE-04")]
        public void OneVariablePerTokenWithAUssValue()
        {
            var expected = 0;
            foreach (var token in TokenCatalog.All)
            {
                if (!(token is EasingToken easing) || easing.UssKeyword != null)
                {
                    expected++;
                }
            }

            var lines = UssTokenWriter.Write(TokenCatalog.All).Split('\n');
            var variables = 0;
            foreach (var line in lines)
            {
                if (line.StartsWith("    --rm-", System.StringComparison.Ordinal))
                {
                    variables++;
                }
            }

            Assert.That(variables, Is.EqualTo(expected));
        }
    }
}
