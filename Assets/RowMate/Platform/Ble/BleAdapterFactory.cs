using System;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Platform.Ble
{
    // Elige el plugin nativo de Bluetooth LE (10a 02-tech-design §5.1). El ÚNICO #if de plataforma del Bluetooth (DT10-19): los
    // puentes de Android (WP-C) se compilan en todas las plataformas pero solo se crean en Android.
    // - Windows (player y Editor): WindowsBleApi sobre RowMateBle.dll, sin permisos (null).
    // - Android: AndroidBleApi + AndroidBlePermissions (WP-C).
    // - Otro sistema, ROWMATE_BLE=off (pruebas, arnés de capturas: ninguna toca la radio) o la DLL que no carga o no tiene la
    //   versión: null → el backend sin Bluetooth de la It. 2 (UnavailableRowerService).
    public static class BleAdapterFactory
    {
        public const string Tag = "BLE";
        public const string DisableVariable = "ROWMATE_BLE";

        // ROWMATE_BLE=off (o 0 / false): sin plugin nativo.
        public static bool IsDisabledByEnvironment
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(DisableVariable);
                return value != null && (value.Equals("off", StringComparison.OrdinalIgnoreCase) || value == "0"
                                         || value.Equals("false", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static INativeBleApi? Create(IAppLogger logger, MainThreadQueue mainThread, out IBlePermissions? permissions)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (mainThread == null)
            {
                throw new ArgumentNullException(nameof(mainThread));
            }

            permissions = null;
            if (IsDisabledByEnvironment)
            {
                logger.Log(LogLevel.Info, Tag, "Bluetooth nativo apagado (" + DisableVariable + "=off)");
                return null;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // WP-C (02-tech-design §4.3): el puente JNI y los permisos de Android (respuesta por MainThreadQueue, DT10-10).
            var android = new AndroidBleApi(logger);
            permissions = new AndroidBlePermissions(android, mainThread, logger);
            return android;
#else
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    try
                    {
                        var version = WindowsBleApi.Probe();
                        if (version != BleWire.ApiVersion)
                        {
                            logger.Log(LogLevel.Error, Tag, "RowMateBle.dll con la versión " + version + " (se esperaba " + BleWire.ApiVersion + ")");
                            return null;
                        }

                        return new WindowsBleApi();
                    }
                    catch (DllNotFoundException)
                    {
                        logger.Log(LogLevel.Error, Tag, "Falta RowMateBle.dll: el remo queda sin Bluetooth");
                        return null;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        logger.Log(LogLevel.Error, Tag, "RowMateBle.dll incompleta: el remo queda sin Bluetooth");
                        return null;
                    }

                default:
                    return null;
            }
#endif
        }
    }
}
