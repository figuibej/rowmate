// Banderas de compilación del composition root.
// legacy: lib/core/dev/dev_config.dart:5 (`const bool kSimulator = bool.fromEnvironment('SIMULATOR')`).
// En Unity el modo simulador es el define ROWMATE_SIMULATOR (ARCHITECTURE §4.2); ningún build de distribución lo define (R-01).
namespace RowMate.Bootstrap
{
    public static class BuildFlags
    {
#if ROWMATE_SIMULATOR
        public const bool IsSimulator = true;
#else
        public const bool IsSimulator = false;
#endif
    }
}
