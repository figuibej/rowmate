using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // AMT: el plugin de Android como texto (10a 02-tech-design §4.1, §4.2, §4.5): el manifiesto del .androidlib declara
    // EXACTAMENTE los permisos del legacy (AC-APP-13-01, am:4-19), la biblioteca es solo el manifiesto y las reglas de R8, los
    // ProjectSettings siguen con API 26 y ARM64 sin tocarlos, y el Kotlin no empareja, no cambia la radio ni depende del
    // reproductor de Unity (AC-BLE-SEC-01, DT10-11). El manifiesto fusionado del APK y el teléfono: NO VERIFICABLE hasta "Android
    // Build Support" ([Device-Android]).
    [TestFixture]
    public sealed class AndroidManifestTests
    {
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";
        private const string Library = "Assets/Plugins/Android/RowMateBle.androidlib";
        private const string Sources = "Assets/Plugins/Android/RowMateBle";

        private static readonly string[] KotlinFiles =
        {
            "AdapterMonitor.kt", "EventRing.kt", "GattQueue.kt", "KeepAlive.kt", "RmBle.kt", "ScanSession.kt", "Wire.kt",
        };

        private static XmlDocument Manifest()
        {
            var path = RepoPaths.Combine(Library, "AndroidManifest.xml");
            Assert.That(File.Exists(path), Is.True, Library + "/AndroidManifest.xml no está");
            var doc = new XmlDocument();
            doc.Load(path);
            return doc;
        }

        private static string Attribute(XmlElement element, string name)
        {
            return element.GetAttribute(name, AndroidNs);
        }

        [Test, Category("AC-APP-13-01")]
        public void ExactPermissions()
        {
            var root = Manifest().DocumentElement!;
            var permissions = new List<string>();
            foreach (XmlElement element in root.GetElementsByTagName("uses-permission"))
            {
                var name = Attribute(element, "name");
                var max = Attribute(element, "maxSdkVersion");
                var flags = Attribute(element, "usesPermissionFlags");
                permissions.Add(name + "|" + max + "|" + flags);
            }

            Assert.That(permissions, Is.EquivalentTo(new[]
            {
                "android.permission.BLUETOOTH_SCAN||neverForLocation",
                "android.permission.BLUETOOTH_CONNECT||",
                "android.permission.BLUETOOTH|30|",
                "android.permission.BLUETOOTH_ADMIN|30|",
                "android.permission.ACCESS_FINE_LOCATION|30|",
                "android.permission.WAKE_LOCK||",
            }));

            // Sin VIBRATE (D10-11: performHapticFeedback) y sin ubicación en Android 12+ (maxSdkVersion 30, neverForLocation).
            Assert.That(permissions.Any(p => p.Contains("VIBRATE")), Is.False);
            Assert.That(permissions.Any(p => p.Contains("ACCESS_COARSE_LOCATION") || p.Contains("ACCESS_BACKGROUND_LOCATION")), Is.False);
            Assert.That(root.GetElementsByTagName("uses-permission-sdk-23").Count, Is.EqualTo(0));

            var features = root.GetElementsByTagName("uses-feature");
            Assert.That(features.Count, Is.EqualTo(1));
            var feature = (XmlElement)features[0]!;
            Assert.That(Attribute(feature, "name"), Is.EqualTo("android.hardware.bluetooth_le"));
            Assert.That(Attribute(feature, "required"), Is.EqualTo("true"));
        }

        [Test, Category("AC-APP-13-01")]
        public void TheLibraryIsOnlyTheManifestAndKeepRules()
        {
            var root = Manifest().DocumentElement!;
            // AGP 8: el namespace va en build.gradle; un package en el manifiesto de la biblioteca es un error de compilación.
            Assert.That(root.HasAttribute("package"), Is.False);
            Assert.That(root.GetElementsByTagName("application").Count, Is.EqualTo(0));

            var properties = File.ReadAllText(RepoPaths.Combine(Library, "project.properties"));
            Assert.That(properties, Does.Contain("android.library=true"));

            var gradle = File.ReadAllText(RepoPaths.Combine(Library, "build.gradle"));
            Assert.That(gradle, Does.Contain("apply plugin: 'com.android.library'"));
            Assert.That(gradle, Does.Contain("namespace 'com.rowmate.ble.manifest'"));
            Assert.That(gradle, Does.Contain("minSdkVersion 26"));
            Assert.That(gradle, Does.Contain("consumerProguardFiles 'proguard-rules.pro'"));

            // RmBle se llama solo por JNI: R8 no puede renombrarlo ni quitarlo.
            var rules = File.ReadAllText(RepoPaths.Combine(Library, "proguard-rules.pro"));
            Assert.That(rules, Does.Contain("-keep class com.rowmate.ble.** { *; }"));
        }

        [Test, Category("AC-APP-13-01")]
        public void ProjectSettingsKeepApi26AndArm64()
        {
            // Q-90: ya estaban; el plugin no los toca (el manifiesto se fusiona).
            var settings = File.ReadAllText(RepoPaths.Combine("ProjectSettings", "ProjectSettings.asset"));
            Assert.That(settings, Does.Contain("AndroidMinSdkVersion: 26"));
            Assert.That(settings, Does.Contain("AndroidTargetArchitectures: 2"));
        }

        [Test, Category("AC-BLE-SEC-01")]
        public void KotlinNeverPairsNorTouchesTheRadio()
        {
            var folder = RepoPaths.Combine(Sources);
            var files = Directory.GetFiles(folder, "*.kt").Select(f => Path.GetFileName(f)!).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.That(files, Is.EqualTo(KotlinFiles));

            var forbidden = new[]
            {
                "createBond", "removeBond", "cancelBondProcess", "setPairingConfirmation", ".enable()", ".disable()", "UnityPlayer",
                "com.unity3d",
            };
            foreach (var name in KotlinFiles)
            {
                var text = File.ReadAllText(Path.Combine(folder, name));
                foreach (var pattern in forbidden)
                {
                    Assert.That(text.Contains(pattern), Is.False, name + " contiene '" + pattern + "'");
                }
            }

            // El filtro de la búsqueda es del sistema (FiltersScanInOs) y el reloj es CLOCK_MONOTONIC (DT10-18), no elapsedRealtime.
            var scan = File.ReadAllText(Path.Combine(folder, "ScanSession.kt"));
            Assert.That(scan, Does.Contain("setServiceUuid"));
            var ring = File.ReadAllText(Path.Combine(folder, "RmBle.kt"));
            Assert.That(ring, Does.Contain("System.nanoTime()"));
            foreach (var name in KotlinFiles)
            {
                Assert.That(File.ReadAllText(Path.Combine(folder, name)), Does.Not.Contain("elapsedRealtime"), name);
            }
        }

        [Test, Category("AC-BLE-SEC-01")]
        public void AndroidFilesHaveNoMachinePaths()
        {
            // Repo público: ni rutas absolutas ni carpetas de usuario en el plugin, sus herramientas y la fijación.
            var roots = new[] { RepoPaths.Combine(Sources), RepoPaths.Combine(Library), RepoPaths.Combine("native", "android") };
            var pattern = new Regex(@"[A-Za-z]:[\\/](Users|Program Files)|/Users/|/home/", RegexOptions.IgnoreCase);
            var checkedFiles = 0;
            foreach (var root in roots)
            {
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var match = pattern.Match(File.ReadAllText(file));
                    Assert.That(match.Success, Is.False, RepoPaths.Relative(file) + " contiene '" + match.Value + "'");
                    checkedFiles++;
                }
            }

            Assert.That(checkedFiles, Is.GreaterThanOrEqualTo(KotlinFiles.Length + 4));
        }
    }
}
