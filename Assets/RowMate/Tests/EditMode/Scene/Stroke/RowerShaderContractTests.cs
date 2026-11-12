using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // RSC · El contrato de los cinco shaders del remero (07-remero-palada/02-tech-design §7.4; 04-design-notes §6; WP-D2), leído del texto
    // de los .shader (corre en dotnet y en Unity sin GPU), como SceneShaderContractTests con los 13 de la 6a: nombre y cola, sin el reloj
    // de Unity, los globales de la escena y target 3.5, las propiedades del material y de instancia, instanciado o SRP Batcher, los
    // transparentes sin profundidad, y cada constante kRm* con su valor en RowerArt (ShaderArt.RowerConstants). El costo lo mide
    // SceneShaderTests en Unity con ShaderArt.Rower.
    [TestFixture]
    public sealed class RowerShaderContractTests
    {
        private static readonly Regex ShaderName = new Regex(@"^\s*Shader\s+""([^""]+)""", RegexOptions.Multiline);
        private static readonly Regex QueueTag = new Regex(@"""Queue""\s*=\s*""([A-Za-z]+)([+-]\d+)?""");
        private static readonly Regex StaticConst = new Regex(@"static\s+const\s+float\s+(kRm\w+)\s*=\s*([-+0-9.eE]+)\s*;");

        private static string ReadShader(string name)
        {
            var path = RepoPaths.Combine("Assets", "RowMate", ShaderArt.ShaderFolder, name + ".shader");
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            return File.ReadAllText(path);
        }

        private static string Code(string text)
        {
            var noBlock = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(noBlock, @"//[^\n]*", " ");
        }

        private static int QueueValue(string tag, string offset)
        {
            var baseQueue = tag == "Geometry" ? 2000 : tag == "Transparent" ? 3000 : throw new AssertionException("cola inesperada: " + tag);
            return baseQueue + (string.IsNullOrEmpty(offset) ? 0 : int.Parse(offset, CultureInfo.InvariantCulture));
        }

        [Test]
        public void TheFiveShadersHaveTheirNameQueueAndGlobals()
        {
            Assert.That(ShaderArt.Rower.Length, Is.EqualTo(5));
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spec in ShaderArt.Rower)
            {
                Assert.That(names.Add(spec.Name), Is.True, "repetido: " + spec.Name);
                Assert.That(Array.Exists(ShaderArt.Scene, s => s.Name == spec.Name), Is.False, spec.Name + ": va aparte de los 13 de la 6a");
                var text = ReadShader(spec.Name);
                Assert.That(ShaderName.Match(text).Groups[1].Value, Is.EqualTo(ShaderArt.Prefix + spec.Name));
                var queues = QueueTag.Matches(text);
                Assert.That(queues.Count, Is.EqualTo(1), spec.Name + ": una cola, en el SubShader");
                Assert.That(QueueValue(queues[0].Groups[1].Value, queues[0].Groups[2].Value), Is.EqualTo(spec.Queue), spec.Name);
                var code = Code(text);
                Assert.That(code, Does.Contain("Include/RowMateScene.hlsl"), spec.Name);
                Assert.That(Regex.IsMatch(code, @"#pragma\s+target\s+3\.5"), Is.True, spec.Name + ": target 3.5 (GLES 3)");
                foreach (var forbidden in ShaderArt.ForbiddenIdentifiers)
                {
                    Assert.That(Regex.IsMatch(code, @"\b" + Regex.Escape(forbidden) + @"\b"), Is.False, spec.Name + " nombra " + forbidden);
                }

                // Lo transparente (la salpicadura y las marcas del agua) va después del agua y no escribe profundidad.
                if (spec.Queue >= 3000)
                {
                    Assert.That(Regex.IsMatch(code, @"\bZWrite\s+Off\b"), Is.True, spec.Name);
                    Assert.That(Regex.IsMatch(code, @"\bZWrite\s+On\b"), Is.False, spec.Name);
                    Assert.That(spec.Queue, Is.GreaterThan(ShaderArt.Find("WaterDegraded").Queue), spec.Name);
                }
            }

            Assert.That(ShaderArt.Find("WaterDegraded").Queue, Is.LessThan(3050));
            Assert.That(ShaderArt.Rower[3].Queue, Is.LessThan(ShaderArt.Rower[2].Queue), "las marcas del agua antes que la salpicadura");
        }

        [Test]
        public void EachShaderDeclaresItsPropertiesAndInstancesOrBatches()
        {
            foreach (var spec in ShaderArt.Rower)
            {
                var code = Code(ReadShader(spec.Name));
                foreach (var property in spec.Properties)
                {
                    Assert.That(Regex.IsMatch(code, @"\bfloat4?\s+" + property + @"\b"), Is.True, spec.Name + ": " + property + " declarada en el HLSL");
                }

                foreach (var property in spec.InstanceProperties)
                {
                    Assert.That(Regex.IsMatch(code, @"UNITY_DEFINE_INSTANCED_PROP\s*\(\s*float4?\s*,\s*" + property + @"\s*\)"), Is.True,
                        spec.Name + ": " + property + " por instancia");
                }

                Assert.That(code, Does.Contain("CBUFFER_START(UnityPerMaterial)"), spec.Name + ": las del material en UnityPerMaterial");
                if (spec.Instanced)
                {
                    Assert.That(code, Does.Contain("#pragma multi_compile_instancing"), spec.Name);
                }
                else
                {
                    Assert.That(code, Does.Not.Contain("UNITY_DEFINE_INSTANCED_PROP"), spec.Name + ": no se instancia");
                    Assert.That(code, Does.Contain("\"ShadowCaster\""), spec.Name + ": lo opaco proyecta (DA7-06: la caña la apaga el renderer)");
                }
            }
        }

        [Test]
        public void TheShaderConstantsAreTheRowerArtOnes()
        {
            var found = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var spec in ShaderArt.Rower)
            {
                foreach (Match m in StaticConst.Matches(Code(ReadShader(spec.Name))))
                {
                    var value = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    Assert.That(found.ContainsKey(m.Groups[1].Value), Is.False, m.Groups[1].Value + " en dos shaders");
                    found[m.Groups[1].Value] = value;
                }
            }

            var expected = ShaderArt.RowerConstants();
            foreach (var pair in expected)
            {
                Assert.That(found.ContainsKey(pair.Key), Is.True, "falta la constante " + pair.Key + " en los shaders del remero");
                Assert.That(found[pair.Key], Is.EqualTo(pair.Value).Within(2e-3 * Math.Max(1, Math.Abs(pair.Value))), pair.Key);
            }

            foreach (var name in found.Keys)
            {
                Assert.That(expected.ContainsKey(name), Is.True, name + ": una constante de los shaders del remero sin su fuente en RowerArt");
            }
        }

        // Las marcas del agua: ≤ 20 ALU por contrato (DA7-08) y el par de remolinos separado como el arte (§8.3).
        [Test]
        public void TheWaterMarksKeepTheArtContract()
        {
            // It. 8: 24 + 3 por los saltos de la ola de la costa y de la luz bajo el puente (08-entornos-selector/04-design-notes §6).
            // It. 6b (#2): + 1 por el tope de la línea (n ≤ 9).
            Assert.That(ShaderArt.Find("WaterDegraded").MaxAlu, Is.EqualTo(28));
            Assert.That(Array.Find(ShaderArt.Rower, s => s.Name == "WaterMarks").MaxAlu, Is.LessThanOrEqualTo(20));
            Assert.That(RowerArt.SwirlSeparation, Is.EqualTo(0.45));
            Assert.That(RowerArt.BuoyGloss, Is.EqualTo(Math.Pow(2, 5.9)).Within(1e-9));
            foreach (var spec in ShaderArt.Rower)
            {
                Assert.That(spec.MaxTextures, Is.EqualTo(0), spec.Name + ": 0 texturas");
                Assert.That(spec.MaxAlu, Is.GreaterThan(0).And.LessThanOrEqualTo(41), spec.Name);
            }
        }
    }
}
