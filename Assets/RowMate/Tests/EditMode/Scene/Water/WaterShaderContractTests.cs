using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WSC · El contrato de los shaders del agua de la 6b, leído del texto (06b-agua-avanzada/02-tech-design §3.4, §4.1, DT6b-22, DT6b-23;
    // corre en dotnet y en Unity sin GPU; el costo en ALU lo mide SceneShaderTests en Unity):
    // - los presupuestos de la spec en ShaderArt (Alto 110/6, Medio 70/5, Lite 35/2; la degradada 28; Foam de nuevo en 30; WaterMarks con
    //   sus ramas ≤ 40; sin WaterWake y Glow sin cambios) — siempre;
    // - lo que escribe WP-D (Art/Shaders/Include/RowMateWater.hlsl, WaterAdvanced.shader y los cambios de WaterDegraded y WaterMarks): el tope
    //   de las 10 líneas, sin Fallback y con el tag RmWater = Advanced, sus palabras clave, sus globales y sus constantes con la fuente en
    //   Core. Mientras WP-D no escribió RowMateWater.hlsl esas pruebas quedan ignoradas con el motivo (como en la It. 15), y nunca se cierra
    //   la iteración con un Ignore.
    [TestFixture]
    public sealed class WaterShaderContractTests
    {
        private static readonly Regex StaticConst = new Regex(@"static\s+const\s+float\s+(kRm\w+)\s*=\s*([-+0-9.eE]+)\s*;");

        private static string ShaderPath(string name)
        {
            return RepoPaths.Combine("Assets", "RowMate", ShaderArt.ShaderFolder, name + ".shader");
        }

        private static string IncludePath(string name)
        {
            return RepoPaths.Combine("Assets", "RowMate", ShaderArt.IncludeFolder, name + ".hlsl");
        }

        private static string Code(string path)
        {
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            var noBlock = Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(noBlock, @"//[^\n]*", " ");
        }

        // Lo de WP-D: hasta que existe la inclusión compartida del agua, ignorada con el motivo.
        private static string RequireWaterInclude()
        {
            var path = IncludePath(ShaderArt.WaterInclude);
            if (!File.Exists(path))
            {
                Assert.Ignore("pendiente WP-D: todavía no existe Art/Shaders/Include/" + ShaderArt.WaterInclude + ".hlsl");
            }

            return Code(path);
        }

        [Test, Category("AC-6B-29")]
        public void TheBudgetsOfTheSpec()
        {
            Assert.That(ShaderArt.WaterTiers, Has.Length.EqualTo(3));
            var high = Array.Find(ShaderArt.WaterTiers, t => t.Tier == RowMate.Core.Scene.QualityTier.High);
            var medium = Array.Find(ShaderArt.WaterTiers, t => t.Tier == RowMate.Core.Scene.QualityTier.Medium);
            var low = Array.Find(ShaderArt.WaterTiers, t => t.Tier == RowMate.Core.Scene.QualityTier.Low);
            Assert.That((high.MaxAlu, high.MaxTextures, high.Keyword), Is.EqualTo((110, 6, ShaderArt.WaterHighKeyword)));
            Assert.That((medium.MaxAlu, medium.MaxTextures, medium.Keyword), Is.EqualTo((70, 5, "")));
            Assert.That((low.MaxAlu, low.MaxTextures, low.Keyword), Is.EqualTo((35, 2, ShaderArt.WaterLiteKeyword)));
            var advanced = ShaderArt.FindWater("WaterAdvanced");
            Assert.That(advanced.MaxAlu, Is.EqualTo(medium.MaxAlu), "el de la spec es el de Medio");
            Assert.That(advanced.Queue, Is.EqualTo(ShaderArt.Find("WaterDegraded").Queue), "opaca, en la cola de la degradada");
            Assert.That(ShaderArt.Find("WaterDegraded").MaxAlu, Is.EqualTo(28).And.EqualTo(WaterArt.MaxAlu), "#2: + 1 por el tope de la línea");
            Assert.That(ShaderArt.Find("WaterDegraded").MaxTextures, Is.EqualTo(0));

            // #22: la espuma de la costa vuelve a ≤ 30 ALU en su prueba.
            Assert.That(Array.Find(ShaderArt.Environment, s => s.Name == "Foam").MaxAlu, Is.EqualTo(30));
            // DT6b-23: la estela y las columnas son tipos de WaterMarks (sin pases nuevos): sus ramas ≤ 40 y el cuerpo sigue en 20.
            var marks = Array.Find(ShaderArt.Rower, s => s.Name == "WaterMarks");
            Assert.That(marks.MaxBranchAlu, Is.EqualTo(40));
            Assert.That(marks.MaxAlu, Is.EqualTo(20));
            var glow = Array.Find(ShaderArt.Environment, s => s.Name == "Glow");
            Assert.That(glow.MaxAlu, Is.EqualTo(14), "Glow sin cambios");
            foreach (var list in new[] { ShaderArt.Scene, ShaderArt.Rower, ShaderArt.Environment, ShaderArt.WaterShaders })
            {
                Assert.That(Array.Exists(list, s => s.Name == "WaterWake"), Is.False, "no hay WaterWake (DT6b-23)");
            }

            Assert.That(ShaderArt.WaterGlobals, Does.Contain("_RmWaterPhases").And.Contain("_RmWaterOffsets").And.Contain("_RmWaterReflectionTex")
                .And.Contain("_RmDegradedBlend").And.Contain("_RmWake"));
        }

        [Test, Category("AC-6B-03")]
        public void DegradedClampsToTenLines()
        {
            // #2 (QA-V6a-PAR-01): la 11.ª línea salía porque el índice no tenía tope (WaterDegraded.shader:176). n = clamp(round(...); 0; 9)
            // en la inclusión compartida por WaterDegraded y el salto degradado de WaterAdvanced.
            Assert.That(WaterArt.Lines, Is.EqualTo(10));
            Assert.That(ShaderArt.WaterConstants()["kRmDegradedLineMax"], Is.EqualTo(9));
            var water = RequireWaterInclude();
            Assert.That(Regex.IsMatch(water, @"clamp\s*\(\s*round\s*\([^;]*,\s*0(\.0)?\s*,\s*(9(\.0)?|kRmDegradedLineMax)\s*\)"), Is.True,
                ShaderArt.WaterInclude + ".hlsl: n = clamp(round(...), 0, 9)");
            Assert.That(Code(ShaderPath("WaterDegraded")), Does.Contain("Include/" + ShaderArt.WaterInclude + ".hlsl"), "la degradada usa el mismo código");
        }

        [Test, Category("AC-6B-06")]
        public void WaterAdvancedHasNoFallbackAndIsTagged()
        {
            RequireWaterInclude();
            var spec = ShaderArt.FindWater("WaterAdvanced");
            var code = Code(ShaderPath(spec.Name));
            Assert.That(code, Does.Match(@"Shader\s+""" + Regex.Escape(spec.FullName) + @""""));
            // G6b-01: con un Fallback a la degradada, Shader.isSupported daría true en un equipo sin soporte.
            Assert.That(Regex.IsMatch(code, @"\bFallback\s+""") , Is.False, "sin Fallback (DT6b-22)");
            var subShaders = Regex.Matches(code, @"\bSubShader\s*\{").Count;
            var tags = Regex.Matches(code, @"""" + ShaderArt.WaterTag + @"""\s*=\s*""" + ShaderArt.WaterTagValue + @"""").Count;
            Assert.That(subShaders, Is.GreaterThanOrEqualTo(1));
            Assert.That(tags, Is.GreaterThanOrEqualTo(subShaders), "el tag RmWater = Advanced en cada SubShader");
            Assert.That(Regex.Matches(code, @"\bPass\s*\{").Count, Is.EqualTo(1), "un solo pase UniversalForward, como la degradada");
            Assert.That(code, Does.Match(@"""LightMode""\s*=\s*""UniversalForward"""));
            Assert.That(code, Does.Match(@"""Queue""\s*=\s*""(AlphaTest|Geometry\+450)"""), "cola 2450");
            Assert.That(code, Does.Match(@"#pragma\s+shader_feature_local(_fragment)?\s+_\s+" + ShaderArt.WaterHighKeyword + @"\s+" + ShaderArt.WaterLiteKeyword)
                .Or.Match(@"#pragma\s+shader_feature_local(_fragment)?\s+_\s+" + ShaderArt.WaterLiteKeyword + @"\s+" + ShaderArt.WaterHighKeyword),
                "las variantes por palabra clave del material: Alto, Medio (sin palabra) y Lite");
            Assert.That(code, Does.Contain("Include/RowMateScene.hlsl"));
            Assert.That(code, Does.Contain("Include/" + ShaderArt.WaterInclude + ".hlsl"));
            Assert.That(Regex.IsMatch(code, @"#pragma\s+target\s+3\.5"), Is.True, "target 3.5 (GLES 3)");
            foreach (var property in spec.Properties)
            {
                Assert.That(Regex.IsMatch(code, @"\b" + property + @"\s*\("), Is.True, spec.Name + ": " + property + " en Properties");
            }

            foreach (var forbidden in ShaderArt.ForbiddenIdentifiers)
            {
                Assert.That(Regex.IsMatch(code, @"\b" + Regex.Escape(forbidden) + @"\b"), Is.False, spec.Name + " nombra " + forbidden);
            }
        }

        [Test, Category("AC-6B-20")]
        public void TheWaterGlobalsAreDeclaredAndTheShaderNeverSeesTheDistance()
        {
            var water = RequireWaterInclude();
            var scene = Code(IncludePath("RowMateScene"));
            foreach (var name in ShaderArt.WaterGlobals)
            {
                var pattern = @"\b(float|float4|half|half4|TEXTURE2D)\s*\(?\s*" + name + @"\b";
                Assert.That(Regex.IsMatch(water, pattern) || Regex.IsMatch(scene, pattern), Is.True, "el global " + name + " declarado");
            }

            // Q-68: el agua trabaja en cinta con las fases de Core; nada de la distancia de escena del legacy (uDistance).
            Assert.That(Regex.IsMatch(water, @"\b(uDistance|_RmDistance)\b"), Is.False);
            foreach (var forbidden in ShaderArt.ForbiddenIdentifiers)
            {
                Assert.That(Regex.IsMatch(water, @"\b" + Regex.Escape(forbidden) + @"\b"), Is.False, ShaderArt.WaterInclude + " nombra " + forbidden);
            }
        }

        [Test, Category("AC-6B-10")]
        public void TheWaterConstantsAreTheCoreOnes()
        {
            RequireWaterInclude();
            var found = new Dictionary<string, double>(StringComparer.Ordinal);
            var sources = new List<string> { IncludePath(ShaderArt.WaterInclude) };
            if (File.Exists(ShaderPath("WaterAdvanced")))
            {
                sources.Add(ShaderPath("WaterAdvanced"));
            }

            foreach (var path in sources)
            {
                foreach (Match m in StaticConst.Matches(Code(path)))
                {
                    found[m.Groups[1].Value] = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                }
            }

            var water = ShaderArt.WaterConstants();
            var all = ShaderArt.Constants();
            foreach (var pair in water)
            {
                Assert.That(found.ContainsKey(pair.Key), Is.True, "falta la constante " + pair.Key + " en los shaders del agua");
                Assert.That(found[pair.Key], Is.EqualTo(pair.Value).Within(5e-6 * Math.Max(1, Math.Abs(pair.Value))), pair.Key);
            }

            foreach (var pair in found)
            {
                var known = water.TryGetValue(pair.Key, out var want) || all.TryGetValue(pair.Key, out want);
                Assert.That(known, Is.True, pair.Key + ": una constante del agua sin su fuente en Core (ShaderArt.WaterConstants)");
                Assert.That(pair.Value, Is.EqualTo(want).Within(5e-6 * Math.Max(1, Math.Abs(want))), pair.Key);
            }
        }

        [Test, Category("AC-6B-14"), Category("AC-6B-18")]
        public void WaterMarksCarriesTheWakeAndTheColumns()
        {
            // DT6b-23: la estela (tipo 5) y las columnas de luz (tipo 4) van en el material de WaterMarks, con la mezcla premultiplicada.
            RequireWaterInclude();
            var code = Code(ShaderPath("WaterMarks"));
            Assert.That(code, Does.Match(@"\bBlend\s+One\s+OneMinusSrcAlpha\b"), "mezcla premultiplicada");
            Assert.That(code, Does.Contain("_RmWake"), "la estela expande la cinta desde _RmWake");
            Assert.That(File.Exists(ShaderPath("WaterWake")), Is.False, "sin WaterWake propio");
        }
    }
}
