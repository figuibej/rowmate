using System;
using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Platform;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // PFU · El archivo de preferencias con Mono (05-series-parciales/02-tech-design §2.7, §6.3; AC-WKT-18-11; QA-G5T-05; ADR 021):
    // el reemplazo atómico (File.Replace / File.Move) deja el viejo o el nuevo, nunca uno a medias, y el almacén real guarda y vuelve a
    // leer, SOLO en una carpeta temporal propia. De DataLocation.PreferencesFile se compara solo la cadena (la carpeta de la base y
    // prefs.json): nunca se abre, crea ni escribe el archivo del usuario (AC-HIS-20-04). Las rutas se arman en tiempo de ejecución.
    public sealed class PreferencesFileTests
    {
        // Los de PreferencesFiles (Infrastructure, que este ensamblado no referencia: §1.2, ninguna referencia nueva). El almacén
        // real con Mono lo prueban BootScenePreferencesTests (PlayMode, el arranque) y JFP (dotnet).
        internal const string FileName = "prefs.json";
        internal const string TempSuffix = ".tmp";

        private string _folder = "";

        [SetUp]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), TempDatabase.RootName, "pfu-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test, Category("AC-WKT-18-11"), Category("AC-HIS-20-04")]
        public void ThePathIsTheDatabaseFolderPlusPrefsJson()
        {
            var previous = Environment.GetEnvironmentVariable(DataLocation.Variable);
            try
            {
                // Sin la variable de desarrollo: la carpeta privada de la app (solo la cadena; no se toca el archivo).
                Environment.SetEnvironmentVariable(DataLocation.Variable, null);
                var platform = PlatformFactory.Create(new RecordingLogger());
                var path = DataLocation.PreferencesFile(platform);
                Assert.That(Path.GetFileName(path), Is.EqualTo(FileName), "prefs.json, el mismo para los dos builds (P-11)");
                Assert.That(Path.GetDirectoryName(path), Is.EqualTo(Path.GetFullPath(DataLocation.DatabaseDirectory(platform)).TrimEnd(Path.DirectorySeparatorChar))
                    .Or.EqualTo(DataLocation.DatabaseDirectory(platform).TrimEnd(Path.DirectorySeparatorChar)), "en la carpeta de la base (DT5-05)");
            }
            finally
            {
                Environment.SetEnvironmentVariable(DataLocation.Variable, previous);
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void ReplaceLeavesTheOldOrTheNewWithMono()
        {
            var target = Path.Combine(_folder, FileName);
            var temp = target + TempSuffix;
            File.WriteAllText(target, "{\"a\":1}");
            File.WriteAllText(temp, "{\"a\":2}");
            File.Replace(temp, target, null);
            Assert.That(File.ReadAllText(target), Is.EqualTo("{\"a\":2}"), "el nuevo, entero");
            Assert.That(File.Exists(temp), Is.False, "sin el temporal");

            // Sin archivo todavía: File.Move.
            File.Delete(target);
            File.WriteAllText(temp, "{\"a\":3}");
            File.Move(temp, target);
            Assert.That(File.ReadAllText(target), Is.EqualTo("{\"a\":3}"));
        }

    }

    // PIS · El aislamiento (AC-HIS-20-04; QA-G5T-05): con ROWMATE_DATA_DIR en una carpeta temporal, el archivo de preferencias cae
    // adentro de ella (la usan run-unity-tests.ps1, capture-snapshots.ps1 y BootSceneTests).
    public sealed class PreferencesIsolationTests
    {
        [Test, Category("AC-HIS-20-04"), Category("AC-WKT-18-11")]
        public void WithTheDevelopmentVariableTheFileIsInsideIt()
        {
            var previous = Environment.GetEnvironmentVariable(DataLocation.Variable);
            var folder = Path.Combine(Path.GetTempPath(), TempDatabase.RootName, "pis-" + Guid.NewGuid().ToString("N"));
            try
            {
                Environment.SetEnvironmentVariable(DataLocation.Variable, folder);
                var platform = PlatformFactory.Create(new RecordingLogger());
                var path = DataLocation.PreferencesFile(platform);
                Assert.That(path, Is.EqualTo(Path.Combine(folder, PreferencesFileTests.FileName)), "adentro de la carpeta de desarrollo");
                Assert.That(File.Exists(path), Is.False, "calcularla no crea el archivo");
            }
            finally
            {
                Environment.SetEnvironmentVariable(DataLocation.Variable, previous);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
    }
}
