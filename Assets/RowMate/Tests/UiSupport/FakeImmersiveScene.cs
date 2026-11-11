using System;
using RowMate.Core.Common;
using RowMate.Presentation.Workout;
using UnityEngine;

namespace RowMate.Tests.UiSupport
{
    // Un IImmersiveScene de prueba (06a-escena-lago/02-tech-design §1.1 B2, §3.3; QA-G6aT-16): cuenta los suscriptores de OutputChanged
    // y de Drawn y las llamadas a Preload y Open, para afirmar que scene-surface solo muestra (nunca arranca el ciclo de vida) y que no
    // deja suscripciones colgadas al desarmarse (HudWarmup arma y desarma dos HudView en el arranque).
    public sealed class FakeImmersiveScene : IImmersiveScene
    {
        private readonly ObservableValue<ImmersiveSceneReadiness> _readiness;
        private Action? _outputChanged;
        private Action? _drawn;

        public FakeImmersiveScene(IAppLogger logger, ImmersiveSceneReadiness readiness = ImmersiveSceneReadiness.NotLoaded)
        {
            _readiness = new ObservableValue<ImmersiveSceneReadiness>(readiness, logger);
        }

        public IReadOnlyObservable<ImmersiveSceneReadiness> Readiness => _readiness;

        public Texture? Output { get; set; }

        public bool IsShowing { get; set; }

        public int OutputSubscribers { get; private set; }

        public int DrawnSubscribers { get; private set; }

        public int Preloads { get; private set; }

        public int Opens { get; private set; }

        public int Flights { get; private set; }

        public int InPlace { get; private set; }

        public int Exits { get; private set; }

        public int Aerials { get; private set; }

        public int Closes { get; private set; }

        public double VeilWeight { get; private set; }

        public event Action? OutputChanged
        {
            add
            {
                _outputChanged += value;
                OutputSubscribers++;
            }
            remove
            {
                _outputChanged -= value;
                OutputSubscribers--;
            }
        }

        public event Action? Drawn
        {
            add
            {
                _drawn += value;
                DrawnSubscribers++;
            }
            remove
            {
                _drawn -= value;
                DrawnSubscribers--;
            }
        }

        public void SetReadiness(ImmersiveSceneReadiness readiness)
        {
            _readiness.Value = readiness;
        }

        public void RaiseOutputChanged()
        {
            _outputChanged?.Invoke();
        }

        public void Preload()
        {
            Preloads++;
        }

        public void Open(double now)
        {
            Opens++;
        }

        public int CatchUps { get; private set; }

        public void CatchUp()
        {
            CatchUps++;
        }

        // It. 15 (AC-D15-30, ItFourteenGapTests.SceneLoading*): desde cuándo corre el vuelo pedido (NaN sin vuelo).
        public double LastFlightStart { get; private set; } = double.NaN;

        public void BeginFlight(double now)
        {
            Flights++;
            LastFlightStart = now;
        }

        public void ShowInPlace(double now)
        {
            InPlace++;
        }

        public void SetPauseVeil(double weight)
        {
            VeilWeight = weight;
        }

        public void BeginExit(double now)
        {
            Exits++;
        }

        public void BeginAerial(double now)
        {
            Aerials++;
        }

        public void Close()
        {
            Closes++;
        }
    }
}
