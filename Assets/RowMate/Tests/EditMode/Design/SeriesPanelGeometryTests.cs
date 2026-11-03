using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // SPG: la geometría de los paneles de series (05-series-parciales/02-tech-design §3.2; 00-deuda-diseno §2; 04-design-notes §4 y
    // §5; DD5-03, DD5-13 a DD5-15, DD5-19, DD5-20, DD5-26): la tabla de 04-design-notes §5.1 (± 0,1), los altos naturales (R-01),
    // el reparto de RowsScroll, las áreas de toque (tope 56, mínimo 48, sin superponerse), la zona del bote, la cuenta grande, lo
    // que no entra se achica (el piso de 0,6), la escala de título por idioma, el corte parejo de los vacíos y las reglas de alfa
    // por la tinta.
    [TestFixture]
    public sealed class SeriesPanelGeometryTests
    {
        private const double Tol = 0.1;

        // Perfil, s, sp, x izquierda, x derecha, ancho dp, techo con y sin objetivo, piso, lugar con y sin (base), regla.
        private static readonly object[] Columns =
        {
            new object[] { "TV", 1.054, 1.054, 14.8, 159.1, 144.4, 228.6, 210.9, 682.2, 430.3, 447.2, SeriesFitRule.FitAll },
            new object[] { "P360", 0.923, 0.923, 12.9, 139.4, 126.5, 200.3, 184.7, 596.9, 429.7, 446.6, SeriesFitRule.FitAll },
            new object[] { "TH", 1.054, 1.054, 14.8, 159.1, 144.4, 228.6, 210.9, 309.8, 77.1, 93.9, SeriesFitRule.ColumnScroll },
            new object[] { "L640", 0.923, 0.923, 12.9, 139.4, 126.5, 200.3, 184.7, 271.4, 77.1, 93.9, SeriesFitRule.ColumnScroll },
            new object[] { "W1280", 1.6, 1.20, 22.4, 186.8, 164.4, 347.1, 320.2, 566.4, 182.7, 205.2, SeriesFitRule.RowsScroll },
        };

        [TestCaseSource(nameof(Columns)), Category("AC-WKT-18-13")]
        public void TheColumnOfEachProfile(string profile, double s, double sp, double left, double right, double width, double topWith,
                                           double topWithout, double floor, double roomWith, double roomWithout, SeriesFitRule rule)
        {
            var column = Column(profile);
            Assert.That(column.HudScale, Is.EqualTo(s).Within(0.001), profile + " s");
            Assert.That(column.Scale, Is.EqualTo(sp).Within(profile == "W1280" ? 0.005 : 0.001), profile + " sp (W1280: 1,20, no 1,6)");
            Assert.That(column.Left, Is.EqualTo(left).Within(Tol), profile + " izquierda");
            Assert.That(column.Right, Is.EqualTo(right).Within(Tol), profile + " derecha");
            Assert.That(column.Width, Is.EqualTo(width).Within(Tol), profile + " ancho");
            Assert.That(column.WidthBase, Is.EqualTo(SeriesPanelGeometry.PanelWidth), "137 base, fijo, cerrado y abierto");
            Assert.That(column.TopWithTarget, Is.EqualTo(topWith).Within(Tol), profile + " techo con objetivo");
            Assert.That(column.TopWithoutTarget, Is.EqualTo(topWithout).Within(Tol), profile + " techo sin objetivo (R-12)");
            Assert.That(column.Floor, Is.EqualTo(floor).Within(Tol), profile + " piso");
            Assert.That(column.RoomWithTargetBase, Is.EqualTo(roomWith).Within(Tol), profile + " lugar con objetivo");
            Assert.That(column.RoomWithoutTargetBase, Is.EqualTo(roomWithout).Within(Tol), profile + " lugar sin objetivo (R-13)");
            Assert.That(column.Rule, Is.EqualTo(rule), profile);
            Assert.That(2 * SeriesPanelGeometry.ChipHeight + SeriesPanelGeometry.ColumnGap, Is.LessThanOrEqualTo(column.RoomWithTargetBase),
                profile + ": los dos chips enteros, sin desplazar, también con SPM con objetivo (76 de 77,1)");
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheWidthNarrowsOnlyBelowTheMinimumWindow()
        {
            // min(137; ventanaBase / 2 − 58): en todo teléfono de 351 dp o más el panel termina 8·s antes del disco.
            Assert.That(SeriesPanelGeometry.Column(Layout(340, 760)).WidthBase, Is.LessThan(137));
            Assert.That(SeriesPanelGeometry.Column(Layout(351, 780)).WidthBase, Is.EqualTo(137).Within(0.3));
            Assert.That(SeriesPanelGeometry.WorstCaseHeight, Is.EqualTo(292), "180 + 8 + 104");
        }

        [Test, Category("AC-WKT-18-04"), Category("AC-WKT-18-07")]
        public void NaturalHeights()
        {
            // 00-deuda-diseno §2.4 con R-01 (la primera vuelta suma 24 y cada una más 22): cerrado 34; parciales 68 / 90 sin y con la
            // serie, 92-158 / 114-180 con 1 a 4 vueltas, 84 / 106 fuera de un trabajo; repeticiones 60, 82, 104 y 84 vacío.
            Assert.That(SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Laps, false, Content(true, 4, true, 3)), Is.EqualTo(34));
            Assert.That(SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Reps, false, Content(true, 4, true, 3)), Is.EqualTo(34));
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 0, false), Is.EqualTo(68));
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 0, true), Is.EqualTo(90));
            Assert.That(new[] { 1, 2, 3, 4 }, Has.All.Matches<int>(n => SeriesPanelGeometry.LapsOpenHeight(true, n, false) == 92 + 22 * (n - 1)));
            Assert.That(new[] { 1, 2, 3, 4 }, Has.All.Matches<int>(n => SeriesPanelGeometry.LapsOpenHeight(true, n, true) == 114 + 22 * (n - 1)));
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 1, true) - SeriesPanelGeometry.LapsOpenHeight(true, 0, true), Is.EqualTo(24), "+24 la primera");
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 9, true), Is.EqualTo(180), "nunca más de 4 a la vista");
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(false, 0, false), Is.EqualTo(84));
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(false, 0, true), Is.EqualTo(106));
            Assert.That(SeriesPanelGeometry.RepsOpenHeight(1), Is.EqualTo(60));
            Assert.That(SeriesPanelGeometry.RepsOpenHeight(2), Is.EqualTo(82));
            Assert.That(SeriesPanelGeometry.RepsOpenHeight(3), Is.EqualTo(104));
            Assert.That(SeriesPanelGeometry.RepsOpenHeight(0), Is.EqualTo(84));
            Assert.That(SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Laps, true, Content(true, 1, true, 0)), Is.EqualTo(114));
            Assert.That(SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Reps, true, Content(true, 1, true, 2)), Is.EqualTo(82));
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 4, true) + 8 + SeriesPanelGeometry.RepsOpenHeight(3), Is.EqualTo(292), "lo más alto");
            Assert.That(SeriesPanelGeometry.LapsOpenHeight(true, 1, true) + 8 + SeriesPanelGeometry.RepsOpenHeight(1),
                Is.EqualTo(SeriesPanelGeometry.MinViewHeight), "la vista mínima, 182");
        }

        [Test, Category("AC-WKT-18-04")]
        public void TheVerticalOfEachRow()
        {
            // 04-design-notes §4.4 a §4.6: la vuelta en curso en 32, la fila k en 64 + 22·k; la línea Rep en 66 sin vueltas, 68 + 22·n
            // con n y 82 fuera de un trabajo; las filas de repeticiones en 32 + 22·k; los renglones de un vacío en 32 y 54.
            Assert.That(SeriesPanelGeometry.ContentTop, Is.EqualTo(32));
            Assert.That(SeriesPanelGeometry.LapRowTop(0), Is.EqualTo(64));
            Assert.That(SeriesPanelGeometry.LapRowTop(3), Is.EqualTo(130));
            Assert.That(SeriesPanelGeometry.RepLineTop(true, 0), Is.EqualTo(66));
            Assert.That(SeriesPanelGeometry.RepLineTop(true, 1), Is.EqualTo(90));
            Assert.That(SeriesPanelGeometry.RepLineTop(true, 4), Is.EqualTo(156));
            Assert.That(SeriesPanelGeometry.RepLineTop(false, 0), Is.EqualTo(82));
            Assert.That(SeriesPanelGeometry.RepRowTop(2), Is.EqualTo(76));
            Assert.That(SeriesPanelGeometry.EmptyLineTop(1), Is.EqualTo(54));
            Assert.That(SeriesPanelGeometry.Baseline(SeriesRowKind.Hero), Is.EqualTo(23.80));
            Assert.That(SeriesPanelGeometry.InkBottom(SeriesRowKind.RepRow), Is.EqualTo(19.89), "la p de Rep");
            Assert.That(SeriesPanelGeometry.InkBottom(SeriesRowKind.HeaderLabel, true), Is.EqualTo(16.87), "Reps tiene p");
            // Con una escala de ajuste, la tinta se achica alrededor del centro de la banda.
            Assert.That(SeriesPanelGeometry.InkBottomAt(SeriesRowKind.Hero, 32, 0.5), Is.EqualTo(32 + 14 + (23.80 - 14) * 0.5).Within(1e-9));
            Assert.That(SeriesPanelGeometry.InkTopAt(SeriesRowKind.LapRow, 64, 1), Is.EqualTo(67));
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheRowsScrollShareInWideWindows()
        {
            // DD5-26 (04-design-notes §5.2), W1280: repeticiones tiene reservado su mínimo; parciales recibe min(natural; lugar − 8 −
            // ese mínimo) y repeticiones lo que queda.
            var column = Column("W1280");
            Check(column, true, Content(true, 4, true, 3), true, true, 114.7, 60, 1, 1);
            Check(column, false, Content(true, 4, true, 3), true, true, 137.2, 60, 2, 1);
            Check(column, true, Content(true, 1, true, 0), true, true, 114, 60.7, 1, -1);
            Check(column, false, Content(true, 1, true, 0), true, true, 114, 83.2, 1, -1);
            Check(column, true, Content(true, 4, true, 3), true, false, 140.7, 34, 2, 0);
            Check(column, false, Content(true, 4, true, 3), true, false, 163.2, 34, 3, 0);
            // CAP-18 (1 vuelta y 1 repetición, 182): entra todo, sin desplazar.
            Check(column, true, Content(true, 1, true, 1), true, true, 114, 60, 1, 1);
            Check(column, false, Content(true, 1, true, 1), true, true, 114, 60, 1, 1);
            // Sin repeticiones a la vista, parciales recibe min(su natural; lugar).
            SeriesPanelGeometry.Place(column, true, 158, 34, false, true, false, Content(true, 4, false, 0), out var laps, out _, out var content);
            Assert.That(laps.HeightBase, Is.EqualTo(158));
            Assert.That(content, Is.EqualTo(158));
        }

        [TestCase("TV")]
        [TestCase("P360")]
        [TestCase("TH")]
        [TestCase("L640")]
        [Category("AC-WKT-18-13")]
        public void FitAllAndColumnScrollKeepTheNaturalHeights(string profile)
        {
            // FitAll: todo a su natural; ColumnScroll: cada uno a su natural y la columna entera se desplaza (columnContentBase).
            var column = Column(profile);
            var content = Content(true, 4, true, 3);
            SeriesPanelGeometry.Place(column, true, 180, 104, true, true, true, content, out var laps, out var reps, out var total);
            Assert.That(laps.HeightBase, Is.EqualTo(180));
            Assert.That(reps.HeightBase, Is.EqualTo(104));
            Assert.That(total, Is.EqualTo(292));
            Assert.That(laps.RowsViewportBase, Is.EqualTo(laps.RowsContentBase), "las filas enteras");
            Assert.That(reps.TopDp, Is.EqualTo(laps.TopDp + (180 + 8) * column.Scale).Within(1e-9), "8·sp entre los dos");
            if (column.Rule == SeriesFitRule.FitAll)
            {
                Assert.That(total, Is.LessThanOrEqualTo(column.RoomWithTargetBase));
            }
        }

        // Perfil, alto del chip (dp), lo que llega 500 m y Reps, con SPM con objetivo (el peor caso).
        private static readonly object[] Hits =
        {
            new object[] { "TV", 35.8, 56.0, 56.0 },
            new object[] { "P360", 31.4, 51.7, 56.0 },
            new object[] { "TH", 35.8, 56.0, 56.0 },
            new object[] { "L640", 31.4, 51.7, 52.7 },
            new object[] { "W1280", 40.8, 56.0, 56.0 },
        };

        [TestCaseSource(nameof(Hits)), Category("AC-WKT-18-09")]
        public void TheTouchAreasOfTheChips(string profile, double chip, double laps, double reps)
        {
            // 04-design-notes §5.5 (R-14): cerrado, el chip agrandado hacia arriba y hacia abajo hasta 56 dp (tope; 48 como mínimo),
            // cada uno con la mitad del aire entre los dos: nunca se superponen.
            var column = Column(profile);
            var content = Content(true, 0, true, 0);
            SeriesPanelGeometry.Place(column, true, 34, 34, true, false, false, content, out var lapsPlace, out var repsPlace, out _);
            Assert.That(34 * column.Scale, Is.EqualTo(chip).Within(Tol), profile + " chip");
            Assert.That(lapsPlace.HitRect.Height, Is.EqualTo(laps).Within(Tol), profile + " 500 m");
            Assert.That(repsPlace.HitRect.Height, Is.EqualTo(reps).Within(Tol), profile + " Reps");
            Assert.That(lapsPlace.HitRect.Height, Is.GreaterThanOrEqualTo(SeriesPanelGeometry.HitMinDp));
            Assert.That(repsPlace.HitRect.Height, Is.GreaterThanOrEqualTo(SeriesPanelGeometry.HitMinDp));
            Assert.That(lapsPlace.HitRect.Height, Is.LessThanOrEqualTo(SeriesPanelGeometry.HitRoomyDp + 1e-9));
            Assert.That(lapsPlace.HitRect.Bottom, Is.LessThanOrEqualTo(repsPlace.HitRect.Top + 1e-9), "no se superponen");
            Assert.That(lapsPlace.HitRect.Bottom, Is.EqualTo(lapsPlace.TopDp + 34 * column.Scale + 4 * column.Scale).Within(1e-6),
                "500 m baja hasta la mitad del aire");
            var layout = LayoutOf(profile);
            Assert.That(lapsPlace.HitRect.Top, Is.GreaterThanOrEqualTo(column.TopWithTarget - 18 * column.HudScale - 1e-6),
                "hacia arriba, a lo sumo el aire con SPM y su relleno de abajo (no responde)");
            Assert.That(lapsPlace.HitRect.Left, Is.EqualTo(column.Left));
            Assert.That(lapsPlace.HitRect.Width, Is.EqualTo(column.Width).Within(1e-9));
            Assert.That(layout.Spm.Bottom, Is.LessThan(lapsPlace.TopDp));
        }

        [Test, Category("AC-WKT-18-09")]
        public void WithoutTargetTheRepsChipOfLSixFortyReachesTheCap()
        {
            var column = Column("L640");
            SeriesPanelGeometry.Place(column, false, 34, 34, true, false, false, Content(true, 0, true, 0), out _, out var reps, out _);
            Assert.That(reps.HitRect.Height, Is.EqualTo(56).Within(Tol));
        }

        [Test, Category("AC-WKT-18-09")]
        public void OpenTheTouchAreaIsTheWholePanel()
        {
            var column = Column("TV");
            SeriesPanelGeometry.Place(column, false, 114, 60, true, true, true, Content(true, 1, true, 1), out var laps, out var reps, out _);
            Assert.That(laps.HitRect.Top, Is.EqualTo(laps.TopDp));
            Assert.That(laps.HitRect.Height, Is.EqualTo(114 * column.Scale).Within(1e-9));
            Assert.That(reps.HitRect.Height, Is.EqualTo(60 * column.Scale).Within(1e-9));
            Assert.That(laps.TopDp, Is.EqualTo(210.9).Within(Tol), "CAP-02: parciales en y 210,9");
            Assert.That(laps.TopDp + 114 * column.Scale, Is.EqualTo(331.0).Within(Tol));
            Assert.That(reps.TopDp, Is.EqualTo(339.4).Within(Tol), "el chip Reps a 8·sp del borde");
        }

        [Test, Category("AC-WKT-18-14"), Category("AC-WKT-17-09")]
        public void TheBoatZone()
        {
            // Cerrados nunca entran; abiertos en vertical solo en la franja de afuera (x ≤ ancho / 2 − 44·s); en horizontal, nunca.
            var tv = LayoutOf("TV");
            var column = SeriesPanelGeometry.Column(tv);
            var chips = DpRect.FromEdges(column.Left, column.TopWithoutTarget, column.Right, column.TopWithoutTarget + 76 * column.Scale);
            Assert.That(chips.Bottom, Is.EqualTo(291.0).Within(Tol));
            Assert.That(tv.BoatZone.Top - chips.Bottom, Is.EqualTo(32.7).Within(Tol), "CAP-01: a 32,7 dp de la zona del bote");
            Assert.That(SeriesPanelGeometry.RespectsBoatZone(chips, false, tv), Is.True);
            var open = DpRect.FromEdges(column.Left, 314.1, column.Right, 377.4);
            Assert.That(open.Intersects(tv.BoatZone), Is.True, "CAP-03: repeticiones abierto entra en la zona");
            Assert.That(SeriesPanelGeometry.RespectsBoatZone(open, true, tv), Is.True, "solo en la franja de afuera (hasta 159,1)");
            Assert.That(SeriesPanelGeometry.RespectsBoatZone(open, false, tv), Is.False, "cerrado no podría");
            var wide = DpRect.FromEdges(column.Left, 314.1, column.Left + 200 * column.Scale, 377.4);
            Assert.That(SeriesPanelGeometry.RespectsBoatZone(wide, true, tv), Is.False, "con 200, pasa el centro del remero");
            foreach (var profile in new[] { "TH", "L640", "W1280" })
            {
                var layout = LayoutOf(profile);
                var c = SeriesPanelGeometry.Column(layout);
                var panel = DpRect.FromEdges(c.Left, c.TopWithTarget, c.Right, c.Floor);
                Assert.That(SeriesPanelGeometry.RespectsBoatZone(panel, true, layout), Is.True, profile);
                Assert.That(layout.BoatZone.Left - panel.Right, Is.GreaterThan(30), profile + ": lejos de la zona");
                var inside = DpRect.FromEdges(layout.BoatZone.Left - 10, c.TopWithTarget, layout.BoatZone.Left + 10, c.Floor);
                Assert.That(SeriesPanelGeometry.RespectsBoatZone(inside, true, layout), Is.False, profile + ": en horizontal, nunca");
            }

            Assert.That(LayoutOf("TH").BoatZone.Left - Column("TH").Right, Is.EqualTo(96.8).Within(Tol));
            Assert.That(LayoutOf("L640").BoatZone.Left - Column("L640").Right, Is.EqualTo(39.8).Within(Tol));
            Assert.That(LayoutOf("W1280").BoatZone.Left - Column("W1280").Right, Is.EqualTo(171.6).Within(Tol));
        }

        [TestCase("TV", false)]
        [TestCase("P360", false)]
        [TestCase("TH", false)]
        [TestCase("L640", false)]
        [TestCase("W1280", false)]
        [Category("AC-WKT-18-15"), Category("AC-WKT-14-18")]
        public void TheBigCountIsNeverBlockedWith137(string profile, bool expected)
        {
            // Con 137 el panel termina 8·s antes del disco (TV: 8,4 dp; P360: 7,4): la cuenta grande y PAUSA siempre en su lugar.
            var layout = LayoutOf(profile);
            var column = SeriesPanelGeometry.Column(layout);
            SeriesPanelGeometry.Place(column, true, 180, 104, true, true, true, Content(true, 4, true, 3), out var laps, out var reps, out _);
            var lapsRect = new DpRect(column.Left, laps.TopDp, column.Width, laps.HeightBase * column.Scale);
            var repsRect = new DpRect(column.Left, reps.TopDp, column.Width, reps.HeightBase * column.Scale);
            Assert.That(SeriesPanelGeometry.BlocksCallout(lapsRect, repsRect, layout), Is.EqualTo(expected), profile);
            if (!layout.IsLandscape)
            {
                Assert.That(layout.BigCount.Left - column.Right, Is.EqualTo(8 * layout.Scale).Within(Tol), profile + ": 8·s de aire");
            }
        }

        [TestCase("TV", true)]
        [TestCase("P360", true)]
        [TestCase("TH", false)]
        [TestCase("L640", false)]
        [TestCase("W1280", false)]
        [Category("AC-WKT-18-15")]
        public void WithTheLegacyWidthTheRuleHidesTheBigCount(string profile, bool expected)
        {
            // La regla de reserva (P-04; spec §12 punto 6): con los 200 del legacy abiertos, en TV y P360 se cruzan con el disco.
            var layout = LayoutOf(profile);
            var column = SeriesPanelGeometry.Column(layout);
            var wide = new DpRect(column.Left, column.TopWithTarget, 200 * column.Scale, 180 * column.Scale);
            Assert.That(SeriesPanelGeometry.BlocksCallout(wide, null, layout), Is.EqualTo(expected), profile);
        }

        [TestCase("TV")]
        [TestCase("P360")]
        [TestCase("TH")]
        [TestCase("L640")]
        [TestCase("W1280")]
        [Category("AC-WKT-18-16"), Category("AC-WKT-13-13")]
        public void NothingOfTheFourBIsCrossed(string profile)
        {
            // Lo que se ve de la columna (de su techo hasta su piso, o hasta lo más alto que mide) no se cruza con ninguna pieza de la
            // 4b: SPM, Split, Watts, kcal, BPM, la banda, los controles, la barra en su alto máximo, el disco ni la llave.
            foreach (var hasTarget in new[] { true, false })
            {
                var layout = Layout(HudGeometryCases.Profile(profile)[0], HudGeometryCases.Profile(profile)[1], true,
                    hasTarget ? HudGeometryCases.RoutineWithBpm : HudGeometryCases.FreeWithBpm);
                var column = SeriesPanelGeometry.Column(layout);
                var top = column.Top(hasTarget);
                var bottom = Math.Min(top + SeriesPanelGeometry.WorstCaseHeight * column.Scale, column.Floor);
                var visible = DpRect.FromEdges(column.Left, top, column.Right, bottom);
                var pieces = new List<KeyValuePair<string, DpRect>>
                {
                    new KeyValuePair<string, DpRect>("SPM", layout.Spm), new KeyValuePair<string, DpRect>("Split", layout.Split),
                    new KeyValuePair<string, DpRect>("Watts", layout.Watts), new KeyValuePair<string, DpRect>("kcal", layout.Kcal),
                    new KeyValuePair<string, DpRect>("banda", layout.Band), new KeyValuePair<string, DpRect>("controles", layout.Controls),
                    new KeyValuePair<string, DpRect>("barra", layout.TopBarMax), new KeyValuePair<string, DpRect>("disco", layout.BigCount),
                };
                if (layout.Bpm.HasValue)
                {
                    pieces.Add(new KeyValuePair<string, DpRect>("BPM", layout.Bpm.Value));
                }

                if (layout.Wrench.HasValue)
                {
                    pieces.Add(new KeyValuePair<string, DpRect>("llave", layout.Wrench.Value));
                }

                foreach (var piece in pieces)
                {
                    Assert.That(visible.Intersects(piece.Value), Is.False, profile + (hasTarget ? " con" : " sin") + " objetivo: " + piece.Key);
                }

                Assert.That(visible.IsInside(layout.WindowWidth, layout.WindowHeight), Is.True);
            }
        }

        [Test, Category("AC-WKT-18-17")]
        public void WhatDoesNotFitShrinks()
        {
            // 04-design-notes §4.7 (las escalas del Designer, K2): una escala pareja; lo que en el piso (0,6) no entra se recorta.
            Assert.That(SeriesPanelGeometry.FitScale(117, 120.7, SeriesPanelGeometry.SeriesNumberMinScale), Is.EqualTo(0.969).Within(0.001),
                "Rep 1 2:05 1.112 m (CAP-11)");
            Assert.That(SeriesPanelGeometry.FitScale(117, 123.3), Is.EqualTo(0.949).Within(0.001), "Rep 3 1:54 1.245 m");
            Assert.That(SeriesPanelGeometry.FitScale(117, 114.3), Is.EqualTo(1), "Rep 2/3 · 1.245 m entra");
            Assert.That(SeriesPanelGeometry.FitScale(117, 164.3), Is.EqualTo(0.712).Within(0.001), "S-LARGO: la línea Rep");
            Assert.That(SeriesPanelGeometry.FitScale(117, 183.5), Is.EqualTo(0.638).Within(0.001), "S-LARGO: las filas de repeticiones");
            Assert.That(SeriesPanelGeometry.FitScale(117, 125.6), Is.EqualTo(0.932).Within(0.001), "S-LARGO: las filas de vueltas");
            // La vuelta en curso: solo la cifra, con los metros a 13 y su aire (38,7 base con 499 m): 0,861 y 0,652.
            Assert.That(SeriesPanelGeometry.FitScale(117 - 38.7, 129.6 - 38.7), Is.EqualTo(0.861).Within(0.002), "10:02.4 + 499 m");
            Assert.That(SeriesPanelGeometry.FitScale(117 - 38.7, 158.8 - 38.7), Is.EqualTo(0.652).Within(0.002), "S-LARGO: 5999:59.9 + 499 m");
            // El piso: S-LARGO llega a 0,638 (nunca se recorta); un split de días con un metro de trabajo queda en 0,6 y se recorta.
            Assert.That(SeriesPanelGeometry.FitScale(117, 300, SeriesPanelGeometry.SeriesNumberMinScale), Is.EqualTo(0.6));
            Assert.That(SeriesPanelGeometry.SeriesNumberMinScale, Is.EqualTo(0.6));
            Assert.That(SeriesPanelGeometry.FitScale(117, 0), Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-18-17")]
        public void OneTitleScalePerLanguage()
        {
            // DD5-20: la menor de los dos títulos, aunque el de repeticiones no se vea: es 0,949 (por Series anteriores, 90,3 de 85,7);
            // en 1 (500 m splits 65,2 y Previous reps 72,6). Hasta 0,8 y, debajo, …
            Assert.That(SeriesPanelGeometry.TitleScale(85.2, 90.3), Is.EqualTo(0.949).Within(0.005));
            Assert.That(SeriesPanelGeometry.TitleScale(65.2, 72.6), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.TitleScale(85.2, 150), Is.EqualTo(SeriesPanelGeometry.TitleMinScale));
        }

        [Test, Category("AC-WKT-18-05"), Category("AC-WKT-18-07")]
        public void TheEmptyTextsBreakIntoTwoEvenLines()
        {
            // DD5-19: de los cortes que dejan las dos líneas en 117 o menos, el de la línea más larga más corta. Anchos de palabra de
            // prueba con los totales de 04-design-notes §4.8 (los mide Unity, R5-02; el espacio, 4).
            const double space = 4;
            // Sin parciales (90,4) / en este paso (90,9): no "Sin parciales en" / "este paso".
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 24.0, 62.4, 16.0, 30.0, 36.9 }, space), Is.EqualTo(1));
            // No splits (61,6) / in this step (76,9).
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 20.0, 37.6, 12.0, 26.0, 30.9 }, space), Is.EqualTo(1));
            // Sin repeticiones (113,7) / completas (73,6): el único que entra.
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 24.0, 85.7, 73.6 }, space), Is.EqualTo(1));
            // No completed (99,5) / reps (31,2): 99,5 es más corto que "No" / "completed reps" (110,7).
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 20.0, 75.5, 31.2 }, space), Is.EqualTo(1));
            // Un empate: el primero. Ninguno entra: −1 (la regla del título).
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 10.0, 10.0, 10.0, 10.0 }, 0, 20), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 30.0, 30.0, 30.0 }, 0, 25), Is.EqualTo(-1));
            Assert.That(SeriesPanelGeometry.BalancedBreak(new[] { 50.0 }, space), Is.EqualTo(-1), "una sola palabra no se corta");
        }

        [Test, Category("AC-WKT-18-18"), Category("AC-WKT-18-04")]
        public void AtRestEveryRowIsFullyVisible()
        {
            // DD5-13 (regla 1, revelar): α = clamp((G − tintaAbajo) / 4); en reposo (G = el alto natural) todo renglón al 100 %: el
            // último termina su tinta entre 7,2 (la línea Rep) y 11 antes del final del vidrio.
            Assert.That(SeriesPanelGeometry.RevealAlpha(90, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.RepLine, SeriesPanelGeometry.RepLineTop(true, 0))),
                Is.EqualTo(1), "la línea Rep sin vueltas (82,8 de 90)");
            Assert.That(SeriesPanelGeometry.RevealAlpha(180, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.RepLine, SeriesPanelGeometry.RepLineTop(true, 4))),
                Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.RevealAlpha(158, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.LapRow, SeriesPanelGeometry.LapRowTop(3))),
                Is.EqualTo(1), "la cuarta vuelta sin serie");
            Assert.That(SeriesPanelGeometry.RevealAlpha(68, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.Hero, SeriesPanelGeometry.ContentTop)), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.RevealAlpha(104, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.RepRow, SeriesPanelGeometry.RepRowTop(2))), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.RevealAlpha(84, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.EmptyLine, SeriesPanelGeometry.EmptyLineTop(1))), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.RevealAlpha(106, SeriesPanelGeometry.InkBottomAt(SeriesRowKind.RepLine, SeriesPanelGeometry.RepLineTop(false, 0))),
                Is.EqualTo(1));
            // Mientras el vidrio crece: 0 hasta que cubre toda la tinta; 1 cuando la pasa por 4 (VID-01: la vuelta en curso aparece
            // cuando el vidrio pasa 55,8).
            Assert.That(SeriesPanelGeometry.RevealAlpha(55.8, 55.8), Is.EqualTo(0));
            Assert.That(SeriesPanelGeometry.RevealAlpha(57.8, 55.8), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(SeriesPanelGeometry.RevealAlpha(52, 55.8), Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheRowsWindowFadesWhatLeavesIt()
        {
            // DD5-14 (regla 2): abajo siempre; arriba solo con el desplazamiento > 0; 4 base de desborde por la tinta.
            Assert.That(SeriesPanelGeometry.RowsEdgeAlpha(70, 80, 64, 80, false), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.RowsEdgeAlpha(70, 82, 64, 80, false), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(SeriesPanelGeometry.RowsEdgeAlpha(70, 84, 64, 80, false), Is.EqualTo(0));
            Assert.That(SeriesPanelGeometry.RowsEdgeAlpha(60, 70, 64, 80, false), Is.EqualTo(1), "arriba sin desplazamiento: sin borde");
            Assert.That(SeriesPanelGeometry.RowsEdgeAlpha(62, 70, 64, 80, true), Is.EqualTo(0.5).Within(1e-9));
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheColumnMaskOfTHAsInCapSixteen()
        {
            // DD5-15 (regla 3; CAP-16): TH, Series 600 a los 380 s, los dos abiertos: parciales (90) desde 210,9; la máscara de 297,2
            // a 309,8 (el piso); la línea Rep (tinta hasta 82,8) a 0,925 (± 0,02); el vidrio de parciales en su borde de abajo
            // (305,7) en 0,33 (± 0,03). Con todo lo que queda escondido debajo (180 − 93,9 base), la fuerza es 1.
            var column = Column("TH");
            var sp = column.Scale;
            var top = column.TopWithoutTarget;
            var columnBottomBase = (column.Floor - top) / sp;
            var hidden = 90 + 8 + 82 - column.RoomWithoutTargetBase;
            Assert.That(column.Floor - 12 * sp, Is.EqualTo(297.2).Within(Tol), "la máscara empieza en 297,2");
            Assert.That(top + 90 * sp, Is.EqualTo(305.7).Within(Tol));
            var repLine = SeriesPanelGeometry.InkBottomAt(SeriesRowKind.RepLine, SeriesPanelGeometry.RepLineTop(true, 0));
            Assert.That(SeriesPanelGeometry.ColumnMaskAlpha(repLine, repLine, 0, columnBottomBase, 0, hidden), Is.EqualTo(0.925).Within(0.02));
            Assert.That(SeriesPanelGeometry.ColumnMaskAlpha(90, 90, 0, columnBottomBase, 0, hidden), Is.EqualTo(0.33).Within(0.03));
            Assert.That(SeriesPanelGeometry.ColumnMaskAlpha(40, 40, 0, columnBottomBase, 0, hidden), Is.EqualTo(1), "la cabecera al 100 %");
            // CAP-15: los dos chips en 76 de 93,9: nada pasa el borde, sin máscara.
            Assert.That(SeriesPanelGeometry.ColumnMaskAlpha(76, 76, 0, columnBottomBase, 0, 0), Is.EqualTo(1));
            Assert.That(SeriesPanelGeometry.ColumnMaskAlpha(90, 90, 0, columnBottomBase, 0, 6), Is.EqualTo(1 - 0.5 * (1 - 0.33)).Within(0.03),
                "la fuerza evita que aparezca de golpe");
        }

        [Test, Category("AC-WKT-18-04")]
        public void TheAnatomyIsConstants()
        {
            // DD5-23 (04-design-notes §4.2 a §4.6): el contenido de x 10 a 127, el ícono en x 10-24, el rótulo y el título desde x 28,
            // la flecha alrededor de (122,5; 17) con su tinta de x 118 a 127, la marca en x 4-7.
            Assert.That(SeriesPanelGeometry.ContentRight - SeriesPanelGeometry.ContentLeft, Is.EqualTo(117));
            Assert.That(SeriesPanelGeometry.PanelWidth - SeriesPanelGeometry.PadH, Is.EqualTo(SeriesPanelGeometry.ContentRight));
            Assert.That(SeriesPanelGeometry.IconLeft + SeriesPanelGeometry.Icon + SeriesPanelGeometry.IconGap, Is.EqualTo(SeriesPanelGeometry.LabelLeft));
            Assert.That(SeriesPanelGeometry.ChevronCenterX - SeriesPanelGeometry.ChevronHalfWidth, Is.EqualTo(118.75));
            Assert.That(SeriesPanelGeometry.ChevronCenterX + SeriesPanelGeometry.ChevronHalfWidth + SeriesPanelGeometry.SeriesChevronStroke / 2,
                Is.EqualTo(SeriesPanelGeometry.ContentRight), "la tinta de la flecha pegada al borde del contenido");
            Assert.That(SeriesPanelGeometry.MarkerLeft + SeriesPanelGeometry.MarkerWidth, Is.EqualTo(7));
            Assert.That(SeriesPanelGeometry.HeaderTop + SeriesPanelGeometry.HeaderHeight / 2, Is.EqualTo(SeriesPanelGeometry.ChevronCenterY));
            Assert.That(SeriesPanelGeometry.GlassRadius, Is.EqualTo(Tokens.RGlass.Value));
        }

        // ── Lo común ──

        private static void Check(in SeriesColumnLayout column, bool target, in SeriesPanelsContent content, bool repsShown, bool repsExpanded,
                                  double laps, double reps, int lapRowsSeen, int repRowsSeen)
        {
            var lapsNatural = SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Laps, true, content);
            var repsNatural = SeriesPanelGeometry.NaturalHeight(SeriesPanelKind.Reps, repsExpanded, content);
            SeriesPanelGeometry.Place(column, target, lapsNatural, repsNatural, repsShown, true, repsExpanded, content, out var l, out var r, out var total);
            var label = (target ? "con" : "sin") + " objetivo, " + content.VisibleLaps + " vueltas, reps " + (repsExpanded ? content.PreviousReps.Count.ToString() : "cerrado");
            Assert.That(l.HeightBase, Is.EqualTo(laps).Within(Tol), label + ": parciales");
            Assert.That(r.HeightBase, Is.EqualTo(reps).Within(Tol), label + ": repeticiones");
            Assert.That(total, Is.LessThanOrEqualTo(column.RoomBase(target) + 1e-9), label + ": entra en la columna");
            Assert.That(RowsSeen(l.RowsViewportBase), Is.EqualTo(lapRowsSeen), label + ": filas de vueltas a la vista");
            if (repRowsSeen >= 0)
            {
                Assert.That(RowsSeen(r.RowsViewportBase), Is.EqualTo(repRowsSeen), label + ": filas de repeticiones a la vista");
            }
        }

        // Cuántas filas de 20 (con 2 entre filas) entran enteras en la ventana.
        private static int RowsSeen(double viewport)
        {
            var n = 0;
            while (SeriesPanelGeometry.RowsHeight(n + 1) <= viewport + 1e-9)
            {
                n++;
            }

            return n;
        }

        private static SeriesPanelsContent Content(bool isWork, int laps, bool inSeries, int reps)
        {
            var list = new RepSummary[reps];
            for (var i = 0; i < reps; i++)
            {
                list[i] = new RepSummary(reps - i, 680, 600, 150);
            }

            return new SeriesPanelsContent(true, 1, isWork, 0, 0, laps, laps > 0 ? new Lap(laps, 125) : default, laps > 1 ? new Lap(laps - 1, 125) : default,
                laps > 2 ? new Lap(laps - 2, 125) : default, laps > 3 ? new Lap(laps - 3, 125) : default, inSeries, 1, 3, 0, list);
        }

        private static SeriesColumnLayout Column(string profile)
        {
            return SeriesPanelGeometry.Column(LayoutOf(profile));
        }

        private static HudLayout LayoutOf(string profile)
        {
            var size = HudGeometryCases.Profile(profile);
            return Layout(size[0], size[1]);
        }

        private static HudLayout Layout(double width, double height, bool simulator = false, HudCase? values = null)
        {
            return HudGeometry.Layout(HudGeometryCases.Input(width, height, SafeInsets.None, simulator, values ?? HudGeometryCases.Free));
        }
    }
}
