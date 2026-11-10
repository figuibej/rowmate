using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // IS · El ícono no usa funciones trascendentes de la plataforma (It. 14, DT14-01, QA-G14-05): en Core/Design/AppIcon*,
    // IconMath y el PNG solo se permiten Math.Sqrt, Abs, Min, Max y Floor (IEEE 754 los fija bit a bit); nada de MathF,
    // Math.Sin/Cos/Tan/Exp/Pow/Log/Atan* ni FusedMultiplyAdd (Math.Round ya lo prohíbe ADR 012).
    [TestFixture]
    public sealed class AppIconSourceScanTests
    {
        private static readonly HashSet<string> Allowed = new HashSet<string> { "Sqrt", "Abs", "Min", "Max", "Floor" };
        private static readonly Regex MathCall = new Regex(@"\bMath\s*\.\s*(\w+)");
        private static readonly Regex MathF = new Regex(@"\bMathF\b");

        [Test, Category("AC-APP-16-08")]
        public void OnlyExactMathInTheIcon()
        {
            var files = new List<string>();
            foreach (var file in SourceScan.CsFiles("Core/Design"))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("AppIcon") || name == "IconMath.cs" || name == "PngEncoder.cs" || name == "DeflateEncoder.cs"
                    || name == "Adler32.cs" || name == "Crc32.cs")
                {
                    files.Add(file);
                }
            }

            Assert.That(files.Count, Is.EqualTo(10), "AppIconGeometry, Level, Raster, Sizes, Sheets, IconMath, Png, Deflate, Adler32, Crc32");
            var offenders = new List<string>();
            foreach (var file in files)
            {
                var code = SourceScan.StripCommentsAndLiterals(File.ReadAllText(file));
                foreach (Match match in MathCall.Matches(code))
                {
                    if (!Allowed.Contains(match.Groups[1].Value))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ": Math." + match.Groups[1].Value);
                    }
                }

                if (MathF.IsMatch(code))
                {
                    offenders.Add(RepoPaths.Relative(file) + ": MathF");
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }
    }
}
