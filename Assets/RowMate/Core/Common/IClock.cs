using System;

namespace RowMate.Core.Common
{
    // Reloj inyectable (ADR 005, ARCHITECTURE §6). Usa DateTimeOffset y no DateTime (D-11): representa
    // "instante + zona" como el DateTime local de Dart, sin las trampas de DateTimeKind.
    public interface IClock
    {
        // Instante actual con offset 0.
        DateTimeOffset UtcNow { get; }

        // Hora local con su offset (los modelos usan DateTimeOffset, D-11).
        DateTimeOffset LocalNow { get; }

        // Reloj monótono en segundos, inmune a cambios de hora.
        double MonotonicSeconds { get; }
    }
}
