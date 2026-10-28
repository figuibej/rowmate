using NUnit.Framework;
using RowMate.Infrastructure.Database;
using RowMate.Tests.Support;
using static RowMate.Tests.EditMode.Infrastructure.DatabaseFileState;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // DFT: la base según la variante (F-HIS-20; lib/core/database/database_service.dart:10-12).
    [TestFixture]
    public sealed class DatabaseFilesTests
    {
        // AC-HIS-20-01: rower_app_dev.db con el simulador y rower_app.db sin él, con el mismo esquema v8.
        [Test, Category("AC-HIS-20-01")]
        public void NameByVariant()
        {
            Assert.That(DatabaseFiles.NameFor(false), Is.EqualTo("rower_app.db"));
            Assert.That(DatabaseFiles.NameFor(true), Is.EqualTo("rower_app_dev.db"));
            Assert.That(DatabaseFiles.NormalName, Is.EqualTo("rower_app.db"));
            Assert.That(DatabaseFiles.SimulatorName, Is.EqualTo("rower_app_dev.db"));
            Assert.That(DatabaseFiles.SchemaVersion, Is.EqualTo(8));
        }

        // AC-HIS-20-01 (y la regla de AC-HIS-20-02): las dos variantes en la MISMA carpeta no se mezclan.
        [Test, Category("AC-HIS-20-01")]
        public void VariantsInTheSameFolderDoNotMix()
        {
            var logger = new RecordingLogger();
            using (var db = new TempDatabase())
            {
                using (var normal = db.Repository(logger, null, DatabaseFiles.NameFor(false)))
                using (var simulator = db.Repository(logger, null, DatabaseFiles.NameFor(true)))
                {
                    Done(simulator.InsertAsync(RoutineSeeds.EscaleraSpm()));
                    Assert.That(Done(normal.GetAllAsync()), Is.Empty, "lo del simulador no aparece en la normal");
                    Done(normal.InsertAsync(RoutineSeeds.SoloCalentamiento()));
                    var simulated = Done(simulator.GetAllAsync());
                    Assert.That(simulated.Count, Is.EqualTo(1));
                    Assert.That(simulated[0].Name, Is.EqualTo("Escalera SPM"));
                    Assert.That(Done(simulator.Database.QueryRawForTestsAsync("PRAGMA user_version"))[0][0], Is.EqualTo(8L), "mismo esquema");
                    Assert.That(Done(normal.Database.QueryRawForTestsAsync("PRAGMA user_version"))[0][0], Is.EqualTo(8L));
                }

                Assert.That(Of(db.Folder).Keys, Is.EquivalentTo(new[] { "rower_app.db", "rower_app_dev.db" }));
            }
        }
    }
}
