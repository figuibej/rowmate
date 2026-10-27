using System;

namespace RowMate.Core.Common
{
    // Logger que descarta todo.
    public sealed class NullAppLogger : IAppLogger
    {
        public static readonly NullAppLogger Instance = new NullAppLogger();

        private NullAppLogger()
        {
        }

        public void Log(LogLevel level, string tag, string message, Exception? error = null)
        {
        }
    }
}
