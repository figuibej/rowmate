using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Tests.Support;

namespace RowMate.Tests.PlayMode
{
    // SMT (05-series-parciales/02-tech-design §4.2, §4.5, §6.3; WP-D1): los IMotion de los paneles de series con el reloj de prueba y
    // callbacks en lugar de vistas (el contrato de la 4b: no conocen las vistas). Lo que se mira en cada uno:
    // - lo que le pasan a la vista, cuadro a cuadro, es lo de su línea de tiempo pura (SeriesTimelines, WP-A, que STL prueba contra
    //   las tablas de 04-design-notes §6), con los ms enteros de HudTimelines.Ms;
    // - IsFinite en cada fase (también la capa de estado) y que salen del MotionDriver al terminar; el resorte del alto con
    //   ISettlingMotion (RemainingDp);
    // - la inversión a mitad de camino, el dato que llega con el resorte corriendo, Instant y "sin armar" (el estado final en el
    //   acto), reducir movimiento (y que se prenda a mitad de camino);
    // - Stop: nada queda animando (ni en el MotionDriver ni pidiendo 60 fps; ROB-02) y ningún aviso después;
    // - 0 B por cuadro del C# de las motions (GcAllocations, con su control positivo en la misma prueba).
    // El dibujo (las reglas de alfa por la tinta, el FLIP, la selección) es de la vista: SPV, SCT y HBT (WP-B1).
    public sealed class SeriesMotionTests
    {
        private const double Eps = 1e-9;
        private const double Laps0 = 68;   // parciales abierto, un trabajo sin vueltas ni serie
        private const double Chip = SeriesPanelGeometry.ChipHeight;

        // ── Abrir y cerrar (§6.1; AC-WKT-18-18) ────────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-18")]
        public void OpenFollowsTheSpringAndTheFadesFrameByFrame()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var calls = 0;
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v =>
            {
                visual = v;
                calls++;
            }) { Scale = 1.054 };
            motion.SetExpanded(false, Chip, true);
            Assert.That(calls, Is.EqualTo(1), "el estado inicial, en el acto");
            AssertRest(visual, Chip, false);
            Assert.That(motion.IsRunning, Is.False);

            var start = rig.Now;
            motion.SetExpanded(true, 90, false);
            Assert.That(motion.IsFinite, Is.True);
            var belowThreshold = false;
            for (var k = 0; k <= 90; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = SeriesTimelines.Open(t, Chip, 90, 0, false);
                if (motion.HeightRemainingDp > 0)
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(expected.HeightBase).Within(Eps), "el resorte (spring.smooth) a los " + t);
                }
                else
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(90), "quieto en su final a los " + t);
                    Assert.That(Math.Abs(expected.HeightBase - 90) * 1.054, Is.LessThanOrEqualTo(0.05), "llega a menos de 0,05 dp");
                }

                if (t < 240)
                {
                    Assert.That(visual.ContentAlpha, Is.EqualTo(expected.ContentAlpha).Within(Eps), "contenido a los " + t);
                    Assert.That(visual.ChipLabelAlpha, Is.EqualTo(expected.ChipLabelAlpha).Within(Eps), "rótulo a los " + t);
                    Assert.That(visual.TitleAlpha, Is.EqualTo(expected.TitleAlpha).Within(Eps), "título a los " + t);
                    Assert.That(visual.ChevronDegrees, Is.EqualTo(expected.ChevronDegrees).Within(Eps), "flecha a los " + t);
                    Assert.That(motion.IsTimedPhaseRunning, Is.True);
                    Assert.That(motion.RemainingDp, Is.EqualTo(double.PositiveInfinity), "con un fundido en curso, la regla de toma bloquea");
                }
                else
                {
                    Assert.That(visual.ContentAlpha, Is.EqualTo(1));
                    Assert.That(visual.ChipLabelAlpha, Is.EqualTo(0));
                    Assert.That(visual.TitleAlpha, Is.EqualTo(1));
                    Assert.That(visual.ChevronDegrees, Is.EqualTo(180));
                    Assert.That(motion.IsTimedPhaseRunning, Is.False, "después de los 240 solo corre el resorte");
                    Assert.That(motion.RemainingDp, Is.EqualTo(Math.Abs(90 - visual.HeightBase) * 1.054).Within(Eps),
                        "ISettlingMotion: lo que le falta al resorte, en dp");
                    belowThreshold |= motion.IsFinite && motion.RemainingDp < SettlingMotion.TakeThresholdDp;
                }

                Assert.That(visual.TailShiftBase, Is.EqualTo(0), "la cola queda en su lugar final: la revela el vidrio");
                Assert.That(visual.RowsPushBase, Is.EqualTo(0));
                Assert.That(visual.MarkerAlpha0, Is.EqualTo(0));
                Assert.That(motion.IsFinite, Is.EqualTo(motion.IsRunning), "todo lo del panel es finito");
            }

            Assert.That(belowThreshold, Is.True, "a menos de 0,15 dp el resorte sigue finito pero no bloquea una toma");
            Assert.That(motion.Expanded, Is.True);
            AssertRest(visual, 90, true);
            rig.AssertIdle("el resorte llegó");
        }

        [Test, Category("AC-WKT-18-18")]
        public void CloseFadesFirstAndTheHeightWaitsEighty()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 90, true);
            AssertRest(visual, 90, true);
            var start = rig.Now;
            motion.SetExpanded(false, Chip, false);
            for (var k = 0; k <= 90; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = SeriesTimelines.Close(t, 90, Chip, 0, false);
                if (motion.HeightRemainingDp > 0)
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(expected.HeightBase).Within(Eps), "alto a los " + t);
                }
                else
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(Chip));
                }

                if (t <= 80)
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(90), "el alto empieza a bajar a los 80");
                }

                if (t < 240)
                {
                    Assert.That(visual.ContentAlpha, Is.EqualTo(expected.ContentAlpha).Within(Eps), "contenido a los " + t);
                    Assert.That(visual.TitleAlpha, Is.EqualTo(expected.TitleAlpha).Within(Eps));
                    Assert.That(visual.ChipLabelAlpha, Is.EqualTo(expected.ChipLabelAlpha).Within(Eps));
                    Assert.That(visual.ChevronDegrees, Is.EqualTo(expected.ChevronDegrees).Within(Eps));
                }
            }

            AssertRest(visual, Chip, false);
            Assert.That(motion.Expanded, Is.False);
            rig.AssertIdle("cerrado");
        }

        [Test, Category("AC-WKT-18-18")]
        public void ATapMidwayInvertsFromWhereItIs()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(false, Chip, true);
            var opened = rig.Now;
            motion.SetExpanded(true, 90, false);
            rig.RunFor(133);
            var t1 = HudTimelines.Ms(rig.Now - opened);
            var mid = SeriesTimelines.Open(t1, Chip, 90, 0, false);
            var before = visual;
            Assert.That(before.HeightBase, Is.EqualTo(mid.HeightBase).Within(Eps));

            var closed = rig.Now;
            motion.SetExpanded(false, Chip, false);
            Assert.That(visual.HeightBase, Is.EqualTo(before.HeightBase).Within(Eps), "el alto no salta en el cuadro del toque");
            Assert.That(visual.ContentAlpha, Is.EqualTo(before.ContentAlpha).Within(Eps));
            Assert.That(visual.ChevronDegrees, Is.EqualTo(before.ChevronDegrees).Within(Eps));
            var start = mid.ToStart();
            var previous = visual.HeightBase;
            for (var k = 1; k <= 20; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - closed);
                var expected = SeriesTimelines.Close(t, start, Chip, false);
                Assert.That(visual.HeightBase, Is.EqualTo(expected.HeightBase).Within(Eps), "el resorte con su posición y su velocidad, a los " + t);
                Assert.That(visual.ContentAlpha, Is.EqualTo(expected.ContentAlpha).Within(Eps), "el contenido desde su opacidad, a los " + t);
                Assert.That(visual.ChevronDegrees, Is.EqualTo(expected.ChevronDegrees).Within(Eps), "la flecha desde su ángulo, a los " + t);
                if (k == 1)
                {
                    Assert.That(visual.HeightBase, Is.GreaterThan(previous), "sigue subiendo un poco antes de volver (inercia, sin salto)");
                }
            }

            // Y otra vez al revés, desde el cierre a medio camino.
            var t2 = HudTimelines.Ms(rig.Now - closed);
            var back = SeriesTimelines.Close(t2, start, Chip, false).ToStart();
            var reopened = rig.Now;
            var atTap = visual.HeightBase;
            motion.SetExpanded(true, 90, false);
            Assert.That(visual.HeightBase, Is.EqualTo(atTap).Within(Eps));
            rig.Frame();
            var t3 = HudTimelines.Ms(rig.Now - reopened);
            Assert.That(visual.HeightBase, Is.EqualTo(SeriesTimelines.Open(t3, back, 90, false).HeightBase).Within(Eps));
            rig.RunFor(1500);
            AssertRest(visual, 90, true);
            rig.AssertIdle("abierto");
        }

        // ── La vuelta nueva y la repetición nueva (§6.2; AC-WKT-18-19) ─────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-19")]
        public void ANewLapPushesTheListAndEntersWithItsMarker()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 90, true);
            var change = HudSeriesChange.Between(Laps(1, true, 0, true), Laps(1, true, 1, true));
            Assert.That(change.NewLaps, Is.EqualTo(1));
            var start = rig.Now;
            motion.ContentChanged(in change, 114, false);
            for (var k = 0; k <= 90; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var height = t < 160 ? SeriesTimelines.DataResize(t, 90, 114, false) : 114;
                Assert.That(visual.HeightBase, Is.EqualTo(height).Within(Eps), "el alto con el empuje (0-160, ease.move), a los " + t);
                Assert.That(visual.TailShiftBase, Is.EqualTo(visual.HeightBase - 114).Within(Eps), "la cola baja con el alto (G − H_final)");
                if (t < 240)
                {
                    var rows = SeriesTimelines.RowsChange(t, 1, false, false);
                    Assert.That(visual.RowsPushBase, Is.EqualTo(rows.RowsPushBase).Within(Eps), "el empuje −22 → 0, a los " + t);
                    Assert.That(visual.PushProgress, Is.EqualTo(rows.PushProgress).Within(Eps), "la escala común llega con el empuje (DD5-24)");
                    Assert.That(visual.NewRowsAlpha, Is.EqualTo(rows.NewRowsAlpha).Within(Eps), "la fila nueva en 80-240, a los " + t);
                    Assert.That(visual.NewRowsShiftBase, Is.EqualTo(rows.NewRowsShiftBase).Within(Eps), "desde −8, a los " + t);
                    Assert.That(visual.NewRows, Is.EqualTo(1));
                }
                else
                {
                    Assert.That(visual.RowsPushBase, Is.EqualTo(0));
                    Assert.That(visual.NewRowsAlpha, Is.EqualTo(1));
                    Assert.That(visual.NewRows, Is.EqualTo(0));
                }

                if (t == 200)
                {
                    Assert.That(visual.NewRowsAlpha, Is.GreaterThanOrEqualTo(0.98), "a los 200 se lee entera (AC-WKT-18-19)");
                }

                Assert.That(visual.MarkerAlpha0, Is.EqualTo(t < 1400 ? SeriesTimelines.Marker(t, false) : 0).Within(Eps), "la marca, a los " + t);
                Assert.That(visual.MarkerAlpha1, Is.EqualTo(0));
                Assert.That(visual.DroppedAlpha, Is.EqualTo(0), "no se va ninguna");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 1400), "F hasta que la marca se apaga, a los " + t);
            }

            AssertRest(visual, 114, true);
            rig.AssertIdle("la marca se apagó");
        }

        [Test, Category("AC-WKT-18-19")]
        public void AFifthLapDropsTheOldestAndTheMarkersMoveDown()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 180, true);
            var first = HudSeriesChange.Between(Laps(1, true, 4, true), Laps(1, true, 5, true));
            Assert.That(first.LapDropped, Is.True);
            var s1 = rig.Now;
            motion.ContentChanged(in first, 180, false);
            for (var k = 0; k <= 60; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - s1);
                Assert.That(visual.HeightBase, Is.EqualTo(180), "con 4 a la vista el alto no cambia");
                Assert.That(visual.DroppedAlpha, Is.EqualTo(t < 240 ? SeriesTimelines.RowsChange(t, 1, true, false).DroppedAlpha : 0).Within(Eps),
                    "la más vieja se va en 0-120, a los " + t);
            }

            var second = HudSeriesChange.Between(Laps(1, true, 5, true), Laps(1, true, 6, true));
            var s2 = rig.Now;
            motion.ContentChanged(in second, 180, false);
            for (var k = 0; k <= 30; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t1 = HudTimelines.Ms(rig.Now - s1);
                var t2 = HudTimelines.Ms(rig.Now - s2);
                Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t2, false)).Within(Eps), "la nueva arriba");
                Assert.That(visual.MarkerAlpha1, Is.EqualTo(SeriesTimelines.Marker(t1, false)).Within(Eps), "la anterior bajó una fila");
            }

            rig.RunFor(1500);
            rig.AssertIdle("las dos marcas se apagaron");
        }

        [Test, Category("AC-WKT-18-19")]
        public void SeveralLapsInOneSecondEnterTogether()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 92, true);
            var change = HudSeriesChange.Between(Laps(1, true, 1, false), Laps(1, true, 3, false));
            Assert.That(change.NewLaps, Is.EqualTo(2));
            var start = rig.Now;
            motion.ContentChanged(in change, 136, false);
            Assert.That(visual.RowsPushBase, Is.EqualTo(-44).Within(Eps), "22·k");
            Assert.That(visual.NewRows, Is.EqualTo(2));
            rig.RunFor(200);
            var t = HudTimelines.Ms(rig.Now - start);
            Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t, false)).Within(Eps));
            Assert.That(visual.MarkerAlpha1, Is.EqualTo(SeriesTimelines.Marker(t, false)).Within(Eps), "cada una con su marca");
            Assert.That(visual.MarkerAlpha2, Is.EqualTo(0));
        }

        // ── El cambio de contenido por pieza (§6.3; DD5-18) ─────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-19")]
        public void TheSwapByPieceAsInVidThree()
        {
            // VID-03 (a los 181 s de Series 600): de un trabajo con una vuelta a un descanso. La vuelta en curso y #1 salen, entra el
            // vacío, y el alto baja de 114 a 106 en 80-240 con la cola (113,9 · 112,8 · 109,0 · 106,6 · 106,0 a los 100 a 233).
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 114, true);
            var change = HudSeriesChange.Between(Laps(1, true, 1, true), Laps(2, false, 0, true));
            Assert.That(change.LapsBody, Is.EqualTo(SeriesBodyChange.ToEmpty));
            Assert.That(change.ClearedLaps, Is.EqualTo(1));
            var start = rig.Now;
            motion.ContentChanged(in change, 106, false);
            var table = new Dictionary<long, double> { { 100, 113.9 }, { 133, 112.8 }, { 167, 109.0 }, { 200, 106.6 }, { 233, 106.0 } };
            for (var k = 0; k <= 20; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var swap = SeriesTimelines.Swap(t, true, false);
                if (t < 280)
                {
                    Assert.That(visual.BodyOldAlpha, Is.EqualTo(swap.OldAlpha).Within(Eps), "lo viejo sale en 0-120, a los " + t);
                    Assert.That(visual.BodyNewAlpha, Is.EqualTo(swap.NewAlpha).Within(Eps), "lo nuevo entra en 120-280, a los " + t);
                    Assert.That(visual.BodyNewRiseBase, Is.EqualTo(swap.NewRiseBase).Within(Eps), "subiendo +4 → 0, a los " + t);
                    Assert.That(visual.ClearedRowsAlpha, Is.EqualTo(swap.ClearedRowsAlpha).Within(Eps), "las filas se vacían en su lugar");
                }

                var height = t < 240 ? SeriesTimelines.DataResize(t, 114, 106, false) : 106;
                Assert.That(visual.HeightBase, Is.EqualTo(height).Within(Eps), "se achica en 80-240, a los " + t);
                Assert.That(visual.TailShiftBase, Is.EqualTo(visual.HeightBase - 106).Within(Eps), "la cola sube con el vidrio");
                if (table.TryGetValue(t, out var vid))
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(vid).Within(0.15), "VID-03 a los " + t);
                }

                Assert.That(visual.RowsPushBase, Is.EqualTo(0), "sin vueltas nuevas no hay empuje");
            }

            AssertRest(visual, 106, true);
            rig.AssertIdle("el cambio terminó");
        }

        [Test, Category("AC-WKT-18-19")]
        public void FromWorkToWorkTheCurrentLapRollsAndBetweenRestsNothingMoves()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var calls = 0;
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v =>
            {
                visual = v;
                calls++;
            });
            motion.SetExpanded(true, 136, true);
            var roll = HudSeriesChange.Between(Laps(1, true, 2, false), Laps(2, true, 0, false));
            Assert.That(roll.LapsBody, Is.EqualTo(SeriesBodyChange.Roll));
            var start = rig.Now;
            motion.ContentChanged(in roll, Laps0, false);
            for (var k = 0; k <= 10; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.BodyOldAlpha, Is.EqualTo(0), "la vuelta en curso no funde: cambia con su rodillo");
                Assert.That(visual.BodyNewAlpha, Is.EqualTo(1));
                Assert.That(visual.BodyNewRiseBase, Is.EqualTo(0));
                Assert.That(visual.ClearedRowsAlpha, Is.EqualTo(t < 120 ? SeriesTimelines.Swap(t, false, false).ClearedRowsAlpha : 0).Within(Eps),
                    "las dos filas salen en su lugar, a los " + t);
            }

            rig.RunFor(400);
            rig.AssertIdle("terminó");

            // De un descanso a un enfriamiento (P-01 con dos pasos que no son de trabajo): el vacío no se mueve.
            motion.SetExpanded(true, 84, true);
            var still = HudSeriesChange.Between(Laps(3, false, 0, false), Laps(4, false, 0, false));
            Assert.That(still.LapsBody, Is.EqualTo(SeriesBodyChange.None));
            calls = 0;
            motion.ContentChanged(in still, 84, false);
            Assert.That(calls, Is.EqualTo(0), "nada cambia");
            Assert.That(motion.IsRunning, Is.False);
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheFirstRepReplacesTheEmptyWithItsMarker()
        {
            // VID-04 (a los 201 s): repeticiones de 84 a 60; sale el vacío y entra Rep 1 en 120-280, con su marca (83,7 · 80,5 · 69,1
            // · 61,7 · 60,0 a los 100 a 233).
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Reps, v => visual = v);
            motion.SetExpanded(true, 84, true);
            var change = HudSeriesChange.Between(Reps(2), Reps(3, 1));
            Assert.That(change.RepsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(change.NewRep, Is.True);
            var start = rig.Now;
            motion.ContentChanged(in change, 60, false);
            var table = new Dictionary<long, double> { { 100, 83.7 }, { 133, 80.5 }, { 167, 69.1 }, { 200, 61.7 }, { 233, 60.0 } };
            for (var k = 0; k <= 30; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var swap = SeriesTimelines.Swap(t, true, false);
                if (t < 280)
                {
                    Assert.That(visual.BodyOldAlpha, Is.EqualTo(swap.OldAlpha).Within(Eps), "el vacío sale, a los " + t);
                    Assert.That(visual.BodyNewAlpha, Is.EqualTo(swap.NewAlpha).Within(Eps), "la primera fila entra, a los " + t);
                    Assert.That(visual.MarkerAlpha0, Is.EqualTo(visual.BodyNewAlpha).Within(Eps), "la marca entra con su fila, a los " + t);
                }

                Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t - 40, false)).Within(Eps));
                Assert.That(visual.RowsPushBase, Is.EqualTo(0), "no hay filas que bajar");
                Assert.That(visual.NewRows, Is.EqualTo(0));
                var height = t < 240 ? SeriesTimelines.DataResize(t, 84, 60, false) : 60;
                Assert.That(visual.HeightBase, Is.EqualTo(height).Within(Eps));
                if (table.TryGetValue(t, out var vid))
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(vid).Within(0.15), "VID-04 a los " + t);
                }
            }

            rig.RunFor(1500);
            rig.AssertIdle("la marca se apagó");
        }

        [Test, Category("AC-WKT-18-19")]
        public void ANewRepPushesTheListAndTheOldestLeaves()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Reps, v => visual = v);
            motion.SetExpanded(true, 104, true);
            var change = HudSeriesChange.Between(Reps(7, 3, 2, 1), Reps(9, 4, 3, 2));
            Assert.That(change.NewRep, Is.True);
            Assert.That(change.RepDropped, Is.True);
            var start = rig.Now;
            motion.ContentChanged(in change, 104, false);
            for (var k = 0; k <= 15; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var rows = SeriesTimelines.RowsChange(t, 1, true, false);
                Assert.That(visual.RowsPushBase, Is.EqualTo(t < 240 ? rows.RowsPushBase : 0).Within(Eps));
                Assert.That(visual.DroppedAlpha, Is.EqualTo(t < 240 ? rows.DroppedAlpha : 0).Within(Eps), "la cuarta se va");
                Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t, false)).Within(Eps), "con la marca de su fila");
                Assert.That(visual.MarkerAlpha3, Is.EqualTo(0), "repeticiones tiene 3 filas");
                Assert.That(visual.HeightBase, Is.EqualTo(104), "con 3 el alto no cambia");
            }
        }

        // ── La línea Rep y la serie (§6.3, §6.4) ───────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-01"), Category("AC-WKT-18-19")]
        public void TheRepLineEntersAndLeavesWithTheSeries()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, Laps0, true);
            var started = HudSeriesChange.Between(Laps(1, true, 0, false), Laps(1, true, 0, true));
            Assert.That(started.SeriesStarted, Is.True);
            var start = rig.Now;
            motion.ContentChanged(in started, 90, false);
            for (var k = 0; k <= 20; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var line = SeriesTimelines.RepLine(t, true, false);
                Assert.That(visual.RepLineAlpha, Is.EqualTo(t < 280 ? line.Opacity : 1).Within(Eps), "entra con lo nuevo (120-280), a los " + t);
                Assert.That(visual.RepLineRiseBase, Is.EqualTo(t < 280 ? line.RiseBase : 0).Within(Eps));
                Assert.That(visual.HeightBase, Is.EqualTo(t < 160 ? SeriesTimelines.DataResize(t, Laps0, 90, false) : 90).Within(Eps),
                    "parciales crece en 0-160, a los " + t);
            }

            var ended = HudSeriesChange.Between(Laps(1, true, 0, true), Laps(1, true, 0, false));
            start = rig.Now;
            motion.ContentChanged(in ended, Laps0, false);
            for (var k = 0; k <= 20; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.RepLineAlpha, Is.EqualTo(t < 120 ? SeriesTimelines.RepLine(t, false, false).Opacity : 0).Within(Eps),
                    "sale en 0-120 y queda en 0, a los " + t);
                Assert.That(visual.HeightBase, Is.EqualTo(t < 240 ? SeriesTimelines.DataResize(t, 90, Laps0, false) : Laps0).Within(Eps),
                    "se achica en 80-240");
            }

            rig.AssertIdle("terminó");
        }

        [Test, Category("AC-WKT-18-01")]
        public void TheRepsPanelAppearsAtRestAndLeavesWithoutMoving()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var calls = 0;
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Reps, v =>
            {
                visual = v;
                calls++;
            });
            motion.SetExpanded(true, 84, true);
            var started = HudSeriesChange.Between(new HudSeries(1, 1, true, "", 0, "", 0, NoLaps, 0, false, 0, null, 0, NoReps), Reps(1));
            Assert.That(started.SeriesStarted, Is.True);
            motion.ContentChanged(in started, 84, false);
            AssertRest(visual, 84, true);
            Assert.That(motion.IsRunning, Is.False, "aparece en su estado guardado: la entrada la hace la columna");

            var ended = HudSeriesChange.Between(Reps(5, 1), new HudSeries(1, 6, false, "", 0, "", 0, NoLaps, 0, false, 0, null, 0, NoReps));
            Assert.That(ended.SeriesEnded, Is.True);
            calls = 0;
            motion.ContentChanged(in ended, Chip, false);
            Assert.That(calls, Is.EqualTo(0), "se va con la columna, sin moverse");
            Assert.That(motion.IsRunning, Is.False);
        }

        // ── Con el panel cerrado (§6.0) ──────────────────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-19")]
        public void AClosedPanelAnimatesNoContentAndOpensWithTheListInPlace()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var calls = 0;
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v =>
            {
                visual = v;
                calls++;
            });
            motion.SetExpanded(false, Chip, true);
            calls = 0;
            var change = HudSeriesChange.Between(Laps(1, true, 0, true), Laps(1, true, 1, true));
            motion.ContentChanged(in change, Chip, false);
            Assert.That(calls, Is.EqualTo(0), "el chip no cambia");
            Assert.That(motion.IsRunning, Is.False);

            motion.SetExpanded(true, 114, false);
            for (var k = 0; k <= 70; k++)
            {
                rig.Frame();
                Assert.That(visual.MarkerAlpha0, Is.EqualTo(0), "al abrir, la lista ya está, sin marcas");
                Assert.That(visual.RowsPushBase, Is.EqualTo(0), "ni empuje");
                Assert.That(visual.NewRowsAlpha, Is.EqualTo(1));
                Assert.That(visual.BodyNewAlpha, Is.EqualTo(1));
            }

            // Una vuelta que llega, y el panel que se cierra con la marca encendida: la marca se apaga con el contenido.
            var next = HudSeriesChange.Between(Laps(1, true, 1, true), Laps(1, true, 2, true));
            rig.RunFor(2000);
            motion.ContentChanged(in next, 136, false);
            rig.RunFor(300);
            Assert.That(visual.MarkerAlpha0, Is.EqualTo(1), "la marca en 1 hasta los 600");
            motion.SetExpanded(false, Chip, false);
            var closedAt = rig.Now;
            while (HudTimelines.Ms(rig.Now - closedAt) < 120)
            {
                rig.Frame();
            }

            Assert.That(visual.ContentAlpha, Is.EqualTo(0));
            Assert.That(visual.MarkerAlpha0, Is.EqualTo(0), "con el contenido ya ido, nada de él sigue");
            rig.RunFor(1500);
            rig.AssertIdle("cerrado");
            motion.SetExpanded(true, 136, false);
            rig.Frame();
            Assert.That(visual.MarkerAlpha0, Is.EqualTo(0), "al abrirlo otra vez, sin marcas");
        }

        // ── Un dato con el resorte corriendo (§4.1, §6.2; D1-03) ─────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-18"), Category("AC-WKT-18-19")]
        public void ADataChangeDuringTheOpeningSpringRetargetsItFromWhereItIs()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(false, Chip, true);
            var opened = rig.Now;
            motion.SetExpanded(true, Laps0, false);
            rig.RunFor(150);
            var tc = HudTimelines.Ms(rig.Now - opened);
            var spring = SeriesTimelines.Open(tc, Chip, Laps0, 0, false);
            var before = visual;
            Assert.That(before.HeightBase, Is.EqualTo(spring.HeightBase).Within(Eps));
            Assert.That(before.HeightBase, Is.LessThan(Laps0), "el vidrio todavía no llegó");

            // Una vuelta: 68 → 92 (la primera suma 24).
            var change = HudSeriesChange.Between(Laps(1, true, 0, false), Laps(1, true, 1, false));
            var changed = rig.Now;
            motion.ContentChanged(in change, 92, false);
            Assert.That(visual.HeightBase, Is.EqualTo(before.HeightBase).Within(Eps), "el alto no salta");
            Assert.That(92 + visual.TailShiftBase, Is.EqualTo(Laps0 + before.TailShiftBase).Within(Eps), "la cola no salta (en su lugar de antes)");
            Assert.That(visual.RowsPushBase, Is.EqualTo(-22).Within(Eps), "las filas, donde estaban");
            var tailFloor = before.TailShiftBase + (Laps0 - 92);
            for (var k = 1; k <= 90; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - changed);
                var g = visual.HeightBase;
                if (motion.HeightRemainingDp > 0)
                {
                    var expected = SeriesTimelines.Spring(spring.HeightBase, spring.Velocity, 92, MotionSpecs.SeriesHeightOmega, t / 1000.0);
                    Assert.That(g, Is.EqualTo(expected.Value).Within(Eps), "el resorte toma el alto nuevo desde su posición y su velocidad, a los " + t);
                }

                var push = t < 240 ? SeriesTimelines.RowsChange(t, 1, false, false).RowsPushBase : 0;
                var shift = t < 160 ? SeriesTimelines.DataResize(t, tailFloor, 0, false) : 0;
                Assert.That(visual.RowsPushBase, Is.LessThanOrEqualTo(push + Eps), "las filas nunca van más rápido que el empuje, a los " + t);
                Assert.That(visual.RowsPushBase, Is.GreaterThanOrEqualTo(-22 - Eps), "ni suben de donde estaban");
                Assert.That(visual.RowsPushBase <= g - 92 + Eps || Math.Abs(visual.RowsPushBase + 22) <= Eps, Is.True,
                    "ni pasan el vidrio, a los " + t);
                Assert.That(visual.TailShiftBase, Is.LessThanOrEqualTo(shift + Eps), "la cola tampoco, a los " + t);
                Assert.That(visual.TailShiftBase, Is.GreaterThanOrEqualTo(tailFloor - Eps));
                Assert.That(visual.TailShiftBase <= g - 92 + Eps || Math.Abs(visual.TailShiftBase - tailFloor) <= Eps, Is.True);
                Assert.That(visual.ContentAlpha, Is.EqualTo(SeriesTimelines.Open(HudTimelines.Ms(rig.Now - opened), Chip, Laps0, 0, false).ContentAlpha)
                    .Within(Eps).Or.EqualTo(1), "los fundidos de abrir siguen los suyos");
            }

            AssertRest(visual, 92, true);
            rig.AssertIdle("todo llegó");
        }

        // ── La capa de estado (§6.10; DD5-28) ───────────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-09")]
        public void TheStateLayerFadesFromWhereItIs()
        {
            foreach (var reduced in new[] { false, true })
            {
                var rig = new Rig(reduceMotion: reduced);
                var visual = default(SeriesPanelVisual);
                var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
                motion.SetExpanded(false, Chip, true);
                Assert.That(HoverFrames(rig, motion, () => visual, true, false, 0, MotionSpecs.SeriesHoverAlpha), Is.EqualTo(90),
                    "entra en 90 (reducido: " + reduced + ")");
                Assert.That(motion.IsRunning, Is.False, "un hover quieto no bloquea: es un estado");
                Assert.That(HoverFrames(rig, motion, () => visual, true, true, MotionSpecs.SeriesHoverAlpha, MotionSpecs.SeriesPressedAlpha),
                    Is.EqualTo(90), "presionado");
                Assert.That(HoverFrames(rig, motion, () => visual, true, false, MotionSpecs.SeriesPressedAlpha, MotionSpecs.SeriesHoverAlpha),
                    Is.EqualTo(160), "soltar con el mouse encima");
                Assert.That(HoverFrames(rig, motion, () => visual, false, false, MotionSpecs.SeriesHoverAlpha, 0), Is.EqualTo(160), "sale");

                // Un PointerDown en medio del fundido sigue desde ahí, sin saltos.
                motion.Hover(true, false, false);
                rig.RunFor(50);
                var mid = visual.HoverAlpha;
                Assert.That(mid, Is.InRange(0.001, MotionSpecs.SeriesHoverAlpha - 0.001));
                var pressedAt = rig.Now;
                motion.Hover(true, true, false);
                Assert.That(visual.HoverAlpha, Is.EqualTo(mid).Within(Eps));
                rig.Frame();
                Assert.That(visual.HoverAlpha, Is.EqualTo(SeriesTimelines.Hover(HudTimelines.Ms(rig.Now - pressedAt), mid, MotionSpecs.SeriesPressedAlpha))
                    .Within(Eps));

                // Instant (el panel inerte en el acto, el segundo plano): su objetivo, en el acto.
                motion.Hover(false, false, true);
                Assert.That(visual.HoverAlpha, Is.EqualTo(0));
                Assert.That(motion.IsRunning, Is.False);

                // Stop (la salida, Dispose): 0 en el acto, sin avisos.
                motion.Hover(true, true, false);
                rig.RunFor(30);
                var calls = 0;
                var stopped = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, _ => calls++);
                stopped.Hover(true, true, false);
                rig.RunFor(30);
                stopped.Stop();
                motion.Stop();
                Assert.That(stopped.Visual.HoverAlpha, Is.EqualTo(0), "Stop la deja en 0");
                calls = 0;
                rig.RunFor(500);
                Assert.That(calls, Is.EqualTo(0));
                rig.AssertIdle("Stop");
            }
        }

        // ── Instant, sin armar y reducir movimiento (§4.3; AC-WKT-02-08, AC-APP-05-08) ─────────────────────────────────

        [Test, Category("AC-WKT-02-08")]
        public void InstantAndUnarmedGoToTheFinalState()
        {
            var unarmed = new Rig(armed: false);
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(unarmed.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, 90, false);
            AssertRest(visual, 90, true);
            var change = HudSeriesChange.Between(Laps(1, true, 0, true), Laps(1, true, 1, true));
            motion.ContentChanged(in change, 114, false);
            AssertRest(visual, 114, true);
            Assert.That(motion.IsRunning, Is.False, "antes del primer cuadro, nada se anima");

            // Armado, con Instant (volver del segundo plano en medio de una apertura y de una marca): todo a su final.
            var rig = new Rig();
            var panel = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            panel.SetExpanded(false, Chip, true);
            panel.SetExpanded(true, 90, false);
            rig.RunFor(100);
            panel.ContentChanged(in change, 114, true);
            AssertRest(visual, 114, true);
            Assert.That(panel.IsRunning, Is.False);
            rig.AssertIdle("Instant");

            // La rotación (Retarget con Instant): en el acto; sin Instant, el resorte con la cola pegada al vidrio.
            panel.Retarget(130, true);
            AssertRest(visual, 130, true);
            var start = rig.Now;
            panel.Retarget(100, false);
            for (var k = 1; k <= 20; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                if (panel.HeightRemainingDp > 0)
                {
                    Assert.That(visual.HeightBase, Is.EqualTo(SeriesTimelines.Spring(130, 0, 100, MotionSpecs.SeriesHeightOmega, t / 1000.0).Value).Within(Eps));
                }

                Assert.That(visual.TailShiftBase, Is.EqualTo(visual.HeightBase - 100).Within(Eps));
                Assert.That(visual.ContentAlpha, Is.EqualTo(1), "sin abrir ni cerrar");
                Assert.That(panel.IsTimedPhaseRunning, Is.False);
                Assert.That(panel.RemainingDp, Is.EqualTo(panel.HeightRemainingDp));
            }
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedMotionVariants()
        {
            var rig = new Rig(reduceMotion: true);
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(false, Chip, true);

            // Abrir: el alto en el cuadro 0, el contenido en 0-160 lineal, la flecha con dos trazos.
            var start = rig.Now;
            motion.SetExpanded(true, 90, false);
            Assert.That(visual.HeightBase, Is.EqualTo(90), "el alto en el cuadro del toque");
            for (var k = 1; k <= 15; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                var expected = SeriesTimelines.Open(t, Chip, 90, 0, true);
                Assert.That(visual.HeightBase, Is.EqualTo(90));
                if (t < 200)
                {
                    Assert.That(visual.ContentAlpha, Is.EqualTo(expected.ContentAlpha).Within(Eps), "contenido a los " + t);
                    Assert.That(visual.ChevronDegrees, Is.EqualTo(0), "no gira");
                    Assert.That(visual.ChevronDownAlpha, Is.EqualTo(expected.ChevronDownAlpha).Within(Eps));
                    Assert.That(visual.ChevronUpAlpha, Is.EqualTo(expected.ChevronUpAlpha).Within(Eps));
                }
            }

            rig.RunFor(100);
            rig.AssertIdle("abierto");

            // La vuelta nueva: todo en su lugar en el cuadro 0, la nueva funde en 0-160, la marca igual.
            var change = HudSeriesChange.Between(Laps(1, true, 0, false), Laps(1, true, 1, false));
            start = rig.Now;
            motion.ContentChanged(in change, 92, false);
            Assert.That(visual.HeightBase, Is.EqualTo(92), "el alto en el acto si crece");
            Assert.That(visual.RowsPushBase, Is.EqualTo(0));
            for (var k = 1; k <= 12; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                var rows = SeriesTimelines.RowsChange(t, 1, false, true);
                Assert.That(visual.NewRowsShiftBase, Is.EqualTo(0), "sin deslizarse");
                Assert.That(visual.NewRowsAlpha, Is.EqualTo(t < 160 ? rows.NewRowsAlpha : 1).Within(Eps));
                Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t, true)).Within(Eps), "la marca, igual");
                Assert.That(visual.TailShiftBase, Is.EqualTo(0));
            }

            // El cambio de contenido: los mismos fundidos sin subir; el alto a los 120 si se achica.
            rig.RunFor(1500);
            var swap = HudSeriesChange.Between(Laps(1, true, 1, false), Laps(2, false, 0, false));
            start = rig.Now;
            motion.ContentChanged(in swap, 84, false);
            for (var k = 1; k <= 18; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.BodyNewRiseBase, Is.EqualTo(0), "sin subir");
                Assert.That(visual.BodyNewAlpha, Is.EqualTo(t < 280 ? SeriesTimelines.Swap(t, true, true).NewAlpha : 1).Within(Eps));
                Assert.That(visual.HeightBase, Is.EqualTo(t < 120 ? 92 : 84), "a los 120, en el acto");
            }

            // Cerrar: el contenido en 0-120 lineal y el alto a los 120.
            rig.RunFor(500);
            start = rig.Now;
            motion.SetExpanded(false, Chip, false);
            for (var k = 1; k <= 12; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.HeightBase, Is.EqualTo(t < 120 ? 84 : Chip), "a los " + t);
                if (t < 200)
                {
                    Assert.That(visual.ContentAlpha, Is.EqualTo(SeriesTimelines.Close(t, 84, Chip, 0, true).ContentAlpha).Within(Eps));
                }
            }
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedMotionTurnedOnMidwaySendsWhatMovesToItsEnd()
        {
            // QA-V4b-MOT-03: se lee en cada cuadro; lo que se mueve va a su final en el acto y las marcas siguen (son opacidad).
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var motion = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => visual = v);
            motion.SetExpanded(true, Laps0, true);
            var change = HudSeriesChange.Between(Laps(1, true, 0, false), Laps(1, true, 1, false));
            var start = rig.Now;
            motion.ContentChanged(in change, 92, false);
            rig.RunFor(100);
            Assert.That(visual.HeightBase, Is.LessThan(92));
            rig.Settings.ReduceMotion = true;
            rig.Frame();
            var t = HudTimelines.Ms(rig.Now - start);
            Assert.That(visual.HeightBase, Is.EqualTo(92), "el alto, a su final");
            Assert.That(visual.TailShiftBase, Is.EqualTo(0));
            Assert.That(visual.RowsPushBase, Is.EqualTo(0), "el empuje, a su final");
            Assert.That(visual.NewRowsAlpha, Is.EqualTo(1));
            Assert.That(visual.MarkerAlpha0, Is.EqualTo(SeriesTimelines.Marker(t, false)).Within(Eps), "la marca sigue");

            // El resorte de una apertura, también.
            var otherVisual = default(SeriesPanelVisual);
            var other = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Reps, v => otherVisual = v);
            rig.Settings.ReduceMotion = false;
            other.SetExpanded(false, Chip, true);
            other.SetExpanded(true, 104, false);
            rig.RunFor(100);
            Assert.That(otherVisual.HeightBase, Is.LessThan(104));
            rig.Settings.ReduceMotion = true;
            rig.Frame();
            AssertRest(otherVisual, 104, true);
            Assert.That(other.IsRunning, Is.False);
        }

        // ── Stop: nada queda animando (§4.4; AC-WKT-13-09, ROB-02) ────────────────────────────────────────────────────

        [Test, Category("AC-WKT-13-09")]
        public void StopLeavesNothingAnimatingAndNoCallbacks()
        {
            var rig = new Rig();
            var visual = default(SeriesPanelVisual);
            var calls = 0;
            var laps = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v =>
            {
                visual = v;
                calls++;
            });
            var columnCalls = 0;
            var gone = 0;
            var column = new SeriesColumnMotion(rig.Driver, _ => columnCalls++);
            laps.SetExpanded(true, 90, true);
            var change = HudSeriesChange.Between(Laps(1, true, 0, true), Laps(1, true, 1, true));
            laps.ContentChanged(in change, 114, false);
            rig.RunFor(300);
            laps.Hover(true, false, false);
            laps.SetExpanded(false, Chip, false);
            column.RepsLeave(false, () => gone++);
            column.FollowSpm(true, false);
            column.WheelTo(22, false);
            rig.RunFor(50);
            Assert.That(laps.IsFinite && column.IsFinite, Is.True, "el resorte, la marca, la capa de estado y la columna en camino");
            var height = visual.HeightBase;

            laps.Stop();
            column.Stop();
            rig.AssertIdle("Dispose del HUD: nada queda animando (ROB-02)");
            Assert.That(laps.Visual.HeightBase, Is.EqualTo(height), "lo que se ve queda donde estaba");
            Assert.That(laps.Visual.HoverAlpha, Is.EqualTo(0), "salvo la capa de estado");
            calls = 0;
            columnCalls = 0;
            rig.RunFor(3000);
            Assert.That(calls + columnCalls + gone, Is.EqualTo(0), "ningún aviso después de Stop (tampoco gone)");
            rig.AssertIdle("y nada vuelve");

            // Al volver a un panel (QA-G5T-12) la vista llama con Instant: el estado final.
            laps.Retarget(Chip, true);
            AssertRest(visual, Chip, false);
            column.RepsAppear(true);
            Assert.That(column.Visual.RepsAlpha, Is.EqualTo(1));
            Assert.That(column.Visual.FollowBase, Is.EqualTo(0));
            Assert.That(column.Visual.ScrollBase, Is.EqualTo(22), "el desplazamiento en su destino");
            rig.AssertIdle("en su final");
        }

        // ── La columna (§6.4 a §6.6) ───────────────────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-18-01")]
        public void TheRepsPanelAppearsAndLeavesWithTheSeries()
        {
            var rig = new Rig();
            var visual = default(SeriesColumnVisual);
            var gone = 0;
            var goneAt = -1L;
            var motion = new SeriesColumnMotion(rig.Driver, v => visual = v);
            motion.RepsLeave(true, () => gone++);
            Assert.That(gone, Is.EqualTo(1), "sin serie: fuera en el acto");
            Assert.That(visual.RepsAlpha, Is.EqualTo(0));

            var start = rig.Now;
            motion.RepsAppear(false);
            for (var k = 0; k <= 30; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = SeriesTimelines.RepsPanel(t, true, false);
                Assert.That(visual.RepsAlpha, Is.EqualTo(t < 400 ? expected.Opacity : 1).Within(Eps), "aparece en 120-400, a los " + t);
                Assert.That(visual.RepsRiseBase, Is.EqualTo(t < 400 ? expected.RiseBase : 0).Within(Eps), "+8 → 0");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 400), "F en la entrada, a los " + t);
            }

            rig.AssertIdle("a la vista");
            start = rig.Now;
            Action onGone = () =>
            {
                gone++;
                goneAt = HudTimelines.Ms(rig.Now - start);
            };
            motion.RepsLeave(false, onGone);
            for (var k = 0; k <= 12; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.RepsAlpha, Is.EqualTo(t < 120 ? SeriesTimelines.RepsPanel(t, false, false).Opacity : 0).Within(Eps), "se va en 0-120");
                Assert.That(visual.RepsRiseBase, Is.EqualTo(0), "sin moverse");
                Assert.That(gone, Is.EqualTo(t < 120 ? 1 : 2), "gone una vez, al terminar");
            }

            Assert.That(goneAt, Is.InRange(120L, 137L));
            rig.AssertIdle("fuera");

            // Una salida que corta una entrada sigue desde la opacidad que tiene.
            motion.RepsAppear(false);
            rig.RunFor(200);
            var mid = visual.RepsAlpha;
            Assert.That(mid, Is.InRange(0.1, 0.9));
            motion.RepsLeave(false, () => gone++);
            Assert.That(visual.RepsAlpha, Is.EqualTo(mid).Within(Eps), "sin saltos");
            rig.Frame();
            Assert.That(visual.RepsAlpha, Is.LessThan(mid));
            // Y una entrada que corta la salida: el panel se queda, sin gone.
            var goneBefore = gone;
            motion.RepsAppear(false);
            rig.RunFor(600);
            Assert.That(visual.RepsAlpha, Is.EqualTo(1));
            Assert.That(gone, Is.EqualTo(goneBefore), "la salida cancelada no avisa");
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheColumnFollowsSpm()
        {
            foreach (var gaining in new[] { true, false })
            {
                var rig = new Rig();
                var visual = default(SeriesColumnVisual);
                var motion = new SeriesColumnMotion(rig.Driver, v => visual = v);
                var start = rig.Now;
                motion.FollowSpm(gaining, false);
                var end = SeriesTimelines.ColumnFollowEndMs(gaining, false);
                for (var k = 0; k <= 20; k++)
                {
                    if (k > 0)
                    {
                        rig.Frame();
                    }

                    var t = HudTimelines.Ms(rig.Now - start);
                    Assert.That(visual.FollowBase, Is.EqualTo(t < end ? SeriesTimelines.ColumnFollow(t, gaining, false) : 0).Within(Eps),
                        "16,9·s con los tiempos de la 4a, a los " + t);
                    Assert.That(motion.IsFinite, Is.EqualTo(t < end));
                }

                rig.AssertIdle("quieta");
            }

            // Un corrimiento que no terminó sigue con el nuevo, sin saltos (relativo a su lugar final nuevo).
            var r = new Rig();
            var v2 = default(SeriesColumnVisual);
            var column = new SeriesColumnMotion(r.Driver, v => v2 = v);
            column.FollowSpm(true, false);
            r.RunFor(100);
            var before = v2.FollowBase;
            column.FollowSpm(false, false);
            Assert.That(v2.FollowBase, Is.EqualTo(before + SeriesTimelines.SpmTargetGrowthBase).Within(Eps));
            r.RunFor(400);
            Assert.That(v2.FollowBase, Is.EqualTo(0));

            // Con reducir movimiento, en el acto.
            var reduced = new Rig(reduceMotion: true);
            var v3 = default(SeriesColumnVisual);
            var fast = new SeriesColumnMotion(reduced.Driver, v => v3 = v);
            fast.FollowSpm(true, false);
            Assert.That(v3.FollowBase, Is.EqualTo(0));
            reduced.AssertIdle("en el acto");
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheWheelSpringsOneRowAndShiftScrollKeepsWhatIsSeen()
        {
            var rig = new Rig();
            var visual = default(SeriesColumnVisual);
            var motion = new SeriesColumnMotion(rig.Driver, v => visual = v) { Scale = 1.054 };
            var start = rig.Now;
            motion.WheelTo(SeriesTimelines.WheelStepBase, false);
            rig.RunFor(100);
            var t = HudTimelines.Ms(rig.Now - start);
            Assert.That(visual.ScrollBase, Is.EqualTo(SeriesTimelines.Wheel(t, 0, 22, 0, false).Value).Within(Eps), "spring.snappy");
            Assert.That(motion.IsFinite, Is.True);

            // Una fila que entra con la vista desplazada: +22 en el mismo cuadro, y el resorte sigue igual, corrido.
            var at = visual.ScrollBase;
            motion.ShiftScroll(22);
            Assert.That(visual.ScrollBase, Is.EqualTo(at + 22).Within(Eps), "lo que se ve no se mueve (DD5-27)");
            Assert.That(motion.ScrollTargetBase, Is.EqualTo(44).Within(Eps));
            rig.Frame();
            t = HudTimelines.Ms(rig.Now - start);
            Assert.That(visual.ScrollBase, Is.EqualTo(SeriesTimelines.Wheel(t, 22, 44, 0, false).Value).Within(Eps));
            rig.RunFor(800);
            Assert.That(visual.ScrollBase, Is.EqualTo(44));
            rig.AssertIdle("la rueda llegó");

            // El arrastre 1:1 y la rotación (Instant) y reducir movimiento: en el acto.
            motion.WheelTo(10, true);
            Assert.That(visual.ScrollBase, Is.EqualTo(10));
            rig.Settings.ReduceMotion = true;
            motion.WheelTo(32, false);
            Assert.That(visual.ScrollBase, Is.EqualTo(32));
            rig.AssertIdle("en el acto");
        }

        // ── La cascada (§3.3, §6.7; AC-WKT-13-06, -07) ─────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-13-06"), Category("AC-WKT-13-07")]
        public void TheCascadeBringsThePanelsWithSpm()
        {
            var count = Enum.GetValues(typeof(HudPiece)).Length;
            foreach (var reduced in new[] { false, true })
            {
                var rig = new Rig(reduceMotion: reduced);
                var opacity = new double[count];
                var shift = new double[count];
                var respondedAt = new long[count];
                for (var i = 0; i < count; i++)
                {
                    respondedAt[i] = -1;
                }

                var start = rig.Now;
                var motion = new ImmersiveCascadeMotion(rig.Driver, (p, v) =>
                {
                    opacity[(int)p] = v.Opacity;
                    shift[(int)p] = v.ShiftDp;
                }, _ => { }, p => respondedAt[(int)p] = HudTimelines.Ms(rig.Now - start), () => { });
                motion.Play();
                for (var k = 0; k <= 70; k++)
                {
                    if (k > 0)
                    {
                        rig.Frame();
                    }

                    var t = HudTimelines.Ms(rig.Now - start);
                    foreach (var piece in new[] { HudPiece.SeriesLaps, HudPiece.SeriesReps })
                    {
                        var expected = ImmersiveTimeline.Entry(piece, Math.Min(t, ImmersiveTimeline.EntryEndMs(reduced)), reduced);
                        Assert.That(opacity[(int)piece], Is.EqualTo(expected.Opacity).Within(Eps), piece + " a los " + t);
                        Assert.That(shift[(int)piece], Is.EqualTo(expected.ShiftDp).Within(Eps));
                        Assert.That(opacity[(int)piece], Is.EqualTo(opacity[(int)HudPiece.Spm]), "con el tramo de SPM");
                        Assert.That(motion.PieceOpacity(piece), Is.EqualTo(expected.Opacity).Within(Eps));
                    }
                }

                var from = reduced ? 0L : 520L;
                Assert.That(respondedAt[(int)HudPiece.SeriesLaps], Is.InRange(from, from + 17), "responden desde su tramo");
                Assert.That(respondedAt[(int)HudPiece.SeriesReps], Is.InRange(from, from + 17));
                Assert.That(motion.IsInteractive(HudPiece.SeriesLaps), Is.True);
                rig.AssertIdle("la cascada terminó");
            }
        }

        // ── 0 B por cuadro (§4.5; DD5-11) ──────────────────────────────────────────────────────────────────────────────

        [Test]
        public void NoAllocationsPerFrame()
        {
            // El C# de las motions (sin repintado: eso lo mide HBT.SeriesNoAllocationsPerFrame con cuadros reales). Primero una vuelta
            // de calentamiento con las mismas escenas (JIT, inicializaciones), después las medidas, 30 cuadros cada una. Con su
            // control positivo: la misma medición sobre una motion cuyo callback asigna en cada cuadro tiene que verlo.
            Scenes(out _);
            Scenes(out var allocations);
            Assert.That(allocations, Is.EqualTo(0), "0 B por cuadro con las motions de la It. 5: " + allocations + " asignaciones");

            var rig = new Rig();
            var sink = 0;
            var control = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => sink += new byte[64].Length + (int)v.HeightBase);
            control.SetExpanded(false, Chip, true);
            control.SetExpanded(true, 180, false);
            var positive = RowMate.Tests.UiSupport.GcAllocations.During(() =>
            {
                for (var i = 0; i < 30; i++)
                {
                    rig.Frame();
                }
            });
            Assert.That(positive, Is.GreaterThanOrEqualTo(30), "el control positivo ve su byte[64] por cuadro: " + positive);
            Assert.That(sink, Is.GreaterThan(0));
            control.Stop();
        }

        // Las animaciones de §4.1, cada una 30 cuadros medidos: abrir, cerrar, la vuelta nueva con la marca, el cambio por pieza, el
        // dato con el resorte corriendo, la serie que empieza (la línea Rep y repeticiones que aparece), la capa de estado, la
        // columna que sigue a SPM, la rueda y la salida de repeticiones.
        private static void Scenes(out int allocations)
        {
            var rig = new Rig();
            var sink = 0.0;
            var laps = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Laps, v => sink += v.HeightBase + v.MarkerAlpha0);
            var reps = new SeriesPanelMotion(rig.Driver, SeriesPanelKind.Reps, v => sink += v.HeightBase);
            var column = new SeriesColumnMotion(rig.Driver, v => sink += v.RepsAlpha + v.FollowBase + v.ScrollBase);
            Action gone = () => sink += 1;
            var newLap = HudSeriesChange.Between(Laps(1, true, 0, false), Laps(1, true, 1, false));
            var swap = HudSeriesChange.Between(Laps(1, true, 1, false), Laps(2, false, 0, false));
            var started = HudSeriesChange.Between(Laps(3, true, 0, false), Laps(3, true, 0, true));
            var firstRep = HudSeriesChange.Between(Reps(2), Reps(3, 1));
            allocations = 0;

            laps.SetExpanded(false, Chip, true);
            laps.SetExpanded(true, Laps0, false);
            allocations += Measure(rig);
            rig.RunFor(1500);
            laps.ContentChanged(in newLap, 92, false);
            allocations += Measure(rig);
            rig.RunFor(1500);
            laps.ContentChanged(in swap, 84, false);
            allocations += Measure(rig);
            laps.SetExpanded(false, Chip, false);
            allocations += Measure(rig);
            rig.RunFor(1500);
            laps.SetExpanded(true, Laps0, false);
            rig.RunFor(150);
            laps.ContentChanged(in newLap, 92, false);
            allocations += Measure(rig);
            rig.RunFor(1500);
            column.RepsLeave(true, gone);
            laps.ContentChanged(in started, 114, false);
            reps.SetExpanded(true, 84, true);
            reps.ContentChanged(in firstRep, 60, false);
            column.RepsAppear(false);
            allocations += Measure(rig);
            laps.Hover(true, true, false);
            column.FollowSpm(true, false);
            column.WheelTo(22, false);
            allocations += Measure(rig);
            laps.Hover(false, false, false);
            column.FollowSpm(false, false);
            column.RepsLeave(false, gone);
            column.ShiftScroll(22);
            allocations += Measure(rig);
            laps.Stop();
            reps.Stop();
            column.Stop();
            rig.AssertIdle("las escenas terminaron");
        }

        private static int Measure(Rig rig)
        {
            return RowMate.Tests.UiSupport.GcAllocations.During(() =>
            {
                for (var i = 0; i < 30; i++)
                {
                    rig.Frame();
                }
            });
        }

        // ── Soporte ────────────────────────────────────────────────────────────────────────────────────────────────────

        private static readonly IReadOnlyList<HudLapRow> NoLaps = new HudLapRow[0];

        private static readonly IReadOnlyList<HudRepRow> NoReps = new HudRepRow[0];

        // Parciales: el paso, si es trabajo, cuántas vueltas cerró (se ven las 4 últimas, la más reciente primero) y si está en serie.
        private static HudSeries Laps(int step, bool work, int lapCount, bool inSeries)
        {
            var rows = new List<HudLapRow>();
            for (var n = lapCount; n > 0 && rows.Count < SeriesPanels.VisibleLaps; n--)
            {
                rows.Add(new HudLapRow(n, "#" + n, "2:05.0", 125));
            }

            return new HudSeries(1, step, work, "0:00.0", 0, "0 m", 0, rows, lapCount, inSeries, 1, inSeries ? "Rep 1/3 · 0 m" : null, 0, NoReps);
        }

        // Repeticiones en serie, en el paso dado: las anteriores, la más reciente primero.
        private static HudSeries Reps(int step, params int[] reps)
        {
            var rows = new List<HudRepRow>();
            foreach (var rep in reps)
            {
                rows.Add(new HudRepRow(rep, "Rep " + rep, "2:05", 125, "500 m", 500));
            }

            return new HudSeries(1, step, true, "0:00.0", 0, "0 m", 0, NoLaps, 0, true, reps.Length + 1, "Rep 1/3 · 0 m", 0, rows);
        }

        // Cuántos ms tarda la capa de estado en llegar, cuadro a cuadro igual a su línea de tiempo y finita mientras corre.
        private static long HoverFrames(Rig rig, SeriesPanelMotion motion, Func<SeriesPanelVisual> visual, bool hovered, bool pressed, double from,
                                        double to)
        {
            var start = rig.Now;
            motion.Hover(hovered, pressed, false);
            var end = SeriesTimelines.HoverEndMs(from, to);
            for (var k = 0; k <= 15; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual().HoverAlpha, Is.EqualTo(t < end ? SeriesTimelines.Hover(t, from, to) : to).Within(Eps), "la capa a los " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < end), "F mientras corre, a los " + t);
            }

            return end;
        }

        private static void AssertRest(in SeriesPanelVisual actual, double heightBase, bool expanded)
        {
            var rest = SeriesPanelVisual.Rest(heightBase, expanded);
            Assert.That(actual.HeightBase, Is.EqualTo(rest.HeightBase).Within(Eps), "alto");
            Assert.That(actual.TailShiftBase, Is.EqualTo(0), "cola");
            Assert.That(actual.ContentAlpha, Is.EqualTo(rest.ContentAlpha), "contenido");
            Assert.That(actual.ChipLabelAlpha, Is.EqualTo(rest.ChipLabelAlpha), "rótulo");
            Assert.That(actual.TitleAlpha, Is.EqualTo(rest.TitleAlpha), "título");
            Assert.That(actual.ChevronDegrees, Is.EqualTo(rest.ChevronDegrees), "flecha");
            Assert.That(actual.ChevronDownAlpha, Is.EqualTo(rest.ChevronDownAlpha));
            Assert.That(actual.ChevronUpAlpha, Is.EqualTo(rest.ChevronUpAlpha));
            Assert.That(actual.RowsPushBase, Is.EqualTo(0), "sin empuje");
            Assert.That(actual.PushProgress, Is.EqualTo(1));
            Assert.That(actual.NewRowsAlpha, Is.EqualTo(1));
            Assert.That(actual.NewRowsShiftBase, Is.EqualTo(0));
            Assert.That(actual.NewRows, Is.EqualTo(0));
            Assert.That(actual.DroppedAlpha, Is.EqualTo(0));
            Assert.That(actual.ClearedRowsAlpha, Is.EqualTo(0));
            Assert.That(actual.MarkerAlpha0 + actual.MarkerAlpha1 + actual.MarkerAlpha2 + actual.MarkerAlpha3, Is.EqualTo(0), "sin marcas");
            Assert.That(actual.BodyOldAlpha, Is.EqualTo(0));
            Assert.That(actual.BodyNewAlpha, Is.EqualTo(1));
            Assert.That(actual.BodyNewRiseBase, Is.EqualTo(0));
            Assert.That(actual.RepLineAlpha, Is.EqualTo(1));
            Assert.That(actual.RepLineRiseBase, Is.EqualTo(0));
        }

        // Un MotionDriver con el reloj de prueba, sin panel: cada Frame es un cuadro de 1/60 s (como el de HudMotionTests).
        private sealed class Rig
        {
            private const long FrameTicks = 166667;

            public Rig(bool reduceMotion = false, bool armed = true)
            {
                Settings.ReduceMotion = reduceMotion;
                Driver = new MotionDriver(Clock, Pacing, Settings);
                if (armed)
                {
                    Driver.Tick();
                }
            }

            public FakeScheduler Clock { get; } = new FakeScheduler();

            public FramePacingPolicy Pacing { get; } = new FramePacingPolicy();

            public MotionSettings Settings { get; } = new MotionSettings();

            public MotionDriver Driver { get; }

            public double Now => Clock.MonotonicSeconds;

            public void Frame()
            {
                Clock.Advance(TimeSpan.FromTicks(FrameTicks));
                Driver.Tick();
            }

            // Cuadros de 1/60 s durante ms.
            public void RunFor(double ms)
            {
                var end = Clock.Elapsed.Ticks + (long)Math.Round(ms * TimeSpan.TicksPerMillisecond);
                while (Clock.Elapsed.Ticks + FrameTicks <= end)
                {
                    Frame();
                }

                if (Clock.Elapsed.Ticks < end)
                {
                    Clock.Advance(TimeSpan.FromTicks(end - Clock.Elapsed.Ticks));
                    Driver.Tick();
                }
            }

            public void AssertIdle(string because)
            {
                Assert.That(Driver.ActiveCount, Is.EqualTo(0), "nada en el MotionDriver: " + because);
                Assert.That(Pacing.Demands, Is.EqualTo(0), "nadie pide 60 fps: " + because);
            }
        }
    }
}
