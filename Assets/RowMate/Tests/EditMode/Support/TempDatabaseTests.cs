using System;
using System.IO;
using NUnit.Framework;
using RowMate.Infrastructure.Database;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Support
{
    // TDT: la base de prueba y su guardia (AC-HIS-20-04; spec §0.4; 02-tech-design §2.4): las pruebas nunca abren, crean ni
    // modifican rower_app.db ni rower_app_dev.db en la carpeta de datos del usuario. Los mensajes nombran el archivo, nunca la
    // carpeta (QA-G3-20).
    [TestFixture]
    public sealed class TempDatabaseTests
    {
        [Test, Category("AC-HIS-20-04")]
        public void RefusesUserDataPaths()
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var userData = Path.Combine(profile, "AppData", "LocalLow", "iguisoft", "RowMate");
            foreach (var path in new[]
            {
                Path.Combine(userData, "rower_app.db"), Path.Combine(userData, "rower_app_dev.db"), Path.Combine(userData, "cualquiera.db"),
                Path.Combine(profile, "Library", "Application Support", "iguisoft", "RowMate", "x.db"),
                Path.Combine(profile, ".config", "unity3d", "iguisoft", "RowMate", "x.db"),
            })
            {
                var error = Assert.Throws<InvalidOperationException>(() => TempDatabase.Guard(path), path);
                Assert.That(error!.Message, Does.Contain(Path.GetFileName(path)), "nombra el archivo");
                Assert.That(error.Message, Does.Not.Contain(profile), "no nombra la carpeta");
                Assert.That(error.Message, Does.Not.Contain("iguisoft"));
            }

            var elsewhere = Path.Combine(Path.GetTempPath(), "otra-carpeta", DatabaseFiles.NormalName);
            var outside = Assert.Throws<InvalidOperationException>(() => TempDatabase.Guard(elsewhere));
            Assert.That(outside!.Message, Does.Contain("rower_app.db"));
            Assert.That(outside.Message, Does.Not.Contain("otra-carpeta"));
            Assert.Throws<InvalidOperationException>(() => TempDatabase.Guard(Path.Combine(Path.GetTempPath(), "x", "ROWER_APP_DEV.DB")), "sin distinguir mayúsculas");
            Assert.DoesNotThrow(() => TempDatabase.Guard(Path.Combine(TempDatabase.Root, "abc", DatabaseFiles.NormalName)), "dentro de la carpeta de pruebas sí");
            Assert.DoesNotThrow(() => TempDatabase.Guard(Path.Combine(Path.GetTempPath(), "x", "otra.db")));
        }

        // La carpeta de cada prueba vive en <temp>/RowMate.Tests/ y admite los nombres de la app adentro (para comparar
        // variantes); se borra en Dispose.
        [Test, Category("AC-HIS-20-04")]
        public void EachTestHasItsOwnFolderThatGoesAway()
        {
            string folder;
            using (var db = new TempDatabase(DatabaseFiles.NormalName))
            {
                folder = db.Folder;
                Assert.That(Path.GetFullPath(folder), Does.StartWith(Path.GetFullPath(TempDatabase.Root)));
                Assert.That(Path.GetFileName(db.FilePath), Is.EqualTo("rower_app.db"));
                Assert.That(Path.GetFileName(db.PathOf(DatabaseFiles.SimulatorName)), Is.EqualTo("rower_app_dev.db"));
                Assert.Throws<ArgumentException>(() => db.PathOf("../escape.db"));
                using (var other = new TempDatabase())
                {
                    Assert.That(other.Folder, Is.Not.EqualTo(db.Folder));
                }

                File.WriteAllText(db.FilePath, "x");
            }

            Assert.That(Directory.Exists(folder), Is.False, "Dispose borra la carpeta");
        }

        // QA-G3-13 g: toda carpeta temporal lleva ñ y á, y SQLite trabaja ahí (sqlite-net pasa la ruta a UTF-8).
        [Test, Category("AC-HIS-20-04")]
        public void WorksUnderAFolderWithAccents()
        {
            var logger = new RecordingLogger();
            using (var db = new TempDatabase())
            {
                Assert.That(Path.GetFileName(db.Folder), Does.EndWith("ñá"));
                using (var repository = db.Repository(logger))
                {
                    var saved = repository.InsertAsync(RoutineSeeds.EscaleraSpm()).GetAwaiter().GetResult();
                    Assert.That(repository.GetByIdAsync(saved.Id!.Value).GetAwaiter().GetResult()!.Steps.Count, Is.EqualTo(4));
                }

                Assert.That(File.Exists(db.FilePath), Is.True, "el archivo está en la carpeta con acentos");
            }
        }
    }
}
