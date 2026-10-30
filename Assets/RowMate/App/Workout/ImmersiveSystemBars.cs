using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.App.Workout
{
    // Cuándo se piden las barras del sistema ocultas y visibles (F-APP-15; AC-APP-15-01, -04; 02-tech-design §4.1 de la It. 4b).
    // El legacy pone el modo inmersivo persistente al crear la página y "de borde a borde" al destruirla
    // (immersive_workout_screen.dart:32-41); la spec fija los momentos: ocultar al empezar cada entrada y mostrar al terminar
    // cada salida. Cuenta los inmersivos entre su entrada y el final de su salida: con la sesión nueva que abre su inmersivo
    // durante la salida del anterior (QA-V4a-01), la salida del viejo no las muestra (DT4b-05). Con el diálogo de completado no
    // pide nada (es de UI Toolkit, no una ventana del sistema). Una por app (AppContext.SystemBars). Hilo principal.
    public sealed class ImmersiveSystemBars : IDisposable
    {
        public const string Tag = "UI";

        private readonly ISystemUi _ui;
        private readonly IAppLogger _logger;
        private readonly List<object> _owners = new List<object>(2);
        private readonly IDisposable _backgroundSubscription;
        private readonly IDisposable _focusSubscription;
        private bool _disposed;

        public ImmersiveSystemBars(ISystemUi ui, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundSubscription = lifecycle.InBackground.Subscribe(OnBackground, emitCurrent: false);
            _focusSubscription = lifecycle.HasFocus.Subscribe(OnFocus, emitCurrent: false);
        }

        public bool Hidden { get; private set; }

        // Los inmersivos entre su entrada y el final de su salida.
        public int Owners => _owners.Count;

        // Cada entrada (el cuadro del toque): SetImmersive(true), aunque ya estén ocultas.
        public void EntryStarted(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (_disposed)
            {
                return;
            }

            if (IndexOf(owner) < 0)
            {
                _owners.Add(owner);
            }

            Request(true);
        }

        // Cada salida terminada (a los 640 ms): SetImmersive(false) si no queda otro inmersivo.
        public void ExitFinished(object owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            var index = IndexOf(owner);
            if (_disposed || index < 0)
            {
                return;
            }

            _owners.RemoveAt(index);
            if (_owners.Count == 0)
            {
                Request(false);
            }
        }

        // La app se cierra: si quedaron ocultas, se muestran.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _backgroundSubscription.Dispose();
            _focusSubscription.Dispose();
            if (Hidden)
            {
                Request(false);
            }

            _owners.Clear();
        }

        // Con Hidden: al volver del segundo plano, SetImmersive(true) otra vez (Android las puede devolver al volver de otra app).
        private void OnBackground(bool inBackground)
        {
            if (!_disposed && !inBackground && Hidden)
            {
                Request(true);
            }
        }

        // Con Hidden: al recuperar el foco, otra vez.
        private void OnFocus(bool hasFocus)
        {
            if (!_disposed && hasFocus && Hidden)
            {
                Request(true);
            }
        }

        private int IndexOf(object owner)
        {
            for (var i = 0; i < _owners.Count; i++)
            {
                if (ReferenceEquals(_owners[i], owner))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Request(bool immersive)
        {
            Hidden = immersive;
            _logger.Log(LogLevel.Debug, Tag, immersive ? "barras del sistema: ocultas" : "barras del sistema: visibles");
            try
            {
                _ui.SetImmersive(immersive);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "barras del sistema: " + error.GetType().Name + ": " + error.Message, error);
            }
        }
    }
}
