using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HMT (04b-hud-exr/02-tech-design §6.2, §7.4; WP-D1): cada IMotion del HUD de la It. 4b con elementos de prueba y el reloj de
    // prueba. Lo que se mira en cada uno:
    // - los valores que le pasa a la vista, cuadro a cuadro, son los de su línea de tiempo pura (HudTimelines, WP-A), con los ms
    //   enteros de HudTimelines.Ms;
    // - IsFinite en cada fase (F bloquea una toma, C no; §5.2) y que sale del MotionDriver cuando la tabla de §7.4 dice "sale";
    // - la interrupción, el reemplazo, Instant y "sin armar" (estado final en el acto), reducir movimiento;
    // - Stop: nada queda animando (ni en el MotionDriver ni pidiendo 60 fps; lección de QA-V4a-02 = ROB-02) y ningún aviso
    //   después;
    // - 0 B por cuadro (ARCHITECTURE §7.8).
    // Además, lo que WP-D1 les suma a las piezas de la 4a para la regla de toma: ISettlingMotion (SpringMotion, StepPulse,
    // FitBox) y el final de TimedMotion con HudTimelines.Reached (QA-G4bT-07).
    // Las categorías son solo las de la matriz de §6.4 para HMT (AC-WKT-13-06, -14-12, -21-05, AC-APP-05-08): lo demás lo
    // verifican en pantalla, con las vistas de WP-B, PRT, CCT, SVT, FBT, PVT, CDT, ITT e IHT.
    public sealed class HudMotionTests : UiTestBase
    {
        private const double Eps = 1e-9;

        // ── Entrada "sumergirse" (§3.2; AC-WKT-13-06) ────────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-13-06")]
        public void CascadeFollowsTheEntryTimelineFrameByFrame()
        {
            var rig = new Rig();
            var start = rig.Now;
            var opacity = new double[7];
            var shift = new double[7];
            var backdrop = -1.0;
            var respondedAt = new long[7];
            for (var i = 0; i < respondedAt.Length; i++)
            {
                respondedAt[i] = -1;
            }

            var doneAt = -1L;
            var done = 0;
            var motion = new ImmersiveCascadeMotion(rig.Driver,
                (p, v) =>
                {
                    opacity[(int)p] = v.Opacity;
                    shift[(int)p] = v.ShiftDp;
                },
                b => backdrop = b,
                p =>
                {
                    Assert.That(respondedAt[(int)p], Is.EqualTo(-1), "cada pieza avisa una sola vez: " + p);
                    respondedAt[(int)p] = HudTimelines.Ms(rig.Now - start);
                },
                () =>
                {
                    done++;
                    doneAt = HudTimelines.Ms(rig.Now - start);
                });

            motion.Play();
            Assert.That(motion.IsRunning, Is.True);
            Assert.That(motion.IsFinite, Is.True, "F de 0 a 1120");
            for (var k = 0; k <= 75; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                foreach (var piece in Pieces)
                {
                    var expected = ImmersiveTimeline.Entry(piece, t, false);
                    Assert.That(opacity[(int)piece], Is.EqualTo(expected.Opacity).Within(Eps), piece + " en " + t);
                    Assert.That(shift[(int)piece], Is.EqualTo(expected.ShiftDp).Within(Eps), piece + " en " + t);
                    Assert.That(motion.PieceOpacity(piece), Is.EqualTo(expected.Opacity).Within(Eps));
                }

                Assert.That(backdrop, Is.EqualTo(ImmersiveTimeline.EntryBackdrop(t, false)).Within(Eps), "el fondo en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 1120), "IsFinite en " + t);
                Assert.That(motion.IsRunning, Is.EqualTo(t < 1120), "sale al terminar, en " + t);
            }

            Assert.That(done, Is.EqualTo(1));
            Assert.That(doneAt, Is.InRange(1120L, 1137L), "la llave entra en el primer cuadro desde los 1120");
            Assert.That(respondedAt[(int)HudPiece.Controls], Is.InRange(840L, 857L), "los controles responden desde su tramo (P-15)");
            Assert.That(respondedAt[(int)HudPiece.TopBar], Is.InRange(400L, 417L));
            rig.AssertIdle("la cascada terminó");
        }

        [Test, Category("AC-WKT-13-06"), Category("AC-APP-05-08")]
        public void ReducedCascadeIsAFadeWithEverythingInPlace()
        {
            var rig = new Rig(reduceMotion: true);
            var start = rig.Now;
            var opacity = new double[7];
            var responded = 0;
            var doneAt = -1L;
            var motion = new ImmersiveCascadeMotion(rig.Driver, (p, v) => opacity[(int)p] = v.Opacity, _ => { }, _ => responded++,
                () => doneAt = HudTimelines.Ms(rig.Now - start));
            motion.Play();
            foreach (var piece in Pieces)
            {
                Assert.That(opacity[(int)piece], Is.EqualTo(1), "con reducir movimiento, a 1 desde el cuadro 0: " + piece);
                Assert.That(motion.PieceShiftDp(piece), Is.EqualTo(0));
            }

            Assert.That(motion.BackdropOpacity, Is.EqualTo(1));
            Assert.That(responded, Is.EqualTo(7), "los controles responden desde el cuadro 0");
            Assert.That(motion.IsFinite, Is.True, "el fundido de la página dura 120");
            rig.RunFor(200);
            Assert.That(doneAt, Is.InRange(120L, 137L), "la llave entra a los 120");
            rig.AssertIdle("terminó el fundido");
        }

        [Test, Category("AC-WKT-13-06")]
        public void CascadeUnarmedOrInstantEndsAtOnce()
        {
            foreach (var armed in new[] { false, true })
            {
                var rig = new Rig(armed: armed);
                var done = 0;
                var opacity = new double[7];
                var motion = new ImmersiveCascadeMotion(rig.Driver, (p, v) => opacity[(int)p] = v.Opacity, _ => { }, _ => { }, () => done++);
                motion.Play(instant: armed);
                Assert.That(done, Is.EqualTo(1), "estado final en el acto (armado: " + armed + ")");
                Assert.That(opacity, Is.All.EqualTo(1.0));
                Assert.That(motion.IsRunning, Is.False);
                rig.AssertIdle("sin animación");
            }
        }

        [Test, Category("AC-WKT-13-06")]
        public void CascadeStopFreezesWhereItIsAndFinishJumpsToTheEnd()
        {
            var rig = new Rig();
            var calls = 0;
            var done = 0;
            var motion = new ImmersiveCascadeMotion(rig.Driver, (_, _) => calls++, _ => calls++, _ => calls++, () => done++);
            var start = rig.Now;
            motion.Play();
            rig.RunFor(500);
            var t = HudTimelines.Ms(rig.Now - start);
            motion.Stop();
            rig.AssertIdle("Stop saca la cascada del MotionDriver en el acto");
            var frozen = motion.PieceOpacity(HudPiece.TopBar);
            Assert.That(frozen, Is.EqualTo(ImmersiveTimeline.Entry(HudPiece.TopBar, t, false).Opacity).Within(Eps),
                "la salida arranca desde la opacidad que tiene (AC-WKT-13-07)");
            calls = 0;
            rig.RunFor(800);
            Assert.That(calls, Is.EqualTo(0), "después de Stop no avisa nada");
            Assert.That(done, Is.EqualTo(0));

            motion.Play();
            rig.RunFor(100);
            motion.Finish();
            Assert.That(done, Is.EqualTo(1), "Finish salta al final");
            Assert.That(motion.PieceOpacity(HudPiece.Controls), Is.EqualTo(1));
            Assert.That(motion.IsInteractive(HudPiece.Controls), Is.True);
            rig.AssertIdle("Finish");
        }

        // ── Salida con la última foto (§3.3; AC-WKT-13-08) ──────────────────────────────────────────────────────────────

        [Test]
        public void ExitFollowsTheTimelineAndRevealsAtFourHundred()
        {
            var rig = new Rig();
            var start = rig.Now;
            var visual = default(ExitVisual);
            var revealAt = -1L;
            var done = 0;
            var motion = new ImmersiveExitMotion(rig.Driver, v => visual = v, () =>
            {
                Assert.That(revealAt, Is.EqualTo(-1L), "RevealStarted una sola vez");
                revealAt = HudTimelines.Ms(rig.Now - start);
            }, () => done++);
            var from = new ExitStart(0.8, 1);
            motion.Play(in from);
            for (var k = 0; k <= 45; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = ImmersiveTimeline.Exit(Math.Min(t, 640), in from, false);
                Assert.That(visual.Hud, Is.EqualTo(expected.Hud).Within(Eps), "HUD en " + t);
                Assert.That(visual.Backdrop, Is.EqualTo(expected.Backdrop).Within(Eps), "fondo en " + t);
                Assert.That(visual.Page, Is.EqualTo(expected.Page).Within(Eps), "página en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 640), "F de 0 a 640, en " + t);
            }

            Assert.That(revealAt, Is.InRange(400L, 417L), "los avisos del final a los 400 (DN4b-24)");
            Assert.That(done, Is.EqualTo(1));
            Assert.That(visual.Page, Is.EqualTo(0));
            rig.AssertIdle("la salida terminó");
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedExitIsAFadeThatRevealsAtFrameZero()
        {
            var rig = new Rig(reduceMotion: true);
            var start = rig.Now;
            var reveal = 0;
            var doneAt = -1L;
            var visual = default(ExitVisual);
            var motion = new ImmersiveExitMotion(rig.Driver, v => visual = v, () => reveal++, () => doneAt = HudTimelines.Ms(rig.Now - start));
            motion.Play(ExitStart.Full);
            Assert.That(reveal, Is.EqualTo(1), "con reducir movimiento los avisos salen en el cuadro 0");
            Assert.That(visual.Hud, Is.EqualTo(1), "la misma última foto, sin irse antes");
            rig.RunFor(200);
            Assert.That(doneAt, Is.InRange(120L, 137L));
            Assert.That(reveal, Is.EqualTo(1));
            rig.AssertIdle("terminó");
        }

        [Test]
        public void ExitFinishAnnouncesWhatWasMissingAndStopAnnouncesNothing()
        {
            var rig = new Rig();
            var reveal = 0;
            var done = 0;
            var motion = new ImmersiveExitMotion(rig.Driver, _ => { }, () => reveal++, () => done++);
            motion.Play(ExitStart.Full);
            rig.RunFor(100);
            motion.Finish();
            Assert.That(reveal, Is.EqualTo(1));
            Assert.That(done, Is.EqualTo(1));
            Assert.That(motion.Current.Page, Is.EqualTo(0));
            rig.AssertIdle("Finish");

            motion.Play(ExitStart.Full);
            rig.RunFor(100);
            motion.Stop();
            rig.RunFor(1000);
            Assert.That(reveal, Is.EqualTo(1), "Stop no avisa");
            Assert.That(done, Is.EqualTo(1));
            rig.AssertIdle("Stop");

            var unarmed = new Rig(armed: false);
            var all = 0;
            new ImmersiveExitMotion(unarmed.Driver, _ => { }, () => all++, () => all++).Play(ExitStart.Full);
            Assert.That(all, Is.EqualTo(2), "sin armar, en el acto");
            unarmed.AssertIdle("sin armar");
        }

        // ── Las píldoras (§3.4; AC-WKT-14-10, -11, -12, -16) ───────────────────────────────────────────────────────────────

        [Test]
        public void PillFillIsTheContinuousProgressAndNeverPassesTheNextTick()
        {
            var rig = new Rig();
            var (pulse, _) = Pulse(rig);
            var repaints = 0;
            var motion = new PillRowMotion(rig.Driver, pulse, () => repaints++);
            var reference = new PillProgress();
            var start = rig.Now;
            var elapsed = 0;
            motion.OnState(Row(0, 0, 10), false);
            reference.OnState(0, 0, 10, true, rig.Now, false);
            var previous = 0.0;
            for (var k = 1; k <= 200; k++)
            {
                rig.Frame();
                reference.ValueAt(rig.Now);
                var second = (int)Math.Floor(rig.Now - start + 1e-9);
                if (second != elapsed)
                {
                    elapsed = second;
                    motion.OnState(Row(0, elapsed, 10), false);
                    reference.OnState(0, elapsed, 10, true, rig.Now, false);
                }

                var fill = motion.Frame.Fill;
                Assert.That(fill, Is.EqualTo(reference.ValueAt(rig.Now)).Within(1e-12), "el relleno es PillProgress, cuadro " + k);
                Assert.That(fill, Is.LessThanOrEqualTo((elapsed + 1) / 10.0 + 1e-12), "nunca pasa del tick siguiente");
                Assert.That(fill, Is.GreaterThanOrEqualTo(previous), "nunca baja");
                previous = fill;
                Assert.That(motion.IsFinite, Is.False, "llenándose es continua (no bloquea una toma)");
                Assert.That(motion.IsRunning, Is.True);
            }

            // En pausa no avanza y, sin halo, sale del MotionDriver.
            motion.OnState(Row(0, elapsed, 10, running: false), false);
            var paused = motion.Frame.Fill;
            rig.RunFor(500);
            Assert.That(motion.Frame.Fill, Is.EqualTo(paused));
            Assert.That(motion.IsRunning, Is.False, "en pausa y sin halo: sale");
            Assert.That(repaints, Is.GreaterThan(0));
            pulse.Stop();
            rig.AssertIdle("en pausa");
        }

        [Test]
        public void PillRepaintsOnlyWhenTheFillMovedAQuarterPixel()
        {
            var rig = new Rig();
            var (pulse, _) = Pulse(rig);
            var repaints = 0;
            var motion = new PillRowMotion(rig.Driver, pulse, () => repaints++) { FillWidthPx = 10 };
            var start = rig.Now;
            var elapsed = 0;
            motion.OnState(Row(0, 0, 10), false);
            repaints = 0;
            for (var k = 1; k <= 120; k++)
            {
                rig.Frame();
                var second = (int)Math.Floor(rig.Now - start + 1e-9);
                if (second != elapsed)
                {
                    elapsed = second;
                    motion.OnState(Row(0, elapsed, 10), false);
                }
            }

            // 10 px por paso de 10 s: 1 px/s, un cuarto de píxel cada 0,25 s (PillRepaintPx): 8 en 2 s, más los dos estados.
            Assert.That(repaints, Is.InRange(6, 12), "se repinta solo cuando el borde avanzó ¼ px, no en cada cuadro");
        }

        [Test, Category("AC-WKT-14-12")]
        public void StepChangeFollowsThePillChangeTimeline()
        {
            foreach (var met in new[] { true, false })
            {
                var rig = new Rig();
                var (pulse, _) = Pulse(rig);
                var motion = new PillRowMotion(rig.Driver, pulse, () => { });
                motion.OnState(Row(0, 9, 10), false);
                rig.RunFor(500);
                var fillBefore = motion.Frame.Fill;
                var change = rig.Now;
                motion.OnState(Row(1, 0, 10, done: met ? StepPillLook.Solid : StepPillLook.Hollow), false);
                var end = met ? 340 : 440;
                var sawFlash = false;
                for (var k = 0; k <= 32; k++)
                {
                    if (k > 0)
                    {
                        rig.Frame();
                    }

                    var t = HudTimelines.Ms(rig.Now - change);
                    var frame = motion.Frame;
                    Assert.That(frame.Changing, Is.EqualTo(t < end), "el cambio dura " + end + ", en " + t);
                    Assert.That(motion.IsFinite, Is.EqualTo(t < end), "F en el cambio de paso, en " + t);
                    Assert.That(frame.Current, Is.EqualTo(1));
                    if (!frame.Changing)
                    {
                        continue;
                    }

                    Assert.That(frame.Ending, Is.EqualTo(0));
                    Assert.That(frame.EndingMet, Is.EqualTo(met));
                    Assert.That(frame.EndingFillFrom, Is.EqualTo(fillBefore).Within(1e-12), "el relleno que tenía la que termina");
                    AssertChange(frame.Change, StepBarTimeline.PillChange(t, met, false), t);
                    AssertThumb(frame.Thumb, StepBarTimeline.ThumbSwap(Math.Min(t, (long)MotionSpecs.PillThumbSwapMs), false), t);
                    Assert.That(frame.ThumbSwapping, Is.EqualTo(t < MotionSpecs.PillThumbSwapMs));
                    sawFlash |= frame.Change.Flash > 0;
                }

                Assert.That(sawFlash, Is.EqualTo(met), "el destello solo en la cumplida");
                Assert.That(motion.Frame.Ending, Is.EqualTo(-1), "al terminar no queda ningún cambio");
                pulse.Stop();
                motion.Stop();
                rig.AssertIdle("Stop");
            }
        }

        [Test, Category("AC-WKT-14-12"), Category("AC-APP-05-08")]
        public void ReducedStepChangeHasNoFlashNorStroke()
        {
            foreach (var met in new[] { true, false })
            {
                var rig = new Rig(reduceMotion: true);
                var (pulse, _) = Pulse(rig);
                var motion = new PillRowMotion(rig.Driver, pulse, () => { });
                motion.OnState(Row(0, 9, 10), false);
                rig.RunFor(200);
                var change = rig.Now;
                motion.OnState(Row(1, 0, 10, done: met ? StepPillLook.Solid : StepPillLook.Hollow), false);
                for (var k = 0; k <= 30; k++)
                {
                    if (k > 0)
                    {
                        rig.Frame();
                    }

                    var t = HudTimelines.Ms(rig.Now - change);
                    var frame = motion.Frame;
                    if (!frame.Changing)
                    {
                        continue;
                    }

                    Assert.That(frame.Change.Flash, Is.EqualTo(0), "sin destello con reducir movimiento (QA-G4bT-03), en " + t);
                    if (!met)
                    {
                        Assert.That(frame.Change.HollowStroke, Is.EqualTo(1), "sin trazo: el borde funde");
                    }

                    AssertChange(frame.Change, StepBarTimeline.PillChange(t, met, true), t);
                    AssertThumb(frame.Thumb, StepBarTimeline.ThumbSwap(Math.Min(t, (long)MotionSpecs.PillThumbSwapMs), true), t);
                }

                pulse.Stop();
                motion.Stop();
                rig.AssertIdle("Stop");
            }
        }

        [Test, Category("AC-WKT-14-12")]
        public void InstantStepChangeAndTwoChangesInARow()
        {
            var rig = new Rig();
            var (pulse, _) = Pulse(rig);
            var motion = new PillRowMotion(rig.Driver, pulse, () => { });
            motion.OnState(Row(0, 9, 10), false);
            rig.RunFor(100);
            motion.OnState(Row(1, 0, 10), true);
            Assert.That(motion.Frame.Changing, Is.False, "con Instant, todo a su estado final en el acto");
            Assert.That(motion.IsFinite, Is.False);

            // Dos cambios seguidos: lo que corría salta a su final y empieza lo nuevo.
            motion.OnState(Row(2, 0, 10, done: StepPillLook.Hollow), false);
            rig.RunFor(100);
            var second = rig.Now;
            var looks = new[] { StepPillLook.Solid, StepPillLook.Hollow, StepPillLook.Solid, StepPillLook.Current };
            motion.OnState(new HudPillRow(Types4, looks, 3, 0, 10, true, false), false);
            Assert.That(motion.Frame.Ending, Is.EqualTo(2));
            Assert.That(motion.Frame.EndingMet, Is.True);
            rig.Frame();
            AssertChange(motion.Frame.Change, StepBarTimeline.PillChange(HudTimelines.Ms(rig.Now - second), true, false), 0);

            // Sin armar: tampoco anima.
            var unarmed = new Rig(armed: false);
            var (p2, _) = Pulse(unarmed);
            var m2 = new PillRowMotion(unarmed.Driver, p2, () => { });
            m2.OnState(Row(0, 9, 10), false);
            m2.OnState(Row(1, 0, 10), false);
            Assert.That(m2.Frame.Changing, Is.False, "sin armar, en el acto");
            pulse.Stop();
            motion.Stop();
            p2.Stop();
            m2.Stop();
            rig.AssertIdle("Stop");
            unarmed.AssertIdle("Stop");
        }

        [Test]
        public void HaloRampsWithEndingSoonPulsesAndGoesOutWithTheEndingPill()
        {
            var rig = new Rig();
            var (pulse, _) = Pulse(rig);
            var motion = new PillRowMotion(rig.Driver, pulse, () => { });
            motion.OnState(Row(0, 4, 10), false);
            Assert.That(motion.Frame.HaloAlpha, Is.EqualTo(0), "sin halo antes de los últimos 5 s");
            rig.RunFor(1000);
            var rampStart = rig.Now;
            pulse.Update(false, true, Color.red, false);
            motion.OnState(Row(0, 5, 10, endingSoon: true), false);
            for (var k = 0; k <= 60; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - rampStart);
                var ramp = HudTimelines.Reached(t, (long)MotionSpecs.PillGlowRampMs) ? 1 : StepBarTimeline.HaloRamp(t, true);
                var expected = StepBarTimeline.Halo(ramp, pulse.PulseAt(rig.Now), false);
                Assert.That(motion.Frame.HaloAlpha, Is.EqualTo(expected).Within(Eps), "α = a·(0,30 + 0,40·p), en " + t);
                Assert.That(motion.IsFinite, Is.False, "el halo es continuo (no bloquea), en " + t);
                Assert.That(motion.IsRunning, Is.True);
            }

            // Cambio de paso: la que termina apaga su halo con la rampa del cambio; la nueva no tiene.
            var change = rig.Now;
            pulse.Update(true, false, Color.red, false);
            motion.OnState(Row(1, 0, 10), false);
            for (var k = 0; k <= 12; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - change);
                var frame = motion.Frame;
                Assert.That(frame.HaloAlpha, Is.EqualTo(0), "la nueva sin halo, en " + t);
                var ending = StepBarTimeline.Halo(StepBarTimeline.PillChange(t, true, false).EndingHaloRamp, pulse.PulseAt(rig.Now), false);
                Assert.That(frame.EndingHaloAlpha, Is.EqualTo(ending).Within(Eps), "el halo de la que termina, en " + t);
            }

            pulse.Stop();
            motion.Stop();
            rig.AssertIdle("Stop: ni el halo queda latiendo");
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedHaloIsFixedAtSeventyPercent()
        {
            var rig = new Rig(reduceMotion: true);
            var (pulse, _) = Pulse(rig);
            var motion = new PillRowMotion(rig.Driver, pulse, () => { });
            motion.OnState(Row(0, 4, 10), false);
            rig.RunFor(1000);
            pulse.Update(false, true, Color.red, false);
            motion.OnState(Row(0, 5, 10, endingSoon: true), false);
            rig.RunFor(400);
            Assert.That(motion.Frame.HaloAlpha, Is.EqualTo(MotionSpecs.PillGlowMax).Within(Eps), "fijo en 0,70 con la rampa");
            rig.RunFor(400);
            Assert.That(motion.Frame.HaloAlpha, Is.EqualTo(MotionSpecs.PillGlowMax).Within(Eps));
            pulse.Stop();
            motion.Stop();
            rig.AssertIdle("Stop");
        }

        // ── La fila de información (§2.15; AC-WKT-14-15) ──────────────────────────────────────────────────────────────────

        [Test]
        public void InfoRowModeChangeAndTargetOut()
        {
            var rig = new Rig();
            var gaps = default(InfoRowVisual);
            var target = -1.0;
            var gone = 0;
            var motion = new InfoRowMotion(rig.Driver, v => gaps = v, o => target = o, () => gone++);
            var normal = new InfoRowPlan(InfoRowMode.Normal, 1, 1);
            var compact = new InfoRowPlan(InfoRowMode.Compact2, 2, 0);
            var start = rig.Now;
            motion.ChangeMode(in normal, in compact, false);
            for (var k = 0; k <= 18; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = InfoRowTimeline.ModeChange(Math.Min(t, 240), in normal, in compact, false);
                Assert.That(gaps.CountdownGap, Is.EqualTo(expected.CountdownGap).Within(Eps), "en " + t);
                Assert.That(gaps.ClockGap, Is.EqualTo(expected.ClockGap).Within(Eps));
                Assert.That(gaps.UpNextPadH, Is.EqualTo(expected.UpNextPadH).Within(Eps));
                Assert.That(motion.IsFinite, Is.EqualTo(t < 240), "F en el cambio de modo, en " + t);
            }

            Assert.That(gaps.UpNextPadH, Is.EqualTo(InfoRowFit.UpNextCompactPadH));
            rig.AssertIdle("terminó el cambio de modo");

            start = rig.Now;
            motion.HideTarget(false);
            for (var k = 0; k <= 10; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(target, Is.EqualTo(InfoRowTimeline.TargetOut(Math.Min(t, 120), false)).Within(Eps), "la pastilla funde, en " + t);
                Assert.That(gone, Is.EqualTo(t >= 120 ? 1 : 0), "sale del árbol al terminar el fundido, en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 120));
            }

            Assert.That(motion.TargetHidden, Is.True);
            motion.HideTarget(false);
            Assert.That(gone, Is.EqualTo(1), "una vez");
            motion.ShowTarget();
            Assert.That(target, Is.EqualTo(1), "un paso nuevo con su pastilla");
            Assert.That(motion.TargetHidden, Is.False);
            rig.AssertIdle("terminó el fundido");
        }

        [Test, Category("AC-APP-05-08")]
        public void InfoRowReducedOrInstantChangesTheGapsAtOnce()
        {
            var normal = new InfoRowPlan(InfoRowMode.Normal, 1, 1);
            var compact = new InfoRowPlan(InfoRowMode.Compact1, 1, 1);
            foreach (var reduced in new[] { true, false })
            {
                var rig = new Rig(reduceMotion: reduced);
                var gaps = default(InfoRowVisual);
                var target = -1.0;
                var gone = 0;
                var motion = new InfoRowMotion(rig.Driver, v => gaps = v, o => target = o, () => gone++);
                motion.ChangeMode(in normal, in compact, instant: !reduced);
                Assert.That(gaps.CountdownGap, Is.EqualTo(InfoRowFit.CompactGap), "en el acto");
                Assert.That(motion.IsRunning, Is.False);
                if (reduced)
                {
                    // Con reducir movimiento la pastilla se va con su fundido de 120 (no en el acto).
                    var start = rig.Now;
                    motion.HideTarget(false);
                    Assert.That(motion.IsFinite, Is.True);
                    rig.Frame();
                    Assert.That(target, Is.EqualTo(InfoRowTimeline.TargetOut(HudTimelines.Ms(rig.Now - start), true)).Within(Eps));
                    rig.RunFor(200);
                }
                else
                {
                    motion.HideTarget(true);
                }

                Assert.That(target, Is.EqualTo(0));
                Assert.That(gone, Is.EqualTo(1));
                rig.AssertIdle("reducido: " + reduced);
            }
        }

        // ── El sello (§3.6; AC-WKT-09-08) ───────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void SealEntersHoldsAndLeavesWithinTwoSeconds()
        {
            var rig = new Rig();
            var visual = default(SealVisual);
            HudSeal? shown = null;
            var contentCalls = 0;
            var motion = new SealMotion(rig.Driver, v => visual = v, s =>
            {
                shown = s;
                contentCalls++;
            });
            rig.RunFor(1000);
            var seal = Seal(0, StepResult.Met, rig.Now);
            motion.Show(seal, false);
            Assert.That(shown, Is.SameAs(seal));
            for (var k = 0; k <= 130; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - seal.At);
                AssertSeal(visual, SealTimeline.At(Math.Min(t, 2000), false), t);
                var finite = t < 300 || (t >= 1800 && t < 2000);
                Assert.That(motion.IsFinite, Is.EqualTo(finite), "F en la entrada y en la salida; C en el sostén, en " + t);
                Assert.That(motion.IsRunning, Is.EqualTo(t < 2000), "sale a los 2 s, en " + t);
                Assert.That(motion.IsShowing, Is.EqualTo(t < 2000));
            }

            Assert.That(visual.BarHeight, Is.EqualTo(SealTimeline.BarHeightClosed));
            Assert.That(shown, Is.Null, "el contenido se va al terminar");
            Assert.That(contentCalls, Is.EqualTo(2));
            rig.AssertIdle("terminó el sello");
        }

        [Test]
        public void NewSealReplacesTheVisibleOne()
        {
            var rig = new Rig();
            var visual = default(SealVisual);
            HudSeal? shown = null;
            var motion = new SealMotion(rig.Driver, v => visual = v, s => shown = s);
            var first = Seal(0, StepResult.Met, rig.Now);
            motion.Show(first, false);
            rig.RunFor(1000);
            var second = Seal(1, StepResult.Missed, rig.Now);
            motion.Show(second, false);
            for (var k = 0; k <= 30; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - second.At);
                var expected = t < 320 ? SealTimeline.Swap(t, false) : SealTimeline.At(t, false);
                AssertSeal(visual, expected, t);
                Assert.That(visual.BarHeight, Is.EqualTo(SealTimeline.BarHeightOpen), "la barra sigue abierta");
                Assert.That(shown, Is.SameAs(t < 80 ? first : second), "el texto viejo hasta los 80, en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 320), "F en el reemplazo, en " + t);
            }

            Assert.That(motion.Seal, Is.SameAs(second));
            rig.RunFor(2000);
            Assert.That(motion.IsShowing, Is.False, "los 2 s volvieron a empezar con el nuevo y terminaron");
            Assert.That(shown, Is.Null);
            rig.AssertIdle("terminó");
        }

        [Test]
        public void SealAfterTheBackgroundShowsWhatIsLeftWithoutEntry()
        {
            var rig = new Rig();
            rig.RunFor(5000);
            var visual = default(SealVisual);
            var applied = 0;
            var motion = new SealMotion(rig.Driver, v =>
            {
                visual = v;
                applied++;
            });

            // 2 s ya pasados: no se muestra.
            motion.Show(Seal(0, StepResult.Met, rig.Now - 2.5), true);
            Assert.That(motion.IsShowing, Is.False);
            Assert.That(motion.IsRunning, Is.False);
            Assert.That(visual.BarHeight, Is.Not.EqualTo(SealTimeline.BarHeightOpen));

            // A los 100 ms de su instante: en el acto, sin entrada (el sostén).
            var early = Seal(1, StepResult.Met, rig.Now - 0.1);
            motion.Show(early, true);
            AssertSeal(visual, SealTimeline.At(300, false), 100);
            Assert.That(motion.IsFinite, Is.False, "sin entrada: es el sostén");
            Assert.That(motion.IsRunning, Is.True, "sigue su reloj hasta los 2 s");

            // Con 1,9 s: lo que le queda de la salida.
            var late = Seal(2, StepResult.Missed, rig.Now - 1.9);
            motion.Show(late, true);
            AssertSeal(visual, SealTimeline.At(1900, false), 1900);
            Assert.That(motion.IsFinite, Is.True);
            rig.RunFor(200);
            Assert.That(motion.IsShowing, Is.False);
            rig.AssertIdle("terminó");
            Assert.That(applied, Is.GreaterThan(0));
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedSealAndClear()
        {
            var rig = new Rig(reduceMotion: true);
            var visual = default(SealVisual);
            HudSeal? shown = null;
            var motion = new SealMotion(rig.Driver, v => visual = v, s => shown = s);
            var seal = Seal(0, StepResult.Met, rig.Now);
            motion.Show(seal, false);
            for (var k = 0; k <= 20; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - seal.At);
                AssertSeal(visual, SealTimeline.At(t, true), t);
                Assert.That(visual.BarHeight, Is.EqualTo(SealTimeline.BarHeightOpen), "el alto en el acto");
                Assert.That(visual.IconScale, Is.EqualTo(1), "el ícono no escala");
            }

            motion.Clear(true);
            Assert.That(visual.BarHeight, Is.EqualTo(SealTimeline.BarHeightClosed));
            Assert.That(shown, Is.Null);
            Assert.That(motion.IsShowing, Is.False);
            rig.AssertIdle("Clear en el acto");

            var normal = new Rig();
            var v2 = default(SealVisual);
            var m2 = new SealMotion(normal.Driver, v => v2 = v);
            m2.Show(Seal(0, StepResult.Met, normal.Now), false);
            normal.RunFor(600);
            m2.Clear(false);
            Assert.That(m2.IsFinite, Is.True, "Clear sin Instant: la salida");
            normal.RunFor(250);
            Assert.That(m2.IsShowing, Is.False);
            Assert.That(v2.BarHeight, Is.EqualTo(SealTimeline.BarHeightClosed));
            normal.AssertIdle("Clear");
        }

        // ── La cuenta grande y PAUSA, un disco por vez (§3.5; AC-WKT-14-18, AC-WKT-13-12) ─────────────────────────────────

        [Test]
        public void CountEntersRingsBetweenTicksAndLeavesWithTheRingEmpty()
        {
            var rig = new Rig();
            var progress = new PillProgress();
            var visual = default(DiscVisual);
            var motion = new CenterDiscMotion(rig.Driver, progress, v => visual = v);
            motion.SetCount(null, false); // el primer estado del HUD (sin cuenta), en el acto
            var tick = rig.Now;
            progress.OnState(0, 7, 10, true, tick, false);
            motion.SetCount(3, false);
            var elapsed = 7;
            for (var k = 0; k <= 170; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var second = 7 + (int)Math.Floor(rig.Now - tick + 1e-9);
                if (second != elapsed && second < 10)
                {
                    elapsed = second;
                    progress.OnState(0, elapsed, 10, true, rig.Now, false);
                    motion.SetCount(10 - elapsed, false);
                }

                var t = HudTimelines.Ms(rig.Now - tick);
                var expected = CenterDiscTimeline.CountIn(Math.Min(t, 200), false);
                Assert.That(visual.CountOpacity, Is.EqualTo(expected.CountOpacity).Within(Eps), "en " + t);
                Assert.That(visual.CountScale, Is.EqualTo(expected.CountScale).Within(Eps));
                Assert.That(visual.PausedOpacity, Is.EqualTo(0));
                Assert.That(motion.IsFinite, Is.EqualTo(t < 200), "F en la entrada; C entre ticks, en " + t);
                Assert.That(motion.IsRunning, Is.True, "entre ticks sigue (el aro)");
                Assert.That(motion.RingFraction, Is.EqualTo(CenterDiscTimeline.RingFraction(progress.RemainingAt(rig.Now))).Within(Eps));
                if (k == 0)
                {
                    Assert.That(motion.RingFraction, Is.EqualTo(1).Within(1e-9), "lleno en el tick del 3");
                }
            }

            // El cambio de paso: la cuenta "se suelta" con el aro ya vacío.
            rig.RunToSecondsAfter(tick, 3.0);
            var change = rig.Now;
            progress.OnState(1, 0, 10, true, change, false);
            motion.SetCount(null, false);
            for (var k = 0; k <= 15; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - change);
                var expected = CenterDiscTimeline.CountOut(Math.Min(t, 200), false);
                Assert.That(visual.CountOpacity, Is.EqualTo(expected.CountOpacity).Within(Eps), "sale, en " + t);
                Assert.That(visual.CountScale, Is.EqualTo(expected.CountScale).Within(Eps));
                Assert.That(motion.RingFraction, Is.EqualTo(0), "el aro ya vacío");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 200));
            }

            Assert.That(motion.IsRunning, Is.False, "sin disco, sale");
            Assert.That(motion.LastCount, Is.EqualTo(1), "la cifra que se va es la última");
            rig.AssertIdle("sin disco");
        }

        [Test]
        public void PausingWithTheCountVisibleSwapsTheDiscs()
        {
            var a = PausedDuringCount(countFirst: true);
            var b = PausedDuringCount(countFirst: false);
            Assert.That(a.Length, Is.EqualTo(b.Length));
            for (var i = 0; i < a.Length; i++)
            {
                Assert.That(a[i], Is.EqualTo(b[i]).Within(Eps), "el orden de SetCount y SetPaused no importa, muestra " + i);
            }
        }

        [Test]
        public void PauseInTheTickOfThreeShowsOnlyPausedFromOneTwenty()
        {
            // CAP-27 (DT4b-18): la cuenta entra y sale en el mismo instante; PAUSA entra a los 120 y a los 600 está quieto.
            var rig = new Rig();
            var progress = new PillProgress();
            progress.OnState(0, 7, 10, true, rig.Now, false);
            var visual = default(DiscVisual);
            var motion = new CenterDiscMotion(rig.Driver, progress, v => visual = v);
            motion.SetCount(null, false);
            var start = rig.Now;
            motion.SetCount(3, false);
            progress.OnState(0, 7, 10, false, rig.Now, false);
            motion.SetPaused(true, false);
            motion.SetCount(null, false);
            for (var k = 0; k <= 30; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.CountOpacity, Is.EqualTo(0).Within(Eps), "la cuenta nunca llega a verse, en " + t);
                if (t < 120)
                {
                    Assert.That(visual.PausedOpacity, Is.EqualTo(0), "PAUSA espera a los 120, en " + t);
                }
            }

            rig.RunToSecondsAfter(start, 0.6);
            Assert.That(visual.PausedOpacity, Is.EqualTo(1), "a los 47,6 s PAUSA está quieto");
            Assert.That(motion.IsRunning, Is.False, "PAUSA quieto sale del MotionDriver");
            Assert.That(motion.Shown, Is.EqualTo(CenterDisc.Paused));
            rig.AssertIdle("PAUSA quieto");
        }

        [Test]
        public void ResumeFadesPausedAndTheCountComesBackAfterOneTwenty()
        {
            var rig = new Rig();
            var progress = new PillProgress();
            progress.OnState(0, 5, 10, false, rig.Now, false);
            var visual = default(DiscVisual);
            var motion = new CenterDiscMotion(rig.Driver, progress, v => visual = v);
            motion.SetPaused(true, true);
            Assert.That(visual.PausedOpacity, Is.EqualTo(1), "en el acto");
            Assert.That(motion.IsRunning, Is.False);

            // Reanudar sin cuenta: PAUSA sale en 240.
            var start = rig.Now;
            motion.SetPaused(false, false);
            for (var k = 0; k <= 16; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = CenterDiscTimeline.PausedOut(Math.Min(t, 240), false);
                Assert.That(visual.PausedOpacity, Is.EqualTo(expected.PausedOpacity).Within(Eps), "en " + t);
                Assert.That(visual.PausedScale, Is.EqualTo(expected.PausedScale).Within(Eps));
                Assert.That(motion.IsFinite, Is.EqualTo(t < 240));
            }

            rig.AssertIdle("sin disco");

            // Reanudar cuando la cuenta vuelve: un disco por vez.
            motion.SetPaused(true, true);
            progress.OnState(0, 8, 10, true, rig.Now, false);
            start = rig.Now;
            motion.SetPaused(false, false);
            motion.SetCount(2, false);
            for (var k = 0; k <= 22; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = CenterDiscTimeline.Swap(Math.Min(t, 320), false, false);
                Assert.That(visual.PausedOpacity, Is.EqualTo(expected.PausedOpacity).Within(Eps), "PAUSA, en " + t);
                Assert.That(visual.CountOpacity, Is.EqualTo(expected.CountOpacity).Within(Eps), "la cuenta, en " + t);
                Assert.That(visual.CountScale, Is.EqualTo(expected.CountScale).Within(Eps));
                Assert.That(visual.PausedOpacity > 1e-6 && visual.CountOpacity > 1e-6, Is.False, "nunca dos discos a la vista");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 320));
            }

            Assert.That(motion.IsRunning, Is.True, "con la cuenta a la vista sigue (el aro)");
            motion.Stop();
            rig.AssertIdle("Stop");
        }

        [Test, Category("AC-APP-05-08")]
        public void ReducedDiscsOnlyFadeAndInstantShowsTheRightOne()
        {
            var rig = new Rig(reduceMotion: true);
            var progress = new PillProgress();
            progress.OnState(0, 7, 10, true, rig.Now, false);
            var visual = default(DiscVisual);
            var motion = new CenterDiscMotion(rig.Driver, progress, v => visual = v);
            motion.SetCount(null, false);
            var start = rig.Now;
            motion.SetCount(3, false);
            for (var k = 0; k <= 12; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(visual.CountOpacity, Is.EqualTo(CenterDiscTimeline.CountIn(Math.Min(t, 160), true).CountOpacity).Within(Eps));
                Assert.That(visual.CountScale, Is.EqualTo(1), "sin escala");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 160), "entra en 160, en " + t);
            }

            motion.SetPaused(true, false);
            rig.RunFor(400);
            Assert.That(visual.PausedOpacity, Is.EqualTo(1));
            Assert.That(visual.PausedScale, Is.EqualTo(1));

            var other = new Rig();
            var p2 = new PillProgress();
            p2.OnState(0, 7, 10, true, other.Now, false);
            var v2 = default(DiscVisual);
            var m2 = new CenterDiscMotion(other.Driver, p2, v => v2 = v);
            m2.SetCount(3, true);
            Assert.That(v2.CountOpacity, Is.EqualTo(1), "al volver del segundo plano, el disco que corresponde sin entrada");
            Assert.That(m2.IsFinite, Is.False);
            Assert.That(m2.IsRunning, Is.True, "el aro sigue");
            Assert.That(m2.RingFraction, Is.EqualTo(1).Within(1e-9));
            m2.Set(null, true, true);
            Assert.That(v2.PausedOpacity, Is.EqualTo(1));
            Assert.That(v2.CountOpacity, Is.EqualTo(0));
            Assert.That(m2.IsRunning, Is.False);
            rig.AssertIdle("reducido");
            other.AssertIdle("instant");
        }

        // ── El velo de pausa (§3.7; AC-WKT-13-12) ────────────────────────────────────────────────────────────────────────

        [Test]
        public void PauseVeilDarkensInThreeHundredAndClearsInTwoForty()
        {
            var rig = new Rig();
            var alpha = -1.0;
            var motion = new PauseVeilMotion(rig.Driver, a => alpha = a);
            motion.SetPaused(false, false);
            Assert.That(alpha, Is.EqualTo(0), "el primer estado, en el acto");
            var start = rig.Now;
            motion.SetPaused(true, false);
            for (var k = 0; k <= 20; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(alpha, Is.EqualTo(PauseTimeline.Veil(Math.Min(t, 300), true, 0, false)).Within(Eps), "en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 300));
            }

            Assert.That(alpha, Is.EqualTo(MotionSpecs.PauseVeilAlpha));
            rig.AssertIdle("velo quieto");

            // Reanudar a mitad de otro reanudar: sigue desde el valor que tiene.
            start = rig.Now;
            motion.SetPaused(false, false);
            rig.RunFor(100);
            var mid = alpha;
            Assert.That(mid, Is.EqualTo(PauseTimeline.Veil(HudTimelines.Ms(rig.Now - start), false, MotionSpecs.PauseVeilAlpha, false)).Within(Eps));
            start = rig.Now;
            motion.SetPaused(true, false);
            rig.Frame();
            Assert.That(alpha, Is.EqualTo(PauseTimeline.Veil(HudTimelines.Ms(rig.Now - start), true, mid, false)).Within(Eps), "sin saltos");
            motion.SetPaused(true, true);
            Assert.That(alpha, Is.EqualTo(MotionSpecs.PauseVeilAlpha), "Instant: en el acto");
            rig.AssertIdle("Instant");
        }

        // ── El chip y el punto en vivo (§3.7; AC-WKT-16-03, -05) ─────────────────────────────────────────────────────────

        [Test]
        public void PhaseChipCrossesColorsAndSwapsTheLabel()
        {
            foreach (var reduced in new[] { false, true })
            {
                var rig = new Rig(reduceMotion: reduced);
                var visual = default(ChipVisual);
                var motion = new PhaseChipMotion(rig.Driver, v => visual = v);
                var start = rig.Now;
                motion.Change(false);
                for (var k = 0; k <= 14; k++)
                {
                    if (k > 0)
                    {
                        rig.Frame();
                    }

                    var t = HudTimelines.Ms(rig.Now - start);
                    var expected = PhaseChipTimeline.At(Math.Min(t, 200));
                    Assert.That(visual.ColorMix, Is.EqualTo(expected.ColorMix).Within(Eps), "en " + t);
                    Assert.That(visual.OldLabelOpacity, Is.EqualTo(expected.OldLabelOpacity).Within(Eps));
                    Assert.That(visual.NewLabelOpacity, Is.EqualTo(expected.NewLabelOpacity).Within(Eps));
                    Assert.That(motion.IsFinite, Is.EqualTo(t < 200), "F en 0-200, en " + t);
                }

                rig.AssertIdle("terminó el cambio");
                motion.Change(true);
                Assert.That(visual.ColorMix, Is.EqualTo(1));
                Assert.That(visual.NewLabelOpacity, Is.EqualTo(1));
                rig.AssertIdle("Instant");
            }
        }

        [Test]
        public void LiveDotStartsAtOneAndPulsesWhileActive()
        {
            var rig = new Rig();
            var opacity = -1.0;
            var motion = new LiveDotMotion(rig.Driver, o => opacity = o);
            var start = rig.Now;
            motion.SetActive(true);
            Assert.That(opacity, Is.EqualTo(1), "empieza en 1");
            Assert.That(motion.IsFinite, Is.False, "el punto es continuo");
            Assert.That(motion.IsRunning, Is.True);
            var expected = new[] { 1, 0.906, 0.675, 0.444, 0.350, 0.444, 0.675, 0.906, 1 };
            for (var i = 1; i < expected.Length; i++)
            {
                rig.RunToSecondsAfter(start, i * 0.2);
                Assert.That(opacity, Is.EqualTo(expected[i]).Within(0.002), "a los " + (i * 200) + " ms (04-design-notes §4.9)");
                Assert.That(opacity, Is.EqualTo(LiveDot.At(HudTimelines.Ms(rig.Now - start), false)).Within(Eps));
            }

            rig.RunToSecondsAfter(start, 42.2);
            Assert.That(opacity, Is.EqualTo(0.444).Within(0.002), "CAP-13: 0,444 a los 42,2 s");
            motion.SetActive(false);
            rig.AssertIdle("sin EN MARCHA");
            motion.SetActive(true);
            Assert.That(opacity, Is.EqualTo(1), "al volver a EN MARCHA empieza otra vez en 1");
            rig.RunFor(100);
            Assert.That(opacity, Is.LessThan(1), "bajando");
            motion.Stop();
            rig.AssertIdle("Stop");

            var reduced = new Rig(reduceMotion: true);
            var r = -1.0;
            var still = new LiveDotMotion(reduced.Driver, o => r = o);
            still.SetActive(true);
            reduced.RunFor(500);
            Assert.That(r, Is.EqualTo(1), "con reducir movimiento, quieto en 1");
            reduced.AssertIdle("fuera del MotionDriver");

            // QA-V4b-MOT-03: la "fase" del punto dura toda la sesión en marcha, así que obedece a reducir movimiento en el cuadro
            // siguiente, como el corazón, el pulso de Próximo y el halo (y no recién en la próxima pausa).
            var live = new Rig();
            var l = -1.0;
            var dot = new LiveDotMotion(live.Driver, o => l = o);
            dot.SetActive(true);
            live.RunFor(500);
            Assert.That(l, Is.LessThan(1), "late");
            live.Settings.ReduceMotion = true;
            live.Frame();
            Assert.That(l, Is.EqualTo(1), "se prende reducir movimiento a mitad de sesión: quieto en 1 en el cuadro siguiente");
            Assert.That(dot.IsRunning, Is.False);
            live.AssertIdle("fuera del MotionDriver");
            dot.SetActive(true);
            live.RunFor(500);
            Assert.That(l, Is.EqualTo(1), "el estado siguiente (sigue EN MARCHA) no lo vuelve a hacer latir");
            live.Settings.ReduceMotion = false;
            dot.SetActive(true);
            Assert.That(dot.IsRunning, Is.True, "se apaga reducir movimiento: el estado siguiente lo vuelve a hacer latir");
            Assert.That(l, Is.EqualTo(1), "desde 1");
            live.RunFor(100);
            Assert.That(l, Is.LessThan(1), "bajando");
            dot.Stop();
            live.AssertIdle("Stop");
        }

        // ── kcal que se desliza (§3.8; AC-WKT-17-11) ─────────────────────────────────────────────────────────────────────

        [Test]
        public void KcalSlidesWhenBpmAppearsAndWhenItGoes()
        {
            var rig = new Rig();
            var visual = default(KcalVisual);
            var gone = 0;
            var goneAt = -1L;
            var start = rig.Now;
            var motion = new KcalSlideMotion(rig.Driver, v => visual = v, () =>
            {
                gone++;
                goneAt = HudTimelines.Ms(rig.Now - start);
            });
            motion.Appear(75.6, false);
            for (var k = 0; k <= 24; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = KcalSlideTimeline.At(Math.Min(t, 360), true, false);
                Assert.That(visual.KcalShift, Is.EqualTo(expected.KcalShift).Within(Eps), "kcal, en " + t);
                Assert.That(visual.BpmOpacity, Is.EqualTo(expected.BpmOpacity).Within(Eps), "BPM, en " + t);
                Assert.That(motion.ShiftDp, Is.EqualTo(expected.KcalShift * 75.6).Within(1e-6));
                Assert.That(motion.IsFinite, Is.EqualTo(t < 360));
            }

            rig.AssertIdle("terminó");
            start = rig.Now;
            motion.Disappear(false, -75.6);
            for (var k = 0; k <= 24; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = KcalSlideTimeline.At(Math.Min(t, 360), false, false);
                Assert.That(visual.BpmOpacity, Is.EqualTo(expected.BpmOpacity).Within(Eps), "BPM funde en su lugar, en " + t);
                Assert.That(motion.LayoutChanged, Is.EqualTo(t >= 120), "BPM sale del árbol visible a los 120, en " + t);
                Assert.That(motion.ShiftDp, Is.EqualTo(t >= 120 ? expected.KcalShift * -75.6 : 0).Within(1e-6), "kcal se mueve en 120-360");
                Assert.That(motion.IsFinite, Is.EqualTo(t < 360));
            }

            Assert.That(gone, Is.EqualTo(1));
            Assert.That(goneAt, Is.InRange(120L, 137L));
            rig.AssertIdle("terminó");
        }

        // QA-V4b-07 (VID-07): BPM ya estaba al abrir el HUD y kcal todavía no tenía layout (su alto es NaN): la distancia llega NaN.
        // El translate de kcal nunca es NaN (antes quedaba kcalShift = NaN hasta el próximo Disappear, y el translate en NaN).
        [Test, Category("AC-WKT-17-11")]
        public void KcalShiftIsNeverNaN()
        {
            var rig = new Rig();
            var motion = new KcalSlideMotion(rig.Driver, _ => { }, () => { });
            motion.Appear(double.NaN, false);
            for (var k = 0; k < 30; k++)
            {
                Assert.That(double.IsNaN(motion.ShiftDp), Is.False, "cuadro " + k + " de la entrada sin layout");
                Assert.That(motion.ShiftDp, Is.EqualTo(0).Within(1e-9), "sin distancia, en su lugar");
                rig.Frame();
            }

            motion.Appear(double.NaN, true);
            Assert.That(motion.ShiftDp, Is.EqualTo(0), "en el acto, también");
            motion.Disappear(false, double.PositiveInfinity);
            for (var k = 0; k < 30; k++)
            {
                rig.Frame();
                Assert.That(double.IsNaN(motion.ShiftDp) || double.IsInfinity(motion.ShiftDp), Is.False, "cuadro " + k + " de la salida");
            }

            motion.Stop();
            rig.AssertIdle("Stop");
        }

        [Test, Category("AC-APP-05-08")]
        public void KcalReducedInstantAndFinish()
        {
            var rig = new Rig(reduceMotion: true);
            var visual = default(KcalVisual);
            var gone = 0;
            var motion = new KcalSlideMotion(rig.Driver, v => visual = v, () => gone++);
            var start = rig.Now;
            motion.Appear(75.6, false);
            Assert.That(motion.ShiftDp, Is.EqualTo(0), "kcal en el acto");
            rig.Frame();
            Assert.That(visual.BpmOpacity, Is.EqualTo(KcalSlideTimeline.At(HudTimelines.Ms(rig.Now - start), true, true).BpmOpacity).Within(Eps));
            rig.RunFor(300);
            Assert.That(visual.BpmOpacity, Is.EqualTo(1), "BPM funde en 240");
            rig.AssertIdle("terminó");
            motion.Disappear(false, -75.6);
            rig.RunFor(150);
            Assert.That(gone, Is.EqualTo(1), "BPM se va en 120");
            rig.AssertIdle("terminó");

            var normal = new Rig();
            var g2 = 0;
            var v2 = default(KcalVisual);
            var m2 = new KcalSlideMotion(normal.Driver, v => v2 = v, () => g2++);
            m2.Disappear(true, -75.6);
            Assert.That(g2, Is.EqualTo(1), "Instant: BPM se va en el acto");
            Assert.That(v2.BpmOpacity, Is.EqualTo(0));
            m2.Appear(75.6, true);
            Assert.That(v2.BpmOpacity, Is.EqualTo(1));
            Assert.That(m2.ShiftDp, Is.EqualTo(0));
            m2.Disappear(false, -75.6);
            normal.RunFor(50);
            m2.Finish();
            Assert.That(g2, Is.EqualTo(2), "Finish avisa lo que faltaba");
            normal.AssertIdle("Finish");
            m2.Disappear(false, -75.6);
            normal.RunFor(50);
            m2.Stop();
            normal.RunFor(500);
            Assert.That(g2, Is.EqualTo(2), "Stop no avisa");
            normal.AssertIdle("Stop");
        }

        // ── El diálogo de completado (§3.9; AC-WKT-21-05) ─────────────────────────────────────────────────────────────────

        [Test, Category("AC-WKT-21-05")]
        public void CompletionDrawsTheCheckAndCountsToTheExactTotals()
        {
            var rig = new Rig();
            var visual = default(CompletionVisual);
            var motion = new CompletionMotion(rig.Driver, v => visual = v);
            var start = rig.Now;
            motion.Play(false);
            for (var k = 0; k <= 75; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = CompletionTimeline.At(Math.Min(t, 1160), false);
                Assert.That(visual.CheckScale, Is.EqualTo(expected.CheckScale).Within(Eps), "en " + t);
                Assert.That(visual.CheckStroke, Is.EqualTo(expected.CheckStroke).Within(Eps));
                Assert.That(visual.TimeFraction, Is.EqualTo(expected.TimeFraction).Within(Eps));
                Assert.That(visual.CaloriesFraction, Is.EqualTo(expected.CaloriesFraction).Within(Eps));
                Assert.That(motion.ValueOf(1120, 1), Is.EqualTo(CountUp.ValueAt(1120, Math.Min(t, 1160), CountUp.StartMs(1))), "la distancia, en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 1160), "F de 0 a 1160, en " + t);
            }

            Assert.That(motion.ValueOf(30, 0), Is.EqualTo(30), "siempre el valor exacto");
            Assert.That(motion.ValueOf(1120, 1), Is.EqualTo(1120));
            Assert.That(motion.ValueOf(43, 2), Is.EqualTo(43));
            rig.AssertIdle("el conteo terminó");

            // La tabla de 04-design-notes §4.12 (Corta), en ms del diálogo (los del fin − 300).
            var table = new Rig();
            var m2 = new CompletionMotion(table.Driver, _ => { });
            var s2 = table.Now;
            m2.Play(false);
            table.RunToSecondsAfter(s2, 0.300);
            Assert.That(m2.ValueOf(30, 0), Is.InRange(8, 10), "00:09 a los 600 del fin");
            table.RunToSecondsAfter(s2, 0.600);
            Assert.That(m2.ValueOf(30, 0), Is.InRange(26, 28), "00:27 a los 900 del fin");
            Assert.That(m2.ValueOf(43, 2), Is.InRange(19, 21), "20 a los 900 del fin");
            m2.Stop();
            table.RunFor(1000);
            Assert.That(m2.IsRunning, Is.False, "Terminar durante el conteo: nada sigue contando (QA-G4bT-08 (a))");
            table.AssertIdle("Stop");
        }

        [Test, Category("AC-WKT-21-05"), Category("AC-APP-05-08")]
        public void CompletionReducedOrInstantShowsTheFinalTotalsAtOnce()
        {
            foreach (var reduced in new[] { true, false })
            {
                var rig = new Rig(reduceMotion: reduced);
                var visual = default(CompletionVisual);
                var motion = new CompletionMotion(rig.Driver, v => visual = v);
                motion.Play(instant: !reduced);
                Assert.That(visual.CheckScale, Is.EqualTo(1));
                Assert.That(visual.CheckStroke, Is.EqualTo(1), "el check entero");
                Assert.That(motion.ValueOf(1120, 1), Is.EqualTo(1120), "los totales finales desde el primer cuadro");
                Assert.That(motion.IsRunning, Is.False);
                rig.AssertIdle("reducido: " + reduced);
            }

            var normal = new Rig();
            var m = new CompletionMotion(normal.Driver, _ => { });
            m.Play(false);
            normal.RunFor(300);
            m.Finish();
            Assert.That(m.ValueOf(43, 2), Is.EqualTo(43));
            normal.AssertIdle("Finish");
        }

        // ── 0 B por cuadro y Stop de todo junto ────────────────────────────────────────────────────────────────────────

        [Test]
        public void NoAllocationsPerFrameAndStopLeavesNothingAnimating()
        {
            var rig = new Rig();
            var sink = 0.0;
            var (pulse, _) = Pulse(rig);
            var progress = new PillProgress();
            var pills = new PillRowMotion(rig.Driver, pulse, () => sink += 1, progress);
            var disc = new CenterDiscMotion(rig.Driver, progress, v => sink += v.CountOpacity);
            var cascade = new ImmersiveCascadeMotion(rig.Driver, (_, v) => sink += v.Opacity, b => sink += b, _ => sink += 1, () => sink += 1);
            var exit = new ImmersiveExitMotion(rig.Driver, v => sink += v.Hud, () => sink += 1, () => sink += 1);
            var info = new InfoRowMotion(rig.Driver, v => sink += v.ClockGap, o => sink += o, () => sink += 1);
            var seal = new SealMotion(rig.Driver, v => sink += v.BarHeight, s => sink += s == null ? 0 : 1);
            var veil = new PauseVeilMotion(rig.Driver, a => sink += a);
            var chip = new PhaseChipMotion(rig.Driver, v => sink += v.ColorMix);
            var dot = new LiveDotMotion(rig.Driver, o => sink += o);
            var kcal = new KcalSlideMotion(rig.Driver, v => sink += v.KcalShift, () => sink += 1);
            var completion = new CompletionMotion(rig.Driver, v => sink += v.CheckScale);

            var normal = new InfoRowPlan(InfoRowMode.Normal, 1, 1);
            var compact = new InfoRowPlan(InfoRowMode.Compact2, 1, 0);
            var row = Row(0, 7, 10, endingSoon: true);
            pulse.Update(true, true, Color.red, false);
            pills.OnState(row, false);
            disc.SetCount(3, false);
            cascade.Play();
            exit.Play(ExitStart.Full);
            info.ChangeMode(in normal, in compact, false);
            info.HideTarget(false);
            seal.Show(Seal(0, StepResult.Met, rig.Now), false);
            veil.SetPaused(true, false);
            chip.Change(false);
            dot.SetActive(true);
            kcal.Appear(75.6, false);
            completion.Play(false);

            for (var i = 0; i < 10; i++)
            {
                rig.Frame();
            }

            // A los 167 ms corren todos (la pastilla ya se fue: 120), más el pulso de la fila.
            Assert.That(rig.Driver.ActiveCount, Is.GreaterThanOrEqualTo(11));
            // Los 20 cuadros que siguen incluyen los finales del chip (200), del cambio de modo (240), de la entrada del sello y
            // del velo (300) y de kcal (360): también terminar es sin asignar.
            // Con el Recorder de "GC.Alloc" (GcAllocations): en Unity GC.GetAllocatedBytesForCurrentThread da siempre 0.
            var allocations = RowMate.Tests.UiSupport.GcAllocations.During(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    rig.Frame();
                }
            });
            Assert.That(allocations, Is.EqualTo(0), "0 B por cuadro con los IMotion de la 4b corriendo: " + allocations + " asignaciones en 20 cuadros");

            pills.Stop();
            disc.Stop();
            cascade.Stop();
            exit.Stop();
            info.Stop();
            seal.Stop();
            veil.Stop();
            chip.Stop();
            dot.Stop();
            kcal.Stop();
            completion.Stop();
            pulse.Stop();
            rig.AssertIdle("Dispose del HUD: nada queda animando (QA-V4a-02 = ROB-02)");
            rig.RunFor(3000);
            rig.AssertIdle("y nada vuelve");
            Assert.That(sink, Is.GreaterThan(0));
        }

        // ── Lo de la 4a para la regla de toma (§5.2, §7.4; QA-G4bT-07) ───────────────────────────────────────────────────

        [Test]
        public void TimedMotionEndsAtTheIntegerMillisecond()
        {
            // 30,2 − 30,0 = 0,1999999999999993 s: con la comparación en ms enteros (HudTimelines.Reached) termina en ese cuadro.
            var rig = new Rig();
            rig.RunToSecondsAfter(rig.Now, 30.0);
            var probe = new ProbeMotion(rig.Driver, 200);
            probe.Start();
            Assert.That(probe.IsDoneAt(rig.Now), Is.False);
            rig.Clock.Advance(TimeSpan.FromTicks(302000000 - rig.Clock.Elapsed.Ticks));
            Assert.That(rig.Now - 30.0, Is.LessThan(0.2), "el borde de la coma flotante");
            Assert.That(probe.IsDoneAt(rig.Now), Is.True);
            rig.Driver.Tick();
            Assert.That(probe.IsRunning, Is.False, "termina en el cuadro de los 200 ms");
            Assert.That(probe.LastElapsedMs, Is.EqualTo(200));
            Assert.That(probe.Finished, Is.EqualTo(1));
            rig.AssertIdle("terminó");
        }

        [Test]
        public void SpringMotionReportsWhatItLacksInDp()
        {
            // El ancho de Próximo: ω 12, 5,5 dp; a los 0,6 s le faltan 0,034 dp (02-tech-design §5.2): no bloquea una toma.
            var rig = new Rig();
            var value = 0.0;
            var spring = new SpringMotion(rig.Driver, v => value = v);
            spring.Snap(100);
            Assert.That(((ISettlingMotion)spring).RemainingDp, Is.EqualTo(0));
            spring.MoveTo(105.5, MotionSpecs.UpNextWidthOmega);
            Assert.That(spring.RemainingDp, Is.EqualTo(5.5).Within(1e-9));
            var start = rig.Now;
            while (rig.Now - start < 0.6 - 1e-9 && spring.IsRunning)
            {
                rig.Frame();
                Assert.That(spring.RemainingDp, Is.EqualTo(spring.IsRunning ? Math.Abs(105.5 - value) : 0).Within(1e-9));
            }

            Assert.That(spring.RemainingDp, Is.LessThan(0.15), "a +0,6 s, por debajo del umbral de la toma");
            Assert.That(spring.RemainingDp, Is.EqualTo(spring.IsRunning ? 0.034 : 0).Within(0.01));
            rig.RunFor(1500);
            Assert.That(spring.RemainingDp, Is.EqualTo(0));
            rig.AssertIdle("asentado");
        }

        [UnityTest]
        public IEnumerator StepPulseAndFitBoxReportTheirScaleInDp()
        {
            Create(new UiHostOptions());
            var motion = H.Handle.Motion;
            var bg = new VisualElement();
            bg.style.position = Position.Absolute;
            // La cuenta como la arma StepInfoBar (TabularNumber toma el ancho de sus cifras).
            var countdown = new TabularNumber(motion);
            countdown.style.position = Position.Absolute;
            countdown.AddToClassList("rm-type-num-s");
            countdown.SetFontSize(20);
            countdown.SetValue("0:03", 3, false);
            H.Root.Add(bg);
            H.Root.Add(countdown);
            var fit = new FitBox(false, false, motion);
            fit.style.position = Position.Absolute;
            fit.style.top = 100;
            fit.style.width = 100;
            fit.style.height = 40;
            var content = new VisualElement();
            content.style.width = 200;
            content.style.height = 20;
            fit.Add(content);
            H.Root.Add(fit);
            yield return Frames(3);

            Assert.That(countdown.layout.width, Is.GreaterThan(10), "la cuenta con su ancho");
            var pulse = new StepPulse(motion, bg, countdown, Color.white);
            pulse.Update(false, true, Color.red, false);
            Assert.That(pulse.RemainingDp, Is.EqualTo(Math.Abs(MotionSpecs.EndingSoonScale - pulse.Scale) * countdown.layout.width).Within(1e-6));
            Assert.That(pulse.RemainingDp, Is.GreaterThan(0.15), "la escala de la cuenta recién empieza: 0,1 del ancho");
            H.Step();
            Assert.That(pulse.RemainingDp, Is.EqualTo(Math.Abs(MotionSpecs.EndingSoonScale - pulse.Scale) * countdown.layout.width).Within(1e-6));
            for (var i = 0; i < 60; i++)
            {
                H.Step();
            }

            Assert.That(pulse.RemainingDp, Is.LessThan(0.15), "asentada");

            Assert.That(fit.FitScale, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(fit.RemainingDp, Is.EqualTo(0));
            fit.ExpectDataChange();
            content.style.width = 100;
            yield return Frames(3);
            Assert.That(fit.FitScale, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(fit.RemainingDp, Is.EqualTo(Math.Abs(fit.FitScale - fit.AppliedScale) * fit.Content.layout.width).Within(1e-3));
            Assert.That(fit.RemainingDp, Is.GreaterThan(0.15), "el resorte de la escala recién arranca");
            for (var i = 0; i < 120; i++)
            {
                H.Step();
            }

            Assert.That(fit.RemainingDp, Is.LessThan(0.15));
            pulse.Stop();
        }

        // ── Soporte ────────────────────────────────────────────────────────────────────────────────────────────────────

        private static readonly HudPiece[] Pieces =
        {
            HudPiece.TopBar, HudPiece.Spm, HudPiece.SplitWatts, HudPiece.Band, HudPiece.Vitals, HudPiece.Controls, HudPiece.Center,
        };

        private static readonly StepType[] Types3 = { StepType.Work, StepType.Rest, StepType.Work };

        private static readonly StepType[] Types4 = { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Work };

        // La fila de 3 pasos: los de antes con su resultado, el en curso y los pendientes.
        private static HudPillRow Row(int current, int elapsed, int? duration, bool running = true, bool endingSoon = false,
                                      StepPillLook done = StepPillLook.Solid)
        {
            var looks = new StepPillLook[Types3.Length];
            for (var i = 0; i < looks.Length; i++)
            {
                looks[i] = i < current ? done : i == current ? StepPillLook.Current : StepPillLook.Pending;
            }

            return new HudPillRow(Types3, looks, current, elapsed, duration, running, endingSoon);
        }

        private static HudSeal Seal(int step, StepResult verdict, double at)
        {
            return new HudSeal(step, verdict, verdict == StepResult.Met ? "Objetivo cumplido" : "No alcanzado", "24.0 / 18 spm", at);
        }

        private static (StepPulse Pulse, TabularNumber Countdown) Pulse(Rig rig)
        {
            var countdown = new TabularNumber(rig.Driver);
            return (new StepPulse(rig.Driver, new VisualElement(), countdown, Color.white), countdown);
        }

        // Pausa con la cuenta a la vista, con SetCount(null) antes o después de SetPaused(true): los valores por cuadro.
        private static double[] PausedDuringCount(bool countFirst)
        {
            var rig = new Rig();
            var progress = new PillProgress();
            progress.OnState(0, 7, 10, true, rig.Now, false);
            var visual = default(DiscVisual);
            var motion = new CenterDiscMotion(rig.Driver, progress, v => visual = v);
            motion.SetCount(null, false);
            motion.SetCount(3, false);
            rig.RunFor(500);
            progress.OnState(0, 7, 10, false, rig.Now, false);
            var start = rig.Now;
            if (countFirst)
            {
                motion.SetCount(null, false);
                motion.SetPaused(true, false);
            }
            else
            {
                motion.SetPaused(true, false);
                motion.SetCount(null, false);
            }

            var samples = new double[4 * 26];
            for (var k = 0; k < 26; k++)
            {
                if (k > 0)
                {
                    rig.Frame();
                }

                var t = HudTimelines.Ms(rig.Now - start);
                var expected = CenterDiscTimeline.Swap(Math.Min(t, 360), true, false);
                Assert.That(visual.CountOpacity, Is.EqualTo(expected.CountOpacity).Within(Eps), "la cuenta funde sin escala, en " + t);
                Assert.That(visual.CountScale, Is.EqualTo(1).Within(Eps));
                Assert.That(visual.PausedOpacity, Is.EqualTo(expected.PausedOpacity).Within(Eps), "PAUSA desde los 120, en " + t);
                Assert.That(visual.PausedScale, Is.EqualTo(expected.PausedScale).Within(Eps));
                Assert.That(visual.PausedOpacity > 1e-6 && visual.CountOpacity > 1e-6, Is.False, "nunca dos discos a la vista, en " + t);
                Assert.That(motion.IsFinite, Is.EqualTo(t < 360));
                samples[4 * k] = visual.CountOpacity;
                samples[4 * k + 1] = visual.CountScale;
                samples[4 * k + 2] = visual.PausedOpacity;
                samples[4 * k + 3] = visual.PausedScale;
            }

            Assert.That(motion.IsRunning, Is.False, "PAUSA quieto: sale");
            rig.AssertIdle("PAUSA quieto");
            return samples;
        }

        private static void AssertChange(in PillChangeVisual actual, in PillChangeVisual expected, long t)
        {
            Assert.That(actual.CompletingFill, Is.EqualTo(expected.CompletingFill).Within(Eps), "relleno que faltaba, en " + t);
            Assert.That(actual.Flash, Is.EqualTo(expected.Flash).Within(Eps), "destello, en " + t);
            Assert.That(actual.SolidOpacity, Is.EqualTo(expected.SolidOpacity).Within(Eps), "sólida, en " + t);
            Assert.That(actual.Highlight, Is.EqualTo(expected.Highlight).Within(Eps), "brillo, en " + t);
            Assert.That(actual.HollowInterior, Is.EqualTo(expected.HollowInterior).Within(Eps), "interior, en " + t);
            Assert.That(actual.HollowStroke, Is.EqualTo(expected.HollowStroke).Within(Eps), "trazo, en " + t);
            Assert.That(actual.HollowBorderOpacity, Is.EqualTo(expected.HollowBorderOpacity).Within(Eps), "borde, en " + t);
            Assert.That(actual.NewTrack, Is.EqualTo(expected.NewTrack).Within(Eps), "pista de la nueva, en " + t);
            Assert.That(actual.EndingHaloRamp, Is.EqualTo(expected.EndingHaloRamp).Within(Eps), "rampa del halo, en " + t);
        }

        private static void AssertThumb(in ThumbVisual actual, in ThumbVisual expected, long t)
        {
            Assert.That(actual.OutgoingHeight, Is.EqualTo(expected.OutgoingHeight).Within(Eps), "pulgar que se va, en " + t);
            Assert.That(actual.OutgoingOpacity, Is.EqualTo(expected.OutgoingOpacity).Within(Eps));
            Assert.That(actual.IncomingHeight, Is.EqualTo(expected.IncomingHeight).Within(Eps), "pulgar que llega, en " + t);
            Assert.That(actual.IncomingOpacity, Is.EqualTo(expected.IncomingOpacity).Within(Eps));
        }

        private static void AssertSeal(in SealVisual actual, in SealVisual expected, long t)
        {
            Assert.That(actual.BarHeight, Is.EqualTo(expected.BarHeight).Within(Eps), "alto de la barra, en " + t);
            Assert.That(actual.ContentOpacity, Is.EqualTo(expected.ContentOpacity).Within(Eps), "contenido, en " + t);
            Assert.That(actual.IconScale, Is.EqualTo(expected.IconScale).Within(Eps), "ícono, en " + t);
            Assert.That(actual.OldContentOpacity, Is.EqualTo(expected.OldContentOpacity).Within(Eps), "contenido viejo, en " + t);
        }

        // Un MotionDriver con el reloj de prueba, sin panel: cada Frame es un cuadro de 1/60 s.
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

            // Un cuadro en el instante exacto origin + seconds.
            public void RunToSecondsAfter(double origin, double seconds)
            {
                var target = (long)Math.Round((origin + seconds) * TimeSpan.TicksPerSecond);
                var delta = target - Clock.Elapsed.Ticks;
                Assert.That(delta, Is.GreaterThanOrEqualTo(0), "el reloj no retrocede");
                Clock.Advance(TimeSpan.FromTicks(delta));
                Driver.Tick();
            }

            public void AssertIdle(string because)
            {
                Assert.That(Driver.ActiveCount, Is.EqualTo(0), "nada en el MotionDriver: " + because);
                Assert.That(Pacing.Demands, Is.EqualTo(0), "nadie pide 60 fps: " + because);
            }
        }

        // Una animación de duración fija de la 4a (TimedMotion) para probar su final.
        private sealed class ProbeMotion : TimedMotion
        {
            private readonly double _total;

            public ProbeMotion(MotionDriver driver, double totalMs)
                : base(driver)
            {
                _total = totalMs;
            }

            public double LastElapsedMs { get; private set; } = -1;

            public int Finished { get; private set; }

            protected override double TotalMs => _total;

            public void Start()
            {
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                LastElapsedMs = elapsedMs;
            }

            protected override void OnFinished()
            {
                Finished++;
            }
        }
    }
}
