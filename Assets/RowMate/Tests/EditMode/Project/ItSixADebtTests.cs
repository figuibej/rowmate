using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I6D · Revisión estática de la deuda de la It. 5 que se salda al empezar la 6a (06a-escena-lago/00-deuda-5 §3):
    // - las herramientas que corren Unity (pruebas, capturas y builds) explican el 198 (sin licencia: la Hub cerrada después de
    //   reiniciar el equipo) en lugar de dejar solo el código; siguen siendo solo ASCII (Windows PowerShell 5.1) y el README lo dice;
    // - O-4: los textos que se dibujan carácter por carácter piden la cadena a Core/Common/CharStrings (sin tablas propias).
    [TestFixture]
    public sealed class ItSixADebtTests
    {
        private const string UnityTests = "tools/unity/run-unity-tests.ps1";
        private const string Captures = "tools/unity/capture-snapshots.ps1";
        private const string Build = "tools/build/build.ps1";
        private const string Readme = "tools/unity/README.md";

        [Test]
        public void UnityLicenseExitIsExplained()
        {
            foreach (var script in new[] { UnityTests, Captures })
            {
                var invoke = BootstrapperStaticTests.Body(Read(script), "function Invoke-Unity");
                Assert.That(invoke, Does.Contain("if ($process.ExitCode -eq 198) { Write-Host '[unity] Unity salio con 198: no encontro una licencia valida."),
                    script + ": el 198 se explica en la consola");
                Assert.That(invoke, Does.Contain("Abri Unity Hub"), script);
                Assert.That(invoke.IndexOf("$process.WaitForExit()", System.StringComparison.Ordinal),
                    Is.LessThan(invoke.IndexOf("-eq 198", System.StringComparison.Ordinal)), script + ": después de esperar a Unity");
                AssertAscii(script);
            }

            var build = Read(Build);
            Assert.That(build, Does.Contain("if ($code -eq 198) { Write-Host '[build] Unity salio con 198: no encontro una licencia valida."),
                "build.ps1 también");
            AssertAscii(Build);
            Assert.That(Read(Readme), Does.Contain("**Unity sale con 198**").And.Contain("Se arregla abriendo Unity Hub"), "el README lo explica");
        }

        [Test]
        public void DrawnTextsAskCharStringsForTheirCharacters()
        {
            foreach (var file in new[] { "Assets/RowMate/Presentation/Controls/DrawnText.cs", "Assets/RowMate/Presentation/Controls/TabularNumber.cs" })
            {
                var code = SourceScan.StripComments(Read(file));
                Assert.That(code, Does.Contain("return RowMate.Core.Common.CharStrings.Of(c);"), file);
                Assert.That(code, Does.Not.Contain("BuildCharStrings"), file + ": sin tabla propia");
                Assert.That(code, Does.Not.Contain("c.ToString()"), file + ": nunca una cadena nueva por carácter");
            }
        }

        private static void AssertAscii(string relative)
        {
            var bytes = File.ReadAllBytes(RepoPaths.Combine(relative));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.That(bytes[i], Is.LessThan(128), relative + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
            }
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }
    }
}
