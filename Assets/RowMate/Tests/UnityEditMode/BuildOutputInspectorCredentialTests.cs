using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using RowMate.EditorTools;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // BOI · Credenciales y red de Strava en el build (12-exportar-tcx/02-tech-design §3.6; QA-G12-01; AC-STR-09-28): cada aguja se
    // detecta en UTF-8 y UTF-16 en un .dll, un .json y un archivo sin extensión (FindForbiddenTraces y FindCredentialTraces), y los
    // restos de Strava que se quedan (StravaActivityId, strava_activity_id, BrandStrava, la palabra Strava y el catálogo real con
    // los textos profile*) NO disparan.
    public sealed class BuildOutputInspectorCredentialTests
    {
        [Test, Category("AC-STR-09-28")]
        public void EachCredentialNeedleIsFlagged()
        {
            Assert.That(BuildOutputInspector.CredentialNeedles, Is.EquivalentTo(new[]
            {
                "strava.com/api", "strava.com/oauth", "api/v3/uploads", "client_secret", "refresh_token", "rowmate://",
            }));
            Assert.That(BuildOutputInspector.DiagnosticNeedles, Does.Contain("-rowmate-export-smoke"), "el humo de exportar, solo en desarrollo");
            var root = Path.Combine(Path.GetTempPath(), "rowmate-credentials-" + Guid.NewGuid().ToString("N"));
            try
            {
                var data = Path.Combine(root, "RowMate_Data");
                Directory.CreateDirectory(Path.Combine(data, "Managed"));
                // El build del simulador lleva sus ensamblados a propósito: FindCredentialTraces no los cuenta (sí FindForbiddenTraces).
                File.WriteAllBytes(Path.Combine(data, "Managed", "RowMate.Simulator.dll"), Encoding.UTF8.GetBytes("limpio"));
                Assert.That(BuildOutputInspector.FindCredentialTraces(root), Is.Empty, "sin agujas (con el ensamblado del simulador)");
                Assert.That(BuildOutputInspector.FindForbiddenTraces(root), Has.Some.Contains("ensamblado del simulador"));
                File.Delete(Path.Combine(data, "Managed", "RowMate.Simulator.dll"));
                foreach (var needle in BuildOutputInspector.CredentialNeedles)
                {
                    foreach (var file in new[] { "Managed/RowMate.App.dll", "RuntimeInitializeOnLoads.json", "level0" })
                    {
                        foreach (var encoding in new[] { Encoding.UTF8, Encoding.Unicode })
                        {
                            var path = Path.Combine(data, file);
                            File.WriteAllBytes(path, encoding.GetBytes("xx " + needle + " yy"));
                            Assert.That(BuildOutputInspector.FindCredentialTraces(root), Has.Some.Contains("\"" + needle + "\""), file + ": " + needle);
                            Assert.That(BuildOutputInspector.FindForbiddenTraces(root), Has.Some.Contains("\"" + needle + "\""), file + ": " + needle);
                            File.Delete(path);
                        }
                    }
                }
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test, Category("AC-STR-09-28")]
        public void StravaLeftoversAreNotFlagged()
        {
            var root = Path.Combine(Path.GetTempPath(), "rowmate-credentials-" + Guid.NewGuid().ToString("N"));
            try
            {
                var data = Path.Combine(root, "RowMate_Data");
                Directory.CreateDirectory(data);
                var leftovers = new StringBuilder();
                leftovers.Append("strava_activity_id StravaActivityId BrandStrava Strava ");
                leftovers.Append(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Localization/strings.es.json")));
                leftovers.Append(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Localization/strings.en.json")));
                Assert.That(leftovers.ToString(), Does.Contain("profile"), "el catálogo real, con las claves profile*");
                File.WriteAllBytes(Path.Combine(data, "resources.assets"), Encoding.UTF8.GetBytes(leftovers.ToString()));
                File.WriteAllBytes(Path.Combine(data, "sharedassets0.assets"), Encoding.Unicode.GetBytes(leftovers.ToString()));
                Assert.That(BuildOutputInspector.FindCredentialTraces(root), Is.Empty, string.Join("\n", BuildOutputInspector.FindCredentialTraces(root)));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
