using System;
using System.Collections.Generic;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Logger que guarda cada línea como "[TAG] Level: mensaje" (los tags del legacy: [BLE], [HRM]…).
    // El tag se pasa sin corchetes ("BLE"); si ya los trae, se respeta.
    public sealed class RecordingLogger : IAppLogger
    {
        private readonly List<string> _lines = new List<string>();
        private readonly List<Exception?> _errors = new List<Exception?>();
        private readonly List<LogLevel> _levels = new List<LogLevel>();

        public IReadOnlyList<string> Lines => _lines;

        // Excepción de cada línea (null si no hubo), en el mismo orden que Lines.
        public IReadOnlyList<Exception?> Errors => _errors;

        public IReadOnlyList<LogLevel> Levels => _levels;

        public void Log(LogLevel level, string tag, string message, Exception? error = null)
        {
            var bracketed = tag != null && tag.StartsWith("[", StringComparison.Ordinal) ? tag : "[" + tag + "]";
            _lines.Add(bracketed + " " + level + ": " + message);
            _errors.Add(error);
            _levels.Add(level);
        }

        public void Clear()
        {
            _lines.Clear();
            _errors.Clear();
            _levels.Clear();
        }
    }
}
