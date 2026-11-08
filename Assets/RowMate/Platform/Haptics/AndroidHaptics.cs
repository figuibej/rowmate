using System;
using RowMate.Core.Ble.Native;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Ble;
using UnityEngine;

namespace RowMate.Platform.Haptics
{
    // Respuesta táctil de Android (A10-06, D10-11 de la It. 10a): RmBle.haptic() → performHapticFeedback(CONFIRM) en API 30+ y
    // CONTEXT_CLICK en 26-29, en el hilo de UI de la actividad. Sin el permiso VIBRATE. Si el plugin no está iniciado (Bluetooth
    // apagado por ROWMATE_BLE=off) no hace nada: solo se usa al quedar "Listo" con el remo. Sin #if (DT10-19): compila en todas
    // las plataformas; solo se crea en Android (HapticsFactory). NO VERIFICABLE sin el módulo de Android y un teléfono.
    public sealed class AndroidHaptics : IHaptics
    {
        private static readonly jvalue[] None = new jvalue[0];

        private readonly IAppLogger _logger;

        public AndroidHaptics(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Confirm()
        {
            if (!RmBleJni.Bind(_logger))
            {
                return;
            }

            var method = RmBleJni.Method(RmBleJniSignatures.Haptic.Name);
            if (method == IntPtr.Zero)
            {
                return;
            }

            AndroidJNI.CallStaticVoidMethod(RmBleJni.Class, method, None);
            RmBleJni.Check(_logger, RmBleJniSignatures.Haptic.Name);
        }
    }
}
