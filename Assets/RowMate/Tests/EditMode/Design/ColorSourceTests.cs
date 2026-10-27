using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // AC-APP-06-02 (ADR 016): ninguna pantalla define colores propios. El hex de un color existe solo en Core/Design/Tokens.cs
    // y, en USS, solo en Tokens.uss (generado). Revisa el código que llega a la UI (Core, App, Simulator, Presentation,
    // Simulator.Presentation, Bootstrap) y el arnés de capturas (Snapshots), y todas las hojas .uss y .tss del proyecto.
    // En C#: literales "#RRGGBB" y colores armados con números (new Color(0.1f…), new Color32(26…)). En USS: #hex, rgb() y
    // rgba(). Los tests no entran: comparan contra hex esperados a propósito.
    [TestFixture]
    public sealed class ColorSourceTests
    {
        private static readonly string[] CodeFolders = { "Core", "App", "Simulator", "Presentation", "SimulatorPresentation", "Bootstrap", "Snapshots" };

        private static readonly Regex HexLiteral = new Regex(@"""#[0-9A-Fa-f]{3,8}""");
        private static readonly Regex NumericColor = new Regex(@"\bnew\s+(UnityEngine\s*\.\s*)?Color(32)?\s*\(\s*[-+]?\d");
        private static readonly Regex UssHex = new Regex(@"#[0-9A-Fa-f]{3,8}\b");
        private static readonly Regex UssFunction = new Regex(@"\brgba?\s*\(");

        [Test, Category("AC-APP-06-02")]
        public void NoColorLiteralsOutsideTokens()
        {
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles(CodeFolders))
            {
                if (RepoPaths.Relative(file) == "Assets/RowMate/Core/Design/Tokens.cs")
                {
                    continue;
                }

                offenders.AddRange(SourceScan.Matches(file, new[] { HexLiteral }, false));
                offenders.AddRange(SourceScan.Matches(file, new[] { NumericColor }, true));
            }

            foreach (var pattern in new[] { "*.uss", "*.tss" })
            {
                foreach (var file in RepoPaths.Files("Assets", pattern))
                {
                    if (RepoPaths.Relative(file) == "Assets/RowMate/Presentation/Styles/Tokens.uss")
                    {
                        continue;
                    }

                    var text = SourceScan.StripComments(File.ReadAllText(file));
                    foreach (var regex in new[] { UssHex, UssFunction })
                    {
                        foreach (Match match in regex.Matches(text))
                        {
                            offenders.Add(RepoPaths.Relative(file) + ":" + SourceScan.LineOf(text, match.Index) + ": " + match.Value);
                        }
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "colores fuera de Tokens.cs / Tokens.uss (usar los tokens, AC-APP-06-02):\n"
                + string.Join("\n", offenders));
        }

        [Test, Category("AC-APP-06-02")]
        public void TheCheckCatchesLiterals()
        {
            // Control del propio chequeo: lo que tiene que encontrar y lo que no.
            Assert.That(HexLiteral.IsMatch("var c = \"#1A2E45\";"), Is.True);
            Assert.That(NumericColor.IsMatch("new Color(0.1f, 0.2f, 0.3f)"), Is.True);
            Assert.That(NumericColor.IsMatch("new Color32(26, 46, 69, 255)"), Is.True);
            Assert.That(NumericColor.IsMatch("new UnityEngine.Color(1, 1, 1, alpha)"), Is.True);
            Assert.That(NumericColor.IsMatch("new Color(rgb.R / 255f, rgb.G / 255f, rgb.B / 255f, alpha)"), Is.False,
                "convertir un token no es un color propio");
            Assert.That(UssHex.IsMatch("color: #FFF;"), Is.True);
            Assert.That(UssFunction.IsMatch("background-color: rgba(0, 0, 0, 0.5);"), Is.True);
            Assert.That(UssHex.IsMatch("color: var(--rm-text-primary);"), Is.False);
            Assert.That(UssHex.IsMatch("#metric-grid { flex-grow: 1; }"), Is.False, "un selector de nombre no es un color");
        }

        [Test, Category("AC-APP-06-02")]
        public void GeneratedStyleSheetsCarryNoColors()
        {
            // Typography.uss (generado en la It. 2) solo trae tamaños y fuentes.
            var path = RepoPaths.Combine("Assets", "RowMate", "Presentation", "Styles", "Typography.uss");
            Assert.That(File.Exists(path), Is.True);
            var text = SourceScan.StripComments(File.ReadAllText(path));
            Assert.That(UssHex.IsMatch(text) || UssFunction.IsMatch(text), Is.False);
            Assert.That(text, Does.Not.Contain("color:"));
        }
    }
}
