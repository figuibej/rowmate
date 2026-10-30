using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // ITO · Revisión estática de las herramientas de la It. 4b (04b-hud-exr/02-tech-design §5, §3.13, §7.2 B3; QA-G4bT-10 (a)): el
    // arnés con el conjunto it4b por defecto y su carpeta, la regla de toma de la 4b (en el instante exacto, con la lista de
    // bloqueos), las tomas de it4a reemplazadas con su reemplazo en it4b (DT4b-07, SEV), BuildCli que prende y restaura
    // enableFrameTimingStats solo en el build de simulador, y ninguna ruta de la PC en lo que el arnés versiona.
    [TestFixture]
    public sealed class ItFourBToolingTests
    {
        private const string CaptureScript = "tools/unity/capture-snapshots.ps1";
        private const string Cli = "Assets/RowMate/Snapshots/SnapshotCli.cs";
        private const string Runner = "Assets/RowMate/Snapshots/SnapshotRunner.cs";
        private const string TakeRule = "Assets/RowMate/Snapshots/SnapshotTakeRule.cs";
        private const string Catalog = "Assets/RowMate/Snapshots/HudExrCatalog.cs";
        private const string WorkoutCatalog = "Assets/RowMate/Snapshots/WorkoutCatalog.cs";
        private const string BuildCli = "Assets/RowMate/Editor/Build/BuildCli.cs";
        private const string It4b = "docs/migration/iterations/04b-hud-exr/snapshots";
        private const string It4a = "docs/migration/iterations/04a-motor-hud-funcional/snapshots";

        private static readonly Regex MachinePath = new Regex(@"(?<![A-Za-z])[A-Za-z]:[\\/]|[\\/]Users[\\/]|[\\/]home[\\/]|AppData[\\/]|\\\\[A-Za-z0-9]", RegexOptions.IgnoreCase);

        [Test, Category("AC-WKT-13-13")]
        public void CaptureScriptDefaultsToIt4b()
        {
            var script = File.ReadAllText(RepoPaths.Combine(CaptureScript));
            Assert.That(script, Does.Contain("[ValidateSet('it4b', 'it4a', 'it3', 'it2')]"));
            Assert.That(script, Does.Contain("[string]$Set = 'it4b',"), "it4b por defecto desde la 4b");
            Assert.That(script, Does.Contain("return '" + It4b + "'"), "su carpeta de salida");
            var bytes = File.ReadAllBytes(RepoPaths.Combine(CaptureScript));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.That(bytes[i], Is.LessThan(128), CaptureScript + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
            }
        }

        [Test, Category("AC-WKT-13-13")]
        public void CliKnowsTheIt4bSet()
        {
            var cli = File.ReadAllText(RepoPaths.Combine(Cli));
            Assert.That(cli, Does.Contain("public const string It4bOutput = \"" + It4b + "\";"));
            Assert.That(cli, Does.Contain("public const string DefaultSet = \"it4b\";"));
            Assert.That(File.Exists(RepoPaths.Combine(Catalog)), Is.True, "HudExrCatalog");
        }

        [Test, Category("AC-WKT-13-13")]
        public void TheFourBRuleTakesAtTheExactInstantAndNeverWaits()
        {
            var runner = File.ReadAllText(RepoPaths.Combine(Runner));
            Assert.That(runner, Does.Contain("SnapshotTakeRule.Blockers(host, inFlight)"), "la lista de bloqueos con nombre (DT4b-19)");
            Assert.That(runner, Does.Contain("_record!.InFlight = inFlight;"), "lo que corría, al manifest");
            var rule = File.ReadAllText(RepoPaths.Combine(TakeRule));
            Assert.That(rule, Does.Contain("public const double RollHalf = 0.5;"), "un rodillo bloquea antes de su mitad");
            Assert.That(rule, Does.Contain("public const double SpringRemainingDp = 0.15;"), "un resorte a más de 0,15 dp");
            var catalog = File.ReadAllText(RepoPaths.Combine(Catalog));
            Assert.That(catalog, Does.Contain("ExactInstant = true"), "las tomas de it4b con la regla de la 4b");
        }

        [Test, Category("AC-WKT-13-13")]
        public void It4aHudShotsAreSupersededByIt4b()
        {
            var catalog = File.ReadAllText(RepoPaths.Combine(WorkoutCatalog));
            Assert.That(catalog, Does.Contain("public const string SupersedingSet = \"it4b\";"));
            Assert.That(catalog, Does.Contain("MarkSuperseded(all);"));
            var manifest = RepoPaths.Combine(It4a, "manifest.json");
            var it4b = RepoPaths.Combine(It4b, "manifest.json");
            if (!File.Exists(manifest) || !File.Exists(it4b))
            {
                Assert.Ignore("sin los manifest de it4a e it4b: los produce capture-snapshots.ps1 (WP-B3)");
            }

            var newIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var shot in Shots(it4b))
            {
                newIds.Add(shot["id"].AsString());
            }

            foreach (var shot in Shots(manifest))
            {
                if (!shot.TryGet("superseded", out var superseded) || superseded.IsNull)
                {
                    continue;
                }

                Assert.That(superseded.AsString(), Is.EqualTo("it4b"), shot["id"].AsString());
                Assert.That(newIds.Count, Is.GreaterThan(0), "it4b tiene sus tomas");
            }

            Assert.That(newIds.Contains("CAP-01") && newIds.Contains("DEMO-12") && newIds.Contains("VID-08"), Is.True, "las tomas que las reemplazan");
        }

        // QA-V4b-07 (§5.5): cada video deja sus valores por cuadro y ninguno es NaN. Antes VID-02 (la salida) no dejaba ninguno y
        // VID-07 dejaba kcalShift=NaN en sus primeros 30 cuadros.
        [Test, Category("AC-WKT-13-08"), Category("AC-WKT-17-11")]
        public void VideosLeaveTheirFrameValuesWithoutNaN()
        {
            var it4b = RepoPaths.Combine(It4b, "manifest.json");
            if (!File.Exists(it4b))
            {
                Assert.Ignore("sin el manifest de it4b: lo produce capture-snapshots.ps1");
            }

            var videos = 0;
            foreach (var shot in Shots(it4b))
            {
                var id = shot["id"].AsString();
                if (!id.StartsWith("VID-", StringComparison.Ordinal))
                {
                    continue;
                }

                videos++;
                Assert.That(shot.TryGet("values", out var values) && !values.IsNull, Is.True, id + ": sin valores por cuadro");
                var count = 0;
                foreach (var value in values.Items)
                {
                    count++;
                    Assert.That(value.AsString(), Does.Not.Contain("NaN"), id + ", cuadro " + count);
                }

                Assert.That(count, Is.GreaterThan(0), id + ": sin valores por cuadro");
            }

            Assert.That(videos, Is.EqualTo(8), "VID-01 a VID-08");
        }

        [Test, Category("QA-G4bT-11")]
        public void BuildCliEnablesFrameTimingOnlyForTheSimulatorBuild()
        {
            var cli = File.ReadAllText(RepoPaths.Combine(BuildCli));
            Assert.That(cli, Does.Contain("var previousFrameTiming = PlayerSettings.enableFrameTimingStats;"));
            Assert.That(cli, Does.Contain("if (request.Simulator)\n                {\n                    PlayerSettings.enableFrameTimingStats = true;").Or.Contain("if (request.Simulator)\r\n                {\r\n                    PlayerSettings.enableFrameTimingStats = true;"),
                "solo en el build de desarrollo con simulador");
            Assert.That(cli, Does.Contain("PlayerSettings.enableFrameTimingStats = previousFrameTiming;"), "y se restaura en finally");
        }

        [Test, Category("AC-HIS-20-04")]
        public void SnapshotOutputsHaveNoMachinePaths()
        {
            foreach (var file in new[] { "manifest.json", "INDEX.md" })
            {
                var path = RepoPaths.Combine(It4b, file);
                if (!File.Exists(path))
                {
                    Assert.Ignore("sin " + RepoPaths.Relative(path) + ": lo produce capture-snapshots.ps1");
                }

                var text = File.ReadAllText(path);
                Assert.That(MachinePath.IsMatch(text), Is.False, RepoPaths.Relative(path) + " lleva una ruta de la PC");
            }
        }

        private static IEnumerable<JsonNode> Shots(string manifest)
        {
            var root = JsonParser.Parse(File.ReadAllText(manifest));
            if (!root.TryGet("shots", out var shots))
            {
                yield break;
            }

            foreach (var shot in shots.Items)
            {
                yield return shot;
            }
        }
    }
}
