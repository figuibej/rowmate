using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RowMate.Scene;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Tests.UnityEditMode
{
    // It. 7 · Los shaders y materiales del remero (07-remero-palada/02-tech-design §6.3, §7.4; WP-B1): Rower, Oar, Spray, WaterMarks y
    // Buoy importan sin errores con su cola, compilan para los destinos de la 6a (Direct3D 11, OpenGL Core, Vulkan y GLES 3) con la base,
    // las sombras de URP y el instanciado; no leen el reloj de Unity; tienen su material en Art/Materials/Scene asignado en SceneWorld.asset
    // y la colección de variantes los cubre. El agua degradada recibe la sombra (DA7-04) y compila con las palabras clave de sombras.
    public sealed class RowerMaterialsTests
    {
        private static readonly (string Shader, int Queue, bool Instanced, bool Shadows)[] Specs =
        {
            (SceneShaderIds.Rower, 2000, false, true),
            (SceneShaderIds.Oar, 2000, false, true),
            (SceneShaderIds.Spray, 3100, true, false),
            (SceneShaderIds.WaterMarks, 3050, true, false),
            (SceneShaderIds.Buoy, 2000, false, true),
        };

        [Test]
        public void TheFiveShadersImportWithTheirQueueAndWithoutTheUnityClock()
        {
            foreach (var spec in Specs)
            {
                var shader = Shader.Find(spec.Shader);
                Assert.That(shader, Is.Not.Null, spec.Shader);
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, spec.Shader + ": con errores");
                Assert.That(shader.renderQueue, Is.EqualTo(spec.Queue), spec.Shader);
                var path = AssetDatabase.GetAssetPath(shader);
                var code = System.IO.File.ReadAllText(path);
                foreach (var forbidden in RowMate.Core.Scene.Art.ShaderArt.ForbiddenIdentifiers)
                {
                    Assert.That(System.Text.RegularExpressions.Regex.IsMatch(code, @"\b" + forbidden + @"\b"), Is.False, spec.Shader + ": " + forbidden);
                }
            }
        }

        [TestCase(ShaderCompilerPlatform.D3D)]
        [TestCase(ShaderCompilerPlatform.OpenGLCore)]
        [TestCase(ShaderCompilerPlatform.Vulkan)]
        [TestCase(ShaderCompilerPlatform.GLES3x)]
        public void EveryPassCompilesFor(ShaderCompilerPlatform platform)
        {
            if (platform == ShaderCompilerPlatform.D3D)
            {
                Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "el compilador de Direct3D es de Windows");
            }

            var target = platform == ShaderCompilerPlatform.GLES3x ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
            var failures = new StringBuilder();
            var all = new List<(string, bool, bool)>();
            foreach (var spec in Specs)
            {
                all.Add((spec.Shader, spec.Instanced, spec.Shadows));
            }

            all.Add((SceneShaderIds.WaterDegraded, false, true));
            foreach (var (name, instanced, shadows) in all)
            {
                var data = ShaderUtil.GetShaderData(Shader.Find(name));
                var subshader = data.GetSubshader(0);
                for (var p = 0; p < subshader.PassCount; p++)
                {
                    var pass = subshader.GetPass(p);
                    var mode = pass.FindTagValue(new ShaderTagId("LightMode")).name;
                    var variants = new List<string[]> { new string[0] };
                    if (instanced)
                    {
                        variants.Add(new[] { "INSTANCING_ON" });
                    }

                    if (shadows && mode == "UniversalForward")
                    {
                        variants.Add(new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT" });
                        variants.Add(new[] { "_MAIN_LIGHT_SHADOWS" });
                    }

                    if (name == SceneShaderIds.WaterDegraded)
                    {
                        variants.Add(new[] { SceneShaderIds.WaterExtrasKeyword, "_MAIN_LIGHT_SHADOWS_CASCADE" });
                    }

                    foreach (var keywords in variants)
                    {
                        foreach (var type in new[] { ShaderType.Vertex, ShaderType.Fragment })
                        {
                            var info = pass.CompileVariant(type, keywords, platform, target);
                            if (!info.Success)
                            {
                                failures.Append(name).Append('/').Append(pass.Name).Append(" [").Append(string.Join(" ", keywords)).Append("] ").Append(type).Append(": ");
                                foreach (var message in info.Messages)
                                {
                                    failures.Append(message.message).Append(" (").Append(message.line).Append(") ");
                                }

                                failures.AppendLine();
                            }
                        }
                    }
                }
            }

            Assert.That(failures.ToString(), Is.Empty, "no compilan para " + platform);
        }

        [Test]
        public void TheMaterialsExistAreAssignedAndTheVariantsCoverThem()
        {
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            Assert.That(world, Is.Not.Null);
            var set = world!.Materials;
            var pairs = new (Material? Material, string Shader)[]
            {
                (set.Rower, SceneShaderIds.Rower), (set.Oar, SceneShaderIds.Oar), (set.Spray, SceneShaderIds.Spray), (set.WaterMarks, SceneShaderIds.WaterMarks),
                (set.Buoy, SceneShaderIds.Buoy),
            };
            foreach (var (material, shader) in pairs)
            {
                Assert.That(material, Is.Not.Null, shader + ": sin material en SceneWorld.asset");
                Assert.That(material!.shader.name, Is.EqualTo(shader));
            }

            Assert.That(set.Spray!.enableInstancing, Is.True);
            Assert.That(set.WaterMarks!.enableInstancing, Is.True);
            var variants = world.Variants;
            Assert.That(variants, Is.Not.Null);
            foreach (var (_, shader) in pairs)
            {
                var pass = PassType.ScriptableRenderPipeline;
                Assert.That(variants!.Contains(new ShaderVariantCollection.ShaderVariant(Shader.Find(shader), pass, new string[0])), Is.True, shader);
            }
        }
    }
}
