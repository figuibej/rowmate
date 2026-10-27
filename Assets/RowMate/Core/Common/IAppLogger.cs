using System;

namespace RowMate.Core.Common
{
    // Log de diagnóstico de la app. Se llama IAppLogger (y no ILogger) para no chocar con UnityEngine.ILogger
    // en los assemblies de Unity (D-03). Los tags son los del legacy, sin corchetes: "BLE", "HRM", "STRAVA_AUTH"…
    // En Unity lo implementa Debug.Log (It. 2); en los tests, RecordingLogger.
    public interface IAppLogger
    {
        void Log(LogLevel level, string tag, string message, Exception? error = null);
    }
}
