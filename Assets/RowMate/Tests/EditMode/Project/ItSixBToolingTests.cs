using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Scene.Geometry;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I6BT · Las herramientas de la It. 6b que se revisan por texto (06b-agua-avanzada/02-tech-design §7, §8; DT6b-14, DT6b-18, DT6b-24):
    // los argumentos del agua solo en desarrollo, el set it6b de las capturas en su carpeta, los sets viejos con la degradada fija y la
    // huella del mapa de normales. Son de archivos de WP-B (AppRoot/SceneBridge, capture-snapshots.ps1, SnapshotCli, UiHostOptions,
    // WaterNormalBaker): mientras WP-B no los escribió quedan ignorados con el motivo, y WP-B quita la guarda al cerrar (no se cierra la
    // iteración con un Ignore, como en la It. 15).
    [TestFixture]
    public sealed class ItSixBToolingTests
    {
        private const string Captures = "tools/unity/capture-snapshots.ps1";
        private const string SnapshotCli = "Assets/RowMate/Snapshots/SnapshotCli.cs";
        private const string UiHostOptions = "Assets/RowMate/Tests/UiSupport/UiHostOptions.cs";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string WaterJson = "Assets/RowMate/Art/Textures/Water/water.json";
        private const string It6bFolder = "docs/migration/iterations/06b-agua-avanzada/snapshots";
        private const string DevelopmentOnly = "#if UNITY_EDITOR || DEVELOPMENT_BUILD";

        private static readonly string[] WaterArguments = { "-rowmate-water", "-rowmate-water-fail", "-rowmate-perf-early", "-rowmate-perf-gpu-load" };

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }

        [Test, Category("AC-6B-06")]
        public void WaterArgumentsAreDevelopmentOnly()
        {
            // DT6b-14: -rowmate-water, -rowmate-water-fail, -rowmate-perf-early y -rowmate-perf-gpu-load solo con UNITY_EDITOR ||
            // DEVELOPMENT_BUILD; el inspector del build normal los busca.
            var files = SourceScan.CsFiles("Bootstrap", "Scene", "App", "Platform");
            var seen = 0;
            foreach (var file in files)
            {
                var text = File.ReadAllText(file).Replace("\r\n", "\n");
                foreach (var argument in WaterArguments)
                {
                    var token = "\"" + argument + "\"";
                    var at = text.IndexOf(token, StringComparison.Ordinal);
                    while (at >= 0)
                    {
                        seen++;
                        var open = text.LastIndexOf(DevelopmentOnly, at, StringComparison.Ordinal);
                        var close = text.LastIndexOf("#endif", at, StringComparison.Ordinal);
                        Assert.That(open, Is.GreaterThan(close), RepoPaths.Relative(file) + ": " + argument + " fuera de " + DevelopmentOnly);
                        at = text.IndexOf(token, at + token.Length, StringComparison.Ordinal);
                    }
                }
            }

            if (seen == 0)
            {
                Assert.Ignore("pendiente WP-B: todavía ningún archivo lee los argumentos del agua (" + string.Join(", ", WaterArguments) + ")");
            }

            var inspector = Read(Inspector);
            foreach (var argument in WaterArguments)
            {
                Assert.That(inspector, Does.Contain("\"" + argument + "\""), "el build normal se revisa también por " + argument);
            }
        }

        [Test, Category("AC-6B-33")]
        public void CaptureSetIt6bWritesToItsFolder()
        {
            var script = Read(Captures);
            if (!script.Contains("'it6b'"))
            {
                Assert.Ignore("pendiente WP-B: capture-snapshots.ps1 todavía no tiene el set it6b");
            }

            Assert.That(script, Does.Contain("[string]$Set = 'it6b',"), "el set de la 6b, por defecto");
            Assert.That(script, Does.Contain("if ($Set -eq 'it6b') { return '" + It6bFolder + "' }"), "su carpeta, y no la de otra iteración");
            Assert.That(Regex.IsMatch(script, @"ValidateSet\([^)]*'it6b'"), Is.True, "en el ValidateSet");
            foreach (var c in script)
            {
                Assert.That(c, Is.LessThan((char)128), Captures + " solo ASCII (PowerShell 5.1)");
            }
        }

        [Test, Category("AC-6B-03"), Category("AC-6B-09"), Category("AC-6B-35")]
        public void OldSetsDrawDegradedWater()
        {
            // DT6b-24 (G6b-03): la evidencia de la 6a a la 15 no cambia de agua: UiHostOptions.SceneWater vale Degraded por defecto y SnapshotCli
            // fija la degradada en it2 a it15 y thumbs; solo it6b (y lo explícito) elige otra.
            var cli = Read(SnapshotCli);
            if (!cli.Contains("WaterFor("))
            {
                Assert.Ignore("pendiente WP-B: SnapshotCli todavía no tiene WaterFor(set)");
            }

            Assert.That(Regex.IsMatch(cli, @"public\s+static\s+WaterForce\s+WaterFor\s*\(\s*string\s+set\s*\)"), Is.True, "SnapshotCli.WaterFor(set)");
            Assert.That(cli, Does.Contain("\"it6b\""), "it6b elige Auto");
            Assert.That(cli, Does.Contain("WaterForce.Degraded"), "lo demás, la degradada");
            var options = Read(UiHostOptions);
            Assert.That(Regex.IsMatch(options, @"\bSceneWater\b[^;]*=\s*WaterForce\.Degraded\s*;"), Is.True, "UiHostOptions.SceneWater = Degraded por defecto");
        }

        [Test, Category("AC-6B-37")]
        public void WaterNormalFootprint()
        {
            // DT6b-18: la huella del mapa de normales horneado (semilla y SHA-256 de los bytes RGBA de WaterNormalMap) en water.json; dotnet la
            // recalcula. Los bytes son enteros (sin depender del runtime): el Editor y .NET dan lo mismo.
            var path = RepoPaths.Combine(WaterJson);
            if (!File.Exists(path))
            {
                Assert.Ignore("pendiente WP-B: todavía no hay mapa de normales horneado (" + WaterJson + ")");
            }

            var json = Read(WaterJson);
            var seed = Regex.Match(json, @"""seed""\s*:\s*(\d+)");
            var size = Regex.Match(json, @"""size""\s*:\s*(\d+)");
            var sha = Regex.Match(json, @"""sha256""\s*:\s*""([0-9a-f]{64})""");
            Assert.That(seed.Success && size.Success && sha.Success, Is.True, "water.json: seed, size y sha256");
            var n = int.Parse(size.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var bytes = new byte[WaterNormalMap.ByteCount(n)];
            WaterNormalMap.Fill(n, uint.Parse(seed.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), bytes);
            using (var hash = SHA256.Create())
            {
                var hex = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                Assert.That(hex, Is.EqualTo(sha.Groups[1].Value), "mapa de normales viejo: volver a hornear (WaterNormalBaker)");
            }

            Assert.That(File.Exists(RepoPaths.Combine("Assets/RowMate/Art/Textures/Water/water-normal.png")), Is.True);
        }
    }
}
