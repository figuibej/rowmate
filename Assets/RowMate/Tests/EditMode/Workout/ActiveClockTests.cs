using System;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // ACK: el reloj activo por tiempo monótono (ADR 017, decisión 4; 02-tech-design §2.2 de la It. 4a) y su uso en el motor.
    [TestFixture]
    public sealed class ActiveClockTests
    {
        private const long Second = TimeSpan.TicksPerSecond;

        [Test, Category("AC-WKT-02-01")]
        public void CountsWholeSecondsOfTheSegment()
        {
            var clock = new ActiveClock();
            Assert.That(clock.Due(10 * Second), Is.EqualTo(0), "sin tramo no cuenta");
            clock.Start(2 * Second);
            Assert.That(clock.IsRunning, Is.True);
            Assert.That(clock.Due(2 * Second + Second - 1), Is.EqualTo(0));
            Assert.That(clock.Due(3 * Second), Is.EqualTo(1));
            Assert.That(clock.NextDueTicks, Is.EqualTo(3 * Second));
            Assert.That(clock.Due(5 * Second + 500), Is.EqualTo(3), "un cuadro tarde: los tres vencidos");
            clock.MarkProcessed();
            Assert.That(clock.SegmentSeconds, Is.EqualTo(1));
            Assert.That(clock.Due(5 * Second), Is.EqualTo(2));
            Assert.That(clock.DueAtTicks(2), Is.EqualTo(4 * Second));
            clock.Stop();
            Assert.That(clock.Due(9 * Second), Is.EqualTo(0));
            Assert.That(ActiveClock.ToTicks(1.25), Is.EqualTo(12500000));
        }

        [Test, Category("AC-WKT-02-01")]
        public void OneSecondPerActiveSecondOnly()
        {
            // Un segundo por cada segundo en marcha, ninguno en pausa, en reposo ni terminada.
            using (var h = new WorkoutHarness())
            {
                h.At(-0.2);
                h.At(20);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(0), "en reposo");
                h.Engine.StartFree();
                h.At(30);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(10));
                h.Engine.Pause();
                h.At(60);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(10), "en pausa");
                h.Engine.Resume();
                h.At(65);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(15));
                h.Engine.FinishAsync();
                h.At(90);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(0), "de vuelta en reposo: los relojes en 0");
            }
        }

        [Test, Category("AC-WKT-01-05")]
        public void ResumeRestartsTheRhythm()
        {
            // Q-58: pausa a los 10,7 s (10 segundos contados) y 30 s de pausa: el segundo 11 llega a los 41,7 s, no a los 41,0.
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(10.7);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(10));
                h.Engine.Pause();
                h.At(40.7);
                h.Engine.Resume();
                h.At(41.69);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(10), "a los 41,0 todavía no");
                h.At(41.7);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(11), "1 s después de reanudar");
                h.At(43);
                h.Engine.FinishAsync();
                Assert.That(WorkoutHarness.Describe(h.Points()), Is.EqualTo("305/1020/40/-/- 310/1040/41/-/- 343/1172/44/-/-"),
                    "el Anexo A: (305), (310), (343), 12 s contados");
            }
        }

        [Test, Category("AC-WKT-02-04")]
        public void SixHundredSecondsWithALateFrame()
        {
            // DV-27: 600 s en marcha dan exactamente 600 segundos, también con un cuadro demorado 2,5 s en el medio (el loop
            // cortado al arrastrar la ventana en Windows): se procesan en orden los segundos que faltan, sin perder ni duplicar.
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
            var sessions = new InMemorySessionRepository();
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler,
                       harness.Lifecycle, harness.Logger))
            {
                rower.Start();
                harness.RunUntil(0);
                engine.StartFree();
                harness.RunUntil(300.2);
                harness.Stall(2.5);
                Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(302), "el cuadro tarde procesa los dos vencidos");
                harness.RunUntil(600.01);
                Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(600));
                Assert.That(sessions.AllPoints.Count, Is.EqualTo(120), "un punto cada 5 s, ninguno de más ni de menos");
            }
        }
    }
}
