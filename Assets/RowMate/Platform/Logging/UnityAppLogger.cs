using System;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Platform.Logging
{
    // IAppLogger de la app (D-03): una línea "[TAG] mensaje" en el registro de Unity, con el nivel de Debug.Log*.
    // La excepción, si hay, va en la misma línea (mensaje y tipo) y después con Debug.LogException en nivel Error, así
    // el registro conserva la pila. Los tags son los del legacy sin corchetes ("BLE", "HRM", "SIM", "APP").
    public sealed class UnityAppLogger : IAppLogger
    {
        public void Log(LogLevel level, string tag, string message, Exception? error = null)
        {
            var line = "[" + (tag ?? "") + "] " + (message ?? "");
            if (error != null && level != LogLevel.Error)
            {
                line += " (" + error.GetType().Name + ": " + error.Message + ")";
            }

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(line);
                    if (error != null)
                    {
                        Debug.LogException(error);
                    }

                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }
        }
    }
}
