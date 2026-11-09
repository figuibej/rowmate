using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // Los números del arte del pulsómetro real (It. 11; 00-arte-pulsometro §1-§5, §7; 02-tech-design §1.3; BleMotionSpecs y HeartGlyph).
    [TestFixture]
    public sealed class HrmMotionSpecsTests
    {
        // D11-05: el corazón del encabezado respira 0,40 ↔ 0,70 en 3 200 ms (ease.pulse); en reposo, el 0,40 del corazón apagado.
        [Test, Category("A11-03")]
        public void HeartBreathesBetweenFortyAndSeventy()
        {
            Assert.That(BleMotionSpecs.HrmBreathMinOpacity, Is.EqualTo(MotionSpecs.DimHeartOpacity), "arranca donde quedó el corazón apagado");
            Assert.That(BleMotionSpecs.HrmBreathOpacity(0), Is.EqualTo(0.40).Within(1e-9));
            Assert.That(BleMotionSpecs.HrmBreathOpacity(1600), Is.EqualTo(0.70).Within(1e-9));
            Assert.That(BleMotionSpecs.HrmBreathOpacity(3200), Is.EqualTo(0.40).Within(1e-9));
            for (var t = 0.0; t < 6400; t += 13)
            {
                Assert.That(BleMotionSpecs.HrmBreathOpacity(t), Is.InRange(0.40 - 1e-9, 0.70 + 1e-9));
            }

            Assert.That(BleMotionSpecs.HrmBreathOpacity(-50), Is.EqualTo(0.40).Within(1e-9), "antes del retardo, quieto en 0,40");
            Assert.That(BleMotionSpecs.HrmBreathDelayMs, Is.EqualTo(Tokens.DurFast.Milliseconds), "§2.3: respira desde los 160 del fundido");
        }

        // §1.1 y §2.2: la línea sin señal y las filas entran con la regla del empuje; la línea sale en 0-120 / 80-240.
        [Test, Category("A11-03"), Category("AC-HRM-05-03")]
        public void StaleLineAndRowsUseThePushRule()
        {
            Assert.That(BleMotionSpecs.HrmGrowEndMs, Is.EqualTo(160));
            Assert.That(BleMotionSpecs.HrmFadeStartMs, Is.EqualTo(60));
            Assert.That(BleMotionSpecs.HrmFadeEndMs, Is.EqualTo(260));
            Assert.That(BleMotionSpecs.HrmOutFadeEndMs, Is.EqualTo(120));
            Assert.That(BleMotionSpecs.HrmOutHeightStartMs, Is.EqualTo(80));
            Assert.That(BleMotionSpecs.HrmOutHeightEndMs, Is.EqualTo(240));
            Assert.That(BleMotionSpecs.HrmRowStaggerMs, Is.EqualTo(35));
            Assert.That(BleMotionSpecs.HrmRowDelayMs(0), Is.EqualTo(0));
            Assert.That(BleMotionSpecs.HrmRowDelayMs(2), Is.EqualTo(70));
            Assert.That(BleMotionSpecs.HrmRowDelayMs(30), Is.EqualTo(245), "8 como máximo");
            Assert.That(BleMotionSpecs.HrmRowHeightDp, Is.EqualTo(56));
            Assert.That(BleMotionSpecs.HrmFollowMs, Is.EqualTo(240));
        }

        // A11-04 y §5: un latido al entrar el aviso "Pulsómetro conectado": sube a +0,16 en 0-90 y vuelve en 90-290.
        [Test, Category("A11-04"), Category("AC-HRM-07-05")]
        public void ToastHeartBeatsOnce()
        {
            Assert.That(BleMotionSpecs.HeartBeatScaleAt(0), Is.EqualTo(1).Within(1e-9));
            Assert.That(BleMotionSpecs.HeartBeatScaleAt(90), Is.EqualTo(1.16).Within(1e-9));
            Assert.That(BleMotionSpecs.HeartBeatScaleAt(290), Is.EqualTo(1).Within(1e-9));
            Assert.That(BleMotionSpecs.HeartBeatScaleAt(5000), Is.EqualTo(1).Within(1e-9), "un solo latido");
            Assert.That(BleMotionSpecs.HeartBeatScaleAt(-1), Is.EqualTo(1).Within(1e-9));
            for (var t = 0.0; t <= 290; t += 7)
            {
                Assert.That(BleMotionSpecs.HeartBeatScaleAt(t), Is.InRange(1 - 1e-9, 1.16 + 1e-9));
            }
        }

        // El favorite de Material (24 × 24) y su versión rota: dos mitades con la grieta compartida y separadas por el hueco.
        [Test, Category("A11-04")]
        public void HeartGlyphIsTheMaterialFavorite()
        {
            HeartGlyph.Bounds(out var minX, out var minY, out var maxX, out var maxY);
            Assert.That(minX, Is.EqualTo(2).Within(1e-9));
            Assert.That(maxX, Is.EqualTo(22).Within(1e-9));
            Assert.That(minY, Is.EqualTo(3).Within(1e-9));
            Assert.That(maxY, Is.EqualTo(21.35).Within(1e-9));
            Assert.That(HeartGlyph.Segments, Is.EqualTo(HeartGlyph.LeftContour.Length / 6));
            var lastX = HeartGlyph.LeftContour[HeartGlyph.LeftContour.Length - 2];
            var lastY = HeartGlyph.LeftContour[HeartGlyph.LeftContour.Length - 1];
            Assert.That(lastX, Is.EqualTo(HeartGlyph.TipX), "el contorno izquierdo termina en la punta");
            Assert.That(lastY, Is.EqualTo(HeartGlyph.TipY));
            Assert.That(HeartGlyph.MirrorX(HeartGlyph.NotchX), Is.EqualTo(HeartGlyph.NotchX), "simétrico");
        }

        [Test, Category("A11-04")]
        public void BrokenHeartCrackGoesFromNotchToTip()
        {
            var crack = HeartGlyph.Crack;
            Assert.That(crack.Length % 2, Is.EqualTo(0));
            Assert.That(crack[0], Is.EqualTo(HeartGlyph.NotchX));
            Assert.That(crack[1], Is.EqualTo(HeartGlyph.NotchY));
            Assert.That(crack[crack.Length - 2], Is.EqualTo(HeartGlyph.TipX));
            Assert.That(crack[crack.Length - 1], Is.EqualTo(HeartGlyph.TipY));
            for (var i = 3; i < crack.Length; i += 2)
            {
                Assert.That(crack[i], Is.GreaterThan(crack[i - 2]), "la grieta baja siempre");
                Assert.That(crack[i - 1], Is.InRange(9.0, 15.0), "cerca del centro");
            }

            Assert.That(HeartGlyph.BrokenGap, Is.InRange(0.5, 1.5), "un hueco visible a 16 dp sin desarmar el corazón");
        }
    }
}
