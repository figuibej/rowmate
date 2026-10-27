using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-10: Unity 6000.3.27f1 con URP 17.3.0 y UI Toolkit, con versiones de paquetes exactas (ADR 001, ADR 006).
    [TestFixture]
    public sealed class ManifestAndVersionTests
    {
        // ARCHITECTURE §1.2 = 02-tech-design §1.3. Un paquete nuevo obliga a tocar este test (decisión explícita, ADR 001).
        private static readonly Dictionary<string, string> ExpectedPackages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "com.gilzoide.sqlite-net", "https://github.com/gilzoide/unity-sqlite-net.git#1.3.3" },
            { "com.unity.animation.rigging", "1.4.1" },
            { "com.unity.ide.visualstudio", "2.0.28" },
            { "com.unity.inputsystem", "1.20.1" },
            { "com.unity.mobile.android-logcat", "1.4.7" },
            { "com.unity.nuget.newtonsoft-json", "3.2.2" },
            { "com.unity.render-pipelines.universal", "17.3.0" },
            { "com.unity.shadergraph", "17.3.0" },
            { "com.unity.test-framework", "1.6.0" },
            { "com.unity.ugui", "2.0.0" },
            { "com.unity.modules.androidjni", "1.0.0" },
            { "com.unity.modules.animation", "1.0.0" },
            { "com.unity.modules.audio", "1.0.0" },
            { "com.unity.modules.imgui", "1.0.0" },
            { "com.unity.modules.jsonserialize", "1.0.0" },
            { "com.unity.modules.particlesystem", "1.0.0" },
            { "com.unity.modules.physics", "1.0.0" },
            { "com.unity.modules.terrain", "1.0.0" },
            { "com.unity.modules.ui", "1.0.0" },
            { "com.unity.modules.uielements", "1.0.0" },
            { "com.unity.modules.unitywebrequest", "1.0.0" },
            { "com.unity.modules.unitywebrequesttexture", "1.0.0" },
        };

        [Test, Category("AC-BASE-10")]
        public void ManifestHasExactlyTheDecidedPackages()
        {
            var manifest = JsonParser.Parse(File.ReadAllText(RepoPaths.Combine("Packages/manifest.json")));
            Assert.That(manifest.Properties.Count, Is.EqualTo(1), "manifest.json: solo \"dependencies\" (sin scopedRegistries ni testables)");
            var actual = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var dependency in manifest["dependencies"].Properties)
            {
                actual.Add(dependency.Key, dependency.Value.AsString());
            }

            Assert.That(actual, Is.EquivalentTo(ExpectedPackages));
        }

        [Test, Category("AC-BASE-10")]
        public void VersionsAreExact()
        {
            // Sin rangos ni "latest": un número exacto o un tag de git.
            foreach (var package in ExpectedPackages)
            {
                Assert.That(package.Value, Does.Match(@"^(\d+\.\d+\.\d+|https://.+\.git#\d+\.\d+\.\d+)$"), package.Key);
            }

            Assert.That(ExpectedPackages["com.unity.render-pipelines.universal"], Is.EqualTo("17.3.0"));
            Assert.That(ExpectedPackages["com.unity.modules.uielements"], Is.EqualTo("1.0.0"));
        }

        [Test, Category("AC-BASE-10")]
        public void ProjectVersionIs6000_3_27f1()
        {
            var lines = File.ReadAllText(RepoPaths.Combine("ProjectSettings/ProjectVersion.txt")).Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            Assert.That(lines[0], Is.EqualTo("m_EditorVersion: 6000.3.27f1"));
            for (var i = 1; i < lines.Length; i++)
            {
                // Unity agrega esta línea al abrir el proyecto.
                Assert.That(lines[i], Does.StartWith("m_EditorVersionWithRevision: 6000.3.27f1 ("), "línea " + (i + 1));
            }

            Assert.That(lines.Length, Is.LessThanOrEqualTo(2));
        }

        [Test, Category("AC-BASE-10")]
        public void CscRspEnablesNullableLikeDotnet()
        {
            // Mismo contexto de nulabilidad que tools/dotnet. R-T12 (primer open, It. 2): Unity lo aplica a todos los asmdefs
            // (Library/Bee/…/RowMate.*.rsp lleva -nullable:enable y -langversion:9.0).
            Assert.That(File.ReadAllText(RepoPaths.Combine("Assets/csc.rsp")).Trim(), Is.EqualTo("-nullable:enable"));
        }

        // Dentro de Unity (AC-BASE-08): -runTests no corre ningún test si hay errores de compilación, y además se comprueba
        // que están cargados todos los assemblies de RowMate (también los de Editor, Scene y Bootstrap, que dotnet no
        // compila). Sin API del motor: Tests/EditMode es código puro (PureCodeRulesTests.NoEngineInPureCode). Los dos del
        // simulador solo existen con ROWMATE_SIMULATOR (tools/unity/run-unity-tests.ps1 -Simulator).
        // En dotnet no hay Editor: el log del open (sin error CS y con código 0) queda en 00-primer-open.md de la It. 2.
        [Test, Category("AC-BASE-10")]
#if !UNITY_EDITOR
        [Ignore("NO VERIFICABLE en dotnet: corre dentro de Unity 6000.3.27f1 (tools/unity/run-unity-tests.ps1, AC-BASE-08)")]
#endif
        public void ProjectOpensWithoutCompileErrors()
        {
            var loaded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                loaded.Add(assembly.GetName().Name ?? "");
            }

            var expected = new List<string>
            {
                "RowMate.Core", "RowMate.App", "RowMate.Infrastructure", "RowMate.Platform", "RowMate.Presentation", "RowMate.Scene",
                "RowMate.Bootstrap", "RowMate.Editor", "RowMate.Tests.Support", "RowMate.Tests.EditMode", "RowMate.Tests.PlayMode",
            };
            if (loaded.Contains("RowMate.Tests.Simulator"))
            {
                expected.AddRange(new[] { "RowMate.Simulator", "RowMate.Simulator.Presentation" });
            }

            foreach (var name in expected)
            {
                Assert.That(loaded, Does.Contain(name), "assembly sin cargar");
            }
        }
    }
}
