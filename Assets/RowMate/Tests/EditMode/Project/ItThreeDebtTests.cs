using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // Deuda de la It. 3 saldada al empezar la It. 4a (docs/migration/iterations/04a-motor-hud-funcional/00-deuda-it3.md): lo que
    // se revisa sin Unity y que ci-core vigila en cada cambio. El motion (QA-V3-02) y el arnés (QA-V3-04) se prueban montados,
    // en PlayMode (RoutineMotionDebtTests). Lo que corre de verdad (build.ps1, capture-cap27.ps1, capture-window.ps1) usa
    // Windows PowerShell 5.1, el de los scripts, y nunca abre Unity, un build ni una ventana: solo en Windows.
    [TestFixture]
    public sealed class ItThreeDebtTests
    {
        private const string Build = "tools/build/build.ps1";
        private const string Cap27 = "tools/unity/capture-cap27.ps1";
        private const string Window = "tools/unity/capture-window.ps1";
        private const string Readme = "tools/unity/README.md";

        private static string Read(string relative)
        {
            var path = RepoPaths.Combine(relative);
            Assert.That(File.Exists(path), Is.True, "falta " + relative);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        // ── QA-V3-05: los motivos de [Ignore] dicen dónde se verifica ──

        // Ningún [Ignore] manda a una iteración cerrada (It. 1, 2 o 3: lo de esas iteraciones ya se verificó o tiene su lugar), y
        // cada prueba que un motivo nombra ("Se verifica en PlayMode: Clase.Método") existe.
        [Test, Category("QA-V3-05")]
        public void IgnoreReasonsNameWhereTheyAreVerified()
        {
            var ignore = new Regex(@"(?<!Assert\.)\bIgnore\(""((?:[^""\\]|\\.)*)""\)");
            var closed = new Regex(@"→\s*It\.\s*[123](?![0-9ab])");
            var cited = new Regex(@"\b([A-Z][A-Za-z0-9]*Tests)\.([A-Z][A-Za-z0-9_]*)\b");
            var sources = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in SourceScan.CsFiles("Tests"))
            {
                sources[file] = SourceScan.StripComments(File.ReadAllText(file));
            }

            var reasons = 0;
            var stale = new List<string>();
            var missing = new List<string>();
            foreach (var pair in sources)
            {
                foreach (Match match in ignore.Matches(pair.Value))
                {
                    reasons++;
                    var reason = match.Groups[1].Value;
                    var where = RepoPaths.Relative(pair.Key) + ":" + SourceScan.LineOf(pair.Value, match.Index);
                    if (closed.IsMatch(reason))
                    {
                        stale.Add(where + ": " + reason);
                    }

                    foreach (Match test in cited.Matches(reason))
                    {
                        if (!TestExists(sources, test.Groups[1].Value, test.Groups[2].Value))
                        {
                            missing.Add(where + ": " + test.Value);
                        }
                    }
                }
            }

            Assert.That(reasons, Is.GreaterThanOrEqualTo(15), "el recorrido no encontró los [Ignore]");
            Assert.That(stale, Is.Empty, "motivos que mandan a una iteración cerrada:\n" + string.Join("\n", stale));
            Assert.That(missing, Is.Empty, "motivos que nombran una prueba que no existe:\n" + string.Join("\n", missing));
        }

        private static bool TestExists(Dictionary<string, string> sources, string type, string method)
        {
            var declaration = new Regex(@"\b(?:IEnumerator|void)\s+" + Regex.Escape(method) + @"\s*\(");
            foreach (var code in sources.Values)
            {
                if (Regex.IsMatch(code, @"\bclass\s+" + Regex.Escape(type) + @"\b") && declaration.IsMatch(code))
                {
                    return true;
                }
            }

            return false;
        }

        // ── QA-V3-07: el comentario de RmTextField dice la regla del legacy y la diferencia (DV-20) ──

        [Test, Category("QA-V3-07")]
        public void SingleLineCommentTellsTheLegacyRuleAndTheDifference()
        {
            var field = Read("Assets/RowMate/Presentation/Controls/RmTextField.cs");
            Assert.That(field, Does.Not.Contain("(singleLineFormatter del legacy)"), "no es la regla del legacy");
            Assert.That(field, Does.Contain("singleLineFormatter = deny('\\n')"), "el legacy solo quita \\n");
            Assert.That(field, Does.Contain("quitar también \\r y \\t es DV-20"));
        }

        // ── QA-V3-10: Presentation no descarta tareas; Guardar, Eliminar y Reintentar registran lo inesperado ──

        // Un "_ = tarea" deja sin observar la excepción que no sea de la base (RoutinesStore ya registra esas): con Forget queda
        // en el registro con su tag (TestMainThreadContextTests.ForgetLogsFaultsThroughTheLogger prueba Forget).
        [Test, Category("QA-V3-10")]
        public void PresentationNeverDiscardsATask()
        {
            var discard = new Regex(@"(?<![\w.])_\s*=(?![=>])");
            var offenders = new List<string>();
            var files = SourceScan.CsFiles("Presentation", "SimulatorPresentation");
            foreach (var file in files)
            {
                foreach (var hit in SourceScan.Matches(file, new[] { discard }, true))
                {
                    offenders.Add(hit);
                }
            }

            Assert.That(files.Count, Is.GreaterThan(40));
            Assert.That(offenders, Is.Empty, "tareas descartadas con _ = (usar .Forget(logger, tag)):\n" + string.Join("\n", offenders));
            Assert.That(Read("Assets/RowMate/Presentation/Routines/RoutineEditorPage.cs"), Does.Contain("() => SaveAsync().Forget(_ui.Logger, RoutinesStore.Tag)"),
                "Guardar");
            var screen = Read("Assets/RowMate/Presentation/Routines/RoutinesScreenView.cs");
            Assert.That(screen, Does.Contain("() => _ui.App.Routines.LoadAsync().Forget(_ui.Logger, RoutinesStore.Tag)"), "Reintentar");
            Assert.That(screen, Does.Contain("Delete(routine.Id.Value).Forget(_ui.Logger, RoutinesStore.Tag);"), "Eliminar");
        }

        // ── QA-V3-08: los builds de desarrollo, siempre en la misma carpeta ──

        [Test, Category("QA-V3-08")]
        public void DevelopmentBuildsAlwaysUseTheSameFolder()
        {
            var build = Read(Build);
            Assert.That(build, Does.Contain("[switch]$AllowNewFolder"));
            Assert.That(build, Does.Contain("$development = $Simulator -or $SmokeTest -or $DbSmoke"));
            Assert.That(build, Does.Contain("$fixedOut = 'Builds/' + $Target + '/' + $variant"));
            Assert.That(build, Does.Contain("if ($development -and -not $Simulator) { $fixedOut = 'Builds/' + $Target + '/dev-smoke' }"));
            Assert.That(build, Does.Contain("elseif ($development -and $Target -eq 'Windows' -and -not $AllowNewFolder) {"));
            var refuse = build.IndexOf("        exit 5", StringComparison.Ordinal);
            var unity = build.IndexOf("$unityExe = Resolve-Unity", StringComparison.Ordinal);
            Assert.That(refuse, Is.GreaterThan(0).And.LessThan(unity), "se niega antes de buscar y abrir Unity: no toca nada");
            Assert.That(Read("Assets/RowMate/Editor/Build/BuildRequest.cs"), Does.Contain("simulator ? \"simulator\" : smokeTest ? \"dev-smoke\" : \"normal\""),
                "BuildCli sin -rowmateOutput usa las mismas carpetas");
            var readme = Read(Readme);
            foreach (var text in new[] { "Firewall de Windows", "Builds/Windows/simulator", "Builds/Windows/dev-smoke", "`Cancelar`", "-AllowNewFolder" })
            {
                Assert.That(readme, Does.Contain(text), "tools/unity/README.md explica " + text);
            }
        }

        // El de verdad, con un Unity.exe que no existe: la negativa sale con 5 sin crear la carpeta, y lo que pasa la negativa
        // llega a la búsqueda de Unity (12) sin compilar nada.
        [Test, Category("QA-V3-08")]
        public void DevelopmentBuildRefusesANewFolderForReal()
        {
            var powershell = WindowsPowerShell();
            var work = Path.Combine(TempDatabase.Root, "build-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            Directory.CreateDirectory(work);
            try
            {
                var other = Path.Combine(work, "otra carpeta");
                var unity = Quote(Path.Combine(work, "no-existe", "Unity.exe"));
                var script = "-File " + Quote(RepoPaths.Combine(Build));
                var simulator = RunPowerShell(powershell, script + " -Simulator -Out " + Quote(other) + " -Unity " + unity);
                Assert.That(simulator.Code, Is.EqualTo(5), simulator.Output);
                Assert.That(simulator.Output, Does.Contain("los builds de desarrollo van siempre a Builds/Windows/simulator"));
                var smoke = RunPowerShell(powershell, script + " -DbSmoke -Out " + Quote(other) + " -Unity " + unity);
                Assert.That(smoke.Code, Is.EqualTo(5), smoke.Output);
                Assert.That(smoke.Output, Does.Contain("los builds de desarrollo van siempre a Builds/Windows/dev-smoke"));
                Assert.That(Directory.Exists(other), Is.False, "la negativa no crea nada");

                foreach (var passes in new[]
                         {
                             " -Simulator -Out " + Quote(other) + " -AllowNewFolder",
                             " -Simulator -Out Builds/Windows/simulator",
                             " -Simulator",
                             " -Out " + Quote(other),
                         })
                {
                    var run = RunPowerShell(powershell, script + passes + " -Unity " + unity);
                    Assert.That(run.Code, Is.EqualTo(12), passes + ": " + run.Output);
                    Assert.That(run.Output, Does.Contain("[build] no existe "), passes);
                }
            }
            finally
            {
                DeleteFolder(work);
            }
        }

        // ── QA-V3-12: la foto de CAP-27 sin franjas del escritorio y nunca directo al repo ──

        [Test, Category("QA-V3-12")]
        public void Cap27PhotoNeverGoesStraightToTheRepo()
        {
            var cap27 = Read(Cap27);
            Assert.That(cap27, Does.Contain("    [string]$Out = '',\n"));
            Assert.That(cap27, Does.Contain("'RowMate.Cap27-foto\\CAP-27-ventana-build-normal.png'"), "por defecto, una carpeta temporal");
            Assert.That(cap27, Does.Not.Contain("docs/migration/iterations/03-rutinas/snapshots/CAP-27"));
            var guard = cap27.IndexOf("if ($outFull.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {", StringComparison.Ordinal);
            Assert.That(guard, Is.GreaterThan(0));
            foreach (var later in new[] { "'AppsUseLightTheme'", "$unityExe = Resolve-Unity", "[System.Diagnostics.Process]::Start($info)", "Copy-Item -LiteralPath" })
            {
                Assert.That(guard, Is.LessThan(cap27.IndexOf(later, StringComparison.Ordinal)), "la negativa va antes de " + later);
            }

            Assert.That(cap27, Does.Contain("Write-Host '[cap27] Mirala antes de pasarla al repo"), "dice cómo pasarla después de mirarla");

            var powershell = WindowsPowerShell();
            var work = Path.Combine(TempDatabase.Root, "cap27-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            Directory.CreateDirectory(work);
            try
            {
                // -Rehearsal y un Unity.exe que no existe: aunque la negativa faltara, el ensayo no toca nada del usuario y sin
                // Unity sale con 12 antes de sembrar.
                var unity = Quote(Path.Combine(work, "no-existe", "Unity.exe"));
                var script = "-File " + Quote(RepoPaths.Combine(Cap27)) + " -Rehearsal -Unity " + unity;
                var refused = RunPowerShell(powershell, script + " -Out docs/migration/iterations/03-rutinas/snapshots/CAP-27-prueba.png");
                Assert.That(refused.Code, Is.EqualTo(5), refused.Output);
                Assert.That(refused.Output, Does.Contain("[cap27] la foto no se escribe directo en el repo"));
                Assert.That(File.Exists(RepoPaths.Combine("docs/migration/iterations/03-rutinas/snapshots/CAP-27-prueba.png")), Is.False);
                var passes = RunPowerShell(powershell, script);
                Assert.That(passes.Code, Is.EqualTo(12), "sin -Out pasa la negativa: " + passes.Output);
            }
            finally
            {
                DeleteFolder(work);
            }
        }

        // capture-window.ps1 recorta al marco visible (DWMWA_EXTENDED_FRAME_BOUNDS) y no al de GetWindowRect, que suma los
        // bordes invisibles de Windows 10 y 11. De verdad: el Add-Type y Get-VisibleFrame del script, con Windows PowerShell 5.1,
        // sobre la barra de tareas (una ventana que ya existe): no abre, no captura ni toca nada.
        [Test, Category("QA-V3-12")]
        public void WindowCaptureUsesTheVisibleFrame()
        {
            var window = Read(Window);
            Assert.That(window, Does.Contain("[DllImport(\"dwmapi.dll\")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT value, int size);"));
            var frame = BootstrapperStaticTests.Body(window, "function Get-VisibleFrame(");
            Assert.That(frame, Does.Contain("[RowMateWindow]::DwmGetWindowAttribute($Window, 9, [ref]$frame, $size)"), "DWMWA_EXTENDED_FRAME_BOUNDS = 9");
            Assert.That(window, Does.Contain("$rect = Get-VisibleFrame $handle"), "la captura y las 9 comprobaciones usan el marco visible");
            Assert.That(Regex.Matches(window, Regex.Escape("[RowMateWindow]::GetWindowRect(")).Count, Is.EqualTo(1), "GetWindowRect, solo de respaldo");

            var powershell = WindowsPowerShell();
            var addTypeAt = window.IndexOf("Add-Type @'", StringComparison.Ordinal);
            var addTypeEnd = window.IndexOf("\n'@", addTypeAt, StringComparison.Ordinal) + 3;
            var functionAt = window.IndexOf("function Get-VisibleFrame(", StringComparison.Ordinal);
            var functionEnd = window.IndexOf(frame, functionAt, StringComparison.Ordinal) + frame.Length;
            var probe = "$ErrorActionPreference = 'Stop'\n"
                        + window.Substring(addTypeAt, addTypeEnd - addTypeAt) + "\n"
                        + "[RowMateWindow]::SetProcessDPIAware() | Out-Null\n"
                        + window.Substring(functionAt, functionEnd - functionAt) + "\n"
                        + "Add-Type @'\nusing System;\nusing System.Runtime.InteropServices;\npublic static class RowMateProbe {\n"
                        + "    [DllImport(\"user32.dll\", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string className, IntPtr windowName);\n}\n'@\n"
                        + "$tray = [RowMateProbe]::FindWindow('Shell_TrayWnd', [IntPtr]::Zero)\n"
                        + "if ($tray -eq [IntPtr]::Zero) { Write-Host 'sin barra de tareas'; exit 3 }\n"
                        + "$visible = Get-VisibleFrame $tray\n"
                        + "$rect = New-Object RowMateWindow+RECT\n"
                        + "[RowMateWindow]::GetWindowRect($tray, [ref]$rect) | Out-Null\n"
                        + "Write-Host ('marco ' + $script:frameSource + ' ' + $visible.Left + ' ' + $visible.Top + ' ' + $visible.Right + ' ' + $visible.Bottom"
                        + " + ' ventana ' + $rect.Left + ' ' + $rect.Top + ' ' + $rect.Right + ' ' + $rect.Bottom)\n"
                        + "exit 0\n";
            var work = Path.Combine(TempDatabase.Root, "marco-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            Directory.CreateDirectory(work);
            try
            {
                var file = Path.Combine(work, "marco.ps1");
                File.WriteAllText(file, probe, new System.Text.UTF8Encoding(true));
                var run = RunPowerShell(powershell, "-File " + Quote(file));
                if (run.Code == 3)
                {
                    Assert.Ignore("sin barra de tareas en esta sesión (" + run.Output.Trim() + "): Get-VisibleFrame no tiene sobre qué correr");
                }

                Assert.That(run.Code, Is.EqualTo(0), run.Output);
                var numbers = Regex.Match(run.Output, @"marco (\w+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) ventana (-?\d+) (-?\d+) (-?\d+) (-?\d+)");
                Assert.That(numbers.Success, Is.True, run.Output);
                Assert.That(numbers.Groups[1].Value, Is.EqualTo("dwm"), "el marco sale de DWM: " + run.Output);
                int N(int group) => int.Parse(numbers.Groups[group].Value, System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(N(4), Is.GreaterThan(N(2)), "marco con ancho");
                Assert.That(N(5), Is.GreaterThan(N(3)), "marco con alto");
                Assert.That(N(2) >= N(6) && N(3) >= N(7) && N(4) <= N(8) && N(5) <= N(9), Is.True, "el marco visible queda dentro de GetWindowRect: " + run.Output);
            }
            finally
            {
                DeleteFolder(work);
            }
        }

        // ── QA-V3-06: el arnés precarga los atlas; los caracteres cubren todos los textos ──

        [Test, Category("QA-V3-06")]
        public void HarnessWarmsTheFontAtlasesBeforeTheFirstShot()
        {
            var start = BootstrapperStaticTests.Body(Read("Assets/RowMate/Snapshots/SnapshotRunner.cs"), "public void Start()");
            var warm = start.IndexOf("FontAtlasWarmup.Run()", StringComparison.Ordinal);
            Assert.That(warm, Is.GreaterThan(0).And.LessThan(start.IndexOf("EditorApplication.update += Update;", StringComparison.Ordinal)), "antes de la primera toma");
            var warmup = Read("Assets/RowMate/Snapshots/FontAtlasWarmup.cs");
            Assert.That(warmup, Does.Contain("foreach (var path in FontAssetPaths.All)"), "en el orden fijo de FontAssetPaths");
            Assert.That(warmup, Does.Contain("font.TryAddCharacters(characters, out var notFound, false);"), "sin características: la tabla de kerning versionada no se toca");
            var text = AtlasCharacters.Text();
            Assert.That(text.Length, Is.EqualTo(95 + 96 + 16), "con ▲ y ▼ de la It. 4a");
            Assert.That(AtlasCharacters.CoversText(0x25B2) && AtlasCharacters.CoversText(0x25BC), Is.True, "las flechas del semáforo");
            for (var i = 1; i < text.Length; i++)
            {
                Assert.That(text[i], Is.GreaterThan(text[i - 1]), "en orden y sin repetidos");
            }

            Assert.That(AtlasCharacters.IconGlyphs(false).Length + AtlasCharacters.IconGlyphs(true).Length, Is.GreaterThanOrEqualTo(Icons.All.Count));
        }

        // Todo lo que dibujan las tomas está en AtlasCharacters: los textos de los dos idiomas, lo que escriben (Type) y los
        // nombres y descripciones de las semillas. Los íconos van por su fuente (área privada) y el 📈 del subtítulo se dibuja con
        // el glifo trending_up de la fuente de íconos (StepSubtitleView).
        [Test, Category("QA-V3-06")]
        public void AtlasCharactersCoverEveryText()
        {
            var missing = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var language in new[] { "es", "en" })
            {
                var relative = "Assets/RowMate/Localization/strings." + language + ".json";
                var json = JsonParser.Parse(File.ReadAllText(RepoPaths.Combine(relative)));
                foreach (var pair in json.Properties)
                {
                    if (!pair.Key.StartsWith("@", StringComparison.Ordinal) && pair.Value.Kind == JsonKind.String)
                    {
                        Check(pair.Value.AsString(), relative + ": " + pair.Key, missing);
                    }
                }
            }

            var catalog = Read("Assets/RowMate/Snapshots/RoutinesCatalog.cs");
            var typed = Regex.Matches(catalog, @"\.Type\(""[^""]+"",\s*""((?:[^""\\]|\\.)*)""\)");
            Assert.That(typed.Count, Is.GreaterThan(10));
            foreach (Match match in typed)
            {
                Check(Regex.Unescape(match.Groups[1].Value), "RoutinesCatalog: Type", missing);
            }

            foreach (var seed in new[] { RoutineSeeds.Lista, RoutineSeeds.Borde, RoutineSeeds.PiramideContiguous, RoutineSeeds.SeriesTimesTen, RoutineSeeds.ListaPlus(26) })
            {
                foreach (Routine routine in seed.Routines)
                {
                    Check(routine.Name, seed.Name + ": " + routine.Name, missing);
                    Check(routine.Description ?? "", seed.Name + ": descripción de " + routine.Name, missing);
                }
            }

            Assert.That(missing, Is.Empty, "caracteres que el arnés no precarga (sumarlos a AtlasCharacters):\n" + string.Join("\n", missing));
        }

        private static void Check(string text, string where, SortedSet<string> missing)
        {
            for (var i = 0; i < text.Length; i++)
            {
                int codepoint = text[i];
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length)
                {
                    codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }

                if (codepoint == '\n' || codepoint == '\r' || codepoint == '\t' || (codepoint >= 0xE000 && codepoint <= 0xF8FF) || codepoint == 0x1F4C8)
                {
                    continue;
                }

                if (!AtlasCharacters.CoversText(codepoint))
                {
                    missing.Add("U+" + codepoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture) + " (" + where + ")");
                }
            }
        }

        // ── Windows PowerShell 5.1, como ItThreeToolingTests ──

        private static string WindowsPowerShell()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("los scripts se prueban con Windows PowerShell 5.1: solo en Windows (dotnet y Unity)");
            }

            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            Assert.That(File.Exists(powershell), Is.True, "falta Windows PowerShell 5.1");
            return powershell;
        }

        private static (int Code, string Output) RunPowerShell(string powershell, string arguments)
        {
            var start = new ProcessStartInfo(powershell, "-NoProfile -NonInteractive " + arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = RepoPaths.Root,
            };
            start.EnvironmentVariables.Remove("PSModulePath");
            using (var process = Process.Start(start))
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

        private static void DeleteFolder(string folder)
        {
            for (var i = 0; i < 20 && Directory.Exists(folder); i++)
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (IOException)
                {
                    Thread.Sleep(250);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(250);
                }
            }
        }
    }
}
