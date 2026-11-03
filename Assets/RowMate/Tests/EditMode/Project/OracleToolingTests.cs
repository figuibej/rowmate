using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // OTT: el oráculo del SeriesTracker (05-series-parciales/02-tech-design §2.9; AC-WKT-07-12): series_tracker.json trae su
    // generador y el commit del legacy (el mismo que el resto de los vectores); el script es Dart puro, importa solo los modelos y el
    // tracker del legacy (que solo importan dart:) y lib/golden.dart, no nombra el módulo de credenciales del legacy (R-02) y no
    // lleva rutas de la PC (regla de repo público). Regenerarlo necesita el Dart de <flutter-sdk>; las pruebas, solo el archivo.
    [TestFixture]
    public sealed class OracleToolingTests
    {
        private const string Script = "tools/oracle/bin/series_vectors.dart";

        [Test, Category("AC-WKT-07-12")]
        public void TheSeriesVectorsHaveTheirGeneratorAndLegacyCommit()
        {
            var file = GoldenVectors.Load("series_tracker.json");
            Assert.That(file.Header.Generator, Is.EqualTo(Script));
            Assert.That(file.Header.Schema, Is.EqualTo(1));
            Assert.That(Regex.IsMatch(file.Header.LegacyCommit, "^[0-9a-f]{40}$"), Is.True);
            Assert.That(file.Header.LegacyCommit, Is.EqualTo(GoldenVectors.Load("routines.json").Header.LegacyCommit));
            Assert.That(file.SizeBytes, Is.LessThan(GoldenCoverageTests.MaxBytes), "el tope de 1 MiB por archivo");
        }

        [Test, Category("AC-WKT-07-12")]
        public void TheScriptIsPureDartOverTheLegacyModels()
        {
            var path = RepoPaths.Combine(Script);
            Assert.That(File.Exists(path), Is.True);
            var text = File.ReadAllText(path);
            foreach (Match import in Regex.Matches(text, @"(?m)^import\s+'([^']+)';"))
            {
                var target = import.Groups[1].Value;
                Assert.That(target == "../lib/golden.dart" || target.StartsWith("dart:", StringComparison.Ordinal)
                            || target == "package:rowmate/core/models/interval_step.dart" || target == "package:rowmate/core/models/routine.dart"
                            || target == "package:rowmate/features/workout/series_tracker.dart",
                    Is.True, "import no permitido: " + target);
            }

            Assert.That(text, Does.Contain("package:rowmate/features/workout/series_tracker.dart"));
            Assert.That(text.IndexOf("str" + "ava", StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1), "R-02");
            Assert.That(Regex.IsMatch(text, @"[A-Za-z]:[\\/]"), Is.False, "sin rutas de la PC");
            Assert.That(text.IndexOf("/" + "Users/", StringComparison.Ordinal), Is.EqualTo(-1));
            Assert.That(text, Does.Contain("const generator = '" + Script + "';"));
        }
    }
}
