using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WMD · El contrato del agua del legacy en números (06b-agua-avanzada/02-tech-design §3.1; AC-6B-10, -13, -15; water.frag:55-98,
    // water_painter.dart:32-60). Lo que el agua avanzada tiene que dar con la normal plana.
    [TestFixture]
    public sealed class WaterModelTests
    {
        [Test, Category("AC-6B-10"), Category("AC-6B-36")]
        public void TheLegacyContractNumbers()
        {
            // Color base lerp(near; far; S(2; 120; z)): a 61 m, la mitad.
            var near = new SceneColor(0, 0, 0);
            var far = new SceneColor(1, 1, 1);
            Assert.That(WaterModel.BaseColor(near, far, 61, 0, 0.25).R, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(WaterModel.BaseColor(near, far, 2, 0, 0.25).R, Is.EqualTo(0).Within(1e-12));
            Assert.That(WaterModel.BaseColor(near, far, 120, 0, 0.25).R, Is.EqualTo(1).Within(1e-12));
            // × (1 + h·A·0,25) (water.frag:68).
            Assert.That(WaterModel.BaseColor(near, far, 120, 1, 0.8).R, Is.EqualTo(1.2).Within(1e-12));

            // Peso del reflejo 0,6·(1 − 2,2/√(z² + 2,2²))³.
            Assert.That(WaterModel.ReflectionWeight(5), Is.EqualTo(0.128).Within(0.005));
            Assert.That(WaterModel.ReflectionWeight(10), Is.EqualTo(0.290).Within(0.005));
            Assert.That(WaterModel.ReflectionWeight(20), Is.EqualTo(0.424).Within(0.005));
            Assert.That(WaterModel.ReflectionWeight(100), Is.EqualTo(0.561).Within(0.005));
            Assert.That(WaterModel.ReflectionWeight(0), Is.EqualTo(0).Within(1e-12), "mirando abajo, nada de espejo");

            // Niebla S(80; 400; z).
            Assert.That(WaterModel.FogWeight(80), Is.EqualTo(0));
            Assert.That(WaterModel.FogWeight(240), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(WaterModel.FogWeight(400), Is.EqualTo(1));

            // Estela: clamp(v/4; 0; 1); nace en la popa a 5,3 m (9,5 − 4,2); semiancho 0,3 + 0,35·retroceso (19,3°); espuma ≤ 35 %.
            Assert.That(WaterModel.WakeIntensity(2), Is.EqualTo(0.5));
            Assert.That(WaterModel.WakeIntensity(4.67), Is.EqualTo(1));
            Assert.That(WaterModel.WakeIntensity(0), Is.EqualTo(0));
            Assert.That(WaterModel.WakeIntensity(-1), Is.EqualTo(0));
            Assert.That(WaterModel.WakeIntensity(double.NaN), Is.EqualTo(0));
            Assert.That(WaterModel.SternRelZ, Is.EqualTo(5.3).Within(1e-12));
            Assert.That(WaterModel.WakeHalfWidth(5.3), Is.EqualTo(2.155).Within(1e-9));
            Assert.That(SceneMath.RadToDeg(Math.Atan(WaterModel.WakeHalfWidthSlope)), Is.EqualTo(19.3).Within(0.05), "el ángulo de Kelvin");
            Assert.That(WaterModel.WakeFoamMax, Is.EqualTo(0.35));

            // La columna de brillo: 25 + 4·z dp, peso 0,4 × la fuerza de la luz (0,2-1,0 de día, 0,45 de noche).
            Assert.That(WaterModel.ColumnWidthDp(10), Is.EqualTo(65));
            Assert.That(WaterModel.ColumnWeight(0, 10, 1), Is.EqualTo(0.4).Within(1e-12));
            Assert.That(WaterModel.ColumnWeight(0, 10, 0.45), Is.EqualTo(0.18).Within(1e-12));
            Assert.That(WaterModel.ColumnWeight(65, 10, 1), Is.EqualTo(0.4 * Math.Exp(-1)).Within(1e-12));

            // Las filas en el horizonte o medio dp debajo, transparentes.
            Assert.That(WaterModel.IsTransparentRow(365.6, 365.6), Is.True);
            Assert.That(WaterModel.IsTransparentRow(366.1, 365.6), Is.True);
            Assert.That(WaterModel.IsTransparentRow(366.2, 365.6), Is.False);
            Assert.That(WaterModel.RowDepth(365.6 + 411 * 2.2 / 61, 365.6, 411), Is.EqualTo(61).Within(1e-9));
        }

        [Test, Category("AC-6B-15")]
        public void FogWeightCurve()
        {
            var last = -1.0;
            for (var z = 0.0; z <= 600; z += 0.5)
            {
                var f = WaterModel.FogWeight(z);
                Assert.That(f, Is.GreaterThanOrEqualTo(last), "creciente en z " + z);
                Assert.That(f, Is.InRange(0.0, 1.0));
                last = f;
            }

            Assert.That(WaterModel.FogWeight(300), Is.EqualTo(0.77).Within(0.01), "la fila de z ≈ 300 m de la sonda fogRow (02-tech-design §8)");
        }

        [Test, Category("AC-6B-15")]
        public void FogReachesFogColorAt400()
        {
            // A 400 m el agua es EXACTAMENTE el color de la niebla de la hora, antes de la costura (water.frag:96).
            foreach (var hour in new[] { 6.0, 12.0, 18.0, 22.0 })
            {
                var p = SceneTimeOfDay.PaletteFor(hour);
                var col = WaterModel.BaseColor(p.WaterNear, p.WaterFar, 400, 0.7, 0.8);
                var fogged = WaterModel.ApplyFog(col, p.Fog, 400);
                Assert.That(fogged, Is.EqualTo(p.Fog), "a las " + hour + " h");
                Assert.That(WaterModel.ApplyFog(col, p.Fog, 80), Is.EqualTo(col), "a 80 m, sin niebla");
            }
        }

        [Test, Category("AC-6B-13")]
        public void ColumnFollowsAstroAndFades()
        {
            // La columna está centrada bajo la x del astro y decae simétrica; con la luz en 0 (el cambio de astro, DV-59), nada.
            for (var z = 5.0; z <= 80; z += 5)
            {
                var peak = WaterModel.ColumnWeight(0, z, 0.8);
                Assert.That(WaterModel.ColumnWeight(-30, z, 0.8), Is.EqualTo(WaterModel.ColumnWeight(30, z, 0.8)).Within(1e-15));
                Assert.That(WaterModel.ColumnWeight(30, z, 0.8), Is.LessThan(peak));
                Assert.That(WaterModel.ColumnWeight(0, z, 0), Is.EqualTo(0));
            }

            // Más ancha cuanto más lejos (25 + 4z dp): a 40 dp del centro pesa más a 80 m que a 5 m.
            Assert.That(WaterModel.ColumnWeight(40, 80, 1), Is.GreaterThan(WaterModel.ColumnWeight(40, 5, 1)));
        }
    }
}
