using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // HGT: HudGeometry (02-tech-design §6.4 de la It. 4a; 04-design-notes §3; DN4a-01, DN4a-02, DN4a-13, DN4a-14). Los
    // rectángulos de 04-design-notes §3.8 (± 0,1) y las escalas de §3.7 (± 0,005) salen de hud_layout.py del Designer, con
    // los mismos anchos naturales (HudGeometryCases): 5 perfiles × 6 casos de valores × con y sin simulador.
    [TestFixture]
    public sealed class HudGeometryTests
    {
        private const double RectTolerance = 0.1;
        private const double ScaleTolerance = 0.005;

        [Test, Category("AC-WKT-17-01")]
        public void ScaleValues()
        {
            // clamp(lado corto / 390; 0,9; 1,6): 1,054 en TV y TH, 0,923 en L640 y P360, 1,6 en W1280 (spec §0.3, §7.2).
            Assert.That(HudGeometry.Scale(411, 914), Is.EqualTo(1.054).Within(0.0005));
            Assert.That(HudGeometry.Scale(914, 411), Is.EqualTo(1.054).Within(0.0005));
            Assert.That(HudGeometry.Scale(640, 360), Is.EqualTo(0.923).Within(0.0005));
            Assert.That(HudGeometry.Scale(360, 800), Is.EqualTo(0.923).Within(0.0005));
            Assert.That(HudGeometry.Scale(1280, 720), Is.EqualTo(1.6));
            Assert.That(HudGeometry.Scale(320, 568), Is.EqualTo(0.9), "el piso");
            Assert.That(HudGeometry.Scale(2560, 1440), Is.EqualTo(1.6), "el techo");
            // CAP-17: en W1280 la caja del valor de SPM mide 64 × 1,6 = 102,4.
            Assert.That(HudGeometry.LargeValue * HudGeometry.Scale(1280, 720), Is.EqualTo(102.4).Within(1e-9));
        }

        [Test, Category("AC-WKT-17-09")]
        public void BoatZone()
        {
            // El centro de la cámara de persecución (DN4a-02): 0,504 en vertical, 0,652 en horizontal; 44 % × 30 %, centrada en x.
            Assert.That(HudGeometry.BoatCenterY(411, 914), Is.EqualTo(0.5041).Within(0.0001));
            Assert.That(HudGeometry.BoatCenterY(360, 800), Is.EqualTo(0.5042).Within(0.0001));
            Assert.That(HudGeometry.BoatCenterY(914, 411), Is.EqualTo(0.6516).Within(0.0001));
            Assert.That(HudGeometry.BoatCenterY(1280, 720), Is.EqualTo(0.6516).Within(0.0001));
            Assert.That(HudGeometry.Horizon(411, 914), Is.EqualTo(0.40));
            Assert.That(HudGeometry.Horizon(914, 411), Is.EqualTo(0.42));
            var layout = HudGeometry.Layout(HudGeometryCases.Input(1280, 720, SafeInsets.None, false, HudGeometryCases.Free));
            Assert.That(layout.BoatZone.Width, Is.EqualTo(0.44 * 1280).Within(1e-9));
            Assert.That(layout.BoatZone.Height, Is.EqualTo(0.30 * 720).Within(1e-9));
            Assert.That(layout.BoatZone.CenterX, Is.EqualTo(640).Within(1e-9));
            Assert.That(layout.HorizonY, Is.EqualTo(0.42 * 720).Within(1e-9));
            // docs/image-03.png (1600 × 1080): el centro da y ≈ 704 y el carro del remero está en y ≈ 700.
            Assert.That(HudGeometry.BoatCenterY(1600, 1080) * 1080, Is.EqualTo(703.7).Within(0.1));
        }

        [TestCaseSource(nameof(Rectangles)), Category("AC-WKT-17-08")]
        public void RectanglesOfTheDesignNotes(string profile, HudCase values, string piece, double x0, double y0, double x1, double y1)
        {
            var size = HudGeometryCases.Profile(profile);
            var layout = HudGeometry.Layout(HudGeometryCases.Input(size[0], size[1], SafeInsets.None, true, values));
            var rect = Piece(layout, piece);
            var what = profile + " · " + values + " · " + piece + ": " + rect;
            Assert.That(rect.Left, Is.EqualTo(x0).Within(RectTolerance), what);
            Assert.That(rect.Top, Is.EqualTo(y0).Within(RectTolerance), what);
            Assert.That(rect.Right, Is.EqualTo(x1).Within(RectTolerance), what);
            Assert.That(rect.Bottom, Is.EqualTo(y1).Within(RectTolerance), what);
        }

        // La escala de cada zona (04-design-notes §3.7). AC-WKT-17-10 (los extremos en pantalla) es de HLT, en PlayMode.
        [TestCaseSource(nameof(Scales)), Category("AC-WKT-17-01")]
        public void ScalesOfTheDesignNotes(string profile, bool simulator, HudCase values, double spm, double split, double watts, double vitals, double band)
        {
            var size = HudGeometryCases.Profile(profile);
            var layout = HudGeometry.Layout(HudGeometryCases.Input(size[0], size[1], SafeInsets.None, simulator, values));
            var what = profile + (simulator ? " sim · " : " normal · ") + values;
            Assert.That(layout.ValueScale(HudValue.Spm), Is.EqualTo(spm).Within(ScaleTolerance), what + ": SPM");
            Assert.That(layout.ValueScale(HudValue.Split), Is.EqualTo(split).Within(ScaleTolerance), what + ": Split");
            Assert.That(layout.ValueScale(HudValue.Watts), Is.EqualTo(watts).Within(ScaleTolerance), what + ": Watts");
            Assert.That(layout.ValueScale(HudValue.Kcal), Is.EqualTo(vitals).Within(ScaleTolerance), what + ": kcal");
            Assert.That(layout.ValueScale(HudValue.Bpm), Is.EqualTo(vitals).Within(ScaleTolerance), what + ": BPM");
            Assert.That(layout.ValueScale(HudValue.Band), Is.EqualTo(band).Within(ScaleTolerance), what + ": banda");
        }

        [Test, Category("AC-WKT-17-08"), Category("AC-WKT-17-09")]
        public void ZonesDoNotOverlap()
        {
            // Las 60 combinaciones (5 perfiles × 6 casos × con y sin simulador): ninguna pieza se cruza con otra (la llave,
            // agrandada 8), ninguna entra en la zona del bote (salvo la barra y la llave) y todo queda dentro de la ventana.
            var problems = new List<string>();
            var combinations = 0;
            foreach (var profile in HudGeometryCases.Profiles)
            {
                foreach (var simulator in new[] { true, false })
                {
                    foreach (var values in HudGeometryCases.All)
                    {
                        combinations++;
                        var w = profile.Value[0];
                        var h = profile.Value[1];
                        var layout = HudGeometry.Layout(HudGeometryCases.Input(w, h, SafeInsets.None, simulator, values));
                        var pieces = Pieces(layout);
                        var where = profile.Key + (simulator ? " sim " : " normal ") + values + ": ";
                        for (var i = 0; i < pieces.Count; i++)
                        {
                            var a = pieces[i];
                            for (var j = i + 1; j < pieces.Count; j++)
                            {
                                var b = pieces[j];
                                var gap = a.Key == "Wrench" || b.Key == "Wrench" ? HudGeometry.WrenchGap : 0;
                                if (a.Value.Intersects(b.Value, gap))
                                {
                                    problems.Add(where + a.Key + " × " + b.Key);
                                }
                            }

                            if (a.Key != "TopBar" && a.Key != "Wrench" && a.Value.Intersects(layout.BoatZone))
                            {
                                problems.Add(where + a.Key + " × zona del bote");
                            }

                            if (!a.Value.IsInside(w, h))
                            {
                                problems.Add(where + a.Key + " fuera de la ventana");
                            }
                        }
                    }
                }
            }

            Assert.That(combinations, Is.EqualTo(60));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-BLE-16-02")]
        public void WrenchSlot()
        {
            // DN4a-14: en vertical, 8 dp sobre la columna de kcal y BPM con BPM, aunque BPM no esté (no se mueve con el pulso);
            // en horizontal, (a) a la derecha de Pausa (TH, W1280) o (b) sobre Pausa (L640), por ventana. Sin simulador, nada.
            foreach (var profile in HudGeometryCases.Profiles)
            {
                var w = profile.Value[0];
                var h = profile.Value[1];
                var withBpm = HudGeometry.Layout(HudGeometryCases.Input(w, h, SafeInsets.None, true, HudGeometryCases.FreeWithBpm));
                var free = HudGeometry.Layout(HudGeometryCases.Input(w, h, SafeInsets.None, true, HudGeometryCases.Free));
                var extremes = HudGeometry.Layout(HudGeometryCases.Input(w, h, SafeInsets.None, true, HudGeometryCases.Extremes));
                Assert.That(free.Wrench, Is.EqualTo(withBpm.Wrench), profile.Key + ": no se mueve con BPM");
                Assert.That(extremes.Wrench, Is.EqualTo(withBpm.Wrench), profile.Key + ": no se mueve con los valores");
                Assert.That(withBpm.Wrench!.Value.Width, Is.EqualTo(HudGeometry.WrenchSize));
                Assert.That(withBpm.Wrench.Value.Height, Is.EqualTo(HudGeometry.WrenchSize));
                if (h > w)
                {
                    Assert.That(withBpm.Wrench.Value.Right, Is.EqualTo(w - 16).Within(1e-9), profile.Key);
                    Assert.That(withBpm.Wrench.Value.Bottom, Is.EqualTo(withBpm.Kcal.Top - 8).Within(1e-9), profile.Key);
                }
                else if (w == 640)
                {
                    Assert.That(withBpm.Wrench.Value.Right, Is.EqualTo(withBpm.Controls.Right).Within(1e-9), "L640 (b)");
                    Assert.That(withBpm.Wrench.Value.Bottom, Is.EqualTo(withBpm.Controls.Top - 8).Within(1e-9), "L640 (b)");
                }
                else
                {
                    Assert.That(withBpm.Wrench.Value.Left, Is.EqualTo(withBpm.Controls.Right + 8).Within(1e-9), profile.Key + " (a)");
                    Assert.That(withBpm.Wrench.Value.CenterY, Is.EqualTo(withBpm.Controls.CenterY).Within(1e-9), profile.Key + " (a)");
                }

                var normal = HudGeometry.Layout(HudGeometryCases.Input(w, h, SafeInsets.None, false, HudGeometryCases.FreeWithBpm));
                Assert.That(normal.Wrench.HasValue, Is.False, "sin simulador no hay llave");
            }

            // Con s = 0,9 (un teléfono de 320 × 568) también entra.
            var small = HudGeometry.Layout(HudGeometryCases.Input(320, 568, SafeInsets.None, true, HudGeometryCases.FreeWithBpm));
            Assert.That(small.Scale, Is.EqualTo(0.9));
            Assert.That(small.Wrench!.Value.IsInside(320, 568), Is.True);
            Assert.That(small.Wrench.Value.Intersects(small.Kcal, 8), Is.False);
            Assert.That(small.Wrench.Value.Intersects(small.Watts, 8), Is.False);
        }

        // La geometría de la fila de abajo; la pantalla de AC-WKT-17-05 es de IHT, en PlayMode.
        [Test, Category("AC-WKT-17-08")]
        public void TheBottomRowInLandscape()
        {
            // DV-33: en horizontal kcal y BPM van en fila a la altura de los controles, kcal en el rincón; la banda es compacta.
            var th = HudGeometry.Layout(HudGeometryCases.Input(914, 411, SafeInsets.None, true, HudGeometryCases.FreeWithBpm));
            Assert.That(th.IsLandscape, Is.True);
            Assert.That(th.VitalsInRow, Is.True);
            Assert.That(th.BandCompact, Is.True);
            Assert.That(th.VitalsMinWidth, Is.True, "TH: entran los dos mínimos");
            Assert.That(th.Kcal.Right, Is.EqualTo(914 - 14 * th.Scale).Within(1e-9), "kcal en el rincón");
            Assert.That(th.Bpm!.Value.Right, Is.EqualTo(th.Kcal.Left - 8 * th.Scale).Within(1e-9), "BPM a su izquierda, 8·s");
            Assert.That(th.Kcal.Bottom, Is.EqualTo(th.Controls.Bottom).Within(1e-9), "a la altura de los controles");
            Assert.That(th.Band.Bottom, Is.EqualTo(th.Controls.Bottom).Within(1e-9));
            var l640 = HudGeometry.Layout(HudGeometryCases.Input(640, 360, SafeInsets.None, true, HudGeometryCases.FreeWithBpm));
            Assert.That(l640.VitalsMinWidth, Is.False, "L640: ancho de contenido");
            var l640Free = HudGeometry.Layout(HudGeometryCases.Input(640, 360, SafeInsets.None, true, HudGeometryCases.Free));
            Assert.That(l640Free.VitalsMinWidth, Is.False, "por ventana: el modo no cambia cuando BPM se va");
            Assert.That(l640Free.Kcal, Is.EqualTo(l640.Kcal), "kcal no se mueve en el rincón");
            var tv = HudGeometry.Layout(HudGeometryCases.Input(411, 914, SafeInsets.None, true, HudGeometryCases.FreeWithBpm));
            Assert.That(tv.VitalsInRow, Is.False);
            Assert.That(tv.BandCompact, Is.False);
            Assert.That(tv.Bpm!.Value.Top, Is.EqualTo(tv.Kcal.Bottom + 8 * tv.Scale).Within(1e-9), "en columna: kcal arriba, BPM abajo");
            Assert.That(tv.Bpm.Value.Bottom, Is.EqualTo(914 - 140 * tv.Scale).Within(1e-9), "a 140·s del borde de abajo");
            var tvFree = HudGeometry.Layout(HudGeometryCases.Input(411, 914, SafeInsets.None, true, HudGeometryCases.Free));
            Assert.That(tvFree.Bpm.HasValue, Is.False);
            Assert.That(tvFree.Kcal.Bottom, Is.EqualTo(914 - 140 * tv.Scale).Within(1e-9), "sin BPM, kcal baja al lugar de BPM");
        }

        [Test, Category("AC-WKT-17-01")]
        public void AnchorsAreTheLegacyZonesTimesTheScale()
        {
            // Las zonas del legacy × s (immersive_workout_screen.dart:612, :640, :672, :710; spec §0.3), con los márgenes
            // seguros arriba (barra y tarjetas) y abajo (controles y, en horizontal, la banda y la fila de kcal y BPM).
            var insets = new SafeInsets(0, 24, 0, 48);
            var tv = HudGeometry.Layout(HudGeometryCases.Input(411, 914, insets, false, HudGeometryCases.FreeWithBpm));
            var s = tv.Scale;
            Assert.That(tv.TopBarZone.AnchorX, Is.EqualTo(12 * s).Within(1e-9));
            Assert.That(tv.TopBarZone.AnchorY, Is.EqualTo(24 + 8 * s).Within(1e-9));
            Assert.That(tv.TopBar.Height, Is.EqualTo(40 * s).Within(1e-9), "las dos barras de 40 (DN4a-04)");
            Assert.That(tv.SpmZone.AnchorX, Is.EqualTo(14 * s).Within(1e-9));
            Assert.That(tv.SpmZone.AnchorY, Is.EqualTo(24 + 90 * s).Within(1e-9));
            Assert.That(tv.SpmZone.Corner, Is.EqualTo(HudCorner.TopLeft));
            Assert.That(tv.RightZone.AnchorX, Is.EqualTo(411 - 14 * s).Within(1e-9));
            Assert.That(tv.RightZone.Corner, Is.EqualTo(HudCorner.TopRight));
            Assert.That(tv.VitalsZone.AnchorY, Is.EqualTo(914 - 140 * s).Within(1e-9), "vertical: sin el margen seguro (legacy)");
            Assert.That(tv.BandZone.AnchorY, Is.EqualTo(914 - 140 * s).Within(1e-9));
            Assert.That(tv.ControlsZone.AnchorY, Is.EqualTo(914 - 48 - 16 * s).Within(1e-9));
            Assert.That(tv.ControlsZone.Corner, Is.EqualTo(HudCorner.BottomCenter));
            Assert.That(tv.Controls.Width, Is.EqualTo((128 + 16 + 148) * s).Within(1e-9));
            var th = HudGeometry.Layout(HudGeometryCases.Input(914, 411, insets, false, HudGeometryCases.FreeWithBpm));
            Assert.That(th.BandZone.AnchorY, Is.EqualTo(411 - 48 - 16 * th.Scale).Within(1e-9), "horizontal: margen + 16·s");
            Assert.That(th.VitalsZone.AnchorY, Is.EqualTo(411 - 48 - 16 * th.Scale).Within(1e-9));
            Assert.That(th.PanelCeiling.HasValue, Is.False);
            Assert.That(tv.PanelCeiling, Is.EqualTo(tv.WattsWithTargetBottom + 8).Within(1e-9));
            Assert.That(tv.WattsWithTargetBottom, Is.EqualTo(24 + (90 + 80.856 + 8 + 97.712) * s).Within(1e-6));
        }

        [Test]
        public void CardHeightsOfTheDesignNotes()
        {
            // 04-design-notes §3.2: grande 102,1 (119,0 con obj.), mediana 80,9 (97,7), chica 63,6.
            Assert.That(HudGeometry.CardHeight(64, 13, 10, false), Is.EqualTo(102.094).Within(1e-9));
            Assert.That(HudGeometry.CardHeight(64, 13, 10, true), Is.EqualTo(118.95).Within(1e-9));
            Assert.That(HudGeometry.CardHeight(44, 12, 10, false), Is.EqualTo(80.856).Within(1e-9));
            Assert.That(HudGeometry.CardHeight(32, 11, 8, false), Is.EqualTo(63.618).Within(1e-9));
        }

        public static IEnumerable<object[]> Rectangles()
        {
            return new[]
            {
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "TopBar", 12.6, 8.4, 398.4, 50.6 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Controls", 51.6, 838.1, 359.4, 897.1 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Wrench", 355.0, 575.9, 395.0, 615.9 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Spm", 14.8, 94.8, 178.3, 202.4 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Split", 255.3, 94.8, 396.2, 180.1 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Watts", 271.9, 188.5, 396.2, 273.7 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Bpm", 295.1, 699.4, 396.2, 766.5 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Kcal", 295.1, 623.9, 396.2, 691.0 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "Band", 14.8, 690.6, 267.8, 766.5 },
            new object[] { "TV", HudGeometryCases.FreeWithBpm, "BoatZone", 115.1, 323.7, 295.9, 597.9 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "TopBar", 12.6, 8.4, 398.4, 50.6 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Controls", 51.6, 838.1, 359.4, 897.1 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Wrench", 355.0, 575.9, 395.0, 615.9 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Spm", 14.8, 94.8, 210.7, 220.2 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Split", 255.3, 94.8, 396.2, 180.1 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Watts", 254.1, 188.5, 396.2, 291.5 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Bpm", 295.1, 699.4, 396.2, 766.5 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Kcal", 295.1, 623.9, 396.2, 691.0 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "Band", 14.8, 690.6, 267.8, 766.5 },
            new object[] { "TV", HudGeometryCases.RoutineWithBpm, "BoatZone", 115.1, 323.7, 295.9, 597.9 },
            new object[] { "TV", HudGeometryCases.Long, "TopBar", 12.6, 8.4, 398.4, 50.6 },
            new object[] { "TV", HudGeometryCases.Long, "Controls", 51.6, 838.1, 359.4, 897.1 },
            new object[] { "TV", HudGeometryCases.Long, "Wrench", 355.0, 575.9, 395.0, 615.9 },
            new object[] { "TV", HudGeometryCases.Long, "Spm", 14.8, 94.8, 178.3, 202.4 },
            new object[] { "TV", HudGeometryCases.Long, "Split", 271.9, 94.8, 396.2, 180.1 },
            new object[] { "TV", HudGeometryCases.Long, "Watts", 259.2, 188.5, 396.2, 273.7 },
            new object[] { "TV", HudGeometryCases.Long, "Bpm", 295.1, 699.4, 396.2, 766.5 },
            new object[] { "TV", HudGeometryCases.Long, "Kcal", 295.1, 623.9, 396.2, 691.0 },
            new object[] { "TV", HudGeometryCases.Long, "Band", 14.8, 690.6, 286.6, 766.5 },
            new object[] { "TV", HudGeometryCases.Long, "BoatZone", 115.1, 323.7, 295.9, 597.9 },
            new object[] { "TV", HudGeometryCases.Extremes, "TopBar", 12.6, 8.4, 398.4, 50.6 },
            new object[] { "TV", HudGeometryCases.Extremes, "Controls", 51.6, 838.1, 359.4, 897.1 },
            new object[] { "TV", HudGeometryCases.Extremes, "Wrench", 355.0, 575.9, 395.0, 615.9 },
            new object[] { "TV", HudGeometryCases.Extremes, "Spm", 14.8, 94.8, 206.2, 202.4 },
            new object[] { "TV", HudGeometryCases.Extremes, "Split", 214.7, 94.8, 396.2, 180.1 },
            new object[] { "TV", HudGeometryCases.Extremes, "Watts", 214.7, 188.5, 396.2, 273.7 },
            new object[] { "TV", HudGeometryCases.Extremes, "Bpm", 283.1, 699.4, 396.2, 766.5 },
            new object[] { "TV", HudGeometryCases.Extremes, "Kcal", 283.1, 623.9, 396.2, 691.0 },
            new object[] { "TV", HudGeometryCases.Extremes, "Band", 14.8, 690.6, 274.7, 766.5 },
            new object[] { "TV", HudGeometryCases.Extremes, "BoatZone", 115.1, 323.7, 295.9, 597.9 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "TopBar", 11.1, 7.4, 348.9, 44.3 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Controls", 45.2, 733.5, 314.8, 785.2 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Wrench", 304.0, 497.9, 344.0, 537.9 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Spm", 12.9, 83.1, 156.2, 177.3 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Split", 223.6, 83.1, 347.1, 157.7 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Watts", 238.2, 165.1, 347.1, 239.7 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Bpm", 258.5, 612.0, 347.1, 670.8 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Kcal", 258.5, 545.9, 347.1, 604.7 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "Band", 12.9, 604.3, 234.5, 670.8 },
            new object[] { "P360", HudGeometryCases.FreeWithBpm, "BoatZone", 100.8, 283.4, 259.2, 523.4 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "TopBar", 11.1, 7.4, 348.9, 44.3 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Controls", 45.2, 733.5, 314.8, 785.2 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Wrench", 304.0, 497.9, 344.0, 537.9 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Spm", 12.9, 83.1, 184.5, 192.9 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Split", 223.6, 83.1, 347.1, 157.7 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Watts", 222.6, 165.1, 347.1, 255.3 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Bpm", 258.5, 612.0, 347.1, 670.8 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Kcal", 258.5, 545.9, 347.1, 604.7 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "Band", 12.9, 604.3, 234.5, 670.8 },
            new object[] { "P360", HudGeometryCases.RoutineWithBpm, "BoatZone", 100.8, 283.4, 259.2, 523.4 },
            new object[] { "P360", HudGeometryCases.Long, "TopBar", 11.1, 7.4, 348.9, 44.3 },
            new object[] { "P360", HudGeometryCases.Long, "Controls", 45.2, 733.5, 314.8, 785.2 },
            new object[] { "P360", HudGeometryCases.Long, "Wrench", 304.0, 497.9, 344.0, 537.9 },
            new object[] { "P360", HudGeometryCases.Long, "Spm", 12.9, 83.1, 156.2, 177.3 },
            new object[] { "P360", HudGeometryCases.Long, "Split", 238.2, 83.1, 347.1, 157.7 },
            new object[] { "P360", HudGeometryCases.Long, "Watts", 227.0, 165.1, 347.1, 239.7 },
            new object[] { "P360", HudGeometryCases.Long, "Bpm", 258.5, 612.0, 347.1, 670.8 },
            new object[] { "P360", HudGeometryCases.Long, "Kcal", 258.5, 545.9, 347.1, 604.7 },
            new object[] { "P360", HudGeometryCases.Long, "Band", 12.9, 604.3, 251.1, 670.8 },
            new object[] { "P360", HudGeometryCases.Long, "BoatZone", 100.8, 283.4, 259.2, 523.4 },
            new object[] { "P360", HudGeometryCases.Extremes, "TopBar", 11.1, 7.4, 348.9, 44.3 },
            new object[] { "P360", HudGeometryCases.Extremes, "Controls", 45.2, 733.5, 314.8, 785.2 },
            new object[] { "P360", HudGeometryCases.Extremes, "Wrench", 304.0, 497.9, 344.0, 537.9 },
            new object[] { "P360", HudGeometryCases.Extremes, "Spm", 12.9, 83.1, 180.7, 177.3 },
            new object[] { "P360", HudGeometryCases.Extremes, "Split", 188.0, 83.1, 347.1, 157.7 },
            new object[] { "P360", HudGeometryCases.Extremes, "Watts", 188.0, 165.1, 347.1, 239.7 },
            new object[] { "P360", HudGeometryCases.Extremes, "Bpm", 248.0, 612.0, 347.1, 670.8 },
            new object[] { "P360", HudGeometryCases.Extremes, "Kcal", 248.0, 545.9, 347.1, 604.7 },
            new object[] { "P360", HudGeometryCases.Extremes, "Band", 12.9, 604.3, 240.6, 670.8 },
            new object[] { "P360", HudGeometryCases.Extremes, "BoatZone", 100.8, 283.4, 259.2, 523.4 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "TopBar", 12.6, 8.4, 901.4, 50.6 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Controls", 303.1, 335.1, 610.9, 394.1 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Wrench", 618.9, 344.6, 658.9, 384.6 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Spm", 14.8, 94.8, 178.3, 202.4 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Split", 758.3, 94.8, 899.2, 180.1 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Watts", 774.9, 188.5, 899.2, 273.7 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Kcal", 798.1, 327.1, 899.2, 394.1 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Bpm", 688.5, 327.1, 789.6, 394.1 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "Band", 14.8, 318.3, 242.5, 394.1 },
            new object[] { "TH", HudGeometryCases.FreeWithBpm, "BoatZone", 255.9, 206.1, 658.1, 329.4 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "TopBar", 12.6, 8.4, 901.4, 50.6 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Controls", 303.1, 335.1, 610.9, 394.1 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Wrench", 618.9, 344.6, 658.9, 384.6 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Spm", 14.8, 94.8, 210.7, 220.2 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Split", 758.3, 94.8, 899.2, 180.1 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Watts", 757.1, 188.5, 899.2, 291.5 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Kcal", 798.1, 327.1, 899.2, 394.1 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Bpm", 688.5, 327.1, 789.6, 394.1 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "Band", 14.8, 318.3, 242.5, 394.1 },
            new object[] { "TH", HudGeometryCases.RoutineWithBpm, "BoatZone", 255.9, 206.1, 658.1, 329.4 },
            new object[] { "TH", HudGeometryCases.Long, "TopBar", 12.6, 8.4, 901.4, 50.6 },
            new object[] { "TH", HudGeometryCases.Long, "Controls", 303.1, 335.1, 610.9, 394.1 },
            new object[] { "TH", HudGeometryCases.Long, "Wrench", 618.9, 344.6, 658.9, 384.6 },
            new object[] { "TH", HudGeometryCases.Long, "Spm", 14.8, 94.8, 178.3, 202.4 },
            new object[] { "TH", HudGeometryCases.Long, "Split", 774.9, 94.8, 899.2, 180.1 },
            new object[] { "TH", HudGeometryCases.Long, "Watts", 762.2, 188.5, 899.2, 273.7 },
            new object[] { "TH", HudGeometryCases.Long, "Kcal", 798.1, 327.1, 899.2, 394.1 },
            new object[] { "TH", HudGeometryCases.Long, "Bpm", 688.5, 327.1, 789.6, 394.1 },
            new object[] { "TH", HudGeometryCases.Long, "Band", 14.8, 318.3, 255.9, 394.1 },
            new object[] { "TH", HudGeometryCases.Long, "BoatZone", 255.9, 206.1, 658.1, 329.4 },
            new object[] { "TH", HudGeometryCases.Extremes, "TopBar", 12.6, 8.4, 901.4, 50.6 },
            new object[] { "TH", HudGeometryCases.Extremes, "Controls", 303.1, 335.1, 610.9, 394.1 },
            new object[] { "TH", HudGeometryCases.Extremes, "Wrench", 618.9, 344.6, 658.9, 384.6 },
            new object[] { "TH", HudGeometryCases.Extremes, "Spm", 14.8, 94.8, 214.4, 202.4 },
            new object[] { "TH", HudGeometryCases.Extremes, "Split", 710.0, 94.8, 899.2, 180.1 },
            new object[] { "TH", HudGeometryCases.Extremes, "Watts", 713.9, 188.5, 899.2, 273.7 },
            new object[] { "TH", HudGeometryCases.Extremes, "Kcal", 786.1, 327.1, 899.2, 394.1 },
            new object[] { "TH", HudGeometryCases.Extremes, "Bpm", 676.5, 327.1, 777.7, 394.1 },
            new object[] { "TH", HudGeometryCases.Extremes, "Band", 14.8, 318.3, 255.9, 394.1 },
            new object[] { "TH", HudGeometryCases.Extremes, "BoatZone", 255.9, 206.1, 658.1, 329.4 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "TopBar", 11.1, 7.4, 628.9, 44.3 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Controls", 185.2, 293.5, 454.8, 345.2 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Wrench", 414.8, 245.5, 454.8, 285.5 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Spm", 12.9, 83.1, 156.2, 177.3 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Split", 503.6, 83.1, 627.1, 157.7 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Watts", 518.2, 165.1, 627.1, 239.7 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Kcal", 574.1, 286.5, 627.1, 345.2 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Bpm", 498.4, 286.5, 566.8, 345.2 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "Band", 12.9, 278.8, 177.8, 345.2 },
            new object[] { "L640", HudGeometryCases.FreeWithBpm, "BoatZone", 179.2, 180.6, 460.8, 288.6 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "TopBar", 11.1, 7.4, 628.9, 44.3 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Controls", 185.2, 293.5, 454.8, 345.2 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Wrench", 414.8, 245.5, 454.8, 285.5 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Spm", 12.9, 83.1, 179.2, 192.9 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Split", 503.6, 83.1, 627.1, 157.7 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Watts", 502.6, 165.1, 627.1, 255.3 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Kcal", 574.1, 286.5, 627.1, 345.2 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Bpm", 498.4, 286.5, 566.8, 345.2 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "Band", 12.9, 278.8, 177.8, 345.2 },
            new object[] { "L640", HudGeometryCases.RoutineWithBpm, "BoatZone", 179.2, 180.6, 460.8, 288.6 },
            new object[] { "L640", HudGeometryCases.Long, "TopBar", 11.1, 7.4, 628.9, 44.3 },
            new object[] { "L640", HudGeometryCases.Long, "Controls", 185.2, 293.5, 454.8, 345.2 },
            new object[] { "L640", HudGeometryCases.Long, "Wrench", 414.8, 245.5, 454.8, 285.5 },
            new object[] { "L640", HudGeometryCases.Long, "Spm", 12.9, 83.1, 156.2, 177.3 },
            new object[] { "L640", HudGeometryCases.Long, "Split", 518.2, 83.1, 627.1, 157.7 },
            new object[] { "L640", HudGeometryCases.Long, "Watts", 507.0, 165.1, 627.1, 239.7 },
            new object[] { "L640", HudGeometryCases.Long, "Kcal", 543.4, 286.5, 627.1, 345.2 },
            new object[] { "L640", HudGeometryCases.Long, "Bpm", 467.7, 286.5, 536.0, 345.2 },
            new object[] { "L640", HudGeometryCases.Long, "Band", 12.9, 278.8, 177.8, 345.2 },
            new object[] { "L640", HudGeometryCases.Long, "BoatZone", 179.2, 180.6, 460.8, 288.6 },
            new object[] { "L640", HudGeometryCases.Extremes, "TopBar", 11.1, 7.4, 628.9, 44.3 },
            new object[] { "L640", HudGeometryCases.Extremes, "Controls", 185.2, 293.5, 454.8, 345.2 },
            new object[] { "L640", HudGeometryCases.Extremes, "Wrench", 414.8, 245.5, 454.8, 285.5 },
            new object[] { "L640", HudGeometryCases.Extremes, "Spm", 12.9, 83.1, 187.8, 177.3 },
            new object[] { "L640", HudGeometryCases.Extremes, "Split", 461.3, 83.1, 627.1, 157.7 },
            new object[] { "L640", HudGeometryCases.Extremes, "Watts", 464.7, 165.1, 627.1, 239.7 },
            new object[] { "L640", HudGeometryCases.Extremes, "Kcal", 534.5, 286.5, 627.1, 345.2 },
            new object[] { "L640", HudGeometryCases.Extremes, "Bpm", 462.8, 286.5, 527.2, 345.2 },
            new object[] { "L640", HudGeometryCases.Extremes, "Band", 12.9, 278.8, 177.8, 345.2 },
            new object[] { "L640", HudGeometryCases.Extremes, "BoatZone", 179.2, 180.6, 460.8, 288.6 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "TopBar", 19.2, 12.8, 1260.8, 76.8 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Controls", 406.4, 604.8, 873.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Wrench", 881.6, 629.6, 921.6, 669.6 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Spm", 22.4, 144.0, 270.7, 307.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Split", 1043.6, 144.0, 1257.6, 273.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Watts", 1068.8, 286.2, 1257.6, 415.5 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Kcal", 1104.0, 592.6, 1257.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Bpm", 937.6, 592.6, 1091.2, 694.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "Band", 22.4, 579.2, 368.1, 694.4 },
            new object[] { "W1280", HudGeometryCases.FreeWithBpm, "BoatZone", 358.4, 361.1, 921.6, 577.1 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "TopBar", 19.2, 12.8, 1260.8, 76.8 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Controls", 406.4, 604.8, 873.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Wrench", 881.6, 629.6, 921.6, 669.6 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Spm", 22.4, 144.0, 319.9, 334.3 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Split", 1043.6, 144.0, 1257.6, 273.4 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Watts", 1041.8, 286.2, 1257.6, 442.5 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Kcal", 1104.0, 592.6, 1257.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Bpm", 937.6, 592.6, 1091.2, 694.4 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "Band", 22.4, 579.2, 368.1, 694.4 },
            new object[] { "W1280", HudGeometryCases.RoutineWithBpm, "BoatZone", 358.4, 361.1, 921.6, 577.1 },
            new object[] { "W1280", HudGeometryCases.Long, "TopBar", 19.2, 12.8, 1260.8, 76.8 },
            new object[] { "W1280", HudGeometryCases.Long, "Controls", 406.4, 604.8, 873.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Long, "Wrench", 881.6, 629.6, 921.6, 669.6 },
            new object[] { "W1280", HudGeometryCases.Long, "Spm", 22.4, 144.0, 270.7, 307.4 },
            new object[] { "W1280", HudGeometryCases.Long, "Split", 1068.8, 144.0, 1257.6, 273.4 },
            new object[] { "W1280", HudGeometryCases.Long, "Watts", 1049.5, 286.2, 1257.6, 415.5 },
            new object[] { "W1280", HudGeometryCases.Long, "Kcal", 1104.0, 592.6, 1257.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Long, "Bpm", 937.6, 592.6, 1091.2, 694.4 },
            new object[] { "W1280", HudGeometryCases.Long, "Band", 22.4, 579.2, 393.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Long, "BoatZone", 358.4, 361.1, 921.6, 577.1 },
            new object[] { "W1280", HudGeometryCases.Extremes, "TopBar", 19.2, 12.8, 1260.8, 76.8 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Controls", 406.4, 604.8, 873.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Wrench", 881.6, 629.6, 921.6, 669.6 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Spm", 22.4, 144.0, 325.5, 307.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Split", 970.2, 144.0, 1257.6, 273.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Watts", 976.2, 286.2, 1257.6, 415.5 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Kcal", 1085.8, 592.6, 1257.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Bpm", 954.6, 592.6, 1073.0, 694.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "Band", 22.4, 579.2, 393.6, 694.4 },
            new object[] { "W1280", HudGeometryCases.Extremes, "BoatZone", 358.4, 361.1, 921.6, 577.1 },
            };
        }

        public static IEnumerable<object[]> Scales()
        {
            return new[]
            {
            new object[] { "TV", true, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", true, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", true, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", true, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", true, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.8 },
            new object[] { "TV", true, HudGeometryCases.Extremes, 0.951, 0.952, 0.976, 1.0, 0.651 },
            new object[] { "TV", false, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", false, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", false, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", false, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TV", false, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.8 },
            new object[] { "TV", false, HudGeometryCases.Extremes, 0.951, 0.952, 0.976, 1.0, 0.651 },
            new object[] { "P360", true, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", true, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", true, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", true, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", true, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.8 },
            new object[] { "P360", true, HudGeometryCases.Extremes, 0.951, 0.952, 0.976, 1.0, 0.651 },
            new object[] { "P360", false, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", false, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", false, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", false, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "P360", false, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.8 },
            new object[] { "P360", false, HudGeometryCases.Extremes, 0.951, 0.952, 0.976, 1.0, 0.651 },
            new object[] { "TH", true, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", true, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", true, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", true, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", true, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.778 },
            new object[] { "TH", true, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 1.0, 0.675 },
            new object[] { "TH", false, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", false, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", false, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", false, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "TH", false, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.778 },
            new object[] { "TH", false, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 1.0, 0.675 },
            new object[] { "L640", true, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", true, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", true, HudGeometryCases.RoutineWithBpm, 0.962, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", true, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", true, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.561 },
            new object[] { "L640", true, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 0.915, 0.486 },
            new object[] { "L640", false, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", false, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", false, HudGeometryCases.RoutineWithBpm, 0.962, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", false, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 0.776 },
            new object[] { "L640", false, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.561 },
            new object[] { "L640", false, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 0.92, 0.486 },
            new object[] { "W1280", true, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", true, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", true, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", true, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", true, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.792 },
            new object[] { "W1280", true, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 1.0, 0.687 },
            new object[] { "W1280", false, HudGeometryCases.Free, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", false, HudGeometryCases.FreeWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", false, HudGeometryCases.RoutineWithBpm, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", false, HudGeometryCases.PiramideWattsSplit, 1.0, 1.0, 1.0, 1.0, 1.0 },
            new object[] { "W1280", false, HudGeometryCases.Long, 1.0, 1.0, 1.0, 1.0, 0.792 },
            new object[] { "W1280", false, HudGeometryCases.Extremes, 1.0, 1.0, 1.0, 1.0, 0.687 },
            };
        }

        private static DpRect Piece(HudLayout layout, string piece)
        {
            switch (piece)
            {
                case "TopBar":
                    return layout.TopBar;
                case "Controls":
                    return layout.Controls;
                case "Wrench":
                    return layout.Wrench!.Value;
                case "Spm":
                    return layout.Spm;
                case "Split":
                    return layout.Split;
                case "Watts":
                    return layout.Watts;
                case "Kcal":
                    return layout.Kcal;
                case "Bpm":
                    return layout.Bpm!.Value;
                case "Band":
                    return layout.Band;
                case "BoatZone":
                    return layout.BoatZone;
                default:
                    throw new ArgumentException(piece, nameof(piece));
            }
        }

        private static List<KeyValuePair<string, DpRect>> Pieces(HudLayout layout)
        {
            var pieces = new List<KeyValuePair<string, DpRect>>
            {
                new KeyValuePair<string, DpRect>("TopBar", layout.TopBar),
                new KeyValuePair<string, DpRect>("Controls", layout.Controls),
                new KeyValuePair<string, DpRect>("Spm", layout.Spm),
                new KeyValuePair<string, DpRect>("Split", layout.Split),
                new KeyValuePair<string, DpRect>("Watts", layout.Watts),
                new KeyValuePair<string, DpRect>("Kcal", layout.Kcal),
                new KeyValuePair<string, DpRect>("Band", layout.Band),
            };
            if (layout.Bpm.HasValue)
            {
                pieces.Add(new KeyValuePair<string, DpRect>("Bpm", layout.Bpm.Value));
            }

            if (layout.Wrench.HasValue)
            {
                pieces.Add(new KeyValuePair<string, DpRect>("Wrench", layout.Wrench.Value));
            }

            return pieces;
        }
    }
}
