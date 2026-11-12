using System;
using System.Collections.Generic;

namespace RowMate.Core.Scene
{
    // La cuenta de -rowmate-perf-early (06b-agua-avanzada/02-tech-design §2.1 (5); AC-6B-01, -02; QA-V6b-01). FrameTimingManager entrega
    // el tiempo de CPU de un cuadro con 1 a 4 cuadros de atraso (más con la GPU lenta); antes se le ponía la hora y la fase del cuadro en
    // que se LEÍA, y el cuadro negro de la entrada (el trabajo pesado y el armado del HUD) aparecía como "con movimiento" a los ≈ 540 ms del
    // toque. Acá cada medida va al cuadro que la produjo por su marca de inicio: el primero cuyo LateUpdate es posterior a esa marca (el
    // cuadro empieza antes que su LateUpdate y después del LateUpdate anterior). Las marcas son del mismo reloj (ticks). Sin Unity.
    public sealed class EarlyEntryFrames
    {
        private readonly List<long> _lateUpdates;
        private readonly List<int> _phases;
        private readonly List<bool> _black;
        private readonly List<long> _timingStarts;
        private readonly List<double> _timingMs;

        public EarlyEntryFrames(int capacity = 512)
        {
            _lateUpdates = new List<long>(capacity);
            _phases = new List<int>(capacity);
            _black = new List<bool>(capacity);
            _timingStarts = new List<long>(capacity);
            _timingMs = new List<double>(capacity);
        }

        public int Frames => _lateUpdates.Count;

        public int Timings => _timingStarts.Count;

        // Un cuadro visto en LateUpdate (en orden): su marca, la fase del armado al empezar el Tick de la escena y si en ese cuadro corrió
        // el trabajo pesado del negro de la entrada (RunPendingHeavyWork).
        public void AddFrame(long lateUpdateTicks, int phase, bool blackFrame)
        {
            _lateUpdates.Add(lateUpdateTicks);
            _phases.Add(phase);
            _black.Add(blackFrame);
            _waits.Add(0);
        }

        // Lo que el hilo principal del cuadro ANTERIOR esperó a la GPU o al hilo de render (ms; los marcadores Gfx.WaitFor* del Profiler,
        // que se leen un cuadro después y sin atraso extra). Va en el cuadro anterior al último anotado.
        public void SetPreviousFrameWait(double waitMs)
        {
            if (_waits.Count >= 2)
            {
                _waits[_waits.Count - 2] = waitMs;
            }
        }

        private readonly List<double> _waits = new List<double>();

        // Una medida de FrameTimingManager (la marca de inicio del cuadro y su CPU del hilo principal, ms). Se piden varias por cuadro: las
        // repetidas (la misma marca) cuentan una vez.
        public void AddTiming(long frameStartTicks, double cpuMs)
        {
            if (frameStartTicks <= 0 || !(cpuMs > 0) || _timingStarts.Contains(frameStartTicks))
            {
                return;
            }

            _timingStarts.Add(frameStartTicks);
            _timingMs.Add(cpuMs);
        }

        // El índice del cuadro que produjo una medida; −1 si es del primer cuadro anotado o de antes (el primero solo sirve de cota: hay que
        // anotar desde antes del toque) o de uno que todavía no llegó a LateUpdate.
        public int FrameOf(long frameStartTicks)
        {
            for (var k = 1; k < _lateUpdates.Count; k++)
            {
                if (_lateUpdates[k] >= frameStartTicks)
                {
                    return frameStartTicks > _lateUpdates[k - 1] ? k : -1;
                }
            }

            return -1;
        }

        // El toque (el cuadro del primer LateUpdate después de la marca del toque), el peor cuadro con movimiento (todos los que siguen al
        // del toque menos el negro y quieto de la entrada, D-03: el fundido a negro, la aparición, el vuelo y la cascada) y el negro entero.
        public EarlyEntryResult Summarize(long tapTicks, double ticksPerMs)
        {
            if (!(ticksPerMs > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerMs));
            }

            var tapFrame = -1;
            for (var k = 1; k < _lateUpdates.Count; k++)
            {
                if (_lateUpdates[k] >= tapTicks)
                {
                    tapFrame = k;
                    break;
                }
            }

            var result = new EarlyEntryResult { WorstMovingPhase = -1, TapFrameFound = tapFrame >= 0 };
            for (var i = 0; i < _timingStarts.Count; i++)
            {
                var k = FrameOf(_timingStarts[i]);
                if (k < 0 || tapFrame < 0 || k < tapFrame)
                {
                    // Las de antes del toque no cuentan; las que no caen en ningún cuadro anotado, sí (como sin atribuir).
                    result.Unmatched += k < 0 && _timingStarts[i] > tapTicks ? 1 : 0;
                    continue;
                }

                result.Matched++;
                var ms = _timingMs[i];
                if (k == tapFrame)
                {
                    result.TapMs = Math.Max(result.TapMs, ms);
                    result.TapWaitMs = _waits[k];
                }
                else if (_black[k])
                {
                    result.BlackFrameMs = Math.Max(result.BlackFrameMs, ms);
                    result.BlackWaitMs = Math.Max(result.BlackWaitMs, _waits[k]);
                    result.BlackFrames++;
                }
                else if (ms > result.WorstMovingMs)
                {
                    result.WorstMovingMs = ms;
                    result.WorstMovingWaitMs = _waits[k];
                    result.WorstMovingAtMs = (_timingStarts[i] - tapTicks) / ticksPerMs;
                    result.WorstMovingPhase = _phases[k];
                }
            }

            return result;
        }

        // Los cuadros desde el toque, para el registro: "+<ms desde el toque>:<CPU>" y "n" en el negro, hasta `max` cuadros (los que tienen
        // medida). Solo para [PERF]: asigna.
        public string Timeline(long tapTicks, double ticksPerMs, int max)
        {
            var parts = new List<KeyValuePair<long, string>>();
            for (var i = 0; i < _timingStarts.Count; i++)
            {
                var k = FrameOf(_timingStarts[i]);
                if (k < 0 || _lateUpdates[k] < tapTicks)
                {
                    continue;
                }

                var at = (_timingStarts[i] - tapTicks) / ticksPerMs;
                parts.Add(new KeyValuePair<long, string>(_timingStarts[i],
                    "+" + RowMate.Core.Text.Fmt.Fixed(at, 0) + ":"
                    + RowMate.Core.Text.Fmt.Fixed(_timingMs[i], 1)
                    + (_waits[k] > 0.05 ? "(e" + RowMate.Core.Text.Fmt.Fixed(_waits[k], 1) + ")" : "") + (_black[k] ? "n" : "")));
            }

            parts.Sort((a, b) => a.Key.CompareTo(b.Key));
            var text = new System.Text.StringBuilder();
            for (var i = 0; i < parts.Count && i < max; i++)
            {
                text.Append(i == 0 ? "" : " ").Append(parts[i].Value);
            }

            return text.ToString();
        }
    }

    // Lo que informa la línea [PERF] de la entrada temprana (ms; la hora del peor, desde el toque).
    public struct EarlyEntryResult
    {
        public double TapMs;
        public double TapWaitMs;
        public double WorstMovingWaitMs;
        public double WorstMovingMs;
        public double WorstMovingAtMs;
        public int WorstMovingPhase;
        public double BlackFrameMs;
        public double BlackWaitMs;
        public int BlackFrames;
        public int Matched;
        public int Unmatched;
        public bool TapFrameFound;
    }
}
