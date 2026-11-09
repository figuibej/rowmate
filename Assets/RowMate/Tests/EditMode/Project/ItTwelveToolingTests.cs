using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I12T · Lo de la It. 12 que se revisa por texto (12-exportar-tcx/02-tech-design §1.1, §3.4, §3.6, §6.3; QA-G12-01, m-06): el
    // golden del TCX con LF, sin datos personales; nada de credenciales ni red en lo que se distribuye; las claves de Strava sin uso
    // en las pantallas; la cadena ROWMATE_DATA_DIR fuera del código puro; y, desde que existen (WP-B1, WP-B2), el manifiesto de
    // Android sin permisos, la purga de la caché de compartir, las agujas del inspector del build y el set it12 de las capturas.
    // Lo de WP-B queda ignorado con el motivo mientras su archivo no existe.
    [TestFixture]
    public sealed class ItTwelveToolingTests
    {
        private const string Golden = "Assets/RowMate/Tests/Golden/tcx/T-1.tcx";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string BuildCli = "Assets/RowMate/Editor/Build/BuildCli.cs";
        private const string ExportManifest = "Assets/Plugins/Android/RowMateExport.androidlib/AndroidManifest.xml";
        private const string AndroidExporter = "Assets/RowMate/Platform/Export/AndroidFileExporter.cs";
        private const string Captures = "tools/unity/capture-snapshots.ps1";

        // Lo que delataría una integración viva con Strava (QA-G12-01): endpoints, credenciales y el regreso del OAuth. La palabra
        // "Strava" no es aguja: los restos (StravaActivityId, strava_activity_id, BrandStrava, profile*) se quedan (§11.1).
        public static readonly string[] CredentialNeedles =
        {
            "strava.com/api", "strava.com/oauth", "api/v3/uploads", "client_secret", "refresh_token", "rowmate://",
        };

        private static readonly string[] ShippedExtensions = { ".cs", ".json", ".uss", ".uxml", ".kt", ".xml" };

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative));
        }

        [Test, Category("AC-STR-09-01")]
        public void GoldenIsLfAndAttributesKeepIt()
        {
            var attributes = Read(".gitattributes");
            Assert.That(attributes, Does.Contain("\n*.tcx text eol=lf\n"), "el golden no pasa a CRLF en un clon de Windows");
            var bytes = File.ReadAllBytes(RepoPaths.Combine(Golden));
            Assert.That(Array.IndexOf(bytes, (byte)'\r'), Is.EqualTo(-1));
            Assert.That(bytes[bytes.Length - 1], Is.EqualTo((byte)'\n'));
        }

        [Test, Category("AC-STR-09-01"), Category("AC-STR-09-28")]
        public void GoldenHasNoPersonalData()
        {
            // Repo público: el golden es de datos inventados (spec §6), sin rutas, usuarios ni equipos.
            var text = Read(Golden);
            foreach (var forbidden in new[] { ":\\", "Users", "AppData", "/home/", "<Creator>", "<Author>", "Strava" })
            {
                Assert.That(text, Does.Not.Contain(forbidden), forbidden);
            }
        }

        [Test, Category("AC-STR-09-28")]
        public void NoCredentialNeedleInShippedSources()
        {
            // Ningún .cs, .json, .uss, .uxml, .kt ni .xml bajo Assets/ (fuera de Tests/, Editor/ y ThirdParty/, que no se versiona)
            // nombra un endpoint, una credencial ni el regreso del OAuth de Strava.
            var offenders = new List<string>();
            var scanned = 0;
            foreach (var file in RepoPaths.Files("Assets", "*"))
            {
                var relative = RepoPaths.Relative(file);
                if (relative.Contains("/Tests/") || relative.Contains("/Editor/") || relative.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)
                    || Array.IndexOf(ShippedExtensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                scanned++;
                var text = File.ReadAllText(file);
                foreach (var needle in CredentialNeedles)
                {
                    if (text.IndexOf(needle, StringComparison.Ordinal) >= 0)
                    {
                        offenders.Add(relative + ": " + needle);
                    }
                }
            }

            Assert.That(scanned, Is.GreaterThan(500), "el escaneo encontró las fuentes");
            Assert.That(offenders, Is.Empty, "credenciales o red de Strava en lo que se distribuye:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-STR-09-28"), Category("AC-STR-09-11")]
        public void ExportCodeHasNoNetwork()
        {
            // m-06: la exportación no hace ningún pedido de red.
            var patterns = new[]
            {
                new Regex(@"\bSystem\s*\.\s*Net\b"),
                new Regex(@"\busing\s+System\.Net\b"),
                new Regex(@"\bUnityWebRequest\b"),
                new Regex(@"\bHttpClient\b"),
                new Regex(@"\bWebClient\b"),
                new Regex(@"\bSocket\b"),
            };
            var folders = new[] { "App/Export", "Core/Export", "Infrastructure/Export", "Platform/Export" };
            var offenders = new List<string>();
            var files = 0;
            foreach (var file in SourceScan.CsFiles(folders))
            {
                files++;
                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(files, Is.GreaterThanOrEqualTo(12), "el escaneo encontró la exportación");
            Assert.That(offenders, Is.Empty, "red en la exportación:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-STR-09-23"), Category("AC-STR-09-28")]
        public void ProfileKeysUnusedInPresentation()
        {
            // Las claves de Strava siguen en el catálogo (spec §10.5) pero ninguna pantalla las muestra; la app arma 4 pestañas.
            var patterns = new[] { new Regex(@"\bL10nKeys\s*\.\s*Profile\w*"), new Regex(@"""profile[A-Z]\w*""") };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles("Presentation", "Bootstrap", "Platform"))
            {
                offenders.AddRange(SourceScan.Matches(file, patterns, false));
            }

            Assert.That(offenders, Is.Empty, "claves de Strava en las pantallas:\n" + string.Join("\n", offenders));
            foreach (var file in SourceScan.CsFiles("Bootstrap"))
            {
                Assert.That(SourceScan.StripComments(File.ReadAllText(file)), Does.Not.Match(@"new\s+ShellOptions\s*\(\s*true"),
                    RepoPaths.Relative(file) + ": la pestaña Perfil");
            }
        }

        [Test, Category("AC-STR-09-16")]
        public void DataDirLabelOnlyOutsidePureCode()
        {
            // DT12-10, R-04: la cadena ROWMATE_DATA_DIR va solo en Bootstrap bajo #if; el código puro (que viaja en el build normal)
            // no la tiene ni en un literal.
            var pattern = new[] { new Regex("ROWMATE_DATA_DIR") };
            var offenders = new List<string>();
            foreach (var file in SourceScan.CsFiles("Core", "App", "Infrastructure"))
            {
                offenders.AddRange(SourceScan.Matches(file, pattern, false));
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-STR-09-28")]
        public void InspectorListsCredentialNeedles()
        {
            var inspector = Read(Inspector);
            if (!inspector.Contains("CredentialNeedles"))
            {
                Assert.Ignore("pendiente WP-B1: BuildOutputInspector todavía no tiene CredentialNeedles");
            }

            foreach (var needle in CredentialNeedles)
            {
                Assert.That(inspector, Does.Contain("\"" + needle + "\","), needle);
            }

            Assert.That(Read(BuildCli), Does.Contain("FindCredentialTraces"), "también sobre el build del simulador");
        }

        [Test, Category("AC-STR-09-24")]
        public void ExportManifestNoPermissionsProviderNotExported()
        {
            if (!File.Exists(RepoPaths.Combine(ExportManifest)))
            {
                Assert.Ignore("pendiente WP-B1: todavía no existe " + ExportManifest);
            }

            var manifest = Read(ExportManifest);
            Assert.That(manifest, Does.Not.Contain("uses-permission"), "sin permisos nuevos (F-APP-13)");
            Assert.That(manifest, Does.Contain("<provider"));
            Assert.That(manifest, Does.Contain("RmExportProvider"));
            Assert.That(manifest, Does.Contain("android:exported=\"false\""));
            Assert.That(manifest, Does.Contain("android:grantUriPermissions=\"true\""));
            Assert.That(manifest, Does.Not.Contain("INTERNET").And.Not.Contain("WRITE_EXTERNAL_STORAGE"));
        }

        [Test, Category("AC-STR-09-24")]
        public void AndroidExporterPurgesWithShareCacheMaxAge()
        {
            if (!File.Exists(RepoPaths.Combine(AndroidExporter)))
            {
                Assert.Ignore("pendiente WP-B1: todavía no existe " + AndroidExporter);
            }

            var code = SourceScan.StripComments(Read(AndroidExporter));
            Assert.That(code, Does.Contain("FolderExportWriter.DeleteOlderThan("));
            Assert.That(code, Does.Contain("FolderExportWriter.ShareCacheMaxAge"));
            Assert.That(code, Does.Contain("TcxDocument.Extension").Or.Contain("request.Extension"));
            Assert.That(code, Does.Contain("ExportDelivery.Share"));
        }

        [Test, Category("AC-STR-09-19")]
        public void CaptureSetIt12WritesToItsFolder()
        {
            var script = Read(Captures);
            if (!script.Contains("'it12'"))
            {
                Assert.Ignore("pendiente WP-B2: capture-snapshots.ps1 todavía no tiene el set it12");
            }

            Assert.That(script, Does.Contain("12-exportar-tcx"), "la carpeta de la 12 (y no la de la 11)");
        }
    }
}
