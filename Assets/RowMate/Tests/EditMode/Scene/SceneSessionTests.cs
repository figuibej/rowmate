using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SSN · El agregado de la escena (06a-escena-lago/02-tech-design §2.15; AC-SCN-01-03, AC-SCN-03-05, AC-SCN-01-04, AC-SCN-01-12):
    // arranca de cero, no se reinicia al cambiar la ventana ni al pausar, el tiempo sigue en pausa, Advance en pasos y la
    // distancia de escena no llega al motor.
    [TestFixture]
    public sealed class SceneSessionTests
    {
        private static readonly DateTimeOffset Local = new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-3));
        private static readonly SceneInputs R4 = new SceneInputs(125, 24, true);
        private static readonly SceneInputs Paused = new SceneInputs(125, 24, false);

        private static SceneTickInputs Inputs(double now, in SceneInputs w, double width = 411, double height = 914, int? forced = null,
                                              DateTimeOffset? local = null, bool reduce = false, double veil = 0)
        {
            var l = local ?? Local;
            return new SceneTickInputs(now, l, l.ToUniversalTime(), w, forced, reduce, width, height, veil);
        }

        private static SceneSession Open(double now = 0)
        {
            var s = new SceneSession(new LakeEnvironment(), SceneSessionOptions.Default);
            s.Open(now);
            return s;
        }

        [Test, Category("AC-SCN-01-03")]
        public void ItStartsFromZeroAndANewSessionInheritsNothing()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Advance(Inputs(0, R4), 20, ref frame);
            Assert.That(frame.Distance, Is.GreaterThan(40));
            s.Open(30);
            Assert.That(s.State.Distance, Is.EqualTo(0));
            Assert.That(s.State.Speed, Is.EqualTo(0));
            Assert.That(s.State.Time, Is.EqualTo(0));
            Assert.That(s.Camera.Mode, Is.EqualTo(CameraMode.Hidden));
            var other = Open();
            other.Tick(Inputs(0, R4), ref frame);
            Assert.That(frame.Distance, Is.EqualTo(0), "otra sesión arranca de cero");
        }

        [Test, Category("AC-SCN-01-03")]
        public void NeitherTheWindowNorThePauseResetsIt()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Advance(Inputs(0, R4), 10, ref frame);
            var d = frame.Distance;
            var t = frame.SceneTime;
            s.Tick(Inputs(10 + 1.0 / 60, R4, 914, 411), ref frame);
            Assert.That(frame.Distance, Is.GreaterThan(d), "rotar no reinicia");
            Assert.That(frame.Window.Width, Is.EqualTo(914), "la cámara toma el horizonte y la focal nuevos en el primer cuadro");
            Assert.That(frame.Window.HorizonY, Is.EqualTo(411 * 0.42).Within(1e-9));
            s.Advance(Inputs(10 + 1.0 / 60, Paused, 914, 411), 5, ref frame);
            Assert.That(frame.SceneTime, Is.GreaterThan(t + 4.9), "en pausa el tiempo de escena sigue: nubes y estrellas se mueven");
            Assert.That(frame.Speed, Is.LessThan(4.0), "el bote frena");
        }

        [Test, Category("AC-SCN-03-04")]
        public void AdvanceUsesSixtiethSteps()
        {
            // El arnés no "salta" 60 s en un cuadro: CAP-13 tiene la distancia de un remo real (≈ 234 m).
            var frame = SceneFrame.Create();
            var s = Open();
            s.Advance(Inputs(0, R4), 60, ref frame);
            Assert.That(frame.Distance, Is.EqualTo(234).Within(0.5));
            Assert.That(frame.SceneTime, Is.EqualTo(60).Within(1e-9));
            var t = Open();
            t.Tick(Inputs(60, R4), ref frame);
            Assert.That(frame.SceneTime, Is.EqualTo(0.1).Within(1e-12), "un Tick suelto avanza como mucho 0,1 s");
        }

        [Test, Category("AC-SCN-01-04")]
        public void ComingBackFromBackgroundAdvancesAtMostATenthAndTheHourIsInstant()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Advance(Inputs(0, R4, forced: 12), 2, ref frame);
            s.NotifyResumed();
            s.Tick(Inputs(600, R4, forced: 22), ref frame);
            Assert.That(frame.SceneTime, Is.EqualTo(2.1).Within(1e-9));
            Assert.That(frame.VisibleHour, Is.EqualTo(22), "la hora de ese cuadro, sin transición");
            Assert.That(frame.StarFade, Is.EqualTo(1), "las estrellas en su valor final");
        }

        [Test, Category("AC-SCN-23-02"), Category("AC-SCN-09-01")]
        public void TheSessionDecidesTheKindOfHourChange()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Tick(Inputs(0, R4), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(10), "al abrir, la efectiva en el acto");
            // El minuto real avanza: 1 s lineal.
            var next = Local.AddMinutes(1);
            s.Tick(Inputs(1, R4, local: next), ref frame);
            s.Tick(Inputs(1.5, R4, local: next), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(10 + 0.5 / 60).Within(1e-9));
            s.Tick(Inputs(2.5, R4, local: next), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(10 + 1 / 60.0));
            // Hora escena: 1,5 s con ease.move.
            s.Tick(Inputs(3, R4, forced: 18, local: next), ref frame);
            s.Tick(Inputs(3.75, R4, forced: 18, local: next), ref frame);
            Assert.That(frame.VisibleHour, Is.GreaterThan(13).And.LessThan(16));
            s.Tick(Inputs(5, R4, forced: 18, local: next), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(18));
            // Un cambio de zona: en el acto.
            s.Tick(Inputs(6, R4, forced: null, local: next.ToOffset(TimeSpan.FromHours(1))), ref frame);
            s.Tick(Inputs(6.1, R4, forced: null, local: next.ToOffset(TimeSpan.FromHours(2))), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(next.ToOffset(TimeSpan.FromHours(2)).Hour + 1 / 60.0));
        }

        [Test, Category("AC-SCN-23-02"), Category("AC-APP-05-08")]
        public void WithReduceMotionTheForcedHourIsInstant()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Tick(Inputs(0, R4, forced: 12, reduce: true), ref frame);
            s.Tick(Inputs(0.1, R4, forced: 18, reduce: true), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(18));
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheTestHourSnapsTheFades()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.Tick(Inputs(0, R4, forced: 12), ref frame);
            Assert.That(frame.StarFade, Is.EqualTo(0));
            s.ForceHourForTest(22);
            s.Tick(Inputs(0.016, R4, forced: 12), ref frame);
            Assert.That(frame.VisibleHour, Is.EqualTo(22));
            Assert.That(frame.StarFade, Is.EqualTo(1), "con la hora fijada por una prueba, en su valor final");
            s.ForceHourForTest(18);
            s.Tick(Inputs(0.032, R4, forced: 12), ref frame);
            Assert.That(frame.StarFade, Is.EqualTo(1), "a las 18:00 se ven (ambiente 0,85)");
            // Por la hora (Hora escena) el conjunto cruza el umbral con su fundido de 0,9 s.
            var t = Open();
            t.Tick(Inputs(0, R4, forced: 15), ref frame);
            t.Tick(Inputs(0.1, R4, forced: 22, reduce: true), ref frame);
            Assert.That(frame.StarFade, Is.LessThan(0.1), "entra con su fundido, no de golpe");
            t.Advance(Inputs(0.1, R4, forced: 22, reduce: true), 1, ref frame);
            Assert.That(frame.StarFade, Is.EqualTo(1).Within(1e-9));
        }

        [Test, Category("AC-SCN-03-05"), Category("AC-WKT-13-14")]
        public void TheSceneDistanceIsOwnAndNeverReachesTheEngine()
        {
            // La escena solo lee la foto (SceneInputs): su distancia sale de su velocidad, con inercia, no del monitor.
            var frame = SceneFrame.Create();
            var s = Open();
            s.Advance(Inputs(0, R4), 60.2, ref frame);
            Assert.That(frame.Distance, Is.Not.EqualTo(240.0), "el monitor sumó 240 m; la escena, ≈ 234");
            Assert.That(typeof(SceneSession).GetConstructors()[0].GetParameters().Length, Is.EqualTo(2), "ni el motor ni el monitor entran a la sesión");
            foreach (var p in typeof(SceneTickInputs).GetProperties())
            {
                Assert.That(p.PropertyType.Namespace, Is.Not.EqualTo("RowMate.Core.Workout"), "la sesión recibe valores, no el motor");
            }
        }

        [Test, Category("AC-SCN-14-07"), Category("AC-SCN-15-05")]
        public void TheFrameCarriesTheLegacyListAndTheCabinLights()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.ForceDistanceForTest(SceneSeeds.E920);
            s.ForceHourForTest(22);
            s.FreezeSceneTimeForTest(SceneSeeds.TakeSceneTime);
            s.Tick(Inputs(0, new SceneInputs(0, 0, false)), ref frame);
            Assert.That(frame.Distance, Is.EqualTo(920));
            Assert.That(frame.SceneTime, Is.EqualTo(3.2));
            Assert.That(frame.VisibleProps.Count, Is.EqualTo(98), "a 920 m (QA: 98)");
            var cabins = 0;
            for (var i = 0; i < frame.VisibleProps.Count; i++)
            {
                if (frame.VisibleProps[i].Kind == PropKind.Cabin)
                {
                    cabins++;
                    Assert.That(frame.PropLight[i], Is.EqualTo(1), "a las 22 h, encendida");
                    Assert.That(frame.VisibleProps[i].Z - frame.Distance, Is.EqualTo(40), "la cabaña a 40 m a la derecha (E-920)");
                }
                else
                {
                    Assert.That(frame.PropLight[i], Is.EqualTo(0));
                }
            }

            Assert.That(cabins, Is.EqualTo(1));
            s.ForceHourForTest(12);
            s.Tick(Inputs(0.016, new SceneInputs(0, 0, false)), ref frame);
            for (var i = 0; i < frame.VisibleProps.Count; i++)
            {
                Assert.That(frame.PropLight[i], Is.EqualTo(0), "a las 12 h, apagada");
            }
        }

        [Test, Category("AC-SCN-11-03"), Category("AC-SCN-02-02")]
        public void StarsAndCloudsFallOnTheirLegacyPixelWithBreathingOff()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.BreathingOffForTest(true);
            s.Camera.ShowInPlace(0);
            s.ForceHourForTest(22);
            s.FreezeSceneTimeForTest(3.2);
            s.Tick(Inputs(0, Paused, 390, 844), ref frame);
            var cam = frame.Window;
            var points = new StarPoint[80];
            StarField.Layout(cam.Width, cam.HorizonY, points);
            for (var i = 0; i < 80; i++)
            {
                var st = frame.Stars[i];
                Assert.That(ChaseCameraProjection.TryProjectWithPose(frame.Projection, frame.Pose, cam, st.DirX * 1e6, 2.2 + st.DirY * 1e6, st.DirZ * 1e6,
                    out var sx, out var sy), Is.True);
                Assert.That(sx, Is.EqualTo(points[i].X).Within(1e-3), "± 1 dp");
                Assert.That(sy, Is.EqualTo(points[i].Y).Within(1e-3));
                Assert.That(st.Alpha, Is.GreaterThan(0));
            }

            for (var i = 0; i < 7; i++)
            {
                var c = frame.Clouds[i];
                Assert.That(ChaseCameraProjection.TryProjectWithPose(frame.Projection, frame.Pose, cam, c.DirX * 1e6, 2.2 + c.DirY * 1e6, c.DirZ * 1e6,
                    out var sx, out var sy), Is.True);
                Assert.That(sx, Is.EqualTo(CloudModel.X(i, cam.Width, 3.2)).Within(1e-3));
                Assert.That(sy, Is.EqualTo(CloudModel.Y(i, cam.HorizonY)).Within(1e-3));
                Assert.That(c.Variant, Is.EqualTo(i % 4));
            }
        }

        [Test, Category("AC-SCN-13-03")]
        public void TheWaterLinesAreTheLegacyOnesCenteredUnderTheVisibleStar()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.ForceHourForTest(12);
            s.ForceDistanceForTest(0);
            s.Tick(Inputs(0, Paused, 390, 844), ref frame);
            var cam = frame.Window;
            Assert.That(frame.ShimmerCenterX, Is.EqualTo(cam.CenterX).Within(1e-9), "al mediodía, centradas");
            Assert.That(frame.Lines[0].Alpha, Is.EqualTo(0.15).Within(1e-12));
            Assert.That(frame.ShimmerVisibility, Is.EqualTo(1).Within(1e-12));
            s.ForceHourForTest(19 + 34 / 60.0);
            s.Tick(Inputs(0.016, Paused, 390, 844), ref frame);
            Assert.That(frame.ShimmerVisibility, Is.EqualTo(0).Within(1e-9), "a las 19:34 no hay líneas en pantalla");
        }

        [Test, Category("AC-SCN-10-07")]
        public void TheMoonPhaseComesFromTheDateAndTheRegionMirrorsIt()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.ForceHourForTest(22);
            s.Tick(Inputs(0, Paused), ref frame);
            Assert.That(frame.Moon.Illumination, Is.EqualTo(0.94).Within(0.02), "la fecha de prueba");
            Assert.That(frame.SouthernMirror, Is.False, "sin región, el norte");
            var south = new SceneSession(new LakeEnvironment(), new SceneSessionOptions { RegionCode = "AR" });
            south.Open(0);
            south.Tick(Inputs(0, Paused), ref frame);
            Assert.That(frame.SouthernMirror, Is.True);
        }

        [Test, Category("AC-WKT-13-12")]
        public void TheVeilGoesIntoTheSameVolume()
        {
            var frame = SceneFrame.Create();
            var s = Open();
            s.ForceHourForTest(12);
            s.Tick(Inputs(0, Paused, veil: 1), ref frame);
            Assert.That(frame.Post.VeilWeight, Is.EqualTo(1));
            Assert.That(frame.Post.ExposureEv, Is.EqualTo(frame.Look.ExposureEv + HourLookTable.Default.VeilEv).Within(1e-12), "VeilEv de la receta (WP-D2 lo ajusta)");
        }

        [Test, Category("AC-SCN-01-12")]
        public void TickDoesNotAllocate()
        {
#if NET5_0_OR_GREATER
            var frame = SceneFrame.Create();
            var s = Open();
            Run(s, ref frame, 0, 600, R4, null); // calentamiento: JIT y estáticos fuera de la medición
            Run(s, ref frame, 10, 300, Paused, 18);
            var best = long.MaxValue;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var start = 20 + attempt * 20;
                var rowing = Run(s, ref frame, start, 600, R4, null);
                var hour = Run(s, ref frame, start + 10, 300, Paused, (attempt % 2 == 0) ? 22 : 18);
                best = Math.Min(best, rowing + hour);
            }

            Assert.That(best, Is.EqualTo(0L), "remando, en pausa y durante Hora escena");
#else
            Assert.Ignore("solo dotnet: en Mono el contador del hilo da 0 (la medida de Unity es la de cuadros reales, SBT)");
#endif
        }

#if NET5_0_OR_GREATER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        private static long Run(SceneSession s, ref SceneFrame frame, double start, int frames, SceneInputs w, int? forced)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < frames; i++)
            {
                var inputs = new SceneTickInputs(start + i / 60.0, Local, Local.ToUniversalTime(), w, forced, false, 411, 914, 0);
                s.Tick(inputs, ref frame);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
#endif
    }
}
