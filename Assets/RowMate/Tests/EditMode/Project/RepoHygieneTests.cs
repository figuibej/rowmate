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
        [TestCase("Assets/ThirdParty/Mixamo/YBot/YBot.fbx.meta", true, Category = "AC-APP-19-03")]
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

        // It. 8 (AC-SCN-20-07; 08-entornos-selector/02-tech-design §1.1): el arte de los escenarios es procedural o propio, sin
        // terceros versionados (lo cubre ThirdPartyOnlyMetaTracked) y sin nombres de lugares reales en el código (fuera de los
        // comentarios, que citan la inspiración). Los generadores, sus números y sus mallas existen y son del repositorio.
        [Test, Category("AC-SCN-20-07")]
        public void TheEnvironmentArtIsOwnAndBrandless()
        {
            var places = new System.Text.RegularExpressions.Regex(@"\b(Bled|Henley|Charles|Bosbaan)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var files = new List<string>(RepoPaths.Files("Assets/RowMate/Core/Scene/Environments", "*.cs"));
            foreach (var name in new[] { "Art/RiverArt.cs", "Art/CoastArt.cs", "Art/RegattaArt.cs", "Geometry/RiverMeshes.cs", "Geometry/CoastMeshes.cs", "Geometry/RegattaMeshes.cs" })
            {
                files.Add(RepoPaths.Combine("Assets/RowMate/Core/Scene/" + name));
            }

            Assert.That(files.Count, Is.GreaterThanOrEqualTo(12));
            var offenders = new List<string>();
            foreach (var file in files)
            {
                Assert.That(File.Exists(file), Is.True, file);
                foreach (var line in File.ReadAllLines(file))
                {
                    var code = line;
                    var comment = code.IndexOf("//", StringComparison.Ordinal);
                    if (comment >= 0)
                    {
                        code = code.Substring(0, comment);
                    }

                    if (places.IsMatch(code))
                    {
                        offenders.Add(RepoPaths.Relative(file) + ": " + line.Trim());
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "nombres de lugares reales en el código de los escenarios:\n" + string.Join("\n", offenders));
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

        // QA-G4aT-05 (02-tech-design §3.7 de la It. 4a): el repo es público. Ningún archivo de texto de tools/ tiene una ruta
        // de la PC: la carpeta del usuario o la temporal (la expresión de QA-G3-20), file:/// seguido de una unidad o de /Users
        // o /home, y la caché de pub (lo que dejan pub get y flutter test en tools/oracle/engine). Se recorre TODO tools/
        // (no solo .ps1 y .sh, como ItThreeToolingTests.NoMachinePathsInScriptsOrManifests) salvo lo que el .gitignore
        // excluye; sin llamar a git: la lista de exclusiones es la del .gitignore y se comprueba que cada una esté ahí. Las
        // carpetas de instalación por defecto (<Program Files>/dotnet, la de Unity) no son datos del usuario (QA-G3-20).
        [Test, Category("AC-APP-19-03")]
        public void ToolsHaveNoMachinePaths()
        {
            var gitignore = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(RepoPaths.Combine(".gitignore")))
            {
                gitignore.Add(line.Trim());
            }

            foreach (var ignore in ToolIgnores)
            {
                Assert.That(gitignore.Contains(ignore.Line), Is.True, "la exclusión " + ignore.Line + " no está en el .gitignore");
            }

            Assert.That(gitignore.Contains("!/tools/oracle/bin/"), Is.True, "los scripts del oráculo se versionan");
            var offenders = new List<string>();
            var scanned = 0;
            foreach (var file in Directory.GetFiles(RepoPaths.Combine("tools"), "*", SearchOption.AllDirectories))
            {
                var relative = RepoPaths.Relative(file).Replace('\\', '/');
                if (IsIgnoredTool(relative) || IsBinary(file))
                {
                    continue;
                }

                scanned++;
                var match = MachinePath.Match(File.ReadAllText(file));
                if (match.Success)
                {
                    offenders.Add(relative + ": " + match.Value);
                }
            }

            Assert.That(scanned, Is.GreaterThan(20), "el recorrido encontró tools/");
            Assert.That(offenders, Is.Empty, "rutas de la PC en tools/ (el repo es público):\n" + string.Join("\n", offenders));
        }

        // Los casos con forma de ruta de la PC van partidos en dos o tres literales (una constante igual): así la búsqueda
        // mecánica del repo público no los confunde con una ruta real (QA-V4a-07). Son de una persona inventada ("alguien").
        [TestCase(@"C:\" + @"Users\alguien\AppData", true, Category = "AC-APP-19-03")]
        [TestCase("c:/" + "users/alguien/x", true, Category = "AC-APP-19-03")]
        [TestCase("/home/alguien/flutter", true, Category = "AC-APP-19-03")]
        [TestCase(@"D:\Temp\AppData\" + @"Local\Temp\" + @"claude\x", true, Category = "AC-APP-19-03")]
        [TestCase("\"rootUri\": \"file:///C:/x/lib/\"", true, Category = "AC-APP-19-03")]
        [TestCase("file:///" + "Users/alguien/x", true, Category = "AC-APP-19-03")]
        [TestCase("file:///home/alguien/x", true, Category = "AC-APP-19-03")]
        [TestCase(@"%LOCALAPPDATA%\Pub\Cache\hosted", true, Category = "AC-APP-19-03")]
        [TestCase("~/.pub-cache/hosted", true, Category = "AC-APP-19-03")]
        [TestCase("C:/Program Files/dotnet/dotnet.exe", false, Category = "AC-APP-19-03")]
        [TestCase(@"C:\Program Files\Unity\Hub\Editor", false, Category = "AC-APP-19-03")]
        [TestCase("$rootUri = 'file:///' + $legacy", false, Category = "AC-APP-19-03")]
        [TestCase("path: ../../../../rowmate-legacy", false, Category = "AC-APP-19-03")]
        [TestCase("<flutter-sdk>/bin/flutter", false, Category = "AC-APP-19-03")]
        public void MachinePathsAreRecognized(string text, bool machine)
        {
            Assert.That(MachinePath.IsMatch(text), Is.EqualTo(machine), text);
        }

        [TestCase("tools/oracle/engine/.dart_tool/package_config.json", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/.flutter-plugins-dependencies", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/build/test_cache/x.dill", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/pubspec_overrides.yaml", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/pubspec.lock", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/.dart_tool/oracle_package_config.json", true, Category = "AC-APP-19-03")]
        [TestCase("tools/dotnet/RowMate.Core/bin/Release/x.dll", true, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/pubspec.yaml", false, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/engine/test/engine_vectors_test.dart", false, Category = "AC-APP-19-03")]
        [TestCase("tools/oracle/bin/fmt_vectors.dart", false, Category = "AC-APP-19-03")]
        [TestCase("tools/build/build.ps1", false, Category = "AC-APP-19-03")]
        public void ToolIgnoresMatchTheGitignore(string path, bool ignored)
        {
            Assert.That(IsIgnoredTool(path), Is.EqualTo(ignored), path);
            var exit = RunGit(RepoPaths.Root, out var output, "check-ignore", "-q", "--no-index", path);
            if (exit != 0 && exit != 1)
            {
                Unavailable("git check-ignore salió con " + exit + ": " + output);
            }

            Assert.That(exit == 0, Is.EqualTo(ignored), path + ": el .gitignore dice otra cosa");
        }

        // La expresión de QA-G3-20 más file:/// con una unidad, /Users o /home, y la caché de pub.
        internal static readonly System.Text.RegularExpressions.Regex MachinePath = new System.Text.RegularExpressions.Regex(
            @"[A-Za-z]:[\\/]+Users[\\/]|[\\/]home[\\/][a-z]|AppData[\\/]+Local[\\/]+Temp[\\/]+claude|file:///+(?:[A-Za-z]:|Users/|home/)|Pub[\\/]+Cache|\.pub-cache",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Lo que el .gitignore excluye dentro de tools/ (cada línea tiene que estar en el .gitignore, al carácter).
        private static readonly (string Line, Func<string, bool> Matches)[] ToolIgnores =
        {
            ("/tools/**/bin/", p => p.Contains("/bin/") && !p.StartsWith("tools/oracle/bin/", StringComparison.Ordinal)),
            ("/tools/**/obj/", p => p.Contains("/obj/")),
            ("/tools/**/TestResults/", p => p.Contains("/TestResults/")),
            ("/tools/dotnet/.artifacts/", p => p.StartsWith("tools/dotnet/.artifacts/", StringComparison.Ordinal)),
            ("/tools/unity/.artifacts/", p => p.StartsWith("tools/unity/.artifacts/", StringComparison.Ordinal)),
            ("/tools/oracle/.dart_tool/", p => p.StartsWith("tools/oracle/.dart_tool/", StringComparison.Ordinal)),
            ("/tools/oracle/pubspec.lock", p => p == "tools/oracle/pubspec.lock"),
            ("/tools/oracle/engine/.dart_tool/", p => p.StartsWith("tools/oracle/engine/.dart_tool/", StringComparison.Ordinal)),
            ("/tools/oracle/engine/build/", p => p.StartsWith("tools/oracle/engine/build/", StringComparison.Ordinal)),
            ("/tools/oracle/engine/.flutter-plugins*", p => p.StartsWith("tools/oracle/engine/.flutter-plugins", StringComparison.Ordinal)),
            ("/tools/oracle/engine/pubspec_overrides.yaml", p => p == "tools/oracle/engine/pubspec_overrides.yaml"),
            ("/tools/oracle/engine/pubspec.lock", p => p == "tools/oracle/engine/pubspec.lock"),
            ("*.log", p => p.EndsWith(".log", StringComparison.OrdinalIgnoreCase)),
            ("*.tmp", p => p.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)),
        };

        private static bool IsIgnoredTool(string relative)
        {
            foreach (var ignore in ToolIgnores)
            {
                if (ignore.Matches(relative))
                {
                    return true;
                }
            }

            return false;
        }

        // Un NUL en los primeros 8 KiB: binario (una fuente, una imagen); no se lee como texto.
        private static bool IsBinary(string file)
        {
            var buffer = new byte[8192];
            int read;
            using (var stream = File.OpenRead(file))
            {
                read = stream.Read(buffer, 0, buffer.Length);
            }

            for (var i = 0; i < read; i++)
            {
                if (buffer[i] == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------ It. 6a (06a-escena-lago/02-tech-design §3.7, §2.10)

        // AC-SCN-15-08: lo de terceros (el Terrain Sample Asset Pack, Mixamo) vive en Assets/ThirdParty/ y no se versiona salvo
        // sus .meta; el CI compila y pasa sin el pack.
        [TestCase("Assets/ThirdParty/TerrainSampleAssetPack/Textures/Rock_Albedo.png", true, Category = "AC-SCN-15-08")]
        [TestCase("Assets/ThirdParty/TerrainSampleAssetPack/Prefabs/Fern.prefab", true, Category = "AC-SCN-15-08")]
        [TestCase("Assets/ThirdParty/TerrainSampleAssetPack/Textures/Rock_Albedo.png.meta", false, Category = "AC-SCN-15-08")]
        [TestCase("Assets/ThirdParty/TerrainSampleAssetPack.meta", false, Category = "AC-SCN-15-08")]
        [TestCase("Assets/RowMate/Slots/terrain.layer.rock.asset", false, Category = "AC-SCN-15-08")]
        public void ThirdPartyPackIsIgnoredExceptMeta(string path, bool ignored)
        {
            var exit = RunGit(RepoPaths.Root, out var output, "check-ignore", "-q", "--no-index", path);
            if (exit != 0 && exit != 1)
            {
                Unavailable("git check-ignore salió con " + exit + ": " + output);
            }

            Assert.That(exit == 0, Is.EqualTo(ignored), path + (ignored ? " tiene que estar ignorado" : " NO tiene que estar ignorado"));
        }

        // QA-G6aT-18 (AC-SCN-15-08): nada versionado de Assets/ referencia un asset de terceros salvo los .asset de los slots
        // (Assets/RowMate/Slots/): se juntan los GUID de los .meta de Assets/ThirdParty/ (versionados o por versionar) y se buscan
        // en los assets de texto de Unity versionados o por versionar. Así, sin el pack, solo un slot apunta a algo que no está y
        // cae al procedural.
        [Test, Category("AC-SCN-15-08")]
        public void NoVersionedAssetReferencesThirdPartyOutsideTheSlots()
        {
            var candidates = TrackedFiles(RepoPaths.Root);
            candidates.AddRange(AddableFiles(RepoPaths.Root));
            var thirdParty = new HashSet<string>(StringComparer.Ordinal);
            var assets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in candidates)
            {
                var full = RepoPaths.Combine(path);
                if (!File.Exists(full))
                {
                    continue;
                }

                if (path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal) && path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    var guid = MetaGuid.Match(File.ReadAllText(full));
                    if (guid.Success)
                    {
                        thirdParty.Add(guid.Groups[1].Value);
                    }
                }
                else if (path.StartsWith("Assets/", StringComparison.Ordinal) && IsUnityTextAsset(path))
                {
                    assets[path] = File.ReadAllText(full);
                }
            }

            Assert.That(assets.Count, Is.GreaterThan(10), "el recorrido encontró los assets de Unity versionados");
            var offenders = ThirdPartyReferenceOffenders(assets, thirdParty);
            Assert.That(offenders, Is.Empty, "assets versionados (fuera de los slots) que referencian Assets/ThirdParty/:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-SCN-15-08")]
        public void TheThirdPartyReferenceRuleFindsAViolation()
        {
            // Control positivo: un material que apunta a una textura del pack se detecta; el mismo GUID en un slot, no.
            const string Guid = "0123456789abcdef0123456789abcdef";
            var guids = new HashSet<string>(StringComparer.Ordinal) { Guid };
            var files = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Assets/RowMate/Art/Materials/Scene/Terrain.mat"] = "  m_Texture: {fileID: 2800000, guid: " + Guid + ", type: 3}\n",
                ["Assets/RowMate/Slots/terrain.layer.rock.asset"] = "  asset: {fileID: 11400000, guid: " + Guid + ", type: 2}\n",
                ["Assets/RowMate/Settings/Quality/Medium.asset"] = "  m_Other: {fileID: 0}\n",
            };
            Assert.That(ThirdPartyReferenceOffenders(files, guids), Is.EqualTo(new[] { "Assets/RowMate/Art/Materials/Scene/Terrain.mat: " + Guid }));
            Assert.That(MetaGuid.Match("fileFormatVersion: 2\nguid: " + Guid + "\n").Groups[1].Value, Is.EqualTo(Guid));
        }

        // ------------------------------------------------------------------ It. 7 (07-remero-palada/02-tech-design §1.3, §6.3; DT7-13)

        // AC-SCN-08-09: de Assets/ThirdParty/Mixamo/ no se versiona NADA, ni los .meta (QA7-G-04); el binding generado tampoco.
        [TestCase("Assets/ThirdParty/Mixamo/YBot/YBot.fbx", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/ThirdParty/Mixamo/YBot/YBot.fbx.meta", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/ThirdParty/Mixamo/YBot.meta", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/ThirdParty/Mixamo.meta", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/ThirdParty/Mixamo/RowerRealistic/RowerRealistic.fbx.meta", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/RowMate/Bootstrap/Generated/Resources/RowerCharacterBinding.asset", true, Category = "AC-SCN-08-09")]
        [TestCase("Assets/ThirdParty/TerrainSampleAssetPack/Textures/Rock_Albedo.png.meta", false, Category = "AC-SCN-08-09")]
        [TestCase("Assets/RowMate/Slots/rower.character.asset", false, Category = "AC-SCN-08-09")]
        public void MixamoIsNeverVersioned(string path, bool ignored)
        {
            var exit = RunGit(RepoPaths.Root, out var output, "check-ignore", "-q", "--no-index", path);
            if (exit != 0 && exit != 1)
            {
                Unavailable("git check-ignore salió con " + exit + ": " + output);
            }

            Assert.That(exit == 0, Is.EqualTo(ignored), path + (ignored ? " tiene que estar ignorado" : " NO tiene que estar ignorado"));
        }

        [Test, Category("AC-SCN-08-09")]
        public void NothingUnderMixamoIsTrackedOrAddable()
        {
            var candidates = TrackedFiles(RepoPaths.Root);
            candidates.AddRange(AddableFiles(RepoPaths.Root));
            var offenders = new List<string>();
            foreach (var path in candidates)
            {
                if (path.StartsWith("Assets/ThirdParty/Mixamo", StringComparison.Ordinal))
                {
                    offenders.Add(path);
                }
            }

            Assert.That(offenders, Is.Empty, "Mixamo versionado o por versionar:\n" + string.Join("\n", offenders));
        }

        // Ningún asset versionado (los slots incluidos) nombra un GUID de Mixamo (los .meta de la máquina, si están): el slot
        // rower.character queda en Procedural en todas las máquinas (lo arma WP-B0).
        [Test, Category("AC-SCN-08-09")]
        public void TheRowerSlotIsProceduralAndNothingReferencesMixamo()
        {
            var guids = new HashSet<string>(StringComparer.Ordinal);
            var mixamo = RepoPaths.Combine("Assets/ThirdParty/Mixamo");
            if (Directory.Exists(mixamo))
            {
                foreach (var meta in Directory.GetFiles(mixamo, "*.meta", SearchOption.AllDirectories))
                {
                    var guid = MetaGuid.Match(File.ReadAllText(meta));
                    if (guid.Success)
                    {
                        guids.Add(guid.Groups[1].Value);
                    }
                }
            }

            var candidates = TrackedFiles(RepoPaths.Root);
            candidates.AddRange(AddableFiles(RepoPaths.Root));
            var offenders = new List<string>();
            foreach (var path in candidates)
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !IsUnityTextAsset(path) || !File.Exists(RepoPaths.Combine(path)))
                {
                    continue;
                }

                var text = File.ReadAllText(RepoPaths.Combine(path));
                foreach (System.Text.RegularExpressions.Match match in GuidReference.Matches(text))
                {
                    if (guids.Contains(match.Groups[1].Value))
                    {
                        offenders.Add(path + ": " + match.Groups[1].Value);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, "assets versionados que referencian Mixamo:\n" + string.Join("\n", offenders));
        }

        // QA-G6aT-19: el puerto del generador de la VM de Dart (BSD-3) lleva su aviso. Si existe Core/Scene/DartRandom.cs, la raíz
        // del repo tiene THIRD_PARTY_NOTICES.md con el copyright, las tres condiciones y el descargo de la licencia del SDK de Dart,
        // y el encabezado del puerto remite a ese archivo (los builds lo copian junto al ejecutable: NoticesPostprocessor, WP-B2).
        [Test, Category("AC-SCN-14-02"), Category("AC-APP-19-03")]
        public void TheDartPortCarriesItsLicense()
        {
            const string Port = "Assets/RowMate/Core/Scene/DartRandom.cs";
            if (!File.Exists(RepoPaths.Combine(Port)))
            {
                Assert.Pass("sin el puerto de Dart no hace falta el aviso");
            }

            var notices = RepoPaths.Combine("THIRD_PARTY_NOTICES.md");
            Assert.That(File.Exists(notices), Is.True, "THIRD_PARTY_NOTICES.md en la raíz del repo");
            var text = File.ReadAllText(notices).Replace("\r\n", "\n");
            foreach (var part in new[]
            {
                "Copyright 2012, the Dart project authors.",
                "Redistributions of source code must retain the above copyright\nnotice, this list of conditions and the following disclaimer.",
                "Redistributions in binary form must reproduce the above\ncopyright notice, this list of conditions and the following\ndisclaimer in the documentation and/or other materials provided\nwith the distribution.",
                "Neither the name of Google LLC nor the names of its\ncontributors may be used to endorse or promote products derived\nfrom this software without specific prior written permission.",
                "THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS\n\"AS IS\" AND ANY EXPRESS OR IMPLIED WARRANTIES",
                "EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.",
                Port,
            })
            {
                Assert.That(Collapse(text), Does.Contain(Collapse(part)), "THIRD_PARTY_NOTICES.md: " + part);
            }

            var header = File.ReadAllText(RepoPaths.Combine(Port));
            Assert.That(header, Does.Contain("THIRD_PARTY_NOTICES.md").And.Contain("BSD-3").And.Contain("the Dart project authors"),
                "el encabezado de DartRandom.cs cita el origen y remite al aviso");
        }

        // Los GUID de un .meta y las referencias de un asset de texto de Unity (YAML: "guid: <32 hex>").
        private static readonly System.Text.RegularExpressions.Regex MetaGuid = new System.Text.RegularExpressions.Regex(@"(?m)^guid:\s*([0-9a-f]{32})\s*$");
        private static readonly System.Text.RegularExpressions.Regex GuidReference = new System.Text.RegularExpressions.Regex(@"\bguid:\s*([0-9a-f]{32})");

        private static readonly string[] UnityTextAssets =
        {
            ".asset", ".prefab", ".unity", ".mat", ".controller", ".overrideController", ".anim", ".playable", ".shadervariants",
            ".preset", ".lighting", ".spriteatlas", ".terrainlayer", ".mask", ".signal", ".renderTexture", ".physicMaterial", ".uxml",
            ".uss", ".tss",
        };

        private static bool IsUnityTextAsset(string path)
        {
            foreach (var extension in UnityTextAssets)
            {
                if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // "<ruta>: <guid>" por cada asset fuera de Assets/ThirdParty/ y de los .asset de Assets/RowMate/Slots/ que nombra un GUID de
        // terceros.
        private static List<string> ThirdPartyReferenceOffenders(IDictionary<string, string> assets, ISet<string> thirdParty)
        {
            var offenders = new List<string>();
            foreach (var entry in assets)
            {
                var slot = entry.Key.StartsWith("Assets/RowMate/Slots/", StringComparison.Ordinal) && entry.Key.EndsWith(".asset", StringComparison.Ordinal);
                if (slot || entry.Key.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (System.Text.RegularExpressions.Match match in GuidReference.Matches(entry.Value))
                {
                    if (thirdParty.Contains(match.Groups[1].Value))
                    {
                        offenders.Add(entry.Key + ": " + match.Groups[1].Value);
                    }
                }
            }

            offenders.Sort(StringComparer.Ordinal);
            return offenders;
        }

        // Para comparar el texto de la licencia sin depender de la sangría de cada línea.
        private static string Collapse(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
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
