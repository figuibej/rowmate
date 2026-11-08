using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // ESC · Lo que la It. 8 suma a los shaders de la escena (08-entornos-selector/02-tech-design §8.4; 04-design-notes; WP-D1), leído del
    // texto (corre en dotnet y en Unity sin GPU; el costo lo mide SceneShaderTests en Unity):
    // - Glow y Foam (ShaderArt.Environment): nombre, cola (3010 aditivo; 3020 la espuma), sin profundidad, instanciados, sus propiedades;
    // - la luz bajo el puente es la de BridgeShade (rampa centrada en el borde, fuerza del uniforme) y la usan el casco, el remero, los
    //   remos, las boyas, los objetos, el terreno y el agua; la ola es la de CoastSwell (los mismos coeficientes);
    // - la vida de banderas, sombrillas y boyas: la marca en el uv que arma KitSegmentMesh (lo demás, quieto);
    // - todo lo nuevo corre en un salto por un uniforme que en el lago vale 0: las tomas de la 6a y la 7 no cambian.
    [TestFixture]
    public sealed class EnvironmentShaderContractTests
    {
        private static string Code(string name, bool include = false)
        {
            var folder = include ? ShaderArt.IncludeFolder : ShaderArt.ShaderFolder;
            var path = RepoPaths.Combine("Assets", "RowMate", folder, name + (include ? ".hlsl" : ".shader"));
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            var text = File.ReadAllText(path);
            var noBlock = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(noBlock, @"//[^\n]*", " ");
        }

        private static SceneShaderSpec Spec(string name)
        {
            var spec = Array.Find(ShaderArt.Environment, s => s.Name == name);
            Assert.That(spec, Is.Not.Null, name + " en ShaderArt.Environment");
            return spec!;
        }

        // Lo común a Glow y Foam: instanciados, transparentes sin profundidad, target 3.5, sus propiedades y sin el reloj de Unity.
        private static string AssertInstancedTransparent(SceneShaderSpec spec)
        {
            var code = Code(spec.Name);
            Assert.That(code, Does.Match(@"Shader\s+""RowMate/Scene/" + spec.Name + @""""));
            Assert.That(code, Does.Match(@"\bZWrite\s+Off\b"), spec.Name);
            Assert.That(code, Does.Match(@"#pragma\s+multi_compile_instancing"), spec.Name);
            Assert.That(code, Does.Match(@"#pragma\s+target\s+3\.5"), spec.Name);
            Assert.That(code, Does.Contain("Include/RowMateScene.hlsl"), spec.Name);
            Assert.That(code, Does.Contain("CBUFFER_START(UnityPerMaterial)"), spec.Name);
            foreach (var property in spec.Properties)
            {
                Assert.That(Regex.IsMatch(code, @"\b" + property + @"\s*\("), Is.True, spec.Name + ": " + property + " en Properties");
            }

            foreach (var property in spec.InstanceProperties)
            {
                Assert.That(Regex.IsMatch(code, @"UNITY_DEFINE_INSTANCED_PROP\s*\(\s*float4\s*,\s*" + property + @"\s*\)"), Is.True, spec.Name + ": " + property);
            }

            // Con el tiempo de escena (sigue en pausa), nunca con el reloj de Unity.
            Assert.That(code, Does.Contain("_RmSceneTime"), spec.Name);
            Assert.That(Regex.IsMatch(code, @"\b_Time\b"), Is.False, spec.Name);
            return code;
        }

        [Test, Category("AC-SCN-20-04")]
        public void GlowIsAnAdditiveInstancedShaderWithItsContract()
        {
            Assert.That(ShaderArt.Environment, Has.Length.EqualTo(2));
            var spec = Spec("Glow");
            Assert.That(spec.FullName, Is.EqualTo("RowMate/Scene/Glow"));
            var code = AssertInstancedTransparent(spec);
            Assert.That(code, Does.Match(@"""Queue""\s*=\s*""Transparent\+10"""), "cola 3010");
            Assert.That(spec.Queue, Is.EqualTo(3010));
            Assert.That(code, Does.Match(@"\bBlend\s+One\s+One\b"), "aditivo");
        }

        // La espuma de la costa (00-arte-entornos §3.3, §3.4): transparente después de los halos, con el ruido de la escena.
        [Test, Category("AC-SCN-12-01")]
        public void FoamIsATransparentInstancedShaderWithItsContract()
        {
            var spec = Spec("Foam");
            Assert.That(spec.FullName, Is.EqualTo("RowMate/Scene/Foam"));
            var code = AssertInstancedTransparent(spec);
            Assert.That(code, Does.Match(@"""Queue""\s*=\s*""Transparent\+20"""), "cola 3020");
            Assert.That(spec.Queue, Is.EqualTo(3020));
            Assert.That(spec.Queue, Is.GreaterThan(Spec("Glow").Queue));
            Assert.That(code, Does.Match(@"\bBlend\s+SrcAlpha\s+OneMinusSrcAlpha\b"));
            Assert.That(code, Does.Contain("_RmNoiseTex"));
        }

        [Test, Category("AC-SCN-16-03")]
        public void TheLightUnderTheBridgeIsTheCoreOne()
        {
            var lighting = Code("RowMateLighting", true);
            // BridgeShade.Occlusion: saturate(mín(z − zIni; zFin − z)/rampa + 0,5).
            Assert.That(lighting, Does.Match(@"min\(\s*positionWS\.z\s*-\s*b\.x\s*,\s*b\.y\s*-\s*positionWS\.z\s*\)"));
            Assert.That(lighting, Does.Match(@"saturate\(\s*inside\s*/\s*max\(\s*b\.z\s*,\s*1e-3\s*\)\s*\+\s*0\.5\s*\)"));
            Assert.That(lighting, Does.Match(@"1\.0\s*-\s*_RmUnderBridge\.w\s*\*\s*RmUnderBridgeOcclusion"), "luz × (1 − fuerza·o)");
            Assert.That(lighting, Does.Match(@"if\s*\(\s*_RmUnderBridge\.w\s*>\s*0\.0\s*\)"), "sin puente: el factor queda en 1");
            Assert.That(BridgeShade.Occlusion(0, 0, 10), Is.EqualTo(0.5), "la rampa centrada en el borde, como el shader");
            Assert.That(BridgeShade.Strength, Is.EqualTo(RiverArt.UnderBridgeDim));
            foreach (var shader in new[] { "Hull", "Rower", "Oar", "Buoy", "Prop", "Terrain", "WaterDegraded" })
            {
                Assert.That(Code(shader), Does.Contain("RmUnderBridgeGamma2("), shader + " se oscurece bajo el puente");
            }
        }

        [Test, Category("AC-SCN-12-01")]
        public void TheSwellOnTheWaterIsTheCoreOne()
        {
            var water = Code("WaterDegraded");
            // CoastSwell.HeightAt(x, relZ, s, φ₁, φ₂) = sin(relZ/s + φ₁)·0,6 + sin((0,7·relZ + 1,3·x)/s + φ₂)·0,4; × (1 + h·A·0,25).
            Assert.That(water, Does.Match(
                @"sin\(\s*pw\.z\s*/\s*swellScale\s*\+\s*_RmSwell\.z\s*\)\s*\*\s*0\.6\s*\+\s*sin\(\s*\(\s*0\.7\s*\*\s*pw\.z\s*\+\s*1\.3\s*\*\s*pw\.x\s*\)\s*/\s*swellScale\s*\+\s*_RmSwell\.w\s*\)\s*\*\s*0\.4"));
            Assert.That(water, Does.Match(@"1\.0\s*\+\s*swell\s*\*\s*\(\s*_RmSwell\.x\s*\*\s*kRmSwellColorGain"));
            Assert.That(water, Does.Match(@"if\s*\(\s*_RmSwell\.x\s*>\s*0\.0\s*\)"), "fuera de la costa no corre");
            Assert.That(CoastSwell.HeightAt(0, 0, 4, 0, 0), Is.EqualTo(0));
            Assert.That(CoastSwell.ColorGain, Is.EqualTo(0.25));
        }

        [Test]
        public void TheLakeDoesNotPayForTheNewEnvironments()
        {
            // Terreno: los suelos nuevos solo con _RmTerrainKind > 0,5 (el lago es 0, el valor por defecto del material).
            Assert.That(Code("Terrain"), Does.Match(@"if\s*\(\s*_RmTerrainKind\s*>\s*0\.5\s*\)"));
            Assert.That(ShaderArt.MaterialDefaults()["Terrain._RmTerrainKind"][0], Is.EqualTo(0));
            // Horizonte: las ventanas del skyline solo con _RmSkylineWindows (0 por defecto; 1 el río) y de noche.
            Assert.That(Code("Horizon"), Does.Match(@"if\s*\(\s*_RmSkylineWindows\s*>\s*0\.5\s*\)"));
            Assert.That(ShaderArt.MaterialDefaults()["Horizon._RmSkylineWindows"][0], Is.EqualTo(0));
            // Los presupuestos: lo de la 6a queda igual salvo las comparaciones de los saltos (agua + 3, horizonte + 1) y lo nuevo va en
            // bloques condicionales.
            Assert.That(ShaderArt.Find("WaterDegraded").MaxAlu, Is.EqualTo(27));
            Assert.That(ShaderArt.Find("Terrain").MaxAlu, Is.EqualTo(35));
            Assert.That(ShaderArt.Find("Prop").MaxAlu, Is.EqualTo(30));
            Assert.That(ShaderArt.Find("Horizon").MaxAlu, Is.EqualTo(16));
        }

        [Test, Category("AC-SCN-20-04")]
        public void TheSkylineWindowsUseTheLegacyGrid()
        {
            // river_environment.dart:58-66: ventanas de 3 × 4 px cada 8 × 7 px, desde 4 px del borde y 6 del techo, hasta 4 del horizonte;
            // columnas de 34 px; #FFE08A al 85 %; de noche (ambiente < 0,6); la mitad de las celdas.
            var c = ShaderArt.Constants();
            Assert.That(c["kRmSkylineColumnPx"], Is.EqualTo(34));
            Assert.That(c["kRmSkylineCellXPx"], Is.EqualTo(8));
            Assert.That(c["kRmSkylineCellYPx"], Is.EqualTo(7));
            Assert.That(c["kRmSkylineWindowXPx"], Is.EqualTo(3));
            Assert.That(c["kRmSkylineWindowYPx"], Is.EqualTo(4));
            Assert.That(c["kRmSkylineMaxPx"], Is.EqualTo(914 * 0.12).Within(1e-9));
            Assert.That(c["kRmSkylineNightAmbient"], Is.EqualTo(0.6));
            var horizon = Code("Horizon");
            Assert.That(horizon, Does.Match(@"xPx\s*-\s*column\s*\*\s*kRmSkylineColumnPx\s*-\s*4\.0"));
            Assert.That(horizon, Does.Match(@"topPx\s*-\s*yPx\s*-\s*6\.0"));
            Assert.That(horizon, Does.Match(@"step\(\s*4\.0\s*,\s*topPx\s*-\s*6\.0"));
        }

        // La vida (00-arte-entornos §1.4): banderas, sombrillas y boyas llevan la marca en el uv (uv.x < 0) y lo demás no.
        [Test, Category("AC-SCN-20-04")]
        public void FlagsUmbrellasAndBuoysAreMarkedToMove()
        {
            Assert.That(CountMarked(EnvironmentId.Regatta, new ShoreProp(10.5, 30, PropKind.Flag, 1.0, 1), KitSegmentMesh.MotionFlag), Is.GreaterThan(0));
            Assert.That(CountMarked(EnvironmentId.Regatta, new ShoreProp(3, 10, PropKind.LaneBuoy, 1.0, 0), KitSegmentMesh.MotionBuoy), Is.GreaterThan(0));
            Assert.That(CountMarked(EnvironmentId.Coast, new ShoreProp(-11, 110, PropKind.BeachUmbrella, 1.0, 2), KitSegmentMesh.MotionUmbrella), Is.GreaterThan(0));
            Assert.That(CountMarked(EnvironmentId.Regatta, new ShoreProp(-10.5, 0, PropKind.DistanceMarker, 1.0, 250), -1), Is.EqualTo(0), "el cartel no se mueve");
        }

        // QA-V8-01 (AC-SCN-18-04, -20-06): el público se mueve apenas. Cada persona lleva la marca 7 con su peso por altura (0 los pies,
        // ~1 la cabeza) y en uv.y su período (en centésimas de s) y su fase; el shader lo mece y lo levanta con el tiempo de escena.
        [Test, Category("AC-SCN-18-04"), Category("AC-SCN-20-06")]
        public void TheCrowdIsMarkedToMove()
        {
            var kit = new KitSegmentMesh(EnvironmentId.Regatta);
            var mesh = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            var y = RegattaArt.BankHeight + RegattaArt.StepHeight;
            var fill = new[] { new FillItem(FillKind.Spectator, 12.75, 210, y, RegattaArt.CrowdHeight, 21, 2.5, 0.25) };
            kit.Build(4, ReadOnlySpan<ShoreProp>.Empty, ReadOnlySpan<double>.Empty, ReadOnlySpan<HouseLayout>.Empty, fill, 0, mesh);
            var marked = 0;
            var top = 0.0;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                var u = mesh.Uv[v * 2];
                if (u < -6.5)
                {
                    marked++;
                    var w = -u - KitSegmentMesh.MotionCrowd;
                    Assert.That(w, Is.InRange(0.0, 1.0));
                    Assert.That(w, Is.EqualTo(Math.Min(0.999, (mesh.Positions[v * 3 + 1] - y) / RegattaArt.CrowdHeight)).Within(1e-4), "el peso es la altura");
                    Assert.That(Math.Floor(mesh.Uv[v * 2 + 1]), Is.EqualTo(250), "el período en centésimas");
                    Assert.That(mesh.Uv[v * 2 + 1] - 250, Is.EqualTo(0.25).Within(1e-3), "la fase en vueltas");
                    top = Math.Max(top, w);
                }
            }

            Assert.That(marked, Is.EqualTo(mesh.VertexCount), "toda la persona");
            Assert.That(top, Is.GreaterThan(0.9), "la cabeza se mueve más que los pies");
            var prop = Code("Prop");
            Assert.That(prop, Does.Contain("code >= 6.5"));
            Assert.That(prop, Does.Contain("kRmCrowdSway * w * sin(a)"));
            Assert.That(prop, Does.Contain("kRmCrowdBob * w * max(sin(2.0 * a), 0.0)"));
        }

        // Cuántos vértices del segmento llevan la marca `motion` (−1: cualquier marca).
        private static int CountMarked(EnvironmentId environment, ShoreProp prop, int motion)
        {
            var kit = new KitSegmentMesh(environment);
            var mesh = new MeshData(KitSegmentMesh.VertexCapacity, KitSegmentMesh.IndexCapacity);
            var k = (long)Math.Floor(prop.Z / ShoreEnvironment.SegmentLength);
            kit.Build(k, new[] { prop }, new[] { PropPlacement.FootY(environment, prop) }, ReadOnlySpan<HouseLayout>.Empty, ReadOnlySpan<FillItem>.Empty, 0, mesh);
            var count = 0;
            for (var v = 0; v < mesh.VertexCount; v++)
            {
                var u = mesh.Uv[v * 2];
                if (u < -0.5 && (motion < 0 || Math.Floor(-u + 1e-9) == motion))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
