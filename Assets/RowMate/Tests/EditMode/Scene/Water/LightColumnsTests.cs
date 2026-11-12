using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // LCL · Las columnas de luz de la noche (#31; AC-6B-18; 00-arte-agua §7.1-§7.3): faroles, ventanas a < 60 m (las 16 más cercanas),
    // cabaña, torre y el faro a ± 20°; el alfa sigue al nivel de su luz; de día ninguna; en Lite, las 8 más cercanas.
    [TestFixture]
    public sealed class LightColumnsTests
    {
        private static readonly DateTimeOffset Local = new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-3));

        private static SceneFrame At(EnvironmentId id, double distance, double hour)
        {
            var s = new SceneSession(EnvironmentCatalog.Create(id), SceneSessionOptions.Default);
            s.Open(0);
            s.ForceDistanceForTest(distance);
            s.ForceHourForTest(hour);
            var frame = SceneFrame.Create();
            var w = new SceneInputs(125, 24, true);
            for (var i = 0; i < 3; i++)
            {
                s.Tick(new SceneTickInputs(i / 60.0, Local, Local.ToUniversalTime(), w, null, false, 411, 914, 0), ref frame);
            }

            return frame;
        }

        private static SceneFrame Manual(double distance)
        {
            var frame = SceneFrame.Create();
            frame.Distance = distance;
            return frame;
        }

        private static void Add(ref SceneFrame frame, in ShoreProp prop, double level)
        {
            frame.PropLight[frame.VisibleProps.Count] = level;
            frame.VisibleProps.Add(prop);
        }

        [Test, Category("AC-6B-18")]
        public void NightOnly()
        {
            var day = At(EnvironmentId.River, 250, 12);
            Assert.That(day.Water.Columns.Count, Is.EqualTo(0), "de día no hay ninguna");
            var night = At(EnvironmentId.River, 250, 22);
            Assert.That(night.Water.Columns.Count, Is.GreaterThan(0), "de noche, los faroles y las ventanas");
            var lamps = 0;
            for (var i = 0; i < night.Water.Columns.Count; i++)
            {
                var c = night.Water.Columns[i];
                lamps += Math.Abs(c.Width - WaterLookArt.LampColumnWidth) < 1e-12 ? 1 : 0;
                Assert.That(c.RelZ, Is.GreaterThan(SceneCamera.MinZ));
                Assert.That(c.Alpha, Is.GreaterThan(0));
            }

            Assert.That(lamps, Is.GreaterThan(0), "los faroles del río");
        }

        [Test, Category("AC-6B-18")]
        public void FollowsLightLevel()
        {
            var columns = new LightColumns();
            var frame = Manual(100);
            Add(ref frame, new ShoreProp(-9, 130, PropKind.Lamp), 0.5);
            Add(ref frame, new ShoreProp(9, 140, PropKind.Pine), 1);
            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.EqualTo(1), "solo lo que se enciende");
            Assert.That(columns[0].Alpha, Is.EqualTo(WaterLookArt.LampColumnAlpha * 0.5).Within(1e-12), "el alfa sigue al fundido de su luz");
            Assert.That(columns[0].X, Is.EqualTo(-9));
            Assert.That(columns[0].RelZ, Is.EqualTo(30).Within(1e-12));
            Assert.That(columns[0].Length, Is.EqualTo(WaterLookArt.LampColumnLength));

            frame.PropLight[0] = 0;
            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.EqualTo(0), "apagada, sin columna");
        }

        [Test, Category("AC-6B-18")]
        public void NearestWindows()
        {
            var columns = new LightColumns();
            var frame = Manual(0);
            for (var k = 0; k < 12; k++)
            {
                Add(ref frame, new ShoreProp(k % 2 == 0 ? -14 : 14, 8 + 10 * k, PropKind.Building, 1, k), 1);
            }

            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.InRange(1, WaterLookArt.WindowColumnMax), "las 16 más cercanas, como mucho");
            var last = 0.0;
            for (var i = 0; i < columns.Count; i++)
            {
                Assert.That(columns[i].RelZ, Is.LessThan(WaterLookArt.WindowColumnRange), "a menos de 60 m");
                Assert.That(columns[i].RelZ, Is.GreaterThanOrEqualTo(last), "de la más cercana a la más lejana");
                Assert.That(Math.Abs(columns[i].X), Is.LessThan(14), "en la cara que mira al agua");
                last = columns[i].RelZ;
            }
        }

        [Test, Category("AC-6B-18")]
        public void LiteKeepsTheEightNearest()
        {
            var columns = new LightColumns();
            var frame = Manual(0);
            for (var k = 0; k < 40; k++)
            {
                Add(ref frame, new ShoreProp(-9, 300 - 7 * k, PropKind.Lamp), 1);
            }

            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.EqualTo(LightColumns.Capacity), "como mucho 32");
            columns.Fill(frame, QualityTier.Low);
            Assert.That(columns.Count, Is.EqualTo(WaterLookArt.LiteColumnMax));
            Assert.That(columns[7].RelZ, Is.EqualTo(300 - 7 * 39 + 7 * 7).Within(1e-9), "las más cercanas");
        }

        [Test, Category("AC-6B-18")]
        public void TheBeaconOnlyWhenItFacesTheCamera()
        {
            // El haz gira con un período de 12 s (dos medios haces, como el shader Glow); la columna, cuando apunta a ± 20° de la cámara.
            const double x = -30, relZ = 100;
            var toCamera = Math.Atan2(-relZ, -x);
            var facingTime = toCamera / (2 * Math.PI) * CoastArt.BeamPeriodSeconds;
            if (facingTime < 0)
            {
                facingTime += CoastArt.BeamPeriodSeconds;
            }

            Assert.That(LightColumns.BeaconFacing(facingTime, x, relZ), Is.EqualTo(1).Within(1e-9));
            Assert.That(LightColumns.BeaconFacing(facingTime + CoastArt.BeamPeriodSeconds / 2, x, relZ), Is.EqualTo(1).Within(1e-9), "el otro medio haz");
            Assert.That(LightColumns.BeaconFacing(facingTime + CoastArt.BeamPeriodSeconds / 4, x, relZ), Is.EqualTo(0), "de costado, nada");
            var off = 21.0 / 360 * CoastArt.BeamPeriodSeconds;
            Assert.That(LightColumns.BeaconFacing(facingTime + off, x, relZ), Is.EqualTo(0), "a más de 20°, nada");

            var columns = new LightColumns();
            var frame = Manual(0);
            frame.SceneTime = facingTime;
            Add(ref frame, new ShoreProp(x, relZ, PropKind.Lighthouse), 1);
            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.EqualTo(1));
            Assert.That(columns[0].Width, Is.EqualTo(WaterLookArt.BeaconColumnWidth));
            frame.SceneTime = facingTime + CoastArt.BeamPeriodSeconds / 4;
            columns.Fill(frame, QualityTier.High);
            Assert.That(columns.Count, Is.EqualTo(0));
        }
    }
}
