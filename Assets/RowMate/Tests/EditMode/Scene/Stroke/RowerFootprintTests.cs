using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // RFP · La huella del remero en la pantalla (AC-WKT-17-14, AC-WKT-18-23; 02-tech-design §2.11): en los siete tamaños y las 10
    // poses, el remero, los remos hasta los pernos y el bote alrededor del remero quedan dentro de la zona del bote; los paneles de
    // series abiertos (TV, P360) y la llave (L640 con el simulador) no tapan el cuerpo (cabeza, tronco, brazos y manos).
    [TestFixture]
    public sealed class RowerFootprintTests
    {
        private static readonly (string Name, double W, double H)[] Sizes =
        {
            ("V390", 390, 844), ("H844", 844, 390), ("TV", 411, 914), ("P360", 360, 800), ("TH", 914, 411), ("L640", 640, 360), ("W1280", 1280, 720),
        };

        private static readonly double[] Poses = { 0, 0.09, 0.18, 0.24, 0.36, 0.395, 0.6, 0.75, 0.85, 0.96 };

        private static IEnumerable<ScullingPose> AllPoses()
        {
            var fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            foreach (var c in Poses)
            {
                foreach (var effort in new[] { 0.0, 1.0 })
                {
                    var pose = ScullingPose.Create();
                    ScullingKinematics.Evaluate(c, effort, BoatSpec.Real, RowerSpec.Mannequin, fit, RowerIdleSample.None, ref pose);
                    yield return pose;
                }
            }
        }

        [Test, Category("AC-WKT-17-14"), Category("AC-SCN-08-01"), Category("AC-SCN-01-08")]
        public void TheRowerAndTheBoatStayInTheBoatZone()
        {
            var problems = new List<string>();
            foreach (var size in Sizes)
            {
                var layout = HudGeometry.Layout(HudGeometryCases.Input(size.W, size.H, SafeInsets.None, false, HudGeometryCases.Free));
                var cam = new SceneCamera(size.W, size.H);
                var zone = layout.BoatZone;
                foreach (var pose in AllPoses())
                {
                    RowerFootprint.Body(cam, pose, out var body);
                    RowerFootprint.BoatToPins(cam, pose, BoatSpec.Real, out var boat);
                    foreach (var (what, rect) in new[] { ("cuerpo", body), ("bote", boat) })
                    {
                        if (rect.Left < zone.Left - 0.5 || rect.Right > zone.Right + 0.5 || rect.Top < zone.Top - 0.5 || rect.Bottom > zone.Bottom + 0.5)
                        {
                            problems.Add(size.Name + " " + what + " " + Describe(rect) + " fuera de " + Describe(zone));
                        }
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [TestCase(411, 914, Category = "AC-WKT-18-23")]
        [TestCase(360, 800, Category = "AC-WKT-18-23")]
        public void TheOpenPanelsDoNotCoverTheBody(double width, double height)
        {
            var layout = HudGeometry.Layout(HudGeometryCases.Input(width, height, SafeInsets.None, false, HudGeometryCases.Free));
            var column = SeriesPanelGeometry.Column(layout);
            var cam = new SceneCamera(width, height);
            Assert.That(column.Right, Is.LessThanOrEqualTo(width / 2 - 44 * layout.Scale + 1e-6), "la franja de afuera (AC-WKT-18-14)");
            foreach (var pose in AllPoses())
            {
                RowerFootprint.Body(cam, pose, out var body);
                Assert.That(body.Left, Is.GreaterThanOrEqualTo(column.Right), "el panel no tapa el cuerpo");
            }
        }

        [Test, Category("AC-WKT-17-14")]
        public void TheWrenchDoesNotCoverTheBodyInL640()
        {
            var layout = HudGeometry.Layout(HudGeometryCases.Input(640, 360, SafeInsets.None, true, HudGeometryCases.Free));
            Assert.That(layout.Wrench.HasValue, Is.True);
            var wrench = layout.Wrench!.Value;
            var cam = new SceneCamera(640, 360);
            foreach (var pose in AllPoses())
            {
                RowerFootprint.Body(cam, pose, out var body);
                Assert.That(body.Intersects(wrench), Is.False, "la llave " + Describe(wrench) + " contra el cuerpo " + Describe(body));
            }
        }

        private static string Describe(DpRect r)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0:0.0}; {1:0.0} – {2:0.0}; {3:0.0}]", r.Left, r.Top, r.Right, r.Bottom);
        }
    }
}
