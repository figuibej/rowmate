using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Haptics
{
    // Respuesta táctil de Android (A10-06, D10-11 de la It. 10a; It. 14, DT14-24, AC-POL-03, AC-DEBT-10): com.rowmate.ui.RmHaptics
    // (Assets/Plugins/Android/RowMateUi) → View.performHapticFeedback en el hilo de UI, sin el permiso VIBRATE y respetando el ajuste
    // del sistema. Desde la It. 14 también Confirm pasa por RmHaptics, así que ya no depende de que el Bluetooth esté iniciado
    // (RmBle.haptic queda como está, por su golden). La actividad se le pasa una sola vez (bind) con la de Unity. Sin #if (DT10-19):
    // compila en todas las plataformas; solo se crea en Android (HapticsFactory). Las firmas las fija native/android/check.ps1 con
    // golden/ui-signatures.txt. NO VERIFICABLE sin el módulo de Android y un teléfono.
    public sealed class AndroidHaptics : IHaptics
    {
        public const string ClassName = "com.rowmate.ui.RmHaptics";

        private readonly IAppLogger _logger;
        private AndroidJavaClass? _class;
        private bool _failed;

        public AndroidHaptics(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Confirm()
        {
            Play(HapticKind.Confirm);
        }

        // Los valores de HapticKind son los de RmHaptics (0 Confirm, 1 Light, 2 Medium, 3 Success).
        public void Play(HapticKind kind)
        {
            var bridge = Bind();
            if (bridge == null)
            {
                return;
            }

            try
            {
                bridge.CallStatic<bool>("perform", (int)kind);
            }
            catch (Exception error)
            {
                // Una falla no corta nada: la háptica es un adorno (A10-06).
                _logger.Log(LogLevel.Warning, "HAPTIC", "perform falló: " + error.GetType().Name);
            }
        }

        private AndroidJavaClass? Bind()
        {
            if (_class != null || _failed)
            {
                return _class;
            }

            try
            {
                var bridge = new AndroidJavaClass(ClassName);
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    bridge.CallStatic("bind", activity);
                }

                _class = bridge;
            }
            catch (Exception error)
            {
                _failed = true;
                _logger.Log(LogLevel.Warning, "HAPTIC", "sin RmHaptics: " + error.GetType().Name);
            }

            return _class;
        }
    }
}
