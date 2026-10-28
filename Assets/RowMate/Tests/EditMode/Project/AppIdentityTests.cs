using System.IO;
using System.Reflection;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // F-APP-16 (también toca): nombre, empresa, versión, identificadores provisionales y mínimos (R-14, Q-85, Q-90, D-02).
    // La parte estática es dotnet; lo visible en equipos lo verifica QA en la PC (It. 2) y en Android y Apple cuando haya equipo.
    [TestFixture]
    public sealed class AppIdentityTests
    {
        [Test, Category("AC-APP-16-01")]
        public void NameCompanyVersion()
        {
            // legacy: pubspec.yaml:4 (1.0.0+1). En Windows el nombre visible deja de ser rower_app (DC-63).
            Assert.That(AppIdentity.ProductName, Is.EqualTo("RowMate"));
            Assert.That(AppIdentity.CompanyName, Is.EqualTo("iguisoft"));
            Assert.That(AppIdentity.Version, Is.EqualTo("1.0.0"));
            Assert.That(AppIdentity.BuildNumber, Is.EqualTo(1));
        }

        [Test, Category("AC-APP-16-02")]
        public void ProvisionalIdsAndNoComExample()
        {
            // AC-APP-16-02 (texto corregido, QA-02): Android com.iguisoft.rowmate; iOS, macOS y el id único de Standalone
            // com.iguisoft.rowmate.unity; Windows sin id propio (producto RowMate de iguisoft); com.iguisoft.rowmate de Windows
            // solo como dato reservado para el MSIX de la It. 14.
            Assert.That(AppIdentity.AndroidApplicationId, Is.EqualTo("com.iguisoft.rowmate"));
            Assert.That(AppIdentity.AppleBundleId, Is.EqualTo("com.iguisoft.rowmate.unity"));
            Assert.That(AppIdentity.WindowsApplicationId, Is.EqualTo("com.iguisoft.rowmate"));

            foreach (var field in typeof(AppIdentity).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    var value = (string)field.GetRawConstantValue()!;
                    Assert.That(value, Does.Not.Contain("com.example"), field.Name);
                    Assert.That(value, Does.Not.Contain("rower_app"), field.Name);
                }
            }

            var texts = new[]
            {
                RepoPaths.Combine("Assets/RowMate/Core/Common/AppIdentity.cs"),
                RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap/ProjectBootstrapper.cs"),
                RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap/PlayerSettingsStep.cs"),
                RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap/UrpProvisioner.cs"),
                RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap/BootstrapState.cs"),
                RepoPaths.Combine("Packages/manifest.json"),
            };
            foreach (var path in texts)
            {
                Assert.That(File.ReadAllText(path), Does.Not.Contain("com.example"), RepoPaths.Relative(path));
            }
        }

        [Test, Category("AC-APP-16-03")]
        public void IdsDifferFromLegacy()
        {
            // R-14: un build nuevo no reemplaza ni borra la app legacy (Android com.example.rower_app, Apple com.iguisoft.rowmate).
            Assert.That(AppIdentity.AndroidApplicationId, Is.Not.EqualTo("com.example.rower_app"));
            Assert.That(AppIdentity.AppleBundleId, Is.Not.EqualTo("com.iguisoft.rowmate"));
        }

        [Test, Category("AC-APP-16-04")]
        public void AndroidMin26Arm64()
        {
            // Q-90: Android 8.0 (API 26) o más nuevo y solo ARM64. El texto del bootstrapper lo cubre BootstrapperStaticTests.
            Assert.That(AppIdentity.AndroidMinSdk, Is.EqualTo(26));
            var code = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap/PlayerSettingsStep.cs")));
            Assert.That(code, Does.Contain("minSdkVersion = (AndroidSdkVersions)AppIdentity.AndroidMinSdk"));
            Assert.That(code, Does.Contain("targetArchitectures = AndroidArchitecture.ARM64"));
        }

        [Test, Category("AC-APP-16-05")]
        public void IosMin15()
        {
            Assert.That(AppIdentity.IosMinimumVersion, Is.EqualTo("15.0"));
        }

        [Test, Category("AC-APP-16-01"), Ignore("Windows: QA en equipo (05-qa-verification de la It. 2: RowMate.exe, título y registro). [Device] Android: cuando estén el módulo y el teléfono; [Apple] It. 10b")]
        public void VisibleNameOnDevices()
        {
        }

        [Test, Category("AC-APP-16-02"), Ignore("Windows: QA en equipo (05-qa-verification de la It. 2: LocalLow/iguisoft/RowMate y las preferencias del legacy intactas). [Device] Android: cuando estén el módulo y el teléfono; [Apple] It. 10b")]
        public void WindowsDataFolderAndCoexistence()
        {
        }

        [Test, Category("AC-APP-16-03"), Ignore("[Device] QA en equipo: abrir la app legacy después de instalar la nueva en Windows (prerrequisito opcional del usuario) y en Android cuando estén el módulo y el teléfono; [Apple] It. 10b")]
        public void InstallsNextToTheLegacyApp()
        {
        }

        [Test, Category("AC-APP-16-04"), Ignore("[Device] QA en equipo cuando estén el módulo de Android y un teléfono: no instala en Android 7.x ni en 32 bits (final en la It. 14)")]
        public void AndroidMinimumOnDevices()
        {
        }

        [Test, Category("AC-APP-16-05"), Ignore("NO VERIFICABLE → [Apple] It. 10b: iOS 15.0 en el proyecto de Xcode")]
        public void IosMinimumOnDevices()
        {
        }
    }
}
