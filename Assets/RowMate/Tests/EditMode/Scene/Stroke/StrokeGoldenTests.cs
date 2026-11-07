using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SKG · Los vectores del oráculo de la palada (02-tech-design §6.2): tools/oracle/run-oracle.ps1 -Stroke corre StrokeCycle, la
    // geometría del BoatPainter y SceneState del legacy con Flutter (tools/oracle/engine/test/stroke_vectors_test.dart) y escribe
    // scene_stroke.json; estas pruebas no necesitan Flutter. Comparador: BIT A BIT lo que es aritmética pura (los ramps, el barrido,
    // la fase, la velocidad, la distancia, el tiempo, los charcos); Within(1e-12) lo que pasa por trigonometría (el cabeceo y la
    // geometría con cos/sin, por el último bit de la libm).
    [TestFixture]
    public sealed class StrokeGoldenTests
    {
        public const string File = "scene_stroke.json";
        public const string Generator = "tools/oracle/engine/test/stroke_vectors_test.dart";

        [Test, Category("AC-SCN-05-01"), Category("AC-SCN-04-01")]
        public void TheFileComesFromTheLegacyStroke()
        {
            var file = GoldenVectors.Load(File);
            Assert.That(file.Header.Generator, Is.EqualTo(Generator));
            Assert.That(System.IO.File.Exists(RepoPaths.Combine(Generator)), Is.True);
            Assert.That(file.Header.LegacyCommit, Is.EqualTo(GoldenVectors.Load("scene_water.json").Header.LegacyCommit), "el mismo legacy que la 6a");
            Assert.That(file.SizeBytes, Is.LessThanOrEqualTo(1048576));
            Assert.That(file.Table("cycle").Count, Is.EqualTo(1001));
            Assert.That(file.Table("boat").Count, Is.EqualTo(1001));
            Assert.That(file.Table("runs").Count, Is.EqualTo(200 + 130 + 50 + 360));
            foreach (var leg in new[] { "LEG-01.png", "LEG-02.png" })
            {
                Assert.That(System.IO.File.Exists(RepoPaths.Combine("docs/migration/iterations/07-remero-palada/snapshots/legacy/" + leg)), Is.True, leg);
            }
        }

        [Test, Category("AC-SCN-05-01"), Category("AC-SCN-05-02"), Category("AC-SCN-05-03"), Category("AC-SCN-05-04")]
        public void TheCycleMatchesTheLegacyBitForBit()
        {
            var bad = GoldenVectors.CheckRows(File, "cycle", row =>
            {
                var p = row.F64("p");
                if (Bits(StrokeCycle.Legs(p)) != Bits(row.F64("legs")) || Bits(StrokeCycle.Back(p)) != Bits(row.F64("back"))
                    || Bits(StrokeCycle.Arms(p)) != Bits(row.F64("arms")) || Bits(StrokeCycle.TorsoLean(p)) != Bits(row.F64("torsoLean"))
                    || Bits(StrokeCycle.OarSweep(p)) != Bits(row.F64("oarSweep")) || StrokeCycle.BladeInWater(p) != row.Cell("bladeIn").AsBool())
                {
                    return "p " + p.ToString("R", CultureInfo.InvariantCulture);
                }

                return null;
            });
            Assert.That(bad, Is.EqualTo(1001), "filas revisadas");
        }

        [Test, Category("AC-SCN-05-05")]
        public void TheLegacyGeometryMatchesTheBoatPainter()
        {
            var bad = GoldenVectors.CheckRows(File, "boat", row =>
            {
                var p = row.F64("p");
                if (Bits(LegacyBoatGeometry.SeatZ(p)) != Bits(row.F64("seatZ")))
                {
                    return "seatZ " + p;
                }

                var hl = LegacyBoatGeometry.Handle(-1, p);
                var hr = LegacyBoatGeometry.Handle(1, p);
                var tl = LegacyBoatGeometry.BladeTip(-1, p);
                var tr = LegacyBoatGeometry.BladeTip(1, p);
                return Near(hl, row, "hl") && Near(hr, row, "hr") && Near(tl, row, "tl") && Near(tr, row, "tr") ? null : "p " + p;
            });
            Assert.That(bad, Is.EqualTo(1001), "filas revisadas");
        }

        [TestCase("rowing24", 0.05, 107, 24, true, 200, Category = "AC-SCN-04-01,AC-SCN-07-01,AC-SCN-06-01")]
        [TestCase("frames60", 1.0 / 60, 125, 24, true, 360, Category = "AC-SCN-04-01,AC-SCN-07-01")]
        public void TheStateMatchesTheLegacyStepByStep(string run, double dt, double pace, double spm, bool active, int steps)
        {
            var s = new SceneState();
            var rows = Rows(run);
            Assert.That(rows.Count, Is.EqualTo(steps));
            for (var i = 0; i < steps; i++)
            {
                s.Update(dt, pace, spm, active);
                Compare(s, rows[i], run + " paso " + (i + 1));
            }
        }

        [Test, Category("AC-SCN-04-02"), Category("AC-SCN-04-03")]
        public void ClosingAndPauseMatchTheLegacy()
        {
            var closing = new SceneState();
            var rows = Rows("closing");
            for (var i = 0; i < rows.Count; i++)
            {
                closing.Update(0.05, 107, i < 10 ? 24 : 0, true);
                Compare(closing, rows[i], "closing paso " + (i + 1));
            }

            var paused = new SceneState();
            rows = Rows("paused");
            for (var i = 0; i < rows.Count; i++)
            {
                paused.Update(0.05, 107, 24, i < 10);
                Compare(paused, rows[i], "paused paso " + (i + 1));
            }
        }

        private static List<GoldenRow> Rows(string run)
        {
            var list = new List<GoldenRow>();
            foreach (var row in GoldenVectors.Rows(File, "runs"))
            {
                if (row.Text("run") == run)
                {
                    list.Add(row);
                }
            }

            return list;
        }

        private static void Compare(SceneState s, GoldenRow row, string where)
        {
            Assert.That(Bits(s.StrokePhase), Is.EqualTo(Bits(row.F64("phase"))), where + ": fase");
            Assert.That(Bits(s.Speed), Is.EqualTo(Bits(row.F64("speed"))), where + ": velocidad");
            Assert.That(Bits(s.Distance), Is.EqualTo(Bits(row.F64("distance"))), where + ": distancia");
            Assert.That(Bits(s.Time), Is.EqualTo(Bits(row.F64("time"))), where + ": tiempo");
            Assert.That(s.Pitch, Is.EqualTo(row.F64("pitch")).Within(1e-12), where + ": cabeceo");
            Assert.That(s.Puddles.Count, Is.EqualTo((int)row.Int("puddles")), where + ": charcos");
            var list = row.Text("list");
            var parts = list.Length == 0 ? new string[0] : list.Split(';');
            for (var k = 0; k < parts.Length; k++)
            {
                var cells = parts[k].Split(':');
                Assert.That(s.Puddles[k].X, Is.EqualTo(FromBits(cells[0])).Within(1e-12), where + ": charco " + k + " x");
                Assert.That(s.Puddles[k].WorldZ, Is.EqualTo(FromBits(cells[1])).Within(1e-12), where + ": charco " + k + " z");
                Assert.That(Bits(s.Puddles[k].Age), Is.EqualTo(Bits(FromBits(cells[2]))), where + ": charco " + k + " edad");
            }
        }

        private static bool Near(in SceneVec3 v, GoldenRow row, string prefix)
        {
            return Math.Abs(v.X - row.F64(prefix + "x")) <= 1e-12 && Math.Abs(v.Y - row.F64(prefix + "y")) <= 1e-12
                && Math.Abs(v.Z - row.F64(prefix + "z")) <= 1e-12;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static double FromBits(string hex) => BitConverter.Int64BitsToDouble(long.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
