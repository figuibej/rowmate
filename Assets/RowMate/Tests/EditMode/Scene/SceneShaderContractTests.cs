using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SSC · El contrato de los shaders de la escena (06a-escena-lago/02-tech-design §7.5, §3.6; 04-design-notes §14; WP-D1), leído
    // del texto de los .shader y los .hlsl (corre en dotnet y en Unity sin GPU). Lo que se fija:
    // - los 13 shaders con su nombre RowMate/Scene/*, su cola y sin profundidad los del cielo (ShaderArt.Scene: una sola lista);
    // - ninguno lee el reloj de Unity (_Time y parientes) ni se limita a unos renderers (compilan para D3D11, Vulkan, GLES 3.2,
    //   Metal y GLCore, QA-G6aT-18); todos incluyen RowMateScene.hlsl y declaran sus propiedades y las de instancia del contrato;
    // - los instanciados con multi_compile_instancing y UNITY_INSTANCING_BUFFER; lo demás en UnityPerMaterial (SRP Batcher);
    // - las palabras clave: RM_WATER_EXTRAS en el agua y RM_TERRAIN_LAYERS en el terreno, nada más propio;
    // - un valor por constante: los valores por defecto de los .shader y las constantes kRm* de los .hlsl son los de Core/Scene/Art
    //   (la prueba compara cada uno con su fuente; si WP-D2 cambia un número, cambia en Art y en el shader a la vez);
    // - las cuentas de Core que copian los shaders existen con su nombre (SkyBackdrop.Gradient, CelestialMask.StarAttenuation,
    //   DegradedWater.ScreenAlpha, ShoreShading.ExtendedFog).
    // El costo (ALU) y la compilación por plataforma los mide SceneShaderTests en Unity (Tests/UnityEditMode).
    [TestFixture]
    public sealed class SceneShaderContractTests
    {
        private static readonly Regex ShaderName = new Regex(@"^\s*Shader\s+""([^""]+)""", RegexOptions.Multiline);
        private static readonly Regex QueueTag = new Regex(@"""Queue""\s*=\s*""([A-Za-z]+)([+-]\d+)?""");
        private static readonly Regex StaticConst = new Regex(@"static\s+const\s+float\s+(kRm\w+)\s*=\s*([-+0-9.eE]+)\s*;");

        private static string ShaderPath(string name)
        {
            return RepoPaths.Combine("Assets", "RowMate", ShaderArt.ShaderFolder, name + ".shader");
        }

        private static string IncludePath(string name)
        {
            return RepoPaths.Combine("Assets", "RowMate", ShaderArt.IncludeFolder, name + ".hlsl");
        }

        private static string ReadShader(string name)
        {
            var path = ShaderPath(name);
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            return File.ReadAllText(path);
        }

        private static string ReadInclude(string name)
        {
            var path = IncludePath(name);
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            return File.ReadAllText(path);
        }

        // Sin comentarios // y /* */ (lo que se busca es código, no texto).
        private static string Code(string text)
        {
            var noBlock = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(noBlock, @"//[^\n]*", " ");
        }

        // Los 13 de la 6a y los de los escenarios de la It. 8 (ShaderArt.Environment: Glow).
        private static IEnumerable<SceneShaderSpec> SceneAndEnvironment()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                yield return spec;
            }

            foreach (var spec in ShaderArt.Environment)
            {
                yield return spec;
            }
        }

        private static IEnumerable<string> AllSources()
        {
            foreach (var spec in SceneAndEnvironment())
            {
                yield return ShaderPath(spec.Name);
            }

            foreach (var include in ShaderArt.Includes)
            {
                yield return IncludePath(include);
            }
        }

        private static int QueueValue(string tag, string offset)
        {
            int baseQueue;
            switch (tag)
            {
                case "Background": baseQueue = 1000; break;
                case "Geometry": baseQueue = 2000; break;
                case "AlphaTest": baseQueue = 2450; break;
                case "Transparent": baseQueue = 3000; break;
                case "Overlay": baseQueue = 4000; break;
                default: throw new AssertionException("cola desconocida: " + tag);
            }

            return baseQueue + (string.IsNullOrEmpty(offset) ? 0 : int.Parse(offset, CultureInfo.InvariantCulture));
        }

        [Test, Category("AC-SCN-11-01")]
        public void TheThirteenShadersHaveTheirNameAndQueue()
        {
            Assert.That(ShaderArt.Scene.Length, Is.EqualTo(13));
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spec in ShaderArt.Scene)
            {
                Assert.That(names.Add(spec.Name), Is.True, "repetido: " + spec.Name);
                var text = ReadShader(spec.Name);
                var name = ShaderName.Match(text);
                Assert.That(name.Success, Is.True, spec.Name + ": sin Shader \"...\"");
                Assert.That(name.Groups[1].Value, Is.EqualTo(ShaderArt.Prefix + spec.Name));
                var queues = QueueTag.Matches(text);
                Assert.That(queues.Count, Is.EqualTo(1), spec.Name + ": una cola, en el SubShader");
                Assert.That(QueueValue(queues[0].Groups[1].Value, queues[0].Groups[2].Value), Is.EqualTo(spec.Queue), spec.Name + ": la cola del contrato");
            }

            // El orden del cielo (AC-SCN-11-01): degradé → estrellas → nubes → cordilleras, antes de lo opaco.
            Assert.That(ShaderArt.Find("Sky").Queue, Is.LessThan(ShaderArt.Find("Stars").Queue));
            Assert.That(ShaderArt.Find("Stars").Queue, Is.LessThan(ShaderArt.Find("Clouds").Queue));
            Assert.That(ShaderArt.Find("Clouds").Queue, Is.LessThan(ShaderArt.Find("Horizon").Queue));
            Assert.That(ShaderArt.Find("Horizon").Queue, Is.LessThan(ShaderArt.Find("Terrain").Queue));
            Assert.That(ShaderArt.Find("WaterDegraded").Queue, Is.LessThan(ShaderArt.Find("BoatShadow").Queue));
        }

        [Test]
        public void TheSkyLayersDoNotWriteDepth()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var code = Code(ReadShader(spec.Name));
                var zwriteOff = Regex.IsMatch(code, @"\bZWrite\s+Off\b");
                if (spec.Queue < 2000 || spec.Queue >= 2451)
                {
                    Assert.That(zwriteOff, Is.True, spec.Name + ": sin escribir profundidad");
                    Assert.That(Regex.IsMatch(code, @"\bZWrite\s+On\b"), Is.False, spec.Name);
                }
            }
        }

        [Test]
        public void NoShaderReadsTheUnityClockNorLimitsTheRenderers()
        {
            foreach (var path in AllSources())
            {
                Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
                var code = Code(File.ReadAllText(path));
                foreach (var forbidden in ShaderArt.ForbiddenIdentifiers)
                {
                    Assert.That(Regex.IsMatch(code, @"\b" + Regex.Escape(forbidden) + @"\b"), Is.False,
                        RepoPaths.Relative(path) + " nombra " + forbidden + " (todo va con _RmSceneTime y las fases de Core)");
                }
            }
        }

        [Test]
        public void EveryShaderIncludesTheSceneGlobals()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var code = Code(ReadShader(spec.Name));
                Assert.That(code, Does.Contain("Include/RowMateScene.hlsl"), spec.Name);
                Assert.That(Regex.IsMatch(code, @"#pragma\s+target\s+3\.5"), Is.True, spec.Name + ": target 3.5 (GLES 3)");
            }

            var globals = Code(ReadInclude("RowMateScene"));
            foreach (var name in ShaderArt.Globals)
            {
                Assert.That(Regex.IsMatch(globals, @"\b(float|float4|half|half4)\s+" + name + @"\s*;"), Is.True,
                    "RowMateScene.hlsl declara el global " + name);
            }
        }

        [Test]
        public void EachShaderDeclaresItsContractProperties()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var code = Code(ReadShader(spec.Name));
                foreach (var property in spec.Properties)
                {
                    Assert.That(Regex.IsMatch(code, @"\b" + property + @"\s*\("), Is.True, spec.Name + ": la propiedad " + property + " en Properties");
                    Assert.That(Regex.IsMatch(code, @"\b(float|float4|half|half4|TEXTURE2D|TEXTURE2D_ARRAY)\s*\(?\s*" + property + @"\b"), Is.True,
                        spec.Name + ": " + property + " declarada en el HLSL");
                }

                foreach (var property in spec.InstanceProperties)
                {
                    Assert.That(Regex.IsMatch(code, @"UNITY_DEFINE_INSTANCED_PROP\s*\(\s*float4?\s*,\s*" + property + @"\s*\)"), Is.True,
                        spec.Name + ": " + property + " por instancia");
                }
            }
        }

        [Test]
        public void InstancedShadersUseTheInstancingBufferAndTheRestTheSrpBatcher()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var code = Code(ReadShader(spec.Name));
                var passes = Regex.Matches(code, @"\bPass\s*\{").Count;
                Assert.That(passes, Is.GreaterThanOrEqualTo(1), spec.Name);
                if (spec.Instanced)
                {
                    Assert.That(Regex.Matches(code, @"#pragma\s+multi_compile_instancing").Count, Is.EqualTo(passes),
                        spec.Name + ": multi_compile_instancing en cada pasada");
                    Assert.That(code, Does.Contain("UNITY_INSTANCING_BUFFER_START(Props)"), spec.Name);
                }
                else
                {
                    Assert.That(code, Does.Not.Contain("UNITY_DEFINE_INSTANCED_PROP"), spec.Name + ": no se instancia");
                }

                if (spec.Properties.Length > 0)
                {
                    Assert.That(code, Does.Contain("CBUFFER_START(UnityPerMaterial)"), spec.Name + ": las del material en UnityPerMaterial");
                }
            }
        }

        [Test, Category("AC-SCN-13-03")]
        public void OnlyTheContractKeywords()
        {
            var keyword = new Regex(@"#pragma\s+(multi_compile|shader_feature)(_local)?(_vertex|_fragment)?\s+([^\n]+)");
            foreach (var spec in ShaderArt.Scene)
            {
                var code = Code(ReadShader(spec.Name));
                foreach (Match m in keyword.Matches(code))
                {
                    foreach (var word in m.Groups[4].Value.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (word == "_" || word.StartsWith("_MAIN_LIGHT_SHADOWS", StringComparison.Ordinal)
                            || word.StartsWith("_SHADOWS_SOFT", StringComparison.Ordinal) || word == "_CASTING_PUNCTUAL_LIGHT_SHADOW")
                        {
                            continue;
                        }

                        Assert.That(ShaderArt.Keywords, Does.Contain(word), spec.Name + ": palabra clave fuera del contrato: " + word);
                    }
                }
            }

            // Las propias, del material y fijas en la sesión (QA-G6aT-07): el agua y el terreno en todos los niveles; el fondo de
            // estrellas y la capa lejana de nubes, solo en Alto.
            Assert.That(Code(ReadShader("WaterDegraded")), Does.Match(@"#pragma\s+shader_feature_local\s+" + ShaderArt.WaterExtrasKeyword));
            Assert.That(Code(ReadShader("Terrain")), Does.Match(@"#pragma\s+shader_feature_local\s+" + ShaderArt.TerrainLayersKeyword));
            Assert.That(Code(ReadShader("Sky")), Does.Match(@"#pragma\s+shader_feature_local_fragment\s+" + ShaderArt.SkyStarFieldKeyword));
            Assert.That(Code(ReadShader("Clouds")), Does.Match(@"#pragma\s+shader_feature_local\s+" + ShaderArt.CloudLayerKeyword));
        }

        [Test, Category("AC-SCN-11-02"), Category("AC-SCN-11-01"), Category("AC-SCN-13-03")]
        public void TheShadersCopyTheCoreCurves()
        {
            // El telón (SkyBackdrop.Gradient), la máscara del astro (CelestialMask.StarAttenuation), el alfa de las líneas en pantalla
            // (DegradedWater.ScreenAlpha) y la niebla extendida (ShoreShading.ExtendedFog) tienen su copia con nombre en el HLSL.
            var scene = Code(ReadInclude("RowMateScene"));
            Assert.That(scene, Does.Contain("RmBackdropGradient("));
            Assert.That(scene, Does.Contain("RmStarAttenuation("));
            Assert.That(scene, Does.Contain("RmHaloAlpha("));
            Assert.That(scene, Does.Contain("RmSrgbToLinear("));
            Assert.That(Code(ReadShader("Sky")), Does.Contain("RmBackdropGradient("));
            Assert.That(Code(ReadShader("Stars")), Does.Contain("RmStarAttenuation("));
            Assert.That(Code(ReadShader("WaterDegraded")), Does.Contain("RmShimmerScreenAlpha("));
            Assert.That(Code(ReadInclude("RowMateFog")), Does.Contain("RmExtendedFog("));
            Assert.That(Code(ReadInclude("RowMateDither")), Does.Contain("RmDitherClip("));
            Assert.That(Code(ReadInclude("RowMateLighting")), Does.Contain("RmVertexLight("));
        }

        // Las constantes kRm* de los .hlsl y los .shader: cada una con su fuente en Core/Scene/Art (un valor por constante).
        [Test, Category("AC-SCN-11-05"), Category("AC-SCN-11-02")]
        public void TheShaderConstantsAreTheArtOnes()
        {
            var found = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var path in AllSources())
            {
                foreach (Match m in StaticConst.Matches(Code(File.ReadAllText(path))))
                {
                    var value = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (found.TryGetValue(m.Groups[1].Value, out var other))
                    {
                        Assert.That(value, Is.EqualTo(other), m.Groups[1].Value + " con dos valores");
                    }

                    found[m.Groups[1].Value] = value;
                }
            }

            var expected = ShaderArt.Constants();
            foreach (var pair in expected)
            {
                Assert.That(found.ContainsKey(pair.Key), Is.True, "falta la constante " + pair.Key + " en los shaders");
                Assert.That(found[pair.Key], Is.EqualTo(pair.Value).Within(5e-6 * Math.Max(1, Math.Abs(pair.Value))), pair.Key);
            }

            foreach (var name in found.Keys)
            {
                Assert.That(expected.ContainsKey(name), Is.True, name + ": una constante de los shaders sin su fuente en Core/Scene/Art");
            }

            // El Bayer 4 × 4 de RowMateDither.hlsl es el de MaterialArt (los tramados de los objetos y del cielo, DN6a-24, DN6a-29).
            var bayer = Regex.Match(Code(ReadInclude("RowMateDither")), @"kRmBayer4\s*\[\s*16\s*\]\s*=\s*\{([^}]*)\}");
            Assert.That(bayer.Success, Is.True, "RowMateDither.hlsl: static const float kRmBayer4[16]");
            var cells = bayer.Groups[1].Value.Split(',');
            Assert.That(cells.Length, Is.EqualTo(16));
            for (var i = 0; i < 16; i++)
            {
                var value = double.Parse(cells[i].Trim(), CultureInfo.InvariantCulture);
                Assert.That(value, Is.EqualTo(MaterialArt.BayerThreshold(i % 4, i / 4)).Within(1e-6), "kRmBayer4[" + i + "]");
            }
        }

        // Los valores por defecto de las propiedades del material son los de Art: así un material recién creado (SceneMaterialsBuilder,
        // WP-B1) ya se ve como el Designer lo pidió.
        [Test]
        public void TheMaterialDefaultsAreTheArtOnes()
        {
            var vector = new Regex(@"\b(_Rm\w+)\s*\(\s*""[^""]*""\s*,\s*(Vector|Color)\s*\)\s*=\s*\(\s*([^)]*)\)");
            var number = new Regex(@"\b(_Rm\w+)\s*\(\s*""[^""]*""\s*,\s*(Float|Range\s*\([^)]*\))\s*\)\s*=\s*([-+0-9.eE]+)");
            var defaults = ShaderArt.MaterialDefaults();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spec in SceneAndEnvironment())
            {
                var code = Code(ReadShader(spec.Name));
                foreach (Match m in vector.Matches(code))
                {
                    var key = spec.Name + "." + m.Groups[1].Value;
                    if (!defaults.TryGetValue(key, out var want))
                    {
                        continue;
                    }

                    seen.Add(key);
                    var parts = m.Groups[3].Value.Split(',');
                    Assert.That(parts.Length, Is.EqualTo(4), key);
                    for (var i = 0; i < 4; i++)
                    {
                        var got = double.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
                        Assert.That(got, Is.EqualTo(want[i]).Within(6e-4 * Math.Max(1, Math.Abs(want[i]))), key + "[" + i + "]");
                    }
                }

                foreach (Match m in number.Matches(code))
                {
                    var key = spec.Name + "." + m.Groups[1].Value;
                    if (!defaults.TryGetValue(key, out var want))
                    {
                        continue;
                    }

                    seen.Add(key);
                    Assert.That(double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), Is.EqualTo(want[0]).Within(6e-4 * Math.Max(1, Math.Abs(want[0]))), key);
                }
            }

            foreach (var key in defaults.Keys)
            {
                Assert.That(seen.Contains(key), Is.True, "sin valor por defecto en el shader: " + key);
            }
        }

        // Los verdes del pino del legacy (shore_painter.dart:89-93) van como constantes del shader Foliage, iguales a LakeArt.
        [Test, Category("AC-SCN-15-06")]
        public void TheFoliagePalettesAreTheLegacyGreens()
        {
            var code = Code(ReadShader("Foliage"));
            var block = Regex.Match(code, @"kRmPineGreens\s*\[\s*9\s*\]\s*=\s*\{([^}]*)\}");
            Assert.That(block.Success, Is.True, "Foliage: static const float3 kRmPineGreens[9]");
            var colors = Regex.Matches(block.Groups[1].Value, @"float3\s*\(\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)\s*\)");
            Assert.That(colors.Count, Is.EqualTo(9));
            for (var i = 0; i < 9; i++)
            {
                var want = SceneColor.FromArgb(LakeArt.PineGreens[i]).ToLinear();
                Assert.That(double.Parse(colors[i].Groups[1].Value, CultureInfo.InvariantCulture), Is.EqualTo(want.R).Within(5e-5), "verde " + i);
                Assert.That(double.Parse(colors[i].Groups[2].Value, CultureInfo.InvariantCulture), Is.EqualTo(want.G).Within(5e-5), "verde " + i);
                Assert.That(double.Parse(colors[i].Groups[3].Value, CultureInfo.InvariantCulture), Is.EqualTo(want.B).Within(5e-5), "verde " + i);
            }
        }

        [Test]
        public void TheShaderSpecsCoverTheBudgetsOfTheContract()
        {
            // 02-tech-design §7.5 con 04-design-notes §14.2 corregida por el gate: el agua ≤ 24 y 0 texturas (DT6a-07); las estrellas
            // ≤ 8 con la máscara del astro (DT6a-17).
            // It. 8: + 3 por los saltos de la ola de la costa y de la luz bajo el puente (08-entornos-selector/04-design-notes §6).
            Assert.That(ShaderArt.Find("WaterDegraded").MaxAlu, Is.EqualTo(WaterArt.MaxAlu).And.EqualTo(27));
            Assert.That(ShaderArt.Find("WaterDegraded").MaxTextures, Is.EqualTo(0));
            Assert.That(ShaderArt.Find("Stars").MaxAlu, Is.EqualTo(8));
            Assert.That(ShaderArt.Find("ScreenOverlay").Queue, Is.EqualTo(3500));
            foreach (var spec in ShaderArt.Scene)
            {
                Assert.That(spec.MaxAlu, Is.GreaterThan(0).And.LessThanOrEqualTo(41), spec.Name);
            }
        }
    }
}
