using System;
using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Platform;
using RowMate.Platform.Storage;
using RowMate.Tests.Support;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // DLT · Carpeta de la base local (AC-HIS-17-01, AC-HIS-20-04; 03-rutinas/02-tech-design §2.4). La carpeta real del
    // usuario solo se calcula (nunca se abre una base ahí); el reemplazo por ROWMATE_DATA_DIR va a una carpeta temporal.
    public sealed class DataLocationTests
    {
        [Test, Category("AC-HIS-17-01")]
        public void WindowsUsesLocalLow()
        {
            // En el Editor y en el player de Windows: Application.persistentDataPath = …\AppData\LocalLow\iguisoft\RowMate.
            var path = Application.persistentDataPath.Replace('\\', '/');
            Assert.That(Application.companyName, Is.EqualTo("iguisoft"), "empresa de AC-APP-16-02");
            Assert.That(Application.productName, Is.EqualTo("RowMate"));
            Assert.That(path, Does.EndWith("/iguisoft/RowMate"), "la carpeta lleva empresa y producto");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Assert.That(path, Does.Contain("/AppData/LocalLow/"), "Windows: LocalLow, privada del usuario");
            }

            var source = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Platform/Storage/AppDataLocation.cs"));
            Assert.That(source, Does.Contain("var folder = Application.persistentDataPath;"), "fuera de Android, persistentDataPath");
        }

        [Test, Category("AC-HIS-17-01")]
        public void AndroidUsesTheDatabasesFolder()
        {
            // El camino de código por JNI (sin teléfono no se puede correr: [Device-And]).
            var source = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Platform/Storage/AppDataLocation.cs"));
            Assert.That(source, Does.Contain("#if UNITY_ANDROID && !UNITY_EDITOR"));
            Assert.That(source, Does.Contain("\"getDatabasePath\""), "context.getDatabasePath(...)");
            Assert.That(source, Does.Contain("\"getParentFile\""), "la carpeta databases de la app, interna");
            Assert.That(source, Does.Not.Contain("\"getExternalFilesDir\""), "nunca el almacenamiento externo (ninguna llamada JNI a getExternalFilesDir)");
            Assert.That(AppDataLocation.AndroidProbeName, Is.EqualTo("rowmate"));
        }

        [Test, Category("AC-HIS-20-04")]
        public void TheDevelopmentVariableReplacesTheFolder()
        {
            var previous = Environment.GetEnvironmentVariable(DataLocation.Variable);
            var folder = Path.Combine(Path.GetTempPath(), TempDatabase.RootName, "dlt-" + Guid.NewGuid().ToString("N"));
            try
            {
                Environment.SetEnvironmentVariable(DataLocation.Variable, folder);
                var platform = PlatformFactory.Create(new RecordingLogger());
                Assert.That(DataLocation.DatabaseDirectory(platform), Is.EqualTo(folder), "en el Editor manda ROWMATE_DATA_DIR");
                Assert.That(Directory.Exists(folder), Is.True, "y la crea");
                Assert.That(DataLocation.IsOverridden, Is.True);
                var appRoot = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Bootstrap/DataLocation.cs"));
                Assert.That(appRoot, Does.Contain("#if UNITY_EDITOR || DEVELOPMENT_BUILD"), "en un build normal la variable no existe");
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
