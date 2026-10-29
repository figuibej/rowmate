using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RowMate.Tests.EditMode.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // SLT: la herramienta de la demo (tools/sessions, proyecto tools/dotnet/RowMate.SessionList; spec §12.9; 02-tech-design
    // §3.6 de la It. 4a), corrida como proceso sobre bases de prueba de TempDatabase (nunca las del usuario: AC-HIS-20-04).
    // No toca la base de origen (mismo SHA-256 y misma fecha, también con un -wal presente) y no escribe rutas: ni en la
    // salida ni en los tres errores de texto fijo (QA-G4aT-18). En Unity la herramienta no está compilada: Ignore.
    [TestFixture]
    public sealed class SessionListToolTests
    {
        private RecordingLogger _logger = null!;
        private TempDatabase _temp = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _temp = new TempDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            _temp.Dispose();
        }

        [Test, Category("AC-HIS-20-04"), Category("AC-WKT-06-10")]
        public void SourceDatabaseIsUntouched()
        {
            // Una cerrada (Corta) y una cortada a los 12 s, con la conexión de la app todavía abierta: la base tiene su -wal
            // (sin checkpoint). La herramienta lista las dos, la más nueva arriba, y la base y su -wal quedan iguales.
            var source = _temp.PathOf("rower_app_dev.db");
            using (var store = _temp.Store(_logger, null, "rower_app_dev.db"))
            {
                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    h.Routine(WorkoutHarness.Saved(RoutineSeeds.Corta(), 3));
                    h.At(31);
                }

                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    h.Free();
                    h.At(12);
                }

                Assert.That(File.Exists(source + "-wal"), Is.True, "con -wal");
                var before = State(source);
                var toolFolders = ToolFolders();
                var run = RunTool("--db", source);
                Assert.That(run.Code, Is.EqualTo(0), run.Error);
                Assert.That(State(source), Is.EqualTo(before), "la base y su -wal, iguales (SHA-256 y fecha)");
                Assert.That(ToolFolders(), Is.EquivalentTo(toolFolders), "la copia se borró");
                var lines = Lines(run.Output);
                Assert.That(lines[0], Is.EqualTo("[sessions] rower_app_dev.db: 2 sesiones"));
                Assert.That(lines.Count, Is.EqualTo(3));
                Assert.That(lines[1], Does.StartWith("  2 · ").And.EndWith(" · — · libre · 2 puntos"), "la cortada, arriba (id desempata)");
                Assert.That(lines[2], Does.StartWith("  1 · ").And.EndWith(" · Corta · 7 puntos"));
                AssertNoPaths(run);
            }
        }

        [Test, Category("AC-HIS-20-04")]
        public void MissingFileErrorHasNoPath()
        {
            var missing = _temp.PathOf("rower_app_dev.db");
            var run = RunTool("--db", missing);
            Assert.That(run.Code, Is.EqualTo(2));
            Assert.That(Lines(run.Error), Is.EqualTo(new[] { "[sessions] no se encontró rower_app_dev.db" }));
            Assert.That(run.Output, Is.Empty);
            AssertNoPaths(run);
        }

        [Test, Category("AC-HIS-20-04")]
        public void LockedFileErrorHasNoPath()
        {
            // La prueba lo tiene abierto con FileShare.None (la app escribiendo, un antivirus): la herramienta reintenta la
            // copia y termina con el texto fijo, sin la carpeta. Con --verbose, solo el tipo de la excepción.
            var source = _temp.PathOf("rower_app_dev.db");
            using (var store = _temp.Store(_logger, null, "rower_app_dev.db"))
            {
                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    h.Free();
                    h.At(6);
                }
            }

            using (new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var run = RunTool("--db", source);
                Assert.That(run.Code, Is.EqualTo(2));
                Assert.That(Lines(run.Error), Is.EqualTo(new[] { "[sessions] rower_app_dev.db está en uso o no se puede leer" }));
                AssertNoPaths(run);
                var verbose = RunTool("--db", source, "--verbose");
                Assert.That(verbose.Code, Is.EqualTo(2));
                Assert.That(Lines(verbose.Error)[0], Is.EqualTo("[sessions] detalle: IOException"));
                AssertNoPaths(verbose);
            }
        }

        [Test, Category("AC-HIS-20-04")]
        public void NotARowMateDatabaseHasNoPath()
        {
            // F-ROTA (bytes que no son SQLite) y un archivo vacío: el tercer texto fijo; el archivo queda igual.
            var source = _temp.PathOf("rower_app_dev.db");
            RoutineSeeds.WriteRota(source);
            var before = State(source);
            var run = RunTool("--db", source);
            Assert.That(run.Code, Is.EqualTo(2));
            Assert.That(Lines(run.Error), Is.EqualTo(new[] { "[sessions] rower_app_dev.db no es una base de RowMate" }));
            Assert.That(State(source), Is.EqualTo(before));
            AssertNoPaths(run);
            File.WriteAllBytes(source, new byte[0]);
            Assert.That(Lines(RunTool("--db", source).Error), Is.EqualTo(new[] { "[sessions] rower_app_dev.db no es una base de RowMate" }));
        }

        [Test, Category("AC-HIS-20-04")]
        public void BadArgumentsShowTheUsage()
        {
            foreach (var args in new[] { new[] { "--variant", "otra" }, new[] { "--db" }, new[] { "--nada" }, new[] { "--variant", "normal", "--db", "x.db" } })
            {
                var run = RunTool(args);
                Assert.That(run.Code, Is.EqualTo(1), string.Join(" ", args));
                Assert.That(run.Error, Does.StartWith("[sessions] uso: RowMate.SessionList"));
            }
        }

        // ── La herramienta como proceso ──

        private static ToolRun RunTool(params string[] args)
        {
            var dll = ToolDll();
            if (!File.Exists(dll))
            {
#if UNITY_EDITOR
                Assert.Ignore("Solo en dotnet: la herramienta de la demo se compila en tools/dotnet (RowMate.SessionList).");
#else
                Assert.Fail("No está compilada la herramienta (" + Path.GetFileName(dll) + "): correr tools/dotnet/run-tests.ps1, que compila la solución entera.");
#endif
            }

            var appHost = Path.ChangeExtension(dll, Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : null);
            var arguments = new List<string>();
            string program;
            if (File.Exists(appHost))
            {
                program = appHost;
            }
            else
            {
                var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
                program = string.IsNullOrEmpty(host) ? "dotnet" : host!;
                arguments.Add(Quote(dll));
            }

            foreach (var arg in args)
            {
                arguments.Add(Quote(arg));
            }

            var start = new ProcessStartInfo(program, string.Join(" ", arguments));
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;
            start.CreateNoWindow = true;
            using (var process = Process.Start(start))
            {
                Assert.That(process, Is.Not.Null, "no se pudo iniciar la herramienta");
                var stdout = process!.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(60000))
                {
                    process.Kill();
                    Assert.Fail("la herramienta no terminó en 60 s");
                }

                process.WaitForExit();
                return new ToolRun(process.ExitCode, stdout.Result, stderr.Result);
            }
        }

        // Entre comillas si hace falta (las rutas de prueba llevan espacios posibles y ñá); las pruebas no pasan comillas ni
        // barras finales.
        private static string Quote(string arg)
        {
            return arg.Length > 0 && arg.IndexOf(' ') < 0 && arg.IndexOf('"') < 0 ? arg : "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        // <artifacts>/bin/RowMate.Tests.EditMode/<config>/ → <artifacts>/bin/RowMate.SessionList/<config>/RowMate.SessionList.dll
        private static string ToolDll()
        {
            var here = Path.GetDirectoryName(typeof(SessionListToolTests).Assembly.Location) ?? "";
            var bin = Path.GetFullPath(Path.Combine(here, "..", ".."));
            return Path.Combine(bin, "RowMate.SessionList", Path.GetFileName(here), "RowMate.SessionList.dll");
        }

        // Ni la carpeta temporal del sistema, ni la de la prueba, ni la de datos de la app, ni un separador antes del nombre.
        private void AssertNoPaths(ToolRun run)
        {
            var all = run.Output + "\n" + run.Error;
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (var folder in new[] { Path.GetTempPath(), _temp.Folder, profile, TempDatabase.Root })
            {
                foreach (var variant in new[] { folder.TrimEnd('\\', '/'), folder.TrimEnd('\\', '/').Replace('\\', '/') })
                {
                    if (variant.Length > 3)
                    {
                        Assert.That(all.IndexOf(variant, StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "sin rutas en la salida");
                    }
                }
            }

            Assert.That(all, Does.Not.Contain("\\rower_app"));
            Assert.That(all, Does.Not.Contain("/rower_app"));
            Assert.That(all, Does.Not.Contain(":\\"));
            Assert.That(all, Does.Not.Contain(":/"));
        }

        private static List<string> Lines(string text)
        {
            var lines = new List<string>();
            foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        // SHA-256 y fecha de la base y de su -wal (si está).
        private static string State(string source)
        {
            var parts = new List<string>();
            foreach (var path in new[] { source, source + "-wal" })
            {
                if (!File.Exists(path))
                {
                    parts.Add(Path.GetFileName(path) + ": no está");
                    continue;
                }

                using (var sha = SHA256.Create())
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    parts.Add(Path.GetFileName(path) + ": " + BitConverter.ToString(sha.ComputeHash(stream)) + " "
                        + File.GetLastWriteTimeUtc(path).Ticks);
                }
            }

            return string.Join(" | ", parts);
        }

        // Las carpetas de trabajo de la herramienta en la temporal del sistema (cada corrida borra la suya).
        private static List<string> ToolFolders()
        {
            var root = Path.Combine(Path.GetTempPath(), "RowMate.SessionList");
            return Directory.Exists(root) ? new List<string>(Directory.GetDirectories(root)) : new List<string>();
        }

        private sealed class ToolRun
        {
            public ToolRun(int code, string output, string error)
            {
                Code = code;
                Output = output;
                Error = error;
            }

            public int Code { get; }

            public string Output { get; }

            public string Error { get; }
        }
    }
}
