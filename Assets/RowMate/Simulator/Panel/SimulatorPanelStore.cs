using System;
using System.Collections.Generic;
using RowMate.App.Scene;
using RowMate.Core.Common;
using RowMate.Core.Text;

namespace RowMate.Simulator.Panel
{
    // Estado y acciones del panel del simulador, sin widgets (F-BLE-16). La fuente de verdad de los objetivos y de Remando es
    // el RowingSimulator compartido (cada cambio vale en el acto y se ve en la lectura siguiente, AC-BLE-16-10); la hora
    // forzada vive en SceneSettings. Nada se guarda (AC-BLE-16-11): al reiniciar, 150 W, 24 spm, "real", Remando encendido
    // y el panel cerrado. Las fallas con el equipo sin conectar no hacen nada ni producen errores (AC-BLE-16-09).
    // legacy: lib/core/dev/simulator_overlay.dart:28-173
    public sealed class SimulatorPanelStore : IDisposable
    {
        public const int WattsStep = 10; // :129-130
        public const int SpmStep = 1;    // :136-137

        private static readonly SimulatorPreset[] PresetTable =
        {
            new SimulatorPreset(SimulatorTexts.PresetSoft, 100, 20),
            new SimulatorPreset(SimulatorTexts.PresetMedium, 180, 24),
            new SimulatorPreset(SimulatorTexts.PresetHard, 280, 30),
        };

        private readonly SimulatorDeviceBackend _backend;
        private readonly SceneSettings _scene;
        private readonly IClock _clock;
        private readonly ObservableValue<SimulatorPanelState> _state;
        private IDisposable? _hourSubscription;
        private bool _open;

        public SimulatorPanelStore(SimulatorDeviceBackend backend, SceneSettings scene, IClock clock, IAppLogger logger)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _state = new ObservableValue<SimulatorPanelState>(Snapshot(), logger);
            State = new ReadOnlyObservable<SimulatorPanelState>(_state);
            _hourSubscription = scene.HourOverride.Subscribe(_ => Refresh(), false);
        }

        // Suave 100/20, Medio 180/24, Fuerte 280/30 (:10-14).
        public static IReadOnlyList<SimulatorPreset> Presets => PresetTable;

        public IReadOnlyObservable<SimulatorPanelState> State { get; }

        // :29: arranca cerrado. Abrir y cerrar no cambian nada más.
        public void Open()
        {
            _open = true;
            Refresh();
        }

        public void Close()
        {
            _open = false;
            Refresh();
        }

        // :115-121: los objetivos del preset; no toca Remando ni la hora (AC-BLE-16-05).
        public void ApplyPreset(SimulatorPreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            Simulator.TargetWatts = preset.Watts;
            Simulator.TargetSpm = preset.Spm;
            Refresh();
        }

        // :129-130: ±10 con los topes de RowingSimulator (30-500, AC-BLE-16-06).
        public void WattsMinus()
        {
            Simulator.TargetWatts -= WattsStep;
            Refresh();
        }

        public void WattsPlus()
        {
            Simulator.TargetWatts += WattsStep;
            Refresh();
        }

        // :136-137: ±1 con los topes de RowingSimulator (14-40).
        public void SpmMinus()
        {
            Simulator.TargetSpm -= SpmStep;
            Refresh();
        }

        public void SpmPlus()
        {
            Simulator.TargetSpm += SpmStep;
            Refresh();
        }

        // :143-144: ((hora ?? hora del reloj) ± 1) % 24 con el % de Dart (nunca negativo); solo la hora, sin los minutos
        // (AC-BLE-16-07: con el reloj a las 13:45, + → 14:00 y − → 12:00; 23 → 0 y 0 → 23).
        public void HourMinus()
        {
            _scene.HourOverride.Value = (int)DartMath.Mod(CurrentHour() - 1, 24);
        }

        public void HourPlus()
        {
            _scene.HourOverride.Value = (int)DartMath.Mod(CurrentHour() + 1, 24);
        }

        // :148: vuelve a "real" (y el botón desaparece).
        public void RealHour()
        {
            _scene.HourOverride.Value = null;
        }

        // :151-156: apagado, desde la lectura siguiente vatios y ritmo 0, split "--:--" y totales congelados (AC-BLE-16-08).
        public void SetRowing(bool rowing)
        {
            Simulator.Rowing = rowing;
            Refresh();
        }

        public void ToggleRowing()
        {
            SetRowing(!Simulator.Rowing);
        }

        // :160: la caída de 3 s del remo (AC-BLE-15-06); sin el remo conectado no hace nada.
        public void SimulateRowerDrop()
        {
            _backend.SimRower.SimulateDisconnect();
        }

        // :165: la caída del pulsómetro (AC-HRM-15-08); sin el pulsómetro conectado no hace nada.
        public void SimulateHeartRateDrop()
        {
            _backend.SimHeartRate.SimulateDisconnect();
        }

        public void Dispose()
        {
            var subscription = _hourSubscription;
            _hourSubscription = null;
            subscription?.Dispose();
        }

        private RowingSimulator Simulator => _backend.Simulator;

        private long CurrentHour()
        {
            var forced = _scene.HourOverride.Value;
            return forced ?? _clock.LocalNow.Hour;
        }

        // Un estado nuevo con los valores actuales del simulador y de la escena (ObservableValue avisa solo si cambió algo).
        private void Refresh()
        {
            _state.Value = Snapshot();
        }

        private SimulatorPanelState Snapshot()
        {
            return new SimulatorPanelState(_open, Simulator.TargetWatts, Simulator.TargetSpm, _scene.HourOverride.Value, Simulator.Rowing);
        }
    }
}
