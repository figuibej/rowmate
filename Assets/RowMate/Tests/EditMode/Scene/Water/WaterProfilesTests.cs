using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WPF · Cada escenario con su agua (06b-agua-avanzada/02-tech-design §3.1; 00-arte-agua §3.2, §4.2; AC-6B-11; DV-150): la amplitud
    // y la escala del generador del legacy, el carácter, la corriente del río, el mar de fondo solo en la costa y las bandas de orilla.
    [TestFixture]
    public sealed class WaterProfilesTests
    {
        [TestCase(EnvironmentId.Lake, WaterLook.Mirror, 0.25, 2.5, TestName = "AC-6B-11 · lago 0,25 / 2,5 Espejo")]
        [TestCase(EnvironmentId.River, WaterLook.Current, 0.35, 2.0, TestName = "AC-6B-11 · río 0,35 / 2,0 Corriente")]
        [TestCase(EnvironmentId.Coast, WaterLook.Sea, 0.8, 4.0, TestName = "AC-6B-11 · costa 0,8 / 4,0 Mar")]
        [TestCase(EnvironmentId.Regatta, WaterLook.Track, 0.3, 1.5, TestName = "AC-6B-11 · regata 0,3 / 1,5 Pista")]
        [Category("AC-6B-11")]
        public void EachEnvironmentHasTheLegacyWave(EnvironmentId id, WaterLook look, double amplitude, double scale)
        {
            var p = WaterProfiles.For(id);
            Assert.That(p.Environment, Is.EqualTo(id));
            Assert.That(p.Look, Is.EqualTo(look));
            Assert.That(p.Amplitude, Is.EqualTo(amplitude));
            Assert.That(p.Scale, Is.EqualTo(scale));
            var env = EnvironmentCatalog.Create(id);
            Assert.That(p.Amplitude, Is.EqualTo(env.WaveAmplitude), "la del generador del legacy");
            Assert.That(p.Scale, Is.EqualTo(env.WaveScale));
            Assert.That(p.Swell, Is.EqualTo(id == EnvironmentId.Coast), "B0 solo en la costa (la misma regla que el casco)");
            Assert.That(p.Swell, Is.EqualTo(EnvironmentCatalog.HasSwell(env)));
            Assert.That(p.CurrentMps, Is.EqualTo(id == EnvironmentId.River ? 0.25 : 0), "la corriente del río hacia la cámara");
            Assert.That(p.B2.Tile, Is.GreaterThan(p.B3.Tile), "el chop es más largo que el rizo");
            Assert.That(p.Left.Present, Is.True);
            Assert.That(p.Right.Present, Is.EqualTo(id != EnvironmentId.Coast), "la costa a la derecha es mar abierto");
        }

        [Test, Category("AC-6B-11")]
        public void TheCharacterOrdersRegattaLakeRiverCoast()
        {
            // La variación del agua cercana ordena regata ≤ lago < río < costa (la sonda de las capturas de las 12 h): la fuerza del chop y la
            // rugosidad, y la regata más espejo (menor distorsión).
            var lake = WaterProfiles.For(EnvironmentId.Lake);
            var river = WaterProfiles.For(EnvironmentId.River);
            var coast = WaterProfiles.For(EnvironmentId.Coast);
            var regatta = WaterProfiles.For(EnvironmentId.Regatta);
            Assert.That(regatta.B2.Strength, Is.LessThanOrEqualTo(lake.B2.Strength));
            Assert.That(lake.B2.Strength, Is.LessThan(river.B2.Strength));
            Assert.That(river.B2.Strength, Is.LessThan(coast.B2.Strength));
            Assert.That(regatta.Roughness, Is.LessThanOrEqualTo(lake.Roughness));
            Assert.That(lake.Roughness, Is.LessThan(river.Roughness));
            Assert.That(river.Roughness, Is.LessThan(coast.Roughness));
            Assert.That(regatta.Distortion, Is.LessThan(lake.Distortion));
            Assert.That(lake.CalmMin, Is.LessThan(regatta.CalmMin), "el lago tiene las calmas de espejo más hondas");
        }

        [Test, Category("AC-6B-11")]
        public void TheRiverRunsTowardTheCameraAndTheNumbersAreTheArtOnes()
        {
            var river = WaterProfiles.For(EnvironmentId.River);
            Assert.That(river.B2.DirectionDeg, Is.EqualTo(180), "el chop del río viaja con la corriente, hacia la cámara");
            Assert.That(river.B2.DirZ, Is.EqualTo(-1).Within(1e-12));
            Assert.That(river.B2.DirX, Is.EqualTo(0).Within(1e-12));
            var lake = WaterProfiles.For(EnvironmentId.Lake);
            Assert.That(lake.B1Wavelength, Is.EqualTo(15.7).Within(0.01), "λ₁ del lago, 2π·2,5");
            Assert.That(lake.B1CrestSpeed, Is.EqualTo(1.5).Within(1e-12), "la cresta de B1 a 0,6·s m/s");
            Assert.That(lake.B2.Tile, Is.EqualTo(WaterLookArt.LakeB2Tile));
            Assert.That(lake.Roughness, Is.EqualTo(WaterLookArt.LakeRoughness));
            Assert.That(lake.Left.X, Is.EqualTo(14));
            Assert.That(lake.Left.Height, Is.EqualTo(9));
            Assert.That(lake.Left.Color, Is.EqualTo(SceneColor.FromArgb(0xFF2F4A3A)), "el bosque");
            var right = lake.Right.Color;
            var meadow = SceneColor.FromArgb(0xFF4F7A3A);
            Assert.That(right.G, Is.EqualTo(meadow.G * 0.8).Within(1e-12), "la pradera × 0,8");
            Assert.That(river.Left.HasLow && river.Right.HasLow, Is.True, "el muro del río, de los dos lados");
            Assert.That(WaterProfiles.For(EnvironmentId.Coast).Right.Covers(0, 10), Is.False, "sin orilla, nada que reflejar");
        }

        [Test, Category("AC-6B-11")]
        public void TheShoreBandCoversLowRays()
        {
            // La banda: el rayo reflejado cruza |x| = x_s a la distancia t; si su elevación es menor que atan(H_s/t), toma el tono.
            var band = WaterProfiles.For(EnvironmentId.Lake).Left;
            var t = 40.0;
            var top = SceneMath.RadToDeg(Math.Atan(band.Height / t));
            Assert.That(band.Covers(top - 0.1, t), Is.True);
            Assert.That(band.Covers(top + 0.1, t), Is.False);
            Assert.That(band.Covers(1, 0), Is.False);
        }
    }
}
