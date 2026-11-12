using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Scene.Art;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Tests.UnityEditMode
{
    // SSH · Los 13 shaders de la escena compilan y entran en su costo (06a-escena-lago/02-tech-design §7.5; 04-design-notes §14.2,
    // "Lo que verifica WP-D1 antes de entregar"; DN6a-23; WP-D1). Con la compilación del Editor, sin GPU:
    // - cada shader de ShaderArt.Scene existe en Art/Shaders/Scene, sin errores, con la cola del contrato;
    // - cada pasada compila para Direct3D 11, OpenGL Core (el CI de Linux, QA-G6aT-18), Vulkan y GLES 3 con sus variantes: la base, la
    //   instanciada, la de sombras (las palabras clave de URP del nivel) y las propias (RM_WATER_EXTRAS, RM_TERRAIN_LAYERS); Metal se
    //   intenta y, si el compilador de Metal no está en este Editor, queda NO VERIFICABLE (sin Mac);
    // - el costo del fragmento de la pasada que dibuja (UniversalForward, sin las sombras de URP, con lo que se usa en la sesión:
    //   instanciado y RM_WATER_EXTRAS): las instrucciones de cálculo del código de Direct3D 11 (la tabla STAT del DXBC: todas menos las
    //   de textura y las de control de flujo) ≤ ShaderArt.MaxAlu, y las lecturas de textura ≤ MaxTextures. El agua, ≤ 24 (DT6a-07).
    // Lo que mide queda en el registro de la corrida ("[SHADER] …") para 04-design-notes.
    public sealed class SceneShaderTests
    {
        private const string ShadowKeyword = "_MAIN_LIGHT_SHADOWS_CASCADE";
        private const string SoftKeyword = "_SHADOWS_SOFT";
        private const string InstancingKeyword = "INSTANCING_ON";

        private static Shader Load(SceneShaderSpec spec)
        {
            var path = "Assets/RowMate/" + ShaderArt.ShaderFolder + "/" + spec.Name + ".shader";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader, Is.Not.Null, "falta " + path);
            return shader;
        }

        private static IEnumerable<string[]> Variants(SceneShaderSpec spec, ShaderData.Pass pass)
        {
            var mode = pass.FindTagValue(new ShaderTagId("LightMode")).name;
            var own = new List<string>();
            if (spec.Instanced)
            {
                own.Add(InstancingKeyword);
            }

            if (spec.Name == "WaterDegraded")
            {
                own.Add(ShaderArt.WaterExtrasKeyword);
            }

            yield return new string[0];
            yield return own.ToArray();
            if (mode == "UniversalForward" && ReceivesShadows(spec))
            {
                var shadowed = new List<string>(own) { ShadowKeyword, SoftKeyword };
                yield return shadowed.ToArray();
            }

            if (spec.Name == "Terrain" && mode == "UniversalForward")
            {
                yield return new[] { ShaderArt.TerrainLayersKeyword };
            }

            if (spec.Name == "Sky")
            {
                yield return new[] { ShaderArt.SkyStarFieldKeyword };
            }

            if (spec.Name == "Clouds")
            {
                yield return new[] { InstancingKeyword, ShaderArt.CloudLayerKeyword };
            }
        }

        private static bool ReceivesShadows(SceneShaderSpec spec)
        {
            return spec.Name == "Terrain" || spec.Name == "Prop" || spec.Name == "Foliage" || spec.Name == "Grass" || spec.Name == "Reeds"
                || spec.Name == "Hull";
        }

        [Test]
        public void TheThirteenShadersImportWithoutErrorsAndWithTheirQueue()
        {
            foreach (var spec in ShaderArt.Scene)
            {
                var shader = Load(spec);
                Assert.That(shader.name, Is.EqualTo(spec.FullName));
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, spec.Name + ": con errores");
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                {
                    Assert.That(message.severity, Is.Not.EqualTo(ShaderCompilerMessageSeverity.Error), spec.Name + ": " + message.message);
                }

                Assert.That(shader.renderQueue, Is.EqualTo(spec.Queue), spec.Name + ": la cola del contrato");
                Assert.That(Shader.Find(spec.FullName), Is.SameAs(shader), spec.Name + ": se encuentra por su nombre");
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
            var stage = platform == ShaderCompilerPlatform.D3D ? ShaderType.Fragment : ShaderType.Vertex;
            var failures = new StringBuilder();
            var compiled = 0;
            foreach (var spec in ShaderArt.Scene)
            {
                var data = ShaderUtil.GetShaderData(Load(spec));
                var subshader = data.GetSubshader(0);
                for (var p = 0; p < subshader.PassCount; p++)
                {
                    var pass = subshader.GetPass(p);
                    foreach (var keywords in Variants(spec, pass))
                    {
                        var stages = platform == ShaderCompilerPlatform.D3D ? new[] { ShaderType.Vertex, ShaderType.Fragment } : new[] { stage };
                        foreach (var type in stages)
                        {
                            var info = pass.CompileVariant(type, keywords, platform, target);
                            compiled++;
                            if (!info.Success)
                            {
                                failures.Append(spec.Name).Append('/').Append(pass.Name).Append(" [").Append(string.Join(" ", keywords)).Append("] ")
                                    .Append(type).Append(": ");
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

            Debug.Log("[SHADER] " + platform + ": " + compiled + " variantes compiladas");
            Assert.That(failures.ToString(), Is.Empty, "no compilan para " + platform);
        }

        [Test]
        public void MetalCompilesOrIsNotVerifiable()
        {
            var failures = new StringBuilder();
            foreach (var spec in ShaderArt.Scene)
            {
                var data = ShaderUtil.GetShaderData(Load(spec));
                var pass = data.GetSubshader(0).GetPass(0);
                ShaderData.VariantCompileInfo info;
                try
                {
                    info = pass.CompileVariant(ShaderType.Vertex, new string[0], ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX);
                }
                catch (Exception e)
                {
                    Assert.Inconclusive("NO VERIFICABLE: el compilador de Metal no corre en este Editor (" + e.GetType().Name + ")");
                    return;
                }

                if (!info.Success)
                {
                    foreach (var message in info.Messages)
                    {
                        failures.Append(spec.Name).Append(": ").Append(message.message).AppendLine();
                    }
                }
            }

            if (failures.ToString().IndexOf("not supported", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Assert.Inconclusive("NO VERIFICABLE: " + failures);
            }

            Assert.That(failures.ToString(), Is.Empty, "no compilan para Metal");
        }

        [Test]
        public void EachFragmentFitsItsBudget()
        {
            MeasureBudgets(ShaderArt.Scene, "costo del fragmento en Medio (D3D11)");
        }

        // It. 7 (WP-D2; 07-remero-palada/04-design-notes §6): los cinco del remero con su presupuesto de ShaderArt.Rower.
        [Test]
        public void TheRowerFragmentsFitTheirBudget()
        {
            MeasureBudgets(ShaderArt.Rower, "costo del fragmento del remero en Medio (D3D11)");
        }

        // It. 8 (WP-D1; 08-entornos-selector/04-design-notes §6): los de los escenarios (Glow) con su presupuesto de ShaderArt.Environment.
        [Test]
        public void TheEnvironmentFragmentsFitTheirBudget()
        {
            MeasureBudgets(ShaderArt.Environment, "costo del fragmento de los escenarios en Medio (D3D11)");
        }

        private static void MeasureBudgets(IEnumerable<SceneShaderSpec> specs, string title)
        {
            Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "la medida es la del código de Direct3D 11");
            var report = new StringBuilder();
            var over = new StringBuilder();
            foreach (var spec in specs)
            {
                var data = ShaderUtil.GetShaderData(Load(spec));
                var pass = FindForward(data);
                Assert.That(pass, Is.Not.Null, spec.Name + ": sin pasada UniversalForward");
                var keywords = new List<string>();
                if (spec.Instanced)
                {
                    keywords.Add(InstancingKeyword);
                }

                if (spec.Name == "WaterDegraded")
                {
                    keywords.Add(ShaderArt.WaterExtrasKeyword);
                }

                var info = pass!.CompileVariant(ShaderType.Fragment, keywords.ToArray(), ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64, true);
                Assert.That(info.Success, Is.True, spec.Name + ": no compila");
                Assert.That(DxbcStats.TryRead(info.ShaderData, out var stats, out var why), Is.True, spec.Name + ": " + why);
                report.Append(spec.Name).Append(": ").Append(stats.Math).Append(" ALU (tope ").Append(spec.MaxAlu).Append(")");
                if (stats.Branch > 0)
                {
                    report.Append(" + ").Append(stats.Branch).Append(" en bloques condicionales (tope ").Append(spec.MaxBranchAlu).Append(")");
                }

                report.Append(", ").Append(stats.Textures).Append(" texturas (tope ").Append(spec.MaxTextures).Append("); ");
                if (stats.Math > spec.MaxAlu || stats.Branch > spec.MaxBranchAlu || stats.Textures > spec.MaxTextures)
                {
                    over.Append(spec.Name).Append(' ');
                    Debug.Log("[SHADER] " + spec.Name + " (D3D11), el desensamblado:\n" + stats.Disassembly);
                }
            }

            Debug.Log("[SHADER] " + title + ": " + report);
            Assert.That(over.ToString(), Is.Empty, "fuera de presupuesto: " + over + "\n" + report);
        }

        // Lo que es solo del nivel Alto (el fondo de estrellas y la capa lejana de nubes): se mide y se informa; el presupuesto es el de
        // Medio (02-tech-design §4.1), y el Alto es el escritorio.
        [TestCase("Sky", ShaderArt.SkyStarFieldKeyword)]
        [TestCase("Clouds", ShaderArt.CloudLayerKeyword)]
        public void TheHighTierExtrasCompileAndAreMeasured(string name, string keyword)
        {
            Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "la medida es la del código de Direct3D 11");
            var spec = ShaderArt.Find(name);
            var pass = FindForward(ShaderUtil.GetShaderData(Load(spec)));
            var keywords = spec.Instanced ? new[] { InstancingKeyword, keyword } : new[] { keyword };
            foreach (var platform in new[] { ShaderCompilerPlatform.OpenGLCore, ShaderCompilerPlatform.Vulkan })
            {
                Assert.That(pass!.CompileVariant(ShaderType.Vertex, keywords, platform, BuildTarget.StandaloneWindows64).Success, Is.True, name + " " + platform);
            }

            var info = pass!.CompileVariant(ShaderType.Fragment, keywords, ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64, true);
            Assert.That(info.Success, Is.True);
            Assert.That(DxbcStats.TryRead(info.ShaderData, out var stats, out var why), Is.True, why);
            Debug.Log("[SHADER] " + name + " en Alto (" + keyword + "): " + stats.Math + " ALU + " + stats.Branch + " en bloques condicionales, "
                + stats.Textures + " texturas");
            Assert.That(stats.Textures, Is.LessThanOrEqualTo(spec.MaxTextures + 1));
        }

        // ── It. 6b (06b-agua-avanzada/02-tech-design §4.1, §6; DT6b-22; AC-6B-06, -29) ──

        private static Shader LoadWaterAdvanced()
        {
            var path = "Assets/RowMate/" + ShaderArt.ShaderFolder + "/WaterAdvanced.shader";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader, Is.Not.Null, "falta " + path);
            return shader;
        }

        private static string[] WaterKeywords(WaterTierBudget budget)
        {
            return budget.Keyword.Length == 0 ? new string[0] : new[] { budget.Keyword };
        }

        // Las tres variantes (Alto 110/6, Medio 70/5, Lite 35/2) con su salto degradado ≤ MaxBranchAlu (D3D11).
        [TestCase(RowMate.Core.Scene.QualityTier.High), TestCase(RowMate.Core.Scene.QualityTier.Medium), TestCase(RowMate.Core.Scene.QualityTier.Low)]
        [Category("AC-6B-29")]
        public void WaterAdvancedFitsItsBudget(RowMate.Core.Scene.QualityTier tier)
        {
            Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "la medida es la del código de Direct3D 11");
            var budget = Array.Find(ShaderArt.WaterTiers, t => t.Tier == tier);
            var spec = ShaderArt.FindWater("WaterAdvanced");
            var pass = FindForward(ShaderUtil.GetShaderData(LoadWaterAdvanced()));
            Assert.That(pass, Is.Not.Null, "sin pasada UniversalForward");
            var info = pass!.CompileVariant(ShaderType.Fragment, WaterKeywords(budget), ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64, true);
            Assert.That(info.Success, Is.True, "no compila");
            Assert.That(DxbcStats.TryRead(info.ShaderData, out var stats, out var why), Is.True, why);
            Debug.Log("[SHADER] WaterAdvanced " + tier + ": " + stats.Math + " ALU (tope " + budget.MaxAlu + ") + " + stats.Branch
                + " en bloques condicionales (tope " + spec.MaxBranchAlu + "), " + stats.Textures + " texturas (tope " + budget.MaxTextures + ")");
            if (stats.Math > budget.MaxAlu || stats.Branch > spec.MaxBranchAlu || stats.Textures > budget.MaxTextures)
            {
                Debug.Log("[SHADER] WaterAdvanced " + tier + " (D3D11), el desensamblado:\n" + stats.Disassembly);
            }

            Assert.That(stats.Math, Is.LessThanOrEqualTo(budget.MaxAlu), tier + ": ALU");
            Assert.That(stats.Branch, Is.LessThanOrEqualTo(spec.MaxBranchAlu), tier + ": el salto degradado");
            Assert.That(stats.Textures, Is.LessThanOrEqualTo(budget.MaxTextures), tier + ": texturas");
        }

        // DT6b-22 (G6b-01): sin Fallback y con el tag RmWater = Advanced en el SubShader activo de las tres variantes, en la cola 2450.
        [Test, Category("AC-6B-06")]
        public void WaterAdvancedHasNoFallbackAndIsTagged()
        {
            var shader = LoadWaterAdvanced();
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, "con errores");
            Assert.That(shader.isSupported, Is.True);
            Assert.That(shader.renderQueue, Is.EqualTo(ShaderArt.FindWater("WaterAdvanced").Queue));
            foreach (var budget in ShaderArt.WaterTiers)
            {
                var material = new Material(shader);
                try
                {
                    foreach (var keyword in WaterKeywords(budget))
                    {
                        material.EnableKeyword(keyword);
                    }

                    Assert.That(material.GetTag(ShaderArt.WaterTag, false), Is.EqualTo(ShaderArt.WaterTagValue), budget.Tier.ToString());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }
        }

        [TestCase(ShaderCompilerPlatform.D3D)]
        [TestCase(ShaderCompilerPlatform.OpenGLCore)]
        [TestCase(ShaderCompilerPlatform.Vulkan)]
        [TestCase(ShaderCompilerPlatform.GLES3x)]
        public void TheWaterAndGlassShadersCompileFor(ShaderCompilerPlatform platform)
        {
            if (platform == ShaderCompilerPlatform.D3D)
            {
                Assume.That(Application.platform, Is.EqualTo(RuntimePlatform.WindowsEditor), "el compilador de Direct3D es de Windows");
            }

            var target = platform == ShaderCompilerPlatform.GLES3x ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
            var failures = new StringBuilder();
            var water = ShaderUtil.GetShaderData(LoadWaterAdvanced()).GetSubshader(0).GetPass(0);
            foreach (var budget in ShaderArt.WaterTiers)
            {
                foreach (var type in new[] { ShaderType.Vertex, ShaderType.Fragment })
                {
                    var info = water.CompileVariant(type, WaterKeywords(budget), platform, target);
                    if (!info.Success)
                    {
                        failures.Append("WaterAdvanced ").Append(budget.Tier).Append(' ').Append(type).Append(": ");
                        foreach (var message in info.Messages)
                        {
                            failures.Append(message.message).Append(' ');
                        }

                        failures.AppendLine();
                    }
                }
            }

            var blur = AssetDatabase.LoadAssetAtPath<Shader>("Assets/RowMate/" + ShaderArt.HudBlurFolder + "/HudBlur.shader");
            Assert.That(blur, Is.Not.Null, "falta HudBlur");
            var data = ShaderUtil.GetShaderData(blur).GetSubshader(0);
            Assert.That(data.PassCount, Is.EqualTo(3), "bajada, Kawase y composición: 3 SetPass");
            for (var p = 0; p < data.PassCount; p++)
            {
                foreach (var type in new[] { ShaderType.Vertex, ShaderType.Fragment })
                {
                    var info = data.GetPass(p).CompileVariant(type, new string[0], platform, target);
                    if (!info.Success)
                    {
                        failures.Append("HudBlur ").Append(p).Append(' ').Append(type).AppendLine();
                    }
                }
            }

            Assert.That(failures.ToString(), Is.Empty, "no compilan para " + platform);
        }

        private static ShaderData.Pass? FindForward(ShaderData data)
        {
            var subshader = data.GetSubshader(0);
            for (var p = 0; p < subshader.PassCount; p++)
            {
                var pass = subshader.GetPass(p);
                if (pass.FindTagValue(new ShaderTagId("LightMode")).name == "UniversalForward")
                {
                    return pass;
                }
            }

            return null;
        }

        // Las instrucciones de un contenedor DXBC, contadas en su desensamblado (D3DDisassemble de d3dcompiler_47, el de Windows: Unity
        // le saca al DXBC la tabla STAT): las de textura (sample*, ld*, gather*, resinfo, lod), las de control de flujo (if, else,
        // endif, loop, break, discard, ret…) y el resto, que son las de cálculo (también mov y movc, como cuenta el "Compiled code").
        private readonly struct DxbcStats
        {
            private static readonly string[] TexturePrefixes = { "sample", "ld", "gather", "resinfo", "lod" };

            private static readonly string[] FlowOps =
            {
                "if_z", "if_nz", "else", "endif", "loop", "endloop", "break", "breakc_z", "breakc_nz", "continue", "continuec_z", "continuec_nz",
                "ret", "retc_z", "retc_nz", "discard_z", "discard_nz", "switch", "case", "default", "endswitch", "call", "callc_z", "callc_nz",
                "label",
            };

            private DxbcStats(int math, int branch, int textures, int flow, string disassembly)
            {
                Math = math;
                Branch = branch;
                Textures = textures;
                Flow = flow;
                Disassembly = disassembly;
            }

            // Las de cálculo fuera de los bloques condicionales (lo que paga cada píxel) y adentro (solo los que entran).
            public int Math { get; }
            public int Branch { get; }
            public int Textures { get; }
            public int Flow { get; }
            public string Disassembly { get; }

            public static bool TryRead(byte[] data, out DxbcStats stats, out string why)
            {
                stats = default;
                why = string.Empty;
                if (data == null || data.Length < 32)
                {
                    why = "sin código compilado";
                    return false;
                }

                var start = -1;
                for (var i = 0; i + 4 <= data.Length; i++)
                {
                    if (data[i] == 'D' && data[i + 1] == 'X' && data[i + 2] == 'B' && data[i + 3] == 'C')
                    {
                        start = i;
                        break;
                    }
                }

                if (start < 0)
                {
                    why = "sin contenedor DXBC (" + data.Length + " bytes)";
                    return false;
                }

                var size = BitConverter.ToInt32(data, start + 24);
                if (size <= 0 || start + size > data.Length)
                {
                    size = data.Length - start;
                }

                string text;
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    var pointer = handle.AddrOfPinnedObject() + start;
                    var hr = NativeMethods.D3DDisassemble(pointer, (UIntPtr)size, 0, null, out var blob);
                    if (hr != 0 || blob == null)
                    {
                        why = "D3DDisassemble falló (0x" + hr.ToString("X8") + ")";
                        return false;
                    }

                    text = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(blob.GetBufferPointer(), (int)blob.GetBufferSize()) ?? string.Empty;
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(blob);
                }
                finally
                {
                    handle.Free();
                }

                int math = 0, branch = 0, textures = 0, flow = 0, depth = 0;
                foreach (var raw in text.Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("dcl_", StringComparison.Ordinal)
                        || line.StartsWith("ps_", StringComparison.Ordinal) || line.StartsWith("vs_", StringComparison.Ordinal) || line[0] == '{'
                        || line[0] == '}' || char.IsDigit(line[0]) || line[0] == '-')
                    {
                        continue;
                    }

                    var op = line.Split(' ', '\t')[0];
                    var bare = op.EndsWith("_sat", StringComparison.Ordinal) ? op.Substring(0, op.Length - 4) : op;
                    if (Array.IndexOf(FlowOps, bare) >= 0)
                    {
                        flow++;
                        if (bare.StartsWith("if_", StringComparison.Ordinal) || bare == "loop")
                        {
                            depth++;
                        }
                        else if ((bare == "endif" || bare == "endloop") && depth > 0)
                        {
                            depth--;
                        }
                    }
                    else if (Array.Exists(TexturePrefixes, p => bare.StartsWith(p, StringComparison.Ordinal)))
                    {
                        textures++;
                    }
                    else if (depth > 0)
                    {
                        branch++;
                    }
                    else
                    {
                        math++;
                    }
                }

                stats = new DxbcStats(math, branch, textures, flow, text);
                return true;
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("d3dcompiler_47.dll")]
            public static extern int D3DDisassemble(IntPtr source, UIntPtr size, uint flags,
                                                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string? comments,
                                                    out ID3DBlob? disassembly);
        }

        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        private interface ID3DBlob
        {
            [System.Runtime.InteropServices.PreserveSig]
            IntPtr GetBufferPointer();

            [System.Runtime.InteropServices.PreserveSig]
            UIntPtr GetBufferSize();
        }
    }
}
