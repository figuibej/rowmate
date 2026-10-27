using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-APP-19-03 (R-02, R-08): higiene del repositorio como tests de dotnet (D-04), así corre igual en la PC y en el CI.
    // Usa git por Process. Sin git o sin repositorio: Inconclusive en la PC y FALLA con ROWMATE_CI=1.
    // QA-V-02 de la It. 1: además de lo versionado (git ls-files), mira lo que está en disco sin versionar y sin ignorar
    // (git ls-files --others --exclude-standard = lo que agregaría `git add -A`). Así un archivo de credenciales nuevo
    // se detecta en la PC ANTES del commit y del push, no recién en el CI.
    // Ningún test lee ni abre el archivo de credenciales del legacy: solo se miran nombres de archivo.
    [TestFixture]
    public sealed class RepoHygieneTests
    {
        private static readonly string[] SecretPatterns =
        {
            "StravaSecrets", "strava_secrets.", "strava.local.json",
        };

        private static readonly string[] SecretExtensions = { ".keystore", ".jks", ".p12", ".mobileprovision", ".ulf" };

        [Test, Category("AC-APP-19-03")]
        public void NoSecretsAreTracked()
        {
            var offenders = SecretOffenders(TrackedFiles(RepoPaths.Root));
            Assert.That(offenders, Is.Empty, "credenciales, llaves de firma o licencias versionadas:\n" + string.Join("\n", offenders));
        }

        // QA-V-02: lo que está en disco y entraría con `git add -A` (ni versionado ni ignorado).
        [Test, Category("AC-APP-19-03"), Category("QA-V-02")]
        public void NoSecretsWouldBeAdded()
        {
            var offenders = SecretOffenders(AddableFiles(RepoPaths.Root));
            Assert.That(offenders, Is.Empty, "credenciales, llaves de firma o licencias en disco SIN ignorar (git add -A las subiría); "
                + "agregalas a .gitignore o borralas:\n" + string.Join("\n", offenders));
        }

        [TestCase("Assets/RowMate/Bootstrap/Generated/StravaSecrets.g.cs", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Foo/StravaSecrets.cs", true, Category = "AC-APP-19-03")]
        [TestCase("tools/strava/strava.local.json", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/ThirdParty/Mixamo/YBot/YBot.fbx", true, Category = "AC-APP-19-03")]
        [TestCase("a.keystore", true, Category = "AC-APP-19-03")]
        [TestCase("b.ulf", true, Category = "AC-APP-19-03")]
        [TestCase("c.jks", true, Category = "AC-APP-19-03")]
        [TestCase("d.p12", true, Category = "AC-APP-19-03")]
        [TestCase("e.mobileprovision", true, Category = "AC-APP-19-03")]
        [TestCase("tools/dotnet/.artifacts/bin/x.dll", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/ThirdParty/Mixamo/YBot/YBot.fbx.meta", false, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Core/Design/Tokens.cs", false, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/bin/fmt_vectors.dart", false, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/lib/golden.dart", false, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/.dart_tool/oracle_package_config.json", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Tests/Golden/oracle/fmt.json", false, Category = "AC-APP-19-03")]
        [TestCase("tools/dotnet/RowMate.Core/RowMate.Core.csproj", false, Category = "AC-APP-19-03")]
        [TestCase("tools/dotnet/RowMate.sln", false, Category = "AC-APP-19-03")]
        // QA-V-02: StravaSecrets* con cualquier extensión (y su .meta), en cualquier carpeta.
        [TestCase("Assets/RowMate/Config/StravaSecrets.json", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Config/StravaSecrets.json.meta", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/StravaSecrets.asset", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/StravaSecrets.asset.meta", true, Category = "AC-APP-19-03")]
        [TestCase("tools/strava/StravaSecrets.txt", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/stravasecrets.json", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Foo/strava_secrets.dart", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/strava.local.json", true, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/strava.local.json.meta", true, Category = "AC-APP-19-03")]
        // Los nombres vecinos que sí se versionan (ARCHITECTURE §11.1): el patrón no es demasiado amplio.
        [TestCase("Assets/RowMate/Bootstrap/StravaCredentials.cs", false, Category = "AC-APP-19-03")]
        [TestCase("Assets/RowMate/Core/Strava/StravaConfig.cs", false, Category = "AC-APP-19-03")]
        [TestCase("tools/strava/write-secrets.ps1", false, Category = "AC-APP-19-03")]
        // Resultados de los tests dentro de Unity (QA-V-05).
        [TestCase("tools/unity/.artifacts/editmode-results.xml", true, Category = "AC-APP-19-03")]
        [TestCase("tools/unity/run-unity-tests.ps1", false, Category = "AC-APP-19-03")]
        public void GitignoreProtectsSecretsAndThirdParty(string path, bool ignored)
        {
            var exit = RunGit(RepoPaths.Root, out var output, "check-ignore", "-q", "--no-index", path);
            if (exit != 0 && exit != 1)
            {
                Unavailable("git check-ignore salió con " + exit + ": " + output);
            }

            Assert.That(exit == 0, Is.EqualTo(ignored), path + (ignored ? " tiene que estar ignorado" : " NO tiene que estar ignorado"));
        }

        [Test, Category("AC-APP-19-03")]
        public void ThirdPartyOnlyMetaTracked()
        {
            // Versionados y, desde QA-V-02, también los que `git add -A` agregaría.
            var candidates = TrackedFiles(RepoPaths.Root);
            candidates.AddRange(AddableFiles(RepoPaths.Root));
            var offenders = new List<string>();
            foreach (var path in candidates)
            {
                if (path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal) && !path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    offenders.Add(path);
                }

                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add(path);
                }
            }

            Assert.That(offenders, Is.Empty, "archivos de terceros versionados o por versionar (solo sus .meta):\n" + string.Join("\n", offenders));
        }

        // QA-V-02: el control funciona de verdad sobre archivos en disco. Repositorio git temporal (fuera del proyecto, así
        // Unity no importa nada): sin .gitignore, un StravaSecrets.json nuevo sin commit aparece como "se agregaría";
        // forzado con `git add -f`, como versionado; y con el .gitignore del proyecto, queda ignorado.
        [Test, Category("AC-APP-19-03"), Category("QA-V-02")]
        public void DetectsSecretFilesOnDiskBeforeTheyAreCommitted()
        {
            var root = Path.Combine(Path.GetTempPath(), "rowmate-hygiene-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var init = RunGit(root, out var initOutput, "init", "-q");
                if (init != 0)
                {
                    Unavailable("git init salió con " + init + ": " + initOutput);
                }

                var secrets = new[]
                {
                    "Assets/RowMate/Config/StravaSecrets.json", "Assets/RowMate/StravaSecrets.asset", "tools/strava/strava.local.json",
                    "x/strava_secrets.dart", "keys/release.keystore", "Unity_v6.ulf",
                };
                foreach (var relative in secrets)
                {
                    Touch(root, relative);
                }

                Touch(root, "Assets/RowMate/Bootstrap/StravaCredentials.cs");
                Touch(root, "Assets/RowMate/Core/Design/Tokens.cs");

                // 1. Sin .gitignore: todo lo de disco "se agregaría" y el control nombra exactamente los secretos.
                Assert.That(SecretOffenders(AddableFiles(root)), Is.EquivalentTo(secrets));
                Assert.That(SecretOffenders(TrackedFiles(root)), Is.Empty);

                // 2. Forzado al índice (git add -f): ahora es "versionado".
                Assert.That(RunGit(root, out var addOutput, "add", "-f", "--", "Assets/RowMate/Config/StravaSecrets.json"), Is.EqualTo(0), addOutput);
                Assert.That(SecretOffenders(TrackedFiles(root)), Is.EqualTo(new[] { "Assets/RowMate/Config/StravaSecrets.json" }));
                Assert.That(RunGit(root, out var resetOutput, "rm", "-q", "--cached", "--", "Assets/RowMate/Config/StravaSecrets.json"), Is.EqualTo(0), resetOutput);

                // 3. Con el .gitignore del proyecto: los secretos quedan ignorados y ya no "se agregarían".
                File.Copy(RepoPaths.Combine(".gitignore"), Path.Combine(root, ".gitignore"));
                Assert.That(SecretOffenders(AddableFiles(root)), Is.Empty);
                Assert.That(AddableFiles(root), Does.Contain("Assets/RowMate/Bootstrap/StravaCredentials.cs"));
            }
            finally
            {
                DeleteTree(root);
            }
        }

        [TestCase("Assets/RowMate/Config/StravaSecrets.json", true, Category = "QA-V-02")]
        [TestCase("Assets/RowMate/STRAVASECRETS.asset", true, Category = "QA-V-02")]
        [TestCase("lib/core/strava/strava_secrets.dart", true, Category = "QA-V-02")]
        [TestCase("tools/strava/strava.local.json", true, Category = "QA-V-02")]
        [TestCase("a/b/upload.P12", true, Category = "QA-V-02")]
        [TestCase("Assets/RowMate/Bootstrap/StravaCredentials.cs", false, Category = "QA-V-02")]
        [TestCase("tools/strava/write-secrets.ps1", false, Category = "QA-V-02")]
        public void SecretNamesAreRecognized(string path, bool secret)
        {
            Assert.That(SecretOffenders(new[] { path }).Count, Is.EqualTo(secret ? 1 : 0), path);
        }

        private static List<string> SecretOffenders(IEnumerable<string> paths)
        {
            var offenders = new List<string>();
            foreach (var path in paths)
            {
                var name = path.Substring(path.LastIndexOf('/') + 1);
                var secret = false;
                foreach (var pattern in SecretPatterns)
                {
                    secret |= name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                foreach (var extension in SecretExtensions)
                {
                    secret |= name.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
                }

                if (secret)
                {
                    offenders.Add(path);
                }
            }

            return offenders;
        }

        // Versionados (en el índice).
        private static List<string> TrackedFiles(string root)
        {
            return ListFiles(root, "ls-files", "-z");
        }

        // En disco, sin versionar y sin ignorar: lo que agregaría `git add -A`.
        private static List<string> AddableFiles(string root)
        {
            return ListFiles(root, "ls-files", "-z", "--others", "--exclude-standard");
        }

        private static List<string> ListFiles(string root, params string[] args)
        {
            var exit = RunGit(root, out var output, args);
            if (exit != 0)
            {
                Unavailable("git " + string.Join(" ", args) + " salió con " + exit + ": " + output);
            }

            var files = new List<string>();
            foreach (var entry in output.Split('\0'))
            {
                if (entry.Length > 0)
                {
                    files.Add(entry.Replace('\\', '/'));
                }
            }

            return files;
        }

        private static int RunGit(string root, out string output, params string[] args)
        {
            var start = new ProcessStartInfo("git")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = root,
            };
            var quoted = new List<string> { "-C", Quote(root) };
            foreach (var arg in args)
            {
                quoted.Add(Quote(arg));
            }

            start.Arguments = string.Join(" ", quoted);
            try
            {
                using (var process = Process.Start(start))
                {
                    if (process == null)
                    {
                        Unavailable("no se pudo iniciar git");
                        output = "";
                        return -1;
                    }

                    var stdout = process.StandardOutput.ReadToEndAsync();
                    var stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    output = stdout.Result + stderr;
                    if (process.ExitCode == 128)
                    {
                        Unavailable("no hay repositorio git: " + stderr.Trim());
                    }

                    return process.ExitCode;
                }
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Unavailable("git no está instalado: " + e.Message);
                output = "";
                return -1;
            }
        }

        private static string Quote(string arg)
        {
            return arg.IndexOf(' ') >= 0 ? "\"" + arg + "\"" : arg;
        }

        private static void Touch(string root, string relative)
        {
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "prueba de higiene: sin credenciales\n");
        }

        // Los objetos de .git quedan de solo lectura en Windows: se les saca el atributo antes de borrar.
        private static void DeleteTree(string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(root, true);
        }

        private static void Unavailable(string reason)
        {
            if (Environment.GetEnvironmentVariable("ROWMATE_CI") == "1")
            {
                Assert.Fail("[CI] la higiene del repositorio necesita git: " + reason);
            }

            Assert.Inconclusive("la higiene del repositorio necesita git: " + reason);
        }
    }
}
