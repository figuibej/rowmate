using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene
{
    // MAT · Los números de los materiales de la escena (04-design-notes §5.5, §7.3 a §7.6, §8.3, §9, §10.1, §10.3, §12.3, §15.2;
    // WP-D1): la aparición y el tramado de los objetos (DV-62, DN6a-24), el fin del pasto por nivel, el viento, el modelo de luz y
    // la niebla. Los shaders leen estos mismos números (SceneShaderContractTests los compara).
    [TestFixture]
    public sealed class MaterialArtTests
    {
        [Test, Category("AC-SCN-14-07")]
        public void ObjectsAppearBetweenFourHundredAndThreeSixtyMeters()
        {
            Assert.That(MaterialArt.InstanceFade(400), Is.EqualTo(0), "en el borde de la lista del legacy, invisible");
            Assert.That(MaterialArt.InstanceFade(401), Is.EqualTo(0));
            Assert.That(MaterialArt.InstanceFade(360), Is.EqualTo(1));
            Assert.That(MaterialArt.InstanceFade(100), Is.EqualTo(1));
            Assert.That(MaterialArt.InstanceFade(380), Is.EqualTo(0.5).Within(1e-12), "simétrica en la mitad");
            var prev = MaterialArt.InstanceFade(400);
            for (var z = 399.75; z >= 360; z -= 0.25)
            {
                var f = MaterialArt.InstanceFade(z);
                Assert.That(f, Is.GreaterThanOrEqualTo(prev), "nunca de golpe: monótona al acercarse");
                Assert.That(f - prev, Is.LessThan(0.02), "un cuadro a 5 m/s (8 cm) cambia menos de 2 %");
                prev = f;
            }
        }

        [Test, Category("AC-SCN-02-03")]
        public void NearTheCameraTheyDitherOutFromTwoToOneMeter()
        {
            // DV-62 corregida: del contrato [d + 1; d + 400], sin banda propia.
            Assert.That(MaterialArt.InstanceFade(2), Is.EqualTo(1));
            Assert.That(MaterialArt.InstanceFade(1), Is.EqualTo(0));
            Assert.That(MaterialArt.InstanceFade(1.5), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(MaterialArt.InstanceFade(0.5), Is.EqualTo(0));
        }

        [Test]
        public void LodChangesAreComplementaryInTenPercentOfTheLimit()
        {
            // DN6a-24: 36-44 m alrededor de 40; el que entra dibuja exactamente lo que el que sale deja (RowMateDither).
            Assert.That(MaterialArt.LodWeight(35, 40), Is.EqualTo(1));
            Assert.That(MaterialArt.LodWeight(36, 40), Is.EqualTo(1));
            Assert.That(MaterialArt.LodWeight(40, 40), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(MaterialArt.LodWeight(44, 40), Is.EqualTo(0));
            Assert.That(MaterialArt.LodWeight(400, 400), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(MaterialArt.LodCrossFraction, Is.EqualTo(0.10));
            // El par de fundidos que va al shader: el cercano con w, el lejano con w − 1 (el patrón complementario).
            MaterialArt.LodFadePair(38, 40, out var near, out var far);
            Assert.That(near, Is.EqualTo(MaterialArt.LodWeight(38, 40)));
            Assert.That(far, Is.EqualTo(near - 1));
            Assert.That(far, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void TheDitherPatternIsTheFixedBayer()
        {
            // Bayer 4 × 4 fijo en pantalla (RowMateDither.hlsl): los 16 umbrales son (k + 0,5)/16, cada uno una vez.
            var seen = new bool[16];
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var t = MaterialArt.BayerThreshold(x, y);
                    var k = (int)Math.Round(t * 16 - 0.5);
                    Assert.That(t, Is.EqualTo((k + 0.5) / 16).Within(1e-12));
                    Assert.That(seen[k], Is.False, "umbral repetido");
                    seen[k] = true;
                    Assert.That(MaterialArt.BayerThreshold(x + 4, y + 8), Is.EqualTo(t), "se repite cada 4 px");
                }
            }

            // Un fundido f deja ver la fracción f de los píxeles; el complementario (f − 1) deja ver exactamente el resto.
            for (var f = 0.0; f <= 1.0; f += 0.0625)
            {
                var shown = 0;
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var a = MaterialArt.DitherKeeps(f, x, y);
                        var b = MaterialArt.DitherKeeps(f - 1, x, y);
                        Assert.That(a ^ b, Is.True, "uno y solo uno de los dos LOD en cada píxel");
                        shown += a ? 1 : 0;
                    }
                }

                Assert.That(shown, Is.EqualTo((int)Math.Round(f * 16)), "f = " + f);
            }
        }

        [Test]
        public void GrassEndsByTierAndShrinksInTheLastFifth()
        {
            Assert.That(MaterialArt.GrassEnd(QualityTier.High), Is.EqualTo(60));
            Assert.That(MaterialArt.GrassEnd(QualityTier.Medium), Is.EqualTo(40));
            Assert.That(MaterialArt.GrassEnd(QualityTier.Low), Is.EqualTo(30));
            Assert.That(MaterialArt.GrassScale(10, 40), Is.EqualTo(1));
            Assert.That(MaterialArt.GrassScale(32, 40), Is.EqualTo(1), "hasta el 80 %");
            Assert.That(MaterialArt.GrassScale(36, 40), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(MaterialArt.GrassScale(40, 40), Is.EqualTo(0));
            Assert.That(MaterialArt.GrassScale(45, 40), Is.EqualTo(0));
        }

        [Test]
        public void TheLowTierShortensTheDistances()
        {
            Assert.That(MaterialArt.TierDistance(40, QualityTier.High), Is.EqualTo(40));
            Assert.That(MaterialArt.TierDistance(40, QualityTier.Medium), Is.EqualTo(40));
            Assert.That(MaterialArt.TierDistance(40, QualityTier.Low), Is.EqualTo(28).Within(1e-12), "× 0,7");
        }

        [Test]
        public void TheWindIsOneWindToTheRight()
        {
            // 04-design-notes §5.5: copas 0,03·sin(1,1t + x·0,13) + ráfaga 0,012; juncos 0,06·sin(1,7t + z·0,4 + semilla) + ráfaga × 2;
            // pasto 0,08·sin(2,3t + x·0,9 + z·0,7); matas × 1,5.
            Assert.That(MaterialArt.PineWind, Is.EqualTo(new[] { 0.03, 1.1, 0.012, 9.0 }));
            Assert.That(MaterialArt.ReedWind, Is.EqualTo(new[] { 0.06, 1.7, 0.024, 9.0 }));
            Assert.That(MaterialArt.GrassWind, Is.EqualTo(new[] { 0.08, 2.3, 0.0, 9.0 }));
            Assert.That(MaterialArt.MeadowWindScale, Is.EqualTo(1.5));
            // La fase por instancia (la pone quien instancia, en double y acotada a [0; 2π)): sin saltos a 42 km.
            var a = MaterialArt.PineWindPhase(12.5);
            Assert.That(a, Is.EqualTo(12.5 * 0.13).Within(1e-12));
            var far = MaterialArt.ReedWindPhase(42195.3, 7);
            Assert.That(far, Is.GreaterThanOrEqualTo(0).And.LessThan(2 * Math.PI));
            var next = MaterialArt.ReedWindPhase(42195.3 + 1e-3, 7);
            Assert.That(Math.Abs(next - far), Is.LessThan(1e-3).Or.GreaterThan(2 * Math.PI - 1e-3), "continua");
            Assert.That(MaterialArt.GrassWindPhase(1, 2), Is.EqualTo(0.9 + 1.4).Within(1e-12));
        }

        [Test, Category("AC-SCN-20-01")]
        public void TheLightingModelKeepsTheLegacyTintAsTheMean()
        {
            // 04-design-notes §10.1 (con WP-D2): luz(N) = T(a) ⊙ [0,80·Hemi(N) + 0,45·Kn·envolvente·sombra]. A las 12 h una cara al
            // astro ≈ 1,25 y una a la sombra ≈ 0,80 (el tinte del legacy es 1). El astro está siempre adelante (acimut ± 35°): lo que mira
            // a la cámara está casi siempre a la sombra, y con 0,65 la orilla de las tomas salía a la mitad del legacy.
            Assert.That(MaterialArt.HemiWeight, Is.EqualTo(HourLookTable.AmbientScale));
            Assert.That(MaterialArt.KeyWeight, Is.EqualTo(HourLookTable.KeyLightScale));
            Assert.That(MaterialArt.Wrap(1, MaterialArt.SolidWrap), Is.EqualTo(1));
            Assert.That(MaterialArt.Wrap(-MaterialArt.FoliageWrap, MaterialArt.FoliageWrap), Is.EqualTo(0));
            Assert.That(MaterialArt.Wrap(0, MaterialArt.FoliageWrap), Is.EqualTo(MaterialArt.FoliageWrap / (1 + MaterialArt.FoliageWrap)).Within(1e-12));
            var lit = MaterialArt.HemiWeight + MaterialArt.KeyWeight * 1.0;
            Assert.That(lit, Is.EqualTo(1.25).Within(1e-12));
            Assert.That(MaterialArt.HemiWeight, Is.EqualTo(0.80));
            Assert.That(MaterialArt.LegacyBlueLift, Is.EqualTo(0.12));
        }

        [Test, Category("AC-SCN-20-01")]
        public void TheFogOfTheShaderIsTheContractOne()
        {
            // RowMateFog.hlsl: 0,8·clamp((z − 80)/320) + 0,2·clamp((z − 400)/800): hasta 400 m, el _c del legacy.
            Assert.That(MaterialArt.FogParams, Is.EqualTo(new[] { 80.0, 320.0, 0.8, 1200.0 }));
            foreach (var z in new[] { 0.0, 80, 240, 400, 800, 1200, 2000 })
            {
                var shader = MaterialArt.FogParams[2] * SceneMath.Clamp01((z - MaterialArt.FogParams[0]) / MaterialArt.FogParams[1])
                    + (1 - MaterialArt.FogParams[2]) * SceneMath.Clamp01((z - 400) / (MaterialArt.FogParams[3] - 400));
                Assert.That(shader, Is.EqualTo(ShoreShading.ExtendedFog(z)).Within(1e-12), "z = " + z);
                if (z <= 400)
                {
                    Assert.That(shader, Is.EqualTo(ShoreShading.Fog(z)).Within(1e-12));
                }
            }
        }

        [Test]
        public void TheScreenScrimsAreTheHudLayerOnes()
        {
            // 04-design-notes §12.3: #06122A del 38 % a 0 en el 24 % de arriba y del 32 % a 0 en el 18 % de abajo.
            Assert.That(MaterialArt.ScrimColor, Is.EqualTo(0xFF06122Au));
            Assert.That(MaterialArt.ScrimTop, Is.EqualTo(new[] { 0.38, 0.24 }));
            Assert.That(MaterialArt.ScrimBottom, Is.EqualTo(new[] { 0.32, 0.18 }));
            Assert.That(MaterialArt.ScrimAlpha(8 / 914.0), Is.EqualTo(0.38 * (1 - (8 / 914.0) / 0.24)).Within(1e-12));
            Assert.That(MaterialArt.ScrimAlpha(0.5), Is.EqualTo(0));
            Assert.That(MaterialArt.ScrimAlpha(1), Is.EqualTo(0.32).Within(1e-12));
            Assert.That(MaterialArt.ScrimAlpha(0.82), Is.EqualTo(0).Within(1e-12), "no toca la zona del bote (termina en 0,654 / 0,802)");
        }

        [Test, Category("AC-SCN-13-03")]
        public void TheWaterLineConstantsReproduceTheLegacyLines()
        {
            // El shader WaterDegraded arma las líneas con WaterArt; tienen que dar lo mismo que DegradedWater (el contrato del oráculo).
            foreach (var d in new[] { 0.0, 3, 920, 1234, 4096, 42195 })
            {
                for (var i = 0; i < WaterArt.Lines; i++)
                {
                    var line = DegradedWater.Line(i, d, 390, 337.6, 506.4, 195, 0.85);
                    var t = DegradedWater.Phase(d) + ((i * WaterArt.LineSpacing + DegradedWater.Phase(d)) >= 1 ? i - 10 : i) * WaterArt.LineSpacing;
                    Assert.That(t, Is.EqualTo(line.T).Within(1e-9), "t de la línea " + i + " a " + d + " m");
                    Assert.That(390 * (WaterArt.LineHalfWidthBase + WaterArt.LineHalfWidthSlope * line.T), Is.EqualTo(line.HalfWidth).Within(1e-9));
                    Assert.That(WaterArt.ShimmerBaseAlpha + WaterArt.ShimmerStrengthAlpha * (1 - line.T) * 0.85, Is.EqualTo(line.Alpha).Within(1e-12));
                }
            }

            Assert.That(WaterArt.Lines * WaterArt.LineSpacing, Is.EqualTo(1).Within(1e-12), "10 líneas en t ∈ [0; 1)");
            Assert.That(WaterArt.GridX.Length, Is.GreaterThan(8));
            for (var i = 1; i < WaterArt.GridX.Length; i++)
            {
                Assert.That(WaterArt.GridX[i], Is.GreaterThan(WaterArt.GridX[i - 1]), "la grilla crece en x");
            }

            for (var i = 1; i < WaterArt.GridZ.Length; i++)
            {
                Assert.That(WaterArt.GridZ[i], Is.GreaterThan(WaterArt.GridZ[i - 1]), "la grilla crece en z");
            }

            Assert.That(WaterArt.GridX[0], Is.EqualTo(-WaterArt.PlaneWidth / 2));
            Assert.That(WaterArt.GridX[WaterArt.GridX.Length - 1], Is.EqualTo(WaterArt.PlaneWidth / 2));
            Assert.That(WaterArt.GridZ[0], Is.EqualTo(-WaterArt.PlaneLength / 2));
            Assert.That(WaterArt.GridZ[WaterArt.GridZ.Length - 1], Is.EqualTo(WaterArt.PlaneLength / 2));
            foreach (var x in new[] { WaterArt.ShallowInner, WaterArt.ShallowOuter })
            {
                Assert.That(Array.IndexOf(WaterArt.GridX, x), Is.GreaterThanOrEqualTo(0), "una columna en |x| = " + x);
                Assert.That(Array.IndexOf(WaterArt.GridX, -x), Is.GreaterThanOrEqualTo(0));
            }

            foreach (var z in new[] { WaterArt.MistNear, WaterArt.MistFar, WaterArt.ReflectNear, WaterArt.ReflectFar })
            {
                Assert.That(Array.IndexOf(WaterArt.GridZ, z), Is.GreaterThanOrEqualTo(0), "una fila en z = " + z);
            }
        }

        [Test]
        public void TheBoatShadowAndTheGelcoat()
        {
            Assert.That(MaterialArt.BoatShadow, Is.EqualTo(new[] { 8.6, 0.9, 0.35, 0.08 }));
            Assert.That(MaterialArt.BoatShadowFalloff, Is.EqualTo(4));
            Assert.That(MaterialArt.Gelcoat, Is.EqualTo(new[] { 0.85, 64.0, 0.3 }));
            // La sombra cae del lado opuesto al astro (contrato del legacy): con el sol a la izquierda (acimut < 0), a la derecha.
            Assert.That(MaterialArt.BoatShadowSide(-17.5), Is.EqualTo(1));
            Assert.That(MaterialArt.BoatShadowSide(17.5), Is.EqualTo(-1));
            Assert.That(MaterialArt.BoatShadowSide(0), Is.EqualTo(1));
        }

        // WP-D2 (B6a-22, punto 5): sin tonemapping (HourLookTable.NeutralTonemapping) lo que pasa de 1 se recorta, y el casco blanco
        // del mediodía salía como una silueta plana. La cubierta (normal hacia arriba) con el ambiente del cielo a la mitad de su matiz y la
        // luz clave del astro, × el gelcoat #F5F5F5 y la exposición del casco, queda blanca (0,80 a 0,95: el post de las 12 h suma saturación)
        // (en el espacio del legacy, donde 1 es el tinte), así se leen la cubierta, los costados y el especular.
        [Test, Category("AC-SCN-01-08")]
        public void TheHullDeckStaysWhiteBelowClippingAtNoon()
        {
            var hour = 12.0;
            var palette = SceneTimeOfDay.PaletteFor(hour);
            var sky = CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default);
            var look = HourLighting.Evaluate(hour, palette, sky, HourLookTable.Default);
            var a = palette.Ambient;
            var tint = new[] { a, a, Math.Min(1, a + MaterialArt.LegacyBlueLift) };
            var ambient = new[] { look.AmbientSky.R, look.AmbientSky.G, look.AmbientSky.B };
            var mean = (ambient[0] + ambient[1] + ambient[2]) / 3;
            var key = new[] { look.KeyColor.R, look.KeyColor.G, look.KeyColor.B };
            var nDotL = Math.Sin(SceneMath.DegToRad(sky.LightElevationDeg));
            var white = SceneColor.FromArgb(LakeArt.HullWhite).R;
            for (var c = 0; c < 3; c++)
            {
                var hemi = mean + (ambient[c] - mean) * MaterialArt.HullAmbientChroma;
                var light = hemi + tint[c] * key[c] * MaterialArt.KeyWeight * look.KeyIntensity / MaterialArt.NoonIntensity * nDotL;
                var display = white * light * MaterialArt.HullExposure;
                Assert.That(display, Is.InRange(0.80, 0.95), "canal " + c + " de la cubierta a las 12 h");
            }

            Assert.That(MaterialArt.HullAmbientChroma, Is.InRange(0.0, 1.0));
        }

        // WP-D2: la ventana encendida (#FFE082, el color de contrato) tiene el rojo en 1. Sin tonemapping, una ganancia de lo emisivo de
        // más de 1 lo recorta antes del balance de la noche y el matiz se corre al verde amarillo (114° en CAP-04 con 1,25, contra 85°-110°
        // de la sonda de aspecto de 04-design-notes §10.4): la ganancia × el canal más alto no pasa de 1 (el bloom sale del umbral de la
        // noche, 0,8).
        [Test, Category("AC-SCN-09-06")]
        public void TheLitWindowDoesNotClipItsHue()
        {
            var window = SceneColor.FromArgb(LakeArt.WindowNight);
            var max = Math.Max(window.R, Math.Max(window.G, window.B));
            if (!HourLookTable.NeutralTonemapping)
            {
                Assert.That(SkyArt.EmissiveGain * max, Is.LessThanOrEqualTo(1.0));
            }

            Assert.That(SkyArt.EmissiveGain, Is.GreaterThanOrEqualTo(1.0), "nunca más oscura que su color de contrato");
            var night = HourLookTable.Default.Evaluate(22);
            Assert.That(SkyArt.EmissiveGain * max, Is.GreaterThan(night.BloomThreshold), "la ventana pasa el umbral del bloom de la noche");
        }
    }

    // STX · Las recetas de las texturas procedurales que hornea WP-B1 por filas al precargar (04-design-notes §4.3, §5.2, §5.4,
    // §7.5; DN6a-18): el atlas de nubes con las manchas del legacy (sky_painter.dart:88-121), la luna, el ruido del terreno y la capa
    // lejana. Deterministas, en [0; 1] y, las que se repiten, sin costura.
    [TestFixture]
    public sealed class SkyTexturesTests
    {
        // La posición en el atlas (u, v de la celda) de un punto de la nube en unidades de su tamaño s (y hacia abajo).
        private static void Cell(double xs, double ys, out double u, out double v)
        {
            u = xs / (2 * SkyTextures.CloudQuadHalfWidth) + 0.5;
            v = (ys - SkyTextures.CloudQuadTop) / (SkyTextures.CloudQuadBottom - SkyTextures.CloudQuadTop);
        }

        [Test, Category("AC-SCN-11-06")]
        public void TheCloudQuadHoldsTheLegacyBlobs()
        {
            // La base de 2,6·s y el borde de los copos (y − 0,8·s) y de la sombra (y + 0,65·s) entran en el quad (DN6a-26: x ± 1,3·s).
            Assert.That(SkyTextures.CloudQuadHalfWidth, Is.GreaterThanOrEqualTo(1.3));
            Assert.That(SkyTextures.CloudQuadTop, Is.LessThanOrEqualTo(-0.8));
            Assert.That(SkyTextures.CloudQuadBottom, Is.GreaterThanOrEqualTo(0.65));
            Assert.That(SkyTextures.CloudAtlasWidth, Is.EqualTo(2 * SkyTextures.CloudCellWidth));
            Assert.That(SkyTextures.CloudAtlasHeight, Is.EqualTo(2 * SkyTextures.CloudCellHeight));
            Assert.That(SkyTextures.CloudVariants, Is.EqualTo(4));
        }

        [TestCase(0, Category = "AC-SCN-11-06")]
        [TestCase(1, Category = "AC-SCN-11-06")]
        [TestCase(2, Category = "AC-SCN-11-06")]
        [TestCase(3, Category = "AC-SCN-11-06")]
        public void EachVariantHasTheLegacyStructure(int variant)
        {
            // Opaco en el centro de los tres copos y de la base, transparente fuera de todo, la sombra solo abajo.
            foreach (var p in new[] { (-0.6, 0.0), (0.0, -0.15), (0.7, 0.05), (0.0, 0.25) })
            {
                Cell(p.Item1, p.Item2, out var u, out var v);
                SkyTextures.CloudTexel(variant, u, v, out var body, out _);
                Assert.That(body, Is.GreaterThan(0.9), "opaco en (" + p.Item1 + "; " + p.Item2 + ")·s");
            }

            foreach (var p in new[] { (-1.38, -0.8), (1.38, -0.8), (0.0, -0.84), (-1.38, 0.69), (1.38, 0.69) })
            {
                Cell(p.Item1, p.Item2, out var u, out var v);
                SkyTextures.CloudTexel(variant, u, v, out var body, out var shade);
                Assert.That(body, Is.LessThan(0.02), "transparente en la esquina (" + p.Item1 + "; " + p.Item2 + ")·s");
                Assert.That(shade, Is.LessThan(0.02));
            }

            Cell(0, 0.5, out var su, out var sv);
            SkyTextures.CloudTexel(variant, su, sv, out _, out var center);
            Assert.That(center, Is.GreaterThan(0.9), "la sombra del legacy: elipse de 2,0·s × 0,3·s en y + 0,5·s");
            Cell(0, -0.15, out var tu, out var tv);
            SkyTextures.CloudTexel(variant, tu, tv, out _, out var top);
            Assert.That(top, Is.EqualTo(0), "sin sombra arriba");
        }

        [Test]
        public void TheVariantsDifferAndTheCellsTile()
        {
            double diff = 0;
            for (var i = 0; i < 64; i++)
            {
                var u = (i + 0.5) / 64;
                SkyTextures.CloudTexel(0, u, 0.3, out var a, out _);
                SkyTextures.CloudTexel(1, u, 0.3, out var b, out _);
                diff += Math.Abs(a - b);
                Assert.That(a, Is.InRange(0.0, 1.0));
            }

            Assert.That(diff, Is.GreaterThan(0.5), "cuatro variantes distintas, no la misma nube");
            SkyTextures.CloudAtlasCell(3, out var x0, out var y0);
            Assert.That(x0, Is.EqualTo(SkyTextures.CloudCellWidth));
            Assert.That(y0, Is.EqualTo(SkyTextures.CloudCellHeight));
            SkyTextures.CloudTexel(2, 0.37, 0.41, out var once, out _);
            SkyTextures.CloudTexel(2, 0.37, 0.41, out var twice, out _);
            Assert.That(once, Is.EqualTo(twice), "determinista");
        }

        [Test]
        public void TheNoiseTilesWithoutSeams()
        {
            var n = SkyTextures.NoiseSize;
            for (var i = 0; i < n; i += 7)
            {
                for (var c = 0; c < 4; c++)
                {
                    var a = SkyTextures.Noise(c, 0, i);
                    var b = SkyTextures.Noise(c, n - 1, i);
                    var x = SkyTextures.Noise(c, n, i);
                    Assert.That(x, Is.EqualTo(a).Within(1e-12), "periódico en x");
                    if (c != 2)
                    {
                        // Los cantos (B) tienen juntas en cualquier lado, también en la costura; los demás canales son continuos.
                        Assert.That(Math.Abs(a - b), Is.LessThan(0.35), "vecinos parecidos en la costura (canal " + c + ")");
                    }

                    Assert.That(SkyTextures.Noise(c, i, n), Is.EqualTo(SkyTextures.Noise(c, i, 0)).Within(1e-12), "periódico en y");
                    Assert.That(a, Is.InRange(0.0, 1.0));
                }
            }
        }

        [Test]
        public void TheMoonHasItsMaria()
        {
            Assert.That(SkyTextures.MoonSize, Is.EqualTo(256));
            double min = 1, max = 0;
            for (var y = -0.95; y <= 0.95; y += 0.05)
            {
                for (var x = -0.95; x <= 0.95; x += 0.05)
                {
                    if (x * x + y * y > 0.9)
                    {
                        continue;
                    }

                    var a = SkyTextures.MoonAlbedo(x, y);
                    min = Math.Min(min, a);
                    max = Math.Max(max, a);
                }
            }

            Assert.That(min, Is.InRange(SkyArt.MoonMariaMin - 0.05, SkyArt.MoonMariaMax), "los mares (WP-D2: claros, a la escala del disco)");
            Assert.That(max, Is.InRange(SkyArt.MoonMariaMax, 1.0), "las tierras altas");
            Assert.That(max - min, Is.GreaterThan(0.12), "los mares se distinguen");
        }

        [Test]
        public void TheFarLayerTilesInAzimuth()
        {
            var w = SkyTextures.CloudLayerWidth;
            double covered = 0;
            var count = 0;
            for (var y = 0; y < SkyTextures.CloudLayerHeight; y += 16)
            {
                Assert.That(SkyTextures.CloudLayer(w, y), Is.EqualTo(SkyTextures.CloudLayer(0, y)).Within(1e-12), "sin costura al dar la vuelta");
                for (var x = 0; x < w; x += 16)
                {
                    var d = SkyTextures.CloudLayer(x, y);
                    Assert.That(d, Is.InRange(0.0, 1.0));
                    covered += d > 0.5 ? 1 : 0;
                    count++;
                }
            }

            Assert.That(covered / count, Is.EqualTo(SkyArt.CloudLayerCoverage).Within(0.12), "cobertura ≈ 0,35");
        }
    }
}
