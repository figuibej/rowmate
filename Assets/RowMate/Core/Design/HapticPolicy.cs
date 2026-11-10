using RowMate.Core.Ports;

namespace RowMate.Core.Design
{
    // Cuándo se emite una respuesta táctil (It. 14, AC-POL-03, AC-DEBT-10; DT14-23; 00-arte §3.4):
    // - como mucho una cada 250 ms (de cualquier origen: el motor y el remo comparten la política);
    // - Success gana sobre Medium: si llega dentro de los 250 ms de un Medium, igual se emite (H-03 tapa a H-02); un Medium que
    //   llega después de un Success dentro de la ventana se pierde;
    // - en segundo plano, nada;
    // - un Confirm de reconexión que cae dentro de los 250 ms de otra se pierde (QA-G14-23: aceptado).
    // Sin reloj propio: el instante (segundos monótonos) llega por argumento.
    public sealed class HapticPolicy
    {
        public const int MinGapMs = 250;

        private bool _hasLast;
        private double _lastSeconds;
        private HapticKind _lastKind;

        public bool TryPlay(HapticKind kind, double nowSeconds, bool inBackground)
        {
            if (inBackground)
            {
                return false;
            }

            if (_hasLast && (nowSeconds - _lastSeconds) * 1000 < MinGapMs)
            {
                if (!(kind == HapticKind.Success && _lastKind == HapticKind.Medium))
                {
                    return false;
                }
            }

            _hasLast = true;
            _lastSeconds = nowSeconds;
            _lastKind = kind;
            return true;
        }
    }
}
