using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // EEF · La cuenta de -rowmate-perf-early (QA-V6b-01; AC-6B-01, -02; 06b-agua-avanzada/02-tech-design §2.1 (5)). La corrida de QA en el
    // build: 8/8 con el peor cuadro "con movimiento" a los ≈ 541-544 ms del toque, "unidad sesión preparada", y siempre ≈ 12 ms más que el
    // negro quieto (44,04 = 32,20 + 11,8; 52,35 = 39,40 + 12,9). Con la pantalla a ≈ 13 cuadros/s, FrameTimingManager entrega el cuadro
    // negro (el trabajo pesado y el armado del HUD) 4 cuadros después, y la cuenta vieja le ponía la hora y la fase del cuadro en que se
    // leía. Estas pruebas reproducen ese caso y fijan la atribución por la marca de inicio del cuadro.
    [TestFixture]
    public sealed class EarlyEntryFramesTests
    {
        private const double TicksPerMs = 10_000; // el reloj de 10 MHz (QueryPerformanceCounter)
        private const long FrameTicks = 80 * 10_000; // 12,5 cuadros por segundo (la pantalla en el modo de respaldo)

        // Un cuadro del player: empieza en Start, su LateUpdate un poco antes de terminar; su CPU.
        private struct Frame
        {
            public long Start;
            public long LateUpdate;
            public double CpuMs;
            public bool Black;
            public int Phase;
        }

        // El caso de QA: 6 cuadros antes del toque (en reposo), el toque en el 6, el negro de la entrada en el 9 (≈ 240 ms: 32 ms del trabajo
        // pesado + 12 del armado del HUD), el resto de la entrada con movimiento a 3 ms. La precarga queda en "sesión preparada" desde el negro.
        private static List<Frame> QaRun(double blackMs = 44.04, double movingMs = 3.0)
        {
            var frames = new List<Frame>();
            for (var i = 0; i < 40; i++)
            {
                var start = 1_000_000_000L + i * FrameTicks;
                frames.Add(new Frame
                {
                    Start = start,
                    LateUpdate = start + 2 * 10_000,
                    CpuMs = i == 9 ? blackMs : movingMs,
                    Black = i == 9,
                    Phase = i < 9 ? SceneHost_PhaseCamera : SceneHost_PhaseNextSession,
                });
            }

            return frames;
        }

        // Las fases de SceneHost (Scene, que dotnet no compila): 0 "preámbulo: cámara y luz" y 3 "sesión preparada".
        private const int SceneHost_PhaseCamera = 0;
        private const int SceneHost_PhaseNextSession = 3;

        private static long TapTicks(List<Frame> frames)
        {
            return frames[6].Start + 10_000;
        }

        // Lo que hace el player: en el LateUpdate de cada cuadro anota el cuadro y lee las últimas medidas, que llegan `lag` cuadros tarde.
        private static EarlyEntryFrames Record(List<Frame> frames, int lag)
        {
            var probe = new EarlyEntryFrames();
            for (var i = 0; i < frames.Count; i++)
            {
                probe.AddFrame(frames[i].LateUpdate, frames[i].Phase, frames[i].Black);
                for (var back = lag; back < lag + 4 && i - back >= 0; back++)
                {
                    probe.AddTiming(frames[i - back].Start, frames[i - back].CpuMs);
                }
            }

            return probe;
        }

        [Test, Category("AC-6B-01")]
        public void TheOldReadTimeAttributionBlamedAMovingFrame()
        {
            // La cuenta vieja: el peor de lo leído en cada cuadro desde los 230 ms del toque, con la hora y la fase del cuadro en que se leyó.
            var frames = QaRun();
            var tap = TapTicks(frames);
            const int lag = 4;
            var worst = 0.0;
            var worstAt = 0.0;
            var worstPhase = -1;
            for (var i = 6; i < frames.Count; i++)
            {
                var read = i - lag >= 0 ? frames[i - lag].CpuMs : 0;
                var at = (frames[i].LateUpdate - tap) / TicksPerMs;
                if (at >= 230 && read > worst)
                {
                    worst = read;
                    worstAt = at;
                    worstPhase = frames[i].Phase;
                }
            }

            Assert.That(worst, Is.EqualTo(44.04), "el negro");
            Assert.That(worstAt, Is.EqualTo(552).Within(20), "a los ≈ 540 ms del toque, como en las 8 corridas de QA");
            Assert.That(worstPhase, Is.EqualTo(SceneHost_PhaseNextSession), "con la fase del cuadro de la lectura");
        }

        [Test, Category("AC-6B-01"), Category("AC-6B-02")]
        public void LateTimingsGoToTheFrameThatProducedThem([Values(1, 2, 4)] int lag)
        {
            var frames = QaRun();
            var result = Record(frames, lag).Summarize(TapTicks(frames), TicksPerMs);
            Assert.That(result.TapFrameFound, Is.True);
            Assert.That(result.BlackFrameMs, Is.EqualTo(44.04), "el negro y quieto, aparte (D-03)");
            Assert.That(result.BlackFrames, Is.EqualTo(1));
            Assert.That(result.WorstMovingMs, Is.EqualTo(3.0), "ningún cuadro con movimiento carga el negro");
            Assert.That(result.TapMs, Is.EqualTo(3.0));
            Assert.That(result.Unmatched, Is.EqualTo(0));
        }

        [Test, Category("AC-6B-01")]
        public void AMovingFrameOverBudgetIsReportedWithItsTimeAndPhase()
        {
            var frames = QaRun();
            var slow = frames[16];
            slow.CpuMs = 21.5;
            frames[16] = slow;
            var tap = TapTicks(frames);
            var result = Record(frames, 4).Summarize(tap, TicksPerMs);
            Assert.That(result.WorstMovingMs, Is.EqualTo(21.5));
            Assert.That(result.WorstMovingAtMs, Is.EqualTo((frames[16].Start - tap) / TicksPerMs).Within(1e-9), "la hora del inicio de ESE cuadro");
            Assert.That(result.WorstMovingPhase, Is.EqualTo(SceneHost_PhaseNextSession));
        }

        [Test, Category("AC-6B-02")]
        public void TheTapIsTheFrameOfTheFirstLateUpdateAfterIt()
        {
            var frames = QaRun();
            var tapFrame = frames[6];
            tapFrame.CpuMs = 12.0;
            frames[6] = tapFrame;
            // Un cuadro anterior caro (en reposo) no es el toque ni "con movimiento".
            var before = frames[5];
            before.CpuMs = 30.0;
            frames[5] = before;
            var result = Record(frames, 2).Summarize(TapTicks(frames), TicksPerMs);
            Assert.That(result.TapMs, Is.EqualTo(12.0));
            Assert.That(result.WorstMovingMs, Is.EqualTo(3.0));
        }

        [Test, Category("AC-6B-02")]
        public void TheWaitOfTheFrameIsReadOneFrameLater()
        {
            var frames = QaRun();
            var tap = TapTicks(frames);
            var probe = new EarlyEntryFrames();
            for (var i = 0; i < frames.Count; i++)
            {
                probe.AddFrame(frames[i].LateUpdate, frames[i].Phase, frames[i].Black);
                // En el cuadro 7 se lee lo que esperó el 6 (el del toque); en el 10, lo del negro (9).
                probe.SetPreviousFrameWait(i == 7 ? 9.5 : i == 10 ? 20.0 : 0);
                probe.AddTiming(frames[i].Start, frames[i].CpuMs);
            }

            var result = probe.Summarize(tap, TicksPerMs);
            Assert.That(result.TapWaitMs, Is.EqualTo(9.5));
            Assert.That(result.BlackWaitMs, Is.EqualTo(20.0));
            Assert.That(result.WorstMovingWaitMs, Is.EqualTo(0));
            Assert.That(probe.Timeline(tap, TicksPerMs, 4), Does.StartWith("+-1:3.0(e9.5) +79:3.0 +159:3.0 +239:44.0(e20.0)n"));
        }

        [Test, Category("AC-6B-01")]
        public void RepeatedTimingsCountOnceAndTheFirstFrameIsOnlyABound()
        {
            var probe = new EarlyEntryFrames();
            probe.AddFrame(100, 0, false);
            probe.AddFrame(200, 0, false);
            probe.AddFrame(300, 0, false);
            Assert.That(probe.FrameOf(50), Is.EqualTo(-1), "de antes del primer cuadro anotado");
            Assert.That(probe.FrameOf(100), Is.EqualTo(-1), "el primero solo es cota");
            Assert.That(probe.FrameOf(150), Is.EqualTo(1));
            Assert.That(probe.FrameOf(250), Is.EqualTo(2));
            Assert.That(probe.FrameOf(350), Is.EqualTo(-1), "todavía sin LateUpdate");
            probe.AddTiming(150, 5);
            probe.AddTiming(150, 5);
            probe.AddTiming(0, 5);
            probe.AddTiming(250, 0);
            Assert.That(probe.Timings, Is.EqualTo(1));
        }
    }
}
