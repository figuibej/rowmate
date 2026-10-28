using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-10 (QA-P-25): revisión estática de los 14 asmdefs, que no se pueden compilar sin Editor (ARCHITECTURE §3, §15.1;
    // 02-tech-design §1.2 y §2.1). AC-BASE-08: los puros no tienen referencias al motor.
    [TestFixture]
    public sealed class AsmdefLintTests
    {
        // Carpeta (relativa a Assets/RowMate) → nombre del asmdef.
        private static readonly Dictionary<string, string> Expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Core", "RowMate.Core" },
            { "App", "RowMate.App" },
            { "Infrastructure", "RowMate.Infrastructure" },
            { "Simulator", "RowMate.Simulator" },
            { "Platform", "RowMate.Platform" },
            { "Presentation", "RowMate.Presentation" },
            { "Scene", "RowMate.Scene" },
            { "SimulatorPresentation", "RowMate.Simulator.Presentation" },
            { "Bootstrap", "RowMate.Bootstrap" },
            { "Editor", "RowMate.Editor" },
            { "Tests/Support", "RowMate.Tests.Support" },
            { "Tests/EditMode", "RowMate.Tests.EditMode" },
            { "Tests/Simulator", "RowMate.Tests.Simulator" },
            { "Tests/PlayMode", "RowMate.Tests.PlayMode" },
            // It. 2 (WP-B, 02-tech-design §1.2): solo de Unity (sin .csproj en tools/dotnet).
            { "Tests/PlayModeSimulator", "RowMate.Tests.PlayMode.Simulator" },
            { "Tests/UnityEditMode", "RowMate.Tests.UnityEditMode" },
            { "Tests/UiSupport", "RowMate.Tests.UiSupport" },
            { "Snapshots", "RowMate.Snapshots" },
        };

        // Assemblies de paquetes de Unity que se referencian por nombre (resueltos en el primer open real, It. 2).
        private static readonly HashSet<string> UnityPackageAssemblies = new HashSet<string>(StringComparer.Ordinal)
        {
            "Unity.InputSystem",
            "Unity.RenderPipelines.Core.Runtime",
            "Unity.RenderPipelines.Universal.Runtime",
            "Unity.RenderPipelines.Universal.Editor",
            "Unity.Animation.Rigging",
            "UnityEngine.TestRunner",
            "UnityEditor.TestRunner",
        };

        // Paquetes de terceros que se referencian por nombre (It. 3, 02-tech-design §1.2): solo Infrastructure, y con su par
        // de NuGet en tools/dotnet (Gilzoide.SqliteNet ↔ sqlite-net-pcl, el mismo SQLite.cs de sqlite-net).
        private static readonly Dictionary<string, string> PackageAssemblies = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Gilzoide.SqliteNet", "Infrastructure" },
        };

        // Orden de claves que escribe Unity.
        private static readonly string[] KeyOrder =
        {
            "name", "rootNamespace", "references", "includePlatforms", "excludePlatforms", "allowUnsafeCode",
            "overrideReferences", "precompiledReferences", "autoReferenced", "defineConstraints", "versionDefines",
            "noEngineReferences",
        };

        private static readonly string[] PureAssemblies = { "RowMate.Core", "RowMate.App", "RowMate.Simulator", "RowMate.Tests.Support" };

        // Con [Test] y el NUnit de Unity.
        private static readonly string[] TestAssemblies =
        {
            "RowMate.Tests.Support", "RowMate.Tests.EditMode", "RowMate.Tests.Simulator", "RowMate.Tests.PlayMode",
            "RowMate.Tests.PlayMode.Simulator", "RowMate.Tests.UnityEditMode",
        };

        // Solo con el test framework (UNITY_INCLUDE_TESTS), sin [Test] ni nunit.framework.dll: el host de UI y el arnés.
        private static readonly string[] TestOnlyAssemblies = { "RowMate.Tests.UiSupport", "RowMate.Snapshots" };

        private static readonly string[] EditorOnlyAssemblies =
        {
            "RowMate.Editor", "RowMate.Tests.EditMode", "RowMate.Tests.Simulator", "RowMate.Tests.UnityEditMode", "RowMate.Snapshots",
        };

        private static readonly string[] SimulatorOnlyAssemblies =
        {
            "RowMate.Simulator", "RowMate.Simulator.Presentation", "RowMate.Tests.Simulator", "RowMate.Tests.PlayMode.Simulator",
        };

        // Proyectos de tools/dotnet que espejan un asmdef (propiedad RowMateAsmdef) y las dos herramientas.
        private static readonly string[] MirroredAssemblies =
        {
            "RowMate.Core", "RowMate.App", "RowMate.Infrastructure", "RowMate.Simulator",
            "RowMate.Tests.Support", "RowMate.Tests.EditMode", "RowMate.Tests.Simulator",
        };

        private static readonly string[] SolutionProjects =
        {
            "RowMate.Core", "RowMate.App", "RowMate.Infrastructure", "RowMate.Simulator", "RowMate.Tests.Support",
            "RowMate.Tests.EditMode", "RowMate.Tests.Simulator", "RowMate.Tests.UnityApiCheck", "RowMate.Tokens",
        };

        [Test, Category("AC-BASE-10")]
        public void AllAsmdefsAreValidJsonWithUnityKeyOrder()
        {
            var files = RepoPaths.Files("Assets", "*.asmdef");
            Assert.That(files.Count, Is.EqualTo(18), "asmdefs: " + string.Join(", ", files.Select(RepoPaths.Relative)));
            foreach (var file in files)
            {
                var json = JsonParser.Parse(File.ReadAllText(file));
                var keys = json.Properties.Select(p => p.Key).ToArray();
                Assert.That(keys, Is.EqualTo(KeyOrder), RepoPaths.Relative(file));
            }
        }

        [Test, Category("AC-BASE-10")]
        public void EachFolderHasItsAsmdefAndAssemblyInfo()
        {
            foreach (var entry in Expected)
            {
                var asmdef = Asmdef(entry.Key);
                Assert.That(asmdef["name"].AsString(), Is.EqualTo(entry.Value), entry.Key);
                Assert.That(asmdef["rootNamespace"].AsString(), Is.EqualTo(RootNamespace(entry.Value)), entry.Key);
                Assert.That(File.Exists(RepoPaths.Combine("Assets/RowMate", entry.Key, "AssemblyInfo.cs")), Is.True,
                    entry.Key + "/AssemblyInfo.cs (Unity no compila un asmdef sin scripts)");
            }
        }

        [Test, Category("AC-BASE-10")]
        public void CommonSettings()
        {
            foreach (var entry in Expected)
            {
                var asmdef = Asmdef(entry.Key);
                Assert.That(asmdef["autoReferenced"].AsBool(), Is.False, entry.Value + ": autoReferenced");
                Assert.That(asmdef["allowUnsafeCode"].AsBool(), Is.False, entry.Value + ": allowUnsafeCode");
                Assert.That(asmdef["versionDefines"].Items, Is.Empty, entry.Value + ": versionDefines");
                Assert.That(asmdef["excludePlatforms"].Items, Is.Empty, entry.Value + ": excludePlatforms");
            }
        }

        [Test, Category("AC-BASE-10")]
        public void ReferencesExistAndAreByName()
        {
            var ours = new HashSet<string>(Expected.Values, StringComparer.Ordinal);
            foreach (var entry in Expected)
            {
                foreach (var reference in References(entry.Key))
                {
                    Assert.That(reference.StartsWith("GUID:", StringComparison.Ordinal), Is.False, entry.Value + " referencia por GUID: " + reference);
                    Assert.That(ours.Contains(reference) || UnityPackageAssemblies.Contains(reference) || PackageAssemblies.ContainsKey(reference), Is.True,
                        entry.Value + " referencia un assembly desconocido: " + reference);
                    Assert.That(reference, Is.Not.EqualTo(entry.Value), entry.Value + " se referencia a sí mismo");
                }
            }
        }

        [Test, Category("AC-BASE-10"), Category("AC-BASE-08")]
        public void PureAssembliesHaveNoEngineReferences()
        {
            foreach (var entry in Expected)
            {
                var asmdef = Asmdef(entry.Key);
                var pure = PureAssemblies.Contains(entry.Value);
                Assert.That(asmdef["noEngineReferences"].AsBool(), Is.EqualTo(pure), entry.Value + ": noEngineReferences");
                if (pure)
                {
                    foreach (var reference in References(entry.Key))
                    {
                        Assert.That(UnityPackageAssemblies.Contains(reference), Is.False, entry.Value + " es puro y referencia " + reference);
                    }
                }
            }
        }

        [Test, Category("AC-BASE-10")]
        public void DefineConstraintsAndPlatforms()
        {
            foreach (var entry in Expected)
            {
                var asmdef = Asmdef(entry.Key);
                var defines = asmdef["defineConstraints"].Items.Select(i => i.AsString()).ToArray();
                var platforms = asmdef["includePlatforms"].Items.Select(i => i.AsString()).ToArray();
                var expectedDefines = new List<string>();
                if (TestAssemblies.Contains(entry.Value) || TestOnlyAssemblies.Contains(entry.Value))
                {
                    expectedDefines.Add("UNITY_INCLUDE_TESTS");
                }

                if (SimulatorOnlyAssemblies.Contains(entry.Value))
                {
                    expectedDefines.Add("ROWMATE_SIMULATOR");
                }

                Assert.That(defines, Is.EquivalentTo(expectedDefines), entry.Value + ": defineConstraints");
                var editorOnly = EditorOnlyAssemblies.Contains(entry.Value);
                Assert.That(platforms, Is.EqualTo(editorOnly ? new[] { "Editor" } : new string[0]), entry.Value + ": includePlatforms");
            }
        }

        [Test, Category("AC-BASE-10")]
        public void TestAssembliesUseUnityNUnit()
        {
            foreach (var entry in Expected)
            {
                if (!TestAssemblies.Contains(entry.Value))
                {
                    continue;
                }

                var asmdef = Asmdef(entry.Key);
                Assert.That(asmdef["overrideReferences"].AsBool(), Is.True, entry.Value);
                Assert.That(asmdef["precompiledReferences"].Items.Select(i => i.AsString()).ToArray(), Is.EqualTo(new[] { "nunit.framework.dll" }), entry.Value);
                var references = References(entry.Key);
                if (entry.Value != "RowMate.Tests.Support")
                {
                    Assert.That(references, Does.Contain("RowMate.Tests.Support"), entry.Value);
                    Assert.That(references, Does.Contain("UnityEngine.TestRunner"), entry.Value);
                }
            }

            // Support no referencia el simulador: sin el define ese assembly no existe (ARCHITECTURE §3.1).
            Assert.That(References("Tests/Support"), Does.Not.Contain("RowMate.Simulator"));
            // PlayMode solo UnityEngine.TestRunner, para poder entrar en un player de test.
            Assert.That(References("Tests/PlayMode"), Does.Not.Contain("UnityEditor.TestRunner"));
            Assert.That(References("Tests/PlayModeSimulator"), Does.Not.Contain("UnityEditor.TestRunner"));
            Assert.That(References("Tests/UnityEditMode"), Does.Contain("UnityEditor.TestRunner"));
        }

        // It. 2 (02-tech-design §1.2): el host de UI y el arnés no son assemblies de prueba (sin [Test] ni NUnit) pero solo
        // existen con el test framework; UiSupport y Snapshots referencian RowMate.Simulator* por nombre (sin el define, Unity
        // descarta la referencia) y lo usan solo dentro de #if ROWMATE_SIMULATOR.
        [Test, Category("AC-BASE-10")]
        public void UiSupportAndSnapshotsAreTestOnlyWithoutNUnit()
        {
            foreach (var folder in new[] { "Tests/UiSupport", "Snapshots" })
            {
                var asmdef = Asmdef(folder);
                Assert.That(asmdef["overrideReferences"].AsBool(), Is.False, folder);
                Assert.That(asmdef["precompiledReferences"].Items, Is.Empty, folder + ": sin nunit.framework.dll");
                var references = References(folder);
                Assert.That(references, Does.Not.Contain("UnityEngine.TestRunner"), folder);
                Assert.That(references, Does.Not.Contain("UnityEditor.TestRunner"), folder);
                Assert.That(references, Does.Contain("RowMate.Simulator"), folder);
                Assert.That(references, Does.Contain("RowMate.Simulator.Presentation"), folder);
                foreach (var file in RepoPaths.Files("Assets/RowMate/" + folder, "*.cs"))
                {
                    var code = SourceScan.StripCommentsAndLiterals(File.ReadAllText(file));
                    Assert.That(code, Does.Not.Match(@"\[\s*(Test|TestCase|UnityTest|TestFixture)\b"), RepoPaths.Relative(file) + ": sin pruebas");
                    Assert.That(code, Does.Not.Contain("NUnit"), RepoPaths.Relative(file) + ": sin NUnit");
                    if (code.Contains("RowMate.Simulator"))
                    {
                        Assert.That(File.ReadAllText(file), Does.Contain("#if ROWMATE_SIMULATOR"), RepoPaths.Relative(file) + ": el simulador solo con el define");
                    }
                }
            }

            Assert.That(References("Tests/UiSupport"), Does.Contain("RowMate.Bootstrap"), "UiSupport arma la UI con ShellHost");
            Assert.That(References("Snapshots"), Does.Contain("RowMate.Tests.UiSupport"), "el arnés usa el mismo host que PlayMode");
        }

        [Test, Category("AC-BASE-10"), Category("AC-BLE-13-04")]
        public void DependencyRule()
        {
            // ARCHITECTURE §3.2: las flechas apuntan hacia el Core.
            Assert.That(References("Core"), Is.Empty, "Core no referencia nada");
            Assert.That(References("App"), Is.EquivalentTo(new[] { "RowMate.Core" }));
            Assert.That(References("Infrastructure"), Is.EquivalentTo(new[] { "RowMate.Core", "Gilzoide.SqliteNet" }), "It. 3: sqlite-net (§1.2)");
            Assert.That(References("Simulator"), Is.EquivalentTo(new[] { "RowMate.Core", "RowMate.App" }));
            foreach (var forbidden in new[] { "RowMate.Scene", "RowMate.Platform", "RowMate.Infrastructure" })
            {
                Assert.That(References("Presentation"), Does.Not.Contain(forbidden), "Presentation → " + forbidden);
            }

            foreach (var forbidden in new[] { "RowMate.Presentation", "RowMate.Platform", "RowMate.Infrastructure" })
            {
                Assert.That(References("Scene"), Does.Not.Contain(forbidden), "Scene → " + forbidden);
            }

            // AC-BLE-13-04: ninguna pantalla sabe del simulador (Presentation no lo referencia).
            Assert.That(References("Presentation"), Does.Not.Contain("RowMate.Simulator"));
            Assert.That(References("Presentation"), Does.Not.Contain("RowMate.Simulator.Presentation"));

            var mayUseSimulator = new[]
            {
                "RowMate.Bootstrap", "RowMate.Editor", "RowMate.Simulator.Presentation", "RowMate.Tests.Simulator",
                "RowMate.Tests.PlayMode.Simulator", "RowMate.Tests.UiSupport", "RowMate.Snapshots",
            };
            var mayUseBootstrap = new[]
            {
                "RowMate.Editor", "RowMate.Tests.PlayMode", "RowMate.Tests.PlayMode.Simulator", "RowMate.Tests.UiSupport",
                "RowMate.Tests.UnityEditMode", "RowMate.Snapshots",
            };
            var mayUsePlatform = new[] { "RowMate.Bootstrap", "RowMate.Editor", "RowMate.Tests.UnityEditMode" };
            var mayUseEditor = new[] { "RowMate.Tests.UnityEditMode" };
            foreach (var entry in Expected)
            {
                var references = References(entry.Key);
                if (references.Contains("RowMate.Simulator") || references.Contains("RowMate.Simulator.Presentation"))
                {
                    Assert.That(mayUseSimulator, Does.Contain(entry.Value), entry.Value + " no puede referenciar RowMate.Simulator*");
                }

                if (references.Contains("RowMate.Bootstrap"))
                {
                    Assert.That(mayUseBootstrap, Does.Contain(entry.Value), entry.Value + " no puede referenciar RowMate.Bootstrap");
                }

                if (references.Contains("RowMate.Platform"))
                {
                    Assert.That(mayUsePlatform, Does.Contain(entry.Value), entry.Value + " no puede referenciar RowMate.Platform");
                }

                if (references.Contains("RowMate.Editor"))
                {
                    Assert.That(mayUseEditor, Does.Contain(entry.Value), entry.Value + " no puede referenciar RowMate.Editor");
                }

                foreach (var reference in references)
                {
                    if (reference.StartsWith("RowMate.Tests.", StringComparison.Ordinal))
                    {
                        Assert.That(TestAssemblies.Contains(entry.Value) || TestOnlyAssemblies.Contains(entry.Value), Is.True,
                            entry.Value + " no es de test y referencia " + reference);
                    }

                    if (reference == "RowMate.Snapshots")
                    {
                        Assert.Fail(entry.Value + " referencia el arnés de capturas");
                    }
                }
            }

            AssertAcyclic();
        }

        // It. 3 (02-tech-design §1.2): solo Infrastructure nombra Gilzoide.SqliteNet, y su .csproj espejo trae el par de NuGet
        // fijado (sqlite-net-pcl 1.9.172 + SQLitePCLRaw.bundle_green 2.1.11); ningún otro proyecto lo pide directo.
        [Test, Category("AC-BASE-10"), Category("AC-HIS-17-02")]
        public void OnlyInfrastructureNamesTheSqlitePackage()
        {
            foreach (var entry in Expected)
            {
                foreach (var package in PackageAssemblies)
                {
                    var names = References(entry.Key).Contains(package.Key);
                    Assert.That(names, Is.EqualTo(entry.Key == package.Value), entry.Value + " y " + package.Key);
                }
            }

            var infrastructure = RepoPaths.Combine("tools/dotnet/RowMate.Infrastructure/RowMate.Infrastructure.csproj");
            Assert.That(PackageVersion(infrastructure, "sqlite-net-pcl"), Is.EqualTo("1.9.172"));
            Assert.That(PackageVersion(infrastructure, "SQLitePCLRaw.bundle_green"), Is.EqualTo("2.1.11"), "fijado: SQLite 3.49.1 (DT-16)");
            foreach (var project in Projects())
            {
                if (RepoPaths.Relative(project) == "tools/dotnet/RowMate.Infrastructure/RowMate.Infrastructure.csproj")
                {
                    continue;
                }

                Assert.That(PackageVersion(project, "sqlite-net-pcl"), Is.Null, RepoPaths.Relative(project));
                Assert.That(PackageVersion(project, "SQLitePCLRaw.bundle_green"), Is.Null, RepoPaths.Relative(project));
            }
        }

        [Test, Category("AC-BASE-10")]
        public void GraphMatchesTheDotnetProjects()
        {
            // QA-P-03: un .csproj por asmdef puro, con las mismas referencias (así dotnet falla donde fallaría Unity).
            var mirrored = new HashSet<string>(StringComparer.Ordinal);
            foreach (var project in Projects())
            {
                var asmdefRelative = Property(project, "RowMateAsmdef");
                if (asmdefRelative == null)
                {
                    continue;
                }

                var assemblyName = Property(project, "AssemblyName");
                var folder = asmdefRelative.Substring(0, asmdefRelative.LastIndexOf('/'));
                Assert.That(Expected.ContainsKey(folder), Is.True, RepoPaths.Relative(project) + ": RowMateAsmdef " + asmdefRelative);
                Assert.That(asmdefRelative, Is.EqualTo(folder + "/" + Expected[folder] + ".asmdef"), RepoPaths.Relative(project));
                Assert.That(assemblyName, Is.EqualTo(Expected[folder]), RepoPaths.Relative(project) + ": AssemblyName");
                mirrored.Add(Expected[folder]);

                var ourReferences = References(folder).Where(r => r.StartsWith("RowMate.", StringComparison.Ordinal)).ToArray();
                Assert.That(ProjectReferenceNames(project), Is.EquivalentTo(ourReferences), RepoPaths.Relative(project) + ": ProjectReference = referencias del asmdef");

                var includes = CompileIncludes(project);
                Assert.That(includes, Is.EqualTo(new[] { "$(UnityAssets)" + folder + "/**/*.cs" }), RepoPaths.Relative(project) + ": Compile Include");

                var defines = Property(project, "DefineConstants") ?? "";
                foreach (var constraint in Asmdef(folder)["defineConstraints"].Items.Select(i => i.AsString()))
                {
                    if (constraint != "UNITY_INCLUDE_TESTS")
                    {
                        Assert.That(defines, Does.Contain(constraint), RepoPaths.Relative(project) + ": DefineConstants");
                    }
                }

                var framework = Property(project, "TargetFramework");
                var isTestRunner = assemblyName == "RowMate.Tests.EditMode" || assemblyName == "RowMate.Tests.Simulator";
                Assert.That(framework, Is.EqualTo(isTestRunner ? "net8.0" : "netstandard2.1"), RepoPaths.Relative(project) + ": TargetFramework");
            }

            Assert.That(mirrored, Is.EquivalentTo(MirroredAssemblies));
        }

        [Test, Category("AC-BASE-10"), Category("AC-BASE-08")]
        public void UnityApiCheckCompilesTestSourcesAgainstNUnit35()
        {
            // QA-10: Support por glob (no por ProjectReference), NUnit 3.5.0 y netstandard2.1.
            var project = RepoPaths.Combine("tools/dotnet/RowMate.Tests.UnityApiCheck/RowMate.Tests.UnityApiCheck.csproj");
            Assert.That(Property(project, "TargetFramework"), Is.EqualTo("netstandard2.1"));
            Assert.That(Property(project, "RowMateAsmdef"), Is.Null, "es una herramienta, no espeja un asmdef");
            Assert.That(CompileIncludes(project), Is.EquivalentTo(new[]
            {
                "$(UnityAssets)Tests/Support/**/*.cs", "$(UnityAssets)Tests/EditMode/**/*.cs", "$(UnityAssets)Tests/Simulator/**/*.cs",
            }));
            Assert.That(PackageVersion(project, "NUnit"), Is.EqualTo("3.5.0"));
            Assert.That(ProjectReferenceNames(project), Is.EquivalentTo(new[] { "RowMate.Core", "RowMate.App", "RowMate.Infrastructure", "RowMate.Simulator" }));
            Assert.That(Property(project, "DefineConstants"), Does.Contain("ROWMATE_SIMULATOR"));
        }

        [Test, Category("AC-BASE-10")]
        public void SolutionContainsTheNineProjects()
        {
            var solution = File.ReadAllText(RepoPaths.Combine("tools/dotnet/RowMate.sln")).Replace('\\', '/');
            foreach (var name in SolutionProjects)
            {
                Assert.That(solution, Does.Contain(name + "/" + name + ".csproj"), "RowMate.sln: falta " + name);
                Assert.That(File.Exists(RepoPaths.Combine("tools/dotnet", name, name + ".csproj")), Is.True, name);
            }

            Assert.That(Projects().Count, Is.EqualTo(SolutionProjects.Length), "tools/dotnet tiene un .csproj de más o de menos");
        }

        [Test, Category("AC-BASE-10")]
        public void NoScriptOutsideAnAssemblyFolder()
        {
            // Ningún .cs de Assets/ cae en Assembly-CSharp (03-qa-harmony §8).
            var folders = Expected.Keys.Select(k => "Assets/RowMate/" + k + "/").ToArray();
            foreach (var file in RepoPaths.Files("Assets", "*.cs"))
            {
                var relative = RepoPaths.Relative(file);
                if (relative.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(folders.Any(f => relative.StartsWith(f, StringComparison.Ordinal)), Is.True, relative + " no está bajo ningún asmdef");
            }
        }

        // QA-V-04 de la It. 1 (ARCHITECTURE §17): ningún namespace propio tiene un segmento llamado como un tipo o un
        // namespace de Unity que se usa sin calificar. `namespace RowMate.Editor` hace que, dentro de RowMate.*,
        // `Editor` resuelva al namespace antes que a UnityEditor.Editor (`class X : Editor` → CS0118). Lo mismo con
        // UnityEngine/UnityEditor como segmento.
        [Test, Category("AC-BASE-10"), Category("QA-V-04")]
        public void NoNamespaceShadowsUnityNames()
        {
            var forbidden = new HashSet<string>(StringComparer.Ordinal) { "Editor", "UnityEditor", "UnityEngine" };
            var declaration = new System.Text.RegularExpressions.Regex(@"\bnamespace\s+([A-Za-z_][\w.]*)");
            var offenders = new List<string>();
            var declared = 0;
            foreach (var file in RepoPaths.Files("Assets/RowMate", "*.cs"))
            {
                var code = SourceScan.StripCommentsAndLiterals(File.ReadAllText(file));
                foreach (System.Text.RegularExpressions.Match match in declaration.Matches(code))
                {
                    declared++;
                    foreach (var segment in match.Groups[1].Value.Split('.'))
                    {
                        if (forbidden.Contains(segment))
                        {
                            offenders.Add(RepoPaths.Relative(file) + ": namespace " + match.Groups[1].Value);
                        }
                    }
                }
            }

            Assert.That(declared, Is.GreaterThan(100), "el escaneo no encontró los namespaces");
            Assert.That(offenders, Is.Empty, "namespaces que tapan nombres de Unity:\n" + string.Join("\n", offenders));
        }

        [Test, Category("AC-BASE-10"), Category("QA-V-04")]
        public void EditorAssemblyUsesTheEditorToolsNamespace()
        {
            Assert.That(Asmdef("Editor")["name"].AsString(), Is.EqualTo("RowMate.Editor"), "el nombre del assembly no cambia");
            Assert.That(Asmdef("Editor")["rootNamespace"].AsString(), Is.EqualTo("RowMate.EditorTools"));
            var files = RepoPaths.Files("Assets/RowMate/Editor", "*.cs");
            Assert.That(files.Count, Is.GreaterThanOrEqualTo(5));
            foreach (var file in files)
            {
                var code = SourceScan.StripCommentsAndLiterals(File.ReadAllText(file));
                Assert.That(code, Does.Match(@"\bnamespace\s+RowMate\.EditorTools\s*\{"), RepoPaths.Relative(file));
                Assert.That(code, Does.Not.Match(@"\bnamespace\s+RowMate\.EditorTools\."), RepoPaths.Relative(file) + ": sin subnamespaces");
            }
        }

        [Test, Category("AC-BASE-10")]
        public void SharedBuildPropertiesKeepUnityCSharp()
        {
            var props = XDocument.Load(RepoPaths.Combine("tools/dotnet/Directory.Build.props"));
            string? Get(string name) => props.Descendants(name).Select(e => e.Value).FirstOrDefault();
            Assert.That(Get("LangVersion"), Is.EqualTo("9.0"));
            Assert.That(Get("Nullable"), Is.EqualTo("enable"));
            Assert.That(Get("ImplicitUsings"), Is.EqualTo("disable"));
            Assert.That(Get("EnableDefaultCompileItems"), Is.EqualTo("false"));
            Assert.That(Get("InvariantGlobalization"), Is.Null, "rompería los tests de cultura de AC-BASE-02");
            foreach (var pure in new[] { "RowMate.Core", "RowMate.App", "RowMate.Infrastructure", "RowMate.Simulator" })
            {
                Assert.That(Property(RepoPaths.Combine("tools/dotnet", pure, pure + ".csproj"), "TreatWarningsAsErrors"), Is.EqualTo("true"), pure);
            }
        }

        private static void AssertAcyclic()
        {
            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            var byName = Expected.ToDictionary(e => e.Value, e => e.Key, StringComparer.Ordinal);

            void Visit(string name, string path)
            {
                if (!byName.ContainsKey(name))
                {
                    return;
                }

                state.TryGetValue(name, out var current);
                if (current == 2)
                {
                    return;
                }

                Assert.That(current, Is.Not.EqualTo(1), "ciclo de asmdefs: " + path + " → " + name);
                state[name] = 1;
                foreach (var reference in References(byName[name]))
                {
                    Visit(reference, path + " → " + name);
                }

                state[name] = 2;
            }

            foreach (var name in byName.Keys)
            {
                Visit(name, "");
            }
        }

        // rootNamespace = nombre del asmdef, salvo RowMate.Editor (QA-V-04: su namespace es RowMate.EditorTools).
        private static string RootNamespace(string assembly)
        {
            return assembly == "RowMate.Editor" ? "RowMate.EditorTools" : assembly;
        }

        private static JsonNode Asmdef(string folder)
        {
            var path = RepoPaths.Combine("Assets/RowMate", folder, Expected[folder] + ".asmdef");
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            return JsonParser.Parse(File.ReadAllText(path));
        }

        private static List<string> References(string folder)
        {
            return Asmdef(folder)["references"].Items.Select(i => i.AsString()).ToList();
        }

        private static List<string> Projects()
        {
            return RepoPaths.Files("tools/dotnet", "*.csproj")
                .Where(p => !RepoPaths.Relative(p).Contains("/.artifacts/"))
                .ToList();
        }

        private static string? Property(string project, string name)
        {
            return XDocument.Load(project).Descendants(name).Select(e => e.Value.Trim()).FirstOrDefault();
        }

        private static string? PackageVersion(string project, string package)
        {
            return XDocument.Load(project).Descendants("PackageReference")
                .Where(e => (string?)e.Attribute("Include") == package)
                .Select(e => (string?)e.Attribute("Version"))
                .FirstOrDefault();
        }

        private static List<string> CompileIncludes(string project)
        {
            return XDocument.Load(project).Descendants("Compile")
                .Select(e => (string?)e.Attribute("Include") ?? "")
                .ToList();
        }

        // Nombre de assembly de cada ProjectReference (leído del .csproj referenciado).
        private static List<string> ProjectReferenceNames(string project)
        {
            var directory = Path.GetDirectoryName(project)!;
            var names = new List<string>();
            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                var include = ((string?)reference.Attribute("Include") ?? "").Replace('\\', '/');
                var path = Path.GetFullPath(Path.Combine(directory, include.Replace('/', Path.DirectorySeparatorChar)));
                Assert.That(File.Exists(path), Is.True, RepoPaths.Relative(project) + ": no existe " + include);
                names.Add(Property(path, "AssemblyName") ?? Path.GetFileNameWithoutExtension(path));
            }

            return names;
        }
    }
}
