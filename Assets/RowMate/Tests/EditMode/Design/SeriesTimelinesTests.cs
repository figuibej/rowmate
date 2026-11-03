using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // STL: las líneas de tiempo puras de los paneles de series (05-series-parciales/02-tech-design §4.1; 04-design-notes §6.1 a
    // §6.10): abrir y cerrar con el resorte (VID-01), el alto por un dato con el empuje (DD5-16; VID-03, VID-04), la cola, la fila
    // nueva en 80-240 (DD5-17; VID-02), la marca, el cambio por pieza (DD5-18), la serie que empieza y termina, la columna, la
    // rueda, el hover; los aires mínimos; la inversión a mitad de camino; reducir movimiento fila por fila (§6.8); la cascada.
    // Las tablas de las notas son por cuadro de 30 fps (n/30 s) y las líneas de tiempo van en ms enteros (HudTimelines.Ms): por eso
    // la tolerancia del alto es 0,15 base (la del resorte entre 133 y 133,3 ms) y la de los alfas, 0,01.
    [TestFixture]
    public sealed class SeriesTimelinesTests
    {
        private const double Alpha = 0.01;
        private const double Height = 0.15;

        [Test, Category("AC-WKT-18-18")]
        public void OpenAsInVidOne()
        {
            // §6.1: parciales de 34 a 90 base (VID-01).
            long[] t = { 0, 33, 67, 100, 133, 167, 200, 233, 267, 300, 333, 367, 400, 467, 533, 600, 700 };
            double[] height = { 34.0, 37.4, 44.7, 52.9, 60.6, 67.3, 72.7, 77.1, 80.4, 83.0, 84.9, 86.3, 87.3, 88.6, 89.3, 89.7, 89.9 };
            double[] content = { 0, 0, 0, 0.331, 0.701, 0.901, 0.983, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            double[] chevron = { 0, 3, 17, 51, 117, 159, 175, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180 };
            for (var i = 0; i < t.Length; i++)
            {
                var v = SeriesTimelines.Open(t[i], 34, 90, 0, false);
                Assert.That(v.HeightBase, Is.EqualTo(height[i]).Within(Height), "alto a los " + t[i]);
                Assert.That(v.ContentAlpha, Is.EqualTo(content[i]).Within(Alpha), "contenido a los " + t[i]);
                Assert.That(v.ChevronDegrees, Is.EqualTo(chevron[i]).Within(1.0), "flecha a los " + t[i]);
                Assert.That(v.ChevronDownAlpha, Is.EqualTo(1), "sin reducir movimiento la flecha gira");
            }

            double[] label = { 1, 0.925, 0.421, 0, 0, 0, 0 };
            double[] title = { 0, 0, 0, 0.422, 0.826, 0.977, 1 };
            for (var i = 0; i < label.Length; i++)
            {
                var v = SeriesTimelines.Open(t[i], 34, 90, 0, false);
                Assert.That(v.ChipLabelAlpha, Is.EqualTo(label[i]).Within(Alpha), "rótulo a los " + t[i]);
                Assert.That(v.TitleAlpha, Is.EqualTo(title[i]).Within(Alpha), "título a los " + t[i]);
            }
        }

        [Test, Category("AC-WKT-18-18")]
        public void CloseFromTheEighty()
        {
            // §6.1: cerrar de 90 a 34: el contenido 0,977 · 0,826 · 0,422 · 0 a los 33 · 67 · 100 · 133; el alto se queda en 90 hasta
            // los 80 y baja: 88,6 · 82,4 · 74,4 · 66,4 · 59,3 · 48,5 a los 100 · 133 · 167 · 200 · 233 · 300; 39,8 a los 400 y 34,8 a
            // los 600.
            Assert.That(SeriesTimelines.Close(33, 90, 34, 0, false).ContentAlpha, Is.EqualTo(0.977).Within(Alpha));
            Assert.That(SeriesTimelines.Close(67, 90, 34, 0, false).ContentAlpha, Is.EqualTo(0.826).Within(Alpha));
            Assert.That(SeriesTimelines.Close(100, 90, 34, 0, false).ContentAlpha, Is.EqualTo(0.422).Within(Alpha));
            Assert.That(SeriesTimelines.Close(133, 90, 34, 0, false).ContentAlpha, Is.EqualTo(0).Within(Alpha));
            Assert.That(SeriesTimelines.Close(80, 90, 34, 0, false).HeightBase, Is.EqualTo(90));
            long[] t = { 100, 133, 167, 200, 233, 300, 400, 600 };
            double[] height = { 88.6, 82.4, 74.4, 66.4, 59.3, 48.5, 39.8, 34.8 };
            for (var i = 0; i < t.Length; i++)
            {
                Assert.That(SeriesTimelines.Close(t[i], 90, 34, 0, false).HeightBase, Is.EqualTo(height[i]).Within(Height), "alto a los " + t[i]);
            }

            var end = SeriesTimelines.Close(240, 90, 34, 0, false);
            Assert.That(end.ChevronDegrees, Is.EqualTo(0));
            Assert.That(end.TitleAlpha, Is.EqualTo(0));
            Assert.That(end.ChipLabelAlpha, Is.EqualTo(1), "el rótulo entra en 80-200");
            Assert.That(SeriesTimelines.Close(140, 90, 34, 0, false).ChipLabelAlpha, Is.InRange(0.3, 0.99));
        }

        [Test, Category("AC-WKT-18-18")]
        public void ATapMidwayInvertsFromWhereItIs()
        {
            // Un toque durante la animación la invierte desde donde está: el resorte con su posición y su velocidad; los fundidos,
            // desde su opacidad; la flecha, desde su ángulo, con lo que le falta de los 240. Sin saltos en el cuadro del toque.
            var mid = SeriesTimelines.Open(133, 34, 90, 0, false);
            Assert.That(mid.Velocity, Is.GreaterThan(0), "subiendo");
            var start = mid.ToStart();
            var inverted = SeriesTimelines.Close(0, start, 34, false);
            Assert.That(inverted.HeightBase, Is.EqualTo(mid.HeightBase).Within(1e-9), "el alto no salta");
            Assert.That(inverted.Velocity, Is.EqualTo(mid.Velocity).Within(1e-9), "la velocidad sigue");
            Assert.That(inverted.ContentAlpha, Is.EqualTo(mid.ContentAlpha).Within(1e-9));
            Assert.That(inverted.ChevronDegrees, Is.EqualTo(mid.ChevronDegrees).Within(1e-9));
            Assert.That(inverted.TitleAlpha, Is.EqualTo(mid.TitleAlpha).Within(1e-9));
            var a = SeriesTimelines.Close(17, start, 34, false);
            Assert.That(a.HeightBase, Is.GreaterThan(mid.HeightBase), "el resorte sigue subiendo un poco antes de volver (inercia, sin salto)");
            Assert.That(a.ContentAlpha, Is.LessThan(mid.ContentAlpha), "el contenido ya se va");
            Assert.That(a.ChevronDegrees, Is.LessThan(mid.ChevronDegrees));
            var later = SeriesTimelines.Close(1200, start, 34, false);
            Assert.That(later.HeightBase, Is.EqualTo(34).Within(0.05));
            Assert.That(later.ContentAlpha, Is.EqualTo(0));
            Assert.That(later.ChevronDegrees, Is.EqualTo(0));
            Assert.That(later.ChipLabelAlpha, Is.EqualTo(1));
            // Y otra vez al revés: abrir desde el cierre a medio camino.
            var back = SeriesTimelines.Open(0, SeriesTimelines.Close(200, 90, 34, 0, false).ToStart(), 90, false);
            Assert.That(back.HeightBase, Is.EqualTo(SeriesTimelines.Close(200, 90, 34, 0, false).HeightBase).Within(1e-9));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheHeightByADataChangeGoesWithThePush()
        {
            // DD5-16 (§6.3): crece en 0-160 y se achica en 80-240 (ease.move). VID-03 (114 → 106): 113,9 · 112,8 · 109,0 · 106,6 ·
            // 106,0 a los 100 · 133 · 167 · 200 · 233. VID-04: 105,8 · 103,7 · 96,1 · 91,1 · 90,0 (106 → 90) y 83,7 · 80,5 · 69,1 ·
            // 61,7 · 60,0 (84 → 60).
            long[] t = { 100, 133, 167, 200, 233 };
            double[] vid3 = { 113.9, 112.8, 109.0, 106.6, 106.0 };
            double[] laps = { 105.8, 103.7, 96.1, 91.1, 90.0 };
            double[] reps = { 83.7, 80.5, 69.1, 61.7, 60.0 };
            for (var i = 0; i < t.Length; i++)
            {
                Assert.That(SeriesTimelines.DataResize(t[i], 114, 106, false), Is.EqualTo(vid3[i]).Within(Height), "VID-03 a los " + t[i]);
                Assert.That(SeriesTimelines.DataResize(t[i], 106, 90, false), Is.EqualTo(laps[i]).Within(Height), "VID-04 parciales a los " + t[i]);
                Assert.That(SeriesTimelines.DataResize(t[i], 84, 60, false), Is.EqualTo(reps[i]).Within(Height), "VID-04 repeticiones a los " + t[i]);
            }

            // Las fracciones de §6.3: crece 0 · 0,045 · 0,290 · 0,792 · 0,972 · 1; se achica 0 · 0 · 0 · 0,014 · 0,147 · 0,621 ·
            // 0,929 · 0,998 · 1.
            long[] f = { 0, 33, 67, 100, 133, 167, 200, 233, 267 };
            double[] grow = { 0, 0.045, 0.290, 0.792, 0.972, 1, 1, 1, 1 };
            double[] shrink = { 0, 0, 0, 0.014, 0.147, 0.621, 0.929, 0.998, 1 };
            for (var i = 0; i < f.Length; i++)
            {
                Assert.That(SeriesTimelines.DataResize(f[i], 0, 1, false), Is.EqualTo(grow[i]).Within(Alpha), "crece a los " + f[i]);
                Assert.That(1 - SeriesTimelines.DataResize(f[i], 1, 0, false), Is.EqualTo(shrink[i]).Within(Alpha), "se achica a los " + f[i]);
            }

            Assert.That(SeriesTimelines.DataResizeEndMs(true, false), Is.EqualTo(160));
            Assert.That(SeriesTimelines.DataResizeEndMs(false, false), Is.EqualTo(240));
            Assert.That(SeriesTimelines.DataResize(50, 90, 90, false), Is.EqualTo(90));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheTailFollowsTheGlass()
        {
            // La cola y la ventana de filas: G − H_final por un dato; con el resorte corriendo, nunca más rápido que el vidrio.
            Assert.That(SeriesTimelines.TailShift(109, 106), Is.EqualTo(3));
            Assert.That(SeriesTimelines.FollowGlass(-10, 100, 114), Is.EqualTo(-14), "el vidrio va más lento: lo sigue");
            Assert.That(SeriesTimelines.FollowGlass(-10, 110, 114), Is.EqualTo(-10), "el empuje manda");
        }

        [Test, Category("AC-WKT-18-19")]
        public void ANewLapAsInVidTwo()
        {
            // §6.2 (VID-02): el empuje −22,0 · −21,0 · −15,7 · −4,6 · −0,6 · 0; la fila nueva 0 hasta los 80, 0,331 a los 100 y
            // 0,983 a los 200 (se lee entera a los 200: AC-WKT-18-19), desde −8: −5,35 · −2,39 · −0,79 · −0,13 · 0; la que se va
            // 1 · 0,978 · 0,823 · 0,422 · 0 (la propia; la ventana la apaga antes).
            long[] t = { 0, 33, 67, 100, 133, 167, 200, 233 };
            double[] push = { -22.0, -21.0, -15.7, -4.6, -0.6, 0, 0, 0 };
            double[] alpha = { 0, 0, 0, 0.331, 0.701, 0.901, 0.983, 1 };
            double[] shift = { -8, -8, -8, -5.35, -2.39, -0.79, -0.13, 0 };
            double[] gone = { 1, 0.978, 0.823, 0.422, 0, 0, 0, 0 };
            for (var i = 0; i < t.Length; i++)
            {
                var v = SeriesTimelines.RowsChange(t[i], 1, true, false);
                Assert.That(v.RowsPushBase, Is.EqualTo(push[i]).Within(Height), "empuje a los " + t[i]);
                Assert.That(v.NewRowsAlpha, Is.EqualTo(alpha[i]).Within(Alpha), "fila nueva a los " + t[i]);
                Assert.That(v.NewRowsShiftBase, Is.EqualTo(shift[i]).Within(Height), "corrimiento a los " + t[i]);
                Assert.That(v.DroppedAlpha, Is.EqualTo(gone[i]).Within(Alpha), "la que se va a los " + t[i]);
                Assert.That(v.NewRows, Is.EqualTo(1));
            }

            Assert.That(SeriesTimelines.RowsChange(200, 1, false, false).NewRowsAlpha, Is.GreaterThanOrEqualTo(0.98));
            Assert.That(SeriesTimelines.RowsChange(80, 1, false, false).PushProgress, Is.EqualTo(SeriesTimelines.DataResize(80, 0, 1, false)).Within(1e-12),
                "la escala común llega con el empuje (DD5-24) y el alto crece con la misma curva");
            // El alto +Δ (Δ = 24): 0 · 1,1 · 6,9 · 19,0 · 23,3 · 24.
            double[] grow = { 0, 1.1, 6.9, 19.0, 23.3, 24 };
            for (var i = 0; i < grow.Length; i++)
            {
                Assert.That(SeriesTimelines.DataResize(t[i], 90, 114, false) - 90, Is.EqualTo(grow[i]).Within(Height), "alto a los " + t[i]);
            }

            Assert.That(SeriesTimelines.RowsChangeEndMs(false), Is.EqualTo(240));
        }

        [Test, Category("AC-WKT-18-19")]
        public void SeveralLapsInOneSecondPushTwentyTwoEach()
        {
            Assert.That(SeriesTimelines.RowsChange(0, 2, false, false).RowsPushBase, Is.EqualTo(-44));
            Assert.That(SeriesTimelines.RowsChange(0, 3, false, false).RowsPushBase, Is.EqualTo(-66));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheNewRowNeverTouchesTheOneBelow()
        {
            // §6.2: el aire mínimo entre la tinta de la fila nueva y la de la que baja es de 5,48 base (a los 82 ms, con opacidad
            // 0,037), en todos los cuadros en que se ve: la fila nueva arriba (64 + corrimiento, tinta de abajo +17) y la que baja
            // (86 + empuje, tinta de arriba +3).
            var min = double.MaxValue;
            for (long ms = 0; ms <= 400; ms++)
            {
                var v = SeriesTimelines.RowsChange(ms, 1, false, false);
                if (v.NewRowsAlpha <= 0.001)
                {
                    continue;
                }

                var newInk = SeriesPanelGeometry.LapRowTop(0) + v.NewRowsShiftBase + SeriesPanelGeometry.InkBottom(SeriesRowKind.LapRow);
                var belowInk = SeriesPanelGeometry.LapRowTop(1) + v.RowsPushBase + SeriesPanelGeometry.InkTop(SeriesRowKind.LapRow);
                min = Math.Min(min, belowInk - newInk);
            }

            // El Designer da 5,48 con la curva exacta de USS (ease-in-out-cubic); con el cubic-bezier de Tokens (la aproximación para
            // C#) queda en ≈ 5,2: igual por encima de 5.
            Assert.That(min, Is.GreaterThanOrEqualTo(5), "≥ 5 base en todos los cuadros de VID-02");
            Assert.That(min, Is.LessThan(6), "el mínimo es el de los cuadros en que la fila nueva empieza a verse");
        }

        [Test, Category("AC-WKT-18-19")]
        public void WhatIsNewNeverTouchesTheTail()
        {
            // VID-04 (parciales de 106 a 90): lo nuevo (la vuelta en curso, que entra en 120-280 subiendo +4) y la cola (la línea
            // Rep, que sube con el vidrio: su lugar final, 66, corrido G − 90) no se tocan; lo viejo (el vacío, que sale en 0-120) y
            // la cola, tampoco. VID-03 (114 a 106): el vacío que entra y la línea Rep (82, corrida G − 106), ídem. ≥ 5 base.
            var min = double.MaxValue;
            for (long ms = 0; ms <= 400; ms++)
            {
                var swap = SeriesTimelines.Swap(ms, true, false);
                var g4 = SeriesTimelines.DataResize(ms, 106, 90, false);
                var tail4 = SeriesPanelGeometry.RepLineTop(true, 0) + SeriesTimelines.TailShift(g4, 90) + SeriesPanelGeometry.InkTop(SeriesRowKind.RepLine);
                if (swap.NewAlpha > 0.001)
                {
                    var hero = SeriesPanelGeometry.ContentTop + swap.NewRiseBase + SeriesPanelGeometry.InkBottom(SeriesRowKind.Hero);
                    min = Math.Min(min, tail4 - hero);
                }

                if (swap.OldAlpha > 0.001)
                {
                    var empty = SeriesPanelGeometry.EmptyLineTop(1) + SeriesPanelGeometry.InkBottom(SeriesRowKind.EmptyLine);
                    min = Math.Min(min, tail4 - empty);
                }

                var g3 = SeriesTimelines.DataResize(ms, 114, 106, false);
                var tail3 = SeriesPanelGeometry.RepLineTop(false, 0) + SeriesTimelines.TailShift(g3, 106) + SeriesPanelGeometry.InkTop(SeriesRowKind.RepLine);
                if (swap.NewAlpha > 0.001)
                {
                    var empty = SeriesPanelGeometry.EmptyLineTop(1) + swap.NewRiseBase + SeriesPanelGeometry.InkBottom(SeriesRowKind.EmptyLine);
                    min = Math.Min(min, tail3 - empty);
                }
            }

            Assert.That(min, Is.GreaterThanOrEqualTo(5));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheMarker()
        {
            // §6.2: entra con su fila, en 1 hasta los 600 y se apaga en 600-1400 (ease.move): 0,929 · 0,500 · 0,071 a los 800 · 1000
            // · 1200; igual con reducir movimiento (es opacidad).
            Assert.That(SeriesTimelines.Marker(100, false), Is.EqualTo(0.331).Within(Alpha));
            Assert.That(SeriesTimelines.Marker(400, false), Is.EqualTo(1));
            Assert.That(SeriesTimelines.Marker(600, false), Is.EqualTo(1));
            Assert.That(SeriesTimelines.Marker(800, false), Is.EqualTo(0.929).Within(Alpha));
            Assert.That(SeriesTimelines.Marker(1000, false), Is.EqualTo(0.500).Within(Alpha));
            Assert.That(SeriesTimelines.Marker(1200, false), Is.EqualTo(0.071).Within(Alpha));
            Assert.That(SeriesTimelines.Marker(1400, false), Is.EqualTo(0));
            Assert.That(SeriesTimelines.Marker(1000, true), Is.EqualTo(0.500).Within(Alpha));
            Assert.That(SeriesTimelines.MarkerEndMs, Is.EqualTo(1400));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheSwapByPiece()
        {
            // §6.3 (DD5-18): lo viejo 1 · 0,978 · 0,823 · 0,422 · 0; lo nuevo y su subida 0 · 4 hasta los 100, 0,226 · 3,10;
            // 0,646 · 1,41; 0,872 · 0,51; 0,973 · 0,11; 0,999 · 0. Solo lo que cambia de clase.
            long[] t = { 0, 33, 67, 100, 133, 167, 200, 233, 267 };
            double[] old = { 1, 0.978, 0.823, 0.422, 0, 0, 0, 0, 0 };
            double[] fresh = { 0, 0, 0, 0, 0.226, 0.646, 0.872, 0.973, 0.999 };
            double[] rise = { 4, 4, 4, 4, 3.10, 1.41, 0.51, 0.11, 0 };
            for (var i = 0; i < t.Length; i++)
            {
                var v = SeriesTimelines.Swap(t[i], true, false);
                Assert.That(v.OldAlpha, Is.EqualTo(old[i]).Within(Alpha), "lo viejo a los " + t[i]);
                Assert.That(v.NewAlpha, Is.EqualTo(fresh[i]).Within(Alpha), "lo nuevo a los " + t[i]);
                Assert.That(v.NewRiseBase, Is.EqualTo(rise[i]).Within(Height), "la subida a los " + t[i]);
            }

            var roll = SeriesTimelines.Swap(33, false, false);
            Assert.That(roll.OldAlpha, Is.EqualTo(0), "con Roll o None, nada funde");
            Assert.That(roll.NewAlpha, Is.EqualTo(1));
            Assert.That(roll.NewRiseBase, Is.EqualTo(0));
            Assert.That(roll.ClearedRowsAlpha, Is.EqualTo(old[1]).Within(Alpha), "las filas que se vacían salen en su lugar (0-120)");
            Assert.That(SeriesTimelines.SwapEndMs, Is.EqualTo(280));
            var line = SeriesTimelines.RepLine(167, true, false);
            Assert.That(line.Opacity, Is.EqualTo(0.646).Within(Alpha), "la línea Rep entra con lo nuevo");
            Assert.That(SeriesTimelines.RepLine(67, false, false).Opacity, Is.EqualTo(0.823).Within(Alpha), "y sale en 0-120");
        }

        [Test, Category("AC-WKT-18-01"), Category("AC-WKT-18-19")]
        public void TheSeriesStartsAndEnds()
        {
            // §6.4: aparece 0,134 · 0,424 · 0,634 · 0,785 · 0,890 · 0,952 · 1 a los 133 · 167 · 200 · 233 · 267 · 300 · 400, con
            // +8 → 0; se va en 0-120 sin moverse.
            long[] t = { 133, 167, 200, 233, 267, 300, 400 };
            double[] opacity = { 0.134, 0.424, 0.634, 0.785, 0.890, 0.952, 1 };
            for (var i = 0; i < t.Length; i++)
            {
                Assert.That(SeriesTimelines.RepsPanel(t[i], true, false).Opacity, Is.EqualTo(opacity[i]).Within(Alpha), "a los " + t[i]);
            }

            Assert.That(SeriesTimelines.RepsPanel(120, true, false).RiseBase, Is.EqualTo(8));
            Assert.That(SeriesTimelines.RepsPanel(400, true, false).RiseBase, Is.EqualTo(0));
            Assert.That(SeriesTimelines.RepsPanel(60, false, false).RiseBase, Is.EqualTo(0));
            Assert.That(SeriesTimelines.RepsPanel(120, false, false).Opacity, Is.EqualTo(0));
            Assert.That(SeriesTimelines.RepsPanelEndMs(true), Is.EqualTo(400));
            Assert.That(SeriesTimelines.RepsPanelEndMs(false), Is.EqualTo(120));
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheColumnFollowsSpm()
        {
            // §6.5: la columna baja o sube 16,9·s con los tiempos de la 4a: gana en 0-240 y pierde en 80-280 (ease.move).
            Assert.That(SeriesTimelines.SpmTargetGrowthBase, Is.EqualTo(16.856).Within(0.001));
            Assert.That(SeriesTimelines.ColumnFollow(0, true, false), Is.EqualTo(-16.856).Within(0.001), "viene de arriba");
            Assert.That(SeriesTimelines.ColumnFollow(240, true, false), Is.EqualTo(0));
            Assert.That(SeriesTimelines.ColumnFollow(80, false, false), Is.EqualTo(16.856).Within(0.001), "perder espera 80");
            Assert.That(SeriesTimelines.ColumnFollow(180, false, false), Is.EqualTo(8.428).Within(0.1), "la mitad a mitad de camino");
            Assert.That(SeriesTimelines.ColumnFollow(280, false, false), Is.EqualTo(0));
            Assert.That(SeriesTimelines.ColumnFollow(10, true, true), Is.EqualTo(0), "con reducir movimiento, en el acto");
            Assert.That(SeriesTimelines.ColumnFollowEndMs(false, false), Is.EqualTo(280));
        }

        [Test, Category("AC-WKT-18-13")]
        public void TheWheel()
        {
            // §6.6: de a una fila (22) con spring.snappy (ω 22, ≈ 0,26 s); con reducir movimiento, en el acto.
            Assert.That(SeriesTimelines.WheelStepBase, Is.EqualTo(22));
            var v = SeriesTimelines.Wheel(260, 0, 22, 0, false);
            Assert.That(v.Value / 22, Is.GreaterThan(0.97));
            Assert.That(SeriesTimelines.Wheel(0, 0, 22, 0, false).Value, Is.EqualTo(0));
            Assert.That(SeriesTimelines.Wheel(0, 0, 22, 0, true).Value, Is.EqualTo(22));
        }

        [Test, Category("AC-WKT-18-09")]
        public void TheHoverLayer()
        {
            // §6.10: hacia 0,04 · 0,08 en 90 (ease.enter): 0,0187 · 0,0373 a los 17; 0,0297 · 0,0595 a los 33; 0,0364 · 0,0728 a los
            // 50; 0,0393 · 0,0786 a los 67; exactos a los 90. Bajar desde 0,08: 0,0792 · 0,0739 · 0,0603 · 0,0341 · 0 a los 33 · 67 ·
            // 100 · 133 · 160 (desde 0,04, la mitad). Siempre desde el alfa que tiene.
            Assert.That(SeriesTimelines.Hover(17, 0, 0.04), Is.EqualTo(0.0187).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(17, 0, 0.08), Is.EqualTo(0.0373).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(33, 0, 0.04), Is.EqualTo(0.0297).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(50, 0, 0.08), Is.EqualTo(0.0728).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(67, 0, 0.08), Is.EqualTo(0.0786).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(90, 0, 0.08), Is.EqualTo(0.08));
            Assert.That(SeriesTimelines.Hover(33, 0.08, 0), Is.EqualTo(0.0792).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(67, 0.08, 0), Is.EqualTo(0.0739).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(100, 0.08, 0), Is.EqualTo(0.0603).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(133, 0.08, 0), Is.EqualTo(0.0341).Within(0.0005));
            Assert.That(SeriesTimelines.Hover(160, 0.08, 0), Is.EqualTo(0));
            Assert.That(SeriesTimelines.Hover(100, 0.04, 0), Is.EqualTo(0.0603 / 2).Within(0.0005), "desde 0,04, la mitad");
            Assert.That(SeriesTimelines.Hover(45, 0.04, 0.08), Is.InRange(0.04, 0.08), "un PointerDown en medio sigue desde ahí");
            Assert.That(SeriesTimelines.HoverEndMs(0, 0.04), Is.EqualTo(90));
            Assert.That(SeriesTimelines.HoverEndMs(0.08, 0), Is.EqualTo(160));
            Assert.That(MotionSpecs.SeriesHoverAlpha, Is.EqualTo(0.04));
            Assert.That(MotionSpecs.SeriesPressedAlpha, Is.EqualTo(0.08));
        }

        [Test, Category("AC-APP-05-08"), Category("AC-WKT-18-18")]
        public void ReducedVariants()
        {
            // §6.8, fila por fila. Abrir: el alto en el cuadro 0; el contenido 0 → 1 en 0-160, lineal (0,208 · 0,417 · 0,625 · 0,833 ·
            // 1 a los 33 · 67 · 100 · 133 · 167); el rótulo y el título con sus fundidos; la flecha no gira: ∨ funde 1 → 0 en 0-80 y ∧
            // 0 → 1 en 80-160, lineales.
            var open0 = SeriesTimelines.Open(0, 34, 90, 0, true);
            Assert.That(open0.HeightBase, Is.EqualTo(90), "el alto en el acto");
            long[] t = { 33, 67, 100, 133, 167 };
            double[] content = { 0.208, 0.417, 0.625, 0.833, 1 };
            for (var i = 0; i < t.Length; i++)
            {
                Assert.That(SeriesTimelines.Open(t[i], 34, 90, 0, true).ContentAlpha, Is.EqualTo(content[i]).Within(Alpha), "contenido a los " + t[i]);
            }

            var chevronMid = SeriesTimelines.Open(40, 34, 90, 0, true);
            Assert.That(chevronMid.ChevronDegrees, Is.EqualTo(0), "no gira");
            Assert.That(chevronMid.ChevronDownAlpha, Is.EqualTo(0.5).Within(Alpha));
            Assert.That(chevronMid.ChevronUpAlpha, Is.EqualTo(0));
            Assert.That(SeriesTimelines.Open(120, 34, 90, 0, true).ChevronUpAlpha, Is.EqualTo(0.5).Within(Alpha));
            Assert.That(SeriesTimelines.Open(160, 34, 90, 0, true).ChevronUpAlpha, Is.EqualTo(1));
            Assert.That(SeriesTimelines.Open(100, 34, 90, 0, true).TitleAlpha, Is.EqualTo(SeriesTimelines.Open(100, 34, 90, 0, false).TitleAlpha),
                "el título con su fundido");

            // Cerrar: el contenido 1 → 0 en 0-120, lineal (0,723 · 0,444 · 0,167 · 0); el alto a los 120, en el acto; la flecha al
            // revés.
            double[] closeContent = { 0.723, 0.444, 0.167, 0 };
            for (var i = 0; i < closeContent.Length; i++)
            {
                Assert.That(SeriesTimelines.Close(t[i], 90, 34, 0, true).ContentAlpha, Is.EqualTo(closeContent[i]).Within(Alpha));
            }

            Assert.That(SeriesTimelines.Close(119, 90, 34, 0, true).HeightBase, Is.EqualTo(90));
            Assert.That(SeriesTimelines.Close(120, 90, 34, 0, true).HeightBase, Is.EqualTo(34));
            var closing = SeriesTimelines.Close(40, 90, 34, 0, true);
            Assert.That(closing.ChevronUpAlpha, Is.EqualTo(0.5).Within(Alpha), "el ∧ (el ∨ girado del panel abierto) se va");
            Assert.That(SeriesTimelines.Close(160, 90, 34, 0, true).ChevronDownAlpha, Is.EqualTo(1));

            // La vuelta nueva: todo en su lugar en el cuadro 0, la nueva funde 0 → 1 en 0-160 sin deslizarse, la que se va en
            // 0-120; la marca, igual.
            var rows = SeriesTimelines.RowsChange(80, 1, true, true);
            Assert.That(rows.RowsPushBase, Is.EqualTo(0));
            Assert.That(rows.NewRowsShiftBase, Is.EqualTo(0));
            Assert.That(rows.NewRowsAlpha, Is.EqualTo(0.5).Within(Alpha));
            Assert.That(rows.DroppedAlpha, Is.EqualTo(1 - 80.0 / 120).Within(Alpha));
            Assert.That(rows.PushProgress, Is.EqualTo(1));
            // El cambio de contenido: los mismos fundidos, sin subir; el alto a los 0 si crece y a los 120 si se achica.
            var swap = SeriesTimelines.Swap(167, true, true);
            Assert.That(swap.NewAlpha, Is.EqualTo(SeriesTimelines.Swap(167, true, false).NewAlpha));
            Assert.That(swap.NewRiseBase, Is.EqualTo(0));
            Assert.That(SeriesTimelines.DataResize(0, 90, 114, true), Is.EqualTo(114));
            Assert.That(SeriesTimelines.DataResize(119, 114, 106, true), Is.EqualTo(114));
            Assert.That(SeriesTimelines.DataResize(120, 114, 106, true), Is.EqualTo(106));
            // La serie que empieza o termina: solo opacidad.
            Assert.That(SeriesTimelines.RepsPanel(200, true, true).RiseBase, Is.EqualTo(0));
            Assert.That(SeriesTimelines.RepsPanel(200, true, true).Opacity, Is.EqualTo(0.634).Within(Alpha));
            // La columna y la rueda, en el acto; el hover, igual.
            Assert.That(SeriesTimelines.ColumnFollow(0, false, true), Is.EqualTo(0));
            Assert.That(SeriesTimelines.Wheel(10, 0, 44, 0, true).Value, Is.EqualTo(44));
            Assert.That(SeriesTimelines.ToggleFadesEndMs(true), Is.EqualTo(200));
            Assert.That(SeriesTimelines.ToggleFadesEndMs(false), Is.EqualTo(240));
        }

        [Test, Category("AC-WKT-13-06")]
        public void ThePanelsEnterWithSpmInTheCascade()
        {
            // §6.7 (DT5-12): los paneles con el tramo de SPM (520, 280, +8 → 0); responden desde los 520.
            foreach (var piece in new[] { HudPiece.SeriesLaps, HudPiece.SeriesReps })
            {
                Assert.That(ImmersiveTimeline.CascadeStartMs(piece), Is.EqualTo(MotionSpecs.ImmersiveCascadeSpmMs));
                foreach (var ms in new long[] { 500, 533, 600, 700, 800 })
                {
                    var series = ImmersiveTimeline.Entry(piece, ms, false);
                    var spm = ImmersiveTimeline.Entry(HudPiece.Spm, ms, false);
                    Assert.That(series.Opacity, Is.EqualTo(spm.Opacity));
                    Assert.That(series.ShiftDp, Is.EqualTo(spm.ShiftDp));
                }

                Assert.That(ImmersiveTimeline.EntryInteractive(piece, 519, false), Is.False);
                Assert.That(ImmersiveTimeline.EntryInteractive(piece, 520, false), Is.True);
                Assert.That(ImmersiveTimeline.Entry(piece, 0, true).Opacity, Is.EqualTo(1), "con reducir movimiento, el fundido con todo el HUD");
            }
        }

        [Test, Category("AC-WKT-18-18")]
        public void TheRestVisual()
        {
            var open = SeriesPanelVisual.Rest(114, true);
            Assert.That(open.HeightBase, Is.EqualTo(114));
            Assert.That(open.ContentAlpha, Is.EqualTo(1));
            Assert.That(open.ChevronDegrees, Is.EqualTo(180));
            Assert.That(open.ChipLabelAlpha, Is.EqualTo(0));
            Assert.That(open.MarkerAlpha0, Is.EqualTo(0), "sin marcas");
            Assert.That(open.RowsPushBase, Is.EqualTo(0), "sin empuje");
            Assert.That(open.HoverAlpha, Is.EqualTo(0));
            var closed = SeriesPanelVisual.Rest(34, false);
            Assert.That(closed.ContentAlpha, Is.EqualTo(0));
            Assert.That(closed.ChipLabelAlpha, Is.EqualTo(1));
            Assert.That(closed.TitleAlpha, Is.EqualTo(0));
        }
    }
}
