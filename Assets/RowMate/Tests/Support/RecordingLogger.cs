using System;
using System.Collections.Generic;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Logger que guarda cada línea como "[TAG] Level: mensaje" (los tags del legacy: [BLE], [HRM]…).
    // El tag se pasa sin corchetes ("BLE"); si ya los trae, se respeta.
    // Seguro entre hilos (It. 3, QA-G3-09): el hilo de la base (SerialExecutor) también registra. Log y Clear toman un lock,
    // y Lines, Errors y Levels devuelven una COPIA tomada con el lock (así una aserción no ve una lista que cambia).
    public sealed class RecordingLogger : IAppLogger
    {
        private readonly object _gate = new object();
        private readonly List<string> _lines = new List<string>();
        private readonly List<Exception?> _errors = new List<Exception?>();
        private readonly List<LogLevel> _levels = new List<LogLevel>();

        public IReadOnlyList<string> Lines
        {
            get
            {
                lock (_gate)
                {
                    return _lines.ToArray();
                }
            }
        }

        // Excepción de cada línea (null si no hubo), en el mismo orden que Lines.
        public IReadOnlyList<Exception?> Errors
        {
            get
            {
                lock (_gate)
                {
                    return _errors.ToArray();
                }
            }
        }

        public IReadOnlyList<LogLevel> Levels
        {
            get
            {
                lock (_gate)
                {
                    return _levels.ToArray();
                }
            }
        }

        public void Log(LogLevel level, string tag, string message, Exception? error = null)
        {
            var bracketed = tag != null && tag.StartsWith("[", StringComparison.Ordinal) ? tag : "[" + tag + "]";
            var line = bracketed + " " + level + ": " + message;
            lock (_gate)
            {
                _lines.Add(line);
                _errors.Add(error);
                _levels.Add(level);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _lines.Clear();
                _errors.Clear();
                _levels.Clear();
            }
        }

        // It. 4b: alguna línea contiene el texto (ordinal).
        public bool Contains(string text)
        {
            lock (_gate)
            {
                foreach (var line in _lines)
                {
                    if (line.IndexOf(text, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
