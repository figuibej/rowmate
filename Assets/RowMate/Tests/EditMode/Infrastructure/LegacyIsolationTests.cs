using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.EditMode.Project;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // LIT: la base del legacy no se toca (AC-HIS-17-03; Q-11, R-14; QA-G3-14), en lo que se puede ver sin el equipo: ningún
    // .cs de producción de Assets/RowMate (fuera de Tests/) nombra la carpeta ni las librerías de datos del legacy
    // (sqflite_common_ffi en .dart_tool/…, las preferencias de shared_preferences en com.example/rower_app), y el nombre de la
    // base (rower_app) aparece solo en DatabaseFiles.cs. Se mira el código sin comentarios (los comentarios citan el legacy).
    // El resto (que la base y las preferencias del legacy sigan con el mismo contenido y fecha) es [Device-Win].
    [TestFixture]
    public sealed class LegacyIsolationTests
    {
        private static readonly string[] LegacyData = { "com.example", ".dart_tool", "sqflite", "shared_preferences", "FlutterSharedPreferences" };

        [Test, Category("AC-HIS-17-03")]
        public void NoCodeReachesTheLegacyData()
        {
            var offenders = new List<string>();
            var scanned = 0;
            foreach (var file in SourceScan.CsFiles(""))
            {
                var relative = RepoPaths.Relative(file);
                if (relative.StartsWith("Assets/RowMate/Tests/", StringComparison.Ordinal))
                {
                    continue;
                }

                scanned++;
                var code = SourceScan.StripComments(File.ReadAllText(file));
                foreach (var name in LegacyData)
                {
                    if (code.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(relative + ": " + name);
                    }
                }

                if (code.IndexOf("rower_app", StringComparison.Ordinal) >= 0 && relative != "Assets/RowMate/Infrastructure/Database/DatabaseFiles.cs")
                {
                    offenders.Add(relative + ": rower_app fuera de DatabaseFiles.cs");
                }
            }

            Assert.That(scanned, Is.GreaterThan(100), "el escaneo no encontró las fuentes");
            Assert.That(offenders, Is.Empty, "código que nombra los datos del legacy:\n" + string.Join("\n", offenders));
            Assert.That(SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Infrastructure/Database/DatabaseFiles.cs"))),
                Does.Contain("\"rower_app.db\"").And.Contain("\"rower_app_dev.db\""));
        }
    }
}
