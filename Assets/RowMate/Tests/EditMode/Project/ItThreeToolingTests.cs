using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Infrastructure.Database;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // ITT · Revisión estática de las herramientas de la It. 3 (03-rutinas/02-tech-design §5.1, §5.5, §6.1, §6.2; archivo de WP-B
    // que corre en dotnet): ninguna corrida abre las bases del usuario (ROWMATE_DATA_DIR temporal y borrado en finally), CAP-27
    // solo a pedido y con sus resguardos, solo AppRoot elige el archivo de la base, las agujas nuevas del inspector, las
    // fuentes de texto con las características congeladas (DN3-24) y ningún script ni manifest con rutas de la PC (QA-G3-20).
    [TestFixture]
    public sealed class ItThreeToolingTests
    {
        private const string UnityTests = "tools/unity/run-unity-tests.ps1";
        private const string Capture = "tools/unity/capture-snapshots.ps1";
        private const string Cap27 = "tools/unity/capture-cap27.ps1";
        private const string Window = "tools/unity/capture-window.ps1";
        private const string WindowlessExe = "tools/unity/new-windowless-exe.ps1";
        private const string Build = "tools/build/build.ps1";

        // La única llamada de capture-cap27.ps1 a capture-window.ps1 (Invoke-WindowCapture), sin -Arguments: la corren la toma
        // real y el ensayo, y WindowCaptureStartsTheExeForReal la repite de verdad (QA-V3-11).
        private const string Cap27Call = "$lines = @(& powershell -NoProfile -File $capture -Exe $Target -Out $Destination -WaitSeconds $WaitSeconds -ClickRelative '0.625,0.98')";
        private const string Snapshots = "docs/migration/iterations/03-rutinas/snapshots";

        private static string Read(string relative)
        {
            var path = RepoPaths.Combine(relative);
            Assert.That(File.Exists(path), Is.True, "falta " + relative);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        [TestCase(UnityTests, "unity-", Category = "AC-HIS-20-04")]
        [TestCase(Capture, "capture-", Category = "AC-HIS-20-04")]
        public void ScriptsIsolateTheDataFolder(string relative, string prefix)
        {
            var script = Read(relative);
            Assert.That(script, Does.Contain("[System.IO.Path]::GetTempPath()) ('RowMate.Tests\\" + prefix), "carpeta temporal propia de la corrida");
            Assert.That(script, Does.Contain("$env:ROWMATE_DATA_DIR = $dataDir"), "Unity la hereda");
            var finallyAt = script.LastIndexOf("finally {", StringComparison.Ordinal);
            var setAt = script.IndexOf("$env:ROWMATE_DATA_DIR = $dataDir", StringComparison.Ordinal);
            Assert.That(finallyAt, Is.GreaterThan(setAt), "el finally viene después de ponerla");
            var tail = script.Substring(finallyAt);
            Assert.That(tail, Does.Contain("Remove-Item -Recurse -Force -Path $dataDir"), "se borra en finally, pase lo que pase");
            Assert.That(tail, Does.Contain("$env:ROWMATE_DATA_DIR = $previousDataDir"), "y la variable vuelve a como estaba");
            var runAt = script.IndexOf(relative == UnityTests ? "Invoke-Unity $testArgs" : "Invoke-Harness", setAt, StringComparison.Ordinal);
            Assert.That(runAt, Is.GreaterThan(setAt).And.LessThan(finallyAt), "Unity corre con la carpeta temporal puesta");
        }

        [Test, Category("AC-HIS-20-04")]
        public void TheAppHonorsTheVariableOnlyInDevelopment()
        {
            var location = Read("Assets/RowMate/Bootstrap/DataLocation.cs");
            Assert.That(location, Does.Contain("#if UNITY_EDITOR || DEVELOPMENT_BUILD\n        public const string Variable = \"ROWMATE_DATA_DIR\";"));
            Assert.That(Read("Assets/RowMate/Editor/Build/BuildOutputInspector.cs"), Does.Contain("\"ROWMATE_DATA_DIR\","), "el build normal no la nombra");
            var boot = Read("Assets/RowMate/Tests/PlayMode/BootSceneTests.cs");
            Assert.That(boot, Does.Contain("Environment.SetEnvironmentVariable(DataLocation.Variable, _dataDir);"), "BootSceneTests usa su carpeta");
            var sim = Read("Assets/RowMate/Tests/PlayModeSimulator/SimulatorBootSceneProbe.cs");
            Assert.That(sim, Does.Contain("Environment.SetEnvironmentVariable(DataLocation.Variable, folder);"), "y el arranque con el simulador también");
        }

        [Test, Category("AC-HIS-20-04")]
        public void Cap27RunsOnlyOnRequest()
        {
            var script = Read(Cap27);
            Assert.That(script, Does.Contain("[switch]$ReplaceUserDatabase"));
            Assert.That(script, Does.Contain("[switch]$Rehearsal"), "el ensayo (QA-V3-01) tampoco corre solo");
            var askAt = script.IndexOf("if (-not $ReplaceUserDatabase -and -not $Rehearsal) {", StringComparison.Ordinal);
            Assert.That(askAt, Is.GreaterThan(0), "sin el switch no toca nada");
            var block = script.Substring(askAt, script.IndexOf('}', askAt) - askAt);
            Assert.That(block, Does.Contain("exit 4"));
            var firstTouch = script.IndexOf("Copy-Item", StringComparison.Ordinal);
            Assert.That(askAt, Is.LessThan(firstTouch), "la pregunta va antes de cualquier copia");

            // Nadie más lo llama: ni los otros scripts ni los workflows.
            var callers = new List<string>();
            foreach (var folder in new[] { "tools", ".github" })
            {
                var root = RepoPaths.Combine(folder);
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    var relative = RepoPaths.Relative(file).Replace('\\', '/');
                    if (relative == Cap27 || relative.EndsWith(".md", StringComparison.Ordinal) || relative.Contains("/.artifacts/") || relative.Contains("/bin/")
                        || relative.Contains("/obj/"))
                    {
                        continue;
                    }

                    // Solo las líneas que ejecutan algo: un comentario que lo nombra no lo llama.
                    foreach (var line in File.ReadAllLines(file))
                    {
                        var code = line.TrimStart();
                        if (code.StartsWith("#", StringComparison.Ordinal) || !code.Contains("capture-cap27"))
                        {
                            continue;
                        }

                        callers.Add(relative);
                        break;
                    }
                }
            }

            Assert.That(callers, Is.Empty, "capture-cap27.ps1 solo a pedido");
            Assert.That(Read(Capture), Does.Contain("CAP-27 (ventana del build normal) NUNCA sale de aca"));
        }

        [Test, Category("AC-HIS-20-04")]
        public void Cap27ProtectsTheUserDatabase()
        {
            var script = Read(Cap27);
            // Tema: nunca se cambia; si no está en Oscuro, NO VERIFICABLE.
            Assert.That(script, Does.Contain("'AppsUseLightTheme'"));
            Assert.That(script, Does.Contain("exit 3"));
            Assert.That(script, Does.Not.Contain("Set-ItemProperty"), "nunca cambia el tema del usuario");
            // Las dos negativas. El Editor se reconoce por la ruta del ejecutable o por el lock del proyecto, no por el nombre del
            // proceso: el "unity.exe serve" de la Hub también se llama unity y -eq no distingue mayúsculas (QA-V3-01 b).
            Assert.That(script, Does.Contain("if ($process.ProcessName -eq 'RowMate') {"), "se niega con RowMate abierto");
            Assert.That(script, Does.Contain("elseif ($process.ProcessName -eq 'Unity') {"));
            Assert.That(script, Does.Contain("if (Test-UnityEditorPath $path) {"), "un proceso Unity cuenta solo si es un Editor");
            Assert.That(script, Does.Contain("return ($Path -match '[\\\\/]Editor[\\\\/]Unity\\.exe$')"), "…\\Editor\\Unity.exe");
            Assert.That(script, Does.Contain("if ($null -eq $Path -or $Path -eq '') { return $true }"), "sin ruta legible, en la duda cuenta");
            Assert.That(script, Does.Contain("$lock = Join-Path $repo 'Temp\\UnityLockfile'"), "y el proyecto abierto en un Editor");
            Assert.That(script, Does.Not.Contain("$_.ProcessName -eq 'Unity'"), "nunca por el nombre solo");
            Assert.That(script, Does.Contain("[System.IO.File]::Open($Path, 'Open', 'ReadWrite', 'None')"), "y con un archivo en uso");
            Assert.That(Regex.Matches(script, "exit 5").Count, Is.GreaterThanOrEqualTo(3), "las negativas salen con 5 sin tocar nada");
            var firstCheck = script.IndexOf("if (-not (Test-MayProceed 'antes de sembrar')) { exit 5 }", StringComparison.Ordinal);
            var seedRun = script.IndexOf("[System.Diagnostics.Process]::Start($info)", StringComparison.Ordinal);
            var secondCheck = script.IndexOf("if (-not (Test-MayProceed 'antes del resguardo')) {", StringComparison.Ordinal);
            var firstBackup = script.IndexOf("Copy-Item -LiteralPath $source", StringComparison.Ordinal);
            Assert.That(firstCheck, Is.GreaterThan(0).And.LessThan(seedRun), "antes de abrir Unity para sembrar");
            Assert.That(secondCheck, Is.GreaterThan(seedRun).And.LessThan(firstBackup), "y otra vez justo antes del resguardo");
            // Resguardo verificado ANTES de la copia que reemplaza.
            var backupCheck = script.IndexOf("if ((Get-Hash (Join-Path $backupFolder $name)) -ne $facts[$name].sha256) {", StringComparison.Ordinal);
            var replace = script.IndexOf("Copy-Item -LiteralPath $seedDb", StringComparison.Ordinal);
            Assert.That(backupCheck, Is.GreaterThan(0), "verifica cada copia de resguardo");
            Assert.That(replace, Is.GreaterThan(backupCheck), "recién después reemplaza la base");
            Assert.That(script, Does.Contain("[System.IO.Path]::GetTempPath()) ('RowMate.Cap27.' + $stamp)"), "resguardo en una carpeta calculada en el momento");
            Assert.That(script, Does.Contain("resguardo.json"));
            Assert.That(script, Does.Contain("$devFacts = Get-Facts (Join-Path $userFolder $devName)"), "anota la base del simulador, que no se toca");
            // Restauración en finally y verificación final.
            var finallyAt = script.LastIndexOf("finally {", StringComparison.Ordinal);
            Assert.That(finallyAt, Is.GreaterThan(replace));
            var restore = script.Substring(finallyAt);
            Assert.That(restore, Does.Contain("$when = [DateTime]::Parse($facts[$name].lastWriteTimeUtc, "), "con la fecha original");
            Assert.That(restore, Does.Contain("(Get-Item -LiteralPath $target).LastWriteTimeUtc = $when"));
            Assert.That(restore, Does.Contain("$now.sha256 -ne $was.sha256"), "verifica el SHA-256");
            Assert.That(restore, Does.Contain("$exitCode = 6"), "si no coincide, 6 y el resguardo queda");
            Assert.That(restore, Does.Contain("Remove-Item -Recurse -Force -Path $work"), "si coincide, borra el resguardo");
            // La base sintética la arma Unity con la semilla Cap27 (nunca datos del usuario), con su carpeta de datos temporal;
            // la corre de verdad Cap27SeedStepRunsForReal.
            Assert.That(script, Does.Contain("-executeMethod RowMate.Snapshots.SnapshotCli.SeedDatabase -snapshotSeedDb"));
            Assert.That(script, Does.Contain("-seed Cap27"));
            Assert.That(script, Does.Contain("$info.EnvironmentVariables['ROWMATE_DATA_DIR'] = (Join-Path $work 'datos-unity')"));
            Assert.That(script, Does.Contain("-not (Test-SqliteFile $seedDb)"), "y comprueba que salió una base SQLite");
            // Solo cierra el build que abrió (nunca un RowMate que ya estaba).
            Assert.That(restore, Does.Contain("if (-not $Rehearsal) { Stop-OwnBuild }"));
            Assert.That(script, Does.Contain("if ($null -ne $path -and $path -eq $exePath -and $null -ne $start -and $start -ge $started) {"));
            Assert.That(script, Does.Not.Contain("ForEach-Object { $_.Kill() }"), "nada de cerrar por la ruta sola");
            // La fecha vuelve exacta, en UTC, sin pasar por la hora local.
            Assert.That(restore, Does.Contain("[System.Globalization.DateTimeStyles]::RoundtripKind"));
        }

        // QA-V3-01 (a): el paso 1 de capture-cap27.ps1 de verdad, con los nombres que usa el script. La semilla se escribe en
        // <temp>/RowMate.Cap27.<algo>/<semilla>/<archivo> con el mismo código que SnapshotCli.SeedDatabase (RoutineSeeds.WriteFile,
        // que pasa por TempDatabase.Guard), y el .db solo, copiado con el nombre del paso 4 en una carpeta nueva, es una base de
        // la app con Escalera SPM y nada más. Con rower_app.db como nombre de la semilla la guardia no deja sembrar: era el
        // defecto (el script salía siempre con 7). En dotnet (sqlite-net-pcl) y en Unity EditMode (SQLite de gilzoide).
        [Test, Category("AC-HIS-20-04"), Category("QA-V3-01")]
        public void Cap27SeedStepRunsForReal()
        {
            var script = Read(Cap27);
            var folderName = Single(script, @"\$seedFolder = Join-Path \$work '([^']+)'");
            var fileName = Single(script, @"\$seedDb = Join-Path \$seedFolder '([^']+)'");
            var copiedAs = Single(script, @"Copy-Item -LiteralPath \$seedDb -Destination \(Join-Path \$userFolder '([^']+)'\) -Force");
            Assert.That(script, Does.Contain("$work = Join-Path ([System.IO.Path]::GetTempPath()) ('RowMate.Cap27.' + $stamp)"));
            Assert.That(script, Does.Contain("-snapshotSeedDb \"' + $seedDb + '\" -seed Cap27"), "Unity siembra ese archivo");
            Assert.That(copiedAs, Is.EqualTo(DatabaseFiles.NormalName), "el paso 4 la deja como la base del build normal");
            Assert.That(Read("Assets/RowMate/Snapshots/SnapshotCli.cs"), Does.Contain("RoutineSeeds.WriteFile(file, RoutineSeeds.Cap27);"),
                "SnapshotCli.SeedDatabase corre lo mismo que esta prueba");

            var work = Path.Combine(Path.GetTempPath(), "RowMate.Cap27.prueba-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            try
            {
                var seed = Path.Combine(work, folderName, fileName);
                RoutineSeeds.WriteFile(seed, RoutineSeeds.Cap27);
                Assert.That(File.Exists(seed), Is.True, "la semilla quedó escrita");
                Assert.That(File.Exists(seed + "-wal"), Is.False, "al cerrar, el WAL pasó al archivo: el .db solo alcanza");
                Assert.That(File.Exists(seed + "-shm"), Is.False);
                var header = new byte[16];
                using (var stream = File.OpenRead(seed))
                {
                    Assert.That(stream.Read(header, 0, header.Length), Is.EqualTo(16));
                }

                Assert.That(Encoding.ASCII.GetString(header, 0, 15), Is.EqualTo("SQLite format 3"), "lo que mira Test-SqliteFile del script");
                Assert.That(header[15], Is.EqualTo(0));

                var logger = new RecordingLogger();
                using (var db = new TempDatabase(copiedAs))
                {
                    File.Copy(seed, db.FilePath);
                    using (var repository = db.Repository(logger))
                    {
                        var all = repository.GetAllAsync().GetAwaiter().GetResult();
                        var expected = RoutineSeeds.EscaleraSpm();
                        Assert.That(all.Count, Is.EqualTo(1), "solo la rutina sintética, nunca datos del usuario");
                        Assert.That(all[0].Name, Is.EqualTo(expected.Name));
                        Assert.That(all[0].Description, Is.EqualTo(expected.Description));
                        Assert.That(all[0].CreatedAt, Is.EqualTo(expected.CreatedAt));
                        Assert.That(all[0].Steps.Count, Is.EqualTo(expected.Steps.Count));
                        for (var i = 0; i < expected.Steps.Count; i++)
                        {
                            Assert.That(all[0].Steps[i] with { Id = null, RoutineId = 0 }, Is.EqualTo(expected.Steps[i]), "paso " + i);
                        }
                    }
                }

                // El defecto: con el nombre de la base de la app, fuera de la carpeta de pruebas, la guardia no deja sembrar.
                var asAppName = Path.Combine(work, folderName, DatabaseFiles.NormalName);
                var error = Assert.Throws<InvalidOperationException>(() => RoutineSeeds.WriteFile(asAppName, RoutineSeeds.Cap27));
                Assert.That(error!.Message, Does.Contain("solo se usa dentro de la carpeta temporal de pruebas"));
                Assert.That(File.Exists(asAppName), Is.False, "y no queda nada");
            }
            finally
            {
                if (Directory.Exists(work))
                {
                    Directory.Delete(work, true);
                }
            }
        }

        // QA-V3-01: el ensayo (-Rehearsal) corre los pasos de verdad sobre una carpeta temporal de prueba y nunca nombra la del
        // usuario, no abre el build ni toca la pantalla; solo el proyecto abierto en un Editor lo frena.
        [Test, Category("AC-HIS-20-04"), Category("QA-V3-01")]
        public void Cap27RehearsalNeverTouchesTheUserFolder()
        {
            var script = Read(Cap27);
            const string Real = "$userFolder = Join-Path $env:USERPROFILE 'AppData\\LocalLow\\iguisoft\\RowMate'";
            const string Fake = "if ($Rehearsal) {\n    $userFolder = Join-Path $work 'ensayo-datos'";
            Assert.That(Regex.Matches(script, Regex.Escape(Real)).Count, Is.EqualTo(1), "la carpeta real se nombra una sola vez");
            var fakeAt = script.IndexOf(Fake, StringComparison.Ordinal);
            var realAt = script.IndexOf(Real, StringComparison.Ordinal);
            Assert.That(fakeAt, Is.GreaterThan(0), "en el ensayo, una carpeta temporal adentro de la de trabajo");
            Assert.That(script.Substring(fakeAt, realAt - fakeAt), Does.Contain("\n}\nelse {\n"), "y la real solo fuera del ensayo");

            // En el ensayo no se abre el build. La única llamada a capture-window.ps1 está en Invoke-WindowCapture: la toma real
            // la usa con el build normal y el ensayo, con el ejecutable de prueba sin ventana, y espera 1 con "el build no abrio
            // ninguna ventana" y ninguna captura (QA-V3-11: antes el ensayo no la corría y no vio que la llamada fallaba).
            Assert.That(Regex.Matches(script, Regex.Escape("& powershell -NoProfile -File $capture")).Count, Is.EqualTo(1), "una sola llamada");
            Assert.That(BootstrapperStaticTests.Body(script, "function Invoke-WindowCapture("), Does.Contain(Cap27Call));
            Assert.That(script, Does.Contain("-ClickRelative '0.625,0.98'"), "la 3.ª de las 4 pestañas (QA-V3-01 c)");
            const string StepFour = "    if ($Rehearsal) {\n        $placed = Join-Path $userFolder 'rower_app.db'";
            var rehearsal = BootstrapperStaticTests.Body(script, StepFour);
            Assert.That(rehearsal, Does.Not.Contain("$exePath"), "el ensayo nunca abre el build");
            Assert.That(rehearsal, Does.Contain("& powershell -NoProfile -File (Join-Path $PSScriptRoot 'new-windowless-exe.ps1') -Out $probe"));
            Assert.That(rehearsal, Does.Contain("$probe = Join-Path $work 'sin-ventana.exe'"), "en la carpeta de trabajo, que se borra");
            Assert.That(rehearsal, Does.Contain("$shot = Invoke-WindowCapture $probe $probeShot"), "la misma llamada que la toma real");
            Assert.That(rehearsal, Does.Contain("if ($shot.Code -eq 1 -and $noWindow -and -not (Test-Path -LiteralPath $probeShot)) {"));
            Assert.That(rehearsal, Does.Contain("'*el build no abrio ninguna ventana*'"));
            Assert.That(Read(Window), Does.Contain("Write-Host '[ventana] el build no abrio ninguna ventana'"), "el mensaje que espera el ensayo");
            Assert.That(Regex.Matches(script, Regex.Escape("Invoke-WindowCapture $exePath $outPath")).Count, Is.EqualTo(1), "la toma real, una vez");
            var real = script.IndexOf("    else {\n        $outPath = Get-FullPath $Out\n        $shot = Invoke-WindowCapture $exePath $outPath", StringComparison.Ordinal);
            Assert.That(real, Is.GreaterThan(script.IndexOf(StepFour, StringComparison.Ordinal)), "en la rama de la toma real, después del ensayo");

            // Lo que se niega en la toma real, en el ensayo se informa; el proyecto abierto en un Editor frena los dos.
            var proceed = script.Substring(script.IndexOf("function Test-MayProceed", StringComparison.Ordinal));
            proceed = proceed.Substring(0, proceed.IndexOf("\n}\n", StringComparison.Ordinal));
            Assert.That(proceed.IndexOf("if (Test-ProjectLocked) {", StringComparison.Ordinal),
                Is.LessThan(proceed.IndexOf("if ($Rehearsal) {", StringComparison.Ordinal)), "el lock frena también el ensayo");
            Assert.That(script, Does.Contain("[System.IO.File]::WriteAllText(($placed + '-shm'), "), "el ensayo deja un -shm que no existía");
        }

        // capture-window.ps1 (la foto de CAP-27 y la de CAP-22 de la It. 2): solo hace clic y captura si la ventana del build es la
        // de adelante; si otra la tapa, sale con 3 sin clic ni captura (en la foto saldría la pantalla del usuario). Los dos
        // scripts, en ASCII y sin operadores de PowerShell 7 (como ITW.ScriptsAreAsciiForWindowsPowerShell51).
        [Test, Category("AC-HIS-20-04"), Category("QA-V3-01")]
        public void WindowCaptureOnlyClicksAndCapturesItsOwnWindow()
        {
            var window = Read(Window);
            var check = window.IndexOf("if (-not (Test-OwnWindowAt $point.X $point.Y)) {", StringComparison.Ordinal);
            var click = window.IndexOf("[RowMateWindow]::mouse_event(0x0002", StringComparison.Ordinal);
            var grid = window.IndexOf("foreach ($fx in @(0.1, 0.5, 0.9)) {", StringComparison.Ordinal);
            var shot = window.IndexOf("$graphics.CopyFromScreen(", StringComparison.Ordinal);
            Assert.That(check, Is.GreaterThan(0).And.LessThan(click), "mira antes de hacer clic");
            Assert.That(grid, Is.GreaterThan(click).And.LessThan(shot), "y antes de capturar");
            Assert.That(window, Does.Contain("return ([RowMateWindow]::GetAncestor($hit, 2) -eq $handle)"));
            Assert.That(Regex.Matches(window, "exit 3").Count, Is.EqualTo(2));
            Assert.That(window, Does.Contain("if ($hadCursor) { [RowMateWindow]::SetCursorPos($before.X, $before.Y) | Out-Null }"), "el puntero vuelve");
            Assert.That(window, Does.Contain("0.625,0.98"));
            Assert.That(window, Does.Not.Contain("0.5,0.96"));

            foreach (var relative in new[] { Cap27, Window, WindowlessExe })
            {
                var bytes = File.ReadAllBytes(RepoPaths.Combine(relative));
                for (var i = 0; i < bytes.Length; i++)
                {
                    Assert.That(bytes[i], Is.LessThan(128), relative + ": byte no ASCII en la posición " + i);
                }

                var text = Read(relative);
                Assert.That(text, Does.Not.Contain("?."), relative + ": sin ?. (no existe en PowerShell 5.1)");
                Assert.That(text, Does.Not.Contain("??"), relative + ": sin ??");
                Assert.That(Regex.IsMatch(text, @"\S\s*&&\s*\S"), Is.False, relative + ": sin &&");
            }
        }

        // QA-V3-11: el arranque de capture-window.ps1 DE VERDAD, con Windows PowerShell 5.1 (el que usan los dos scripts), contra
        // un ejecutable de prueba sin ventana (new-windowless-exe.ps1: subsistema gráfico y Main vacío, en una carpeta temporal
        // con ñá que se borra). Primero con los parámetros de la llamada de capture-cap27.ps1 (sin -Arguments) y después con
        // -Arguments, como CAP-22 de la It. 2: las dos tienen que llegar a "el build no abrio ninguna ventana" y salir con 1
        // enseguida. Eran dos defectos: sin -Arguments, Start-Process -ArgumentList '' no pasaba la validación de 5.1 (la toma
        // real salía con 8 después de reemplazar la base); y con eso arreglado, MainWindowHandle de un proceso que terminó (en
        // 5.1 da $null) pasaba por una ventana y el script se caía en SetForegroundWindow. Sin ventana no hay clic ni captura:
        // no toca el mouse ni la pantalla. Lo estático corre en todos lados; el arranque, solo en Windows.
        [Test, Category("AC-HIS-20-04"), Category("QA-V3-11")]
        public void WindowCaptureStartsTheExeForReal()
        {
            var window = Read(Window);
            Assert.That(window, Does.Contain("$start = @{ FilePath = $exePath; PassThru = $true }"));
            Assert.That(window, Does.Contain("if ($Arguments -ne '') { $start['ArgumentList'] = $Arguments }"), "-ArgumentList solo con argumentos");
            Assert.That(window, Does.Not.Contain("-ArgumentList $Arguments"), "nunca -ArgumentList ''");
            Assert.That(window, Does.Contain("while ($handle -eq [IntPtr]::Zero -and -not $process.HasExited -and (Get-Date) -lt $deadline) {"),
                "deja de esperar si el proceso terminó");
            Assert.That(BootstrapperStaticTests.Body(window, "function Get-MainWindow"), Does.Contain("if ($null -eq $window -or $process.HasExited) { return [IntPtr]::Zero }"),
                "un proceso que terminó no tiene ventana (en 5.1, MainWindowHandle da $null)");
            var trap = BootstrapperStaticTests.Body(window, "\ntrap {\n");
            Assert.That(trap, Does.Contain("exit 4"), "un error no previsto sale con 4, no con el 1 de \"no aparecio la ventana\"");
            var cap27 = Read(Cap27);
            Assert.That(BootstrapperStaticTests.Body(cap27, "function Invoke-WindowCapture("), Does.Contain(Cap27Call), "la llamada que se repite abajo");
            Assert.That(Cap27Call, Does.Not.Contain("-Arguments"), "capture-cap27.ps1 llama sin -Arguments: el caso de QA-V3-11");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("el arranque de capture-window.ps1 se prueba con Windows PowerShell 5.1: solo en Windows (dotnet y Unity)");
            }

            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            Assert.That(File.Exists(powershell), Is.True, "falta Windows PowerShell 5.1 (lo usan capture-cap27.ps1 y capture-window.ps1)");
            var work = Path.Combine(TempDatabase.Root, "ventana-" + Guid.NewGuid().ToString("N") + TempDatabase.AccentSuffix);
            Directory.CreateDirectory(work);
            try
            {
                var probe = Path.Combine(work, "sin-ventana.exe");
                var made = RunPowerShell(powershell, "-File " + Quote(RepoPaths.Combine(WindowlessExe)) + " -Out " + Quote(probe));
                Assert.That(made.Code, Is.EqualTo(0), "new-windowless-exe.ps1: " + made.Output);
                Assert.That(PeSubsystem(probe), Is.EqualTo(2), "subsistema gráfico: no abre una consola");

                var shot = Path.Combine(work, "sin-ventana.png");
                var common = "-File " + Quote(RepoPaths.Combine(Window)) + " -Exe " + Quote(probe) + " -Out " + Quote(shot) + " -WaitSeconds 8";
                var variants = new[]
                {
                    new[] { "como capture-cap27.ps1 (sin -Arguments)", " -ClickRelative " + Quote("0.625,0.98") },
                    new[] { "con -Arguments (CAP-22 de la It. 2)", " -Arguments " + Quote("-screen-width 1280 -screen-height 720") },
                };
                foreach (var variant in variants)
                {
                    var watch = Stopwatch.StartNew();
                    var run = RunPowerShell(powershell, common + variant[1]);
                    watch.Stop();
                    var what = variant[0] + ": " + run.Output;
                    Assert.That(run.Output, Does.Not.Contain("Cannot validate argument"), what);
                    Assert.That(run.Output, Does.Not.Contain("error inesperado"), what);
                    Assert.That(run.Code, Is.EqualTo(1), what);
                    Assert.That(run.Output, Does.Contain("[ventana] el build no abrio ninguna ventana"), what);
                    Assert.That(run.Output, Does.Contain("[ventana] el proceso termino sin ventana (codigo 0)"), what);
                    Assert.That(watch.Elapsed.TotalSeconds, Is.LessThan(20), variant[0] + ": sin proceso no espera los WaitSeconds + 20 s");
                    Assert.That(File.Exists(shot), Is.False, variant[0] + ": sin ventana no hay captura");
                }
            }
            finally
            {
                DeleteFolder(work);
            }
        }

        // Windows PowerShell 5.1 sin perfil, sin ventana y con la salida leída (estándar y de error juntas). Sin PSModulePath:
        // si la corrida viene de pwsh 7 (el CI), 5.1 no tiene que buscar sus módulos en las carpetas de 7.
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

        // Subsistema del encabezado PE (e_lfanew + 0x5C): 2 gráfico, 3 consola.
        private static int PeSubsystem(string exe)
        {
            var bytes = File.ReadAllBytes(exe);
            return BitConverter.ToUInt16(bytes, BitConverter.ToInt32(bytes, 0x3C) + 0x5C);
        }

        // El antivirus puede tener un momento el .exe recién creado: reintenta 5 s.
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

        private static string Single(string text, string pattern)
        {
            var matches = Regex.Matches(text, pattern);
            Assert.That(matches.Count, Is.EqualTo(1), "una sola vez: " + pattern);
            return matches[0].Groups[1].Value;
        }

        [Test, Category("AC-HIS-20-01")]
        public void OnlyAppRootChoosesTheDatabaseFile()
        {
            var users = new List<string>();
            foreach (var file in Directory.GetFiles(RepoPaths.Combine("Assets/RowMate"), "*.cs", SearchOption.AllDirectories))
            {
                var relative = RepoPaths.Relative(file).Replace('\\', '/');
                if (relative.StartsWith("Assets/RowMate/Tests/", StringComparison.Ordinal) || relative.EndsWith("/DatabaseFiles.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                if (SourceScan.StripCommentsAndLiterals(File.ReadAllText(file)).Contains("DatabaseFiles.NameFor("))
                {
                    users.Add(relative);
                }
            }

            Assert.That(users, Is.EquivalentTo(new[] { "Assets/RowMate/Bootstrap/AppRoot.cs" }), "solo AppRoot elige rower_app.db o rower_app_dev.db");
            Assert.That(Read("Assets/RowMate/Bootstrap/AppRoot.cs"), Does.Contain("DatabaseFiles.NameFor(BuildFlags.IsSimulator)"), "por la variante del build");
        }

        [Test, Category("AC-BLE-13-02")]
        public void InspectorKnowsTheNewNeedles()
        {
            var inspector = Read("Assets/RowMate/Editor/Build/BuildOutputInspector.cs");
            foreach (var needle in new[] { "\"-rowmate-db-smoke\",", "\"-rowmate-db-roundtrip\",", "\"ROWMATE_DATA_DIR\"," })
            {
                Assert.That(inspector, Does.Contain(needle), "el build normal no tiene " + needle);
            }

            var appRoot = Read("Assets/RowMate/Bootstrap/AppRoot.cs");
            var guardAt = appRoot.IndexOf("#if UNITY_EDITOR || DEVELOPMENT_BUILD\n        public const string PipelineArgument", StringComparison.Ordinal);
            Assert.That(guardAt, Is.GreaterThan(0));
            var endAt = appRoot.IndexOf("#endif", guardAt, StringComparison.Ordinal);
            var block = appRoot.Substring(guardAt, endAt - guardAt);
            Assert.That(block, Does.Contain("DbSmokeArgument = \"-rowmate-db-smoke\""), "solo en desarrollo");
            Assert.That(block, Does.Contain("DbRoundTripArgument = \"-rowmate-db-roundtrip\""));
            var build = Read(Build);
            Assert.That(build, Does.Contain("[switch]$DbSmoke"));
            Assert.That(build, Does.Contain("'-rowmate-db-smoke', $smokeDir"), "el humo corre sobre una carpeta temporal");
            Assert.That(build, Does.Contain("Remove-Item -Recurse -Force -Path $smokeDir"));
        }

        [Test, Category("DN3-24")]
        public void TextFacesFreezeTheirFontFeatures()
        {
            var faces = FontAssetPaths.TextFaces;
            Assert.That(faces.Count, Is.EqualTo(7));
            foreach (var face in faces)
            {
                var relative = FontAssetPaths.For(face.Family, face.Weight);
                Assert.That(Read(relative), Does.Contain("m_GetFontFeatures: 0"), relative + ": sin reaplicar el kerning en tiempo de ejecución (DN3-24)");
            }
        }

        [Test, Category("QA-G3-20")]
        public void NoMachinePathsInScriptsOrManifests()
        {
            var machinePath = new Regex(@"[A-Za-z]:[\\/]+Users[\\/]|[\\/]home[\\/][a-z]|AppData[\\/]+Local[\\/]+Temp[\\/]+claude", RegexOptions.IgnoreCase);
            var files = new List<string>();
            foreach (var pattern in new[] { "*.ps1", "*.sh" })
            {
                files.AddRange(Directory.GetFiles(RepoPaths.Combine("tools"), pattern, SearchOption.AllDirectories));
            }

            var snapshots = RepoPaths.Combine(Snapshots);
            foreach (var name in new[] { "manifest.json", "INDEX.md" })
            {
                var path = Path.Combine(snapshots, name);
                if (File.Exists(path))
                {
                    files.Add(path);
                }
            }

            foreach (var file in files)
            {
                var relative = RepoPaths.Relative(file).Replace('\\', '/');
                if (relative.Contains("/.artifacts/"))
                {
                    continue;
                }

                var match = machinePath.Match(File.ReadAllText(file));
                Assert.That(match.Success, Is.False, relative + ": ruta de la PC (" + (match.Success ? match.Value : "") + ")");
            }
        }
    }
}
