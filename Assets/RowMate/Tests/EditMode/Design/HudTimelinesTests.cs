using System;
using NUnit.Framework;
using RowMate.Core.Design;
using LiveDotTimeline = RowMate.Core.Design.LiveDot;

namespace RowMate.Tests.EditMode.Design
{
    // HTL: las líneas de tiempo puras del motion de la 4b (02-tech-design §3.1 a §3.10 de la It. 4b; 04-design-notes §4), contra
    // las tablas por cuadro de las notas (a 30 cuadros por segundo: 33 = 33,3 ms, de ahí la tolerancia de 0,01 en los tramos
    // empinados). Son funciones del tiempo en ms enteros: ninguna decide cuándo pasa algo (AC-WKT-13-14).
    [TestFixture]
    public sealed class HudTimelinesTests
    {
        private const double Frame = 0.01;

        [Test, Category("AC-WKT-13-06")]
        public void EntryTableOfTheSpec()
        {
            // 04-design-notes §4.1: Workout funde a negro en 220, el fondo en 220-400 y la cascada de 280 por pieza desde los 400.
            AssertNear(ImmersiveTimeline.EntryPage(33, false), 0.389);
            AssertNear(ImmersiveTimeline.EntryPage(100, false), 0.835);
            AssertNear(ImmersiveTimeline.EntryPage(200, false), 0.999);
            Assert.That(ImmersiveTimeline.EntryPage(220, false), Is.EqualTo(1));
            Assert.That(ImmersiveTimeline.EntryBackdrop(220, false), Is.EqualTo(0));
            AssertNear(ImmersiveTimeline.EntryBackdrop(233, false), 0.207);
            AssertNear(ImmersiveTimeline.EntryBackdrop(267, false), 0.593);
            AssertNear(ImmersiveTimeline.EntryBackdrop(333, false), 0.947);
            Assert.That(ImmersiveTimeline.EntryBackdrop(400, false), Is.EqualTo(1));
            AssertNear(ImmersiveTimeline.Entry(HudPiece.TopBar, 433, false).Opacity, 0.317);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.TopBar, 500, false).Opacity, 0.732);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.TopBar, 433, false).ShiftDp, 5.46, 0.08);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.TopBar, 500, false).ShiftDp, 2.14, 0.08);
            Assert.That(ImmersiveTimeline.Entry(HudPiece.TopBar, 680, false).ShiftDp, Is.EqualTo(0));
            Assert.That(ImmersiveTimeline.Entry(HudPiece.TopBar, 400, false).ShiftDp, Is.EqualTo(8), "+8 (shift.s; la spec pide ≤ 12)");
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Spm, 533, false).Opacity, 0.137);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Spm, 600, false).Opacity, 0.634);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.SplitWatts, 633, false).Opacity, 0.317);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Band, 700, false).Opacity, 0.200);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Vitals, 767, false).Opacity, 0.071);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Controls, 867, false).Opacity, 0.261);
            AssertNear(ImmersiveTimeline.Entry(HudPiece.Controls, 867, false).ShiftDp, 5.91, 0.08);
            Assert.That(ImmersiveTimeline.Entry(HudPiece.Controls, 1120, false).Opacity, Is.EqualTo(1));
            Assert.That(ImmersiveTimeline.Entry(HudPiece.Center, 520, false).Opacity, Is.EqualTo(0), "las del centro van con SPM");
            Assert.That(ImmersiveTimeline.Entry(HudPiece.Controls, 839, false).Opacity, Is.EqualTo(0));
            Assert.That(ImmersiveTimeline.EntryInteractive(HudPiece.Controls, 839, false), Is.False, "P-15: desde su tramo");
            Assert.That(ImmersiveTimeline.EntryInteractive(HudPiece.Controls, 840, false), Is.True);
            Assert.That(ImmersiveTimeline.EntryEndMs(false), Is.EqualTo(1120));
            AssertNear(ImmersiveTimeline.WrenchIn(1133, false), 0.230, 0.02);
            AssertNear(ImmersiveTimeline.WrenchIn(1167, false), 0.644, 0.02);
            Assert.That(ImmersiveTimeline.WrenchIn(1280, false), Is.EqualTo(1));
            Assert.That(ImmersiveTimeline.WrenchIn(1120, false), Is.EqualTo(0), "la llave entra al terminar la cascada");
        }

        [Test, Category("AC-WKT-13-08")]
        public void ExitTable()
        {
            // 04-design-notes §4.2: el HUD 0-200 (ease.exit), el fondo a negro 200-400 (ease.exit) y la página 400-640 (ease.enter).
            var full = ExitStart.Full;
            AssertNear(ImmersiveTimeline.Exit(33, full, false).Hud, 0.995);
            AssertNear(ImmersiveTimeline.Exit(133, full, false).Hud, 0.702);
            AssertNear(ImmersiveTimeline.Exit(167, full, false).Hud, 0.421);
            Assert.That(ImmersiveTimeline.Exit(200, full, false).Hud, Is.EqualTo(0));
            Assert.That(ImmersiveTimeline.Exit(200, full, false).Backdrop, Is.EqualTo(1));
            AssertNear(ImmersiveTimeline.Exit(333, full, false).Backdrop, 0.702);
            Assert.That(ImmersiveTimeline.Exit(400, full, false).Backdrop, Is.EqualTo(0));
            Assert.That(ImmersiveTimeline.Exit(400, full, false).Page, Is.EqualTo(1));
            AssertNear(ImmersiveTimeline.Exit(433, full, false).Page, 0.638);
            AssertNear(ImmersiveTimeline.Exit(467, full, false).Page, 0.378);
            AssertNear(ImmersiveTimeline.Exit(600, full, false).Page, 0.005);
            Assert.That(ImmersiveTimeline.Exit(640, full, false).Page, Is.EqualTo(0));
            Assert.That(ImmersiveTimeline.ExitEndMs(false), Is.EqualTo(640), "menos de 1 s");
            Assert.That(ImmersiveTimeline.RevealStartMs(false), Is.EqualTo(400), "los avisos del final, DN4b-24");
            Assert.That(MotionSpecs.EndToastStartMs, Is.EqualTo(400));
            // Interrumpida la entrada, la salida arranca desde las opacidades que tiene (AC-WKT-13-07).
            var partial = new ExitStart(0.4, 0.9);
            Assert.That(ImmersiveTimeline.Exit(0, partial, false).Backdrop, Is.EqualTo(0.4));
            Assert.That(ImmersiveTimeline.Exit(0, partial, false).Page, Is.EqualTo(0.9));
            AssertNear(ImmersiveTimeline.Exit(433, partial, false).Page, 0.9 * 0.638);
        }

        [Test, Category("AC-WKT-13-12")]
        public void PauseVeil()
        {
            // 04-design-notes §4.8: el velo 0 → 0,15 en 300 (ease.move) y de vuelta en 240; PAUSA entra y sale en 240 (0,96 ↔ 1).
            AssertNear(PauseTimeline.Veil(33, true, 0, false), 0.0017, 0.001);
            AssertNear(PauseTimeline.Veil(100, true, 0, false), 0.0224, 0.002);
            AssertNear(PauseTimeline.Veil(200, true, 0, false), 0.1276, 0.002);
            Assert.That(PauseTimeline.Veil(300, true, 0, false), Is.EqualTo(0.15));
            AssertNear(PauseTimeline.Veil(40, false, 0.15, false), 0.1459, 0.002);
            AssertNear(PauseTimeline.Veil(120, false, 0.15, false), 0.0750, 0.002);
            Assert.That(PauseTimeline.Veil(240, false, 0.15, false), Is.EqualTo(0));
            Assert.That(PauseTimeline.EndMs(true), Is.EqualTo(300));
            var paused = CenterDiscTimeline.PausedIn(33, false);
            AssertNear(paused.PausedOpacity, 0.362);
            AssertNear(paused.PausedScale, 0.974, 0.002);
            Assert.That(CenterDiscTimeline.PausedIn(240, false).PausedOpacity, Is.EqualTo(1));
            var resumed = CenterDiscTimeline.PausedOut(200, false);
            AssertNear(resumed.PausedOpacity, 0.422);
            AssertNear(resumed.PausedScale, 0.977, 0.002);
            Assert.That(CenterDiscTimeline.PausedOut(240, false).PausedScale, Is.EqualTo(0.96));
        }

        [Test, Category("AC-WKT-14-11")]
        public void HaloIntensityAndRamp()
        {
            // 04-design-notes §4.4: α = a·(0,30 + 0,40·p), la rampa de 150 (ease.move); con reducir movimiento, a·0,70.
            AssertNear(StepBarTimeline.HaloRamp(100, true), 0.851);
            Assert.That(StepBarTimeline.HaloRamp(150, true), Is.EqualTo(1));
            Assert.That(StepBarTimeline.HaloRamp(0, true), Is.EqualTo(0));
            AssertNear(StepBarTimeline.Halo(0.851, 0.5, false), 0.425, 0.002);
            Assert.That(StepBarTimeline.Halo(1, 1, false), Is.EqualTo(0.70).Within(1e-12));
            Assert.That(StepBarTimeline.Halo(1, 0, false), Is.EqualTo(0.30).Within(1e-12));
            AssertNear(StepBarTimeline.Halo(0.851, 0.5, true), 0.596, 0.002);
            Assert.That(StepBarTimeline.HaloAtEdge(0.70), Is.EqualTo(0.455).Within(1e-9), "en el borde, 0,65·α");
        }

        [Test, Category("AC-WKT-14-11")]
        public void HaloEdgeMatchesTheLegacyShadow()
        {
            // DN4b-17: lo que deja el BoxShadow del legacy (expansión 2, desenfoque 8, sigma 5,12; step_pill.dart:88-94) a una
            // distancia d del borde: 0,652 en el borde, que es el 0,65 de PillGlowEdgeFactor.
            Assert.That(PillShapes.LegacyShadowSigma, Is.EqualTo(5.12).Within(0.005));
            var legacy = new[] { (0.0, 0.652), (1.0, 0.577), (2.0, 0.500), (4.0, 0.348), (6.0, 0.217), (8.0, 0.121), (10.0, 0.059) };
            foreach (var e in legacy)
            {
                Assert.That(PillShapes.LegacyShadowFraction(e.Item1), Is.EqualTo(e.Item2).Within(0.001), "d = " + e.Item1);
            }

            Assert.That(MotionSpecs.PillGlowEdgeFactor, Is.EqualTo(PillShapes.LegacyShadowFraction(0)).Within(0.003));
            Assert.That(MotionSpecs.PillGlowBlurDp, Is.EqualTo(PillShapes.LegacyShadowBlur));
            Assert.That(MotionSpecs.PillGlowSpreadDp, Is.EqualTo(PillShapes.LegacyShadowSpread));
        }

        [Test, Category("AC-WKT-14-12")]
        public void PillChangeTable()
        {
            // 04-design-notes §4.5, t = 0 en el cuadro en que el estado trae el paso nuevo.
            AssertNear(StepBarTimeline.PillChange(33, true, false).CompletingFill, 0.092);
            AssertNear(StepBarTimeline.PillChange(67, true, false).CompletingFill, 0.652);
            AssertNear(StepBarTimeline.PillChange(100, true, false).CompletingFill, 0.973);
            Assert.That(StepBarTimeline.PillChange(120, true, false).CompletingFill, Is.EqualTo(1));
            Assert.That(StepBarTimeline.PillChange(119, true, false).Flash, Is.EqualTo(0));
            AssertNear(StepBarTimeline.PillChange(133, true, false).Flash, 0.331);
            AssertNear(StepBarTimeline.PillChange(167, true, false).Flash, 0.196);
            AssertNear(StepBarTimeline.PillChange(200, true, false).Flash, 0.104);
            AssertNear(StepBarTimeline.PillChange(267, true, false).Flash, 0.015);
            Assert.That(StepBarTimeline.PillChange(340, true, false).Flash, Is.EqualTo(0));
            AssertNear(StepBarTimeline.PillChange(167, true, false).SolidOpacity, 0.993);
            AssertNear(StepBarTimeline.PillChange(233, true, false).SolidOpacity, 0.919);
            AssertNear(StepBarTimeline.PillChange(300, true, false).SolidOpacity, 0.855);
            Assert.That(StepBarTimeline.PillChange(340, true, false).SolidOpacity, Is.EqualTo(0.85).Within(1e-12));
            AssertNear(StepBarTimeline.PillChange(167, true, false).Highlight, 0.952);
            AssertNear(StepBarTimeline.PillChange(233, true, false).Highlight, 0.457, 0.02);
            Assert.That(StepBarTimeline.PillChange(340, true, false).Highlight, Is.EqualTo(0));
            AssertNear(StepBarTimeline.PillChange(200, false, false).HollowInterior, 0.942);
            AssertNear(StepBarTimeline.PillChange(267, false, false).HollowInterior, 0.685, 0.02);
            AssertNear(StepBarTimeline.PillChange(333, false, false).HollowInterior, 0.302, 0.02);
            Assert.That(StepBarTimeline.PillChange(440, false, false).HollowInterior, Is.EqualTo(0.18).Within(1e-12));
            AssertNear(StepBarTimeline.PillChange(167, false, false).HollowStroke, 0.020);
            AssertNear(StepBarTimeline.PillChange(267, false, false).HollowStroke, 0.385, 0.02);
            AssertNear(StepBarTimeline.PillChange(333, false, false).HollowStroke, 0.851, 0.02);
            Assert.That(StepBarTimeline.PillChange(440, false, false).HollowStroke, Is.EqualTo(1));
            Assert.That(StepBarTimeline.PillChange(440, false, false).Flash, Is.EqualTo(0), "la hueca no destella");
            AssertNear(StepBarTimeline.PillChange(33, false, false).NewTrack, 0.186);
            AssertNear(StepBarTimeline.PillChange(100, false, false).NewTrack, 0.282);
            Assert.That(StepBarTimeline.PillChange(150, false, false).NewTrack, Is.EqualTo(0.30).Within(1e-12));
            AssertNear(StepBarTimeline.PillChange(33, true, false).EndingHaloRamp, 0.947);
            AssertNear(StepBarTimeline.PillChange(67, true, false).EndingHaloRamp, 0.650);
            Assert.That(StepBarTimeline.PillChange(150, true, false).EndingHaloRamp, Is.EqualTo(0));
            Assert.That(StepBarTimeline.PillChangeEndMs(true), Is.EqualTo(340));
            Assert.That(StepBarTimeline.PillChangeEndMs(false), Is.EqualTo(440));
        }

        [Test, Category("AC-WKT-14-16")]
        public void ThumbSwap()
        {
            // DN4b-18: el pulgar que se va baja a la franja y se apaga mientras el nuevo sube, en 150 (ease.move); nunca hay dos
            // pulgares enteros.
            var start = StepBarTimeline.ThumbSwap(0, false);
            Assert.That(start.OutgoingHeight, Is.EqualTo(10));
            Assert.That(start.OutgoingOpacity, Is.EqualTo(1));
            Assert.That(start.IncomingHeight, Is.EqualTo(8));
            Assert.That(start.IncomingOpacity, Is.EqualTo(0));
            var mid = StepBarTimeline.ThumbSwap(75, false);
            Assert.That(mid.OutgoingHeight, Is.EqualTo(9).Within(0.01));
            Assert.That(mid.IncomingOpacity, Is.EqualTo(0.5).Within(0.01));
            var end = StepBarTimeline.ThumbSwap(150, false);
            Assert.That(end.OutgoingOpacity, Is.EqualTo(0));
            Assert.That(end.IncomingHeight, Is.EqualTo(10));
            for (long t = 0; t <= 150; t++)
            {
                var v = StepBarTimeline.ThumbSwap(t, false);
                Assert.That(v.OutgoingOpacity < 1 || v.IncomingOpacity < 1, Is.True, "nunca dos enteros (" + t + ")");
            }
        }

        [Test, Category("AC-WKT-14-15")]
        public void ModeChangeAndTargetOut()
        {
            // DN4b-16: con el cambio de paso, los aires y el relleno de Próximo pasan al modo nuevo en 0-240 (ease.move); la pastilla
            // que no entra con Próximo funde en 120 (ease.exit).
            var normal = new InfoRowPlan(InfoRowMode.Normal, 1, 1);
            var compact = new InfoRowPlan(InfoRowMode.Compact2, 2, 0);
            var start = InfoRowTimeline.ModeChange(0, normal, compact, false);
            Assert.That(start.CountdownGap, Is.EqualTo(8));
            Assert.That(start.ClockGap, Is.EqualTo(10));
            Assert.That(start.UpNextPadH, Is.EqualTo(7));
            var mid = InfoRowTimeline.ModeChange(120, normal, compact, false);
            Assert.That(mid.CountdownGap, Is.EqualTo(7).Within(0.01));
            Assert.That(mid.ClockGap, Is.EqualTo(8).Within(0.01));
            var end = InfoRowTimeline.ModeChange(240, normal, compact, false);
            Assert.That(end.CountdownGap, Is.EqualTo(6));
            Assert.That(end.ClockGap, Is.EqualTo(6));
            Assert.That(end.UpNextPadH, Is.EqualTo(4));
            Assert.That(InfoRowTimeline.TargetOut(0, false), Is.EqualTo(1));
            AssertNear(InfoRowTimeline.TargetOut(60, false), 0.872);
            Assert.That(InfoRowTimeline.TargetOut(120, false), Is.EqualTo(0));
            Assert.That(MotionSpecs.InfoRowModeChangeMs, Is.EqualTo(240));
            Assert.That(MotionSpecs.TargetPillOutMs, Is.EqualTo(120));
        }

        [Test, Category("AC-WKT-14-18")]
        public void BigCountInOut()
        {
            // 04-design-notes §4.6: entra en 200 (0,85 → 1, ease.enter) y sale en 200 (ease.exit, 1 → 1,10 "se suelta").
            AssertNear(CenterDiscTimeline.CountIn(33, false).CountOpacity, 0.421);
            AssertNear(CenterDiscTimeline.CountIn(33, false).CountScale, 0.913);
            AssertNear(CenterDiscTimeline.CountIn(100, false).CountOpacity, 0.872);
            AssertNear(CenterDiscTimeline.CountIn(100, false).CountScale, 0.981);
            Assert.That(CenterDiscTimeline.CountIn(200, false).CountScale, Is.EqualTo(1));
            AssertNear(CenterDiscTimeline.CountOut(33, false).CountOpacity, 0.995);
            AssertNear(CenterDiscTimeline.CountOut(33, false).CountScale, 1.042);
            AssertNear(CenterDiscTimeline.CountOut(133, false).CountOpacity, 0.702);
            AssertNear(CenterDiscTimeline.CountOut(133, false).CountScale, 1.096);
            Assert.That(CenterDiscTimeline.CountOut(200, false).CountOpacity, Is.EqualTo(0));
            Assert.That(CenterDiscTimeline.CountOut(200, false).CountScale, Is.EqualTo(1.10).Within(1e-12));
            AssertNear(CenterDiscTimeline.CountIn(33, true).CountOpacity, 0.503);
            Assert.That(CenterDiscTimeline.CountIn(33, true).CountScale, Is.EqualTo(1), "solo opacidad");
            AssertNear(CenterDiscTimeline.CountOut(33, true).CountOpacity, 0.977);
            AssertNear(CenterDiscTimeline.CountOut(100, true).CountOpacity, 0.422);
            Assert.That(CenterDiscTimeline.CountOut(120, true).CountOpacity, Is.EqualTo(0));
            Assert.That(CenterDiscTimeline.CountInEndMs(false), Is.EqualTo(200));
            Assert.That(CenterDiscTimeline.CountOutEndMs(true), Is.EqualTo(120));
        }

        [Test, Category("AC-WKT-14-18"), Category("AC-WKT-13-12")]
        public void OneDiscAtATime()
        {
            // DN4b-14: el que sale funde en 0-120 (ease.exit, sin escala) y el que entra empieza a los 120 con su entrada; nunca dos
            // discos a la vista (04-design-notes §4.8).
            AssertNear(CenterDiscTimeline.Swap(33, true, false).CountOpacity, 0.977);
            AssertNear(CenterDiscTimeline.Swap(100, true, false).CountOpacity, 0.422);
            Assert.That(CenterDiscTimeline.Swap(120, true, false).CountOpacity, Is.EqualTo(0));
            AssertNear(CenterDiscTimeline.Swap(133, true, false).PausedOpacity, 0.158, 0.02);
            AssertNear(CenterDiscTimeline.Swap(200, true, false).PausedOpacity, 0.702);
            Assert.That(CenterDiscTimeline.Swap(360, true, false).PausedOpacity, Is.EqualTo(1));
            Assert.That(CenterDiscTimeline.Swap(33, true, false).CountScale, Is.EqualTo(1), "el que sale, sin escala");
            AssertNear(CenterDiscTimeline.Swap(200, false, false).CountScale, 0.85 + 0.15 * Easing.Evaluate(Tokens.EaseEnter, 0.4), 1e-9);
            for (long t = 0; t <= 400; t++)
            {
                foreach (var toPaused in new[] { true, false })
                {
                    foreach (var reduced in new[] { true, false })
                    {
                        var v = CenterDiscTimeline.Swap(t, toPaused, reduced);
                        Assert.That(Math.Min(v.CountOpacity, v.PausedOpacity), Is.EqualTo(0), "un disco por vez (" + t + ")");
                    }
                }
            }

            Assert.That(CenterDiscTimeline.Swap(200, true, true).PausedScale, Is.EqualTo(1), "con reducir movimiento, sin escala");
            Assert.That(CenterDiscTimeline.SwapEndMs(true, false), Is.EqualTo(360));
            Assert.That(CenterDiscTimeline.SwapEndMs(false, false), Is.EqualTo(320));
        }

        [Test, Category("AC-WKT-09-08")]
        public void SealWithinTwoSeconds()
        {
            // 04-design-notes §4.7: todo dentro de los 2 s; t = 0 en LastOutcomeAt.
            AssertNear(SealTimeline.At(33, false).BarHeight, 52.4, 0.1);
            AssertNear(SealTimeline.At(67, false).BarHeight, 54.0, 0.3);
            AssertNear(SealTimeline.At(100, false).BarHeight, 58.3, 0.4);
            AssertNear(SealTimeline.At(200, false).BarHeight, 73.4, 0.2);
            Assert.That(SealTimeline.At(240, false).BarHeight, Is.EqualTo(74));
            Assert.That(SealTimeline.At(40, false).ContentOpacity, Is.EqualTo(0));
            AssertNear(SealTimeline.At(67, false).ContentOpacity, 0.350);
            AssertNear(SealTimeline.At(100, false).ContentOpacity, 0.655);
            Assert.That(SealTimeline.At(240, false).ContentOpacity, Is.EqualTo(1));
            AssertNear(SealTimeline.At(100, false).IconScale, 0.846, 0.02);
            AssertNear(SealTimeline.At(167, false).IconScale, 1.025);
            AssertNear(SealTimeline.At(200, false).IconScale, 1.039);
            Assert.That(SealTimeline.At(300, false).IconScale, Is.EqualTo(1));
            Assert.That(SealTimeline.At(1000, false).BarHeight, Is.EqualTo(74), "el sostén");
            AssertNear(SealTimeline.At(1833, false).ContentOpacity, 0.977);
            AssertNear(SealTimeline.At(1900, false).ContentOpacity, 0.422);
            Assert.That(SealTimeline.At(1920, false).ContentOpacity, Is.EqualTo(0));
            AssertNear(SealTimeline.At(1867, false).BarHeight, 70.7, 0.3);
            AssertNear(SealTimeline.At(1900, false).BarHeight, 63.0, 0.3);
            Assert.That(SealTimeline.At(2000, false).BarHeight, Is.EqualTo(52));
            Assert.That(SealTimeline.At(2000, false).ContentOpacity, Is.EqualTo(0));
            Assert.That(SealTimeline.EndMs, Is.EqualTo(2000));
            // El reemplazo (pasos de 2 s o menos): el viejo funde en 0-80, el nuevo entra en 80-200; la barra sigue abierta.
            AssertNear(SealTimeline.Swap(33, false).OldContentOpacity, 0.925);
            AssertNear(SealTimeline.Swap(67, false).OldContentOpacity, 0.421, 0.02);
            Assert.That(SealTimeline.Swap(80, false).OldContentOpacity, Is.EqualTo(0));
            AssertNear(SealTimeline.Swap(100, false).ContentOpacity, 0.422);
            AssertNear(SealTimeline.Swap(133, false).ContentOpacity, 0.826);
            Assert.That(SealTimeline.Swap(200, false).ContentOpacity, Is.EqualTo(1));
            Assert.That(SealTimeline.Swap(100, false).BarHeight, Is.EqualTo(74));
            Assert.That(SealTimeline.Swap(320, false).IconScale, Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-16-05")]
        public void LiveDot()
        {
            // 04-design-notes §4.9: late entre 0,35 y 1 con un ciclo de 1600 (800 bajando y 800 subiendo, ease.pulse), siempre
            // dentro de [0,35; 1]; en 800 toca el mínimo y en cada múltiplo de 1600 vuelve a 1.
            for (long t = 0; t <= 6400; t += 10)
            {
                var v = LiveDotTimeline.At(t, false);
                Assert.That(v, Is.InRange(MotionSpecs.LiveDotMinOpacity - 1e-9, 1 + 1e-9), "en " + t);
            }

            AssertNear(LiveDotTimeline.At(800, false), MotionSpecs.LiveDotMinOpacity, 1e-9);
            AssertNear(LiveDotTimeline.At(2400, false), MotionSpecs.LiveDotMinOpacity, 1e-9);
            AssertNear(LiveDotTimeline.At(1600, false), 1, 1e-9);
            AssertNear(LiveDotTimeline.At(400, false), LiveDotTimeline.At(1200, false), 1e-9);
            Assert.That(LiveDotTimeline.At(100, false), Is.LessThan(1), "empieza bajando");
        }

        [Test, Category("AC-WKT-16-05")]
        public void LiveDotStartsAtOne()
        {
            // 04-design-notes §4.9: q de 1 a 0 en 800 y vuelta (ease.pulse), opacidad 0,35 + 0,65·q, empezando en 1 al pasar a EN
            // MARCHA. Por cuadro de 200: 1 · 0,906 · 0,675 · 0,444 · 0,350 · 0,444 · 0,675 · 0,906 · 1.
            var expected = new[] { 1, 0.906, 0.675, 0.444, 0.350, 0.444, 0.675, 0.906, 1 };
            for (var i = 0; i < expected.Length; i++)
            {
                AssertNear(LiveDotTimeline.At(i * 200, false), expected[i], 0.002);
            }

            AssertNear(LiveDotTimeline.At(42200, false), 0.444, 0.002);
            Assert.That(LiveDotTimeline.At(3200, false), Is.EqualTo(1).Within(1e-9), "DEMO-02");
            Assert.That(LiveDotTimeline.At(1000, true), Is.EqualTo(1), "con reducir movimiento, quieto en 1");
        }

        [Test, Category("AC-WKT-16-03")]
        public void PhaseChipColorsAndLabel()
        {
            // 04-design-notes §4.9: los colores cruzan en 150 (0,053 · 0,350 · 0,851 · 0,989 en los cuadros 1 a 4) y la etiqueta se
            // reemplaza (0-80 y 80-200).
            AssertNear(PhaseChipTimeline.At(33).ColorMix, 0.053, 0.015);
            AssertNear(PhaseChipTimeline.At(67).ColorMix, 0.350, 0.03);
            AssertNear(PhaseChipTimeline.At(100).ColorMix, 0.851, 0.01);
            AssertNear(PhaseChipTimeline.At(133).ColorMix, 0.989, 0.01);
            AssertNear(PhaseChipTimeline.At(33).OldLabelOpacity, 0.925);
            Assert.That(PhaseChipTimeline.At(80).OldLabelOpacity, Is.EqualTo(0));
            AssertNear(PhaseChipTimeline.At(100).NewLabelOpacity, 0.422);
            AssertNear(PhaseChipTimeline.At(133).NewLabelOpacity, 0.826);
            Assert.That(PhaseChipTimeline.At(200).NewLabelOpacity, Is.EqualTo(1));
            Assert.That(PhaseChipTimeline.EndMs, Is.EqualTo(200));
        }

        [Test, Category("AC-WKT-17-11")]
        public void KcalSlide()
        {
            // 04a/04-design-notes §5.6 (FLIP): aparece BPM, kcal en 0-240 y BPM en 120-360; se va, BPM en 0-120 y kcal en 120-360.
            AssertNear(1 - KcalSlideTimeline.At(60, true, false).KcalShift, 0.071);
            AssertNear(1 - KcalSlideTimeline.At(120, true, false).KcalShift, 0.500);
            Assert.That(KcalSlideTimeline.At(240, true, false).KcalShift, Is.EqualTo(0));
            AssertNear(KcalSlideTimeline.At(180, true, false).BpmOpacity, 0.577);
            AssertNear(KcalSlideTimeline.At(240, true, false).BpmOpacity, 0.872);
            Assert.That(KcalSlideTimeline.At(360, true, false).BpmOpacity, Is.EqualTo(1));
            AssertNear(KcalSlideTimeline.At(60, false, false).BpmOpacity, 0.872);
            Assert.That(KcalSlideTimeline.At(120, false, false).BpmOpacity, Is.EqualTo(0));
            Assert.That(KcalSlideTimeline.At(120, false, false).KcalShift, Is.EqualTo(1), "kcal espera a que BPM salga");
            AssertNear(1 - KcalSlideTimeline.At(180, false, false).KcalShift, 0.071);
            Assert.That(KcalSlideTimeline.At(360, false, false).KcalShift, Is.EqualTo(0));
            Assert.That(KcalSlideTimeline.EndMs(true, false), Is.EqualTo(360));
        }

        [Test, Category("AC-WKT-21-05")]
        public void CompletionCheckAndCountUp()
        {
            // 04-design-notes §4.12, t = 0 cuando entra el diálogo (a los 300 del fin): el disco 0,6 → 1 en 60-300 (ease.emphasis),
            // el trazo en 180-500 (ease.move) y los totales en 700 desde los 220, de a 120.
            AssertNear(CompletionTimeline.At(67, false).CheckScale, 0.650, 0.02);
            AssertNear(CompletionTimeline.At(100, false).CheckScale, 0.846, 0.02);
            AssertNear(CompletionTimeline.At(200, false).CheckScale, 1.039);
            Assert.That(CompletionTimeline.At(300, false).CheckScale, Is.EqualTo(1));
            AssertNear(CompletionTimeline.At(200, false).CheckStroke, 0.003, 0.005);
            AssertNear(CompletionTimeline.At(300, false).CheckStroke, 0.208, 0.02);
            AssertNear(CompletionTimeline.At(400, false).CheckStroke, 0.875, 0.02);
            Assert.That(CompletionTimeline.At(500, false).CheckStroke, Is.EqualTo(1));
            Assert.That(CompletionTimeline.EndMs(false), Is.EqualTo(1160), "el conteo termina a los 1160 del diálogo");
            Assert.That(CountUp.StartMs(0), Is.EqualTo(220));
            Assert.That(CountUp.StartMs(1), Is.EqualTo(340));
            Assert.That(CountUp.StartMs(2), Is.EqualTo(460));
            // Corta: 30 s, 1120 m, 43 kcal (tabla de las notas, ± 1 por el redondeo del cuadro).
            Assert.That(CountUp.ValueAt(30, 220, 220), Is.EqualTo(0));
            Assert.That(CountUp.ValueAt(30, 300, 220), Is.InRange(8, 10), "00:09 a los 600 del fin");
            Assert.That(CountUp.ValueAt(30, 600, 220), Is.InRange(26, 28), "00:27 a los 900");
            Assert.That(CountUp.ValueAt(1120, 467, 340), Is.InRange(504, 506), "505 m a los 767");
            Assert.That(CountUp.ValueAt(1120, 533, 340), Is.InRange(692, 694), "693 m a los 833");
            Assert.That(CountUp.ValueAt(43, 600, 460), Is.InRange(19, 21), "20 a los 900");
        }

        [Test, Category("AC-WKT-21-05")]
        public void CountUpEndsAtTheExactValue()
        {
            // Los totales terminan SIEMPRE en el valor exacto, nunca lo pasan y nunca bajan (ANIMATION_BIBLE §2.5).
            foreach (var final in new long[] { 30, 1120, 43, 35999, 16777215, 65535, 1, 0 })
            {
                var last = -1L;
                for (long t = 0; t <= 1300; t += 7)
                {
                    var value = CountUp.ValueAt(final, t, 220);
                    Assert.That(value, Is.LessThanOrEqualTo(final));
                    Assert.That(value, Is.GreaterThanOrEqualTo(last));
                    last = value;
                }

                Assert.That(CountUp.ValueAt(final, 920, 220), Is.EqualTo(final), final + ": exacto al final");
                Assert.That(CountUp.ValueAt(final, 0, 220, reduced: true), Is.EqualTo(final), "con reducir movimiento, desde el primer cuadro");
            }
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedVariants()
        {
            // §3.10, fila por fila. El cambio de paso va SIN destello ni trazo (AC-APP-05-08; QA-G4bT-03).
            for (long t = 0; t <= 440; t++)
            {
                Assert.That(StepBarTimeline.PillChange(t, true, true).Flash, Is.EqualTo(0), "sin destello (" + t + ")");
                Assert.That(StepBarTimeline.PillChange(t, false, true).HollowStroke, Is.EqualTo(1), "sin trazo: el borde entero que funde");
            }

            Assert.That(StepBarTimeline.PillChange(120, false, true).HollowBorderOpacity, Is.EqualTo(0));
            Assert.That(StepBarTimeline.PillChange(280, false, true).HollowBorderOpacity, Is.EqualTo(1), "funde de 0 a 1 en 160");
            Assert.That(StepBarTimeline.PillChange(230, true, true).SolidOpacity, Is.LessThan(1), "la sólida solo funde de 1 a 0,85");
            Assert.That(StepBarTimeline.PillChange(230, true, true).Highlight, Is.LessThan(1), "el brillo se va con su fundido");
            var thumb = StepBarTimeline.ThumbSwap(75, true);
            Assert.That(thumb.OutgoingHeight, Is.EqualTo(10), "el pulgar sin subir ni bajar");
            Assert.That(thumb.IncomingHeight, Is.EqualTo(10));
            Assert.That(thumb.OutgoingOpacity + thumb.IncomingOpacity, Is.EqualTo(1).Within(1e-9), "fundido cruzado de 150");
            Assert.That(ImmersiveTimeline.Entry(HudPiece.TopBar, 0, true).Opacity, Is.EqualTo(1), "sin cascada");
            Assert.That(ImmersiveTimeline.EntryBackdrop(0, true), Is.EqualTo(1), "sin negro");
            Assert.That(ImmersiveTimeline.EntryPage(60, true), Is.EqualTo(0.5).Within(1e-9), "fundido lineal de 120");
            Assert.That(ImmersiveTimeline.EntryInteractive(HudPiece.Controls, 0, true), Is.True, "los controles desde el cuadro 0");
            Assert.That(ImmersiveTimeline.EntryEndMs(true), Is.EqualTo(120), "la llave a los 120");
            Assert.That(ImmersiveTimeline.Exit(60, ExitStart.Full, true).Page, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(ImmersiveTimeline.ExitEndMs(true), Is.EqualTo(120));
            Assert.That(ImmersiveTimeline.RevealStartMs(true), Is.EqualTo(0), "los avisos en el cuadro 0");
            Assert.That(StepBarTimeline.Halo(1, 0, true), Is.EqualTo(0.70), "el halo fijo en 0,70");
            var mode = InfoRowTimeline.ModeChange(0, new InfoRowPlan(InfoRowMode.Normal, 0, 0), new InfoRowPlan(InfoRowMode.Compact2, 0, 0), true);
            Assert.That(mode.CountdownGap, Is.EqualTo(6), "los aires en el acto");
            Assert.That(InfoRowTimeline.TargetOut(60, true), Is.LessThan(1), "la pastilla, su fundido de 120");
            Assert.That(CenterDiscTimeline.CountIn(100, true).CountScale, Is.EqualTo(1));
            Assert.That(CenterDiscTimeline.CountOut(60, true).CountScale, Is.EqualTo(1));
            Assert.That(CenterDiscTimeline.PausedIn(100, true).PausedScale, Is.EqualTo(1));
            Assert.That(CenterDiscTimeline.Swap(200, false, true).CountScale, Is.EqualTo(1), "un disco por vez, sin escala");
            Assert.That(SealTimeline.At(0, true).BarHeight, Is.EqualTo(74), "el sello: el alto en el acto");
            Assert.That(SealTimeline.At(100, true).IconScale, Is.EqualTo(1), "el ícono no escala");
            Assert.That(SealTimeline.At(160, true).ContentOpacity, Is.EqualTo(1), "el contenido en 160");
            Assert.That(SealTimeline.At(1920, true).ContentOpacity, Is.EqualTo(0), "y en 120");
            Assert.That(PauseTimeline.Veil(150, true, 0, true), Is.GreaterThan(0), "el velo: el mismo fundido");
            Assert.That(LiveDotTimeline.At(600, true), Is.EqualTo(1), "el punto quieto en 1");
            Assert.That(KcalSlideTimeline.At(0, true, true).KcalShift, Is.EqualTo(0), "kcal en el acto");
            AssertNear(KcalSlideTimeline.At(120, true, true).BpmOpacity, Easing.Evaluate(Tokens.EaseEnter, 0.5), 1e-9);
            var completion = CompletionTimeline.At(0, true);
            Assert.That(completion.CheckScale, Is.EqualTo(1), "el check entero");
            Assert.That(completion.CheckStroke, Is.EqualTo(1));
            Assert.That(completion.TimeFraction, Is.EqualTo(1), "los totales finales desde el primer cuadro");
            Assert.That(CompletionTimeline.EndMs(true), Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-13-14")]
        public void ReachedAndMsAtTheEdges()
        {
            // QA-G4bT-07 (b): 30,2 − 30,0 = 0,1999999999999993 s da 200 ms; Reached no depende de la coma flotante.
            Assert.That(30.2 - 30.0, Is.LessThan(0.2));
            Assert.That(HudTimelines.Ms(30.2 - 30.0), Is.EqualTo(200));
            Assert.That(HudTimelines.Reached(HudTimelines.Ms(30.2 - 30.0), 200L), Is.True);
            Assert.That(HudTimelines.Reached(199, 200L), Is.False);
            Assert.That(HudTimelines.Ms(0.0004), Is.EqualTo(0));
            Assert.That(HudTimelines.Ms(1.1205), Is.EqualTo(1121), "la mitad, lejos del cero (DartMath)");
        }

        private static void AssertNear(double actual, double expected, double tolerance = Frame)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance));
        }
    }
}
