using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // It. 14 (14-pulido-plataformas/02-tech-design §2.4; AC-APP-16-06 a -12, AC-APP-15-05 a -09, AC-POL-07): los valores exactos de
    // ProjectSettings que escribe el bootstrapper v7 (identity y player.window), leídos del YAML versionado (dotnet, sin Unity). Lo que
    // no cambia se verifica igual; cualquier otra deriva la revisa QA en el diff.
    [TestFixture]
    public sealed class ProjectSettingsIt14Tests
    {
        private const string Settings = "ProjectSettings/ProjectSettings.asset";
        private const string Identity = "Assets/RowMate/Art/Identity";

        private static string[] Lines => File.ReadAllLines(RepoPaths.Combine(Settings));

        // "  clave: valor" del nivel de PlayerSettings (dos espacios de sangría).
        private static string Value(string key)
        {
            var prefix = "  " + key + ":";
            foreach (var line in Lines)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal) && (line.Length == prefix.Length || line[prefix.Length] == ' '))
                {
                    return line.Substring(prefix.Length).Trim();
                }
            }

            throw new AssertionException("ProjectSettings sin " + key);
        }

        // Una subclave de un mapa ("  applicationIdentifier:" → "    Android: …").
        private static string MapValue(string map, string key)
        {
            var lines = Lines;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "  " + map + ":")
                {
                    continue;
                }

                for (var j = i + 1; j < lines.Length && lines[j].StartsWith("    ", StringComparison.Ordinal); j++)
                {
                    var t = lines[j].Trim();
                    if (t.StartsWith(key + ":", StringComparison.Ordinal))
                    {
                        return t.Substring(key.Length + 1).Trim();
                    }
                }
            }

            throw new AssertionException("ProjectSettings sin " + map + "." + key);
        }

        private static string Guid(string assetPath)
        {
            var meta = File.ReadAllLines(RepoPaths.Combine(assetPath + ".meta"));
            var line = meta.First(l => l.StartsWith("guid: ", StringComparison.Ordinal));
            return line.Substring(6).Trim();
        }

        // El bloque de texto de una clave de lista (hasta la próxima clave del mismo nivel).
        private static string Block(string key)
        {
            var lines = Lines;
            var start = Array.IndexOf(lines, "  " + key + ":");
            Assert.That(start, Is.GreaterThanOrEqualTo(0), key);
            var end = start + 1;
            while (end < lines.Length && (lines[end].StartsWith("  - ", StringComparison.Ordinal) || lines[end].StartsWith("    ", StringComparison.Ordinal)))
            {
                end++;
            }

            return string.Join("\n", lines, start, end - start);
        }

        [Test, Category("AC-APP-16-06")]
        public void NameAndVersion()
        {
            Assert.That(Value("productName"), Is.EqualTo("RowMate"));
            Assert.That(Value("companyName"), Is.EqualTo("iguisoft"));
            Assert.That(Value("bundleVersion"), Is.EqualTo("1.0.0"));
            Assert.That(Value("AndroidBundleVersionCode"), Is.EqualTo("1"));
            Assert.That(MapValue("buildNumber", "iPhone"), Is.EqualTo("1"));
            Assert.That(MapValue("buildNumber", "Standalone"), Is.EqualTo("1"));
        }

        [Test, Category("AC-APP-16-07")]
        public void Identifiers()
        {
            Assert.That(MapValue("applicationIdentifier", "Android"), Is.EqualTo("com.iguisoft.rowmate"));
            Assert.That(MapValue("applicationIdentifier", "iPhone"), Is.EqualTo("com.iguisoft.rowmate.unity"));
            Assert.That(MapValue("applicationIdentifier", "Standalone"), Is.EqualTo("com.iguisoft.rowmate.unity"));
            Assert.That(File.ReadAllText(RepoPaths.Combine(Settings)), Does.Not.Contain("com.example"));
        }

        [Test, Category("AC-APP-16-08")]
        public void StandaloneIconsForEverySize()
        {
            var block = Block("m_BuildTargetIcons");
            Assert.That(block, Does.Contain("- m_BuildTarget: Standalone"));
            foreach (var size in new[] { 1024, 512, 256, 128, 64, 48, 32, 16 })
            {
                var guid = Guid(Identity + "/Standalone/icon-" + size + ".png");
                Assert.That(block, Does.Match(@"m_Icon: \{fileID: 2800000, guid: " + guid + @", type: 3\}\s+m_Width: " + size + @"\s+m_Height: " + size),
                    "icon-" + size);
            }
        }

        [Test, Category("AC-APP-16-09")]
        public void AndroidPlatformIcons()
        {
            var block = Block("m_BuildTargetPlatformIcons");
            Assert.That(block, Does.Contain("- m_BuildTarget: Android"));
            foreach (var size in new[] { 432, 324, 216, 162, 108, 81 })
            {
                var bg = Guid(Identity + "/Android/adaptive-bg-" + size + ".png");
                var fg = Guid(Identity + "/Android/adaptive-fg-" + size + ".png");
                Assert.That(block, Does.Match(@"guid: " + bg + @", type: 3\}\s+- \{fileID: 2800000, guid: " + fg + @", type: 3\}\s+m_Width: " + size
                    + @"\s+m_Height: " + size + @"\s+m_Kind: 2"), "adaptativo " + size + " (fondo, frente)");
            }

            foreach (var size in new[] { 192, 144, 96, 72, 48, 36 })
            {
                var round = Guid(Identity + "/Android/round-" + size + ".png");
                var legacy = Guid(Identity + "/Android/legacy-" + size + ".png");
                Assert.That(block, Does.Match(@"guid: " + round + @", type: 3\}\s+m_Width: " + size + @"\s+m_Height: " + size + @"\s+m_Kind: 1"), "redondo " + size);
                Assert.That(block, Does.Match(@"guid: " + legacy + @", type: 3\}\s+m_Width: " + size + @"\s+m_Height: " + size + @"\s+m_Kind: 0"), "legacy " + size);
            }

            Assert.That(Regex.Matches(block, @"m_Kind: 2").Count, Is.EqualTo(6));
            Assert.That(Regex.Matches(block, @"m_Kind: 1").Count, Is.EqualTo(6));
            Assert.That(Regex.Matches(block, @"m_Kind: 0").Count, Is.EqualTo(6));
        }

        [Test, Category("AC-APP-16-11"), Category("AC-APP-16-10")]
        public void SplashAndLaunchScreen()
        {
            Assert.That(Value("androidSplashScreen"), Is.EqualTo("{fileID: 2800000, guid: " + Guid(Identity + "/Android/splash-mark.png") + ", type: 3}"));
            Assert.That(Value("AndroidSplashScreenScale"), Is.EqualTo("0"), "centrar");
            Assert.That(Value("iOSLaunchScreenType"), Is.EqualTo("5"), "CustomStoryboard");
            Assert.That(Value("iOSLaunchScreeniPadType"), Is.EqualTo("5"), "CustomStoryboard");
            Assert.That(Value("iOSLaunchScreenCustomStoryboardPath"), Is.EqualTo(Identity + "/Apple/LaunchScreen.storyboard"));
            Assert.That(Value("iOSLaunchScreeniPadCustomStoryboardPath"), Is.EqualTo(Identity + "/Apple/LaunchScreen.storyboard"));
            Assert.That(Value("m_ShowUnitySplashScreen"), Is.EqualTo("0"));
            Assert.That(Value("m_SplashScreenBackgroundColor"), Is.EqualTo("{r: 0.050980393, g: 0.105882354, b: 0.16470589, a: 1}"), "bg.app #0D1B2A");

            var storyboard = File.ReadAllText(RepoPaths.Combine(Identity + "/Apple/LaunchScreen.storyboard"));
            Assert.That(storyboard, Does.Contain("launchScreen=\"YES\""));
            Assert.That(storyboard, Does.Contain("image=\"RowMateLaunchMark\""));
            Assert.That(storyboard, Does.Contain("red=\"0.050980392156862744\" green=\"0.10588235294117647\" blue=\"0.16470588235294117\""), "fondo #0D1B2A");
            Assert.That(storyboard, Does.Contain("constant=\"96\""), "marca de 96 pt");
            Assert.That(File.Exists(RepoPaths.Combine(Identity + "/Apple/AppIcon.appiconset/Contents.json")), Is.True);
            Assert.That(File.Exists(RepoPaths.Combine(Identity + "/Apple/RowMateLaunchMark.imageset/Contents.json")), Is.True);
        }

        [Test, Category("AC-APP-15-05"), Category("AC-APP-15-07")]
        public void WindowsWindow()
        {
            Assert.That(Value("allowFullscreenSwitch"), Is.EqualTo("0"), "F11 / Alt+Enter los maneja WindowModeController (DT14-15)");
            Assert.That(Value("defaultScreenWidth"), Is.EqualTo("1280"));
            Assert.That(Value("defaultScreenHeight"), Is.EqualTo("720"));
            Assert.That(Value("resizableWindow"), Is.EqualTo("1"));
            Assert.That(Value("fullscreenMode"), Is.EqualTo("3"), "Windowed");
        }

        [Test, Category("AC-APP-15-09")]
        public void Orientations()
        {
            foreach (var key in new[] { "allowedAutorotateToPortrait", "allowedAutorotateToPortraitUpsideDown", "allowedAutorotateToLandscapeRight",
                "allowedAutorotateToLandscapeLeft" })
            {
                Assert.That(Value(key), Is.EqualTo("1"), key);
            }

            Assert.That(Value("defaultScreenOrientation"), Is.EqualTo("4"), "automática");
            Assert.That(Value("androidAutoRotationBehavior"), Is.EqualTo("1"), "respeta el bloqueo de rotación");
            Assert.That(Value("androidResizeableActivity"), Is.EqualTo("1"), "pantalla dividida");
            Assert.That(Value("uIRequiresFullScreen"), Is.EqualTo("0"), "multitarea en iPad");
        }

        [Test, Category("AC-APP-16-12")]
        public void AndroidMinimum()
        {
            Assert.That(Value("AndroidMinSdkVersion"), Is.EqualTo("26"));
            Assert.That(Value("AndroidTargetArchitectures"), Is.EqualTo("2"), "solo ARM64");
        }

        [Test, Category("AC-POL-07")]
        public void NoSimulatorDefineAndBootstrapV7()
        {
            var lines = Lines;
            var start = Array.IndexOf(lines, "  scriptingDefineSymbols:");
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            // Durante la corrida de las suites del simulador (la marca de SimulatorDefine), el define está a propósito; lo controla
            // SimulatorInUnityTests.CommittedSettingsDoNotEnableTheSimulator.
            var simulatorRun = File.Exists(RepoPaths.Combine("Library/RowMate/simulator-tests.txt"));
            for (var i = start + 1; !simulatorRun && i < lines.Length && lines[i].StartsWith("    ", StringComparison.Ordinal); i++)
            {
                Assert.That(lines[i], Does.Not.Contain("ROWMATE_SIMULATOR"));
            }

            var state = File.ReadAllText(RepoPaths.Combine("ProjectSettings/RowMateBootstrap.json"));
            Assert.That(state, Does.Contain("\"version\": 7"));
            foreach (var step in new[] { "identity.import", "identity.standalone", "identity.mobile" })
            {
                Assert.That(state, Does.Contain("\"id\": \"" + step + "\""), step);
            }
        }

        [Test, Category("AC-APP-16-08")]
        public void IdentityMetasAreVersioned()
        {
            // ProjectSettings referencia los GUID de Art/Identity: cada PNG lleva su .meta (QA-G14-16).
            var missing = new List<string>();
            foreach (var png in Directory.GetFiles(RepoPaths.Combine(Identity), "*.png", SearchOption.AllDirectories))
            {
                if (!File.Exists(png + ".meta"))
                {
                    missing.Add(RepoPaths.Relative(png));
                }
            }

            Assert.That(missing, Is.Empty);
        }
    }
}
