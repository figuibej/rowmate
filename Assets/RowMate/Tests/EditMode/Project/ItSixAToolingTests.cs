using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I6T · Las herramientas de la It. 6a (06a-escena-lago/02-tech-design §6.1, §6.2, §6.3, §4.5, §7.0): el oráculo de la escena
    // (run-oracle.ps1 -Scene) adentro de la guardia del legacy y sin nombrar el módulo de credenciales; las referencias LEG sin
    // rutas de la PC; y, desde que existen (WP-B0, B2 y B3), la guardia de ProjectSettings en los tres scripts de Unity, el conjunto
    // it6a por defecto y los argumentos [PERF] de la escena solo en desarrollo. Lee los fuentes como texto (dotnet no compila el
    // Editor ni el arnés). Lo de WP-B queda ignorado con el motivo mientras su archivo no existe.
    [TestFixture]
    public sealed class ItSixAToolingTests
    {
        private const string Oracle = "tools/oracle/run-oracle.ps1";
        private const string OracleReadme = "tools/oracle/README.md";
        private const string Vectors = "tools/oracle/engine/test/scene_vectors_test.dart";
        private const string Images = "tools/oracle/engine/test/scene_legacy_images_test.dart";
        private const string Snapshots = "docs/migration/iterations/06a-escena-lago/snapshots";
        private const string UnityTests = "tools/unity/run-unity-tests.ps1";
        private const string Captures = "tools/unity/capture-snapshots.ps1";
        private const string Build = "tools/build/build.ps1";
        private const string Guard = "tools/unity/ProjectSettingsGuard.ps1";
        private const string SceneCatalog = "Assets/RowMate/Snapshots/SceneCatalog.cs";
        private const string Notices = "Assets/RowMate/Editor/Build/NoticesPostprocessor.cs";
        private const string AppRoot = "Assets/RowMate/Bootstrap/AppRoot.cs";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string DevelopmentOnly = "#if UNITY_EDITOR || DEVELOPMENT_BUILD";

        private static readonly string[] SceneFiles =
        {
            "scene_random.json", "scene_palette.json", "scene_light.json", "scene_sky.json", "scene_lake.json", "scene_water.json",
        };

        [Test, Category("AC-SCN-01-14"), Category("AC-SCN-14-02")]
        public void TheSceneOracleRunsInsideTheLegacyGuard()
        {
            var script = Read(Oracle);
            Assert.That(script, Does.Contain("[switch]$Scene,"), "-Scene");
            Assert.That(script, Does.Contain("$flutterProject = $Engine -or $Scene"), "los mismos controles del proyecto engine/ que -Engine");
            const string Run = "& $FlutterExe test test/scene_vectors_test.dart test/scene_legacy_images_test.dart";
            var before = script.IndexOf("$snapshotBefore = Get-LegacySnapshot", StringComparison.Ordinal);
            var run = script.IndexOf(Run, StringComparison.Ordinal);
            var after = script.IndexOf("$snapshotAfter = Get-LegacySnapshot", StringComparison.Ordinal);
            Assert.That(before, Is.GreaterThan(0), "la guardia del legacy antes");
            Assert.That(run, Is.GreaterThan(before), "el oráculo de la escena corre después de la guardia de antes");
            Assert.That(after, Is.GreaterThan(run), "y antes de la de después");
            Assert.That(script.IndexOf("$forbidden = 'str' + 'ava'", StringComparison.Ordinal), Is.InRange(0, before), "R-02 antes de todo");
            Assert.That(script, Does.Contain("$legOut = Join-Path $repo 'docs\\migration\\iterations\\06a-escena-lago\\snapshots\\legacy'"));
            Assert.That(script, Does.Contain("$env:ROWMATE_LEG_OUT = $legOut"));
            Assert.That(script, Does.Contain("Remove-Item Env:ROWMATE_LEG_OUT -ErrorAction SilentlyContinue"), "la variable no queda puesta");
            Assert.That(script, Does.Contain("flutter test de la escena"), "un fallo del oráculo de la escena se informa y cuenta");
            AssertAscii(Oracle);
        }

        [Test, Category("AC-SCN-01-14"), Category("AC-APP-19-03")]
        public void TheSceneOracleOnlyReadsTheLegacyScene()
        {
            // R-02: ninguno de los dos nombra el módulo de credenciales; del legacy importan solo la escena del inmersivo.
            var forbidden = "str" + "ava";
            var import = new Regex(@"^import\s+'package:rowmate/([^']+)'", RegexOptions.Multiline);
            foreach (var file in new[] { Vectors, Images })
            {
                var text = Read(file);
                Assert.That(text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1), file + ": R-02");
                var imports = 0;
                foreach (Match match in import.Matches(text))
                {
                    imports++;
                    Assert.That(match.Groups[1].Value, Does.StartWith("features/workout/scene/"), file + ": " + match.Value);
                }

                Assert.That(imports, Is.GreaterThan(3), file + ": lee el código del legacy");
            }

            Assert.That(Read(Vectors), Does.Contain("import '../../lib/golden.dart';"), "el formato de los vectores");
            Assert.That(Read(Vectors), Does.Contain("const String generator = 'tools/oracle/engine/test/scene_vectors_test.dart';"));
            Assert.That(Read(Vectors), Does.Contain("ROWMATE_ORACLE_OUT").And.Contain("ROWMATE_LEGACY_COMMIT"));
            Assert.That(Read(Images), Does.Contain("ROWMATE_LEG_OUT"), "las referencias van a la carpeta que le pasa el script");
        }

        [Test, Category("AC-SCN-01-14")]
        public void TheSceneOracleIsDocumented()
        {
            var readme = Read(OracleReadme);
            Assert.That(readme, Does.Contain("tools/oracle/run-oracle.ps1 -Scene"), "el comando");
            Assert.That(readme, Does.Contain("engine/test/scene_vectors_test.dart").And.Contain("engine/test/scene_legacy_images_test.dart"));
            foreach (var name in SceneFiles)
            {
                Assert.That(readme, Does.Contain(name), "README: " + name);
            }

            Assert.That(readme, Does.Contain("LEG-01").And.Contain("LEG-07"), "las referencias del legacy");
            Assert.That(readme, Does.Contain("Bit a bit").Or.Contain("bit a bit"), "el comparador de cada valor");
        }

        // El repo es público: ni las referencias LEG (PNG, con sus metadatos), ni las tomas de la 6a, ni los vectores de la escena, ni
        // el oráculo tienen una ruta de la PC (la carpeta del usuario, la temporal, file:/// o la caché de pub).
        [Test, Category("AC-APP-19-03")]
        public void NoMachinePathsInTheSceneOracleAndSnapshots()
        {
            var files = new System.Collections.Generic.List<string>();
            if (Directory.Exists(RepoPaths.Combine(Snapshots)))
            {
                files.AddRange(Directory.GetFiles(RepoPaths.Combine(Snapshots), "*", SearchOption.AllDirectories));
            }

            foreach (var name in SceneFiles)
            {
                files.Add(RepoPaths.Golden(name));
            }

            files.Add(RepoPaths.Combine(Vectors));
            files.Add(RepoPaths.Combine(Images));
            var offenders = new System.Collections.Generic.List<string>();
            var legacy = 0;
            foreach (var file in files)
            {
                legacy += file.Replace('\\', '/').Contains("/snapshots/legacy/LEG-") ? 1 : 0;
                var text = Latin1(File.ReadAllBytes(file));
                var match = RepoHygieneTests.MachinePath.Match(text);
                if (match.Success)
                {
                    offenders.Add(RepoPaths.Relative(file) + ": " + match.Value);
                }
            }

            Assert.That(legacy, Is.GreaterThanOrEqualTo(10), "LEG-01 a LEG-07 y las tres con marcas");
            Assert.That(offenders, Is.Empty, "rutas de la PC (el repo es público):\n" + string.Join("\n", offenders));
        }

        // AC-HIS-20-04: las corridas de Unity de la 6a (pruebas, capturas) usan una carpeta de datos temporal (ROWMATE_DATA_DIR) y
        // la devuelven como estaba; el oráculo de la escena no abre ninguna base (corre la escena del legacy, sin servicios).
        [Test, Category("AC-HIS-20-04")]
        public void UnityRunsNeverTouchTheUserData()
        {
            foreach (var script in new[] { UnityTests, Captures })
            {
                var text = Read(script);
                Assert.That(text, Does.Contain("$previousDataDir = $env:ROWMATE_DATA_DIR"), script);
                Assert.That(text, Does.Contain("$env:ROWMATE_DATA_DIR = $dataDir"), script);
                Assert.That(text, Does.Contain("if ($null -eq $previousDataDir) { Remove-Item Env:ROWMATE_DATA_DIR -ErrorAction SilentlyContinue } else { $env:ROWMATE_DATA_DIR = $previousDataDir }"),
                    script + ": la deja como estaba");
            }

            foreach (var file in new[] { Vectors, Images })
            {
                var text = Read(file);
                Assert.That(text, Does.Not.Contain("DatabaseService").And.Not.Contain("SharedPreferences").And.Not.Contain("sqflite"), file);
            }
        }

        // QA-G6aT-05: ninguna corrida de Unity deja cambiados ProjectSettings ni Assets/RowMate/Settings. La guardia es de WP-B0 y
        // cada script la engancha en su paquete: run-unity-tests.ps1 (B0), build.ps1 (B2, desde NoticesPostprocessor) y
        // capture-snapshots.ps1 (B3, desde SceneCatalog).
        [Test, Category("AC-BASE-09")]
        public void TheUnityScriptsUseTheProjectSettingsGuard()
        {
            if (!File.Exists(RepoPaths.Combine(Guard)))
            {
                Assert.Ignore("pendiente WP-B0: todavía no existe " + Guard);
            }

            AssertAscii(Guard);
            Assert.That(Read(UnityTests), Does.Contain("ProjectSettingsGuard.ps1"), UnityTests + " (B0)");
            if (File.Exists(RepoPaths.Combine(Notices)))
            {
                Assert.That(Read(Build), Does.Contain("ProjectSettingsGuard.ps1"), Build + " (B2)");
            }

            if (File.Exists(RepoPaths.Combine(SceneCatalog)))
            {
                Assert.That(Read(Captures), Does.Contain("ProjectSettingsGuard.ps1"), Captures + " (B3)");
            }
        }

        // §5: el conjunto it6a es el de por defecto desde WP-B3, con su carpeta; it4b e it5 siguen (la regeneración única).
        [Test, Category("AC-BASE-09")]
        public void TheIt6aSetIsTheDefault()
        {
            if (!File.Exists(RepoPaths.Combine(SceneCatalog)))
            {
                Assert.Ignore("pendiente WP-B3: todavía no existe " + SceneCatalog);
            }

            var script = Read(Captures);
            Assert.That(script, Does.Contain("[string]$Set = 'it9',"));
            Assert.That(script, Does.Contain("return '" + Snapshots + "'"), "su carpeta de salida");
            Assert.That(script, Does.Contain("'it5'").And.Contain("'it4b'"), "it4b e it5 siguen");
            AssertAscii(Captures);
        }

        // §4.5: los argumentos de medida de la escena solo existen en el build de desarrollo y el inspector del build normal los busca
        // (desde WP-B2).
        [Test, Category("AC-BASE-09")]
        public void TheSceneArgumentsAreDevelopmentOnly()
        {
            var root = Read(AppRoot);
            if (!root.Contains("\"-rowmate-perf-scene\""))
            {
                Assert.Ignore("pendiente WP-B2: AppRoot todavía no tiene los argumentos [PERF] de la escena");
            }

            var inspector = Read(Inspector);
            foreach (var argument in new[] { "-rowmate-perf-scene", "-rowmate-scene-delay", "-rowmate-quality", "-rowmate-perf-hours", "-rowmate-perf-completion",
                                              "-rowmate-perf-long-series", "-rowmate-perf-env", "-rowmate-perf-hour" })
            {
                var token = "\"" + argument + "\"";
                var at = root.IndexOf(token, StringComparison.Ordinal);
                Assert.That(at, Is.GreaterThan(0), "AppRoot: " + argument);
                var open = root.LastIndexOf(DevelopmentOnly, at, StringComparison.Ordinal);
                var close = root.LastIndexOf("#endif", at, StringComparison.Ordinal);
                Assert.That(open, Is.GreaterThan(close), "AppRoot: " + argument + " fuera de " + DevelopmentOnly);
                Assert.That(inspector, Does.Contain(token), "el build normal se revisa también por " + argument);
            }

            Assert.That(inspector, Does.Contain("THIRD_PARTY_NOTICES.md"), "el build lleva el aviso de Dart (QA-G6aT-19)");
        }

        // QA-V6a-REN-01 (ronda 1 de la verificación): el detector de [PERF] sobre el registro del player cuenta las líneas que el player
        // escribe de verdad ("Uploaded shader variant to the GPU driver: …") de la escena y de URP DESPUÉS de la marca de Ready. Se corre
        // de verdad con registros de prueba armados acá: el control positivo (una subida de UberPost después de la marca: 2 y la línea),
        // el negativo (la UI y lo de antes de la marca: 0) y sin la marca (3, NO VERIFICABLE).
        [Test, Category("AC-WKT-13-15")]
        public void ThePerfLogCheckCountsShaderUploadsAfterReady()
        {
            const string Script = "tools/build/perf-log-check.ps1";
            AssertAscii(Script);
            var text = Read(Script);
            Assert.That(text, Does.Contain("'" + RowMate.Core.Scene.ShaderWorkLog.ReadyMark + "'"), "la misma marca que FrameTimeProbe");
            var powershell = WindowsPowerShell();
            var work = Path.Combine(TempDatabase.Root, "perf-log-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            Directory.CreateDirectory(work);
            try
            {
                string Upload(string shader) => "Uploaded shader variant to the GPU driver: " + shader + " (instance 0x10), pass: <Unnamed Pass 0>, stage: pixel, keywords <no keywords>, time: 0.9 ms";
                var before = new[] { "[SCENE] escena: nivel High", Upload("RowMate/Scene/Sky"), Upload("Hidden/Universal Render Pipeline/Bloom") };
                var mark = RowMate.Core.Scene.ShaderWorkLog.ReadyMark;
                var positive = Path.Combine(work, "positivo.log");
                File.WriteAllLines(positive, Concat(before, new[] { mark, Upload("Hidden/Internal-UIRDefault"), Upload("Hidden/Universal Render Pipeline/UberPost") }));
                var negative = Path.Combine(work, "negativo.log");
                File.WriteAllLines(negative, Concat(before, new[] { mark, Upload("Hidden/Internal-UIRDefault"), "[PERF] entrada 1: libre" }));
                var unmarked = Path.Combine(work, "sin-marca.log");
                File.WriteAllLines(unmarked, before);

                var found = RunPowerShell(powershell, "-File " + Script + " -Log " + Quote(positive));
                Assert.That(found.Code, Is.EqualTo(2), found.Output);
                Assert.That(found.Output, Does.Contain("despues de Ready: 1 "), found.Output);
                Assert.That(found.Output, Does.Contain("UberPost"), found.Output);
                var clean = RunPowerShell(powershell, "-File " + Script + " -Log " + Quote(negative));
                Assert.That(clean.Code, Is.EqualTo(0), clean.Output);
                Assert.That(clean.Output, Does.Contain("despues de Ready: 0 "), clean.Output);
                var none = RunPowerShell(powershell, "-File " + Script + " -Log " + Quote(unmarked));
                Assert.That(none.Code, Is.EqualTo(3), none.Output);
            }
            finally
            {
                try
                {
                    Directory.Delete(work, true);
                }
                catch (IOException)
                {
                    // La carpeta temporal se va con TempDatabase.
                }
            }
        }

        private static string[] Concat(string[] a, string[] b)
        {
            var all = new string[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }

        private static string WindowsPowerShell()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Assert.Ignore("los scripts se prueban con Windows PowerShell 5.1: solo en Windows (dotnet y Unity)");
            }

            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            Assert.That(File.Exists(powershell), Is.True, "falta Windows PowerShell 5.1");
            return powershell;
        }

        private static (int Code, string Output) RunPowerShell(string powershell, string arguments)
        {
            var start = new System.Diagnostics.ProcessStartInfo(powershell, "-NoProfile -NonInteractive " + arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = RepoPaths.Root,
            };
            start.EnvironmentVariables.Remove("PSModulePath");
            using (var process = System.Diagnostics.Process.Start(start))
            {
                Assert.That(process, Is.Not.Null, "no se pudo iniciar powershell.exe");
                var stdout = process!.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(90000))
                {
                    process.Kill();
                    Assert.Fail("powershell.exe no terminó en 90 s: " + arguments);
                }

                process.WaitForExit();
                return (process.ExitCode, stdout.Result + stderr.Result);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }

        // Cada byte como un carácter (ISO-8859-1): así se ven también las rutas guardadas en los metadatos de un PNG.
        private static string Latin1(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length);
            foreach (var b in bytes)
            {
                sb.Append((char)b);
            }

            return sb.ToString();
        }

        private static void AssertAscii(string relative)
        {
            var bytes = File.ReadAllBytes(RepoPaths.Combine(relative));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.That(bytes[i], Is.LessThan(128), relative + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
            }
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }
    }
}
