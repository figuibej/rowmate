using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    // La fase visual φ (D-ANI-01; AC-SCN-04-04, AC-SCN-04-05; ANIMATION_BIBLE §3.1, §3.2, §3.7, §3.9): la que anima el remero, los
    // remos, el agua y el bote. Sigue a la fase del contrato p sin saltos y nunca retrocede: φ' = ω_s + K·wrap(p − φ), φ' ≥ 0, con ω_s
    // siguiendo a spm/60 (τ 0,5 s activo) o cayendo a 0 (τ 0,15 s en pausa). Integración semi-implícita del término K con el paso
    // acotado de la escena. El ratio r se fija al cruzar el catch (nunca cambia a mitad de palada) con el SPM suavizado τ 0,8 s.
    // Sin paladas (activo con spm 0): ω_s apunta a 12/60 hasta que φ cruza 0; en ese cuadro φ = 0 y ω_s = 0 exactos y quedan así
    // (traba del catch, QA7-G-03). Sin asignar.
    public sealed class VisualStrokeClock
    {
        private const double MaxStep = SceneState.MaxStep;
        private const double ClosingSpeed = SceneState.IdleSpm / 60;

        private double _ratioSpm = SceneState.IdleSpm;
        private bool _latched = true;
        private double? _forced;

        public VisualStrokeClock()
        {
            Reset();
        }

        // φ ∈ [0; 1).
        public double Phase { get; private set; }

        // c = StrokeRhythm.ToCanonical(φ, r).
        public double Canonical { get; private set; }

        // r, fijado al cruzar φ = 0.
        public double LatchedRatio { get; private set; }

        // ω_s (ciclos/s).
        public double PhaseSpeed { get; private set; }

        // Segundos con ω_s < 0,01 y en el catch trabado o en pausa (para RowerIdle).
        public double StillSeconds { get; private set; }

        // El SPM suavizado (τ 0,8 s) del ratio y del surge.
        public double SmoothedSpm => _ratioSpm;

        // true mientras está trabado en el catch sin paladas (o antes de la primera palada).
        public bool IsLatched => _latched;

        public void Reset()
        {
            Phase = 0;
            Canonical = 0;
            PhaseSpeed = 0;
            StillSeconds = 0;
            _ratioSpm = SceneState.IdleSpm;
            LatchedRatio = StrokeRhythm.DriveRatio(SceneState.IdleSpm);
            _latched = true;
        }

        // Un cuadro: dt (s, se acota a [0; 0,1]), la fase del contrato de ese cuadro (después de SceneState.Update), el SPM y si
        // está activo. Devuelve los eventos que pasaron en el cuadro.
        public StrokeEvents Step(double dt, double contractPhase, double spm, bool active)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > MaxStep ? MaxStep : dt);
            var rowing = active && spm > 0;
            if (_forced.HasValue)
            {
                Phase = StrokeRhythm.FromCanonical(_forced.Value, LatchedRatio);
                Canonical = _forced.Value;
                PhaseSpeed = 0;
                StillSeconds = 0;
                return StrokeEvents.None;
            }

            if (rowing && _latched)
            {
                // Arranca desde el catch: el ratio de esta palada sale del SPM de este cuadro.
                _latched = false;
                _ratioSpm = spm;
                LatchedRatio = StrokeRhythm.DriveRatio(spm);
            }

            // El SPM del ratio (τ 0,8 s): el de las paladas o, cerrando sin paladas, 12.
            if (active)
            {
                var target = rowing ? spm : SceneState.IdleSpm;
                _ratioSpm += (target - _ratioSpm) * (1 - Math.Exp(-step / MotionSpecs.StrokeRatioSpmTauSeconds));
            }

            if (_latched)
            {
                Phase = 0;
                PhaseSpeed = 0;
                Canonical = 0;
                StillSeconds += step;
                return StrokeEvents.None;
            }

            // ω_s: spm/60 remando; 12/60 cerrando sin paladas; 0 en pausa.
            double omegaTarget;
            double tau;
            if (rowing)
            {
                omegaTarget = spm / 60;
                tau = MotionSpecs.StrokeSpeedTauSeconds;
            }
            else if (active)
            {
                omegaTarget = ClosingSpeed;
                tau = MotionSpecs.StrokeSpeedTauSeconds;
            }
            else
            {
                omegaTarget = 0;
                tau = MotionSpecs.StrokeStopTauSeconds;
            }

            PhaseSpeed += (omegaTarget - PhaseSpeed) * (1 - Math.Exp(-step / tau));
            if (!active && PhaseSpeed < 1e-9)
            {
                PhaseSpeed = 0;
            }

            var error = Wrap(contractPhase - Phase);
            var increment = step * (PhaseSpeed + MotionSpecs.StrokePhaseGain * error) / (1 + MotionSpecs.StrokePhaseGain * step);
            if (!(increment > 0))
            {
                // Nunca retrocede.
                increment = 0;
            }

            var events = StrokeEvents.None;
            var previousCanonical = Canonical;
            var next = Phase + increment;
            if (next >= 1)
            {
                events |= StrokeEvents.Catch;
                if (!rowing && active)
                {
                    // Traba del catch sin paladas (QA7-G-03).
                    next = 0;
                    PhaseSpeed = 0;
                    _latched = true;
                }
                else
                {
                    next -= Math.Floor(next);
                }

                LatchedRatio = StrokeRhythm.DriveRatio(_ratioSpm);
            }

            Phase = next;
            Canonical = StrokeRhythm.ToCanonical(Phase, LatchedRatio);
            if (increment > 0 && active)
            {
                // En pausa no hay eventos (ningún charco ni salpicadura nuevos), aunque φ termine de alcanzar a p.
                var before = OarCurves.BladeCenterY(previousCanonical);
                var after = OarCurves.BladeCenterY(Canonical);
                if (before > 0 && after <= 0)
                {
                    events |= StrokeEvents.BladeEntry;
                }
                else if (before <= 0 && after > 0)
                {
                    events |= StrokeEvents.BladeExit;
                }

                if (previousCanonical < 0.36 && Canonical >= 0.36)
                {
                    events |= StrokeEvents.Finish;
                }
            }

            var still = PhaseSpeed < 0.01 && (_latched || !active);
            StillSeconds = still ? StillSeconds + step : 0;
            return active ? events : StrokeEvents.None;
        }

        // La fase forzada de las pruebas y del arnés: φ = FromCanonical(c, r actual), ω_s = 0, sin eventos. null = la de la sesión.
        internal void ForceCanonicalForTest(double? canonical)
        {
            _forced = canonical;
            if (canonical.HasValue)
            {
                Canonical = canonical.Value;
                Phase = StrokeRhythm.FromCanonical(canonical.Value, LatchedRatio);
                PhaseSpeed = 0;
            }
        }

        // wrap ∈ (−0,5; 0,5].
        public static double Wrap(double x)
        {
            var w = x - Math.Floor(x);
            return w > 0.5 ? w - 1 : w;
        }
    }
}
