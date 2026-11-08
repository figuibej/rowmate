using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // BRG · El puente que sigue y la sombra bajo el tablero (AC-SCN-16-03; DT8-07, DT8-08).
    [TestFixture]
    public sealed class BridgeTests
    {
        [Test, Category("AC-SCN-16-03")]
        public void TheBridgeTrailsUntilFifteenMetersBehindTheCamera()
        {
            var segments = new ShoreSegments(new RiverEnvironment());
            var into = new ShorePropBuffer(SceneFrame.TrailingCapacity);
            var visible = new ShorePropBuffer(120);
            // El puente de 270 m: en la lista del legacy hasta relZ 1; después, en TrailingLandmarks hasta relZ −15.
            segments.VisibleProps(268.5, visible);
            Assert.That(visible.Items.ToArray().Any(p => p.Kind == PropKind.Bridge), Is.True, "relZ 1,5: en la lista");
            Assert.That(segments.TrailingLandmarks(268.5, RiverArt.TrailingBehind, into), Is.EqualTo(0));
            segments.VisibleProps(269.5, visible);
            Assert.That(visible.Items.ToArray().Any(p => p.Kind == PropKind.Bridge), Is.False, "relZ 0,5: fuera de la lista (LT-101)");
            Assert.That(segments.TrailingLandmarks(269.5, RiverArt.TrailingBehind, into), Is.EqualTo(1));
            Assert.That(into[0].Z, Is.EqualTo(270));
            segments.VisibleProps(284.9, visible);
            Assert.That(segments.TrailingLandmarks(284.9, RiverArt.TrailingBehind, into), Is.EqualTo(1), "relZ −14,9");
            segments.VisibleProps(285.1, visible);
            Assert.That(segments.TrailingLandmarks(285.1, RiverArt.TrailingBehind, into), Is.EqualTo(0), "relZ −15,1");
        }

        [Test, Category("AC-SCN-16-03")]
        public void OcclusionIsContinuousAndDimsFortyPercent()
        {
            BridgeShade.DeckSpan(20, out var s, out var e);
            Assert.That((s, e), Is.EqualTo((20.0, 30.0)));
            Assert.That(BridgeShade.Occlusion(25, s, e), Is.EqualTo(1));
            Assert.That(BridgeShade.Occlusion(10, s, e), Is.EqualTo(0));
            Assert.That(BridgeShade.Occlusion(20, s, e), Is.EqualTo(0.5), "el borde, a mitad de la rampa de 1,5 m");
            Assert.That(BridgeShade.LightFactor(1), Is.EqualTo(0.6).Within(1e-12), "−40 %");
            var prev = BridgeShade.Occlusion(15, s, e);
            for (var z = 15.0; z <= 35; z += 0.01)
            {
                var o = BridgeShade.Occlusion(z, s, e);
                Assert.That(Math.Abs(o - prev), Is.LessThan(0.01 / 1.5 + 1e-9), "continua en " + z);
                prev = o;
            }
        }

        [Test, Category("AC-SCN-16-03")]
        public void TheSessionReportsTheNearestBridgeAndItsShadeOnTheBoat()
        {
            var s = new SceneSession(new RiverEnvironment(), SceneSessionOptions.Default);
            var frame = SceneFrame.Create();
            s.Open(0);
            s.ForceDistanceForTest(266);
            s.Tick(CoastSwellTests.Inputs(0.016), ref frame);
            Assert.That(frame.BridgeStartRelZ, Is.EqualTo(4).Within(1e-9));
            Assert.That(frame.BridgeEndRelZ, Is.EqualTo(14).Within(1e-9));
            Assert.That(frame.UnderBridgeAtBoat, Is.EqualTo(1), "el bote (relZ 9,5) bajo el tablero");
            var lake = new SceneSession(new LakeEnvironment(), SceneSessionOptions.Default);
            var lf = SceneFrame.Create();
            lake.Open(0);
            lake.Tick(CoastSwellTests.Inputs(0.016), ref lf);
            Assert.That(double.IsNaN(lf.BridgeStartRelZ) && lf.UnderBridgeAtBoat == 0 && lf.TrailingProps.Count == 0, Is.True, "el lago no tiene puentes");
        }
    }

    // MTX · El texto del cartel (AC-SCN-18-02; DT8-04, DT8-15).
    [TestFixture]
    public sealed class MarkerTextTests
    {
        [TestCase(250, "250 m", Category = "AC-SCN-18-02")]
        [TestCase(1000, "1000 m", Category = "AC-SCN-18-02")]
        [TestCase(10000, "10000 m", Category = "AC-SCN-18-02")]
        [TestCase(0, "0 m", Category = "AC-SCN-18-02")]
        public void TheLegacyTextFitsTheBoard(int meters, string text)
        {
            Assert.That(DistanceMarkerText.Text(meters), Is.EqualTo(text), "'${seed} m' sin separador");
            Assert.That(MarkerGlyphs.WidthMeters(text.AsSpan()), Is.LessThanOrEqualTo(RegattaArt.MarkerTextMaxWidth + 1e-12));
            Assert.That(MarkerGlyphs.DigitHeightMeters(text.AsSpan()), Is.EqualTo(0.55).Within(1e-12), "el alto es siempre 0,55 m (DT8-04)");
            Assert.That(MarkerGlyphs.HorizontalScale(text.AsSpan()), Is.GreaterThan(0).And.LessThanOrEqualTo(1), "solo se comprime de costado");
            foreach (var c in text)
            {
                Assert.That(MarkerGlyphs.SevenSegments(c) != 0 || c == ' ', Is.True, "el respaldo de 7 segmentos tiene '" + c + "'");
            }
        }

        [Test, Category("AC-SCN-18-02")]
        public void FormatDoesNotAllocateAndCoversTheTwelveGlyphs()
        {
            var buffer = new char[DistanceMarkerText.MaxChars];
            Assert.That(DistanceMarkerText.Format(42195, buffer), Is.EqualTo(7));
            Assert.That(new string(buffer, 0, 7), Is.EqualTo("42195 m"));
            Assert.That(MarkerGlyphs.Chars.Length, Is.EqualTo(MarkerGlyphs.Count));
            for (var i = 0; i < MarkerGlyphs.Chars.Length; i++)
            {
                Assert.That(MarkerGlyphs.IndexOf(MarkerGlyphs.Chars[i]), Is.EqualTo(i));
            }

            Assert.That(MarkerGlyphs.SevenSegments('8'), Is.EqualTo(0x7F));
            Assert.Throws<ArgumentOutOfRangeException>(() => MarkerGlyphs.IndexOf('.'));
        }
    }

    // SGF · Las gaviotas (AC-SCN-19-04, -20-06; DV-89).
    [TestFixture]
    public sealed class SeagullFlightTests
    {
        [Test, Category("AC-SCN-19-04"), Category("AC-SCN-20-06")]
        public void TheLegacyFlightInAngles()
        {
            for (var i = 0; i < SeagullFlight.Count; i++)
            {
                SeagullFlight.Sample(i, 3.2, out var az, out var el, out var flap, out var alpha);
                SeagullFlight.Sample(i, 3.2, out var az2, out var el2, out _, out _);
                Assert.That((az, el), Is.EqualTo((az2, el2)), "determinista");
                // La x del legacy: ((t·(8 + 3i) + 170i) mod 471) − 30.
                var gx = (3.2 * (8 + 3 * i) + i * 170) % 471 - 30;
                Assert.That(az * 411 + 205.5, Is.EqualTo(gx).Within(1e-9));
                var gy = 365.6 * (0.35 + 0.1 * i) + Math.Sin(3.2 * 2 + i) * 4;
                Assert.That(el, Is.EqualTo(Math.Atan((365.6 - gy) / 411)).Within(1e-12));
                Assert.That(flap, Is.EqualTo(Math.Sin(3.2 * 6 + i)).Within(1e-12));
                Assert.That(alpha, Is.InRange(0.0, 1.0));
            }

            Assert.That(SeagullFlight.SpeedPxPerSecond(0), Is.EqualTo(8));
            Assert.That(SeagullFlight.SpeedPxPerSecond(4), Is.EqualTo(20));
            Assert.That(SeagullFlight.SizeScale(4), Is.EqualTo(9 / 5.0));
            Assert.That(SeagullFlight.Opacity(1), Is.EqualTo(0.9).Within(1e-12));
        }

        [Test, Category("AC-SCN-19-04")]
        public void LeftToRightAndBackWithAFadeNoJump()
        {
            for (var i = 0; i < SeagullFlight.Count; i++)
            {
                double prevAz = double.NaN;
                for (var t = 0.0; t < 60; t += 1 / 60.0)
                {
                    SeagullFlight.Sample(i, t, out var az, out _, out _, out var alpha);
                    if (!double.IsNaN(prevAz) && az < prevAz)
                    {
                        Assert.That(alpha, Is.LessThan(0.05), "vuelve por la izquierda invisible (gaviota " + i + ")");
                    }

                    prevAz = az;
                }
            }
        }
    }

    // SGK · Las gaviotas puestas en el mundo (QA-V8-01; AC-SCN-19-04, -20-06): SeagullFlock deja cada una a 90 m de la cámara en el
    // acimut (relativo al rumbo) y la elevación de SeagullFlight, con la envergadura de 5 px de TV × (5 + i)/5, el aleteo en la punta,
    // invisibles con alfa 0 y sin asignar por cuadro.
    [TestFixture]
    public sealed class SeagullFlockTests
    {
        private static SeagullInstance[] Sample(double t)
        {
            var gulls = new SeagullInstance[SeagullFlight.Count];
            for (var i = 0; i < gulls.Length; i++)
            {
                SeagullFlight.Sample(i, t, out var az, out var el, out var flap, out var alpha);
                gulls[i] = new SeagullInstance(az, el, flap, SeagullFlight.SizeScale(i), alpha);
            }

            return gulls;
        }

        [Test, Category("AC-SCN-19-04"), Category("AC-SCN-20-06")]
        public void EachGullIsWhereTheLegacyDrawsIt()
        {
            var flock = new SeagullFlock();
            var camera = new CameraPose(0.3, 2.2, -6, 4, 0, 0, 411, 365.6);
            var gulls = Sample(3.2);
            flock.Pose(gulls, camera);
            Assert.That(flock.VertexCount, Is.EqualTo(flock.PerGull * 5));
            for (var i = 0; i < gulls.Length; i++)
            {
                flock.Center(i, out var x, out var y, out var z);
                double dx = x - 0.3, dy = y - 2.2, dz = z + 6;
                Assert.That(Math.Sqrt(dx * dx + dy * dy + dz * dz), Is.EqualTo(SeagullFlock.Distance).Within(1e-9));
                Assert.That(Math.Atan2(dx, dz), Is.EqualTo(SceneMath.DegToRad(4) + gulls[i].AzimuthRad).Within(1e-9), "el acimut desde el rumbo");
                Assert.That(Math.Atan2(dy, Math.Sqrt(dx * dx + dz * dz)), Is.EqualTo(gulls[i].ElevationRad).Within(1e-9), "la elevación");
            }

            Assert.That(flock.Visible, Is.EqualTo(5));
            // La envergadura de la gaviota 0 sin aletear: 5 px de TV a 90 m (411·1,1/90 ≈ 5,02).
            var still = new[] { new SeagullInstance(0, 0.3, 0, 1, 1), default, default, default, default };
            flock.Pose(still, new CameraPose(0, 0, 0, 0, 0, 0, 411, 365.6));
            var minX = double.MaxValue;
            var maxX = double.MinValue;
            for (var v = 0; v < flock.PerGull; v++)
            {
                minX = Math.Min(minX, flock.Positions[v * 3]);
                maxX = Math.Max(maxX, flock.Positions[v * 3]);
            }

            Assert.That((maxX - minX) / SeagullFlock.Distance * 411, Is.EqualTo(5).Within(0.1));
            Assert.That(flock.Visible, Is.EqualTo(1), "con alfa 0 no se ven");
        }

        [Test, Category("AC-SCN-20-06")]
        public void TheWingTipsFlap()
        {
            var flock = new SeagullFlock();
            var camera = new CameraPose(0, 0, 0, 0, 0, 0, 411, 365.6);
            flock.Pose(new[] { new SeagullInstance(0, 0, 1, 1, 1), default, default, default, default }, camera);
            var up = TipY(flock);
            flock.Pose(new[] { new SeagullInstance(0, 0, -1, 1, 1), default, default, default, default }, camera);
            var down = TipY(flock);
            Assert.That(up - down, Is.EqualTo(2 * SeagullFlock.FlapTip).Within(1e-4), "la punta ± 0,25 m");
            Assert.That(flock.Indices.Length % 6, Is.EqualTo(0), "las dos caras");
        }

#if NET5_0_OR_GREATER
        [Test, Category("AC-SCN-01-12")]
        public void PoseDoesNotAllocate()
        {
            var flock = new SeagullFlock();
            var gulls = Sample(1);
            var camera = new CameraPose(0, 2, -6, 0, 0, 0, 411, 365.6);
            // Como CharStringsTests: calentar el JIT (con sus niveles) y quedarse con la menor de tres medidas.
            for (var i = 0; i < 3; i++)
            {
                PoseMany(flock, gulls, camera);
                System.Threading.Thread.Sleep(20);
            }

            var best = long.MaxValue;
            for (var i = 0; i < 3; i++)
            {
                best = Math.Min(best, PoseMany(flock, gulls, camera));
            }

            Assert.That(best, Is.EqualTo(0L));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long PoseMany(SeagullFlock flock, SeagullInstance[] gulls, CameraPose camera)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                flock.Pose(gulls, camera);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif

        // El y de la punta del ala derecha (el vértice con mayor x) de la gaviota 0.
        private static double TipY(SeagullFlock flock)
        {
            var best = 0;
            for (var v = 1; v < flock.PerGull; v++)
            {
                if (flock.Positions[v * 3] > flock.Positions[best * 3])
                {
                    best = v;
                }
            }

            return flock.Positions[best * 3 + 1];
        }
    }

    // CAT · El atlas del público (DA8-12; AC-SCN-20-06).
    [TestFixture]
    public sealed class CrowdAtlasTests
    {
        [Test, Category("AC-SCN-20-06")]
        public void TheAtlasIsTheSameInTwoRunsWithNineFilledCells()
        {
            var a = new byte[CrowdAtlas.ByteCount];
            var b = new byte[CrowdAtlas.ByteCount];
            CrowdAtlas.Bake(a);
            CrowdAtlas.Bake(b);
            Assert.That(a, Is.EqualTo(b));
            for (var cell = 0; cell < CrowdAtlas.Cells; cell++)
            {
                CrowdAtlas.CellOrigin(cell, out var x0, out var y0);
                var opaque = 0;
                for (var y = 0; y < CrowdAtlas.CellHeight; y++)
                {
                    for (var x = 0; x < CrowdAtlas.CellWidth; x++)
                    {
                        opaque += a[((y0 + y) * CrowdAtlas.Width + x0 + x) * 4 + 3] == 255 ? 1 : 0;
                    }
                }

                Assert.That(opaque, Is.InRange(800, 6000), "cuadro " + cell + " con una persona");
            }

            Assert.That(CrowdAtlas.Width * CrowdAtlas.Height, Is.EqualTo(512 * 256));
        }
    }

    // THB · La miniatura (AC-SCN-22-04; DT8-02, DT8-16).
    [TestFixture]
    public sealed class ThumbnailTests
    {
        [Test, Category("AC-SCN-22-04")]
        public void TheRuleIsTwelveOClockTwentyMetersAndOneHundredFifty()
        {
            Assert.That((ThumbnailRule.Hour, ThumbnailRule.Distance, ThumbnailRule.SceneTime, ThumbnailRule.NearRange), Is.EqualTo((12.0, 20.0, 0.0, 150.0)));
            Assert.That((ThumbnailRule.Width, ThumbnailRule.Height), Is.EqualTo((456, 288)));
            Assert.That(ThumbnailRule.Camera.HorizonY, Is.EqualTo(96 * 0.42).Within(1e-12), "apaisada: horizonte al 42 %");
            Assert.That(ThumbnailRule.Camera.Focal, Is.EqualTo(96), "focal = lado corto");
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var env = EnvironmentCatalog.Create(id);
                var visible = new ShorePropBuffer(env.MaxVisibleProps);
                new ShoreSegments(env).VisibleProps(ThumbnailRule.Distance, visible);
                var kept = new ShorePropBuffer(env.MaxVisibleProps);
                ThumbnailRule.Filter(visible.Items, kept);
                Assert.That(kept.Items.ToArray(), Is.EqualTo(visible.Items.ToArray().Where(p => p.Z < 170).ToArray()), id + ": z < 170, en orden");
            }

            // DT8-02: el puente de 270 m y la tribuna de 210 m no salen.
            Assert.That(ThumbnailRule.Keeps(new ShoreProp(0, 270, PropKind.Bridge)) || ThumbnailRule.Keeps(new ShoreProp(12, 210, PropKind.Grandstand)), Is.False);
        }

        [Test, Category("AC-SCN-22-04")]
        public void BoxDownsampleAveragesFourByFour()
        {
            var src = new byte[8 * 4 * 4];
            for (var i = 0; i < src.Length; i++)
            {
                src[i] = (byte)(i * 7 % 256);
            }

            var dst = new byte[2 * 1 * 4];
            BoxDownsample.Apply(src, 8, 4, 4, dst);
            for (var c = 0; c < 4; c++)
            {
                var sum = 0;
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        sum += src[(y * 8 + x) * 4 + c];
                    }
                }

                Assert.That(dst[c], Is.EqualTo((sum * 2 + 16) / 32), "canal " + c);
            }

            Assert.Throws<ArgumentException>(() => BoxDownsample.Apply(src, 8, 4, 3, dst));
        }

        [Test, Category("AC-SCN-22-04")]
        public void TheFingerprintChangesWithShadersAndIsStable()
        {
            var shaders = new[] { new KeyValuePair<string, string>("Scene/Facade.shader", "a\r\nb"), new KeyValuePair<string, string>("Scene/Glow.shader", "c") };
            var a = ThumbnailFingerprint.Compute(shaders);
            var b = ThumbnailFingerprint.Compute(shaders.Reverse());
            var lf = ThumbnailFingerprint.Compute(new[] { new KeyValuePair<string, string>("Scene/Facade.shader", "a\nb"), shaders[1] });
            var other = ThumbnailFingerprint.Compute(new[] { new KeyValuePair<string, string>("Scene/Facade.shader", "a\nB"), shaders[1] });
            Assert.That(a, Is.EqualTo(b), "el orden de entrada no importa");
            Assert.That(a, Is.EqualTo(lf), "fin de línea normalizado");
            Assert.That(a, Is.Not.EqualTo(other));
            Assert.That(a, Does.Match("^[0-9a-f]{16}$"));
            Assert.That(ThumbnailFingerprint.ArtConstants(), Does.Contain("RiverArt.WallX=9").And.Contain("HourLookTable.Default="));
        }

        [Test, Category("AC-SCN-22-04")]
        public void TheVersionedThumbnailsAreUpToDate()
        {
            // DT8-16: las miniaturas las hornea WP-B1 (Editor). Hasta que existan, esta prueba no tiene qué comparar.
            var json = RepoPaths.Combine("Assets/RowMate/Art/Textures/Thumbnails/thumbnails.json");
            if (!File.Exists(json))
            {
                Assert.Ignore("todavía no hay miniaturas horneadas (WP-B1): Art/Textures/Thumbnails/thumbnails.json");
            }

            var text = File.ReadAllText(json);
            var expected = ThumbnailFingerprint.Compute(ShaderSources());
            Assert.That(text, Does.Contain("\"fingerprint\": \"" + expected + "\""), "miniatura vieja: volver a hornear (tools/unity/bake-thumbnails.ps1)");
            foreach (var key in new[] { "lake", "river", "coast", "regatta" })
            {
                Assert.That(File.Exists(RepoPaths.Combine("Assets/RowMate/Art/Textures/Thumbnails/" + key + ".png")), Is.True, key + ".png");
            }
        }

        // El texto de Art/Shaders/** con la ruta relativa a Art/Shaders (con '/').
        internal static IEnumerable<KeyValuePair<string, string>> ShaderSources()
        {
            var root = RepoPaths.Combine("Assets/RowMate/Art/Shaders");
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.Ordinal))
                {
                    continue;
                }

                var rel = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
                yield return new KeyValuePair<string, string>(rel, File.ReadAllText(file));
            }
        }
    }
}
